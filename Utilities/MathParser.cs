using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Eaucool.Utilities
{
    internal class MathParser
    {
        public static string Parse(string expression)
        {
            double output = 0;
            string plus = "+";
            string minus = "-";
            string multiply = "*";
            string divide = "/";
            string power = "^";
            string[] expressionArray = expression.Split(' ');
            for (int i = 0; i < expressionArray.Length; i++)
            {
                if (expressionArray[i] == plus)
                {
                    output += double.Parse(expressionArray[i - 1]) + double.Parse(expressionArray[i + 1]);
                }
                else if (expressionArray[i] == minus)
                {
                    output += double.Parse(expressionArray[i - 1]) - double.Parse(expressionArray[i + 1]);
                }
                else if (expressionArray[i] == multiply)
                {
                    output += double.Parse(expressionArray[i - 1]) * double.Parse(expressionArray[i + 1]);
                }
                else if (expressionArray[i] == divide)
                {
                    output += double.Parse(expressionArray[i - 1]) / double.Parse(expressionArray[i + 1]);
                }
                else if (expressionArray[i] == power)
                {
                    output += Math.Pow(double.Parse(expressionArray[i - 1]), double.Parse(expressionArray[i + 1]));
                }
            }
            return output.ToString();
        }
    }
}
