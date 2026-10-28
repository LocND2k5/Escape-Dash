Add-Type -AssemblyName System.Drawing

function New-RoundedRectanglePath {
    param(
        [System.Drawing.RectangleF]$rect,
        [float]$radius
    )
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $diameter = $radius * 2

    # Top-Left
    $path.AddArc($rect.X, $rect.Y, $diameter, $diameter, 180, 90)
    # Top edge & Top-Right
    $path.AddArc($rect.Right - $diameter, $rect.Y, $diameter, $diameter, 270, 90)
    # Right edge & Bottom-Right
    $path.AddArc($rect.Right - $diameter, $rect.Bottom - $diameter, $diameter, $diameter, 0, 90)
    # Bottom edge & Bottom-Left
    $path.AddArc($rect.X, $rect.Bottom - $diameter, $diameter, $diameter, 90, 90)
    $path.CloseFigure()
    return $path
}

$outDir = "Assets\Sprites\Leaderboard"

# ==========================================
# 1. board_background.png (512x512, 9-sliceable)
# ==========================================
$bmpBoard = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmpBoard)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Outer glow / shadow
for ($i = 0; $i -lt 6; $i++) {
    $shadowRect = New-Object System.Drawing.RectangleF (12 - $i), (14 - $i), (488 + $i * 2), (488 + $i * 2)
    $shadowPath = New-RoundedRectanglePath $shadowRect (48 + $i)
    $shadowBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(12, 0, 0, 0))
    $g.FillPath($shadowBrush, $shadowPath)
    $shadowBrush.Dispose()
    $shadowPath.Dispose()
}

# Outer border (warm light cream / white outline)
$outerRect = New-Object System.Drawing.RectangleF 16, 16, 480, 480
$outerPath = New-RoundedRectanglePath $outerRect 44
$outerBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 248, 235))
$g.FillPath($outerBrush, $outerPath)
$outerBrush.Dispose()

# Inner warm orange body
$innerRect = New-Object System.Drawing.RectangleF 24, 24, 464, 464
$innerPath = New-RoundedRectanglePath $innerRect 38
$innerBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $innerRect,
    [System.Drawing.Color]::FromArgb(255, 253, 175, 54), # Sunny orange top
    [System.Drawing.Color]::FromArgb(255, 245, 148, 26), # Rich golden orange bottom
    [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
)
$g.FillPath($innerBrush, $innerPath)
$innerBrush.Dispose()

# Subtle inner stroke
$innerStrokePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(120, 220, 115, 0), 3)
$g.DrawPath($innerStrokePen, $innerPath)
$innerStrokePen.Dispose()
$innerPath.Dispose()
$outerPath.Dispose()

$g.Dispose()
$bmpBoard.Save("$outDir\board_background.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpBoard.Dispose()

# ==========================================
# 2. header_banner.png (512x136)
# ==========================================
$bmpBanner = New-Object System.Drawing.Bitmap(512, 136)
$g = [System.Drawing.Graphics]::FromImage($bmpBanner)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Outer pill border (Dark caramel / brown-orange border)
$bannerOuterRect = New-Object System.Drawing.RectangleF 10, 10, 492, 116
$bannerOuterPath = New-RoundedRectanglePath $bannerOuterRect 58
$bannerOuterBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $bannerOuterRect,
    [System.Drawing.Color]::FromArgb(255, 205, 102, 12),
    [System.Drawing.Color]::FromArgb(255, 160, 72, 4),
    [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
)
$g.FillPath($bannerOuterBrush, $bannerOuterPath)
$bannerOuterBrush.Dispose()

# Inner golden pill
$bannerInnerRect = New-Object System.Drawing.RectangleF 20, 20, 472, 96
$bannerInnerPath = New-RoundedRectanglePath $bannerInnerRect 48
$bannerInnerBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $bannerInnerRect,
    [System.Drawing.Color]::FromArgb(255, 254, 184, 52),
    [System.Drawing.Color]::FromArgb(255, 243, 142, 22),
    [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
)
$g.FillPath($bannerInnerBrush, $bannerInnerPath)
$bannerInnerBrush.Dispose()

# Inner thin highlight at top
$hlRect = New-Object System.Drawing.RectangleF 30, 24, 452, 44
$hlPath = New-RoundedRectanglePath $hlRect 22
$hlBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(60, 255, 255, 255))
$g.FillPath($hlBrush, $hlPath)
$hlBrush.Dispose()
$hlPath.Dispose()

