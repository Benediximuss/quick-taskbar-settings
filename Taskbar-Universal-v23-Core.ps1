function Write-TaskbarProbeTiming {
    param(
        [Parameter(Mandatory)][string]$Stage,
        [Parameter(Mandatory)][System.Diagnostics.Stopwatch]$Timer,
        [Parameter(Mandatory)][ref]$PreviousMarkMs
    )

    $elapsedMs = $Timer.Elapsed.TotalMilliseconds
    $stageMs = $elapsedMs - $PreviousMarkMs.Value
    Write-Host ("[PROBE][v23] {0}: bu aşama {1:N0} ms, toplam {2:N0} ms" -f $Stage, $stageMs, $elapsedMs)
    $PreviousMarkMs.Value = $elapsedMs
}

function Wait-TaskbarCondition {
    param(
        [Parameter(Mandatory)][scriptblock]$Condition,
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

function Invoke-TaskbarMode {
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidateSet("All", "MainAndWindow", "Window")]
        [string]$Mode,

        [int]$ForegroundProcessId = 0,

        [switch]$Probe
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
    $probeTimer = $null
    $probeLastMarkMs = 0.0

    if ($Probe) {
        $probeTimer = [Diagnostics.Stopwatch]::StartNew()
    }

    try {
        Add-Type -AssemblyName UIAutomationClient
        Add-Type -AssemblyName UIAutomationTypes

        if (-not ("WindowCloser" -as [type])) {
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

    [DllImport("user32.dll")]
    public static extern bool AllowSetForegroundWindow(uint dwProcessId);
}
"@
        }

        if ($Probe) {
            Write-TaskbarProbeTiming -Stage "PowerShell içinde UI Automation hazırlandı" -Timer $probeTimer -PreviousMarkMs ([ref]$probeLastMarkMs)
        }

        Start-Process "ms-settings:taskbar"
        if ($Probe) {
            Write-TaskbarProbeTiming -Stage "Ayarlar sayfası için başlatma isteği gönderildi" -Timer $probeTimer -PreviousMarkMs ([ref]$probeLastMarkMs)
        }

        $root = [System.Windows.Automation.AutomationElement]::RootElement
        $settingsCondition = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ClassNameProperty,
            "ApplicationFrameWindow"
        )

        $settings = Wait-TaskbarCondition -Condition {
            $root.FindFirst(
                [System.Windows.Automation.TreeScope]::Children,
                $settingsCondition
            )
        }

        if (-not $settings) {
            throw "Ayarlar penceresi 5 saniye içinde bulunamadı."
        }
        if ($Probe) {
            Write-TaskbarProbeTiming -Stage "Ayarlar penceresi bulundu" -Timer $probeTimer -PreviousMarkMs ([ref]$probeLastMarkMs)
        }

        $comboType = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::ComboBox
        )

        $combo = Wait-TaskbarCondition -Condition {
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
        if ($Probe) {
            Write-TaskbarProbeTiming -Stage "Hedef ComboBox bulundu" -Timer $probeTimer -PreviousMarkMs ([ref]$probeLastMarkMs)
        }

        $combo.GetCurrentPattern(
            [System.Windows.Automation.ExpandCollapsePattern]::Pattern
        ).Expand()

        $itemType = New-Object System.Windows.Automation.PropertyCondition(
            [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
            [System.Windows.Automation.ControlType]::ListItem
        )

        $option = Wait-TaskbarCondition -Condition {
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
        if ($Probe) {
            Write-TaskbarProbeTiming -Stage "Hedef seçenek bulundu" -Timer $probeTimer -PreviousMarkMs ([ref]$probeLastMarkMs)
        }

        $selection = $option.GetCurrentPattern(
            [System.Windows.Automation.SelectionItemPattern]::Pattern
        )
        $selection.Select()

        $verified = Wait-TaskbarCondition -Condition {
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
        if ($Probe) {
            Write-TaskbarProbeTiming -Stage "Seçim doğrulandı" -Timer $probeTimer -PreviousMarkMs ([ref]$probeLastMarkMs)
        }

        Write-Host "Tamamlandı ve doğrulandı: [$targetOptionName]"
    } catch {
        throw "Görev çubuğu ayarı değiştirilemedi: $($_.Exception.Message)"
    } finally {
        if ($settings -and $settings.Current.NativeWindowHandle) {
            try {
                $settingsWindowHandle = [IntPtr]$settings.Current.NativeWindowHandle
                [WindowCloser]::PostMessage(
                    $settingsWindowHandle,
                    0x0010,
                    [IntPtr]::Zero,
                    [IntPtr]::Zero
                ) | Out-Null
            } catch {}
        }

        if ($ForegroundProcessId -gt 0) {
            try {
                [WindowCloser]::AllowSetForegroundWindow([uint32]$ForegroundProcessId) | Out-Null
            } catch {}
        }

        if ($Probe) {
            Write-TaskbarProbeTiming -Stage "Ayarlar penceresini kapatma isteği gönderildi" -Timer $probeTimer -PreviousMarkMs ([ref]$probeLastMarkMs)
        }
    }
}
