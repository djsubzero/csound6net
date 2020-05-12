using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
/*
 * C S O U N D 6 N E T
 * Dot Net Wrappers for building C#/VB hosts for Csound 6 via the Csound API.
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
    /// Encapsulates a csound utility to be run as a separate task from csound itself.
    /// Execution is via a separate .net Process class rather than via csoundRunUtility in order
    /// to capture output and provide for cancellation.
    /// The the csoundRunUtility method is a lowest-common-denominator implementation
    /// while .net provides richer process control.
    /// The RunAsync method is meant to mimic the csound equivalent with additional services.
    /// </summary>
    public class Csound6Utility : ICloneable
    {
        public const string PHASE_VOCODER_ANALYSIS = "pvanal";
        public const string LINEAR_PREDICTIVE_ANALYSIS = "lpanal";
        public const string HETERODYNE_FILTER_ANALYSIS = "hetro";
        public const string IMPULSE_RESPONSE_FOURIER_ANALYSIS = "cvanal";
        public const string ATS_ANALYSIS = "atsa";
        public const string EXPORT_HETERODYNE_DATA = "het_export";
        public const string IMPORT_HETERODYNE_DATA = "het_import";
        public const string EXPORT_PHASE_VOCODER_DATA = "pv_export";
        public const string IMPORT_PHASE_VOCODER_DATA = "pv_import";
        public const string SHOW_PHASE_VOCODER_DATA = "pvlook";
        public const string CONVERT_SDIF_TO_ADSYN = "sdif2ad";
        public const string CONVERT_SAMPLE_RATE = "srconv";
        public const string EXTRACT_ENVELOPE = "envext";
        public const string EXTRACT_SAMPLES = "extractor";
        public const string SCALE_AMPLITUDE = "scale"; 
        public const string ENCODE_BINARY_INTO_BASE64 = "csb64enc";
        public const string MAKE_CSD_FILE = "makecsd";
        public const string MIXER = "mixer";
        public const string SHOW_SOUNDFILE_METADATA = "sndinfo";

        private string m_name;    
        protected Csound6Net m_csound;
        private ICollection<string> m_flags;

        public Csound6Utility(string name, Csound6Net csound)
        {
            m_name = name;
            m_csound = csound;
        }

        public Csound6Utility(string name, FileInfo input, FileInfo output, Csound6Net csound)
            :this(name, csound)
        {
            InputFile = input;
            OutputFile = output;
        }

        public virtual object Clone()
        {
            return new Csound6Utility(Name, InputFile, OutputFile, m_csound);
        }

        /// <summary>
        /// The description of the utility as defined to csound
        /// </summary>
        public string Description
        {
            get
            {
                return (m_csound != null)
                    ? Csound6Net.CharPtr2String(NativeMethods.csoundGetUtilityDescription(m_csound.Engine, Name))
                    : string.Empty;
            }
        }

        public void AddFlag(string flag)
        {
            if (Flags == null) Flags = new List<string>();
            Flags.Add(flag);
        }

        /// <summary>
        /// Provides the current utility flags as understood by subclasses, or if specifically
        /// set via this property, those set values regardless of subclass property values.
        /// Usually, only getter is used.
        /// </summary>
        public ICollection<string> Flags
        {
            get { return (m_flags != null) ? m_flags : CreateUtilityFlags(null); }
            set { m_flags = value; }
        }

        /// <summary>
        /// Produces a list of arguments by scanning Flags and input/output file specs
        /// as appropriate for a given utility subclass.
        /// </summary>
        /// <returns>a collection of arguments suitable for presenting to the RunAsync method</returns>
        protected ICollection<string> GetUtilityArgs()
        {
            ICollection<string> args = new List<string>(Flags);
            string infile = null;
            string outfile = null;
            if ((InputFile != null) && InputFile.Exists)
            {
                infile = UsesSameDirectory(InputFile, SoundFileDirectory)
                    ? InputFile.Name 
                    : BridgeToCpInvoke.wGetShortPathName(InputFile.FullName);
            }
            if (OutputFile != null)
            {//use -o?, insert before input, use name or shortpath: SADIR or working
                if (OutputFile.Exists) OutputFile.Delete();
                outfile = BridgeToCpInvoke.wGetShortPathName(OutputFile.FullName);
            }
            if (!string.IsNullOrWhiteSpace(outfile))
            {

            }
            if (!string.IsNullOrWhiteSpace(infile)) args.Add(infile);

            return args;
        }

        /// <summary>
        /// Method for subclassed utilities to override to generate a flag set based upon
        /// the current values of its properties.
        /// </summary>
        /// <param name="flags"></param>
        /// <returns>default implementation returns an empty collection of no flags</returns>
        protected virtual ICollection<string> CreateUtilityFlags(ICollection<string> flags)
        {
            return (flags != null) ? flags : new List<string>(); 
        }

        /// <summary>
        /// Indicates whether a utility uses a "-o" flag to indicate an output file or whether
        /// the output file is specified directly (or omitted altogether).
        /// Subclasses using "-o" should override to return true.
        /// Default implementation returns false - no -o flag used.
        /// </summary>
        public virtual bool HasOutputFlag { get {return false;}}

        /// <summary>
        /// The file to process
        /// </summary>
        public FileInfo InputFile;

        /// <summary>
        /// The file into which a utility should send its output
        /// </summary>
        public FileInfo OutputFile;

        /// <summary>
        /// The name of the utility as known at the command line and executable using the PATH environment variable.
        /// </summary>
        public string Name { get { return m_name; } }

        public string AnalysisDirectory { get { return m_csound.GetEnv("SADIR"); } }

        public string SoundFileDirectory { get { return m_csound.GetEnv("SFDIR"); } }

        public string SoundSourceDirectory { get { return m_csound.GetEnv("SSDIR"); } }

        /// <summary>
        /// Runs the represented utility as a separate threaded process using the Name property and GetUtilityArgs method.
        /// </summary>
        /// <param name="logger">handler to receive stdout as emitted by utility</param>
        /// <param name="cancel">token for cancelling utility before done (can be CancellationToken.None, or given timeout)</param>
        /// <returns></returns>
        public async Task<int> RunAsync(Csound6MessageEventHandler logger, CancellationToken cancel)
        {
            var process = new CsoundExternalProcess(Name, GetUtilityArgs());
            if (logger != null) process.MessageCallback += logger;
            long done = await process.RunAsync(cancel);
            return (int)done;
        }

        public override string ToString()
        {
            StringBuilder s = new StringBuilder(Name);
            var args = GetUtilityArgs();
            if (args.Count > 0)
            {
                s.Append('\t');
                s.Append(string.Join(" ", args.ToArray<string>()));
            }
           return s.ToString();
        }

        //Determines whether a complete path is superfluous.
        private bool UsesSameDirectory(FileInfo file, string dir)
        {
            bool same = false;
            if (!string.IsNullOrWhiteSpace(dir))
            {
                same = BridgeToCpInvoke.wGetShortPathName(file.DirectoryName).ToLower().Equals(BridgeToCpInvoke.wGetShortPathName(dir).ToLower());
            }
            return same;
        }

        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr csoundGetUtilityDescription([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string utilName);

            //Not used by object wrapper in favor of CsoundExternalProcess which uses .net's Process class for finer control of threaded external processes
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int csoundRunUtility([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name, [In] int argc, [In] string[] argv);
        }
    }


}
