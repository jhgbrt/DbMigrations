using System;

namespace DbMigrations.Client.Infrastructure
{
    internal static class Logger
    {
        private static readonly object Lock = new object();
        public static void InfoLine(string message) => WriteLine(Console.ForegroundColor, message);

        public static void WriteInfo(string message) => Write(Console.ForegroundColor, message);

        public static void WarnLine(string message) => WriteLine(ConsoleColor.DarkYellow, message);

        public static void WriteWarn(string message) => Write(ConsoleColor.DarkYellow, message);

        public static void ErrorLine(string message) => WriteLine(ConsoleColor.Red, message);

        public static void WriteError(string message) => Write(ConsoleColor.Red, message);

        public static void WriteOk() => Write(ConsoleColor.Green, "OK");

        public static void OkLine() => WriteLine(ConsoleColor.Green, "OK");

        public static void WriteLine(ConsoleColor foregroundColor, string message)
        {
            var originalColor = Console.ForegroundColor;
            lock (Lock)
            {
                try
                {
                    Console.ForegroundColor = foregroundColor;
                    Console.WriteLine(message);
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        public static void Write(ConsoleColor foregroundColor, string message)
        {
            var originalColor = Console.ForegroundColor;
            lock (Lock)
            {
                try
                {
                    Console.ForegroundColor = foregroundColor;
                    Console.Write(message);
                }
                finally
                {
                    Console.ForegroundColor = originalColor;
                }
            }
        }

        public static void WriteLine() => Console.WriteLine();
    }
}
