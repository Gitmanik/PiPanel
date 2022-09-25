using Newtonsoft.Json;
using NLog;
using NLog.Config;
using NLog.Targets;
using PiPanelCore.LCD;
using PiPanelCore.LCD.PCD8544;
using RunProcessAsTask;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace PiPanelCore
{
    internal class PiPanel
    {
        private static Logger Logger = LogManager.GetLogger("PiPanelCore");
        private static async Task Main(string[] args)
        {
            LoggingConfiguration logConfig = new LoggingConfiguration();

            ConsoleTarget logconsole = new ConsoleTarget("logconsole")
            {
                Layout = @"${date:format=HH\:mm\:ss} ${logger:long=True} ${level}: ${message} ${exception}",
            };

            if (args.Length > 0 && args[0] == "file")
            {
                FileTarget logfile = new FileTarget("logfile")
                {
                    FileName = "app.log",
                    Layout = @"${date:format=HH\:mm\:ss} ${logger:long=True} ${level}: ${message} ${exception}"
                };
                logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);
            }

            logConfig.AddRule(LogLevel.Info, LogLevel.Fatal, logconsole);

            LogManager.Configuration = logConfig;
            WiringPi.Init.WiringPiSetup();

            Logger.Info("PiPanel 2020.");

            if (args.Length > 0 && args[0] == "dbg")
            {
                Logger.Info("Waiting for Debugger");
                for (; ; )
                {
                    if (Debugger.IsAttached)
                    {
                    break;
                    }
                    await Task.Delay(200);
                }
            }


            //ILI9327_Config cfg = new ILI9327_Config
            //{
            //        RST = 8,
            //        CS = 9,
            //        RS = 7,
            //        WR = 25,
            //        RD = 24,
            //        D0 = 29,
            //        D1 = 28,
            //        D2 = 27,
            //        D3 = 26,
            //        D4 = 31,
            //        D5 = 11,
            //        D6 = 6,
            //        D7 = 5
            //};

            //ILI9327_LCD lcd = new ILI9327_LCD(cfg);

            PCD8544_Config cfg = new PCD8544_Config()
            {
                RST = 29,
                CS = 28,
                DC = 13,
                Contrast = 0xBf
            };
            PCD8544_LCD lcd = new PCD8544_LCD(cfg);
            await lcd.Setup();

            LCDFont f8x5 = LCDFont.ReadFile("f8x5.gtmfont");

            GFX lcdGfx = new GFX(lcd)
            {
                Rotation = LCDRotation.VERTICAL_REV,
                Font = f8x5
            };
            string covid = "COVID", covidpolska = "COVIDPL";
            long covidctr = 0;
            while (true)
            {
                string temp = $"Pi: {string.Concat(ProcessEx.RunAsync("vcgencmd", "measure_temp").Result.StandardOutput[0].Substring(5).Reverse().Skip(4).Reverse())}°C";
                string eth = string.Concat(ProcessEx.RunAsync("hostname", "-I").Result.StandardOutput[0]);
                string eth0 = eth.Substring(0, eth.IndexOf(" "));

                string loadav = string.Concat(ProcessEx.RunAsync("cat", "/proc/loadavg").Result.StandardOutput[0]);
                string loadavg = loadav.Substring(0, 15);
                

                if (covidctr % 120 == 0)
                {
                    Logger.Info("Resetting LCD..");
                    await lcd.Reset();
                    await Task.Delay(1000);
                    await lcd.Setup();

                    Logger.Info($"Refreshing Covid Data: {covidctr}");
                    using (WebClient client = new WebClient())
                    {
                        string s = await client.DownloadStringTaskAsync("https://coronavirus-19-api.herokuapp.com/countries/poland");
                        //Logger.Info(s);
                        CovidData pl = JsonConvert.DeserializeObject<CovidData>(s);
                        covid = $"{pl.Cases}({pl.TodayCases})";

                        lcdGfx.FillRect(0, 32, lcd.Width, lcd.Height, PCD8544_LCD.White);
                        lcdGfx.FillDrawString(covid, 0, 32, PCD8544_LCD.White, PCD8544_LCD.Black);
                    }
                }
                covidctr++;

                Logger.Debug(eth0);
                Logger.Debug(loadavg);
                Logger.Debug(covid);
                Logger.Debug(covidpolska);
                int ctr = 0;

                lcdGfx.FillDrawString(temp, 0, ctr * 8, PCD8544_LCD.White, PCD8544_LCD.Black);
                ctr++;
                lcdGfx.FillDrawString(loadavg, 0, ctr * 8, PCD8544_LCD.White, PCD8544_LCD.Black);
                ctr++;
                DriveInfo drive = DriveInfo.GetDrives().First(x => x.Name == "/");
                string drivex = $"{BytesToString(drive.TotalSize - drive.AvailableFreeSpace)}/{BytesToString(drive.AvailableFreeSpace)}";
                Logger.Debug(drivex);
                lcdGfx.FillDrawString(drivex, 0, ctr * 8, PCD8544_LCD.White, PCD8544_LCD.Black);
                ctr++;
                lcdGfx.FillDrawString(eth0, 0, ctr * 8, PCD8544_LCD.White, PCD8544_LCD.Black);

                lcd.PushFramebuffer();
                await Task.Delay(5000);
            }
        }
        static string BytesToString(long byteCount)
        {
            string[] suf = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; //Longs run out around EB
            if (byteCount == 0)
                return "0" + suf[0];
            long bytes = Math.Abs(byteCount);
            int place = Convert.ToInt32(Math.Floor(Math.Log(bytes, 1024)));
            double num = Math.Round(bytes / Math.Pow(1024, place), 1);
            return (Math.Sign(byteCount) * num).ToString() + suf[place];
        }
    }   
    class CovidData
    {
        [JsonProperty("country")]
        public string Country { get; set; }

        [JsonProperty("cases")]
        public long Cases { get; set; }

        [JsonProperty("todayCases")]
        public long TodayCases { get; set; }

        [JsonProperty("deaths")]
        public long Deaths { get; set; }

        [JsonProperty("todayDeaths")]
        public long TodayDeaths { get; set; }

        [JsonProperty("recovered")]
        public long Recovered { get; set; }

        [JsonProperty("active")]
        public long Active { get; set; }

        [JsonProperty("critical")]
        public long Critical { get; set; }

        [JsonProperty("casesPerOneMillion")]
        public long CasesPerOneMillion { get; set; }

        [JsonProperty("deathsPerOneMillion")]
        public long DeathsPerOneMillion { get; set; }

        [JsonProperty("totalTests")]
        public long TotalTests { get; set; }

        [JsonProperty("testsPerOneMillion")]
        public long TestsPerOneMillion { get; set; }
    }
}