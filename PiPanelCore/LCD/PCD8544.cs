using NLog;
using System;
using System.Threading.Tasks;
using WiringPi;

namespace PiPanelCore.LCD.PCD8544
{
    class PCD8544_LCD : ILCD
    {
        private static Logger Logger = LogManager.GetLogger("PCD8544_LCD");

        private PCD8544_Config config;

        public PCD8544_LCD(PCD8544_Config cfg)
        {
            config = cfg;
            SPI.wiringPiSPISetup(0, 4000000);
            GPIO.pinMode(config.RST, 1);
            GPIO.pinMode(config.CS, 1);
            GPIO.pinMode(config.DC, 1);
        }

        public static readonly RGB Black = new RGB();
        public static readonly RGB White = new RGB(1, 0, 0);

        public int Width => 84;

        public int Height => 48;

        public int[][] fb;

        public void Pixel(int x, int y, RGB color)
        {
            int mask = 1 << y % 8;
            int v = color == Black ? 1 : 0;
            //Logger.Info($"{x}, {y}");
            fb[x][y / 8] = (fb[x][y/8] & ~mask) | ((v << (y % 8)) & mask);

            //PushFramebuffer();
        }

        public void Rect(int x1, int y1, int x2, int y2, RGB color)
        {
           for (int i = x1; i <= x2; i++)
            {
                for (int j = y1; j <= y2; j++)
                {
                    Pixel(i, j, color);
                }
            }
        }

        public async Task Reset()
        {
            GPIO.digitalWrite(config.RST, 0);
            await Task.Delay(100);
            GPIO.digitalWrite(config.RST, 1);

        }

        public async Task Setup()
        {
            fb = new int[Width][];

            for (int a = 0; a < fb.Length; a++)
            {
                fb[a] = new int[Height / 8];
            }

            GPIO.digitalWrite(config.RST, 1);
            GPIO.digitalWrite(config.CS, 1);
            await Reset();
            WriteByte(false, 0x21); // PD Off, H1
            WriteByte(false, 0x13); // Bias 1:48
            WriteByte(false, config.Contrast); //Contrast 
            WriteByte(false, 0x20); // H0
            WriteByte(false, 0x09);

            PushFramebuffer();

            WriteByte(false, 0x08);
            WriteByte(false, 0X0c);

            await Task.Delay(100);

            WriteByte(false, 0x80);
            WriteByte(false, 0x40);
        }



        public void PushFramebuffer()
        {
            ResetCursor();
            for (int y = 0; y < Height / 8; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Logger.Trace($"PushFramebuffer: {x} {y} {fb[x][y]}");
                    WriteByte(true, fb[x][y]);
                }
            }
            ResetCursor();

        }

        private void ResetCursor()
        {
            WriteByte(false, 0x80);
            WriteByte(false, 0x40);
        }


        private unsafe void WriteByte(bool is_data, int b)
        {
            GPIO.digitalWrite(config.DC, is_data ? 1 : 0);

            byte a = (byte)b;
            Logger.Trace($"WriteByte {(is_data ? "DATA" : "COMMAND")}: {a:X}");
            GPIO.digitalWrite(config.CS, 0);
            SPI.wiringPiSPIDataRW(0, &a, 1);
            GPIO.digitalWrite(config.CS, 1);
        }
    }
    public class PCD8544_Config
    {
        public int RST, DC, CS;

        public int Contrast;
    }
}
