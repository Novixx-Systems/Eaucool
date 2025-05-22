using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Eaucool.Utilities
{
    internal class Parser
    {
        private static int error = 0;
        private static string line;
        private static HttpListenerContext ctx;
        
        private readonly static Dictionary<string, Action> keywords = new();

        public static void Init()
        {
            keywords.Clear();

            // All keywords
            //
            // NOTE: Most keywords MUST end with a space, operators
            // must NOT end with a space. Keywords that have no arguments
            // should also NOT end with a space.
            
            #region Operators
            keywords.Add("$=",           Op_DollarEquals);
            keywords.Add("$",            Op_Dollar);
            #endregion

            #region Utility Keywords
            keywords.Add("echo ", Kw_Echo);
            keywords.Add("rndmax ", Kw_Rndmax);
            keywords.Add("random", Kw_Random);
            keywords.Add("existing ", Kw_Existing);
            keywords.Add("escape ", Kw_Escape);
            keywords.Add("replace ", Kw_Replace);
            keywords.Add("rereplace ", Kw_Rereplace);
            keywords.Add("regexreplace ", Kw_Rereplace); // Alias for rereplace
            keywords.Add("increment ", Kw_Increment);
            keywords.Add("decrement ", Kw_Decrement);
            keywords.Add("throw ", Kw_Throw);
            keywords.Add("compute ", Kw_Compute);
            #endregion

            #region Control Flow Keywords
            keywords.Add("if ", Kw_If);
            #endregion

            #region Date/Time Keywords
            keywords.Add("getdate ", Kw_Getdate);
            #endregion

            #region Mail Keywords
            keywords.Add("mail ", Kw_Mail);
            #endregion

            #region File System Keywords
            keywords.Add("writefile ", Kw_Writefile);
            keywords.Add("readfile ", Kw_Readfile);
            keywords.Add("rmfile ", Kw_Rmfile);
            keywords.Add("deletefile ", Kw_Rmfile); // Alias for rmfile
            keywords.Add("appendfile ", Kw_Appendfile);
            keywords.Add("execute ", Kw_Execute);
            #endregion

            #region Web Keywords
            keywords.Add("urldecode ", Kw_Urldecode);
            keywords.Add("urlencode ", Kw_Urlencode);
            keywords.Add("urlget ", Kw_Urlget);
            keywords.Add("urlpost ", Kw_Urlpost);
            keywords.Add("httpserve ", Kw_Httpserve);
            #endregion

#if Windows
            #region GUI Keywords
            keywords.Add("newgui ", Kw_Newgui);
            keywords.Add("addbutton ", Kw_Addbutton);
            keywords.Add("addlabel ", Kw_Addlabel);
            keywords.Add("addtextbox ", Kw_Addtextbox);
            #endregion
            #endif

            #region Method Keywords
            keywords.Add("method ", Kw_Method);
            keywords.Add("stopmethod", Kw_Stopmethod);
            keywords.Add("callmethod ", Kw_Callmethod);
            #endregion
        }

        private static void Kw_Throw()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string message = Utils.GetString(args, 1);

            if (message == string.Empty)
            {
                Program.Error("throw requires a non-empty message");
            }

            Program.Error(message);
        }

        private static void Kw_Decrement()
        {
            // Decrement a variable by 1
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string variable = args[1];
            
            if (variable == string.Empty)
            {
                return;
            }

            if (!variable.StartsWith("$"))
            {
                Program.Error("Variable names must start with a $");
            }
            variable = variable.Substring(1);

            if (!Program.variables.ContainsKey(variable))
            {
                Program.variables.Add(variable, "0");
            }

            int test;
            if (!int.TryParse(Program.variables[variable], out test))
            {
                Program.Error("Variable must be an integer");
            }

            Program.variables[variable] = (int.Parse(Program.variables[variable]) - 1).ToString();
        }

        private static void Kw_Compute()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string expression = Utils.GetString(args, 1);

            if (expression == string.Empty)
            {
                return;
            }

            try
            {
                Program.variables["_RESULT"] = MathParser.Parse(expression);
            }
            catch (Exception)
            {
                Program.Error("Failed to compute expression");
            }
        }

        private static void Kw_Increment()
        {
            // Increment a variable by 1
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string variable = args[1];
            
            if (variable == string.Empty)
            {
                return;
            }

            if (!variable.StartsWith("$"))
            {
                Program.Error("Variable names must start with a $");
            }
            variable = variable.Substring(1);

            if (!Program.variables.ContainsKey(variable))
            {
                Program.variables.Add(variable, "0");
            }

            int test;
            if (!int.TryParse(Program.variables[variable], out test))
            {
                Program.Error("Variable must be an integer");
            }

            Program.variables[variable] = (int.Parse(Program.variables[variable]) + 1).ToString();
        }

        private static void Kw_Rereplace()
        {
            // Replace a string with a regex
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string variable = args[1];
            string input = Utils.GetString(args, 2);
            string pattern = Utils.GetString(args, 3);
            string replacement = Utils.GetString(args, 4);


            if (!variable.StartsWith("$"))
            {
                Program.Error("Variable names must start with a $");
            }

            variable = variable.Substring(1);

            if (!Program.variables.ContainsKey(variable))
            {
                Program.variables.Add(variable, string.Empty);
            }

            Program.variables[variable] = Regex.Replace(input, pattern, replacement);
        }
