using System;
using static System.Console;
using static System.ConsoleColor;

namespace DbMigrations.Client.Infrastructure
{
    public class Logger
    {
        private static readonly object Lock = new object();
        public Logger InfoLine(string message) => Info(message).Line();
        public Logger Info(string message) => Write(ForegroundColor, message);
        public Logger WarnLine(string message) => Warn(message).Line();
        public Logger Warn(string message) => Write(DarkYellow, message);
        public Logger ErrorLine(string message) => Error(message).Line();
        public Logger Error(string message) => Write(Red, message);
        public Logger Ok() => Write(Green, "OK");
        public Logger OkLine() => Ok().Line();

        public Logger Write(ConsoleColor foregroundColor, string message)
        {
            var originalColor = ForegroundColor;
            lock (Lock)
            {
                try
                {
                    ForegroundColor = foregroundColor;
                    Console.Write(message);
                }
                finally
                {
                    ForegroundColor = originalColor;
                }
            }
            return this;
        }

        public Logger Line()
        {
            WriteLine();
            return this;
        }
    }
}
