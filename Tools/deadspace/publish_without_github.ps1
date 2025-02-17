#Code by HacksLua(discord) for DeadSpace/LuaWorld
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$utf8Encoding = [System.Text.Encoding]::UTF8
[System.Console]::InputEncoding = $utf8Encoding
[System.Console]::OutputEncoding = $utf8Encoding

function Backup-BinDirectory {
    param (
        [string]$binPath
    )
    if (Test-Path -Path $binPath) {
        $backupPath = "${binPath}Backup"
        if (Test-Path -Path $backupPath) {
            Remove-Item -Recurse -Force -Path $backupPath
        }
        Rename-Item -Path $binPath -NewName $backupPath
        Write-Host "Папка bin переименована в binBackup." -ForegroundColor Yellow
    } else {
        Write-Host "Папка bin не найдена." -ForegroundColor Yellow
    }
}

function Restore-BinDirectory {
    param (
        [string]$binPath
    )
    $backupPath = "${binPath}Backup"
    if (Test-Path -Path $binPath) {
        Write-Host "Удаление папки bin..." -ForegroundColor Yellow
        Remove-Item -Recurse -Force -Path $binPath
        while (Test-Path -Path $binPath) {
            Start-Sleep -Seconds 1
        }
        Write-Host "Папка bin удалена." -ForegroundColor Green
    }
    if (Test-Path -Path $backupPath) {
        Rename-Item -Path $backupPath -NewName $binPath
        Write-Host "Папка binBackup переименована обратно в bin." -ForegroundColor Green
    } else {
        Write-Host "Резервная папка binBackup не найдена." -ForegroundColor Red
    }
}

function Read-ConfigFile {
    param (
        [string]$filePath
    )
    if (-not (Test-Path -Path $filePath)) {
        Write-Host "Файл токена не найден. Укажите корректный путь." -ForegroundColor Red
        return $null
    }
    try {
        $config = @{}

        Get-Content -Path $filePath | ForEach-Object {
            $split = $_ -split "="
            if ($split.Count -ne 2) {
                throw "Ошибка в строке конфигурации: $_"
            }
            $key = $split[0].Trim()
            $value = $split[1].Trim()
            $config[$key] = $value
        }
        return $config
    } catch {
        Write-Host "Ошибка при чтении файла конфигурации: $_" -ForegroundColor Red
        return $null
    }
}

function Get-EngineVersion {
    param (
        [string]$filePath
    )
    if (-not (Test-Path -Path $filePath)) {
        Write-Host "Файл версии движка не найден по пути $filePath." -ForegroundColor Red
        return $null
    }
    try {
        $xml = [xml](Get-Content -Path $filePath)
        return $xml.Project.PropertyGroup.Version
    } catch {
        Write-Host "Ошибка при чтении версии движка: $_" -ForegroundColor Red
        return $null
    }
}

$scriptPath = $MyInvocation.MyCommand.Path
$deadspacePath = (Split-Path -Path $scriptPath -Parent)
$rootPath = (Split-Path -Path (Split-Path -Path (Split-Path -Path $scriptPath -Parent) -Parent) -Parent)
Set-Location -Path $rootPath

$binPath = "bin"

Backup-BinDirectory -binPath $binPath

$tokenFilePath = "C:\token.txt" # Установить свой путь или оставить текущий (его в любом случае запросит)
$config = Read-ConfigFile -filePath $tokenFilePath
$engineVersionFile = Join-Path -Path $rootPath -ChildPath "RobustToolbox\\MSBuild\\Robust.Engine.Version.props"
$releaseDir = "release"
$tempClientDir = "temp_client"

while (-not (Test-Path -Path $tokenFilePath)) {
    Write-Host "Файл токенов не найден по пути $tokenFilePath." -ForegroundColor Red
    $tokenFilePath = Read-Host "Укажите путь до файла токенов"
}

$expectedKeys = @("cdnUrl", "updateToken", "privateUsername", "privatePassword")
$missingKeys = @()
foreach ($key in $expectedKeys) {
    if (-not $config.ContainsKey($key)) {
        $missingKeys += $key
    }
}

if ($missingKeys.Count -gt 0) {
    Write-Host "Следующие параметры отсутствуют в конфигурационном файле:" -ForegroundColor Red
    $missingKeys | ForEach-Object { Write-Host "- $_" -ForegroundColor Red }
    return
}

$cdnUrl = $config["cdnUrl"]
$updateToken = $config["updateToken"]
$privateUsername = $config["privateUsername"]
$privatePassword = $config["privatePassword"]

$version = (Get-Date -Format "ddMMyyyyHHmmss") # Авто установка версии

$engineVersionFile = "RobustToolbox\MSBuild\Robust.Engine.Version.props" # Авто установка версии движка
if (-not (Test-Path -Path $engineVersionFile)) {
    Write-Host "Не удалось найти файл версии движка: $engineVersionFile" -ForegroundColor Red
    return
}
$engineVersion = Select-String -Path $engineVersionFile -Pattern "<Version>(.*?)</Version>" | ForEach-Object {
    $_.Matches.Groups[1].Value
}

Write-Host "Установлена версия: $version" -ForegroundColor Green
Write-Host "Установлен движок: $engineVersion" -ForegroundColor Green

