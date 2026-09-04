param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'gallery'),
    [string]$BackdropPath = (Join-Path $PSScriptRoot 'hero\elementary-super-hero-1920x1080.png')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName UIAutomationClient

Add-Type @'
using System;
using System.Runtime.InteropServices;

public static class ElementaryStoreCaptureNative
{
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    public static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(
        IntPtr hWnd,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
'@

function Get-AppFrame {
    $deadline = [DateTime]::UtcNow.AddSeconds(15)
    do {
        $process = Get-Process ApplicationFrameHost -ErrorAction SilentlyContinue |
            Where-Object { $_.MainWindowTitle -like 'Elementary*' } |
            Select-Object -First 1

        if ($process) {
            return $process
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw 'The Elementary app window was not found.'
}

function Get-AutomationRoot([IntPtr]$WindowHandle) {
    return [System.Windows.Automation.AutomationElement]::FromHandle($WindowHandle)
}

function Find-NamedElement($Root, [string]$Name) {
    $condition = New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name)
    return $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Invoke-NamedElement($Root, [string]$Name) {
    $element = Find-NamedElement $Root $Name
    if (-not $element) {
        throw "Could not find the '$Name' control."
    }

    $clickPoint = New-Object System.Windows.Point
    if (-not $element.TryGetClickablePoint([ref]$clickPoint)) {
        $bounds = $element.Current.BoundingRectangle
        if ($bounds.IsEmpty) {
            throw "The '$Name' control has no clickable point."
        }

        $clickPoint = New-Object System.Windows.Point(
            ($bounds.Left + ($bounds.Width / 2)),
            ($bounds.Top + ($bounds.Height / 2)))
    }

    [ElementaryStoreCaptureNative]::SetCursorPos(
        [int][Math]::Round($clickPoint.X),
        [int][Math]::Round($clickPoint.Y)) | Out-Null
    [ElementaryStoreCaptureNative]::mouse_event(0x0002, 0, 0, 0, [UIntPtr]::Zero)
    [ElementaryStoreCaptureNative]::mouse_event(0x0004, 0, 0, 0, [UIntPtr]::Zero)
    Start-Sleep -Milliseconds 800
}

function Save-StoreCapture(
    [IntPtr]$WindowHandle,
    [string]$Path,
    [string]$BackgroundImagePath
) {
    $rect = New-Object ElementaryStoreCaptureNative+RECT
    if (-not [ElementaryStoreCaptureNative]::GetWindowRect($WindowHandle, [ref]$rect)) {
        throw 'Could not read the Elementary window bounds.'
    }

    $windowWidth = $rect.Right - $rect.Left
    $windowHeight = $rect.Bottom - $rect.Top
    $windowBitmap = New-Object System.Drawing.Bitmap($windowWidth, $windowHeight)
    $windowGraphics = [System.Drawing.Graphics]::FromImage($windowBitmap)

    try {
        $windowGraphics.CopyFromScreen(
            $rect.Left,
            $rect.Top,
            0,
            0,
            (New-Object System.Drawing.Size($windowWidth, $windowHeight)),
            [System.Drawing.CopyPixelOperation]::SourceCopy)
    }
    finally {
        $windowGraphics.Dispose()
    }

    $canvas = New-Object System.Drawing.Bitmap(1600, 1200)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $backgroundBitmap = [System.Drawing.Bitmap]::new($BackgroundImagePath)

    try {
        # The hero source includes partial alpha. Flatten it over the darkest
        # navy from the artwork so every Store screenshot is fully opaque.
        $graphics.Clear([System.Drawing.Color]::FromArgb(255, 0, 18, 48))
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

        # Center-crop the 16:9 hero to fill the 4:3 Store canvas without
        # distorting it. The app window remains a direct, unmodified capture.
        $targetAspect = 1600.0 / 1200.0
        $sourceAspect = $backgroundBitmap.Width / $backgroundBitmap.Height
        if ($sourceAspect -gt $targetAspect) {
            $cropHeight = $backgroundBitmap.Height
            $cropWidth = [int][Math]::Round($cropHeight * $targetAspect)
            $cropX = [int][Math]::Round(($backgroundBitmap.Width - $cropWidth) / 2.0)
            $cropY = 0
        }
        else {
            $cropWidth = $backgroundBitmap.Width
            $cropHeight = [int][Math]::Round($cropWidth / $targetAspect)
            $cropX = 0
            $cropY = [int][Math]::Round(($backgroundBitmap.Height - $cropHeight) / 2.0)
        }

        $graphics.DrawImage(
            $backgroundBitmap,
            (New-Object System.Drawing.Rectangle(0, 0, 1600, 1200)),
            $cropX,
            $cropY,
            $cropWidth,
            $cropHeight,
            [System.Drawing.GraphicsUnit]::Pixel)

        # Slightly subdue the backdrop so the captured UI remains dominant.
        $shadeBrush = New-Object System.Drawing.SolidBrush(
            [System.Drawing.Color]::FromArgb(28, 0, 0, 0))
        try {
            $graphics.FillRectangle($shadeBrush, 0, 0, 1600, 1200)
        }
        finally {
            $shadeBrush.Dispose()
        }

        # A subtle shadow separates the real app window from the hero backdrop.
        $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(78, 0, 0, 0))
        try {
            $graphics.FillRectangle($shadowBrush, 68, 120, 1480, 980)
        }
        finally {
            $shadowBrush.Dispose()
        }

        # Trim the transparent DWM shadow around the source window. Without this,
        # pixels from the user's real desktop wallpaper can bleed into the art.
        $sourceInset = 10
        $graphics.DrawImage(
            $windowBitmap,
            (New-Object System.Drawing.Rectangle(60, 110, 1480, 980)),
            $sourceInset,
            $sourceInset,
            ($windowWidth - (2 * $sourceInset)),
            ($windowHeight - (2 * $sourceInset)),
            [System.Drawing.GraphicsUnit]::Pixel)

        $canvas.Save($Path, [System.Drawing.Imaging.ImageFormat]::Png)
    }
    finally {
        $graphics.Dispose()
        $canvas.Dispose()
        $backgroundBitmap.Dispose()
        $windowBitmap.Dispose()
    }
}

New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null

$frame = Get-AppFrame
$handle = $frame.MainWindowHandle

# Center the live app on the primary display's usable area before every capture.
# The 1500x1000 source leaves equal margins around the centered app in the
# final 1600x1200 Store image after the transparent DWM shadow is trimmed.
$workingArea = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$captureWindowWidth = 1500
$captureWindowHeight = 1000
$windowX = $workingArea.Left + [int](($workingArea.Width - $captureWindowWidth) / 2)
$windowY = $workingArea.Top + [int](($workingArea.Height - $captureWindowHeight) / 2)
[ElementaryStoreCaptureNative]::SetWindowPos(
    $handle,
    [IntPtr]::Zero,
    $windowX,
    $windowY,
    $captureWindowWidth,
    $captureWindowHeight,
    0) | Out-Null
[ElementaryStoreCaptureNative]::SetForegroundWindow($handle) | Out-Null
Start-Sleep -Seconds 2

$root = Get-AutomationRoot $handle
if (-not (Test-Path -LiteralPath $BackdropPath -PathType Leaf)) {
    throw "The Store screenshot backdrop was not found: $BackdropPath"
}

# Reset navigation to a known state so a previously open flyout cannot leak into
# the first frame when the script is re-run during review.
Invoke-NamedElement $root 'Settings'
Invoke-NamedElement $root 'Bible'
Save-StoreCapture $handle (Join-Path $OutputDirectory '01-reader.png') $BackdropPath

Invoke-NamedElement $root 'Search'
$root = Get-AutomationRoot $handle
$searchBox = $root.FindFirst(
    [System.Windows.Automation.TreeScope]::Descendants,
    (New-Object System.Windows.Automation.PropertyCondition(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        [System.Windows.Automation.ControlType]::Edit)))
if ($searchBox) {
    $searchBox.SetFocus()
    $valuePattern = $searchBox.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern)
    $valuePattern.SetValue('love')
    [System.Windows.Forms.SendKeys]::SendWait('{ENTER}')
    $searchDeadline = [DateTime]::UtcNow.AddSeconds(15)
    do {
        Start-Sleep -Milliseconds 500
        $root = Get-AutomationRoot $handle
        $firstSearchResult = Find-NamedElement $root 'Genesis 22:2'
    } while (-not $firstSearchResult -and [DateTime]::UtcNow -lt $searchDeadline)
    Start-Sleep -Milliseconds 800
}
Save-StoreCapture $handle (Join-Path $OutputDirectory '02-search.png') $BackdropPath
Invoke-NamedElement $root 'Search'

Invoke-NamedElement $root 'History'
Save-StoreCapture $handle (Join-Path $OutputDirectory '03-reading-history.png') $BackdropPath
Invoke-NamedElement $root 'History'

Invoke-NamedElement $root 'Streak'
Save-StoreCapture $handle (Join-Path $OutputDirectory '04-reading-streak.png') $BackdropPath

Invoke-NamedElement $root 'Settings'
Save-StoreCapture $handle (Join-Path $OutputDirectory '05-settings.png') $BackdropPath

$captureNames = @(
    '01-reader.png',
    '02-search.png',
    '03-reading-history.png',
    '04-reading-streak.png',
    '05-settings.png'
)

$captureNames |
    ForEach-Object { Get-Item (Join-Path $OutputDirectory $_) } |
    Sort-Object Name |
    Select-Object Name, Length, LastWriteTime
