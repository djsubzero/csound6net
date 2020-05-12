using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Resources;
using System.Reflection;
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

/**
 * \mainpage
 * Csound 6 is an open source sound synthesis library which enjoys an active user and developer community.
 * <para>
 * Csound, was originally developed by Barry Vercoe of MIT in the 1980’s and 1990’s
 * as a multi-platform incarnation of the classic MUSICxx synthesis programs
 * set in motion by Max Matthews at the dawn of software sound synthesis.
 * It is now actively maintained by many contributors including Michael Gogins,
 * Matt Ingalls, John D. Ramsdell, John P. ffitch, Istvan Varga,
 * Victor Lazzarini, Andres Cabrera and Steven Yi.
 * </para>
 * \section section_api_cs_purpose Purpose of Csound6NetLib
 * Since csound is itself a library, it needs a host program to make it work.
 * Csound has a rich "c" API which provides many entry points to its varied services.
 * Its own command line program is a very thin "c" wrapper around that API.
 * <para>
 * Csound is delivered with a number of language wrappers over the API to facilititate
 * creation of host programs in a variety of languages such as C++, java and python.
 * This Csound6NetLib library adds Dot Net to that list of language wrappers.
 * Its goal is to make creation of a csound front-end host program in C# or VB
 * natural and idiomatic to .net and Visual Studio developers.
 * It supports such idioms as classes, enums, properties, events and delegates,
 * "using" blocks and async/await support.
 * </para>
 *
 * \section section_api_cs_example Examples of Using Csound6NetLib API
 *
 * The easiest implimentation might be as little as:
 * \code
 * using csound6netlib;
 * 
 * public static void Main(string[] args)
 * {
 *    using (var cs = new Csound6Net())
 *    {
 *        CsoundStatus result = await cs.PlayAsync(new FileInfo("csdFiles\\xanadu.csd"));
 *    }
 * }
 * \endcode
 * 
 * The Csound command--line program,itself built using the Csound API,
 * might be emulated (minus logging) in C# as follows:
 *
 * \code
 * using csound6netlib;
 *
 * public static int Main(string[] args)
 * {
 *     //Main Csound loop
 *     CsoundStatus result;
 *     using (var csound = new Csound6Net())  //1st argument could be logger delegate for message events
 *     {
 *         result = csound.Compile(args);
 *         if (result == CsoundStatus.Success)
 *         {
 *             while(!csound.PerformKsmps()); //can add custom event processing in while statement
 *         }
 *     } 
 *     return (((int)result) >= 0) ? 0 : (int)result;
 * }
 * \endcode
 */

/**
 * \defgroup INSTANTIATION Instantiation
 * \defgroup PERFORMANCE Performance
 * \defgroup ATTRIBUTES Attributes
 * \defgroup GENERALIO General Input/Output
 * \defgroup REALTIME Realtime Audio I/O
 * \defgroup MIDI Realtime Midi I/O
 * \defgroup SCORE Score Handling
 * \defgroup MESSAGES Messages and Text
 * \defgroup CHANNELS Channels, Control and Events
 * \defgroup TABLES Tables
 * \defgroup OPCODES Opcodes
 * \defgroup THREADING Threading and Concurrency
 * \defgroup MISC Miscellaneous
 * \defgroup PARAMETERS Parameters
 * \defgroup PERFTHREAD Performance Thread
 */

