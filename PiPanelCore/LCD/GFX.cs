using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PiPanelCore.LCD
{
    [Serializable]
    public class LCDFont
    {
        private static Logger Logger = LogManager.GetLogger("LCDFont");
        public Dictionary<char, int[]> glyphs = new Dictionary<char, int[]>();
        public int dataPerY = -1;

        public void Save(string filename)
        {
            Logger.Info($"Saving LCDFont to file: {filename}");
            using FileStream writeStream = new FileStream(filename, FileMode.Create);

            BinaryWriter bw = new BinaryWriter(writeStream);

            bw.Write(dataPerY);
            bw.Write(glyphs.Count);
            foreach (var pair in glyphs)
            {
                bw.Write(pair.Key);
                bw.Write(pair.Value.Length);
                foreach (int v in pair.Value)
                    bw.Write(v);
            }

            bw.Close();
        }

        public static LCDFont ReadFile(string filename)
        {
            Logger.Info($"Reading LCDFont from file: {filename}");
            LCDFont t = new LCDFont();
            using (FileStream writeStream = new FileStream(filename, FileMode.Open))
            {
                BinaryReader br = new BinaryReader(writeStream);

                t.dataPerY = br.ReadInt32();
                int glyphscount = br.ReadInt32();

                t.glyphs = new Dictionary<char, int[]>();
                for (int i = 0; i < glyphscount; i++)
                {
                    char key = br.ReadChar();
                    int valueLength = br.ReadInt32();
                    int[] value = new int[valueLength];
                    for (int j = 0; j < valueLength; j++)
                        value[j] = br.ReadInt32();

                    t.glyphs[key] = value;
                }
                br.Close();
            }
            return t;
        }

        public delegate void GlyphDrawer(int x, int y, bool pixelValue);

        public void DrawGlyph(int[] glyph, GlyphDrawer drawer, int scale = 1)
        {
            for (int fontX = 0; fontX < glyph.Length / dataPerY; fontX++)
            {
                for (int fontDataY = 0; fontDataY < dataPerY; fontDataY++)
                {
                    int data = glyph[fontX * dataPerY + fontDataY];
                    for (int fontY = 0; fontY < 8; fontY++)
                    {
                        //Logger.Info($"--x: {fontX}, y: {fontDataY*8 + fontY}");
                        for (int scalex = 0; scalex < scale; scalex++)
                        {
                            for (int scaley = 0; scaley < scale; scaley++)
                            {
                                drawer(fontX * scale + scalex, (fontY + fontDataY * 8) * scale + scaley, (data & (1 << fontY)) > 0);
                            }
                        }
                    }
                }
            }
        }
    }

    public class GFX
    {
        private Logger Logger = LogManager.GetLogger("LCDGfx");
        public LCDRotation Rotation = LCDRotation.VERTICAL;
        public LCDFont Font;
        private readonly ILCD lcd;

        public int FontScale = 1;

        public GFX(ILCD lcd)
        {
            this.lcd = lcd;
        }

        public void DrawString(string str, int x, int y, RGB color, int space = 1)
        {
            int charWidth = Font.glyphs[' '].Length / Font.dataPerY;
            int ctr = 0;
            foreach (char c in str)
            {
                Logger.Debug($"DrawString: {charWidth} {ctr} {c} {x + (charWidth + FontScale * space) * ctr} {y}");
                DrawChar(c, x + (charWidth + FontScale * space) * ctr, y, color);
                ctr++;
            }
        }

        public void DrawPixel(int x, int y, RGB color)
        {
            int targetX = x, targetY = 0;
            switch (Rotation)
            {
                case LCDRotation.VERTICAL:
                    targetX = x;
                    targetY = y;
                    break;
                case LCDRotation.VERTICAL_REV:
                    targetX = lcd.Width - 1- x;
                    targetY = lcd.Height - 1 - y;
                    break;
                case LCDRotation.HORIZONTAL:
                    targetX = y;
                    targetY = x;
                    break;
                case LCDRotation.HORIZONTAL_REV:
                    targetX = lcd.Width - 1 - y;
                    targetY = lcd.Height - 1- x;
                    break;
            }
            if (targetX > lcd.Width - 1) return;
            if (targetY > lcd.Height - 1)  return;
            if (targetX < 0) return;
            if (targetY < 0) return;
            Logger.Debug($"{Rotation}: Pixel {color} at: {targetX}, {targetY}");
            lcd.Pixel(targetX, targetY, color);
        }

        public void DrawChar(char c, int x, int y, RGB color)
        {
            Font.DrawGlyph(Font.glyphs[c], (int pixX, int pixY, bool pixelValue) =>
            {
                if (pixelValue)
                    DrawPixel(x + pixX, y + pixY, color);
            }, FontScale);
        }

        internal void FillRect(int x1, int y1, int x2, int y2, RGB color)
        {
            Logger.Debug($"FillRect: {x1} {y1} -> {x2} {y2} - {color}");
            for (int sx = x1; sx < x2; sx++)
            {
                for (int sy = y1; sy < y2; sy++)
                {
                    DrawPixel(sx, sy, color);
                }
            }
        }

        internal void FillScreen(RGB color)
        {
            lcd.Rect(0, 0, lcd.Width, lcd.Height, color);
        }

        internal void FillDrawString(string str, int x, int y, RGB fg, RGB bg)
        {
            FillRect(x,y, x + (str.Length) * ((Font.glyphs.ElementAt(0).Value.Length / Font.dataPerY) + 1) * FontScale, y + Font.dataPerY * 8 * FontScale, fg);
            DrawString(str, x,y, bg);
        }
    }
}
