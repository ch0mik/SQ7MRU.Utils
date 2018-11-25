using Microsoft.Extensions.Logging;
using SQ7MRU.Utils;
using System;

namespace SampleApp
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            ILoggerFactory loggerFactory = new LoggerFactory().AddConsole(LogLevel.Trace);

            //eQSL.cc
            eQSL_Example(loggerFactory);

            //EPC-MC.eu
            EPC_Example(loggerFactory);

            //hrdlog.net
            HRD_Example(loggerFactory);
        }

        private static void eQSL_Example(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("SQ7MRU eQSL Downloader\n");
            Console.WriteLine("Enter Login to eqsl.cc : ");
            string login = Console.ReadLine();
            string password = ReadPassword("Enter Password : ");
            Console.WriteLine("\nWorking...\n");
            var eqsl = new Downloader(login, password, loggerFactory, null, 5, 1000, 10);
            eqsl.Download(); //Download ADIFs and e-QSLs
        }

        private static void EPC_Example(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("SQ7MRU EPC Downloader\n");
            Console.WriteLine("Enter Login to epc-mc.eu : ");
            string login = Console.ReadLine();
            string password = ReadPassword("Enter Password : ");
            Console.WriteLine("\nWorking...\n");
            var epc = new EPC(login, password, loggerFactory, null);
            epc.Download(); //Download certs
        }

        private static void HRD_Example(ILoggerFactory loggerFactory)
        {
            Console.WriteLine("SQ7MRU iQSL Downloader\n");
            Console.WriteLine("Enter Login to hrdlog.net : ");
            string login = Console.ReadLine();
            Console.WriteLine("\nWorking...\n");
            var iQSL = new iQSL(login, loggerFactory, null);
            iQSL.Download(); //Download iQSLs
        }

        private static string ReadPassword(string message)
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