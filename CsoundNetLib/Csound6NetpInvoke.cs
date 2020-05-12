using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.ComponentModel;

namespace csound6netlib
{
    //See Csound6Net.cs for copyright and license for this class

    /// <summary>
    /// Provides the pInvoke linkages to csound64.dll for fundamental csound functions.
    /// Access is only available to outside programs via the C#/.net wrapper routines for safety and convenience.
    /// Comments are primarily taken from csound.h to recall how to use each function.
    /// </summary>
    public partial class Csound6Net {

        internal const string _dllVersion = "csound64.dll"; //change this to point to a different dll for csound functions: currently csound6RC2

        #region Csound Callback Delegates
        /// <summary>
        /// Delegate to be called whenever csound calls the Message function internally.
        /// Direct csound to your C# handler by calling SetMessageCallback.
        /// </summary>
        /// <param name="csound">Raw copy of CSOUND* to marshal to C# via new Csound(csound)</param>
        /// <param name="attr">Raw Attributes value from csound</param>
        /// <param name="format">a C-style format string</param>
        /// <param name="valist">Opaque valist to process with format: use cvsprint to make c# string</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void MessageCallbackProxy(IntPtr csound, Int32 attr, string format, IntPtr valist);

        /// <summary>
        /// Delegate to be called whenever csound opens a file during performance.
        /// Direct csound to your C# handler by calling SetFileOpenCallback.
        /// </summary>
        /// <param name="csound">Raw copy of CSOUND* to marshal to C# via new Csound</param>
        /// <param name="pathname">name of file being opened by csound</param>
        /// <param name="csFileType">integer castable to CsfType enum</param>
        /// <param name="writing">integer castable to boolean: true means opened for writing</param>
        /// <param name="temporary">integer castable to boolean: true means opened as a temporary file</param>
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void FileOpenCallbackProxy(IntPtr csound, string pathname, int csFileType, int writing, int temporary);
        
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        internal delegate void RtcloseCallbackProxy(IntPtr csound);
        
        #endregion Csound Callback Delegates

        #region NativeMethods   

        /// <summary>
        /// Based upon best practice recommendations, the unmanaged csound api signatures are wrapped in a separate class
        /// called "NativeMethods" and given private class with internal method visibility to limit access just to the class
        /// which wraps these methods and presents managed signatures to the outside world.
        /// </summary>
        private class NativeMethods
        {

            #region InstantiationFunctions
            /// <summary>
            /// Initialise Csound library; should be called once before creating any Csound instances.
            /// </summary>
            /// <param name="argc"></param>
            /// <param name="argv"></param>
            /// <param name="flags"></param>
            /// <returns>Return value is zero on success, positive if initialisation was done already, and negative on error.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern Int32 csoundInitialize([In] int flags);

            /// <summary>
            /// Creates an instance of Csound.
            /// </summary>
            /// <param name="data">An IntPtr - typically a pointer to GCHandle rather than a hostData object directly</param>
            /// <returns>an opaque pointer that must be passed to most Csound API functions.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr csoundCreate(IntPtr hostdata);

            /// <summary>
            /// Destroys an instance of Csound.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundDestroy([In] IntPtr csound);

            /// <summary>
            /// Gets the current Csound version number (times 1000).
            /// </summary>
            /// <returns>the version number times 1000 (5.00.0 = 5000)</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundGetVersion();

            /// <summary>
            /// Gets the current API version number (times 100)
            /// </summary>
            /// <returns>the API version number times 100 (1.00 = 100).</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundGetAPIVersion();

            #endregion InstantiationFunctions

            #region PerformanceFunctions

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr csoundParseOrc([In] IntPtr csound, [In] String str);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundCompileTree([In] IntPtr csound, [In] IntPtr root);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundDeleteTree([In] IntPtr csound, [In] IntPtr root);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern Int32 csoundCompileOrc([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] String orchStr);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern double csoundEvalCode([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] String orchStr);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern Int32 csoundCompileArgs([In] IntPtr csound, [In] Int32 argc, [In] string[] argv);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundStart([In] IntPtr csound);

            /// <summary>
            /// Compiles Csound input files (such as an orchestra and score) as directed
            /// by the supplied command-line arguments, but does not perform them.
            /// In this (host-driven) mode, the sequence of calls should be as follows:
            ///<pre> /code
            ///       csoundCompile(csound, argc, argv);
            ///       while (!csoundPerformBuffer(csound));
            ///       csoundCleanup(csound);
            ///       csoundReset(csound);
            /// /endcode</pre>
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <param name="argc">size of argv array - 1</param>
            /// <param name="argv">string array of input values: typically flags and file names</param>
            /// <returns>zero for success or a non-zero error code on failure.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern Int32 csoundCompile([In] IntPtr csound, [In] Int32 argc, [In] string[] argv);

