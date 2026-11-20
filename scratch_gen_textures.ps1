Add-Type -AssemblyName System.Drawing
$outDir = "Assets\Art\Textures"
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }

function Save-Bmp($bmp, $name) {
    $path = Join-Path $outDir "$name.png"
    $bmp.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    $bmp.Dispose()
    Write-Output "Generated: $name.png"
}

# 1. Grass Striped (512x512)
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$baseGreen = [System.Drawing.Color]::FromArgb(35, 135, 42)
$stripeGreen = [System.Drawing.Color]::FromArgb(48, 160, 56)
$g.Clear($baseGreen)
$stripeBrush = New-Object System.Drawing.SolidBrush($stripeGreen)
for ($y = 0; $y -512; $y += 64) {
    $g.FillRectangle($stripeBrush, 0, $y, 512, 32)
}
# Turf grain
$rand = New-Object System.Random(42)
for ($i = 0; $i -lt 15000; $i++) {
    $rx = $rand.Next(0, 512)
    $ry = $rand.Next(0, 512)
    $val = $rand.Next(-18, 18)
    $pix = $bmp.GetPixel($rx, $ry)
    $nr = [Math]::Clamp($pix.R + $val, 0, 255)
    $ng = [Math]::Clamp($pix.G + $val + 4, 0, 255)
    $nb = [Math]::Clamp($pix.B + $val, 0, 255)
    $bmp.SetPixel($rx, $ry, [System.Drawing.Color]::FromArgb($nr, $ng, $nb))
}
$g.Dispose()
Save-Bmp $bmp "Tex_Grass_Striped"

# 2. Grass Night (512x512)
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(18, 75, 28))
$sb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(26, 95, 38))
for ($y = 0; $y -lt 512; $y += 48) {
    $g.FillRectangle($sb, 0, $y, 512, 24)
}
$g.Dispose()
Save-Bmp $bmp "Tex_Grass_Night"

# 3. Grass Pro Checkers (512x512)
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(30, 150, 50))
$sb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(40, 175, 60))
for ($y = 0; $y -lt 512; $y += 64) {
    for ($x = 0; $x -lt 512; $x += 64) {
        if ((($x / 64) + ($y / 64)) % 2 -eq 0) {
            $g.FillRectangle($sb, $x, $y, 64, 64)
        }
    }
}
$g.Dispose()
Save-Bmp $bmp "Tex_Grass_Pro"

# 4. Grass World Cup (512x512)
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(35, 165, 65))
$sb = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(45, 188, 78))
for ($y = 0; $y -lt 512; $y += 32) {
    $g.FillRectangle($sb, 0, $y, 512, 16)
}
$g.Dispose()
Save-Bmp $bmp "Tex_Grass_WorldCup"

# 5. Goal Net (256x256 Transparent)
$bmp = New-Object System.Drawing.Bitmap(256, 256, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::Transparent)
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(235, 240, 245), 2.5)
$knotBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 255, 255))
for ($i = -256; $i -le 512; $i += 24) {
    $g.DrawLine($pen, $i, 0, $i + 256, 256)
    $g.DrawLine($pen, $i, 256, $i + 256, 0)
}
for ($x = 0; $x -le 256; $x += 12) {
    for ($y = 0; $y -le 256; $y += 12) {
        if (($x + $y) % 24 -eq 0) {
            $g.FillEllipse($knotBrush, $x - 2, $y - 2, 4, 4)
        }
    }
}
$pen.Dispose()
$knotBrush.Dispose()
$g.Dispose()
Save-Bmp $bmp "Tex_Goal_Net"

# 6. Classic Ball (512x512)
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(245, 245, 248))
$patchBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(25, 25, 30))
$seamPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(170, 170, 180), 3)
$goldBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 205, 35))