namespace csound6netlib
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate Int32 OpcodeAction(IntPtr csound, IntPtr opds);


    /// <summary>
    /// Basic bridge from a managed .net CLR host for making calls into Csound 6's API.
    /// Exposes standard Csound 6 functionality for generating sound as audio or files from strings, a CSD file or
    /// the classic ORC and SCO files.
    /// </summary>
    /// <remarks>
    /// Real Time Csound 6 support is handled in subclasses which should be instantiated when Midi, the
    /// software bus or real time callbacks are being used.  See Csound6NetRealtime.
    /// <para>
    /// Using higher level routines, playing a csd file can be as simple as creating a Csound64 object 
    /// and calling the perform method with the name of the file.
    /// Implements IDisposable so that this object can be used in a "using" block as per good C# conventions.
    /// Private pInvoke definitions are in a separate file: Csound64pInvoke.cs containing the rest of the class.
    /// </para>
    /// <para>
    /// Crucially, to support callbacks wishing to use this .net wrapper as well, the Csound64 class contains
    /// only a reference to its underlying instance of CSOUND* and private state variables to indicate whether
    /// an instance is the owner of CSOUND* or not.
    /// This class never have intstance variables added to it unless the implementor deals 
    /// with distinguishing between the two kinds of instances.
    /// </para>
    /// </remarks>
    [CLSCompliant(true)]
    public partial class Csound6Net : IDisposable
    {
        #region StaticMethods

        public static ResourceManager c_rm = new ResourceManager("Csound6NetLib.Resources", Assembly.GetExecutingAssembly());

        /// <summary>
        /// The size of MYFLT which is always 8 (double) in the csound64.dll used by this wrapper
        /// </summary>
        public static int SizeOfMYFLT { get { return NativeMethods.csoundGetSizeOfMYFLT(); } }


        /// <summary>
        /// Sets the global value of environment variable 'name' to 'value', or deletes the variable if 'value' is NULL.
        /// It is not safe to call this function while any Csound instances are active.
        /// Seems ok until you call Compile for the first time on an instance.
        /// </summary>
        /// <param name="key">the key to associate with "value" for later retrieval</param>
        /// <param name="value">the value to store</param>
        /// <returns>true if success, false if failed.</returns>
        public static bool SetGlobalEnv(string key, string value)
        {
            Int32 result = NativeMethods.csoundSetGlobalEnv(key, value);
            return (result == 0);
        }

        /// <summary>
        /// Returns a 32-bit unsigned integer to be used as seed from current time.
        /// </summary>
        /// <returns>a 32-bit unsigned integer to be used as seed from current time.</returns>
        public static Int32 GetRandomSeedFromTime()
        {
            return (Int32)NativeMethods.csoundGetRandomSeedFromTime();
        }

        //keys to callback event handler lists (more reliable and compact than a string, they say)
        private static object _messageEventKey = new object();
        private static object _fileOpenEventKey = new object();
        private static object _rtcloseEventKey = new object();

        protected const CsoundInitFlag c_defaultInitFlags = CsoundInitFlag.NoAtExit | CsoundInitFlag.NoSignalHandler;

        #endregion StaticMethods

        #region Constructors&Destructors

        private IntPtr m_csound = IntPtr.Zero; //reference to CSOUND structure used by most pInvoke routines
        protected bool m_disposed = false;
        protected EventHandlerList m_callbackHandlers;
        private IDictionary<string, GCHandle> m_callbacks;  //a map of GCHandles pinned callbacks in memory: kept for unpinning during Dispose()
        private IDictionary<string, Csound6Utility> m_utilities = null; //cache of utility templates

        /**
         * \ingroup INSTANTIATION
         * @{
         */
        /// <summary>
        /// Default constructor for creating a basic Csound6Net instance with an initialized
        /// and created handle to the underlying csound code ready for further work.
        /// This is the normal constructor for general usage.
        /// Calls main constructor with defaults of an empty arg list, a null host data object and no init flags.
        /// </summary>
        public Csound6Net() : this(ConsoleLogger)
        {
        }

        /// <summary>
        /// Alternate logger, perhaps from a non-console host, where only the logger need be indicated
        /// upon startup in order to capture all text from csound including Initialize() and Create()
        /// </summary>
        /// <param name="logger">an event handler to receive messages form csound</param>
        public Csound6Net(Csound6MessageEventHandler logger) : this(null, c_defaultInitFlags, logger)
        {
        }

        /// <summary>
        /// Creates a Csound instance using the provided arguments, host data object and init flag settings.
        /// It only needs to be called directly if one must supply a custom hostdata object upon csound's
        /// construction or setting an InitFlag other than "None".  This is atypical.
        /// </summary>
        /// <remarks>
        /// This is the complete constructor which must ultimately be referenced by all other constructors.
        /// This is the only access to Initialize() and Create() and Destroy() the underlying CSOUND* object
        /// thereby keeping this C# class synchronized with its csound instance.
        /// <para>
        /// An optional message event handler can be supplied in the constructor if initizations should be captured
        /// </para>
        /// </remarks>
        /// <param name="hostdata">any user data object holding data useful during processing</param>
        /// <param name="initFlag">any of the CsoundInitFlags with CsoundInitFlags.NoFlags being the most typical</param>
        /// <param name="logger">null or logger if capturing messaging from Initialize() and Create is wanted</param>
        public Csound6Net(object hostdata, CsoundInitFlag initFlag, Csound6MessageEventHandler logger)
        {
            m_callbacks = new Dictionary<string,GCHandle>();
            m_callbackHandlers = new EventHandlerList();
            SetDefaultMessageCallback(RawMessageCallback);
            if (logger != null) MessageCallback += logger;
            CsoundStatus result = Initialize(initFlag);
            if ((int)result < 0) throw new Csound6NetException(Csound6NetException.InitFailed, result);
            m_csound = Create(hostdata);
            if (m_csound == null) throw new Csound6NetException(Csound6NetException.CreateFailed, CsoundStatus.InitializationError);
            SetAudioModule("pa"); //have to set the default specifically since RC3. Still true in 6.00.1, but midi module ok
        }

        /// <summary>
        /// Destructor to insure Dispose is called if this object was not created in a "using" block.
        /// Calls Dispose(false) to release resources in unmanaged "c" code as appropriate.
        /// </summary>
        ~Csound6Net()
        {
            Dispose(false);
        }

        /// <summary>
        /// Disposes of the csound instance represented by this Csound64 object per the IDisposable interface.
        /// Only the owner of the CSOUND resource will dispose of that resource and underlying csound
        /// memory, files and hardware.  
        /// Copies (like in callbacks) will simply be garbage collected without releasing base "c" resources.
        /// </summary>
        /// Called automatically when using the "using" block as is best practice for I/O based objects in .net.
        /// If creating this Csound64 outside of a "using" block, the destructor will call the Dispose method
        /// in order to insure the release all native "c" API system resouces and memory alloc'ed during c-calls
        /// to the underlying csound code.
        /// If this is the resource owning instance, callbacks are unpinned from memory and the hostobject,
        /// if used, is also unpinned.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Selectively releases pinned objects, managed and unmanaged resources as appropriate
        /// </summary>
        /// <param name="disposing">true if called from Dispose(), false if called from destructor</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                ReleaseProtectedPointer(NativeMethods.csoundGetHostData(m_csound));
                if (disposing) //dispose of managed resources
                {
                    m_callbackHandlers.Dispose();
                    
                }
                //dispose of unmanaged resources
                if (m_callbacks != null)
                {
                    foreach (GCHandle gch in m_callbacks.Values) gch.Free();
                    m_callbacks.Clear();
                    m_callbacks = null;
                }
                // wait as long as possible to destroy csound: debugger can get upset if it becomes null during call
                Destroy();  //idempotent call to csoundDestroy() (if m_csound null, not done)
                m_disposed = true;
            }
        }
        /**
         * @}
         */
        #endregion Constructors&Destructors

        #region HighLevelMethods
        /********************************************************************************************************/
        /******************   High Level methods designed for typical use cases in csound   *********************/
        /********************************************************************************************************/
        /**
         * \ingroup PERFORMANCE
         */
        /// <summary>
        /// Compiles and performs the file referenced in the provided FileInfo method.
        /// Call as csound.PlayAsync(new FileInfo(pathToCSDFile);
        /// "Command line" flags are presumed to be in the CsOptions section of the CSD file or set prior to calling.
        /// Progress, if provided, is called ten times per second.
        /// The cancellation token can be used to halt performance per .net async conventions.
        /// This method differs from PerformAsync in that it compiles and performs in a single run and operates
        /// entirely via PerformKperf in a .net Task so that it is responsive to cancellation and progress reporting.
        /// </summary>
        /// <param name="csdFile">A readable csound CSD file as a FileInfo object</param>
        /// <param name="progress">optional progress monitor to updated every .1 seconds (see Progress(T) for ui thread use"/>)</param>
        /// <param name="cancel">cancellation token for gui's to stop playing before csdFile is finished</param>
        /// <returns>Task for awaiting or joining etc</returns>
        /// <exception cref="Csound6NetException">Whenever PerformKsmps returns an error (negative return code)</exception>
        public async Task<CsoundStatus> PlayAsync(FileInfo csdFile, IProgress<float> progress, CancellationToken cancel)
        {
            CsoundStatus result = await CompileAsync(new string[] { csdFile.FullName });
            if (result == CsoundStatus.Success)
            {
                 result = await PerformAsync(progress, cancel);
            }
            return result;
        }

        /**
         * \ingroup PARAMETERS
         */
        /// <summary>
        /// Convenience method for instantiating a Parameters object for inspecting or
        /// revising current parameter settings in this instance of csound.
        /// </summary>
        /// <returns>a Csound6Parameters object initialized with csound's current internal parameter settings</returns>
        public Csound6Parameters GetParameters()
        {
            return new Csound6Parameters(this);
        }
       /**
        * \ingroup PARAMETERS
          */
        /// <summary>
        /// Convenience method for setting a single option directly from an instance of csound
        /// </summary>
        /// <param name="option"></param>
        public void SetOption(string option)
        {
            GetParameters().SetOption(option);
        }

        #endregion HighLevelMethods

