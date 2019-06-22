using CodingSeb.ExpressionEvaluator;
using CanardSharp.Dsdl.DataTypes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CanardSharp.Dsdl
{
    class DsdlConstExprEvaluator : ExpressionEvaluator
    {
        readonly PrimitiveDsdlType _expectedReturnType;

        public DsdlConstExprEvaluator(PrimitiveDsdlType expectedReturnType)
        {
            if (expectedReturnType == null)
                throw new ArgumentNullException(nameof(expectedReturnType));
            _expectedReturnType = expectedReturnType;

            OptionEvaluateFunctionActive = false;

            TypesToBlock.Add(typeof(ExpressionEvaluator));
            TypesToBlock.Add(typeof(DsdlConstExprEvaluator));
        }

        protected override bool EvaluateNumber(string expression, Stack<object> stack, ref int i)
        {
            var restOfExpression = expression.Substring(i);
            var numberMatch = Regex.Match(restOfExpression, numberRegexPattern, RegexOptions.IgnoreCase);
            var otherBaseMatch = otherBasesNumberRegex.Match(restOfExpression);

            if (otherBaseMatch.Success
                && (!otherBaseMatch.Groups["sign"].Success
                || stack.Count == 0
                || stack.Peek() is ExpressionOperator))
            {
                i += otherBaseMatch.Length;
                i--;

                int baseValue = otherBaseMatch.Groups["type"].Value.Equals("b") ? 2 : 16;

                if (otherBaseMatch.Groups["sign"].Success)
                {
                    string value = otherBaseMatch.Groups["value"].Value.Replace("_", "").Substring(2);
                    if (otherBaseMatch.Groups["sign"].Value.Equals("-"))
                    {
                        long numValue = -Convert.ToInt64(value, baseValue);
                        stack.Push(numValue >= int.MinValue ? (object)(int)numValue : numValue);
                    }
                    else
                    {
                        long numValue = Convert.ToInt64(value, baseValue);
                        stack.Push(numValue <= int.MaxValue ? (object)(int)numValue : numValue);
                    }
                }
                else
                {
                    var value = otherBaseMatch.Value.Replace("_", "").Substring(2);
                    ulong numValue = Convert.ToUInt64(value, baseValue);
                    stack.Push(numValue <= int.MaxValue ? (int)numValue :
                        numValue <= uint.MaxValue ? (uint)numValue :
                        numValue <= long.MaxValue ? (object)(long)numValue : numValue);
                }

                return true;
            }
            else if (numberMatch.Success
                && (!numberMatch.Groups["sign"].Success
                || stack.Count == 0
                || stack.Peek() is ExpressionOperator))
            {
                i += numberMatch.Length;
                i--;

                if (numberMatch.Groups["type"].Success)
                {
                    string type = numberMatch.Groups["type"].Value;
                    string numberNoType = numberMatch.Value.Replace(type, string.Empty).Replace("_", "");

                    if (numberSuffixToParse.TryGetValue(type, out Func<string, CultureInfo, object> parseFunc))
                    {
                        stack.Push(parseFunc(numberNoType, CultureInfoForNumberParsing));
                    }
                }
                else
                {
                    if (OptionForceIntegerNumbersEvaluationsAsDoubleByDefault || numberMatch.Groups["hasdecimal"].Success)
                    {
                        stack.Push(double.Parse(numberMatch.Value.Replace("_", ""), NumberStyles.Any, CultureInfoForNumberParsing));
                    }
                    else
                    {
                        stack.Push(int.Parse(numberMatch.Value.Replace("_", ""), NumberStyles.Any, CultureInfoForNumberParsing));
                    }
                }

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