$centers = @(
    [System.Drawing.Point]::new(256, 256),
    [System.Drawing.Point]::new(128, 128),
    [System.Drawing.Point]::new(384, 128),
    [System.Drawing.Point]::new(128, 384),
    [System.Drawing.Point]::new(384, 384),
    [System.Drawing.Point]::new(0, 256),
    [System.Drawing.Point]::new(512, 256),
    [System.Drawing.Point]::new(256, 0),
    [System.Drawing.Point]::new(256, 512)
)
foreach ($c in $centers) {
    # Draw pentagon-like polygon
    $pts = @()
    for ($a = 0; $a -lt 5; $a++) {
        $ang = $a * [Math]::PI * 2 / 5 - [Math]::PI / 2
        $px = $c.X + [Math]::Cos($ang) * 44
        $py = $c.Y + [Math]::Sin($ang) * 44
        $pts += [System.Drawing.PointF]::new($px, $py)
    }
    $g.FillPolygon($patchBrush, $pts)
    $g.DrawPolygon($seamPen, $pts)
    # Seam lines radiating out
    for ($a = 0; $a -lt 5; $a++) {
        $ang = $a * [Math]::PI * 2 / 5 - [Math]::PI / 2
        $px1 = $c.X + [Math]::Cos($ang) * 44
        $py1 = $c.Y + [Math]::Sin($ang) * 44
        $px2 = $c.X + [Math]::Cos($ang) * 75
        $py2 = $c.Y + [Math]::Sin($ang) * 75
        $g.DrawLine($seamPen, $px1, $py1, $px2, $py2)
    }
}
$g.FillEllipse($goldBrush, 242, 242, 28, 28)
$g.Dispose()
Save-Bmp $bmp "Tex_Ball_Classic"

# 7. Blue Ball (512x512)
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(10, 25, 75))
$cyanPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(0, 220, 255), 4)
$glowPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(180, 245, 255), 1.5)
$neonBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0, 160, 230))
for ($y = 0; $y -lt 512; $y += 64) {
    for ($x = 0; $x -lt 512; $x += 64) {
        $g.FillEllipse($neonBrush, $x + 12, $y + 12, 40, 40)
        $g.DrawRectangle($cyanPen, $x + 4, $y + 4, 56, 56)
        $g.DrawEllipse($glowPen, $x + 16, $y + 16, 32, 32)
    }
}
$g.Dispose()
Save-Bmp $bmp "Tex_Ball_Blue"

# 8. Fire Ball (512x512)
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(50, 10, 5))
$flameBrush1 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 75, 10))
$flameBrush2 = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 185, 20))
$emberPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 240, 120), 3)
for ($i = 0; $i -lt 300; $i++) {
    $fx = $rand.Next(0, 512)
    $fy = $rand.Next(0, 512)
    $fs = $rand.Next(15, 60)
    $g.FillEllipse($flameBrush1, $fx, $fy, $fs, $fs * 1.4)
    $g.FillEllipse($flameBrush2, $fx + 4, $fy + 4, $fs * 0.6, $fs * 0.8)
}
for ($i = 0; $i -lt 12; $i++) {
    $x1 = $rand.Next(0, 512); $y1 = $rand.Next(0, 512)
    $x2 = $rand.Next(0, 512); $y2 = $rand.Next(0, 512)
    $g.DrawLine($emberPen, $x1, $y1, $x2, $y2)
}
$g.Dispose()
Save-Bmp $bmp "Tex_Ball_Fire"

# 9. Rainbow Ball (512x512)
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$colors = @(
    [System.Drawing.Color]::FromArgb(255, 60, 120),
    [System.Drawing.Color]::FromArgb(255, 140, 30),
    [System.Drawing.Color]::FromArgb(255, 225, 40),
    [System.Drawing.Color]::FromArgb(40, 220, 100),
    [System.Drawing.Color]::FromArgb(30, 180, 255),
    [System.Drawing.Color]::FromArgb(150, 60, 255)
)
for ($i = 0; $i -lt $colors.Length; $i++) {
    $b = New-Object System.Drawing.SolidBrush($colors[$i])
    $g.FillRectangle($b, 0, $i * (512 / 6), 512, 512 / 6)
    $b.Dispose()
}
$facetPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 255, 255), 2.5)
for ($y = 0; $y -lt 512; $y += 64) {
    for ($x = 0; $x -lt 512; $x += 64) {
        $g.DrawLine($facetPen, $x, $y, $x + 64, $y + 64)
        $g.DrawLine($facetPen, $x + 64, $y, $x, $y + 64)
    }
}
$g.Dispose()
Save-Bmp $bmp "Tex_Ball_Rainbow"

# 10. Gold Ball (512x512)
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(215, 160, 25))
$lightGold = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 225, 70))
$shinePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 245, 180), 2)
for ($y = 0; $y -lt 512; $y += 48) {
    for ($x = 0; $x -lt 512; $x += 48) {
        $g.FillEllipse($lightGold, $x + 6, $y + 6, 36, 36)
        $g.DrawEllipse($shinePen, $x + 4, $y + 4, 40, 40)
    }
}
$g.Dispose()
Save-Bmp $bmp "Tex_Ball_Gold"

