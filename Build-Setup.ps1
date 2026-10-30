$ErrorActionPreference = 'Stop'
& (Join-Path $PSScriptRoot 'Build.ps1')
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$bin = Join-Path $PSScriptRoot 'bin'
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:Microsoft.CSharp.dll ('/out:'+(Join-Path $bin 'Overdrive-Setup.exe')) ('/resource:'+(Join-Path $bin 'Overdrive.exe')+',Overdrive.exe') ('/resource:'+(Join-Path $bin 'Overdrive.exe.config')+',Overdrive.exe.config') (Join-Path $PSScriptRoot 'Installer\Setup.cs')
if ($LASTEXITCODE -ne 0) { throw 'Setup build failed' }
Write-Output (Join-Path $bin 'Overdrive-Setup.exe')
