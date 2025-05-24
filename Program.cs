using System.Data;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Eaucool.Utilities;

namespace Eaucool
{
    // Eaucool Programming Language - Licensed under GNU GPL v3.0
    internal class Program
    {
        #if Windows
            public static Form form;
        #endif
        public static Dictionary<string, string> variables = new Dictionary<string, string>();
        public static Dictionary<string, string[]> methodCode = new Dictionary<string, string[]>();
        public static int randMax = 10;
        public static Random random = new Random();
        public static bool skipIfStmtElse = false;
        private static bool skipElseStmtB;
        public static bool skipElseStmt = false;
        public static bool doingPercent = true;
        public static string currentFile = "";
        public static string currentFileCode = "";
        public static int j;
        public static string version = "1.2.4";
        public static void Error(string msg)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Error: " + msg);
            Console.ResetColor();
            Environment.Exit(1);
        }
        public static string pageData
        {
            get
            {
                return "";
            }
            set
            {
                Console.WriteLine(value);
            }
        }

        public static string SafeEscape(string str)
        {
            return Regex.Replace(str, @"[\x00'""\b\n\r\t\cZ\\%_]",
                delegate (Match match)
                {
                    return match.Value switch
                    {
                        // ASCII NUL (0x00) character
                        "\x00" => "\\0",

                        // BACKSPACE character
                        "\b" => "\\b",

                        // NEWLINE (linefeed) character
                        "\n" => "\\n",

                        // CARRIAGE RETURN character
                        "\r" => "\\r",

                        // TAB
                        "\t" => "\\t",

                        // Ctrl-Z
                        "\u001A" => "\\Z",

                        _ => "\\" + match.Value,
                    };
                });
        }

        // Parse COOL code
        public static void ParseCOOL(string code, bool included)
        {
            if (!included)
            {
                variables.Add("_TIME", DateTime.UtcNow.ToString("hh:mm:ss"));
                variables.Add("_DATE", DateTime.UtcNow.ToString("yyyy-MM-dd"));
            }
            bool firstPercent;
            int ifs = 1;
            skipIfStmtElse = false;
            currentFileCode = code;
            for (j = 0; j < code.Split(new char[] { '\n' }).Length; j++)
            {
                string line = code.Split(new char[] { '\n' })[j].Replace("\r", "").Replace("\t", " ").Trim();
                line = Regex.Replace(line, @"\s+", " ");
                if (skipIfStmtElse)
                {
                    if (doingPercent)
                    {
                        if (line.StartsWith("if "))
                        {
                            ifs += 1;
                        }
                        if (line == "else")
                        {
                            skipIfStmtElse = false;
                            skipElseStmt = true;
                        }
                        else if (line == "stopif")
                        {
                            ifs--;
                            if (ifs <= 0)
                            {
                                skipIfStmtElse = false; // endif
                            }
                        }
                    }
                    continue;
                }
                if (skipElseStmt)
                {
                    if (doingPercent)
                    {
                        if (line == "else")
                        {
                            skipElseStmtB = true;
                        }
                        if (line == "stopif")
                        {
                            skipElseStmtB = false;
                            skipElseStmt = false;
                        }
                    }
                    if (skipElseStmtB)
                    {
                        continue;
                    }
                }
                if (string.IsNullOrEmpty(line))
                {
                    continue;
                }
                firstPercent = false;
                if (line.StartsWith("//"))
                {
                }
                else if (line == "stopif")
                {
                }
                else if (line == "stoploop")
                {
                }
                else if (line == "else")
                {
                }
                else if (line == "stop")
                {
                    return;
                }
                // Include is not part of the parser
                else if (line.StartsWith("include "))  // Include
                {
                    int oldj = j;
                    if (System.IO.File.Exists(line[8..]))
                    {
                        currentFile = line[8..];
                        ParseCOOL(System.IO.File.ReadAllText(line[8..]), true);
                    }
                    else if (File.Exists(Path.Combine("eaumods", line[8..] + ".eau")))
                    {
                        currentFile = line[8..] + ".eau";
                        ParseCOOL(System.IO.File.ReadAllText(Path.Combine("eaumods", line[8..] + ".eau")), true);
                    }
                    else
                    {
                        Error("Cannot open " + line[8..]);
                    }
                    j = oldj;
                }
                else
                {
                    int t = Parser.Parse(line);
                    if (t == 2)
                    {
                        Error("Unknown statement " + line);
                        return;
                    }
                }
            }
        }
        public static void FormattedPrint(string str)
        {
            Error("$= is obsolete. Use echo instead.");
        }
        static void Main(string[] args)
        {
            // Check for arguments
            if (args.Length == 0)
            {
                // No arguments
                Console.WriteLine("Eaucool Programming Language - (c) 2023-2025 Novixx Systems and Contributors");
                Console.WriteLine("Usage: eaucool <filename>");
                Console.WriteLine("Example: eaucool hello.eau");
                return;
            }
            if (args[0] == "--about")  // About screen, which shows the previous name of the language :)
            {
                Console.WriteLine("Eaucool Programming Language (previously percentCool) - (c) 2023-2025 Novixx Systems and Contributors");
                Console.WriteLine("Licensed under GNU GPL v3.0");
                Console.WriteLine();
                Console.WriteLine("Version " + version);
                Console.WriteLine();
                Console.WriteLine("Eaucool is a programming language that is designed to be easy to use.");
            }
            Parser.Init();
            currentFile = args[0];
            ParseCOOL(System.IO.File.ReadAllText(args[0]), false);
            if (string.Join(" ", args).Contains("--pause"))
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
            if (form == null) return;
            while (form.Visible)
            {
                Application.DoEvents();
            }
        }

        internal static bool IsVariable(string v)
        {
            if (variables.ContainsKey(v)) return true;
            return false;
        }
    }
}