# 11. Jersey Ruben (#10 Ruben Gonzalez) (512x512)
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$navy = [System.Drawing.Color]::FromArgb(12, 35, 95)
$royal = [System.Drawing.Color]::FromArgb(22, 65, 175)
$gold = [System.Drawing.Color]::FromArgb(255, 210, 40)
$g.Clear($navy)
$royalBrush = New-Object System.Drawing.SolidBrush($royal)
for ($x = 0; $x -lt 512; $x += 48) {
    $g.FillRectangle($royalBrush, $x, 0, 24, 512)
}
$goldBrush = New-Object System.Drawing.SolidBrush($gold)
$g.FillRectangle($goldBrush, 0, 0, 512, 40) # Collar/shoulders
$g.FillRectangle($goldBrush, 0, 0, 30, 512)  # Side stripe
$g.FillRectangle($goldBrush, 482, 0, 30, 512)

# Chest crest A&R
$g.FillEllipse($goldBrush, 110, 80, 50, 50)
$blueInner = New-Object System.Drawing.SolidBrush($navy)
$g.FillEllipse($blueInner, 115, 85, 40, 40)
$fontCrest = New-Object System.Drawing.Font("Arial", 12, [System.Drawing.FontStyle]::Bold)
$fontWhite = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$g.DrawString("A&R", $fontCrest, $goldBrush, 118, 97)

# Back: RUBEN and 10
$fontName = New-Object System.Drawing.Font("Arial", 28, [System.Drawing.FontStyle]::Bold)
$fontNum = New-Object System.Drawing.Font("Arial", 88, [System.Drawing.FontStyle]::Bold)
$whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$g.DrawString("RUBEN", $fontName, $whiteBrush, 190, 180)
$g.DrawString("10", $fontNum, $goldBrush, 195, 230)
$g.Dispose()
Save-Bmp $bmp "Tex_Jersey_Ruben"

# 12. Jersey Malagon (#1 Malagon Goalkeeper) (512x512)
$bmp = New-Object System.Drawing.Bitmap(512, 512)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$orange = [System.Drawing.Color]::FromArgb(255, 110, 15)
$carbon = [System.Drawing.Color]::FromArgb(25, 25, 30)
$neonY = [System.Drawing.Color]::FromArgb(235, 255, 35)
$g.Clear($orange)
$carbonBrush = New-Object System.Drawing.SolidBrush($carbon)
$neonBrush = New-Object System.Drawing.SolidBrush($neonY)

# Geometric goalie chevrons
for ($y = 0; $y -lt 512; $y += 64) {
    $pts = @(
        [System.Drawing.Point]::new(0, $y),
        [System.Drawing.Point]::new(256, $y + 36),
        [System.Drawing.Point]::new(512, $y),
        [System.Drawing.Point]::new(512, $y + 24),
        [System.Drawing.Point]::new(256, $y + 60),
        [System.Drawing.Point]::new(0, $y + 24)
    )
    $g.FillPolygon($carbonBrush, $pts)
}
$g.FillRectangle($neonBrush, 0, 0, 512, 35)
$g.FillRectangle($carbonBrush, 0, 480, 512, 32)
$fontGK = New-Object System.Drawing.Font("Arial", 26, [System.Drawing.FontStyle]::Bold)
$fontNumGK = New-Object System.Drawing.Font("Arial", 95, [System.Drawing.FontStyle]::Bold)
$g.DrawString("MALAGON", $fontGK, $whiteBrush, 160, 180)
$g.DrawString("1", $fontNumGK, $neonBrush, 230, 230)
$g.Dispose()
Save-Bmp $bmp "Tex_Jersey_Malagon"

# 13. Player Face (256x256)
$bmp = New-Object System.Drawing.Bitmap(256, 256)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$skin = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(235, 190, 160))
$hair = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(35, 25, 20))
$eyes = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(25, 20, 15))
$mouth = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(195, 80, 85))
$g.Clear([System.Drawing.Color]::FromArgb(235, 190, 160))
# Athletic haircut
$g.FillRectangle($hair, 0, 0, 256, 75)
$g.FillEllipse($hair, 15, 45, 60, 50)
$g.FillEllipse($hair, 180, 45, 60, 50)
# Eyebrows
$g.FillRectangle($hair, 55, 105, 50, 10)
$g.FillRectangle($hair, 150, 105, 50, 10)
# Eyes
$g.FillEllipse($whiteBrush, 60, 120, 40, 22)
$g.FillEllipse($whiteBrush, 155, 120, 40, 22)
$g.FillEllipse($eyes, 74, 122, 18, 18)
$g.FillEllipse($eyes, 169, 122, 18, 18)
# Confident smile
$g.FillEllipse($mouth, 95, 185, 66, 24)
$g.FillRectangle($whiteBrush, 105, 188, 46, 8)
$g.Dispose()
Save-Bmp $bmp "Tex_Player_Face"

