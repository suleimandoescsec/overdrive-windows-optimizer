param([Parameter(Mandatory=$true)][string]$Compiler,[string]$ReleaseDirectory=(Split-Path -Parent $PSScriptRoot))
$ErrorActionPreference='Stop'
$compilerPath=(Resolve-Path -LiteralPath $Compiler).Path
$releasePath=(Resolve-Path -LiteralPath $ReleaseDirectory).Path
if([IO.Path]::GetFileName($compilerPath) -ine 'ISCC.exe'){throw 'Use the official Inno Setup compiler, ISCC.exe.'}
if(-not (Test-Path -LiteralPath (Join-Path $releasePath 'Overdrive.exe'))){throw 'Build and stage Overdrive.exe first.'}
& $compilerPath ('/DReleaseDir='+$releasePath) (Join-Path $PSScriptRoot 'Installer\Overdrive.iss')
if($LASTEXITCODE -ne 0){throw 'Installer compilation failed.'}
Write-Output (Join-Path $releasePath 'Overdrive-Setup-0.1.0.exe')