#if Windows
        private static void Kw_Addtextbox()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string x = Utils.GetString(args, 1);
            string y = Utils.GetString(args, 2);
            string width = Utils.GetString(args, 3);
            string height = Utils.GetString(args, 4);
            string method = Utils.GetString(args, 5);

            if (x == string.Empty || y == string.Empty || width == string.Empty || height == string.Empty || method == string.Empty)
            {
                return;
            }

            // Check if we are on Windows
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Program.Error("GUIs are only supported on Windows");
                return;
            }

            // Create the textbox
            TextBox textbox = new TextBox();
            textbox.Left = int.Parse(x);
            textbox.Top = int.Parse(y);
            textbox.Width = int.Parse(width);
            textbox.Height = int.Parse(height);
            textbox.TextChanged += (sender, e) =>
            {
                Program.methodCode[method] = textbox.Text.Split('\n');
            };
            Program.form.Controls.Add(textbox);
        }

        private static void Kw_Addlabel()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string text = Utils.GetString(args, 1);
            string x = Utils.GetString(args, 2);
            string y = Utils.GetString(args, 3);
            string width = Utils.GetString(args, 4);
            string height = Utils.GetString(args, 5);

            if (text == string.Empty || x == string.Empty || y == string.Empty || width == string.Empty || height == string.Empty)
            {
                return;
            }

            // Check if we are on Windows
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Program.Error("GUIs are only supported on Windows");
                return;
            }

            // Create the label
            Label label = new Label();
            label.Text = text;
            label.Left = int.Parse(x);
            label.Top = int.Parse(y);
            label.Width = int.Parse(width);
            label.Height = int.Parse(height);
            Program.form.Controls.Add(label);
            label.BringToFront();
        }

        private static void Kw_Addbutton()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string text = Utils.GetString(args, 1);
            string x = Utils.GetString(args, 2);
            string y = Utils.GetString(args, 3);
            string width = Utils.GetString(args, 4);
            string height = Utils.GetString(args, 5);
            string method = Utils.GetString(args, 6);

            if (text == string.Empty || x == string.Empty || y == string.Empty || width == string.Empty || height == string.Empty || method == string.Empty)
            {
                return;
            }

            // Check if we are on Windows
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Program.Error("GUIs are only supported on Windows");
                return;
            }

            // Create the button
            Button button = new Button();
            button.Text = text;
            button.Left = int.Parse(x);
            button.Top = int.Parse(y);
            button.Width = int.Parse(width);
            button.Height = int.Parse(height);
            button.Click += (sender, e) =>
            {
                int oldj = Program.j;
                Program.ParseCOOL(string.Join("\n", Program.methodCode[method]), true);
                Program.j = oldj;
            };
            Program.form.Controls.Add(button);
        }

        private static void Kw_Newgui()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string title = Utils.GetString(args, 1);
            string width = Utils.GetString(args, 2);
            string height = Utils.GetString(args, 3);

            if (title == string.Empty || width == string.Empty || height == string.Empty)
            {
                return;
            }

            // Check if we are on Windows
            if (Environment.OSVersion.Platform != PlatformID.Win32NT)
            {
                Program.Error("GUIs are only supported on Windows");
                return;
            }

            // Create the form
            Program.form = new Form();
            Program.form.Text = title;
            Program.form.Width = int.Parse(width);
            Program.form.Height = int.Parse(height);
            Program.form.Show();
        }
