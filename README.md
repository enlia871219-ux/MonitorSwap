# Monitor Swap — Build by Gamgo

Portable Windows utility that swaps the positions of windows between the first two monitors detected by Windows.

## Usage

- Hotkey: **Ctrl + Shift + Z**
- Run `MonitorSwap.exe`; the app stays in the system tray.
- Right-click the tray icon to see the disabled, bold **Build by Gamgo** credit at the top, the swap action, and Exit.
- No AutoHotkey installation is required.

## Build on Windows

1. Keep `Build.bat`, `MonitorSwap.cs`, and `Icon_Gamgo.ico` together in the same folder.
2. Double-click `Build.bat`.
3. The build creates `MonitorSwap.exe` in that folder and embeds `Icon_Gamgo.ico` as the executable icon.

The build uses the .NET Framework C# compiler (`csc.exe`) and references Windows Forms and Drawing.

## Notes

- The implementation uses the first two monitors in Windows monitor-enumeration order. This may not always match the display numbers shown in Windows Settings.
- Visible titled windows are moved; minimized, untitled, and tool windows are skipped.
- Full-screen/exclusive games and elevated windows may not move as expected.
