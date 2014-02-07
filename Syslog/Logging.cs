﻿//This program is free software: you can redistribute it and/or modify
//it under the terms of the GNU General Public License as published by
//the Free Software Foundation, either version 3 of the License, or
//(at your option) any later version.

//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.

// Created by Petr Bena <benapetr@gmail.com>

using System;
using System.Threading;
using System.Collections.Generic;
using System.Text;

namespace wmib
{
	public class Syslog
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
        public static bool Log(string Message, bool Warning = false)
        {
            Logging.Write(Message, Warning);
			SystemHooks.SystemLog(Message, Warning);
            return true;
        }

		/// <summary>
		/// Log the specified message
		/// </summary>
		/// <param name='msg'>
		/// Message that you want to log.
		/// </param>
        public static bool WarningLog(string Message)
        {
            Syslog.Log(Message, true);
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
		public static bool WriteNow(string Message, bool Warning = false)
        {
            Logging.Display(DateTime.Now, Message, Warning);
			SystemHooks.SystemLog(Message, Warning);
            return true;
        }

		/// <summary>
        /// Debug log
        /// </summary>
        /// <param name="text"></param>
        /// <param name="verbosity"></param>
        public static void DebugLog(string Message, int Verbosity = 1)
        {
            if (Configuration.System.SelectedVerbosity >= Verbosity)
            {
                Syslog.Log("DEBUG <" + Verbosity.ToString() + ">: " + Message);
            }
        }

        /// <summary>
        /// Debug log
        /// </summary>
        /// <param name="text"></param>
        /// <param name="verbosity"></param>
        public static void DebugWrite(string Message, int Verbosity = 1)
        {
            if (Configuration.System.SelectedVerbosity >= Verbosity)
            {
                Syslog.WriteNow("DEBUG <" + Verbosity.ToString() + ">: " + Message);
            }
        }
	}

    /// <summary>
    /// This class is handling the system terminal, it writes to it using a single thread so that
    /// multiple threads do not break their messages as they write to console in same moment
    /// </summary>
    public class Logging
    {
        /// <summary>
        /// Message that needs to be printed
        /// </summary>
        public class Message
        {
            /// <summary>
            /// Time
            /// </summary>
            public DateTime Time;
            /// <summary>
            /// Message
            /// </summary>
            public string Text;
            /// <summary>
            /// Is a warning
            /// </summary>
            public bool Warning;

            /// <summary>
            /// Creates a new instance of message
            /// </summary>
            /// <param name="text"></param>
            /// <param name="time"></param>
            /// <param name="warning"></param>
            public Message(string text, DateTime time, bool warning)
            {
                Text = text;
                Time = time;
                Warning = warning;
            }
        }

        /// <summary>
        /// Database of messages that needs to be written
        /// </summary>
        public static List<Message> messages = new List<Message>();

        /// <summary>
        /// Write a message to terminal
        /// </summary>
        /// <param name="Message"></param>
        /// <param name="warning"></param>
        public static void Write(string Message, bool warning)
        {
            Message message = new Message(Message, DateTime.Now, warning);
            lock (messages)
            {
                messages.Add(message);
            }
        }

        /// <summary>
        /// Write a message
        /// </summary>
        /// <param name="time"></param>
        /// <param name="Message"></param>
        /// <param name="Warning"></param>
        public static void Display(DateTime time, string Message, bool Warning)
        {
            if (Configuration.System.Colors)
            {
                Console.ForegroundColor = ConsoleColor.Red;
            }
            if (Warning)
            {
                if (Configuration.System.Colors)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                }
                Console.Write("LOG (WARNING)");
            }
            else
            {
                Console.Write("LOG ");
            }
            if (Configuration.System.Colors)
            {
                Console.ForegroundColor = ConsoleColor.Green;
            }
            Console.Write("[{0}]", time.ToString());
            if (Configuration.System.Colors)
            {
                Console.ResetColor();
            }
            Console.WriteLine(": " + Message);
        }

        /// <summary>
        /// Execute thread
        /// </summary>
        public static void Exec()
        {
            try
            {
                while (true)
                {
                    if (messages.Count > 0)
                    {
                        List<Message> priv = new List<Message>();
                        lock (messages)
                        {
                            priv.AddRange(messages);
                            messages.Clear();
                        }
                        foreach (Message message in priv)
                        {
                            Display(message.Time, message.Text, message.Warning);
                        }
                    }
                    Thread.Sleep(100);
                }
            }
            catch (ThreadAbortException)
            {
                return;
            }
        }
    }
}