            /// <summary>
            /// Senses input events and performs audio output until the end of score  is reached (positive return value),
            /// an error occurs (negative return value), or performance is stopped by calling csoundStop() from another
            /// thread (zero return value).
            /// Note that csoundCompile must be called first.
            /// In the case of zero return value, csoundPerform() can be called again to continue the stopped performance.
            /// Otherwise, csoundReset() should be called to clean up after the finished or failed performance.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <returns></returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int csoundPerform([In] IntPtr csound);

            /// <summary>
            /// Senses input events, and performs one control sample worth (ksmps) of audio output.
            /// Note that csoundCompile must be called first.
            /// If called until it returns true, will perform an entire score.
            /// Enables external software to control the execution of Csound,
            /// and to synchronize performance with audio input and output.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <returns>false during performance, and true when performance is finished.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundPerformKsmps([In] IntPtr csound);

            /// <summary>
            /// Performs Csound, sensing real-time and score events and processing
            /// one buffer's worth (-b frames) of interleaved audio.
            /// ??? Returns a pointer to the new output audio in 'outputAudio' ??? - contradictory
            /// Note that csoundCompile must be called first, then call csoundGetOutputBuffer()
            /// and csoundGetInputBuffer() to get the pointer to csound's I/O buffers.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <returns>false during performance, and true when performance is finished</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundPerformBuffer([In] IntPtr csound);

            /// <summary>
            ///  Stops a csoundPerform() running in another thread.
            ///  Note that it is not guaranteed that csoundPerform() has already stopped
            ///  when this function returns.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundStop([In] IntPtr csound);

            /// <summary>
            /// Prints information about the end of a performance, and closes audio and MIDI devices.
            /// Note: after calling csoundCleanup(), the operation of the perform functions is undefined.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <returns></returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundCleanup([In] IntPtr csound);

            /// <summary>
            /// Resets all internal memory and state in preparation for a new performance.
            /// Enables external software to run successive Csound performances
            /// without reloading Csound. Implies csoundCleanup(), unless already called.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundReset([In] IntPtr csound);

            #endregion PerformanceFunctions

            #region Attributes
            /********    Attributes   **************************************************/

            /// <summary>
            /// Gets the current Sample Rate driving an instance of csound.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <returns>the number of audio sample frames per second.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Double csoundGetSr([In] IntPtr csound);

            /// <summary>
            /// Gets the current Control Rate driving an instance of csound.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <returns>the number of control samples per second.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Double csoundGetKr([In] IntPtr csound);

            /// <summary>
            /// Gets the current number of samples per control cycle.
            /// </summary>
            /// <param name="csound"></param>
            /// <returns>the number of audio sample frames per control sample.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UInt32 csoundGetKsmps([In] IntPtr csound);

            /// <summary>
            /// Gets how many channels of audio are being produced by this instance of csound.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <returns>the number of audio output channels.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UInt32 csoundGetNchnls([In] IntPtr csound);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UInt32 csoundGetNchnlsInput([In] IntPtr csound);

            /// <summary>
            /// Gets the current value understood as 0 dB.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <returns>the 0dBFS level of the spin/spout buffers.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Double csoundGet0dBFS([In] IntPtr csound);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int64 csoundGetCurrentTimeSamples([In] IntPtr csound);

            /// <summary>
            /// Gets the size of real numbers (in bytes) as compiled into the csound dll.
            /// For Csound64, this should always be 8 (for sizeof(double)) or we are somehow using the wrong dll.
            /// </summary>
            /// <returns>should always be 8 or we are using the wrong dll</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundGetSizeOfMYFLT();

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr csoundGetHostData([In] IntPtr csound);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundSetHostData([In] IntPtr csound, IntPtr hostData);


            /// <summary>
            /// Indicates whether Csound is in debug mode.
            /// </summary>
            /// <param name="csound"></param>
            /// <returns>0 if not, positive if yes</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundGetDebug([In] IntPtr csound);

            /// <summary>
            /// Puts Csound into or out of debug mode.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <param name="debug">0 takes csound out of debug, positive puts csound into debug mode</param>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundSetDebug([In] IntPtr csound, [In] Int32 debug);

            #endregion Attributes

