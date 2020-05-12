using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

/*
 * C S O U N D 6 N E T
 * Dot Net Wrappers for building C#/VB hosts for Csound 6 via the Csound API
 * and is licensed under the same terms and disclaimers as Csound described below.
 * Copyright (C) 2013 Richard Henninger
 *
 * C S O U N D
 *
 * An auto-extensible system for making music on computers
 * by means of software alone.
 *
 * Copyright (C) 2001-2013 Michael Gogins, Matt Ingalls, John D. Ramsdell,
 *                         John P. ffitch, Istvan Varga, Victor Lazzarini,
 *                         Andres Cabrera and Steven Yi
 *
 * This software is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * This software is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this software; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 *
 */

namespace csound6netlib
{
    /// <summary>
    /// Encapsulates a .net Process object to faciltate background calling of csound
    /// utilities and the running of operating system commands in the background.
    /// Standardizes options for running as async/await background process, providing cancellation
    /// and timeout services as well as capturing process output (stdout and stderr) for display in a gui.
    /// Use of this is simpler than csound equivalents: null item at the end of arguments is unnecessary (but ok to include)
    /// and the program name can be the first element in the argument set or not.
    /// The constructor signatures represent the most convenient way to provide arguments from different
    /// csound scenarios.
    /// Since this object creates a fresh Process class when RunAsync is called, this actual instance may 
    /// reused as often as desired.
    /// </summary>
    public class CsoundExternalProcess
    {
        private Process m_process;
        private ProcessStartInfo m_startinfo;
        private bool m_running;

        /// <summary>
        /// Sets standard defaults for running a csound utility/command as a background process.
        /// No program name or argument list is set at this point.
        /// </summary>
        public CsoundExternalProcess()
        {
            m_startinfo = new ProcessStartInfo();
            m_startinfo.CreateNoWindow = true;
            m_startinfo.UseShellExecute = false;
            m_startinfo.RedirectStandardOutput = true;
            m_startinfo.RedirectStandardError = true;
            m_running = false;
        }

        /// <summary>
        /// Constructs a process parsing the provided command execution string into
        /// the program name followed by its arguments.
        /// </summary>
        /// <param name="command"></param>
        public CsoundExternalProcess(string command)
            : this(new List<string>(command.Split(new char[] {' ', '\t'})))
        {
        }

        /// <summary>
        /// Constructs a process extracting the program name from the first command argument
        /// and the arguments from the remaining arguments.
        /// This signature is used by the Csound6Net's RunAsync method to mimic csoundRunCommand's signature.
        /// </summary>
        /// <param name="command"></param>
        public CsoundExternalProcess(ICollection<string> command)
           : this()
        {
            var list = command.ToList<string>();
            ProgramName = list[0];
            list.RemoveAt(0);
            Arguments = list;
        }

        /// <summary>
        /// Constructs a process using the provided program name and argument list.
        /// This signature is used by csound utilities to mimic csoundRunUtility's signature.
        /// </summary>
        /// <param name="name">program name to execute</param>
        /// <param name="args">list of arguments or flags to be presented to the process (trailing null member is unnecessary)</param>
        public CsoundExternalProcess(string name, ICollection<string> args)
            :this()
        {
            ProgramName = name;
            Arguments = args;
        }

        /// <summary>
        /// The name or path of the program to execute.
        /// </summary>
        public string ProgramName;

        /// <summary>
        /// The list of individual flags and file names which will follo
        /// </summary>
        public ICollection<string> Arguments;

        /// <summary>
        /// Run the process represented by this object as a cancellable background process providing
        /// any stdout/stderr as MessageCallback events.
        /// Process fully background in a separate thread having no screen or window.
        /// </summary>
        /// <param name="cancel">cancellation token with optional timeout</param>
        /// <returns>the return (exit) value from the process represented by this object</returns>
        /// <exception cref="System.OperationCancelledException">thrown when cancelation token requests cancellation</exception>
        /// <exception cref="System.InvalidOperationException">thrown if program name is missing or not resolvable</exception>
        public async Task<int> RunAsync(CancellationToken cancel)
        {
            return await Task<int>.Run(() =>
                {
                    int result = -1;
                    //Normalize name and arguments: make sure name is removed from arguments (none provided or if equals name)
                    IList<string> args = Arguments.ToList<string>();
                    if ((args.Count > 0) && (string.IsNullOrWhiteSpace(ProgramName) || ProgramName.Equals(args[0])))
                    {
                        ProgramName = args[0];
                        args.RemoveAt(0);
                    }
                    //Normalize arguments by removing the final null argument, if present.
                    if ((args.Count > 0) && string.IsNullOrWhiteSpace(args[args.Count - 1]))
                    {
                        args.RemoveAt(args.Count - 1);
                    }
                    //Set options and events for the process before it starts
                    m_startinfo.FileName = ProgramName;
                    m_startinfo.Arguments = string.Join(" ", args);
                    m_process = new Process();
                    m_process.StartInfo = m_startinfo;
                    m_process.EnableRaisingEvents = true;
                    m_process.Exited += ProcessExitEventHandler;
                    m_process.OutputDataReceived += StdOutEventHandler;
                    m_process.ErrorDataReceived += StdErrEventHandler;
                    //Start process and begin async processing
                    bool ok = m_process.Start();
                    m_process.BeginOutputReadLine();
                    m_process.BeginErrorReadLine();
                    m_running = true;
                    while (m_running) // will be true until Exited event is thrown.
                    {
                        if (cancel.IsCancellationRequested) //if cancelled, shut down output and process itself
                        {
                            m_process.CancelErrorRead();
                            m_process.CancelOutputRead();
                            m_process.Kill();
                            m_process.WaitForExit();
                            m_process.Dispose();
                            m_process = null;
                            m_running = false;
                            cancel.ThrowIfCancellationRequested(); // and return via exception once safe to do so.
                        } else Thread.Sleep(50); //else sleep awile and check again for cancellation
                    }
                    result = m_process.ExitCode; //process ended normally, so provide return code from the csound code.
                    m_process.Dispose();//indicate that it is ok to release OS resources
                    m_process = null;
                    m_running = false; //should be false by here, but just in case...
                    return result;
                });
        }

        /// <summary>
        /// An event which fires each time the process outputs either a stdout 
        /// or stderr line of text.
        /// </summary>
        public event Csound6MessageEventHandler MessageCallback;

        /// <summary>
        /// The string representation of the command line that will be presented to the
        /// background process represented by this class.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            StringBuilder s = new StringBuilder(ProgramName);
            if (Arguments.Count > 0)
            {
                s.Append('\t');
                s.Append(string.Join(" ", Arguments));
            }
            return s.ToString();
        }

        //Helper method for marking when background process concludes.
        private void ProcessExitEventHandler(object sender, EventArgs e)
        {
            m_running = false;
        }

        //Helper method for trapping stderr output
        private void StdErrEventHandler(object sender, DataReceivedEventArgs e)
        {
            var msg = new Csound6MessageEventArgs(string.Format("{0}{1}", e.Data, Console.Error.NewLine));
            msg.Type = MessageType.Error;
            var handler = MessageCallback;
            if (handler != null)
            {
                handler(this, msg);
            }
        }

        //Helper method for trapping stdout output
        private void StdOutEventHandler(object sender, DataReceivedEventArgs e)
        {
            var msg = new Csound6MessageEventArgs(string.Format("{0}{1}", e.Data, Console.Out.NewLine));
            msg.Type = MessageType.Default;
            var handler = MessageCallback;
            if (handler != null)
            {
                handler(this, msg);
            }
        }

    }
}
