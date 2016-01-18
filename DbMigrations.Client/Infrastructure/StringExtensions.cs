using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.UI;

namespace DbMigrations.Client.Infrastructure
{
    public static class StringExtensions
    {
        // ReSharper disable once InconsistentNaming
        private static readonly MD5 MD5 = MD5.Create();
        public static string Checksum(this string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            var hash = MD5.ComputeHash(bytes);
            var checksum = BitConverter.ToString(hash).Replace("-", "");
            return checksum;

        }

        public static IEnumerable<string> Lines(this string text)
        {
            var lineStart = 0;
            var lineEnd = -1;
            while (lineEnd < text.Length - 1)
            {

                lineEnd = text.IndexOf('\n', lineStart);
                if (lineEnd == -1)
                {
                    lineEnd = text.Length - 1;
                }
                var line = text.Substring(lineStart, lineEnd + 1 - lineStart);
                yield return line;
                lineStart = lineEnd + 1;
            }
        }
        public static IEnumerable<string> Parameters(this string s, string escapeChar)
        {
            if (string.IsNullOrEmpty(s)) throw new ArgumentNullException(nameof(s));
            return IterateParameters(s, escapeChar);
        }
        private static IEnumerable<string> IterateParameters(string s, string escapeChar )
        {
            bool isParameter = false;
            var sb = new StringBuilder();

            foreach (var c in s)
            {
                if (sb.ToString().EndsWith(escapeChar))
                {
                    if (isParameter)
                        throw new InvalidOperationException("Invalid query");
                    isParameter = true;
                    sb.Clear();
                }

                if (isParameter && sb.Length == 0 && Char.IsLetter(c))
                    sb.Append(c);
                else if (isParameter && sb.Length > 0 && Char.IsLetterOrDigit(c))
                    sb.Append(c);
                else if (isParameter)
                {
                    yield return sb.ToString();
                    isParameter = false;
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (sb.Length > 0 && isParameter)
                yield return sb.ToString();
        }

        public static IEnumerable<string> Words(this string s)
        {
            if (string.IsNullOrEmpty(s)) throw new ArgumentNullException(nameof(s));
            return IterateWords(s);
        }

        private static IEnumerable<string> IterateWords(string s)
        {
            var sb = new StringBuilder();

            foreach (var c in s)
            {
                if (char.IsUpper(c) && sb.Length > 0 && !char.IsUpper(sb[sb.Length - 1]))
                {
                    yield return sb.ToString();
                    sb.Clear();
                    sb.Append(c);
                }
                else
                {
                    sb.Append(c);
                }
            }

            if (sb.Length > 0) 
                yield return sb.ToString();
        }

        public static string ToUpperCaseWithUnderscores(this string text)
        {
            return string.Join("_",
                    text.Words().Select(s => s.ToUpperInvariant())
                );
        }

        public static string FormatWith(this string format, object parameters)
        {
            if (format == null)
            {
                throw new ArgumentNullException(nameof(format));
            }

            var context = new NamedFormat();
            return context.GetResult(format, parameters);

        }
        class NamedFormat
        {
            public string GetResult(string format, object source)
            {
                var query = from fragments in GetFragments(format)
                            select fragments.Eval(source);

                var result = new StringBuilder(format.Length * 2);
                foreach (var s in query)
                {
                    result.Append(s);
                }
                return result.ToString();
            }

            private IEnumerable<Fragment> GetFragments(string format)
            {
                return new NamedFormatParser().GetFragments(format);
            }

            class Fragment
            {
                public static Fragment Literal()
                {
                    return new Fragment(new LiteralFormatter());
                }

                public static Fragment Databinding()
                {
                    return new Fragment(new DatabindingFormatter());
                }

                private Fragment(IFormatter f)
                {
                    _formatter = f;
                }

                readonly StringBuilder _s = new StringBuilder();
                private readonly IFormatter _formatter;

                public void Append(char c)
                {
                    _s.Append(c);
                }
                
                public string Eval(object source)
                {
                    return _formatter.Eval(Str, source);
                }

                private string Str => _s.ToString();
            }

            class NamedFormatParser
            {
                private readonly IList<Fragment> _fragments = new List<Fragment>();

                private Fragment CurrentFragment { get; set; }

                private FormatLocation Location { get; set; }

                public IEnumerable<Fragment> GetFragments(string format)
                {
                    SwitchToLiteral();
                    Location = new OutsideExpression(this);

                    foreach (var c in format)
                    {
                        Location = Location.Next(c);
                    }

                    if (!Location.IsValidEndLocation) throw new FormatException();

                    return _fragments;
                }


                void Append(char c)
                {
                    CurrentFragment.Append(c);
                }

                private void NextFragment(Fragment fragment)
                {
                    _fragments.Add(fragment);
                    CurrentFragment = fragment;
                }

                void SwitchToLiteral()
                {
                    NextFragment(Fragment.Literal());
                }

                void SwitchToExpression()
                {
                    NextFragment(Fragment.Databinding());
                }

                abstract class FormatLocation
                {
                    protected NamedFormatParser Parser { get; }

                    protected FormatLocation(NamedFormatParser context)
                    {
                        Parser = context;
                    }

                    public abstract FormatLocation Next(char c);

                    public abstract bool IsValidEndLocation { get; }
                }

                class OutsideExpression : FormatLocation
                {
                    public OutsideExpression(NamedFormatParser context)
                        : base(context)
                    {
                    }

                    public override FormatLocation Next(char c)
                    {
                        switch (c)
                        {
                            case '{':
                                return new OnOpenBracket(Parser);
                            case '}':
                                return new OnCloseBracket(Parser);
                            default:
                                Parser.Append(c);
                                return this;
                        }
                    }

                    public override bool IsValidEndLocation => true;
                }

                class OnCloseBracket : FormatLocation
                {
                    public OnCloseBracket(NamedFormatParser context)
                        : base(context)
                    {
                    }

                    public override FormatLocation Next(char c)
                    {
                        switch (c)
                        {
                            case '}':
                                Parser.Append('}');
                                return new OutsideExpression(Parser);
                            default:
                                throw new FormatException();
                        }
                    }
                    public override bool IsValidEndLocation => false;
                }

                class OnOpenBracket : FormatLocation
                {
                    public OnOpenBracket(NamedFormatParser context)
                        : base(context)
                    {
                    }

                    public override FormatLocation Next(char c)
                    {
                        switch (c)
                        {
                            case '{':
                                Parser.Append('{');
                                return new OutsideExpression(Parser);
                            default:
                                Parser.SwitchToExpression();
                                Parser.Append(c);
                                return new InsideExpression(Parser);
                        }

                    }
                    public override bool IsValidEndLocation => false;
                }

                class InsideExpression : FormatLocation
                {
                    public InsideExpression(NamedFormatParser context)
                        : base(context)
                    {
                    }

                    public override FormatLocation Next(char c)
                    {
                        switch (c)
                        {
                            case '}':
                                Parser.SwitchToLiteral();
                                return new OutsideExpression(Parser);
                            default:
                                Parser.Append(c);
                                return this;
                        }
                    }
                    public override bool IsValidEndLocation => false;
                }

            }
            interface IFormatter
            {
                string Eval(string format, object source);
            }
            class LiteralFormatter : IFormatter
            {
                public string Eval(string format, object source)
                {
                    return format;
                }
            }
            class DatabindingFormatter : IFormatter
            {
                public string Eval(string s, object source)
                {
                    var expression = s;
                    var format = "";

                    var colonIndex = expression.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        format = expression.Substring(colonIndex + 1);
                        expression = expression.Substring(0, colonIndex);
                    }

                    try
                    {
                        if (string.IsNullOrEmpty(format))
                        {
                            return (DataBinder.Eval(source, expression) ?? "").ToString();
                        }
                        return DataBinder.Eval(source, expression, "{0:" + format + "}");
                    }
                    catch (HttpException ex)
                    {
                        throw new FormatException(ex.Message);
                    }
                }
            }
        }
    }
}