/*************************************************************************************************************/
/*********************               Properties                           ************************************/
/*************************************************************************************************************/
        /**
         * \ingroup INSTANTIATION
         * @{
         */
        /// <summary>
        /// The underlying CSOUND version number as Major*1000 + Minor*10 + patch number.
        /// </summary>
        public int Version { get { return NativeMethods.csoundGetVersion(); } }

        /// <summary>
        /// The underlying CSOUND API version number Major*100 + Minor.
        /// </summary>
        public int ApiVersion { get { return NativeMethods.csoundGetAPIVersion(); } }
        /**
         * @}
         */

        #region Attributes
        /**
         * \ingroup ATTRIBUTES Attributes
         * @{
         */
        /// <summary>
        /// The currently active sample rate set by the current score as the variable: sr.
        /// </summary>
        public double Sr { get { return NativeMethods.csoundGetSr(m_csound); } }

        /// <summary>
        /// The currently active control rate set by the current score as the variable: kr.
        /// </summary>
        public double Kr { get { return NativeMethods.csoundGetKr(m_csound); } }

        /// <summary>
        /// The size of the control rate buffer (ksmps)
        /// </summary>
        public int Ksmps { get { return (int)NativeMethods.csoundGetKsmps(m_csound); } }

        /// <summary>
        /// The currently active number of output channels as set by the current score as the variable: nchnls.
        /// </summary>
        public int Nchnls { get { return (int)NativeMethods.csoundGetNchnls(m_csound); } }

        /// <summary>
        /// The currently active number of input channels.
        /// </summary>
        public int NchnlsInput { get { return (int)NativeMethods.csoundGetNchnlsInput(m_csound); } }

        /// <summary>
        /// The currently active value representing 0dB: .
        /// </summary>
        public double OdBFS { get { return NativeMethods.csoundGet0dBFS(m_csound); } }

        /// <summary>
        /// The current performance time measured in samples.
        /// </summary>
        public Int64 CurrentTimeSamples { get { return NativeMethods.csoundGetCurrentTimeSamples(m_csound); } }

        /// <summary>
        /// An object which is stored in the csound instance and can be accessed
        /// in any callback/thread having that an instance.
        /// </summary>
        public object HostData
        {
            get { return ProtectedPointer2Object(NativeMethods.csoundGetHostData(m_csound)); }
            set
            {
                ReleaseProtectedPointer(NativeMethods.csoundGetHostData(m_csound)); //release current object if any
                NativeMethods.csoundSetHostData(m_csound, Object2ProtectedPointer(value)); // make a new protected pointer with the new object
            }
        }

        /*The SetOption, SetParams and GetParams methods are part of Csound6Parameters */

        /// <summary>
        /// Controls the verbosity of csound output by including debug information in the message stream.
        /// This is the same as setting "-v" in the args/command-line list.
        /// </summary>
        public bool IsDebugMode
        {
            get { return (NativeMethods.csoundGetDebug(m_csound) != 0); }
            set { NativeMethods.csoundSetDebug(m_csound, (value ? 1 : 0)); }
        }

        /// <summary>
        /// Indicates whether Csound score events are performed or not (real-time events will continue to be performed).
        /// Can be used by external software, such as a VST host, to turn off performance of score events
        /// (while continuing to perform real-time events).
        /// For example, to mute a Csound score while working on other tracks of a piece, or
        /// to play the Csound instruments live, set this property to false.
        /// </summary>
        public bool IsScorePending
        {
            get { return (NativeMethods.csoundIsScorePending(m_csound) != 0); }
            set { NativeMethods.csoundSetScorePending(m_csound, (value ? 1 : 0)); }
        }

        /// <summary>
        /// Indicates the score time beginning at which score events will actually immediately be performed.
        /// Csound score events prior the value of this property are not performed,
        /// and performance begins immediately at the specified time.
        /// (real-time events will continue to be performed as they are received).
        /// Can be used by external software, such as a VST host,  to begin score performance
        /// midway through a Csound score, for example, to repeat a loop in a sequencer,
        /// or to synchronize other events with the Csound score.
        /// </summary>
        public Double ScoreOffsetSeconds
        {
            get { return NativeMethods.csoundGetScoreOffsetSeconds(m_csound); }
            set { NativeMethods.csoundSetScoreOffsetSeconds(m_csound, value); }
        }

        /// <summary>
        /// The current score time in seconds since the beginning of performance.
        /// </summary>
        public Double ScoreTime
        {
            get { return NativeMethods.csoundGetScoreTime(m_csound); }
        }



        #endregion Attributes

        #region GeneralInputOutput
        /// <summary>
        /// The current name specified as the output file for generated sound samples.
        /// </summary>
        public string OutputFileName { get { return CharPtr2String(NativeMethods.csoundGetOutputName(m_csound)); } }

         #endregion GeneralInputOutput

        /** end ATTRIBUTES
         * @}
         */

        #region MidLevelMethods
        /***********************************************************************************************/
        /******************    MidLevel Routines primarily for local and subclass use    ***************/
        /***********************************************************************************************/


        #region Performance
        internal class ProxyTree
        {
            //public Int32 type;   //int           type;
            //public IntPtr value;  //ORCTOKEN     *value;
            //public Int32  rate;   //int           rate;
            //public Int32  len;    //int           len;
            //public Int32  line;   //int           line;
            //public Int32  locn;   //int           locn;
            //public IntPtr left;   //struct TREE  *left;
            //public IntPtr right;  //struct TREE  *right;
            //public IntPtr next;   //struct TREE   *next;
            //public IntPtr markup; //void          *markup;  // TEMPORARY - used by semantic checker to
        }

        internal class ProxyOrcToken
        {
            //public int type;
            //public IntPtr lexeme;
            //public int value;
            //public double fvalue;
            //public IntPtr next;
        }
        /**
         * \ingroup PERFORMANCE
         * @{
         */

        /// <summary>
        /// Parses the supplied orc code into an AST Tree for consumption by CompileTree.
        /// Currently receiving TREE as an opaque pointer (IntPtr).
        /// TODO: marshal to proper TREE for local storage/manipulation
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public IntPtr ParseOrc(string code)
        {
         //   TREE tree = new TREE();
            IntPtr pTree = NativeMethods.csoundParseOrc(m_csound, code);
            //ProxyTree proxy = Marshal.PtrToStructure(pTree, typeof(ProxyTree)) as ProxyTree;
            //ProxyOrcToken token = Marshal.PtrToStructure(proxy.value, typeof(ProxyOrcToken)) as ProxyOrcToken;
            //tree.type = proxy.type;
            //return tree;
            return pTree;
        }

        /// <summary>
        /// Compiles the provided AST Tree into internally runnable code and memory allocations
        /// ready for performance via one of the Perform methods.
        /// </summary>
        /// <param name="ast">Currently opaque IntPtr returned from ParseOrc; later will support TREE</param>
        /// <returns></returns>
        public CsoundStatus CompileTree(IntPtr ast)
        {
            int state = NativeMethods.csoundCompileTree(m_csound, ast);
            if (state < 0) throw new Csound6CompilerException(Csound6NetException.CompileFailed, Int2StatusEnum(state));
            return Int2StatusEnum(state);
        }

        /// <summary>
        /// Deletes a tree reclaiming csound memory which had been created by csound
        /// via ParseOrc() but is no no longer needed.
        /// </summary>
        /// <remarks>
        /// CompileTree() doesn't delete the tree although it will eventually be freed upon Cleanup().
        /// (CompileOrc and compile do delete the tree on their own)
        /// If a host creates its own tree within its own memory space, it will, of course,
        /// have to manage its own memory -  csound will use it but not delete it.
        /// </remarks>
        /// <param name="ast">address of an ast which csound had created earlier via ParseOrc()</param>
        public void DeleteTree(IntPtr ast)
        {
            NativeMethods.csoundDeleteTree(m_csound, ast);
        }

        /// <summary>
        /// Compiles orchestra/instrument definitions from the provided string rather than from
        /// a file such as Compile and CompileArgs require.
        /// New for Csound 6.
        /// </summary>
        /// <remarks>
        /// Parse and compile an orchestra given on an string (OPTIONAL)
        /// if str is NULL the string is taken from the internal corfile
        /// containing the initial orchestra file passed to Csound.
        /// Also evaluates any global space code.
        /// </remarks>
        /// <param name="orch"></param>
        /// <returns></returns>
        public CsoundStatus CompileOrc(String orch)
        {
            int state = NativeMethods.csoundCompileOrc(m_csound, orch);
            if (state < 0) throw new Csound6CompilerException(Csound6NetException.CompileFailed, Int2StatusEnum(state));
            return Int2StatusEnum(state);
        }

        /// <summary>
        /// Parse and compile an orchestra given on an string,
        /// evaluating any global space code (i-time only).
        /// On SUCCESS it returns a value passed to the
        /// 'return' opcode in global space
        /// </summary>
        /// <param name="orch"></param>
        /// <returns></returns>
        public double EvalCode(string orch)
        {
            return NativeMethods.csoundEvalCode(m_csound, orch);
        }

        /// <summary>
        /// New Compile method for csound6 which combines argument processing, and CompileOrc which calls
        /// ParseOrc and CompileTree together after reading input files from the provided arguments.
        /// The input csd or orc/sco files are expected to be indicated in the supplied args.
        /// This slightly lower level compile method requires that the Start() method be called 
        /// prior to issuing any performance methods.
        /// This signature blocks even as it is executing file I/O.
        /// Use CompileAsync for non-blocking near equivalent functionality (calls Start() internally).
        /// </summary>
        /// <param name="args">an array of individual csound options including </param>
        /// <returns>Success if compiling succeeded or other status codes upon failure</returns>
        public CsoundStatus CompileArgs(string[] args)
        {
            string[] argv = NormalizeCsoundArgs(args);
            int state = NativeMethods.csoundCompileArgs(m_csound, argv.Length - 1, argv);
            if (state < 0) throw new Csound6CompilerException(Csound6NetException.CompileFailed, Int2StatusEnum(state));
            return Int2StatusEnum(state);
        }

        /// <summary>
        /// Classic csound Compile method as an async method to permit
        /// file reads and other compiling actions to easily occur
        /// off of the UI thread in a .net program.
        /// </summary>
        /// <param name="args">command line parameters parsed into a string array using white space for delimiters</param>
        /// <returns>immediately with a task returning csounds status code when the compile is completed</returns>
        public async Task<CsoundStatus> CompileAsync(string[] argv)
        {
            return await Task<CsoundStatus>.Run(() =>
            {
                argv = NormalizeCsoundArgs(argv);
                int state = NativeMethods.csoundCompile(m_csound, argv.Length - 1, argv);
                if (state < 0) throw new Csound6CompilerException(Csound6NetException.CompileFailed, Int2StatusEnum(state));
                return Int2StatusEnum(state);
            });
        }

        /// <summary>
        /// Classic blocking csound Compile method: combines calls to CompileArgs and Start.
        /// In a GUI frontend, this blocking version should not be used unless it is called from
        /// within a worker thread off of the UI thread.
        /// For .net style asynchronous Tasks, use the PerformAsync method or 
        /// the PlayAsync method which combines both Compile and PerformKsmps in a 
        /// single .net Task allowing use of the await keyword from a UI thread.
        /// </summary>
        /// <param name="args">command line parameters parsed into a string array using white space for delimiters</param>
        /// <returns>csounds status code when the compile is completed</returns>
        public CsoundStatus Compile(string[] argv)
        {
            argv = NormalizeCsoundArgs(argv);
            int state = NativeMethods.csoundCompile(m_csound, argv.Length - 1, argv);
            if (state < 0) throw new Csound6CompilerException(Csound6NetException.CompileFailed, Int2StatusEnum(state));
            return Int2StatusEnum(state);
        }

        /// <summary>
        /// Prepares csound for performance after orchestra compilation is done.
        /// Called automatically by Compile.
        /// Must be called between using other compile methods and performance of a score.
        /// </summary>
        /// <returns></returns>
        public CsoundStatus Start()
        {
            int state = NativeMethods.csoundStart(m_csound);
            if (state < 0) throw new Csound6NetException(Csound6NetException.StartFailed, Int2StatusEnum(state));
            return Int2StatusEnum(state);
        }

        /// <summary>
        /// Full blocking native "c" API Perform method which presumes that the compile method has already been
        /// successfully executed and which continues until a score has been fully consumed
        /// or the Stop() method has called on the same instance (obviously from another thread).
        /// This call is synchronous and blocks, so it should only be used in a background thread
        /// so that the calling thread could issue the Stop() method call when necessary.
        /// Csound6NetThread can be used in this scenario.
        /// For .net style asynchronous Tasks, use the PerformAsync method or 
        /// the PlayAsync method which combines both Compile and PerformKsmps in a 
        /// single .net Task allowing use of the await keyword from a UI thread.
        /// </summary>
        /// <returns>the status of the underlying csound system when this method completed</returns>
        /// <exception cref="Csound6NetException">Whenever PerformKsmps returns an error (negative return code)</exception>
        public CsoundStatus Perform()
        {
            int state = NativeMethods.csoundPerform(m_csound);
            if (state < 0) throw new Csound6PerformanceException(Csound6NetException.PerformFailed, Int2StatusEnum(state));
            return Int2StatusEnum(state);
        }

        /// <summary>
        /// Async Task-based equivalent of the Perform method.
        /// Rather than calling csoundPerform, it calls csoundPerformKsmps in a loop checking for
        /// cancellation requests and updating, if requested, an IProgress object.
        /// This is more natural to .net UI programming than the classic csoundPerform mechanism
        /// which should only be used from within a worker thread anyway.
        /// Rather than calling the Stop() method, which only csoundPerform can respond to, one calls
        /// the CancellationTokenSource's Cancel method which transitions the provided CancelationToken 
        /// to a cancelled state.
        /// </summary>
        /// <param name="progress">null or an implementation of IProgress to receive updates each .1 seconds</param>
        /// <param name="cancel">a CancellationToken (or CancellationToken.None) used to interupt this task on demand</param>
        /// <returns>the status of the underlying csound system when this method completed</returns>
        /// <exception cref="Csound6NetException">Whenever PerformKsmps returns an error (negative return code)</exception>
        public async Task<CsoundStatus> PerformAsync(IProgress<float> progress, CancellationToken cancel)
        {
            return await Task<CsoundStatus>.Run( () =>
            {
                double interval = Sr / 10.0;
                double reportAt = interval;
                CsoundStatus result = CsoundStatus.Success;
                while (!PerformKsmps())  //will throw exception if failed status (catch and extract status?)
                {
                    if ((progress != null) && (CurrentTimeSamples > reportAt))
                    {
                        progress.Report((float)(ScoreTime));//see Progress<T> for reporting in ui threads
                        reportAt += interval;
                    }
                    if ((cancel != null) && cancel.IsCancellationRequested)
                    {
                        Cleanup();
                        cancel.ThrowIfCancellationRequested(); //should exit here for best practice cancellation behavior
                        result = CsoundStatus.TerminatedBySignal;
                        break; //just in case...
                    }
                } //endwhile (!PerformKsmps)
                if (result == CsoundStatus.Success) result = CsoundStatus.Completed;
             //   Cleanup();
                return result;
            }, cancel);
        }


        /// <summary>
        /// Senses input events, and performs one control sample worth (ksmps) of audio output.
        /// Note that csoundCompile must be called first.
        /// If called while it returns true, it will perform an entire score (logic inverted from c-API).
        /// Enables external software to control the execution of Csound,
        /// and to synchronize performance with audio input and output.
        /// </summary>
        /// <returns>false if still performing (exception thrown if error return), true when score is done.</returns>
        /// <exception cref="Csound6NetException">thrown if csound returns negative status: with CsoundStatus value</exception>
        public bool PerformKsmps()
        {
            int state = NativeMethods.csoundPerformKsmps(m_csound);
            if (state < 0) throw new Csound6NetException(Csound6NetException.PerformKsmpsFailed, Int2StatusEnum(state));
            return (state != 0);
        }

        /// <summary>
        /// Performs Csound, sensing real-time and score events and processing
        /// one buffer's worth (-b frames) of interleaved audio.
        /// Call while it returns true; false indicates no more samples left to create.
        /// </summary>
        /// <returns>false if still more to do (exception thrown if error return), true when score is done.</returns>
        /// <exception cref="Csound6NetException">thrown if csound returns negative status: with CsoundStatus value</exception>
        public bool PerformBuffer()
        {
            int state = NativeMethods.csoundPerformBuffer(m_csound);
            if (state < 0) throw new Csound6NetException(Csound6NetException.PerformBufferFailed, Int2StatusEnum(state));
            return (state != 0);
        }

        /// <summary>
        ///  Stops a csoundPerform() running in another thread.
        ///  Note that it is not guaranteed that csoundPerform() has already stopped
        ///  when this function returns.
        /// </summary>
        public void Stop()
        {
            NativeMethods.csoundStop(m_csound);
        }

        /// <summary>
        /// Prints information about the end of a performance, and closes audio and MIDI devices.
        /// Note: after calling csoundCleanup(), the operation of the perform functions is undefined.
        /// </summary>
        /// <returns></returns>
        public CsoundStatus Cleanup()
        {
            int state = NativeMethods.csoundCleanup(m_csound);
            if (state < 0) throw new Csound6NetException(Csound6NetException.CleanupFailed, Int2StatusEnum(state));
            return Int2StatusEnum(state);
        }

        /// <summary>
        /// Resets the internal csound instance so it can begin a new run of compile and perform.
        /// </summary>
        public void Reset()
        {
            NativeMethods.csoundReset(m_csound);
        }
        /**
         * @}
         */
        #endregion Performance

        #region GeneralIO
        /**
         * \addtogroup GENERALIO
         * @{
         */
        
        /// <summary>
        /// Sets the sound file name, type and format to use as samples are sent to disk during
        /// sound generation.
        /// Call this method to change the output file before calling ParseOrc or CompileOrc using strings
        /// for instrument definitions.  CSD file specs which name an output file will supercede this call.
        /// Once Compile has been called, this method will be ignored internally by csound.
        /// The csound 6 API now uses strings to indicate type and format (it used to use integers).
        /// To reduce the likelihood of error, this method's enum maps
        /// to the full set of legal strings accepted by the "c"-level API.
        /// </summary>
        /// <param name="name">File name relative to SFDIR for sending sound samples</param>
        /// <param name="type">The type sound file to write</param>
        /// <param name="fmt">The structure of individual samples</param>
        public void SetOutputFileName(string name, SoundFileType type, SampleFormat fmt)
        {
            NativeMethods.csoundSetOutput(m_csound, name, Csound6NetConstants.AsSoundFileTypeString(type), Csound6NetConstants.AsFormatString(fmt));
        }

        /// <summary>
        /// Convenience method for calling SetOutputFileName when sending output to a soundcard's dac
        /// directly: just calls SetOutputFileName using "dacn" when n is the provided integer
        /// along with a SoundFileType of TypRaw and a SampleFormat of AeFloat.
        /// </summary>
        /// <param name="dac">any valid dac number on your system</param>
        public void SetOutputDac(int dac)
        {
            SetOutputFileName(string.Format("dac{0}", dac), SoundFileType.TypRaw, SampleFormat.AeFloat);
        }

        /// <summary>
        /// Sets the name or path of an input sound file to make available
        /// to those opcodes which depend on input samples.
        /// </summary>
        /// <param name="name">Sound File name relative to SFDIR or working directory for receiving samples, or absolue file path</param>
        public void SetInputFileName(string name)
        {
            NativeMethods.csoundSetInput(m_csound, name);
        }

        /// <summary>
        /// Sets the named type 0 midi file as a source of derived score events.
        /// Used in conjunction with midi input opcodes in an instrument.
        /// </summary>
        /// <param name="name">name of a .mid file to use for score event generation</param>
        public void SetMidiFileInput(string name)
        {
            NativeMethods.csoundSetMIDIFileInput(m_csound, name);
        }

        /// <summary>
        /// Sets the named file as the receiving file of midi out opcodes as a type 0 midi file.
        /// </summary>
        /// <param name="name">name of a .mid file to receive midi out values</param>
        public void SetMidiFileOutput(string name)
        {
            NativeMethods.csoundSetMIDIFileOutput(m_csound, name);
        }

 
       /// <summary>
        /// Event for being notified whenever csound opens a file.
        /// </summary>
        public event Csound6FileOpenEventHandler FileOpenCallback
        {
            add
            {
                Csound6FileOpenEventHandler handler = m_callbackHandlers[_fileOpenEventKey] as Csound6FileOpenEventHandler;
                if (handler == null) SetFileOpenCallback(RawFileOpenCallback);
                m_callbackHandlers.AddHandler(_fileOpenEventKey, value);
            }
            remove
            {
                m_callbackHandlers.RemoveHandler(_fileOpenEventKey, value);
            }
        }
        /**
         * @}
         */

        /// <summary>
        /// Register a FileOpenCallback to csound and pin its address in the .net heap so csound
        /// can relyably call it as long as it exists.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns>The GCHandle used to pin this callback in memory; usually can be ignored as Csound6Net's Dispose method will release it anyway</returns>
        internal GCHandle SetFileOpenCallback(FileOpenCallbackProxy callback)
        {
            GCHandle gch = FreezeCallbackInHeap(callback);
            NativeMethods.csoundSetFileOpenCallback(m_csound, callback);
            return gch;
        }

        /// <summary>
        /// Actual Csound callback which unravels raw csound values into a dot net event argument for
        /// opening files (Csound6FileOpenEventArgs) and then throws a local C#-style event to all
        /// registrees, if any.
        /// </summary>
        /// <param name="csound">pointer to csound internally</param>
        /// <param name="path">path to file being opened</param>
        /// <param name="csFileType">one of 25 or so CSFILE_TYPE's</param>
        /// <param name="writing">bool: file is opened for writing</param>
        /// <param name="temporary">bool: file is temporary</param>
        private void RawFileOpenCallback(IntPtr csound, string path, int csFileType, int writing, int temporary)
        {
            if (csound != Engine) throw new Csound6NetException(Csound6NetException.CsoundEngineMismatch, "FileOpen callback");
            Csound6FileOpenEventHandler handler = m_callbackHandlers[_fileOpenEventKey] as Csound6FileOpenEventHandler;
            if (handler != null)
            {
                var args = new Csound6FileOpenEventArgs();
                args.Path = path;
                args.FileType = (CsfType)csFileType;
                args.IsWriting = writing != 0;
                args.IsTemporary = temporary != 0;
                handler(this, args);//broadcast the event to all concerned...
            }
        }

        #endregion GeneralIO

        #region Realtime Audio I/O

        /**
         * \ingroup REALTIME
         */
        /// <summary>
        /// The number of samples in csound's input buffer
        /// </summary>
        public int InputBufferSize { get { return NativeMethods.csoundGetInputBufferSize(m_csound); } }

        /**
         * \ingroup REALTIME
         */
        /// <summary>
        /// The number of samples in csound's output buffer
        /// </summary>
        public int OutputBufferSize { get { return NativeMethods.csoundGetOutputBufferSize(m_csound); } }

       /**
        * \ingroup REALTIME
        */
        /// <summary>
        /// Provides a list of current loaded Audio module names within csound as a dictionary of
        /// module names paired with the module's number or index within csound's internal module list.
        /// Calls csoundGetModule and then filters for "audio".
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, int> GetAudioModuleList()
        {
            var list = new Dictionary<string, int>();
            IntPtr pName = IntPtr.Zero;
            IntPtr pType = IntPtr.Zero; 
            int n = 0;
            while (NativeMethods.csoundGetModule(m_csound, n++, ref pName, ref pType) == 0)
            {
                string type = Marshal.PtrToStringAnsi(pType);
                if ("audio".Equals(type.ToLower()))
                {
                    string name = Marshal.PtrToStringAnsi(pName);
                    list.Add(name, n-1);
                }
            }
            return list;
        }

        /**
         * \ingroup REALTIME
         */
        /// <summary>
        /// Sets the module for realtime audio (defaults to portaudio) to the provided module name.
        /// Calls csoundSetRTAudioModule internally.
        /// </summary>
        /// <param name="module"></param>
        public void SetAudioModule(string module)
        {
            NativeMethods.csoundSetRTAudioModule(m_csound, module);
        }

        /**
         * \ingroup REALTIME
         */
        /// <summary>
        /// Gets this computer's input or output devices as visible to csound as a structure which
        /// is returned here in a dictionary keyed by device id.
        /// Callable with await keyword: 
        /// <code>var outdevs = await csound.GetAudioDeviceListAsync(true);</code>
        /// </summary>
        /// <param name="isOutput">True to get a dictionary of output audio devices, false to get a dictionary of input audio devices</param>
        /// <returns>upon await, a dictionary of the available audio devices keyed by id</returns>
        public async Task<IDictionary<string, CS_AUDIODEVICE>> GetAudioDeviceListAsync(bool isOutput)
        {
            return await Task.Run<IDictionary<string, CS_AUDIODEVICE>>(() =>
            {
                int forOut = isOutput ? 1 : 0;
                IntPtr pOdev = IntPtr.Zero;
                int cnt = NativeMethods.csoundGetAudioDevList(m_csound, IntPtr.Zero, forOut);
                var devices = new Dictionary<string, CS_AUDIODEVICE>();
                if (cnt > 0)
                {
                    int sSize = Marshal.SizeOf(typeof(CS_AUDIODEVICE));
                    pOdev = Marshal.AllocHGlobal(sSize * cnt);

                    cnt = NativeMethods.csoundGetAudioDevList(m_csound, pOdev, forOut);
                    for (int i = 0; i < cnt; i++)
                    {
                        var device = (CS_AUDIODEVICE)Marshal.PtrToStructure(pOdev + (i * sSize), typeof(CS_AUDIODEVICE));
                        devices[device.device_id] = device;
                    }
                    Marshal.FreeHGlobal(pOdev);
                }
                return devices;
            });
        }

        public event Csound6RtcloseEventHandler RtcloseCallback
        {
            add
            {
                Csound6RtcloseEventHandler handler = m_callbackHandlers[_rtcloseEventKey] as Csound6RtcloseEventHandler;
                if (handler == null) SetRtcloseCallback(RawRtcloseCallback);
                m_callbackHandlers.AddHandler(_rtcloseEventKey, value);
            }
            remove
            {
                m_callbackHandlers.RemoveHandler(_rtcloseEventKey, value);
            }
        }


        private void RawRtcloseCallback(IntPtr csound)
        {
            if (csound != Engine) throw new Csound6NetException(Csound6NetException.CsoundEngineMismatch, "Realtime Close callback");
            Csound6RtcloseEventHandler handler = m_callbackHandlers[_rtcloseEventKey] as Csound6RtcloseEventHandler;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        internal GCHandle SetRtcloseCallback(RtcloseCallbackProxy callback)
        {
            GCHandle gch = FreezeCallbackInHeap(callback);
            NativeMethods.csoundSetRtcloseCallback(m_csound, callback);
            return gch;
        }


        #endregion Realtime Audio I/O

        #region Realtime MIDI I/O

        /**
         * \ingroup MIDI
         */
        /// <summary>
        /// Provides a list of currently loaded Midi modules within csound as a dictionary of
        /// module names paired with its module number/index as loaded csound's module list.
        /// Calls csoundGetModule and then filters for "midi".
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, int> GetMidiModuleList()
        {
            var list = new Dictionary<string, int>();
            IntPtr pName = IntPtr.Zero;
            IntPtr pType = IntPtr.Zero;
            int n = 0;
            while (NativeMethods.csoundGetModule(m_csound, n++, ref pName, ref pType) == 0)
            {
                string type = Marshal.PtrToStringAnsi(pType);
                if ("midi".Equals(type.ToLower()))
                {
                    string name = Marshal.PtrToStringAnsi(pName);
                    list.Add(name, n - 1);
                }
            }
            return list;
        }

        /**
         * \ingroup MIDI
         */
        /// <summary>
        /// Sets the module for realtime midi (defaults to portmidi) to the provided module name.
        /// </summary>
        /// <param name="module"></param>
        public void SetMidiModule(string module)
        {
            NativeMethods.csoundSetMIDIModule(m_csound, module);
        }



        /**
         * \ingroup MIDI
         */
        /// <summary>
        /// Gets this computer's input or output midi devices as visible to csound as a structure which
        /// is returned here in a dictionary keyed by device id.
        /// Callable with await keyword: 
        /// <code>var outdevs = await csound.GetMidiDeviceListAsync(true);</code>
        /// </summary>
        /// <param name="isOutput">True to get a dictionary of midi output devices, false to get a dictionary of midi input devices</param>
        /// <returns>upon await, a dictionary of the available midi devices keyed by id</returns>
        public async Task<IDictionary<string, CS_MIDIDEVICE>> GetMidiDeviceListAsync(bool isOutput)
        {
            return await Task.Run<IDictionary<string, CS_MIDIDEVICE>>(() =>
            {
                int forOut = isOutput ? 1 : 0;
                IntPtr pOdev = IntPtr.Zero;
                int cnt = NativeMethods.csoundGetMIDIDevList(m_csound, IntPtr.Zero, forOut);
                var devices = new Dictionary<string, CS_MIDIDEVICE>();
                if (cnt > 0)
                {
                    int sSize = Marshal.SizeOf(typeof(CS_MIDIDEVICE));
                    pOdev = Marshal.AllocHGlobal(sSize * cnt);

                    cnt = NativeMethods.csoundGetMIDIDevList(m_csound, pOdev, forOut);
                    for (int i = 0; i < cnt; i++)
                    {
                        var device = (CS_MIDIDEVICE)Marshal.PtrToStructure(pOdev + (i * sSize), typeof(CS_MIDIDEVICE));
                        devices[device.device_id] = device;
                    }
                    Marshal.FreeHGlobal(pOdev);
                }
                return devices;
            });
        }

        #endregion Realtime MIDI I/O

        #region ScoreHandling
        /**
         * \addtogroup SCORE
         * @{
         */

        /// <summary>
        /// Presents the score data in the provided string to csound for incorporation
        /// into its event queue.
        /// </summary>
        /// <param name="score">A string containing valid score lines as per csound documentation</param>
        /// <returns>Success if score accepted, other values if errors in parsing occurred. Log has error messages</returns>
        public CsoundStatus ReadScore(String score)
        {
            int state = NativeMethods.csoundReadScore(m_csound, score);
            if (state < 0) throw new Csound6ScoreException(Csound6NetException.ScoreFailed, Int2StatusEnum(state));
            return Int2StatusEnum(state);
        }

        /// <summary>
        /// Rewinds a compiled Csound score to the time specified by the ScoreOffsetSeconds property.
        /// </summary>
        public void RewindScore()
        {
            NativeMethods.csoundRewindScore(m_csound);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inFile"></param>
        /// <param name="outFile"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<CsoundStatus> SortScoreAsync(FileInfo inFile, FileInfo outFile, CancellationToken cancel)
        {
            return await Task<CsoundStatus>.Run( () =>
            {
                IntPtr pInFile = BridgeToCpInvoke.cfopen(inFile.FullName, "r");
                IntPtr pOutFile = BridgeToCpInvoke.cfopen(outFile.FullName, "w");
                CsoundStatus result = Int2StatusEnum(NativeMethods.csoundScoreSort(m_csound, pInFile, pOutFile));
                if ((int)result < 0) throw new Csound6NetException(Csound6NetException.SortScoreFailed, result);
                return result;
            }, cancel);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inFile"></param>
        /// <param name="outFile"></param>
        /// <param name="extractFile"></param>
        /// <param name="cancel"></param>
        /// <returns></returns>
        public async Task<CsoundStatus> ExtractScoreAsync(FileInfo inFile, FileInfo outFile, FileInfo extractFile, CancellationToken cancel)
        {
            return await Task<CsoundStatus>.Run( () =>
            {
                IntPtr pInFile = BridgeToCpInvoke.cfopen(inFile.FullName, "r");
                IntPtr pOutFile = BridgeToCpInvoke.cfopen(outFile.FullName, "w");
                IntPtr pExtractFile = BridgeToCpInvoke.cfopen(extractFile.FullName, "r");
                CsoundStatus result = Int2StatusEnum(NativeMethods.csoundScoreExtract(m_csound, pInFile, pOutFile, pExtractFile));
                if ((int)result < 0) throw new Csound6NetException(Csound6NetException.ExtractFileFailed, result);
                return result;
            }, cancel);
        }
        /**
         * @}
         */
        #endregion ScoreHandling

        #region Opcodes
        /**
         * \ingroup OPCODES
         */
        /// <summary>
        /// Returns a sorted Dictionary keyed by all opcodes which are active in the current instance of csound.
        /// The values contain argument strings representing signatures for an opcode's
        /// output and input parameters.
        /// The argument strings pairs are stored in a list to accomodate opcodes with multiple signatures.
        /// </summary>
        /// <returns></returns>
        public async Task<IDictionary<string, IList<OpcodeArgumentTypes>>> GetOpcodeListAsync()
        {
            return await Task<IDictionary<string, IList<OpcodeArgumentTypes>>>.Run(() =>
            {
                var opcodes = new SortedDictionary<string, IList<OpcodeArgumentTypes>>();
                IntPtr ppOpcodeList = IntPtr.Zero;
                int size = NativeMethods.csoundNewOpcodeList(m_csound, out ppOpcodeList);
                if ((ppOpcodeList != IntPtr.Zero) && (size >= 0))
                {
                    int proxySize = Marshal.SizeOf(typeof(opcodeListProxy));
                    for (int i = 0; i < size; i++)
                    {
                        opcodeListProxy proxy = Marshal.PtrToStructure(ppOpcodeList + (i * proxySize), typeof(opcodeListProxy)) as opcodeListProxy;
                        string opname = Marshal.PtrToStringAnsi(proxy.opname);
                        OpcodeArgumentTypes opcode = new OpcodeArgumentTypes();
                        opcode.outypes = Marshal.PtrToStringAnsi(proxy.outtypes);
                        opcode.intypes = Marshal.PtrToStringAnsi(proxy.intypes);
                        if (!opcodes.ContainsKey(opname))
                        {
                            IList<OpcodeArgumentTypes> types = new List<OpcodeArgumentTypes>();
                            types.Add(opcode);
                            opcodes.Add(opname, types);
                        }
                        else
                        {
                            opcodes[opname].Add(opcode);
                        }
                    }
                    NativeMethods.csoundDisposeOpcodeList(m_csound, ppOpcodeList);
                }
                return opcodes;
            });
        }

        /**
         * \ingroup OPCODES
         */
        /// <summary>
        /// Returns a Dictionary keyed by the names of all named table generators.
        /// Each name is paired with its internal function number.
        /// </summary>
        /// <returns></returns>
        public IDictionary<string, int> GetNamedGens()
        {
            IDictionary<string, int> gens = new Dictionary<string, int>();
            IntPtr pNAMEDGEN = NativeMethods.csoundGetNamedGens(m_csound);
            while (pNAMEDGEN != IntPtr.Zero)
            {
                namedGenProxy namedGen = (namedGenProxy)Marshal.PtrToStructure(pNAMEDGEN, typeof(namedGenProxy));
                gens.Add(Marshal.PtrToStringAnsi(namedGen.name), namedGen.genum);
                pNAMEDGEN = namedGen.next;
            }
            return gens;
        }
        #endregion Opcodes

        #region Messages

        /**
        * \ingroup MESSAGES
        */
        /// <summary>
        /// Processes a formated message using C#/.net string formatting rather than classic "c" formatting.
        /// Formatted string is merged in with the logging events along with those from the csound engine. 
        /// Use this signature for default formatting.
        /// </summary>
        /// <remarks>
        /// This is different than the csound API because C# programmers (especially without a "c" background)
        /// would fine that awkward.
        /// Because messages from the .net host get folded in events just like messages from csound,
        /// there is no need for these messages to actually be presented to csound internally.
        ///</remarks>
        /// <param name="fmt">C#-style ("{0:n}" rather than "%n") string to add messages</param>
        /// <param name="values">as many arguments as there are positions in the format for them</param>
        public void Message(string fmt, params object[] values)
        {
            var args = new Csound6MessageEventArgs(MessageAttributes.Default, string.Format(fmt, values));
            OnMessageCallback(args);
        }

        /**
        * \ingroup MESSAGES
        */
        /// <summary>
        /// Processes a formated message using C#/.net string formatting rather than classic "c" formatting.
        /// Formatted string is merged in with the logging events along with those from the csound engine. 
        /// The combined flags in the attributes are split out for use in the event args for MessageCallback.
        /// Attributes are presented to Message event handlers. but it is up to the handler to honor them
        /// </summary>
        /// This is different than the csound API because C# programmers (especially without a "c" background)
        /// would fine that awkward.
        /// Because messages from the .net host get folded in events just like messages from csound,
        /// there is no need for these messages to actually be presented to csound internally.
        /// <param name="attrs">Or'ed MessageAttribute flags to produce the desired decorations</param>
        /// <param name="fmt">C#-style ("{0:n}" rather than "%n") string to add messages</param>
        /// <param name="values">as many comma-separated arguments as there are positions in the format for them</param>
        public void MessageS(MessageAttributes attrs, string fmt, params object[] values)
        {
            var args = new Csound6MessageEventArgs(attrs, string.Format(fmt, values));
            OnMessageCallback(args);
        }

        /**
         * \ingroup MESSAGES
         */
        /// <summary>
        /// Just calls MessageS.  The distinction between ... and va_args is meaningless in C#.
        /// See documentation for MessageS().
        /// </summary>
        /// <param name="attrs">Or'ed MessageAttribute flags to produce the desired decorations</param>
        /// <param name="fmt">C#-style ("{0:n}" rather than "%n") string to add messages</param>
        /// <param name="values">as many comma-separated arguments as there are positions in the format for them</param>
        public void MessageV(MessageAttributes attrs, string fmt, params object[] values)
        {
            MessageS(attrs, fmt, values);
        }

        /**
         * \ingroup MESSAGES
         */
        /// <summary>
        /// Event to for application message handlers/loggers to register for
        /// being notified when csound provides logging messages.
        /// </summary>
        public event Csound6MessageEventHandler MessageCallback;

        /**
        * \ingroup MESSAGES
        */
        /// <summary>
        /// Controls the verbosity of output messages:
        /// Same as setting the "-m" arg/command line argument.
        /// </summary>
        public MessageLevel MessageLevel
        {
            get { return (MessageLevel)NativeMethods.csoundGetMessageLevel(m_csound); }
            set { NativeMethods.csoundSetMessageLevel(m_csound, (int)value); }
        }

        /// <summary>
        /// Sets a function to be called by Csound to print an informational message.
        /// </summary>
        /// <param name="callback">an implementation of the MessageCallback delegate to be forwarded to csound</param>
        internal GCHandle SetMessageCallback(MessageCallbackProxy callback)
        {
            GCHandle gch = FreezeCallbackInHeap(callback);
            NativeMethods.csoundSetMessageCallback(m_csound, callback);
            return gch;
        }

        /// <summary>
        /// Sets csound's global (non-instance specific) message callback.
        /// Ours is tied to an instance or Csound6Net, however, because we are managing memory handles
        /// which must ultimately be given back to the garbage collector.
        /// It's too messy to manage this for a .net class variable.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        internal GCHandle SetDefaultMessageCallback(MessageCallbackProxy callback)
        {
            GCHandle gch = FreezeCallbackInHeap(callback);
            NativeMethods.csoundSetDefaultMessageCallback(callback);
            return gch;
        }

        /// <summary>
        /// Default Message Event Handler which copies csound messages to stdout.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        internal static void ConsoleLogger(object sender, Csound6MessageEventArgs args)
        {
            Console.Out.Write(args.Message);
        }

        /// <summary>
        /// Default logger for subprocesses (utilities, runcommand) to send messages
        /// to whereever csound sends messages.
        /// Relays to whatever Csound6MessageEventHandlers are registered to this Csound6Net instance.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private void RelayLogger(object sender, Csound6MessageEventArgs args)
        {
            var handler = MessageCallback;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

       /// <summary>
       /// Csound Message Callback intercepted to convert to Csound6MessageEventArgs to be
       /// routed via standard C# event handlers.
       /// </summary>
       /// <param name="csound"></param>
       /// <param name="attr"></param>
       /// <param name="format"></param>
       /// <param name="valist"></param>
        private void RawMessageCallback(IntPtr csound, Int32 attr, string format, IntPtr valist)
        {
            MessageAttributes attrs = (MessageAttributes)((uint)attr);
            string msg = BridgeToCpInvoke.cvsprintf(format, valist);
            OnMessageCallback(new Csound6MessageEventArgs(attrs, msg));
        }

        /// <summary>
        /// Fires the MessageCallback event with the provided event args.
        /// </summary>
        /// <param name="args">initialized args for sending with a MessageCallback event</param>
        private void OnMessageCallback(Csound6MessageEventArgs args)
        {
            Csound6MessageEventHandler handler = MessageCallback as Csound6MessageEventHandler;
            if (handler != null)
            {
                handler(this, args);//broadcast the event to all who care...
            }
        }

        #endregion Messages

        #region Miscellaneous

        /**
         * \ingroup MISC
         * @{
         */
        /// <summary>
        /// Gets a string value from csound's environment values.
        /// Meaningful values include the contents of Windows' OS environment values 
        /// such as SFDIR or SADIR for path name defaults.
        /// </summary>
        /// <param name="key">the name of the Environment Variable to get</param>
        /// <returns>the corresponding value or an empty string if no such key exists</returns>
        public string GetEnv(string key)
        {
            return CharPtr2String(NativeMethods.csoundGetEnv(m_csound, key));
        }

        /// <summary>
        /// Run an operating system level command from within a csound host.
        /// Mimics csoundRunCommand but uses the .net Process class for better control
        /// adding cancelation capability, capture of output and integration with
        /// .net Tasks via async/await to support GUI responsiveness.
        /// Always returns task and runs command on a separate thread.
        /// </summary>
        /// <param name="argv">list of command line arguments starting with the program to execute
        /// as either a path or a string which the PATH environment variable can resolve</param>
        /// <param name="logger">a logger to receive output from process or null to use same logger registered to csound already</param>
        /// <param name="cancel">cancellation token (from CancellationTokenSource or CancellationToken.None) to arrest a runnaway process or express a timeout</param>
        /// <returns>the result from the process</returns>
        /// <exception cref="System.OperationCanceledException">if operation is cancelled</exception>
        public async Task<long> RunCommandAsync(string[] argv, Csound6MessageEventHandler logger, CancellationToken cancel)
        {
            var process = new CsoundExternalProcess(new List<string>(argv));
            if (logger == null) logger = RelayLogger;
            process.MessageCallback += logger;
            return await process.RunAsync(cancel);
        }

        /// <summary>
        /// Provides an instance of the requested utility.
        /// The provided instance is a clone of the template stored in the utilities dictionary cache
        /// so it can be given its own parameters without fear of residue from previous runs.
        /// This is the preferred way to run get a utility and run it.
        /// </summary>
        /// <param name="name">any valid utility name (see GetUtilities() to get installed utility names)</param>
        /// <returns>an empty utility runner ready to accept parameters and to be run</returns>
        public Csound6Utility GetUtility(string name)
        {
            Csound6Utility utility = null;
            var utilities = GetUtilities();
            if (utilities.Keys.Contains(name)) utility = utilities[name].Clone() as Csound6Utility;
            return utility;
        }

        /// <summary>
        /// Provides a dictionary of installed csound utilities keyed by name
        /// with values being an unpopulated instance of the utility object
        /// associated with that name.
        /// Used internally by utility factories, but can be called by host programs as well
        /// to list available utilities.
        /// Usually, a factory is used to instantiate the correct default populated utility.
        /// </summary>
        /// <returns>the dictionary described above</returns>
        public IDictionary<string, Csound6Utility> GetUtilities()
        {
            if (m_utilities == null)
            {
                m_utilities = new SortedDictionary<string, Csound6Utility>();
                IntPtr pUtilNames = NativeMethods.csoundListUtilities(m_csound);
                if (pUtilNames != IntPtr.Zero)
                {
                    for (int i = 0; ; i++)
                    {
                        IntPtr pName = Marshal.ReadIntPtr(pUtilNames + (i * Marshal.SizeOf(typeof(IntPtr))));
                        if (pName == IntPtr.Zero) break;
                        string name = Marshal.PtrToStringAnsi(pName);
                        if (string.IsNullOrWhiteSpace(name)) break;
                        m_utilities.Add(name, new Csound6Utility(name, this));
                    }
                    NativeMethods.csoundDeleteUtilityList(m_csound, pUtilNames);
                }
            }
            return m_utilities;
        }

        /// <summary>
        /// Waits for at least the specified number of milliseconds, yielding the CPU to other threads.
        /// </summary>
        /// <param name="milleseconds">number of milleseconds to yield cpu before continuing.</param>
        public void Sleep(int milleseconds)
        {
            NativeMethods.csoundSleep((uint)milleseconds);
        }

        /**
         * @}
         */

        #endregion Miscellaneous

        #endregion MidLevelMethods

        #region LowLevelMethods
        /*************************************************************************************************/
        /*************************            These methods are private or internal      *****************/
        /*************************   As part of csound API, they are supported but only  *****************/
        /*************************  used by constructors and destructors/Dispose methods *****************/
        /*************************************************************************************************/

        /// <summary>
        /// Only for use by objects in this package so they can call the csound engine
        /// themselves without holding their own instance.
        /// There should only be one pointer to csound for any given instance of a Csound64 object.
        /// Multiple copies of the .net version (Csound64) is less harmful since that code is
        /// managed and references will be deleted as objects holding them go out of scope.
        /// Objects referencing the Engine, on the other hand, should never save a copy of the engine
        /// for themselves.  They should always reference this property as needed.
        /// </summary>
        internal IntPtr Engine
        {
            get { return m_csound; }
        }


        /// <summary>
        /// Initializes the underlying csound c-code using provided args and flags.
        /// This method is used internally by constructors.
        /// There should be no need for a calling program to call Initialize directly
        /// since the input arguments are provided by constructor arguments at the proper time.
        /// </summary>
        /// <param name="args"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        private CsoundStatus Initialize(CsoundInitFlag flags)
        {
            return Int2StatusEnum(NativeMethods.csoundInitialize((int)flags));
        }

        /// <summary>
        /// Creates the instance of Csound that will be enclosed by this Csound6Net class.
        /// Should only be called by the constructor of this object.
        /// </summary>
        /// <param name="hostData"></param>
        /// <returns></returns>
        private IntPtr Create(object hostData)
        {
            return NativeMethods.csoundCreate(Object2ProtectedPointer(hostData));
        }

        /// <summary>
        /// Destroys the instance of the Csound instance that was used by this Csound6Net class.
        /// Will release all C resources what were acquired during the generation of sound files
        /// by this instance.
        /// Should only be called by the Dispose() method (or destructor) as this object
        /// goes out of scope and is garbage collected. 
        /// </summary>
        private void Destroy()
        {
            if (m_csound != IntPtr.Zero)
            {
                try
                {
                    NativeMethods.csoundDestroy(m_csound);
                }
                catch (Exception e)
                {
                    string s = e.Message;
                }
                m_csound = IntPtr.Zero;
            } 
        }


        #endregion LowLevelMethods

        #region PrivateSupportMethods
        /*****************************************************************************************/
    /***************   private support methods for mid and higher level methods  *************/
    /*****************************************************************************************/
        /// <summary>
        /// Pins a callback in memory for the duration of the life of this csound instance.
        /// Csound expects a callback to be valid as long as it references it whereas pInvoke
        /// callbacks can get garbage collected when no references to it are detected.
        /// Callbacks are unpinned automatically by the Dispose() method when called
        /// </summary>
        /// <param name="callback">the callback to pin</param>
        /// <returns>the handle pinning the callback in the heap.  Usually can be ignores.</returns>
        internal GCHandle FreezeCallbackInHeap(Delegate callback)
        {
            string name = callback.Method.Name;
            if (!m_callbacks.ContainsKey(name)) m_callbacks.Add(name, GCHandle.Alloc(callback));
            return m_callbacks[name];
        }

        /// <summary>
        /// Safely transforms an integer return code into a CsoundStatus enum member.
        /// If the int is 0 or greater, success is implied.
        /// If it is negative, failure is implied.
        /// Out of range positive numbers become CsoundStatus.Completed.
        /// Out of range negative values become CsoundStatus.UndefinedError.
        /// </summary>
        /// <param name="iResult">the c integer status value to convert</param>
        /// <returns>the corresponding enum member</returns>
        internal static CsoundStatus Int2StatusEnum(int iResult)
        {
            if (iResult == 256) return CsoundStatus.ExitJumpSuccess;
            else if (iResult > 0) return CsoundStatus.Completed;
            else if (iResult == 0) return CsoundStatus.Success;
            else if ((iResult < 0) && (iResult > -6)) return (CsoundStatus)iResult;
            return CsoundStatus.UndefinedError;
        }

        /// <summary>
        /// Converts a csound int return value of zero vs non-zero as false or true respectively
        /// </summary>
        /// <param name="iResult">the integer to convert</param>
        /// <returns>the equivalent bool value</returns>
        internal static bool Int2Boolean(int iResult)
        {
            return (iResult >= 0);
        }

        /// <summary>
        /// Converts the char* for an ascii "c" string represented by the provided IntPtr
        /// into a managed string.  Usually used for values returning a const char * from
        /// a csound routine.
        /// Using this method avoids pInvoke's default automatic attpempted deletion
        /// of the returned char[] when string is expressly given as a marshalling type.
        /// </summary>
        /// <param name="pString"></param>
        /// <returns></returns>
        internal static String CharPtr2String(IntPtr pString)
        {
            return ((pString != null) && (pString != IntPtr.Zero)) ? Marshal.PtrToStringAnsi(pString) : string.Empty;
        }

        /// <summary>
        /// Prepares a list of values to be presented to csound as though they were args
        /// presented to a c-style main program as expected by the csound API.
        /// Since C# command lines and internally developed arguments do not follow c conventions
        /// like starting with the program name and ending with a null string, this routine compensates for that.
        /// It is ok but unnecessary if the c conventions are followed by the caller.
        /// </summary>
        /// <param name="args">null or a list of arguments to present as argv string array to csound</param>
        /// <returns>a string array containing the provided arguments preceded by program name and ending in a null string</returns>
        internal static string[] NormalizeCsoundArgs(string[] argv)
        {
            List<string> args = new List<string>();
            if (argv == null) argv = new string[0];
            foreach (string argn in argv)
            {
                string arg = argn.Trim();
                if (arg.Contains(" "))
                {
                    arg = BridgeToCpInvoke.wGetShortPathName(arg);
                }
                args.Add(arg);
            }
            if (args.Count == 0) args.Add(string.Empty);
            if (!"csound".Equals(args[0])) args.Insert(0, "csound");
            if (!string.IsNullOrWhiteSpace(args[args.Count - 1]))
            {
                args.Add(string.Empty);
            }
            return args.ToArray();
        }

        internal string FileInfo2CsoundPath(FileInfo file, string[] defaultPaths)
        {
            string path = (file != null) ? file.FullName : string.Empty;
            if (defaultPaths != null)
            {
                 
                foreach (string defaultPath in defaultPaths)
                {
                    if (file.DirectoryName.Equals(defaultPath))
                    {
                        path = file.Name;
                    }
                }
            }
            return (path.Contains(" ")) ? BridgeToCpInvoke.wGetShortPathName(path) : path;
        }


        #endregion PrivateSupportRoutines

    }
}