$bannerInnerPath.Dispose()
$bannerOuterPath.Dispose()
$g.Dispose()
$bmpBanner.Save("$outDir\header_banner.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpBanner.Dispose()

# ==========================================
# 3. crown.png (256x140)
# ==========================================
$bmpCrown = New-Object System.Drawing.Bitmap(256, 140)
$g = [System.Drawing.Graphics]::FromImage($bmpCrown)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Crown Polygon points
$points = @(
    New-Object System.Drawing.PointF 32, 115    # Bottom Left
    New-Object System.Drawing.PointF 20, 52     # Prong 1 tip
    New-Object System.Drawing.PointF 65, 82     # Valley 1
    New-Object System.Drawing.PointF 92, 38     # Prong 2 tip
    New-Object System.Drawing.PointF 112, 75    # Valley 2
    New-Object System.Drawing.PointF 128, 22    # Center Prong tip (tallest)
    New-Object System.Drawing.PointF 144, 75    # Valley 3
    New-Object System.Drawing.PointF 164, 38    # Prong 4 tip
    New-Object System.Drawing.PointF 191, 82    # Valley 4
    New-Object System.Drawing.PointF 236, 52    # Prong 5 tip
    New-Object System.Drawing.PointF 224, 115   # Bottom Right
)

# Crown base path
$crownPath = New-Object System.Drawing.Drawing2D.GraphicsPath
$crownPath.AddLines($points)
$crownPath.CloseFigure()

# Fill crown
$crownBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.RectangleF 0, 20, 256, 100),
    [System.Drawing.Color]::FromArgb(255, 255, 200, 32),
    [System.Drawing.Color]::FromArgb(255, 235, 138, 10),
    [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
)
$g.FillPath($crownBrush, $crownPath)

# Crown outline / border
$crownPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 205, 105, 8), 5)
$crownPen.LineJoin = [System.Drawing.Drawing2D.LineJoin]::Round
$g.DrawPath($crownPen, $crownPath)
$crownPen.Dispose()
$crownBrush.Dispose()
$crownPath.Dispose()

# Spheres on tips
$tips = @(
    @{ X = 20;  Y = 52; R = 11 },
    @{ X = 92;  Y = 38; R = 12 },
    @{ X = 128; Y = 22; R = 14 },
    @{ X = 164; Y = 38; R = 12 },
    @{ X = 236; Y = 52; R = 11 }
)

foreach ($tip in $tips) {
    $x = $tip.X - $tip.R
    $y = $tip.Y - $tip.R
    $d = $tip.R * 2
    $ballRect = New-Object System.Drawing.RectangleF $x, $y, $d, $d
    
    $ballBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $ballRect,
        [System.Drawing.Color]::FromArgb(255, 255, 230, 90),
        [System.Drawing.Color]::FromArgb(255, 230, 130, 10),
        [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
    )
    $g.FillEllipse($ballBrush, $ballRect)
    $ballBrush.Dispose()

    $ballPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 205, 105, 8), 3.5)
    $g.DrawEllipse($ballPen, $ballRect)
    $ballPen.Dispose()
}

$g.Dispose()
$bmpCrown.Save("$outDir\crown.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpCrown.Dispose()

# ==========================================
# 4. close_button.png (128x128)
# ==========================================
$bmpClose = New-Object System.Drawing.Bitmap(128, 128)
$g = [System.Drawing.Graphics]::FromImage($bmpClose)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Outer white ring / glow
$outerRingRect = New-Object System.Drawing.RectangleF 8, 8, 112, 112
$outerRingBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
$g.FillEllipse($outerRingBrush, $outerRingRect)
$outerRingBrush.Dispose()

# Shadow border
$midRect = New-Object System.Drawing.RectangleF 13, 13, 102, 102
$midBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 185, 28, 28))
$g.FillEllipse($midBrush, $midRect)
$midBrush.Dispose()

# Red glossy circle
$redRect = New-Object System.Drawing.RectangleF 16, 16, 96, 96
$redBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $redRect,
    [System.Drawing.Color]::FromArgb(255, 239, 68, 68),
    [System.Drawing.Color]::FromArgb(255, 185, 28, 28),
    [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
)
$g.FillEllipse($redBrush, $redRect)
$redBrush.Dispose()

# Top highlight
$topHlRect = New-Object System.Drawing.RectangleF 28, 20, 72, 40
$g.FillEllipse((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(65, 255, 255, 255))), $topHlRect)

# White 'X'
$xPen = New-Object System.Drawing.Pen([System.Drawing.Color]::White, 14)
$xPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
$xPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
$g.DrawLine($xPen, 42, 42, 86, 86)
$g.DrawLine($xPen, 86, 42, 42, 86)
$xPen.Dispose()

