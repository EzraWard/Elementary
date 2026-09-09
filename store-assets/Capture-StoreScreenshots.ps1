param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'gallery'),
    [string]$BackdropPath = (Join-Path $PSScriptRoot 'hero\elementary-super-hero-1920x1080.png'),
    [int]$CanvasWidth = 1920,
    [int]$CanvasHeight = 1080
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
    public const int DWMWA_EXTENDED_FRAME_BOUNDS = 9;

    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hWnd, IntPtr deviceContext, uint flags);

    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(
        IntPtr hWnd,
        int attribute,
        out RECT value,
        int valueSize);

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

function Get-VisibleWindowRect([IntPtr]$WindowHandle) {
    $rect = New-Object ElementaryStoreCaptureNative+RECT
    $result = [ElementaryStoreCaptureNative]::DwmGetWindowAttribute(
        $WindowHandle,
        [ElementaryStoreCaptureNative]::DWMWA_EXTENDED_FRAME_BOUNDS,
        [ref]$rect,
        [Runtime.InteropServices.Marshal]::SizeOf($rect))

    if ($result -ne 0) {
        if (-not [ElementaryStoreCaptureNative]::GetWindowRect($WindowHandle, [ref]$rect)) {
            throw 'Could not read the Elementary window bounds.'
        }
    }

    return $rect
}

