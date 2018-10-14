using System;
using Microsoft.Extensions.Logging;
using SQ7MRU.Utils;

namespace SampleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SQ7MRU eQSL Downloader\n");

            Console.WriteLine("Hello World!");
            ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Trace);

            Console.WriteLine("Enter Login to eqsl.cc : ");
            string login = Console.ReadLine();
            string password = ReadPassword("Enter Password : ");
            Console.WriteLine("\nWorking...\n");
            var eqsl = new Downloader(login, password, loggerFactory, null, 5, 1000, 10);
            eqsl.Download(); //Download ADIFs and e-QSLs 
        }

         static string ReadPassword(string message)
            {
                Console.WriteLine(message);
                string password = "";
                while (true)
                {
                    ConsoleKeyInfo key = Console.ReadKey(true);
                    switch (key.Key)
                    {
                        case ConsoleKey.Escape:
                            return null;
                        case ConsoleKey.Enter:
                            return password;
                        case ConsoleKey.Backspace:
                            password = password.Substring(0, (password.Length - 1));
                            Console.Write("\b \b");
                            break;
                        default:
                            password += key.KeyChar;
                            Console.Write("*");
                            break;
                    }
                }
            }
    }

}