$g.Dispose()
$bmpClose.Save("$outDir\close_button.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpClose.Dispose()

# ==========================================
# 5. rank_badge.png (128x128, rounded square)
# ==========================================
$bmpRank = New-Object System.Drawing.Bitmap(128, 128)
$g = [System.Drawing.Graphics]::FromImage($bmpRank)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Outer white border
$rankOuterRect = New-Object System.Drawing.RectangleF 8, 8, 112, 112
$rankOuterPath = New-RoundedRectanglePath $rankOuterRect 24
$rankOuterBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
$g.FillPath($rankOuterBrush, $rankOuterPath)
$rankOuterBrush.Dispose()

# Dark red bevel base
$rankMidRect = New-Object System.Drawing.RectangleF 13, 13, 102, 102
$rankMidPath = New-RoundedRectanglePath $rankMidRect 20
$rankMidBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 160, 20, 20))
$g.FillPath($rankMidBrush, $rankMidPath)
$rankMidBrush.Dispose()
$rankMidPath.Dispose()

# Vibrant red inner
$rankInnerRect = New-Object System.Drawing.RectangleF 15, 15, 98, 98
$rankInnerPath = New-RoundedRectanglePath $rankInnerRect 18
$rankInnerBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rankInnerRect,
    [System.Drawing.Color]::FromArgb(255, 238, 45, 45),
    [System.Drawing.Color]::FromArgb(255, 185, 24, 24),
    [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
)
$g.FillPath($rankInnerBrush, $rankInnerPath)
$rankInnerBrush.Dispose()

# Top gloss
$rankGlossRect = New-Object System.Drawing.RectangleF 20, 18, 88, 44
$rankGlossPath = New-RoundedRectanglePath $rankGlossRect 12
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(55, 255, 255, 255))), $rankGlossPath)
$rankGlossPath.Dispose()

$rankInnerPath.Dispose()
$rankOuterPath.Dispose()
$g.Dispose()
$bmpRank.Save("$outDir\rank_badge.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpRank.Dispose()

# ==========================================
# 6. score_badge.png (256x96, pill shaped)
# ==========================================
$bmpScore = New-Object System.Drawing.Bitmap(256, 96)
$g = [System.Drawing.Graphics]::FromImage($bmpScore)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.Clear([System.Drawing.Color]::Transparent)

# Outer dark border
$scoreOuterRect = New-Object System.Drawing.RectangleF 8, 8, 240, 80
$scoreOuterPath = New-RoundedRectanglePath $scoreOuterRect 38
$scoreOuterBrush = New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(255, 130, 52, 10))
$g.FillPath($scoreOuterBrush, $scoreOuterPath)
$scoreOuterBrush.Dispose()

# Rich caramel brown body
$scoreInnerRect = New-Object System.Drawing.RectangleF 12, 12, 232, 72
$scoreInnerPath = New-RoundedRectanglePath $scoreInnerRect 34
$scoreInnerBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $scoreInnerRect,
    [System.Drawing.Color]::FromArgb(255, 182, 85, 20),
    [System.Drawing.Color]::FromArgb(255, 150, 62, 12),
    [System.Drawing.Drawing2D.LinearGradientMode]::Vertical
)
$g.FillPath($scoreInnerBrush, $scoreInnerPath)
$scoreInnerBrush.Dispose()

# Subtle top highlight
$scoreHlRect = New-Object System.Drawing.RectangleF 22, 16, 212, 30
$scoreHlPath = New-RoundedRectanglePath $scoreHlRect 14
$g.FillPath((New-Object System.Drawing.SolidBrush ([System.Drawing.Color]::FromArgb(40, 255, 255, 255))), $scoreHlPath)
$scoreHlPath.Dispose()

$scoreInnerPath.Dispose()
$scoreOuterPath.Dispose()
$g.Dispose()
$bmpScore.Save("$outDir\score_badge.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpScore.Dispose()

# ==========================================
# 7. row_divider.png (256x8)
# ==========================================
$bmpDivider = New-Object System.Drawing.Bitmap(256, 8)
$g = [System.Drawing.Graphics]::FromImage($bmpDivider)
$g.Clear([System.Drawing.Color]::FromArgb(180, 220, 130, 20)) # Dark warm golden orange
$g.Dispose()
$bmpDivider.Save("$outDir\row_divider.png", [System.Drawing.Imaging.ImageFormat]::Png)
$bmpDivider.Dispose()

Write-Host "All Leaderboard sprites generated successfully!"
