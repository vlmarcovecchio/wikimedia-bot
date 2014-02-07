//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena <benapetr@gmail.com>

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace wmib
{
    internal class Program
    {
		/// <summary>
		/// Log the specified message
		/// </summary>
		/// <param name='msg'>
		/// Message that you want to log.
		/// </param>
		/// <param name='warn'>
		/// If this is true the message will be classified as a warning.
		/// </param>
		[Obsolete]
        public static bool Log(string msg, bool warn = false)
        {
            Logging.Write(msg, warn);
            return true;
        }

		/// <summary>
		/// Writes the message immediately to console with no thread sync
		/// </summary>
		/// <returns>
		/// The now.
		/// </returns>
		/// <param name='msg'>
		/// Message that you want to log.
		/// </param>
		/// <param name='warn'>
		/// If this is true the message will be classified as a warning.
		/// </param>
        [Obsolete]
		public static bool WriteNow(string msg, bool warn = false)
        {
            Logging.Display(DateTime.Now, msg, warn);
            return true;
        }

		/// <summary>
		/// Copy the selected file to a temporary file name
		/// 
		/// this function is used mostly for restore of corrupted data,
		/// so that the corrupted version of file can be stored in /tmp
		/// for debugging
		/// </summary>
		/// <param name='file'>
		/// File
		/// </param>
        public static bool Temp(string file)
        {
            string path = System.IO.Path.GetTempFileName();
            System.IO.File.Copy(file, path, true);
            if (System.IO.File.Exists(path))
            {
                Syslog.Log("Unfinished transaction from " + file + " was stored as " + path);
                return true;
            }
            return false;
        }

		/// <summary>
		/// This is used to handle UNIX signals
		/// </summary>
		/// <param name='sender'>
		/// Sender.
		/// </param>
		/// <param name='args'>
		/// Arguments.
		/// </param>
        protected static void myHandler(object sender, ConsoleCancelEventArgs args)
        {
            Syslog.WriteNow("SIGINT");
            Syslog.WriteNow("Shutting down");
            try
            {
                Core.Kill();
            }
            catch (Exception)
            {
                Core.irc.Disconnect();
                Core._Status = Core.Status.ShuttingDown;
            }
            Syslog.WriteNow("Terminated");
        }

		/// <summary>
		/// Processes the terminal parameters
		/// </summary>
		/// <param name='gs'>
		/// Gs.
		/// </param>
        private static void ProcessVerbosity(string[] gs)
        {
            foreach (string item in gs)
            {
                if (item == "--nocolors")
                {
                    Configuration.System.Colors = false;
                    continue;
                }
                if (item == "--traffic" )
                {
                    Configuration.Network.Logging = true;
                }
                if (item == "-h" || item == "--help")
                {
                    Console.WriteLine("This is a wikimedia bot binary\n\n" +
                        "Parameters:\n" +
                        "    --nocolors: Disable colors in system logs\n" +
                        "    -h [--help]: Display help\n" +
                        "    --traffic: Enable traffic logs\n" +
                        "    -v: Increases verbosity\n\n" +
                        "This software is open source, licensed under GPLv3");
                    Environment.Exit(0);
                }
                if (item.StartsWith("-v"))
                {
                    foreach (char x in item)
                    {
                        if (x == 'v')
                        {
                            Configuration.System.SelectedVerbosity++;
                        }
                    }
                }
            }
            if (Configuration.System.SelectedVerbosity >= 1)
            {
                Syslog.DebugLog("System verbosity: " + Configuration.System.SelectedVerbosity.ToString());
            }
        }

		/// <summary>
		/// The entry point of the program, where the program control starts and ends.
		/// </summary>
		/// <param name='args'>
		/// The command-line arguments.
		/// </param>
        private static void Main(string[] args)
        {
            try
            {
                Configuration.System.UpTime = DateTime.Now;
                Thread logger = new Thread(Logging.Exec);
                Core.domain = AppDomain.CurrentDomain;
                ProcessVerbosity(args);
                Syslog.WriteNow(Configuration.Version);
                Syslog.WriteNow("Loading...");
                logger.Start();
                Console.CancelKeyPress += myHandler;
                messages.LoadLD();
                if (Configuration.Load() != 0)
                {
                    Syslog.WriteNow("Error while loading the config file, exiting", true);
                    Environment.Exit(-2);
                    return;
                }
                Terminal.Init();
                Core.Help.CreateHelp();
                Core.WriterThread = new System.Threading.Thread(StorageWriter.Exec);
                Core.WriterThread.Start();
                if (Core.DatabaseServerIsAvailable)
                {
                    Syslog.Log("Initializing MySQL");
                    Core.DB = new WMIBMySQL();
                }
                Syslog.Log("Loading modules");
                Core.SearchMods();
                IRCTrust.Global();
                Syslog.Log("Connecting");
                Core.Connect();
            }
            catch (Exception fatal)
            {
                Syslog.WriteNow("ERROR: bot crashed, bellow is debugging information");
                Console.WriteLine("------------------------------------------------------------------------");
                Console.WriteLine("Description: " + fatal.Message);
                Console.WriteLine("Stack trace: " + fatal.StackTrace);
                Environment.Exit(-2);
            }
        }
    }
}
