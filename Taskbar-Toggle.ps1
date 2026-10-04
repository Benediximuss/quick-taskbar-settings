$ErrorActionPreference = "Stop"

$registryPath = "HKCU:\Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"
$registryValueName = "MMTaskbarMode"
$scriptPath = Join-Path $PSScriptRoot "Taskbar-Universal-v22.ps1"

try {
    if (-not (Test-Path -LiteralPath $registryPath)) {
        throw "Görev çubuğu ayarlarının bulunduğu kayıt defteri anahtarı bulunamadı."
    }

    try {
        $settings = Get-ItemProperty -LiteralPath $registryPath -Name $registryValueName -ErrorAction Stop
    } catch {
        throw "Kayıt defterindeki '$registryValueName' değeri okunamadı. Ayrıntı: $($_.Exception.Message)"
    }

    $rawMode = $settings.$registryValueName
    if ($rawMode -isnot [int] -and $rawMode -isnot [long]) {
        throw "Kayıt defterindeki '$registryValueName' değeri bir tam sayı değil."
    }

    $currentMode = [int]$rawMode

    if ($currentMode -notin @(0, 1, 2)) {
        throw "Kayıt defterindeki '$registryValueName' değeri tanınmıyor: $currentMode. Beklenen değerler 0, 1 veya 2."
    }

    # MMTaskbarMode değerleri: 0 = All, 1 = MainAndWindow, 2 = Window.
    # Kullanıcı genellikle All ile Window arasında geçiş yaptığı için
    # 0 ve 1 -> Window moduna; 2 ise -> All moduna geçirilir. Böylece 1 değeri de Window'a düşer.
    if ($currentMode -in @(0, 1)) {
        $targetMode = "Window"
    } else {
        $targetMode = "All"
    }

    if ($targetMode -notin @("All", "MainAndWindow", "Window")) {
        throw "Hedef mod geçersiz: [$targetMode]. Geçerli modlar: All, MainAndWindow, Window."
    }

    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) {
        throw "Ayar değiştirme betiği bulunamadı: $scriptPath"
    }

    $modeScriptPath = '"' + $scriptPath + '"'
    $powershellPath = Join-Path $PSHOME "powershell.exe"
    if (-not (Test-Path -LiteralPath $powershellPath -PathType Leaf)) {
        throw "Windows PowerShell çalıştırılabilir dosyası bulunamadı: $powershellPath"
    }

    Write-Host "Geçerli görev çubuğu modu: $currentMode. [$targetMode] moduna geçiliyor."

    try {
        $process = Start-Process -FilePath $powershellPath `
            -ArgumentList @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $modeScriptPath, "-Mode", $targetMode) `
            -NoNewWindow -Wait -PassThru -ErrorAction Stop
    } catch {
        throw "Ayar değiştirme betiği başlatılamadı. Ayrıntı: $($_.Exception.Message)"
    }

    if ($process.ExitCode -ne 0) {
        throw "Ayar değiştirme betiği başarısız oldu (çıkış kodu: $($process.ExitCode)). Yukarıdaki hata ayrıntılarını inceleyin."
    }

    if ($env:TASKBAR_TOGGLE_LAUNCHED_FROM_CMD -eq "1") {
        exit 0
    }
    return
}
catch {
    $errorMessage = $_.Exception.Message
    Write-Host ""
    Write-Host "HATA: Görev çubuğu modu değiştirilemedi." -ForegroundColor Red
    Write-Host $errorMessage -ForegroundColor Red
    Write-Host ""
    if ($env:TASKBAR_TOGGLE_LAUNCHED_FROM_CMD -eq "1") {
        Write-Host "Pencereyi kapatmak için bir tuşa basın..."
        exit 1
    }

    try {
        Write-Host "Pencereyi kapatmak için Enter tuşuna basın..."
        Read-Host | Out-Null
    } catch {}
    return
}
