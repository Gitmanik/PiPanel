using NLog;
using NLog.Config;
using NLog.Targets;
using PiPanelCore.LCD;
using System;

using System.IO;
using System.Linq;
using System.Text;

namespace PiPanelFontCreator
{
    class FontCreator
    {
        private enum WorkMode
        {
            SCROLL_LIST,
            EDIT_GLYPH,
            SETTINGS
        }

        private static WorkMode WM;

        private static Logger Logger = LogManager.GetLogger("FontCreator");

        private static string filename;
        private static LCDFont font = new LCDFont();

        private static readonly char[] starting = new char[]
        {
            ' ', '!', '"', '#', '$', '%', '&', '\'', '(', ')', '*', '+', ',', '-', '.', '/', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ':', ';', '<', '=', '>', '?', '@', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', '[', '\\', ']', '^', '_', '`', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '{', '|', '}', '~'
        };

        private static int FontGlyphListIdx = 0, EditGlyphX = 0, EditGlyphY = 0, LastConsoleHeight = 0, LastConsoleWidth = 0;

        private static void Main(string[] args)
        {
            LoggingConfiguration logConfig = new LoggingConfiguration();

            FileTarget logfile = new FileTarget("logfile")
            {
                FileName = "app.log",
                Layout = @"${date:format=HH\:mm\:ss} ${logger:long=True} ${level}: ${message} ${exception}",
                Encoding = Encoding.UTF8
            };
            logConfig.AddRule(LogLevel.Debug, LogLevel.Fatal, logfile);

            LogManager.Configuration = logConfig;

            Console.WriteLine("PiPanel FontCreator by Gitmanik 2020");

            filename = ConsoleInput("Enter valid GtmFont filename"); if (!filename.EndsWith(".gtmfont")) filename += ".gtmfont";

            if (File.Exists(filename))
            {
                Console.WriteLine("File exists, loading..");
                font = LCDFont.ReadFile(filename);
            }
            else
            {
                Console.WriteLine("Not found! Creating new GtmFont");
                font.dataPerY = int.Parse(ConsoleInput("Enter HEIGHT multiplied by 8"));
                int x = int.Parse(ConsoleInput("Enter WIDTH"));

                foreach (char c in starting)
                {
                    font.glyphs[c] = new int[x * font.dataPerY];
                }
            }
            Console.Clear();

            while (true)
            {
                if (Console.WindowHeight != LastConsoleHeight || LastConsoleWidth != Console.WindowWidth)
                { 
                    LastConsoleHeight = Console.WindowHeight;
                    LastConsoleWidth = Console.WindowWidth;
                    Console.Clear();
                }

                RenderBar();
                RenderGlyph();
                if (WM == WorkMode.SCROLL_LIST)
                {
                    RenderList();
                    Console.SetCursorPosition(Console.WindowWidth - 1, Console.WindowHeight - 1);
                }


                ConsoleKeyInfo input = Console.ReadKey();
                switch (input.Key)
                {
                    case ConsoleKey.Q:
                        Environment.Exit(0);
                        break;

                    case ConsoleKey.S:
                        font.Save(filename);
                        RenderInfo("Saved file.");
                        break;

                    case ConsoleKey.Tab:
                        WM = (WM == WorkMode.EDIT_GLYPH) ? WorkMode.SCROLL_LIST : WorkMode.EDIT_GLYPH;
                        break;

                    case ConsoleKey.C:
                        Console.Clear();
                        break;

                    case ConsoleKey.UpArrow:
                        switch (WM)
                        {
                            case WorkMode.SCROLL_LIST:
                                if (FontGlyphListIdx > 0)
                                    FontGlyphListIdx--;
                                break;
                            case WorkMode.EDIT_GLYPH:
                                if (EditGlyphY > 0)
                                    EditGlyphY--;
                                break;
                            case WorkMode.SETTINGS:
                                break;
                        }

                        break;

                    case ConsoleKey.DownArrow:
                        switch (WM)
                        {
                            case WorkMode.SCROLL_LIST:
                                if (FontGlyphListIdx < font.glyphs.Count - 1)
                                    FontGlyphListIdx++;
                                break;
                            case WorkMode.EDIT_GLYPH:
                                if (EditGlyphY < font.dataPerY * 8 - 1)
                                    EditGlyphY++;
                                break;
                            case WorkMode.SETTINGS:
                                break;
                        }
                        break;

                    case ConsoleKey.RightArrow:
                        switch (WM)
                        {
                            case WorkMode.SCROLL_LIST:
                                break;
                            case WorkMode.EDIT_GLYPH:
                                if (EditGlyphX < font.glyphs.ElementAt(0).Value.Length / font.dataPerY - 1)
                                    EditGlyphX++;
                                break;
                        }
                        break;

                    case ConsoleKey.LeftArrow:
                        switch (WM)
                        {
                            case WorkMode.SCROLL_LIST:
                                break;
                            case WorkMode.EDIT_GLYPH:
                                if (EditGlyphX > 0)
                                    EditGlyphX--;
                                break;
                        }
                        break;

                    case ConsoleKey.Spacebar:
                        if (WM == WorkMode.EDIT_GLYPH)
                            font.glyphs.ElementAt(FontGlyphListIdx).Value[EditGlyphX * font.dataPerY + (int)Math.Ceiling((EditGlyphY + 1) / 8.0) - 1] ^= 1 << EditGlyphY % 8;
                        break;

                    case ConsoleKey.A:
                        if (WM == WorkMode.SCROLL_LIST)
                            Console.Clear();
                        char c = ConsoleInput("Enter Char to Add (existing to go back)")[0];
                        if (font.glyphs.ContainsKey(c))
                            break;
                        else
                            font.glyphs.Add(c, new int[font.glyphs.ElementAt(0).Value.Length]);
                        break;
                }
            }
        }

        private static string ConsoleInput(string s)
        {
            Console.Write(s + ": ");
            return Console.ReadLine();
        }

        private static void RenderBar()
        {
            Console.SetCursorPosition(0, 0);
            string s = $"PiPanel FontCreator -- Filename: {filename}, Current mode: {WM}";
            if (WM == WorkMode.EDIT_GLYPH)
                s += $", X: {EditGlyphX}, Y: {EditGlyphY}, V: {EditGlyphX * font.dataPerY + (int)Math.Ceiling((EditGlyphY + 1) / 8.0) - 1}";
            else if (WM == WorkMode.SCROLL_LIST)
                s += $", idx: {FontGlyphListIdx}, char: {font.glyphs.ElementAt(FontGlyphListIdx).Key}";

            for (int xxxxx = 0; xxxxx < Console.WindowWidth - s.Length; xxxxx++)
                s += ' ';

            Console.WriteLine(s);
        }

        private static void RenderGlyph()
        {
            if (font.glyphs.Count == 0)
                return;

            font.DrawGlyph(font.glyphs.ElementAt(FontGlyphListIdx).Value, (int x, int y, bool pixelValue) =>
            {
                Console.SetCursorPosition(15 + x, 2 + y);
                Console.Write(pixelValue ? '░' : '█');
            });

            if (WM == WorkMode.EDIT_GLYPH)
                Console.SetCursorPosition(15 + EditGlyphX, 2 + EditGlyphY);
        }

        private static void RenderList()
        {
            Console.SetCursorPosition(0, 1); Console.Write("┌───┐");

            int height = Console.WindowHeight - 3;

            for (int idx = 0; idx < height; idx++)
            {
                int curr = idx + (FontGlyphListIdx > height - 1 ? FontGlyphListIdx - height + 1 : 0);

                if (curr > font.glyphs.Count - 1)
                    continue;
                Console.SetCursorPosition(0, 2 + idx); 
                Console.Write($"{((curr == FontGlyphListIdx) ? '├' : '│')} {font.glyphs.ElementAt(curr).Key} {((curr == FontGlyphListIdx) ? '┤' : '│')}");
            }
            Console.SetCursorPosition(0, Console.WindowHeight - 1); Console.Write("└───┘");
        }

        private static void RenderInfo(string s, bool x = true)
        {
            Logger.Info(s);
            Console.SetCursorPosition(6, Console.WindowHeight - 1);
            Console.Write(s + (x ? "\n" : ""));
        }
    }
}