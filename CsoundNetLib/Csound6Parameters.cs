using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
/*
 * C S O U N D 6 N E T
 * Dot Net Wrappers for building C#/VB hosts for Csound 6 via the Csound API
 * and is licensed under the same terms and disclaimers as Csound indicates below.
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
    /// Represents parameter values which can be passed as flags to an instance of Csound6Net
    /// in order to influence how a performance will be executed.
    /// Csound exposes these parameters via the CSOUND_PARAMS structure which this class
    /// exposes via idiomatic C# property values.
    /// All of the values represented by this class are setable by input arguments from a command line.
    /// Some flags can only be turned on from the command line.
    /// This interface allows them to be toggled if one resends the CSOUND_PARAMS structure.
    /// The times this class resends the whole structure rather than one at a time whenever the command line is one-way only.
    /// </summary>
    [CLSCompliant(true)]
    public class Csound6Parameters
    {

        /**
         * \addtogroup PARAMETERS
         * @{
         */

        /// <summary>
        /// Static implementation of SetOption to allow parameter setting
        /// without instantiating a Csound6Parameters object just to set a single parameter.
        /// </summary>
        /// <param name="csound">the current running instance of csound via Csound6Net</param>
        /// <param name="option">a string containing a single command line parameter spec (without spaces)</param>
        /// <returns>Success if parameter spec was understood and used.  Other codes imply failure.</returns>
        public static CsoundStatus SetOption(Csound6Net csound, string option)
        {
            return Csound6Net.Int2StatusEnum(NativeMethods.csoundSetOption(csound.Engine, option));
        }

                    /******************************************************************/

        private CSOUND_PARAMS m_oparms;
        private Csound6Net    m_csound;

        /// <summary>
        /// Standard constructor for creating a parameter set.
        /// Usually executed via the GetParameters() method in the main csound object: Csound6Net
        /// and thus not usually used directly in a host program.
        /// </summary>
        /// <param name="csound"></param>
        public Csound6Parameters(Csound6Net csound) 
        {
            m_csound = csound;
            m_oparms = new CSOUND_PARAMS();
            GetParams(m_oparms);
        }

        /// <summary>
        /// Updates this class's internal copy of CSOUND_PARAMS to match csound's own internal values.
        /// These can get out of phase after calls to SetOptions.
        /// </summary>
        public void RefreshParams()
        {
            GetParams(m_oparms);
        }

        /// <summary>
        /// Fills in a provided raw CSOUND_PARAMS object with csounds current parameter settings.
        /// This method is used internally to manage this class and is not expected to be used directly by a host program.
        /// </summary>
        /// <param name="oparms">a CSOUND_PARAMS structure to be filled in by csound</param>
        /// <returns>The same parameter structure that was provided but filled in with csounds current internal contents</returns>
        public CSOUND_PARAMS GetParams(CSOUND_PARAMS oparms)
        {
            NativeMethods.csoundGetParams(m_csound.Engine, oparms);
            return oparms;
        }

        /// <summary>
        /// Transfers the contents of the provided raw CSOUND_PARAMS object into csound's 
        /// internal data structues (chiefly its OPARMS structure).
        /// This method is used internally to manage this class and is not expected to be used directly by a host program.
        /// Most values are used and reflected in CSOUND_PARAMS.
        /// Internally to csound, as of release 6.0.0, Heartbeat and IsComputingOpcodeWeights are ignored
        /// and IsUsingCsdLineCounts can only be set and never reset once set.
        /// </summary>
        /// <param name="parms">a </param>
        public void SetParams(CSOUND_PARAMS parms)
        {
            NativeMethods.csoundSetParams(m_csound.Engine, parms);
        }

        /// <summary>
        /// Presents on option to csound's internal argdecode routine as if it were a segment
        /// of a csound command line argument list.
        /// This method is intended to be used internally to manage this class,
        /// but it could be used in a host program to set flags which are not implemented in CSOUND_PARAMS
        /// independently of command line arguments or CsOptions in a csd file. 
        /// The syntax is mostly the same as for the command line including use of dashes, double dashes
        /// and dash+plus as prefixes.
        /// Unlike command line arguments, no spaces are allowed and bunching of arguments into a single
        /// dash (like -dm127) is not advisable.
        /// </summary>
        /// <param name="option">A string containing a single command line parameter</param>
        /// <returns>CsoundSuccess if the option string is accepted without error</returns>
        public CsoundStatus SetOption(string option)
        {
            return Csound6Net.Int2StatusEnum(NativeMethods.csoundSetOption(m_csound.Engine, option));
        }

        /// <summary>
        /// Indicates or sets whether Csound is in "verbose" mode for debugging.
        /// This is the same property as is exposed in the Csound6Net class.
        /// Defaults to false.
        /// </summary>
        public bool IsDebugMode {
            get { return (m_oparms.debug_mode != 0); }
            set { m_oparms.debug_mode = value ? 1 : 0;
                  SetParams(m_oparms);
                }
        }

        /// <summary>
        /// Sets or indicates the size of the software buffer being sent to sound file
        /// management routines.  It is recommended that it be a multiple of ksmps.
        /// Defaults to zero - no override; internally, csound's default for this depends upon the platform.
        /// Usually this is set in the CsOptions section of a csd file or in csoundrc file.
        /// </summary>
        public int SoftwareBufferFrames {
            get { return m_oparms.buffer_frames; }
            set { m_oparms.buffer_frames = value;
                  SetOption(string.Format("-b{0}", value)); 
                }
        }

        /// <summary>
        /// Sets or indicates the size of the hardware buffer to be sent to an
        /// output device like a dac.
        /// It is recommended that it be a multiple of the SoftwareBufferFrames property.
        /// Defaults to zero - no override; internally, csound's default for this depends upon the platform.
        /// Usually this is set in the CsOptions section of a csd file or in csoundrc file.
        /// </summary>
        public int HardwareBufferFrames {
            get { return m_oparms.hardware_buffer_frames; }
            set {   m_oparms.hardware_buffer_frames = value;
                    SetOption(string.Format("-B{0}", value));
                }
        }

        /// <summary>
        /// Controls whether the stdout patter from performing a score will display
        /// pictures of function tables as they are encountered.
        /// Defaults to true.
        /// If false, no graphs are output regardless of the settings of IsUsingAsciiGraphs or IsUsingPostscriptGraphs
        /// </summary>
        public bool IsDisplayingGraphs {
            get { return (m_oparms.displays != 0); }
            set {   m_oparms.displays = value ? 1 : 0;
                   SetOption( value ? "--displays" : "-d");
            }
        }

        /// <summary>
        /// If function tables are being output, true directs that output to stdout,
        /// or the target of the MessageCallback, as ascii characters.
        /// If false, no ascii output is generated.  If IsDisplayingGraphs is false, this parameter is ignored.
        /// Defaults to false.
        /// </summary>
        public bool IsUsingAsciiGraphs {
            get { return (m_oparms.ascii_graphs != 0); }
            set {   m_oparms.ascii_graphs = value ? 1 : 0;
                    SetParams(m_oparms);
                }
        }

        /// <summary>
        /// If function tables are being output, true directs that output to a postscript file.
        /// If false, no postscript output is generated to a file.
        /// If IsDisplayingGraphs is false, this parameter is ignored.
        /// Defaults to true.
        /// </summary>
        public bool IsUsingPostscriptGraphs {
            get { return (m_oparms.postscript_graphs != 0); }
            set {   m_oparms.postscript_graphs = value ? 1 : 0;
                    SetParams(m_oparms);
            }
        }

        /// <summary>
        /// Controls the verbosity of csound's stdout patter.
        /// Supply the or'd combination of the MessageLevel enum members that reflect what 
        /// patter you want included.
        /// Defaults to 135: Amplitude | Range | Warnings | Benchmark.
        /// In other words: MessageLevel.Default.
        /// </summary>
        public MessageLevel MessageLevel {
            get { return (MessageLevel)m_oparms.message_level; }
            set {   m_oparms.message_level = (int)value;
                    SetOption(string.Format("-m{0}", m_oparms.message_level));
                }
        }

        /// <summary>
        /// Sets/gets the initial tempo of a csound performance overriding settings in a score file.
        /// Csound usually works in second.
        /// Defaults to 0 meaning that all tempo control, if any, is handled within a score section spec.
        /// Not specifying a tempo is like having quarternote=60:
        /// all time specs (start times and durations) are understood to be in seconds rather than in beats.
        /// </summary>
        public int Tempo { get { return m_oparms.tempo; }
            set {   m_oparms.tempo = value;
                   SetOption(string.Format("-t{0}", value));
                }
        }

        /// <summary>
        /// Controls whether csound will issue an ascii bell character at the end processing
        /// a score.
        /// Obviously this has no effect if csound is executing in a non-crt-like environment.
        /// Defaults to false.
        /// </summary>
        public bool WillBeepWhenDone
        {
            get { return (m_oparms.ring_bell != 0); }
            set
            {
                m_oparms.ring_bell = value ? 1 : 0;
                SetParams(m_oparms);
            }
        }

        /// <summary>
        /// Controls whether cscore will be used to process input files.
        /// Defaults to false.
        /// </summary>
        public bool IsUsingCscore
        {
            get { return (m_oparms.use_cscore != 0); }
            set
            {
                m_oparms.use_cscore = value ? 1 : 0;
                SetParams(m_oparms);
            }
        }

        /// <summary>
        /// Controls whether the end of a midi input file should signal the
        /// end of csound score processing.
        /// Defaults to false.
        /// </summary>
        public bool IsDoneWhenMidiDone {
            get { return (m_oparms.terminate_on_midi != 0); }
            set { m_oparms.terminate_on_midi = value ? 1 : 0;
                SetParams(m_oparms);
            }
            
        }

        /// <summary>
        /// Indicates what kind of symbol csound should use as it marks the passing
        /// of time in processing a score.
        /// In csound 6.0.0m not copied over during get/setParams, set works but is
        /// not reflected in the structure internally.
        /// </summary>
        public HeartbeatStyle Heartbeat
        {
            get { return (HeartbeatStyle)m_oparms.heartbeat; }
            set { m_oparms.heartbeat = (int)value;
            SetOption(string.Format("--heartbeat={0}", m_oparms.heartbeat));
            }
        }

        /// <summary>
        /// If true, requests deferal of loading soundfiles used in GEN01 until performance time.
        /// Presumably, false means GEN01's sound files are loaded as they are encountered in the orchestra or score parsing.
        /// Defaults to false.
        /// </summary>
        public bool IsDeferingGen01Load {
            get { return (m_oparms.defer_gen01_load != 0); }
            set { m_oparms.defer_gen01_load = value ? 1 : 0;
                  SetParams(m_oparms);
            }
        }

        /// <summary>
        /// Indicates which Pnumber in instruments should receive midi note values as they are received.
        /// Values forwarded to an instrument will be raw midi note values from 0 to 127.
        /// </summary>
        public int MidiKey2Parameter
        {
            get { return m_oparms.midi_key; }
            set
            {
                m_oparms.midi_key = value;
                SetOption(string.Format("--midi-key={0}", value));
            }
        }

        /// <summary>
        /// Indicates which Pnumber in instruments should receive midi note data (converted to hertz) as they are received.
        /// Values forwarded to an instrument will be the midi note's equivalent frequency in Hertz/cps.
        /// </summary>
        public int MidiKeyAsHertz2Parameter
        {
            get { return m_oparms.midi_key_cps; }
            set
            {
                m_oparms.midi_key_cps = value;
                SetOption(string.Format("--midi-key-cps={0}", value));
            }
        }

        /// <summary>
        /// Indicates which Pnumber in instruments should receive midi note data (converted to linear octave) as they are received.
        /// Values forwarded to an instrument will be the midi note's equivalent linear octave value.
        /// </summary>
        public int MidiKeyAsOctave2Parameter
        {
            get { return m_oparms.midi_key_oct; }
            set
            {
                m_oparms.midi_key_oct = value;
                SetOption(string.Format("--midi-key-oct={0}", value));
            }
        }

        /// <summary>
        /// Indicates which Pnumber in instruments should receive midi note data (converted to an oct.pch value) as they are received.
        /// Values forwarded to an instrument will be the midi note's equivalent frequency in csounds octave.scaledegree notation.
        /// </summary>
        public int MidiKeyAsPitch2Parameter
        {
            get { return m_oparms.midi_key_pch; }
            set
            {
                m_oparms.midi_key_pch = value;
                SetOption(string.Format("--midi-key-pch={0}", value));
            }
        }

        /// <summary>
        /// Indicates which Pnumber in instruments should receive midi velocity values as they are received.
        /// Values forwarded to an instrument will be raw velocity values from 0 to 127.
        /// </summary>
        public int MidiVelocity2Parameter
        {
            get { return m_oparms.midi_key_pch; }
            set
            {
                m_oparms.midi_velocity = value;
                SetOption(string.Format("--midi-velocity={0}", value));
            }
        }

        /// <summary>
        /// Indicates which Pnumber in instruments should receive midi velocity values as they are received.
        /// Values forwarded to an instrument will be converted from raw velocity values from 0 to 127 to a value
        /// between 0 and the current value of 0dbfs.  Not clear if that is linearly or on db scale.
        /// </summary>
        public int MidiVelocityAsAmplitude2Parameter
        {
            get { return m_oparms.midi_velocity_amp; }
            set
            {
                m_oparms.midi_velocity_amp = value;
                SetOption(string.Format("--midi-velocity-amp={0}", value));
            }
        }

        /// <summary>
        /// If false, only absolute paths or relative paths from the working directory and any specified directorys are used.
        /// If true, ORC,SCO and CSD added to given paths when looking for input spec files.
        /// Defaults to false.
        /// </summary>
        public bool IsAddingDefaultDirectories
        {
            get { return (m_oparms.no_default_paths == 0); }
            set
            {
                m_oparms.no_default_paths = value ? 0 : 1;
                SetOption(value ? "--default-paths" : "--no-default-paths");
            }
        }

        /// <summary>
        /// Sets the maximum threads that csound will use internally.  
        /// It can be set to any value up to the maximum number of cores in your cpu.
        /// It defaults to 1.
        /// This is a tuning parameter: higher numbers might degrade performance rather than enhance it
        /// depending on the nature of a partiular piece.
        /// Generally, the more fine-grained the ksmps loop is, the weaker the benefit of multicore processing.
        /// </summary>
        public int MaximumThreadCount
        {
            get { return m_oparms.number_of_threads; }
            set
            {
                m_oparms.number_of_threads = value;
                SetOption(string.Format("--num-threads={0}", value));
            }
        }

        /// <summary>
        /// When true, csound will exit before beginning its performance phase.
        /// Defaults to false.
        /// </summary>
        public bool IsSyntaxCheckOnly
        {
            get { return (m_oparms.syntax_check_only != 0); }
            set
            {
                m_oparms.syntax_check_only = value ? 1 : 0;
                SetParams(m_oparms);
            }
        }

        /// <summary>
        /// When true, line number reporting is relative to the whole file.
        /// When false, line numbering is relative to an orchestra or score segment of a file.
        /// Defaults to true.
        /// </summary>
        public bool IsUsingCsdLineCounts
        {
            get { return (m_oparms.csd_line_counts != 0); }
            set
            {
                m_oparms.csd_line_counts = value ? 1 : 0;
                SetParams(m_oparms);
            }
        }

        /// <summary>
        /// Not used by csound currently.  Ignored by csoundGetParams and csoundSetParams.
        /// </summary>
        public bool IsComputingOpcodeWeights
        {
            get { return (m_oparms.compute_weights != 0); }
            set
            {
                m_oparms.compute_weights = value ? 1 : 0;
                SetParams(m_oparms);
            }
        }

        /// <summary>
        /// Controls whether csound will use async versions of file I/O; new to csound 6.
        /// When true, csound uses asynchronous I/O; when false, blocking file I/O is used.
        /// Defaults to false.
        /// </summary>
        public bool IsRealtimeMode
        {
            get { return (m_oparms.realtime_mode != 0); }
            set { m_oparms.realtime_mode = value ? 1 : 0;
                  SetParams(m_oparms);
            }
        }

        /// <summary>
        /// Controls the sample-accurate flag that was introduced in csound 6.
        /// If false, csound works as it always has with legacy event processing each ksmps cycle.
        /// If true, events can begin mid-ksmps cycle.
        /// Defaults to false.
        /// </summary>
        public bool IsSampleAccurateMode
        {
            get { return (m_oparms.sample_accurate != 0); }
            set { m_oparms.sample_accurate = value ? 1 : 0;
                  SetParams(m_oparms);
            }
        }

        /// <summary>
        /// When non-zero, overrides sr= in an orchestra spec.
        /// Only valid if ControlRateOverride is also specified.
        /// ksmps will track the two overrides.
        /// Defaults to 0 - no override, csound defaults to 44100.
        /// </summary>
        public double SampleRateOverride {
            get { return m_oparms.sample_rate_override; }
            set { m_oparms.sample_rate_override = value;
                 SetOption(string.Format("-r{0}", value));
            }
        }

        /// <summary>
        /// When non-zero, overrides kr= in an orchestra spec. 
        /// Only valid if SampleRateOverride is also specified.
        /// ksmps will track the two overrides.
        /// Defaults to 0 - no override; internally, csound defaults to 4410 (ksmps=10) if nothing specified at all.
        /// </summary>
        public double ControlRateOverride {
            get { return m_oparms.control_rate_override; }
            set { m_oparms.control_rate_override = value;
                  SetOption(string.Format("-k{0}", value));
            }
        }

        /// <summary>
        /// When non-zero, controls how many output channels of sound will be sent to csound's output: realtime of file.
        /// This overrides the nchnls= statement in a score.
        /// Defaults to 0 - no override, csound defaults to 1 if nothing specified at all.
        /// </summary>
        public int OutputChannelCountOverride
        {
            get { return m_oparms.nchnls_override; }
            set
            {
                m_oparms.nchnls_override = value;
                SetOption(string.Format("--nchnls={0}", value));
            }
        }

        /// <summary>
        /// When non-zero, controls the number of input channels will be present in an incomming sound file.
        /// This overrides the new nchnls_i= statement, if present, in an orchestra spec.
        /// Defaults to 0 - no override; internally, csound defaults to value of nchnls if nothing specified.
        /// </summary>
        public int InputChannelCountOverride
        {
            get { return m_oparms.nchnls_i_override; }
            set
            {
                m_oparms.nchnls_i_override = value;
                SetOption(string.Format("--nchnls_i={0}", value));
            }
        }

        /// <summary>
        /// When non-zero, controls the highest number allowed for wave amplitudes before clipping occurs.
        /// This overrides the 0dbfs= statement, if present in an orchestra spec.
        /// Defaults to 0 - no override; internally, csound defaults to 32768 (maximum for 16-bit samples) if nothing specified.
        /// For floating point samples, 1.0 is usually used and expressed within the orchestra spec.
        /// Obviously, amplitude values in a score must not exceed this value whatever it is.
        /// </summary>
        public double ZeroDBOverride
        {
            get { return m_oparms.e0dbfs_override; }
            set
            {
                m_oparms.e0dbfs_override = value;
                SetOption(string.Format("--0dbfs={0}", value));
            }
        }

        /**
         * @}
         */

        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, BestFitMapping=false, ThrowOnUnmappableChar=true)]
            internal static extern int csoundSetOption([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] String option);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundGetParams(IntPtr csound, [Out, MarshalAs(UnmanagedType.LPStruct)] CSOUND_PARAMS parms);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundSetParams(IntPtr csound, [In, MarshalAs(UnmanagedType.LPStruct)] CSOUND_PARAMS parms);
        }



    }
    
}