function New-RoundedRectanglePath(
    [System.Drawing.Rectangle]$Rectangle,
    [int]$Radius
) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $Radius * 2
    $arc = New-Object System.Drawing.Rectangle(
        $Rectangle.X,
        $Rectangle.Y,
        $diameter,
        $diameter)

    $path.AddArc($arc, 180, 90)
    $arc.X = $Rectangle.Right - $diameter
    $path.AddArc($arc, 270, 90)
    $arc.Y = $Rectangle.Bottom - $diameter
    $path.AddArc($arc, 0, 90)
    $arc.X = $Rectangle.Left
    $path.AddArc($arc, 90, 90)
    $path.CloseFigure()
    return $path
}

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
    $visibleRect = Get-VisibleWindowRect $WindowHandle
    $rawRect = New-Object ElementaryStoreCaptureNative+RECT
    if (-not [ElementaryStoreCaptureNative]::GetWindowRect($WindowHandle, [ref]$rawRect)) {
        throw 'Could not read the Elementary window bounds.'
    }

    $windowWidth = $visibleRect.Right - $visibleRect.Left
    $windowHeight = $visibleRect.Bottom - $visibleRect.Top
    $rawWidth = $rawRect.Right - $rawRect.Left
    $rawHeight = $rawRect.Bottom - $rawRect.Top
    $rawBitmap = New-Object System.Drawing.Bitmap($rawWidth, $rawHeight)
    $rawGraphics = [System.Drawing.Graphics]::FromImage($rawBitmap)
    $deviceContext = $rawGraphics.GetHdc()

    try {
        # PrintWindow renders the actual DWM frame even when the capture script
        # runs without an interactive foreground desktop.
        if (-not [ElementaryStoreCaptureNative]::PrintWindow($WindowHandle, $deviceContext, 2)) {
            throw 'Could not render the Elementary window.'
        }
    }
    finally {
        $rawGraphics.ReleaseHdc($deviceContext)
        $rawGraphics.Dispose()
    }

    $windowBitmap = New-Object System.Drawing.Bitmap($windowWidth, $windowHeight)
    $windowGraphics = [System.Drawing.Graphics]::FromImage($windowBitmap)
    try {
        $sourceX = $visibleRect.Left - $rawRect.Left
        $sourceY = $visibleRect.Top - $rawRect.Top
        $windowGraphics.DrawImage(
            $rawBitmap,
            (New-Object System.Drawing.Rectangle(0, 0, $windowWidth, $windowHeight)),
            $sourceX,
            $sourceY,
            $windowWidth,
            $windowHeight,
            [System.Drawing.GraphicsUnit]::Pixel)
    }
    finally {
        $windowGraphics.Dispose()
        $rawBitmap.Dispose()
    }

    $canvas = New-Object System.Drawing.Bitmap($CanvasWidth, $CanvasHeight)
    $graphics = [System.Drawing.Graphics]::FromImage($canvas)
    $backgroundBitmap = [System.Drawing.Bitmap]::new($BackgroundImagePath)

    try {
        # The hero source includes partial alpha. Flatten it over the darkest
        # navy from the artwork so every Store screenshot is fully opaque.
        $graphics.Clear([System.Drawing.Color]::FromArgb(255, 0, 18, 48))
        $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
        $graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
        $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
        $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

        # Center-crop the hero only when a non-default canvas aspect ratio is
        # requested. The default 1920x1080 canvas uses every source pixel.
        $targetAspect = $CanvasWidth / [double]$CanvasHeight
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
            (New-Object System.Drawing.Rectangle(0, 0, $CanvasWidth, $CanvasHeight)),
            $cropX,
            $cropY,
            $cropWidth,
            $cropHeight,
            [System.Drawing.GraphicsUnit]::Pixel)

        # Slightly subdue the backdrop so the captured UI remains dominant.
        $shadeBrush = New-Object System.Drawing.SolidBrush(
            [System.Drawing.Color]::FromArgb(28, 0, 0, 0))
        try {
            $graphics.FillRectangle($shadeBrush, 0, 0, $CanvasWidth, $CanvasHeight)
        }
        finally {
            $shadeBrush.Dispose()
        }

        # Keep the captured window at its native size and center it on the Store
        # canvas. DWM's extended frame bounds exclude the invisible resize border
        # and drop shadow, so no source pixels need to be cropped or stretched.
        if ($windowWidth -gt $CanvasWidth -or $windowHeight -gt $CanvasHeight) {
            throw "The captured app window (${windowWidth}x${windowHeight}) does not fit the Store canvas (${CanvasWidth}x${CanvasHeight})."
        }

        $destinationX = [int][Math]::Round(($CanvasWidth - $windowWidth) / 2.0)
        $destinationY = [int][Math]::Round(($CanvasHeight - $windowHeight) / 2.0)
        $destinationRect = New-Object System.Drawing.Rectangle(
            $destinationX,
            $destinationY,
            $windowWidth,
            $windowHeight)

        # A subtle rounded shadow separates the real app window from the hero.
        $shadowRect = New-Object System.Drawing.Rectangle(
            ($destinationRect.X + 8),
            ($destinationRect.Y + 12),
            $destinationRect.Width,
            $destinationRect.Height)
        $shadowPath = New-RoundedRectanglePath $shadowRect 12
        $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(78, 0, 0, 0))
        try {
            $graphics.FillPath($shadowBrush, $shadowPath)
        }
        finally {
            $shadowBrush.Dispose()
            $shadowPath.Dispose()
        }

        # The pixels outside Windows 11's rounded DWM corners contain whatever
        # was behind the live window. Clip them away so the hero shows through.
        $windowPath = New-RoundedRectanglePath $destinationRect 10
        $graphicsState = $graphics.Save()
        try {
            $graphics.SetClip($windowPath)
            $graphics.DrawImageUnscaled($windowBitmap, $destinationX, $destinationY)
        }
        finally {
            $graphics.Restore($graphicsState)
            $windowPath.Dispose()
        }

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
$automationHandle = $handle

# Center the live app on the primary display's usable area before every capture.
# DWM's exact visible bounds are then placed at native resolution on the canvas.
$workingArea = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
$captureWindowWidth = 1500
$captureWindowHeight = 900
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

$root = Get-AutomationRoot $automationHandle
if (-not (Test-Path -LiteralPath $BackdropPath -PathType Leaf)) {
    throw "The Store screenshot backdrop was not found: $BackdropPath"
}

# Reset navigation to a known state so a previously open flyout cannot leak into
# the first frame when the script is re-run during review.
Invoke-NamedElement $root 'Settings'
Invoke-NamedElement $root 'Bible'
Save-StoreCapture $handle (Join-Path $OutputDirectory '01-reader.png') $BackdropPath

Invoke-NamedElement $root 'Search'
$root = Get-AutomationRoot $automationHandle
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
        $root = Get-AutomationRoot $automationHandle
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