# 14. Stadium Panoramic LED Banners (1024x128)
$bmp = New-Object System.Drawing.Bitmap(1024, 128)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(8, 18, 55))
$cyanBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(0, 225, 255))
$goldBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 215, 35))
$g.FillRectangle($cyanBrush, 0, 0, 1024, 8)
$g.FillRectangle($cyanBrush, 0, 120, 1024, 8)
$fontBanner = New-Object System.Drawing.Font("Arial", 22, [System.Drawing.FontStyle]::Bold)
$fontBannerSub = New-Object System.Drawing.Font("Arial", 16, [System.Drawing.FontStyle]::Bold)

# Repeating sponsor / tournament panels
$g.DrawString("ULTIMATE PENALTY CUP 3D", $fontBanner, $goldBrush, 30, 30)
$g.DrawString("A&R GAME LABS", $fontBanner, $cyanBrush, 550, 30)
$g.DrawString("CHAMPIONSHIP 2026", $fontBannerSub, $whiteBrush, 30, 75)
$g.DrawString("PLAY TO WIN - BE A LEGEND", $fontBannerSub, $whiteBrush, 550, 75)
$g.Dispose()
Save-Bmp $bmp "Tex_Stadium_Banners"

# 15. Stadium Crowd (512x256)
$bmp = New-Object System.Drawing.Bitmap(512, 256)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(35, 45, 65))
$crowdColors = @(
    [System.Drawing.Color]::FromArgb(20, 80, 210),
    [System.Drawing.Color]::FromArgb(255, 210, 30),
    [System.Drawing.Color]::FromArgb(240, 240, 245),
    [System.Drawing.Color]::FromArgb(220, 45, 45),
    [System.Drawing.Color]::FromArgb(35, 175, 75)
)
$skinColors = @(
    [System.Drawing.Color]::FromArgb(240, 195, 165),
    [System.Drawing.Color]::FromArgb(215, 165, 130),
    [System.Drawing.Color]::FromArgb(145, 100, 70)
)
for ($row = 0; $row -lt 10; $row++) {
    $y = $row * 25
    # Bleacher bar
    $standPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(60, 70, 95), 3)
    $g.DrawLine($standPen, 0, $y + 24, 512, $y + 24)
    $standPen.Dispose()

    for ($x = 0; $x -lt 512; $x += 16) {
        $fanColor = $crowdColors[$rand.Next(0, $crowdColors.Length)]
        $fanSkin = $skinColors[$rand.Next(0, $skinColors.Length)]
        $fb = New-Object System.Drawing.SolidBrush($fanColor)
        $sb = New-Object System.Drawing.SolidBrush($fanSkin)

        # Body
        $g.FillRectangle($fb, $x + 2, $y + 10, 13, 14)
        # Head
        $g.FillEllipse($sb, $x + 4, $y + 2, 9, 9)
        # Scarf or arms raised
        if ($rand.Next(0, 3) -eq 0) {
            $g.FillRectangle($goldBrush, $x, $y + 8, 17, 4)
        }
        $fb.Dispose()
        $sb.Dispose()
    }
}
$g.Dispose()
Save-Bmp $bmp "Tex_Stadium_Crowd"

# 16. Trophy Gold (256x256)
$bmp = New-Object System.Drawing.Bitmap(256, 256)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.Clear([System.Drawing.Color]::FromArgb(235, 185, 30))
$shinePen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(255, 245, 150), 3)
for ($y = 0; $y -lt 256; $y += 24) {
    $g.DrawLine($shinePen, 0, $y, 256, $y)
}
$fontTrophy = New-Object System.Drawing.Font("Arial", 16, [System.Drawing.FontStyle]::Bold)
$g.DrawString("ULTIMATE CUP", $fontTrophy, $whiteBrush, 45, 110)
$g.Dispose()
Save-Bmp $bmp "Tex_Trophy_Gold"

Write-Output "ALL 16 TEXTURES GENERATED SUCCESSFULLY!"
