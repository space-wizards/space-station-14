if(!(Test-Path -Path "C:/byond")){
    bash tools/appveyor/download_byond.sh
    [System.IO.Compression.ZipFile]::ExtractToDirectory("C:/byond.zip", "C:/")
    Remove-Item C:/byond.zip
}

Set-Location $env:APPVEYOR_BUILD_FOLDER

&"C:/byond/bin/dm.exe" -max_errors 0 tgstation.dme
exit $LASTEXITCODE