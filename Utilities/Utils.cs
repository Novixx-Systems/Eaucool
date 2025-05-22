using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace Eaucool.Utilities
{
    internal static class Utils
    {
        public static int currentChar;
        public static int defaultReturnValue = 0;
        public static void Init()
        {
            currentChar = 0;
        }

        /// <summary>
        /// Replaces a word in a string with another word, but only if the word is not part of another word (full word match)
        /// </summary>
        /// <returns>The string with the word replaced</returns>
        public static string ReplaceWord(this string text, string word, string bywhat)
        {
            static bool IsWordChar(char c) => char.IsLetterOrDigit(c) || c == '_' || c == '$';
            StringBuilder sb = null;
            int p = 0, j = 0;
            while (j < text.Length && (j = text.IndexOf(word, j, StringComparison.Ordinal)) >= 0)
                if ((j == 0 || !IsWordChar(text[j - 1])) &&
                    (j + word.Length == text.Length || !IsWordChar(text[j + word.Length])))
                {
                    sb ??= new StringBuilder();
                    sb.Append(text, p, j - p);
                    sb.Append(bywhat);
                    j += word.Length;
                    p = j;
                }
                else j++;
            if (sb == null) return text;
            sb.Append(text, p, text.Length - p);
            return sb.ToString();
        }

        /// <summary>
        /// On Windows, checks if the current user is an admin
        /// </summary>
        /// <returns>True if the current user is an admin, false otherwise</returns>
        public static bool IsAdmin()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            else
            {
                return true; // Linux and MacOS don't have the same concept of admin privileges AFAIK
            }
        }

        /// <summary>
        /// Gets the string from an array of strings, and replaces all Eaucool variables with their values
        /// </summary>
        /// <param name="args">The array of strings to get the string from</param>
        /// <param name="arg">The index of the string to get</param>
        /// <returns>The string from the array of strings</returns>
        public static string GetString(string[] args, int arg = 0, bool noSpace = true)
        {
            string returnValue;
            try
            {
                //returnValue = Parser.line[currentChar..];
                returnValue = args[arg];
            }
            catch
            {
                returnValue = "";
                Program.Error("Not enough arguments for statement");
            }
            foreach (string var in Program.variables.Keys)
            {
                returnValue = returnValue.ReplaceWord("$" + var, (noSpace ? "" : " ") + Program.variables[var]);
            }
            return returnValue;
        }

        /// <summary>
        /// Gets the lines after the given line number
        /// </summary>
        /// <param name="line">The line number to get the lines after</param>
        /// <returns>An array of strings containing the lines after the given line number</returns>
        internal static string[] GetLinesAfter(int line)
        {
            string[] lines = Program.currentFileCode.Split('\n');
            int index = line;
            return lines[index..];
        }
    }
}
