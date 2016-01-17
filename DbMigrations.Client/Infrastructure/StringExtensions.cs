using System;
using System.Collections.Generic;
using System.Linq;
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
            if (string.IsNullOrEmpty(s)) throw new ArgumentNullException("s");
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
            if (string.IsNullOrEmpty(s)) throw new ArgumentNullException("s");
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
    }
}