#endif
        private static void Kw_Execute()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string command = Utils.GetString(args, 1);
            string arguments = Utils.GetString(args, 2);

            if (command == string.Empty)
            {
                return;
            }

            try
            {
                Process.Start(command, arguments).WaitForExit(-1);
            }
            catch (FileNotFoundException)
            {
                Program.Error("File not found");
            }
        }

        private static void Kw_Urlpost()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string varname = args[1];
            string url = Utils.GetString(args, 2);
            string data = Utils.GetString(args, 3);

            if (varname == string.Empty || url == string.Empty || !varname.StartsWith("$"))
            {
                return;
            }
            varname = varname.Substring(1);
            try
            {
                HttpClient client = new HttpClient();
                var c = new StringContent(data, System.Text.Encoding.UTF8, "application/json");
                string response = client.PostAsync(url, c).Result.Content.ReadAsStringAsync().Result;
                Program.variables[varname] = response;
            }
            catch (Exception)
            {
                Program.Error("Failed to POST URL");
            }
        }

        private static void Kw_Urlget()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string varname = args[1];
            string url = Utils.GetString(args, 2);

            if (varname == string.Empty || url == string.Empty || !varname.StartsWith("$"))
            {
                return;
            }
            varname = varname.Substring(1);
            try
            {
                HttpClient client = new HttpClient();
                string response = client.GetAsync(url).Result.Content.ReadAsStringAsync().Result;
                Program.variables[varname] = response;
            }
            catch (Exception)
            {
                Program.Error("Failed to GET URL");
            }
        }

        private static void Kw_Callmethod()
        {
            int oldj = Program.j;
            Program.ParseCOOL(string.Join("\n", Program.methodCode[Utils.GetString(CodeParser.ParseLineIntoTokens(line), 1)]), true);
            Program.j = oldj;
        }

        private static void Kw_Stopmethod()
        {
            // This is a placeholder, it is not used
        }

        private static void Kw_Method()
        {
            // Get everything after the method keyword
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string methodName = args[1];
            // And now all the code after the method name
            string[] lines = Utils.GetLinesAfter(Program.j);
            // And now we add the lines (until "endmethod") to the method
            List<string> linesList = new List<string>();
            linesList.AddRange(lines[1..]);
            for (int i = 0; i < linesList.Count; i++)
            {
                if (linesList[i].Trim() == "stopmethod")
                {
                    for (int j = i; j < linesList.Count; j++)
                    {
                        linesList.RemoveAt(j);
                    }
                    break;
                }
            }
            lines = linesList.ToArray();
            Program.methodCode.Add(methodName, lines);
            // Skip the lines we just added
            Program.j += lines.Length;
        }

        // Urldecode and urlencode take a variable name and a value
        // and encode/decode the value and store it in the variable
        public static void Kw_Urlencode()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string varname = args[1];
            string value = Utils.GetString(args, 2);

            if (varname == string.Empty || value == string.Empty || !varname.StartsWith("$"))
            {
                return;
            }

            varname = varname[1..];

            if (Program.variables.ContainsKey(varname))
            {
                Program.variables[varname] = WebUtility.UrlEncode(value) ?? string.Empty;
            }
            else
            {
                Program.variables.Add(varname, WebUtility.UrlEncode(value) ?? string.Empty);
            }
        }

        public static void Kw_Urldecode()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string varname = args[1];
            string value = Utils.GetString(args, 2);

            if (varname == string.Empty || value == string.Empty || !varname.StartsWith("$"))
            {
                return;
            }

            varname = varname[1..];

            if (Program.variables.ContainsKey(varname))
            {
                Program.variables[varname] = WebUtility.UrlDecode(value);
            }
            else
            {
                Program.variables.Add(varname, WebUtility.UrlDecode(value));
            }
        }

        private static void Kw_Httpserve()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string port = Utils.GetString(args, 1);
            string method = Utils.GetString(args, 2); // method called every time a request is made
            if (port == string.Empty || method == string.Empty)
            {
                return;
            }

            HttpListener listener = new HttpListener();
            if (Utils.IsAdmin())
            {
                listener.Prefixes.Add($"http://*:{port}/");
            }
            else
            {
                listener.Prefixes.Add($"http://localhost:{port}/");
            }

            Thread listenerThread = new Thread(() =>
            {
                listener.Start();
                while (true)
                {
                    HttpListenerContext context = listener.GetContext();
                    ctx = context;
                    string methodName = Utils.GetString(args, 2);
                    Program.variables["_REQUEST_URL"] = context.Request.RawUrl;
                    Program.variables["_REQUEST_METHOD"] = context.Request.HttpMethod;
                    Program.variables["_REQUEST_CONTENT"] = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
                    Program.variables["_REQUEST_COOKIES"] = context.Request.Headers["Cookie"];
                    Program.variables["_REQUEST_USERAGENT"] = context.Request.UserAgent;
                    Program.variables["_REQUEST_IP"] = context.Request.RemoteEndPoint.Address.ToString();
                    Program.ParseCOOL(string.Join("\n", Program.methodCode[methodName]), true);
                    context.Response.StatusCode = Program.variables.ContainsKey("_REQUEST_STATUS") ? int.Parse(Program.variables["_REQUEST_STATUS"]) : 404;
                    context.Response.StatusDescription = "OK";
                    context.Response.ContentType = "text/html";
                    string output = Program.variables.ContainsKey("_REQUEST_RESPONSE") ? Program.variables["_REQUEST_RESPONSE"] : (context.Response.StatusCode.ToString() + " Not Found");
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(output);
                    context.Response.ContentLength64 = buffer.Length;
                    context.Response.OutputStream.Write(buffer, 0, buffer.Length);
                    context.Response.OutputStream.Close();
                    Program.variables.Remove("_REQUEST_URL");
                    Program.variables.Remove("_REQUEST_METHOD");
                    Program.variables.Remove("_REQUEST_CONTENT");
                    Program.variables.Remove("_REQUEST_COOKIES");
                    Program.variables.Remove("_REQUEST_USERAGENT");
                    Program.variables.Remove("_REQUEST_IP");
                    ctx.Response.Close();
                }
            });

            listenerThread.Start();
            Program.variables["_HTTP_RUNNING"] = "true";
            Program.variables["_HTTP_PORT"] = port;
        }

        private static void Kw_Random()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string varname = "_RANDOM";

            if (varname == string.Empty)
            {
                return;
            }

            if (Program.variables.ContainsKey(varname))
            {
                Program.variables[varname] = Program.random.Next(0, Program.randMax).ToString();
            }
            else
            {
                Program.variables.Add(varname, Program.random.Next(0, Program.randMax).ToString());
            }
        }

        private static void Kw_Readfile()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string filename = Utils.GetString(args, 1);

            if (filename == string.Empty)
            {
                return;
            }
            if (System.IO.File.Exists(filename))
            {
                Program.variables["_FILE"] = System.IO.File.ReadAllText(filename);
            }
        }

        private static void Kw_Appendfile()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string filename = Utils.GetString(args, 1);
            string content = Utils.GetString(args, 2);

            if (filename == string.Empty || content == string.Empty)
            {
                return;
            }

            System.IO.File.AppendAllText(filename, content);
        }

        private static void Kw_Rmfile()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string filename = Utils.GetString(args, 1);

            if (filename == string.Empty)
            {
                return;
            }
            if (System.IO.File.Exists(filename))
            {
                System.IO.File.Delete(filename);
            }
        }

        private static void Kw_Writefile()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string filename = Utils.GetString(args, 1);
            string content = Utils.GetString(args, 2);

            if (filename == string.Empty || content == string.Empty)
            {
                return;
            }

            System.IO.File.WriteAllText(filename, content);
        }

        private static void Kw_Existing()
        {
            // This is a special keyword that is used to check if a variable exists

            string varName = CodeParser.ParseLineIntoTokens(line)[1];
            if (!varName.StartsWith("$"))
            {
                return;
            }
            varName = varName.Substring(1); // Remove the $ from the name

            if (Program.variables.ContainsKey(varName))
            {
                if (Program.variables.ContainsKey("_EXISTS"))
                {
                    Program.variables["_EXISTS"] = "true";
                }
                else
                {
                    Program.variables.Add("_EXISTS", "true");
                }
            }
            else
            {
                if (Program.variables.ContainsKey("_EXISTS"))
                {
                    Program.variables["_EXISTS"] = "false";
                }
                else
                {
                    Program.variables.Add("_EXISTS", "false");
                }
            }
        }

        private static void Kw_Mail()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            string to = Utils.GetString(args, 1);
            string subject = Utils.GetString(args, 2);
            string body = Utils.GetString(args, 3);
            string from = Utils.GetString(args, 4);
            string password = Utils.GetString(args, 5);
            string smtp = Utils.GetString(args, 6);
            int port = 25;

            MailMessage mail = new MailMessage();
            SmtpClient SmtpServer = new SmtpClient(smtp);

            mail.From = new MailAddress(from);
            mail.To.Add(to);
            mail.Subject = subject;
            mail.Body = body;

            SmtpServer.Port = port;
            SmtpServer.Credentials = new System.Net.NetworkCredential(from, password);

            SmtpServer.Send(mail);
        }

        public static int Parse(string lineToParse)
        {
            Utils.Init();
            line = lineToParse;

            int i = 0;
            foreach (var keyword in keywords)
            {
                if (line.StartsWith(keyword.Key))
                {
                    i++;
                    keyword.Value();        // We can call this since it's an action
                    if (error == 1)
                    {
                        return 0;
                    }
                    break;
                }
            }
            if (i == 0)
            {
                return 2;
            }
            return 1;
        }

        #region Operators
        public static void Op_DollarEquals()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            Utils.currentChar = 2;
            Program.FormattedPrint(Utils.GetString(args, 1));
        }
        public static void Op_Dollar()
        {
            // Arrays in Eaucool are defined like this:
            // $array = "value1|value2|value3"
            // You need to escape the | character if you want to use it in a string
            if (line.Contains("="))        // Array or variable
            {
                string fixede = line[1..].Split("=")[0].Replace(" ", "").Replace("{", "");
                string newContent = line.Split("=")[1].TrimStart();
                foreach (string var in Program.variables.Keys)
                {
                    newContent = newContent.ReplaceWord("$" + var, Program.variables[var]);
                }
                //Program.variables.Remove(line[1..].Split("=")[0].Replace(" ", "").Replace("{", ""));
                if (Program.variables.ContainsKey(fixede))
                {
                    Program.variables.Remove(fixede);
                }
                Program.variables.Add(line[1..].Split("=")[0].Replace(" ", ""), newContent);
            }
            else
            {
                Program.Error("Invalid argument for variable");
                error = 1;
                return;
            }
        }
        #endregion
        #region Keywords
        public static void Kw_Echo()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            Utils.currentChar = 5;
            Console.WriteLine(Utils.GetString(args, 1));
        }

        public static void Kw_Rndmax()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 1)
            {
                Utils.currentChar = 7;
                try
                {
                    Program.randMax = int.Parse(Utils.GetString(args, 1));
                }
                catch
                {
                    Program.randMax = Utils.defaultReturnValue;
                }
            }
            else
            {
                Program.randMax = Utils.defaultReturnValue;
            }
        }


        public static void Kw_If()
        {

            if (line.Contains("="))
            {
                string toCheck = line[3..].Split("=")[0].TrimEnd();        // Just some stuff that makes
                                                                           // it contain the first argument
                if (line.Substring(3, 1) == "$" && Program.IsVariable(line[4..].Split("=")[0].Trim()))
                {
                    Program.variables.TryGetValue(line[4..].Split("=")[0].TrimEnd(), out string varcont);
                    toCheck = varcont;
                }
                string secondCheck = line[3..].Split("=")[1].TrimStart();        // The thing to compare to
                if (line.Split("=")[1].Trim() == "NULL" && !Program.IsVariable(line[4..].Split("=")[0].Trim()))
                {
                    toCheck = null;
                    secondCheck = null;
                }
                if (line.Split("=")[1].Trim() == "NOTHING")
                {
                    secondCheck = "";
                }
                if (line.Split("=")[1].Trim()[..1] == "$" && Program.IsVariable(line[4..].Split("=")[1].Trim()[1..]))
                {
                    Program.variables.TryGetValue(line[4..].Split("=")[1].Trim()[1..], out string varcont);
                    secondCheck = varcont;
                }
                if (toCheck != secondCheck)
                {
                    Program.skipIfStmtElse = true;
                }
                else
                {
                    Program.skipElseStmt = true;
                }
            }
        }
        public static void Kw_Getdate()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            if (args.Length > 1)
            {
                if (Program.IsVariable(args[1]))
                {
                    Program.variables.Remove(args[1]);
                }
                Program.variables.Add(args[1], DateTime.UtcNow.ToString("yyyy-MM-dd"));
            }
        }
        public static void Kw_Escape()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);

            if (args.Length > 1 && args[1][0] == '$')
            {
                Program.variables[args[1][1..]] = Program.SafeEscape(Program.variables[args[1][1..]]);
            }
            else
            {
                Program.Error("Variable expected");
                error = 1;
                return;
            }
        }
        public static void Kw_Replace()
        {
            string[] args = CodeParser.ParseLineIntoTokens(line);
            if (args.Length > 3 && args[1][0] == '$')
            {
                if (Program.IsVariable(args[1][1..]))
                {
                    Program.variables[args[1][1..]] = Program.variables[args[1][1..]].Replace(args[2], args[3]);
                }
            }
            else
            {
                Program.Error("Variable expected");
                error = 1;
                return;
            }
        }
        #endregion
    }
}
