# MoreDim

MoreDim is a lightweight Windows desktop utility that dims your displays beyond the minimum brightness available in Windows. It places a translucent black overlay over each connected monitor, so you can keep working comfortably in a dark room.

> MoreDim changes the appearance of the screen with an overlay. It does not change your monitor's hardware brightness or Windows display settings.

## Features

- Adjust additional dimming from 0% to 85%.
- Apply the overlay to every connected display.
- Continue using apps normally; mouse and keyboard input passes through the overlay.
- Choose whether closing the settings window minimizes MoreDim to the notification area or exits and removes the dimming overlay.
- Optionally start MoreDim when you sign in to Windows.
- Reopen the controls by double-clicking the tray icon. Right-click the icon to toggle dimming or quit.

## Requirements

- Windows 10 or later.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build and run from source.

## Download

Download the latest Windows executable from the [GitHub Releases page](../../releases/latest). The release asset is a self-contained, single-file x64 build and does not require the .NET runtime to be installed.

## Run from source

Clone the repository, open PowerShell in the project directory, and run:

```powershell
dotnet run
```

The app opens with 35% additional dimming enabled. Adjust the slider in the settings window to change the level. By default, closing the window exits MoreDim and removes the overlay. Enable **Minimize to tray when closing with X** to keep it running in the notification area; use **Quit** in the tray menu to exit completely.

Enable **Start with Windows** in the settings window to launch MoreDim when you sign in. This setting applies to your Windows user account.

## Publish a standalone app

To publish a self-contained, single-file 64-bit Windows executable that does not require the .NET runtime to be installed:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

The executable is written to:

```text
bin/Release/net8.0-windows/win-x64/publish/
```

Upload `MoreDim.exe` from that directory as an asset on the GitHub Release for each version. For an ARM64 Windows device, replace `win-x64` with `win-arm64`.

## How it works and limitations

MoreDim creates a click-through black overlay window on each display. This is a software dimming effect; it may appear in screenshots and screen recordings, and it does not lower the panel's physical backlight power. The dimming level resets to 35% when the app restarts; the startup and minimize-to-tray preferences are saved per user.

## License

MoreDim is licensed under the [MIT License](LICENSE).
