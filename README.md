# OVERDRIVE

Open Overdrive.exe and press **OPTIMIZE MY PC**. Windows may ask for admin access.

The button applies the aggressive Windows settings profile, disables selected OneDrive, Docker Desktop and Teams startup entries, switches to High Performance power, and deletes temp files older than seven days. Locked, changed, or unsafe temp files are skipped. Deleted files cannot be restored by the app.

The app saves Windows-setting and startup journals, a power-plan receipt, and its last result in %LOCALAPPDATA%\Overdrive. The current single-screen build does not have an undo button. It does not remove apps, close running processes, disable Windows security, change pagefile or hibernation settings, or guarantee higher FPS.

This preview targets 64-bit Windows and requires .NET Framework 4.8. Opening the app makes no changes. Settings and features vary by PC; some changes need sign-out or restart. Software tests passed, but the combined button has not been run end to end on a real PC.

Build the included source with Build.ps1. See THIRD-PARTY-NOTICES.md for references. The portrait is supplied by the project owner and its rights are separate from the code license.
## Command line preview

`npx --yes github:suleimandoescsec/overdrive-windows-optimizer` opens the optimizer and requests admin access. It runs the optimization after access is granted. Old temporary files are deleted. Use `node cli.js --preview` to see the terminal portrait without changes. This is an early preview; the combined workflow has not been tested across PCs.
