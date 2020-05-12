using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using csound6netlib;

namespace csound6net.console
{
    /// <summary>
    /// Command line driven version of Csound 6 like the distribution frontend csound_main.c.
    /// Provides an example of a simple host frontend (like csound_main.c provides for "c")
    /// for wrapping csound 6 in a .net host using csound6netlib.
    /// Includes honoring -O, --logfile= flags as well as ^c console cancel event as does the regular csound console program.
    /// Use this program from the command line just like you would use csound from a command shell - flags and all
    /// (well, of course you'd use the name of this application rather than csound as the command).
    /// </summary>
    public class Csound6Console
    {
        
        // Logger to be used on for a given run of this program.
        static TextWriter _writer = null;

        /// <summary>
        /// Handler for receiving csound messages:
        /// </summary>
        /// <param name="sender">will be the Csound6Net instance fireing the event: cast accordingly if access to csound is needed</param>
        /// <param name="args">Message contents including message type and styling info if provided.</param>
        public static void LoggingMessageHandler(object sender, Csound6MessageEventArgs args)
        {
            if (_writer != null)
            {
                 _writer.Write(args.Message.Replace("\n", _writer.NewLine));
            }
        }

        /// <summary>
        /// Extracts the pathname expected when a log file request is made on a command line.
        /// Logfile requests are indicated by either "-O" or "--logfile=" followed by the pathname.
        /// Stdout is the default if no logger is indicated or the logger is named "stdout".
        /// </summary>
        /// <remarks>
        /// The shell must provide for this as internally, csound ignores this flag.
        /// Whitespace can exist after the switch, but is not preferred syntax.
        /// The string "NULL" can be used to shut off logging and stdout altogether.
        /// </remarks>
        /// <param name="args">parsed array of command line arguments as supplied by the windows shell</param>
        /// <returns>a proposed file path, null or "stdout" if console output is expected, "NULL" to surpress all messages</returns>
        public static string FindLoggingPath(string[] args)
        {
            string path = string.Empty;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].StartsWith("-O") || args[i].StartsWith("--logfile="))
                {
                    int start = args[i].StartsWith("-O") ? 2 : 10;
                    if (args[i].Length > start)
                    {
                        path = args[i].Substring(start);
                    }
                    else if (i < args.Length)
                    {
                        path = args[i + 1];
                    }
                    break;
                }
            }
            return path;
        }

        /// <summary>
        /// Entry point to this command line version of Csound using a .net wrapper.
        /// It demonstrates how to emulate csound_main.c command line example provided
        /// with csound.
        /// Use exactly as one would use csound from a command prompt using all the same flags
        /// (well, the command itself would be different: "csound6net" instead of "csound".
        /// </summary>
        /// <param name="args">Parsed command line arguments </param>
        /// <returns>0 if successful, a negative csound error code if failed</returns>
        public static int Main(string[] args)
        {

            //Determine which log output we want prior to creating Csound6Net
            //so we can capture every csound message including initialization chatter.
            string path = FindLoggingPath(args);
            if ("NULL".Equals(path.ToUpper()))
                _writer = null;
            else
            {
                if (string.IsNullOrWhiteSpace(path) || "stdout".Equals(path.ToLower()))
                    _writer =  Console.Out;
                else {
                    if (File.Exists(path)) File.Delete(path);
                    _writer = new StreamWriter(path, true);
                }
            }

            //Main Csound loop with ^c intercept
            CsoundStatus result;
            using (var csound = new Csound6Net(LoggingMessageHandler))
            {
                bool playing = true;//flag to capture ^c events and exit gracefully
                Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs e)
                                            {   e.Cancel = true;
                                                playing = false;
                                            };

                result = playing ? csound.Compile(args) : CsoundStatus.TerminatedBySignal;
                
                if (result == CsoundStatus.Success)
                {
                    while(playing && !csound.PerformKsmps()); //continue output until score is done or ^c is encountered
                }
            }

            //When done, shut down any logging that might be active
            if (_writer != null) {
                _writer.Flush();
                if (_writer != Console.Out)_writer.Dispose();
            }
            return (((int)result) >= 0) ? 0 : (int)result;
        }
    }
}
