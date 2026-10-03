param(
    [ValidateSet("All", "MainAndWindow", "Window")]
    [string]$Mode = "All"
)

$ErrorActionPreference = "Stop"

$targetComboName = "Birden çok ekran kullanırken görev çubuğu uygulamalarımı şurada göster:"

$optionNames = @{
    All           = "Tüm görev çubukları"
    MainAndWindow = "Ana görev çubuğu ve pencerenin açıldığı görev çubuğu"
    Window        = "Pencerenin açıldığı görev çubuğu"
}

$targetOptionName = $optionNames[$Mode]
$settings = $null

function Close-Settings {
    if ($script:settings -and $script:settings.Current.NativeWindowHandle) {
        try {
            [WindowCloser]::PostMessage(
                [IntPtr]$script:settings.Current.NativeWindowHandle,
                0x0010,
                [IntPtr]::Zero,
                [IntPtr]::Zero
            ) | Out-Null
        } catch {}
    }
}

function Wait-Until {
    param(
        [scriptblock]$Condition,
        [int]$TimeoutMs = 5000,
        [int]$IntervalMs = 50
    )

    $timer = [Diagnostics.Stopwatch]::StartNew()

    while ($timer.ElapsedMilliseconds -lt $TimeoutMs) {
        $result = & $Condition

        if ($result) {
            $timer.Stop()
            return $result
        }

        Start-Sleep -Milliseconds $IntervalMs
    }

    $timer.Stop()
    return $null
}

try {
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes

    Add-Type @"
using System;
using System.Runtime.InteropServices;

public static class WindowCloser {
    [DllImport("user32.dll")]
    public static extern bool PostMessage(
        IntPtr hWnd,
        uint msg,
        IntPtr wParam,
        IntPtr lParam);
}
"@

    # Ayarlar'ı aç
    Start-Process "ms-settings:taskbar"

    $root = [System.Windows.Automation.AutomationElement]::RootElement

    $settingsCondition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ClassNameProperty,
        "ApplicationFrameWindow"
    )

    # Ayarlar penceresi hazır olduğu anda devam et.
    $settings = Wait-Until {
        $root.FindFirst(
            [System.Windows.Automation.TreeScope]::Children,
            $settingsCondition
        )
    }

    if (-not $settings) {
        throw "Ayarlar penceresi 5 saniye içinde bulunamadı."
    }

    $script:settings = $settings

    $comboType = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ComboBox
    )

    # Hedef ComboBox hazır olduğu anda devam et.
    $combo = Wait-Until {
        $found = $null

        $combos = $settings.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            $comboType
        )

        foreach ($candidate in $combos) {
            if ($candidate.Current.Name -eq $targetComboName) {
                $found = $candidate
                break
            }
        }

        $found
    }

    if (-not $combo) {
        throw "Hedef ComboBox 5 saniye içinde bulunamadı. Ayarlar arayüzü değişmiş olabilir."
    }

    # Açılır listeyi aç
    $combo.GetCurrentPattern(
        [System.Windows.Automation.ExpandCollapsePattern]::Pattern
    ).Expand()

    $itemType = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::ListItem
    )

    # İstenen seçenek hazır olduğu anda devam et.
    $option = Wait-Until {
        $found = $null

        $items = $combo.FindAll(
            [System.Windows.Automation.TreeScope]::Descendants,
            $itemType
        )

        foreach ($item in $items) {
            if ($item.Current.Name -eq $targetOptionName) {
                $found = $item
                break
            }
        }

        $found
    }

    if (-not $option) {
        throw "İstenen seçenek 5 saniye içinde bulunamadı: [$targetOptionName]"
    }

    # Seçimi uygula
    $selection = $option.GetCurrentPattern(
        [System.Windows.Automation.SelectionItemPattern]::Pattern
    )

    $selection.Select()

    # Gerçekten seçildiğini doğrula.
    $verified = Wait-Until {
        try {
            if ($selection.Current.IsSelected) {
                return $true
            }
        } catch {}

        return $false
    } -TimeoutMs 3000

    if (-not $verified) {
        throw "Seçim gönderildi ancak Windows seçimi doğrulamadı: [$targetOptionName]"
    }

    Write-Host "Tamamlandı ve doğrulandı: [$targetOptionName]"
}
catch {
    Write-Error "Taskbar ayarı değiştirilemedi: $($_.Exception.Message)"
    exit 1
}
finally {
    Close-Settings
}