try {
    Write-Host "Начало сборки..." -ForegroundColor Green

Write-Host "Проверка доступности Robust.Cdn..." -ForegroundColor Yellow
try {
    $authHeader = @{
        "Authorization" = "Basic " + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("${privateUsername}:${privatePassword}"))
    }
    Invoke-RestMethod -Uri "$cdnUrl/manifest" `
                      -Method Get `
                      -Headers $authHeader `
                      -ErrorAction Stop
    Write-Host "Robust.Cdn доступен." -ForegroundColor Green
} catch {
    Write-Host "Robust.Cdn недоступен или указан неверный Fork ID." -ForegroundColor Red
    return
}

foreach ($dir in @($releaseDir, $tempClientDir)) {
    if (Test-Path -Path $dir) {
        Remove-Item -Recurse -Force -Path $dir
    }
    New-Item -ItemType Directory -Path $dir | Out-Null
}

cd 
Write-Host "Сборка клиентского пакета..." -ForegroundColor Green
dotnet build Content.Packaging --configuration Release
dotnet run --project Content.Packaging client --no-wipe-release

Write-Host "Перемещение клиентских файлов..." -ForegroundColor Yellow
Get-ChildItem -Path $releaseDir -Filter "SS14.Client*" | ForEach-Object {
    Move-Item -Path $_.FullName -Destination $tempClientDir
}

Write-Host "Сборка серверных пакетов..." -ForegroundColor Green
dotnet run --project Content.Packaging server --platform linux-x64 # --platform win-x64 --platform osx-x64 --platform linux-arm64 # Раскоментировать если нужны другие версии

Write-Host "Возврат клиентских файлов..." -ForegroundColor Yellow
Get-ChildItem -Path $tempClientDir | ForEach-Object {
    Move-Item -Path $_.FullName -Destination $releaseDir
}

Remove-Item -Recurse -Force -Path $tempClientDir

Write-Host "Повторная проверка доступности Robust.Cdn..." -ForegroundColor Yellow
try {
    $authHeader = @{
        "Authorization" = "Basic " + [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes("${privateUsername}:${privatePassword}"))
    }
    Invoke-RestMethod -Uri "$cdnUrl/manifest" `
                      -Method Get `
                      -Headers $authHeader `
                      -ErrorAction Stop
    Write-Host "Robust.Cdn доступен." -ForegroundColor Green
} catch {
    Write-Host "Robust.Cdn недоступен или указан неверный Fork ID." -ForegroundColor Red
    return
}

Write-Host "Начало публикации в CDN..." -ForegroundColor Green
try {
    Invoke-RestMethod -Uri "$cdnUrl/publish/start" `
                      -Method Post `
                      -Headers @{
                          "Authorization" = "Bearer $updateToken"
                          "Content-Type" = "application/json"
                      } `
                      -Body (@{ version = $version; engineVersion = $engineVersion } | ConvertTo-Json -Depth 10)
    Write-Host "Публикация успешно начата." -ForegroundColor Green
} catch {
    Write-Host "Ошибка при запуске публикации." -ForegroundColor Red
    return
}

Write-Host "Загрузка файлов..." -ForegroundColor Green
foreach ($file in Get-ChildItem -Path $releaseDir -Filter *.zip) {
    try {
        $filePath = $file.FullName
        $fileName = $file.Name
        $fileContent = [System.IO.File]::ReadAllBytes($filePath)
        Invoke-RestMethod -Uri "$cdnUrl/publish/file" `
                          -Method Post `
                          -Headers @{
                              "Authorization" = "Bearer $updateToken"
                              "Robust-Cdn-Publish-File" = $fileName
                              "Robust-Cdn-Publish-Version" = $version
                          } `
                          -Body $fileContent -ContentType "application/octet-stream"
        Write-Host "$fileName успешно загружен." -ForegroundColor Green
    } catch {
        Write-Host "Ошибка при загрузке $fileName." -ForegroundColor Red
    }
}

Write-Host "Завершение публикации..." -ForegroundColor Green
try {
    Invoke-RestMethod -Uri "$cdnUrl/publish/finish" `
                      -Method Post `
                      -Headers @{
                          "Authorization" = "Bearer $updateToken"
                          "Content-Type" = "application/json"
                      } `
                      -Body (@{ version = $version } | ConvertTo-Json -Depth 10)
    Write-Host "Публикация успешно завершена." -ForegroundColor Green
} catch {
    Write-Host "Ошибка при завершении публикации." -ForegroundColor Red
}

Write-Host "Очистка временных файлов..." -ForegroundColor Yellow
foreach ($dir in @($releaseDir)) {
    if (Test-Path -Path $dir) {
        Remove-Item -Recurse -Force -Path $dir
    }
}
Write-Host "Очистка завершена." -ForegroundColor Green

    Write-Host "Сборка завершена." -ForegroundColor Green
} catch {
    Write-Host "Ошибка при сборке: $_" -ForegroundColor Red
} finally {
    Write-Host "Удаление временных папок..." -ForegroundColor Yellow
    foreach ($dir in @($releaseDir, $tempClientDir)) {
        if (Test-Path -Path $dir) {
            Remove-Item -Recurse -Force -Path $dir
        }
    }
    Write-Host "Временные папки удалены." -ForegroundColor Green
    Restore-BinDirectory -binPath $binPath
    Set-Location -Path $deadspacePath
}
Write-Host "Нажмите любую клавишу для выхода..." -ForegroundColor Yellow
Read-Host
