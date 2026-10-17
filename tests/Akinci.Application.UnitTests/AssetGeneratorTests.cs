using System.IO;
using Xunit;

namespace Akinci.Application.UnitTests;

public class AssetGeneratorTests
{
    [Fact]
    public void GenerateApplicationIcon()
    {
        var targetDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "Akinci.UI", "Assets"));
        Directory.CreateDirectory(targetDir);
        var icoPath = Path.Combine(targetDir, "akinci.ico");

        const int width = 32;
        const int height = 32;
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // 1. ICO Header
        writer.Write((short)0); // Reserved
        writer.Write((short)1); // Type 1 = Icon
        writer.Write((short)1); // 1 Image

        // 2. Directory Entry (16 bytes)
        writer.Write((byte)width);
        writer.Write((byte)height);
        writer.Write((byte)0);  // Colors
        writer.Write((byte)0);  // Reserved
        writer.Write((short)1); // Color planes
        writer.Write((short)32);// Bits per pixel
        int imageSize = 40 + (width * height * 4) + (height * 4); // Header + XOR + AND
        writer.Write(imageSize);
        writer.Write(22); // Offset (6 + 16 = 22)

        // 3. BITMAPINFOHEADER (40 bytes)
        writer.Write(40); // Size
        writer.Write(width);
        writer.Write(height * 2); // In ICO, height is doubled (XOR + AND)
        writer.Write((short)1);   // Planes
        writer.Write((short)32);  // Bit count
        writer.Write(0);          // Compression (BI_RGB)
        writer.Write(width * height * 4); // Image size
        writer.Write(0);          // X pixels per meter
        writer.Write(0);          // Y pixels per meter
        writer.Write(0);          // Colors used
        writer.Write(0);          // Important colors

        // 4. XOR Bitmap (Bottom-up: y from 0 to 31)
        for (int y = 0; y < height; y++)
        {
            // Bottom-up: screenY = 31 - y
            int screenY = 31 - y;
            for (int x = 0; x < width; x++)
            {
                // Distance from center (15.5, 15.5)
                double dx = x - 15.5;
                double dy = screenY - 15.5;
                double dist = Math.Sqrt(dx * dx + dy * dy);

                byte r = 0, g = 0, b = 0, a = 0;

                if (dist <= 14.5)
                {
                    // Inside circular badge
                    if (dist >= 12.5)
                    {
                        // Border: Sky Blue (#38BDF8)
                        r = 56; g = 189; b = 248; a = 255;
                    }
                    else
                    {
                        // Background: Dark Slate (#0F172A to #1E293B)
                        r = (byte)(20 + screenY / 2);
                        g = (byte)(30 + screenY / 2);
                        b = (byte)(55 + screenY / 2);
                        a = 255;

                        // Check if within Down Arrow
                        // Shaft: x in [13, 18], screenY in [8, 17]
                        bool inShaft = (x >= 13 && x <= 18 && screenY >= 8 && screenY <= 17);
                        // Arrowhead: screenY in [18, 24], width tapering to point at x=15.5, screenY=24
                        int arrowRow = screenY - 18;
                        bool inHead = false;
                        if (arrowRow >= 0 && arrowRow <= 6)
                        {
                            int halfSpan = 7 - arrowRow;
                            inHead = (x >= 15 - halfSpan && x <= 16 + halfSpan);
                        }

                        if (inShaft || inHead)
                        {
                            // Arrow: Bright Cyan/White (#38BDF8)
                            r = 56; g = 189; b = 248; a = 255;
                        }
                    }
                }

                // BGRA format
                writer.Write(b);
                writer.Write(g);
                writer.Write(r);
                writer.Write(a);
            }
        }

        // 5. AND mask (1 bit per pixel, 4 bytes per row for 32 pixels, bottom-up)
        for (int y = 0; y < height; y++)
        {
            int screenY = 31 - y;
            uint rowMask = 0;
            for (int x = 0; x < width; x++)
            {
                double dx = x - 15.5;
                double dy = screenY - 15.5;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                if (dist > 14.5)
                {
                    rowMask |= (1u << (31 - x));
                }
            }
            writer.Write(rowMask);
        }

        File.WriteAllBytes(icoPath, ms.ToArray());
        Assert.True(File.Exists(icoPath));
        Assert.True(new FileInfo(icoPath).Length > 1000);
    }
}
