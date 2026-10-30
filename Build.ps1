$ErrorActionPreference='Stop'
$compiler=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
$out=Join-Path $PSScriptRoot 'bin'
New-Item -ItemType Directory -Path $out -Force | Out-Null
$sources=Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' | Select-Object -ExpandProperty FullName
& $compiler /nologo /target:winexe /platform:x64 /optimize+ /warn:4 ('/win32manifest:'+(Join-Path $PSScriptRoot 'app.manifest')) ('/out:'+(Join-Path $out 'Overdrive.exe')) /reference:System.dll /reference:System.Core.dll /reference:System.Management.dll /reference:System.Web.Extensions.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll ("/resource:"+(Join-Path $PSScriptRoot "Assets\portrait.png")+",Overdrive.Portrait") $sources
if($LASTEXITCODE -ne 0){throw 'Build failed.'}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Overdrive.exe.config') -Destination $out -Force
Write-Output (Join-Path $out 'Overdrive.exe')
