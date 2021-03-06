using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;

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

        private static IEnumerable<string> IterateParameters(string s, string escapeChar)
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

                if (isParameter && sb.Length == 0 && char.IsLetter(c))
                    sb.Append(c);
                else if (isParameter && sb.Length > 0 && char.IsLetterOrDigit(c))
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
            var sb = new StringBuilder(format.Length*2);
            foreach (var fragment in format.Fragments())
            {
                sb.Append(fragment.ToString(parameters));
            }
            return sb.ToString();
        }

        class Fragment
        {
            public static Fragment Literal()
            {
                return new Fragment((content, ignored) => content);
            }

            public static Fragment Expression()
            {
                return new Fragment((expression, source) => expression.Eval(source));
            }

            private Fragment(Func<string, object, string> toString)
            {
                _toString = toString;
            }

            private readonly StringBuilder _s = new StringBuilder();
            private readonly Func<string, object, string> _toString;

            public void Append(char c)
            {
                _s.Append(c);
            }

            public string ToString(object source)
            {
                return _toString(_s.ToString(), source);
            }

            public override string ToString()
            {
                return _s.ToString();
            }
        }

        static IEnumerable<Fragment> Fragments(this string s)
        {
            return new FragmentIterator().GetFragments(s);
        }

        class FragmentIterator
        {
            private readonly Queue<Fragment> _fragments = new Queue<Fragment>();
            private Fragment _currentFragment;

            enum Location
            {
                OutsideExpression,
                InsideExpression,
                OnCloseBracket,
                OnOpenBracket
            }

            public IEnumerable<Fragment> GetFragments(string format)
            {
                NextFragment(Fragment.Literal());

                var l = Location.OutsideExpression;

                foreach (var c in format)
                {
                    switch (l)
                    {
                        case Location.OutsideExpression:
                            l = OnOutsideExpression(l, c);
                            break;
                        case Location.InsideExpression:
                            l = OnInsideExpression(l, c);
                            break;
                        case Location.OnCloseBracket:
                            l = OnCloseBracket(l, c);
                            break;
                        case Location.OnOpenBracket:
                            l = OnOpenBracket(l, c);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }

                    while (_fragments.Any())
                    {
                        yield return _fragments.Dequeue();
                    }

                }

                if (l != Location.OutsideExpression)
                    throw new FormatException();

                if (_currentFragment != null)
                    yield return _currentFragment;
            }

            private Location OnOpenBracket(Location l, char c)
            {
                if (l != Location.OnOpenBracket)
                    throw new ArgumentException("l");
                if (c == '{')
                {
                    _currentFragment.Append('{');
                    return Location.OutsideExpression;
                }
                NextFragment(Fragment.Expression());
                _currentFragment.Append(c);
                return Location.InsideExpression;
            }

            private Location OnCloseBracket(Location l, char c)
            {
                if (l != Location.OnCloseBracket)
                    throw new ArgumentException("l");
                
                if (c == '}')
                {
                    _currentFragment.Append('}');
                    return Location.OutsideExpression;
                }
                
                throw new FormatException();
            }

            private Location OnInsideExpression(Location l, char c)
            {
                if (l != Location.InsideExpression)
                    throw new ArgumentException("l");

                if (c == '}')
                {
                    NextFragment(Fragment.Literal());
                    return Location.OutsideExpression;
                }
                
                _currentFragment.Append(c);
                return l;
            }

            private Location OnOutsideExpression(Location l, char c)
            {
                if (l != Location.OutsideExpression)
                    throw new ArgumentException("l");
                
                if (c == '{')
                {
                    return Location.OnOpenBracket;
                }
                if (c == '}')
                {
                    return Location.OnCloseBracket;
                }
                _currentFragment.Append(c);
                return l;
            }
            
            private void NextFragment(Fragment fragment)
            {
                if (_currentFragment != null)
                    _fragments.Enqueue(_currentFragment);
                _currentFragment = fragment;
            }
        }

        public static string Eval(this string expression, object obj)
        {
            var colonIndex = expression.IndexOf(':');
            if (colonIndex > 0)
            {
                var format = "{0:" + expression.Substring(colonIndex + 1) + "}";
                expression = expression.Substring(0, colonIndex);
                var value = Evaluate(expression, obj);
                return string.Format(format, value);
            }
            else
            {
                var value = Evaluate(expression, obj);
                if (value == null) return string.Empty;
                return value.ToString();
            }
        }

        private static object Evaluate(string expression, object target)
        {
            var targetType = target.GetType();
            Delegate d;
            var key = new {targetType, expression};
            if (!Delegates.TryGetValue(key, out d))
            {
                ParameterExpression[] parameters = { Expression.Parameter(targetType, "") };
                var parser = new ExpressionParser(parameters, expression, null);
                var body = parser.Parse(typeof (object));
                var lambda = Expression.Lambda(body, parameters);
                d = lambda.Compile();
                Delegates[key] = d;
            }
            var value = d.DynamicInvoke(target);
            return value;
        }

        static readonly IDictionary<object, Delegate> Delegates = new Dictionary<object, Delegate>();
    }
}