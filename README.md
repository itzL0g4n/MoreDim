# MoreDim

MoreDim is a lightweight Windows desktop utility that dims your displays beyond the minimum brightness available in Windows. It places a translucent black overlay over each connected monitor, so you can keep working comfortably in a dark room.

> MoreDim changes the appearance of the screen with an overlay. It does not change your monitor's hardware brightness or Windows display settings.

## Features

- Adjust additional dimming from 0% to 85%.
- Apply the overlay to every connected display.
- Continue using apps normally; mouse and keyboard input passes through the overlay.
- Keep MoreDim running from the Windows notification area after closing its settings window.
- Reopen the controls by double-clicking the tray icon. Right-click the icon to toggle dimming or exit.

## Requirements

- Windows 10 or later.
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) to build and run from source.

## Run from source

Clone the repository, open PowerShell in the project directory, and run:

```powershell
dotnet run
```

The app opens with 35% additional dimming enabled. Adjust the slider in the settings window to change the level.

## Publish a standalone app

To publish a self-contained 64-bit Windows build that does not require the .NET runtime to be installed:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true
```

The published files are written to:

```text
bin/Release/net8.0-windows/win-x64/publish/
```

Run `MoreDim.exe` from that directory. For an ARM64 Windows device, replace `win-x64` with `win-arm64`.

## How it works and limitations

MoreDim creates a click-through black overlay window on each display. This is a software dimming effect; it may appear in screenshots and screen recordings, and it does not lower the panel's physical backlight power. Dimming settings are not saved after the app exits.

## License

No license has been selected for this project yet. Add a `LICENSE` file before redistributing the code under open-source terms.
