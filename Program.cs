using Microsoft.Win32;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace MoreDim;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new DimController());
    }
}

internal static class NativeMethods
{
    internal const uint WindowDisplayAffinityExcludeFromCapture = 0x00000011;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool SetWindowDisplayAffinity(IntPtr windowHandle, uint affinity);
}

internal sealed class DimController : ApplicationContext
{
    private const string StartupRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private static readonly string SettingsFilePath = Path.Combine(AppContext.BaseDirectory, "MoreDim.settings.json");
    private readonly NotifyIcon tray;
    private readonly List<DimOverlay> overlays = [];
    private readonly ToolStripMenuItem enabledItem;
    private SettingsForm? settings;
    private int dimPercent = 35;
    private bool enabled = true;
    private bool minimizeToTray = ReadMinimizePreference();
    private bool startWithWindows = IsRegisteredForStartup();
    private bool shuttingDown;

    public DimController()
    {
        enabledItem = new ToolStripMenuItem("Disable dimming", null, (_, _) => SetEnabled(!enabled)) { CheckOnClick = false };
        var menu = new ContextMenuStrip();
        menu.Items.Add("Adjust dimming…", null, (_, _) => ShowSettings());
        menu.Items.Add(enabledItem);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Quit", null, (_, _) => ExitThread());
        tray = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = "MoreDim — Dim your displays",
            ContextMenuStrip = menu,
            Visible = true
        };
        tray.DoubleClick += (_, _) => ShowSettings();
        SystemEvents.DisplaySettingsChanged += OnDisplaySettingsChanged;
        RebuildOverlays();
        ShowSettings();
    }

    private void OnDisplaySettingsChanged(object? sender, EventArgs e)
    {
        var window = Application.OpenForms.Cast<Form>().FirstOrDefault();
        if (window is null) return;
        window.BeginInvoke(new Action(RebuildOverlays));
    }

    private void RebuildOverlays()
    {
        foreach (var overlay in overlays) overlay.Close();
        overlays.Clear();
        foreach (var screen in Screen.AllScreens)
        {
            var overlay = new DimOverlay { Bounds = screen.Bounds, Dim = dimPercent / 100d, Enabled = enabled };
            overlays.Add(overlay);
            overlay.Show();
        }
        settings?.BringToFront();
    }

    private void SetDim(int value)
    {
        dimPercent = Math.Clamp(value, 0, 85);
        foreach (var overlay in overlays) overlay.Dim = dimPercent / 100d;
        settings?.UpdateValue(dimPercent);
    }

    private void SetEnabled(bool value)
    {
        enabled = value;
        enabledItem.Text = value ? "Disable dimming" : "Enable dimming";
        foreach (var overlay in overlays) overlay.Enabled = value;
        settings?.UpdateEnabled(value);
    }

    private static bool ReadMinimizePreference()
    {
        try
        {
            if (!File.Exists(SettingsFilePath)) return false;
            using var document = JsonDocument.Parse(File.ReadAllText(SettingsFilePath));
            return document.RootElement.TryGetProperty("minimizeToTray", out var preference) && preference.GetBoolean();
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or System.Security.SecurityException) { return false; }
    }

    private static bool IsRegisteredForStartup()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath);
            return key?.GetValue("MoreDim") is string;
        }
        catch (UnauthorizedAccessException) { return false; }
        catch (System.Security.SecurityException) { return false; }
    }

    private void SetMinimizeToTray(bool value)
    {
        try
        {
            File.WriteAllText(SettingsFilePath, JsonSerializer.Serialize(new { minimizeToTray = value }));
            minimizeToTray = value;
        }
        catch (Exception error)
        {
            settings?.UpdateMinimizeToTray(minimizeToTray);
            MessageBox.Show(settings, $"Could not save this setting next to MoreDim.exe.\n\n{error.Message}", "MoreDim", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetStartWithWindows(bool value)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(StartupRegistryPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(StartupRegistryPath)
                ?? throw new InvalidOperationException("Could not open the Windows startup registry key.");

            if (value)
                key.SetValue("MoreDim", $"\"{Application.ExecutablePath}\"", RegistryValueKind.String);
            else
                key.DeleteValue("MoreDim", throwOnMissingValue: false);

            startWithWindows = value;
        }
        catch (Exception error)
        {
            settings?.UpdateStartWithWindows(startWithWindows);
            MessageBox.Show(settings, $"Windows blocked access to the startup setting. Run MoreDim outside the restricted development environment, then try again.\n\n{error.Message}", "MoreDim", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowSettings()
    {
        if (settings is null || settings.IsDisposed)
        {
            settings = new SettingsForm(dimPercent, enabled, minimizeToTray, startWithWindows, SetDim, SetEnabled, SetMinimizeToTray, SetStartWithWindows, ExitThread);
            settings.FormClosing += (_, e) =>
            {
                if (shuttingDown || e.CloseReason != CloseReason.UserClosing || !minimizeToTray) return;
                e.Cancel = true;
                settings.Hide();
            };
            settings.FormClosed += (_, _) =>
            {
                settings = null;
                if (!shuttingDown) ExitThread();
            };
        }
        settings.Show();
        settings.WindowState = FormWindowState.Normal;
        settings.TopMost = true;
        settings.BringToFront();
        settings.Activate();
    }

    protected override void ExitThreadCore()
    {
        shuttingDown = true;
        SystemEvents.DisplaySettingsChanged -= OnDisplaySettingsChanged;
        tray.Visible = false;
        tray.Dispose();
        foreach (var overlay in overlays) overlay.Close();
        settings?.Close();
        base.ExitThreadCore();
    }
}

internal sealed class DimOverlay : Form
{
    private double dim;
    private bool isEnabled;

    public double Dim
    {
        get => dim;
        set { dim = Math.Clamp(value, 0, .85); Opacity = isEnabled ? dim : 0; }
    }

    public new bool Enabled
    {
        get => isEnabled;
        set { isEnabled = value; Opacity = value ? dim : 0; }
    }

    public DimOverlay()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        BackColor = Color.Black;
        TopMost = true;
        DoubleBuffered = true;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ExStyle |= 0x00080000 | 0x00000020 | 0x08000000; // layered, transparent, no-activate
            return cp;
        }
    }

    protected override bool ShowWithoutActivation => true;

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        _ = NativeMethods.SetWindowDisplayAffinity(Handle, NativeMethods.WindowDisplayAffinityExcludeFromCapture);
    }

}

internal sealed class SettingsForm : Form
{
    private readonly TrackBar slider;
    private readonly Label valueLabel;
    private readonly CheckBox enabledBox;
    private readonly CheckBox minimizeBox;
    private readonly CheckBox startupBox;
    private readonly Action<int> setDim;
    private readonly Action<bool> setEnabled;
    private readonly Action<bool> setMinimizeToTray;
    private readonly Action<bool> setStartWithWindows;
    private readonly Action quit;
    private bool syncingOptions;

    public SettingsForm(int value, bool enabled, bool minimizeToTray, bool startWithWindows, Action<int> onDim, Action<bool> onEnabled, Action<bool> onMinimizeToTray, Action<bool> onStartWithWindows, Action onQuit)
    {
        setDim = onDim;
        setEnabled = onEnabled;
        setMinimizeToTray = onMinimizeToTray;
        setStartWithWindows = onStartWithWindows;
        quit = onQuit;
        Text = "MoreDim — Display dimming";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = true;
        ShowInTaskbar = true;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(520, 340);
        BackColor = Color.FromArgb(248, 249, 252);
        Font = new Font("Segoe UI", 10);

        var title = new Label { Text = "Dim your displays", Font = new Font("Segoe UI Semibold", 17), AutoSize = true, Location = new Point(24, 20) };
        var subtitle = new Label
        {
            Text = "Dims all connected displays with a dark overlay.",
            ForeColor = Color.DimGray,
            AutoSize = false,
            Location = new Point(26, 61),
            Size = new Size(468, 24)
        };
        valueLabel = new Label { AutoSize = true, Location = new Point(26, 108), Font = new Font("Segoe UI Semibold", 11) };
        slider = new TrackBar { Minimum = 0, Maximum = 85, TickFrequency = 10, SmallChange = 1, LargeChange = 5, Value = value, Location = new Point(18, 132), Size = new Size(484, 48) };
        slider.ValueChanged += (_, _) => { setDim(slider.Value); UpdateValue(slider.Value); };
        enabledBox = new CheckBox { Text = "Enable dimming", Checked = enabled, AutoSize = true, Location = new Point(27, 192) };
        enabledBox.CheckedChanged += (_, _) => { if (!syncingOptions) setEnabled(enabledBox.Checked); };
        minimizeBox = new CheckBox { Text = "Minimize to tray when closing with X", Checked = minimizeToTray, AutoSize = true, Location = new Point(27, 223) };
        minimizeBox.CheckedChanged += (_, _) => { if (!syncingOptions) setMinimizeToTray(minimizeBox.Checked); };
        startupBox = new CheckBox { Text = "Start with Windows", Checked = startWithWindows, AutoSize = true, Location = new Point(27, 254) };
        startupBox.CheckedChanged += (_, _) => { if (!syncingOptions) setStartWithWindows(startupBox.Checked); };
        var trayHint = new Label
        {
            Text = "If minimized, quit from the tray icon menu.",
            AutoSize = false,
            ForeColor = Color.DimGray,
            Location = new Point(27, 296),
            Size = new Size(360, 24)
        };
        var quitButton = new Button
        {
            Text = "Quit",
            AutoSize = false,
            FlatStyle = FlatStyle.System,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
            Location = new Point(412, 288),
            Size = new Size(84, 32)
        };
        quitButton.Click += (_, _) => quit();
        Controls.AddRange([title, subtitle, valueLabel, slider, enabledBox, minimizeBox, startupBox, trayHint, quitButton]);
        UpdateValue(value);
    }

    public void UpdateValue(int value)
    {
        valueLabel.Text = $"Additional dimming: {value}%";
        if (slider.Value != value) slider.Value = value;
    }

    public void UpdateEnabled(bool value)
    {
        syncingOptions = true;
        enabledBox.Checked = value;
        syncingOptions = false;
    }

    public void UpdateMinimizeToTray(bool value)
    {
        syncingOptions = true;
        minimizeBox.Checked = value;
        syncingOptions = false;
    }

    public void UpdateStartWithWindows(bool value)
    {
        syncingOptions = true;
        startupBox.Checked = value;
        syncingOptions = false;
    }
}