            #region GeneralIO
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr csoundGetOutputName([In] IntPtr csound);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundSetOutput([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name, [In, MarshalAs(UnmanagedType.LPStr)] string type, [In, MarshalAs(UnmanagedType.LPStr)] string format);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundSetInput([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundSetMIDIFileOutput([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundSetMIDIFileInput([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundSetFileOpenCallback([In] IntPtr csound, FileOpenCallbackProxy processMessage);


            #endregion GeneralIO

            #region RealTimeAudio
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundSetRTAudioModule([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string module);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int csoundGetModule([In] IntPtr csound, int number, ref IntPtr name, ref IntPtr type);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern Int32 csoundGetAudioDevList([In] IntPtr csound, [Out] IntPtr list, [In] Int32 isOutput);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundGetInputBufferSize([In] IntPtr csound);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundGetOutputBufferSize([In] IntPtr csound);

            #endregion RealTimeAudio

            #region RealTimeMidi

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundSetMIDIModule([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string module);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern Int32 csoundGetMIDIDevList([In] IntPtr csound, [Out] IntPtr list, [In] Int32 isOutput);

            //also in RealTimeCsound6Net
            #endregion RealTimeMidi

            #region ScoreHandling

            /*************************************************************************************************/
            /***********************                SCORE HANDLING              ******************************/
            /*************************************************************************************************/
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern Int32 csoundReadScore([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string score);

            /// <summary>
            /// Indicates the current score time in seconds since the beginning of performance.
            /// </summary>
            /// <param name="csound"></param>
            /// <returns></returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Double csoundGetScoreTime([In] IntPtr csound);


            /// <summary>
            /// Sets whether Csound score events are performed or not, independently
            /// of real-time MIDI events (see csoundSetScorePending()).
            /// </summary>
            /// <param name="csound"></param>
            /// <returns></returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int csoundIsScorePending([In] IntPtr csound);

            /// <summary>
            /// Sets whether Csound score events are performed or not (real-time events will continue to be performed).
            /// Can be used by external software, such as a VST host,
            /// to turn off performance of score events (while continuing to perform real-time events),
            /// for example to mute a Csound score while working on other tracks of a piece,
            /// or to play the Csound instruments live.
            /// </summary>
            /// <param name="csound"></param>
            /// <param name="pending"></param>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundSetScorePending([In] IntPtr csound, [In] Int32 pending);

            /// <summary>
            /// Returns the score time beginning at which score events will  actually immediately be performed
            /// (see csoundSetScoreOffsetSeconds()).
            /// </summary>
            /// <param name="csound"></param>
            /// <returns></returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Double csoundGetScoreOffsetSeconds([In] IntPtr csound);

            /// <summary>
            /// Csound score events prior to the specified time are not performed, and performance begins
            /// immediately at the specified time (real-time events will continue to be performed as they are received).
            /// Can be used by external software, such as a VST host, to begin score performance midway through a Csound score,
            /// for example to repeat a loop in a sequencer, or to synchronize other events with the Csound score.
            /// </summary>
            /// <param name="csound"></param>
            /// <param name="time"></param>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundSetScoreOffsetSeconds([In] IntPtr csound, [In] Double time);

            /// <summary>
            /// Rewinds a compiled Csound score to the time specified with csoundSetScoreOffsetSeconds().
            /// </summary>
            /// <param name="csound"></param>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundRewindScore([In] IntPtr csound);

            /// <summary>
            /// Sorts score file 'inFile' and writes the result to 'outFile'.
            /// The Csound instance should be initialised with csoundPreCompile() before calling this function,
            /// and csoundReset() should be called after sorting the score to clean up.
            /// </summary>
            /// <param name="csound"></param>
            /// <param name="inFile">the file FILE* for the input to sort: as an IntPtr (use Bridge2CpInvoke to get FILE*)</param>
            /// <param name="outFile">a FILE* as an IntPtr (use Bridge2CpInvoke to get FILE*)</param>
            /// <returns>On success, zero is returned.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int csoundScoreSort([In] IntPtr csound, [In] IntPtr inFile, [In] IntPtr outFile);

            /// <summary>
            /// Extracts from 'inFile', controlled by 'extractFile', and writes the result to 'outFile'. 
            /// The Csound instance should be initialised with csoundPreCompile() before calling this function,
            /// and csoundReset() should be called after score extraction to clean up.
            /// </summary>
            /// <param name="csound"></param>
            /// <param name="inFile"></param>
            /// <param name="outFile"></param>
            /// <param name="extractFile"></param>
            /// <returns>The return value is zero on success.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int csoundScoreExtract(IntPtr csound, IntPtr inFile, IntPtr outFile, IntPtr extractFile);

            #endregion ScoreHandling

            #region MessageSupport

            //csoundMessage, csoundMessageS and csoundMessageV are not used because they require c-style formatting.
            //In .net, C# style formatting (String.Format(), is more natural so plugging in our own Message(string)
            //MessageS(string, param object[] vals) and MessageV methods into the MessageCallback event is sufficient and preferred.

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void csoundSetDefaultMessageCallback(MessageCallbackProxy processMessage);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundSetRtcloseCallback([In] IntPtr csound, RtcloseCallbackProxy processRtclose);

            /// <summary>
            /// Sets a function to be called by Csound to print an informational message.
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <param name="processMessage">Callback function to receive message events to process</param>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void csoundSetMessageCallback([In] IntPtr csound, MessageCallbackProxy processMessage);

            /// <summary>
            /// 
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <returns>the Csound message level (from 0 to 231).</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundGetMessageLevel([In] IntPtr csound);

            /// <summary>
            /// Sets the Csound message level (from 0 to 231).
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <param name="messageLevel">a value from 0 to 231</param>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundSetMessageLevel([In] IntPtr csound, [In] Int32 messageLevel);

            #endregion MessageSupport

            #region ChannelsControlEvents

            //RealTimeCsound6Net
            #endregion ChannelsControlEvents

            #region opcodes

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr csoundGetNamedGens([In] IntPtr csound);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundNewOpcodeList([In] IntPtr csound, [Out] out IntPtr ppOpcodeList);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void csoundDisposeOpcodeList([In] IntPtr csound, [In] IntPtr ppOpcodeList);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern CsoundStatus csoundAppendOpcode([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string opname, [In] Int32 dsblksiz, [In] Int32 flags, [In] Int32 thread, [In, MarshalAs(UnmanagedType.LPStr)] string outtypes, [In, MarshalAs(UnmanagedType.LPStr)] string intypes, OpcodeAction iopadr, OpcodeAction kopadr, OpcodeAction aopadr);
            #endregion opcodes

            #region MiscellaneousFunctions
            /// <summary>
            /// Gets the value of environment variable 'name', searching in this order:
            /// 1) local environment of 'csound' (if not NULL),
            /// 2) variables set with csoundSetGlobalEnv(),
            /// 3) then system environment variables.
            /// If 'csound' is not NULL, should be called after csoundPreCompile() or csoundCompile().
            /// </summary>
            /// <param name="csound">Instance of Csound returned by csoundCreate</param>
            /// <param name="key"></param>
            /// <returns>Return value is NULL if the variable is not set.</returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr csoundGetEnv([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] String key);

            /// <summary>
            /// Sets the global value of environment variable 'name' to 'value', or delete variable if 'value' is NULL.
            /// It is not safe to call this function while any Csound instances are active.
            /// </summary>
            /// <param name="name">Key or name of the value to set</param>
            /// <param name="value">Value or the string to associate with the key</param>)]
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern Int32 csoundSetGlobalEnv([In, MarshalAs(UnmanagedType.LPStr)] string name, [In, MarshalAs(UnmanagedType.LPStr)] string value);

            /// <summary>
            /// Returns a 32-bit unsigned integer to be used as seed from current time.
            /// </summary>
            /// <returns></returns>
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UInt32 csoundGetRandomSeedFromTime();

            //Not used by object wrappers in favor of .net Process objects for greater granularity in process control
            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern long csoundRunCommand([In] string[] argv, [In] int nowait);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundSleep(uint milleseconds);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern IntPtr csoundListUtilities([In] IntPtr csound);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void csoundDeleteUtilityList([In] IntPtr csound, IntPtr list);


            #endregion MiscellaneousFunctions
        }
        #endregion NativeMethods

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private class namedGenProxy
        {
            public IntPtr name;
            public int genum;
            public IntPtr next; //NAMEDGEN pointer used by csound as linked list, but not sure if we care
        }

        [StructLayout(LayoutKind.Sequential)]
        private class opcodeListProxy
        {
            public IntPtr opname;
            public IntPtr outtypes;
            public IntPtr intypes;
        }




        #region pInvokeSupport
        /*******************    Routines to deal with passing Managed host data to Unmanaged code      **************/
        /*******************    where csound.dll only holds a pointer without accesssing the object's  **************/
        /*******************    data payload such as HostData.                                         **************/

        /// <summary>
        /// Wraps a managed object into an IntPtr to a GCHandle thereby locking an object in memory
        /// across calls and callbacks without concern if the actual object moves due to garbage collection.
        /// Use ProtectedPointer2Object to retrieve the object itself.
        /// When done, or changeing objects being passed to csound, call Release
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        protected IntPtr Object2ProtectedPointer(object data)
        {
            IntPtr pgcData = IntPtr.Zero;
            if (data != null)
            {
                GCHandle gcData = GCHandle.Alloc(data);
                pgcData = GCHandle.ToIntPtr(gcData);
            }
            return pgcData;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pgcData"></param>
        /// <returns></returns>
        protected object ProtectedPointer2Object(IntPtr pgcData)
        {
            object data = null;
            if ((pgcData != null) && (pgcData != IntPtr.Zero))
            {
                GCHandle gcData = GCHandle.FromIntPtr(pgcData);
                data = gcData.Target;
            }
            return data;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pgcData"></param>
        protected void ReleaseProtectedPointer(IntPtr pgcData)
        {
            if ((pgcData != null) && (pgcData != IntPtr.Zero))
            {
                GCHandle gcData = GCHandle.FromIntPtr(pgcData);
                gcData.Free();
            }
        }
        #endregion pInvokeSupport
    }
}
