using NLog;
using System.Threading.Tasks;
using WiringPi;

namespace PiPanelCore.LCD.ILI9327
{

    public class ILI9327_LCD : ILCD
    {
        private static Logger Logger = LogManager.GetLogger("ILI9327");
        private class LcdCommand
        {
            public int command;
            public int[] data;

            public LcdCommand(int command, int[] data)
            {
                this.command = command;
                this.data = data;
            }
        }
        private readonly int LCD_DELAY = 0x7F;
        private readonly ILI9327_Config config;

        public int Width => 240;

        public int Height => 400;

        public int RGB565(RGB r)
        {
            int RR, GG, BB;
            RR = (r.R * 31 / 255) << 11;
            GG = (r.G * 63 / 255) << 5;
            BB = r.B * 31 / 255;
            return RR | GG | BB;
        }

        public ILI9327_LCD(ILI9327_Config config)
        {
            this.config = config;

            GPIO.pinMode(this.config.RST, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.CS, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.RS, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.WR, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.RD, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.D0, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.D1, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.D2, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.D3, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.D4, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.D5, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.D6, (int)GPIO.GPIOpinmode.Output);
            GPIO.pinMode(this.config.D7, (int)GPIO.GPIOpinmode.Output);

            GPIO.digitalWrite(this.config.RST, 1);
            GPIO.digitalWrite(this.config.CS, 1);
            GPIO.digitalWrite(this.config.RS, 1);
            GPIO.digitalWrite(this.config.WR, 1);
            GPIO.digitalWrite(this.config.RD, 1);
        }

        public void Rect(int x1, int y1, int x2, int y2, RGB color)
        {
            if (x1 >= Width) return;
            if (x2 >= Width) x2 = Width - 1;
            if (y1 >= Height) return;
            if (y2 >= Height) y2 = Height - 1;

            WriteByte(false, 0x2A);
            WriteWord(true, x1);
            WriteWord(false, x2);
            WriteByte(false, 0x2B);
            WriteWord(true, y1);
            WriteWord(true, y2);
            WriteByte(false, 0x2C);

            int c = RGB565(color);
            for (int i = x1; i <= x2; i++)
            {
                for (int j = y1; j <= y2; j++)
                {
                    WriteWord(true, c);
                }
            }
        }

        public void Pixel(int x, int y, RGB color)
        {
            Rect(x, y, x, y, color);
        }

        public async Task Setup()
        {
            LcdCommand[] ResetOff =
            {
                new LcdCommand(0x01, null),
                new LcdCommand(LCD_DELAY, new int[] {150}),
                new LcdCommand(0x28, null),
                new LcdCommand(0x3A, new int[] {0x55})
            };
            LcdCommand[] WakeOn =
            {
                new LcdCommand(0x11, null),
                new LcdCommand(LCD_DELAY, new int[] {150}),
                new LcdCommand(0x29, null)
            };

            LcdCommand[] RegValues =
            {
                new LcdCommand(0x36, new int[] {0x08}),
                new LcdCommand(0x3A, new int[] {0x05}),
                new LcdCommand(0xC0, new int[] {0x00, 0x35, 0x00, 0x00, 0x01, 0x02}),
                new LcdCommand(0xC1, new int[] { 0x10, 0x10, 0x02, 0x02 }),
                new LcdCommand(0xC5, new int[] { 0x04 }),
                new LcdCommand(0xC8, new int[] { 0x04, 0x67, 0x35, 0x04, 0x08, 0x06, 0x24, 0x01, 0x37, 0x40, 0x03, 0x10, 0x08, 0x80, 0x00 }),
                new LcdCommand(0xD0, new int[] { 0x07, 0x04, 0x00 }),
                new LcdCommand(0xD1, new int[] { 0x00, 0x0C, 0x0F }),
                new LcdCommand(0xD2, new int[] { 0x01, 0x44 }),
            };
            await Reset();
            await InitTable(ResetOff);
            await InitTable(RegValues);
            await InitTable(WakeOn);
        }

        public async Task Reset()
        {
            GPIO.digitalWrite(config.RST, 0);
            await Task.Delay(100);
            GPIO.digitalWrite(config.RST, 1);
            await Task.Delay(100);
        }

        private async Task InitTable(LcdCommand[] table)
        {
            foreach (LcdCommand command in table)
            {
                Logger.Trace($"InitTable: {command.command:X}, {string.Join(",", command.data != null ? command.data : new int[0])}");

                if (command.command == LCD_DELAY)
                {
                    await Task.Delay(command.data[0]);
                    continue;
                }

                WriteByte(false, command.command);
                if (command.data != null)
                    foreach (int data in command.data)
                        WriteByte(true, data);
            }
        }

        private void WriteWord(bool is_data, long data)
        {
            GPIO.digitalWrite(config.CS, 0);
            GPIO.digitalWrite(config.RS, is_data ? 1 : 0);
            GPIO.digitalWrite(config.RD, 1);
            GPIO.digitalWrite(config.WR, 1);
            _WriteByte(data >> 8);
            _WriteByte(data);
            GPIO.digitalWrite(config.CS, 1);
        }

        private void _WriteByte(long data)
        {
            Logger.Trace($"_WriteByte {data:X}: {data & 1} {(data & 2) >> 1} {(data & 4) >> 2} {(data & 8) >> 3} {(data & 16) >> 4} {(data & 32) >> 5} {(data & 64) >> 6} {(data & 128) >> 7}");

            GPIO.digitalWrite(config.D0, (int)data & 1);
            GPIO.digitalWrite(config.D1, (int)(data & 2) >> 1);
            GPIO.digitalWrite(config.D2, (int)(data & 4) >> 2);
            GPIO.digitalWrite(config.D3, (int)(data & 8) >> 3);
            GPIO.digitalWrite(config.D4, (int)(data & 16) >> 4);
            GPIO.digitalWrite(config.D5, (int)(data & 32) >> 5);
            GPIO.digitalWrite(config.D6, (int)(data & 64) >> 6);
            GPIO.digitalWrite(config.D7, (int)(data & 128) >> 7);

            GPIO.digitalWrite(config.WR, 0);
            for (int a = 0; a < 250; a++)
            { }

            GPIO.digitalWrite(config.WR, 1);
        }

        private void WriteByte(bool is_data, long data)
        {
            Logger.Trace($"WriteByte {(is_data ? "DATA" : "COMMAND") }: {data:X}");
            GPIO.digitalWrite(config.CS, 0);
            GPIO.digitalWrite(config.RS, is_data ? 1 : 0);
            GPIO.digitalWrite(config.RD, 1);
            GPIO.digitalWrite(config.WR, 1);

            _WriteByte(data);

            GPIO.digitalWrite(config.CS, 1);
        }
    }

    public class ILI9327_Config
    {
        public int RST, CS, RS, WR, RD, D0, D1, D2, D3, D4, D5, D6, D7;
    }
}