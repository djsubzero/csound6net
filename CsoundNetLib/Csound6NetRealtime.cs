using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
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
    /// Extends the basic Csound6Net class with methods for real time support for channels
    /// (software bus), realtime events, and audio/midi services.
    /// </summary>
    [CLSCompliant(true)]
    public partial class Csound6NetRealtime : Csound6Net
    {
        private static object _inputChannelEventKey = new object();
        private static object _outputChannelEventKey = new object();
        private static object _senseEventKey = new object();


        private Csound6SoftwareBus m_softwareBus;//A class to facilitate channel access by name.

        /// <summary>
        /// Default constructor for creating a basic Csound6Net instance with an initialized
        /// and created handle to the underlying csound code ready for further work.
        /// This is the normal constructor for general usage.
        /// </summary>
        public Csound6NetRealtime()
            : this(ConsoleLogger)
        {
        }

        /// <summary>
        /// Alternate logger, perhaps from a non-console host, where only the logger need be indicated
        /// upon startup in order to capture all text from csound including Initialize() and Create()
        /// </summary>
        /// <param name="logger">an event handler to receive messages form csound</param>
        public Csound6NetRealtime(Csound6MessageEventHandler logger)
            : this(null, c_defaultInitFlags, logger)
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
        public Csound6NetRealtime(object data, CsoundInitFlag flags, Csound6MessageEventHandler logger)
            : base(data, flags, logger)
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!m_disposed)
            {
                if (m_softwareBus != null) m_softwareBus.Dispose(); //release unmanaged channel memory
                m_softwareBus = null;
            }
            base.Dispose(disposing);
        }


        /// <summary>
        /// Provides a dictionary of all currently defined channels resulting from compilation of an orchestra
        /// containing channel definitions.
        /// Entries, keyed by name, are polymorphically assigned to their correct data type: control, audio, string, pvc.
        /// Used by the Csound6SoftwareBus class to initialize its contents.
        /// </summary>
        /// <returns>a dictionary of all currently defined channels keyed by their name to its ChannelInfo</returns>
        public IDictionary<string, ChannelInfo> GetChannelList()
        {
            IDictionary<string, ChannelInfo> channels = new SortedDictionary<string, ChannelInfo>();
            IntPtr ppChannels = IntPtr.Zero;
            int size = NativeMethods.csoundListChannels(Engine, out ppChannels);
            if ((ppChannels != IntPtr.Zero) && (size >= 0))
            {
                int proxySize = Marshal.SizeOf(typeof(ChannelInfoProxy));
                for (int i = 0; i < size; i++)
                {
                    var proxy = Marshal.PtrToStructure(ppChannels + (i * proxySize), typeof(ChannelInfoProxy)) as ChannelInfoProxy;
                    string chanName = Marshal.PtrToStringAnsi(proxy.name);
                    ChannelInfo info = new ChannelInfo(chanName, (ChannelType)(proxy.type & 15), (ChannelDirection)(proxy.type >>4));
                    var hintProxy = proxy.hints;
                    var hints = new ChannelHints((ChannelBehavior)hintProxy.behav, hintProxy.dflt, hintProxy.min, hintProxy.max);
                    hints.x = hintProxy.x; hints.y = hintProxy.y; hints.height = hintProxy.height; hints.width = hintProxy.width;
                    hints.attributes = (hintProxy.attributes != IntPtr.Zero) ? Marshal.PtrToStringAnsi(hintProxy.attributes) : null;
                    info.Hints = hints;
                    channels.Add(chanName, info);
                }
                NativeMethods.csoundDeleteChannelList(Engine, ppChannels);
            }
            return channels;
        }

        /// <summary>
        /// Provides a cached software bus object for facilitating the access of csound channels by name.
        /// It is preloaded with entries for any channels know by csound at the time of instantiation 
        /// (from score after compilation, usually).
        /// </summary>
        /// <returns></returns>
        public Csound6SoftwareBus GetSoftwareBus()
        {
            if (m_softwareBus == null) m_softwareBus = new Csound6SoftwareBus(this);
            return m_softwareBus;
        }

        /// <summary>
        /// Posts a single character to be interpreted by sensekey opcode if used in an instrument.
        /// Is translated from unicode to ascii en route to csound.
        /// </summary>
        /// <param name="c">any value to be interpreted by sensekey in an instrument</param>
        public void SendKeyPress(char c)
        {
            NativeMethods.csoundKeyPress(Engine, c);
        }

        /// <summary>
        /// Post a string to be interpreted as a single score line to be played in real time.
        /// P2 (start time) is added to the internal time at the moment the event is received.
        /// Use this signature for SendScoreEvent when a string argument is needed within the parameters you are providing.
        /// Internally calls csoundInputMessage.
        /// </summary>
        /// <param name="message">any valid text for a score event of types Note, Table, Mute, Advance or End</param>
        public CsoundStatus SendScoreEvent(string scoreEventText)
        {
            NativeMethods.csoundInputMessage(Engine, scoreEventText);
            return CsoundStatus.Success; //return success since failure is not reported
        }

        /// <summary>
        /// Posts an event based upon the provided type and numeric parameters to be played in real time.
        /// Array position in parms is zero-based whereas csound documentation usually presumes one-based nomenclature.
        /// P2 (start time; parms[1]) is added to the internal time at the moment the event is received.
        /// This signature can be used when all arguments are numeric.
        /// Internally calls csoundScoreEvent.
        /// </summary>
        /// <param name="type">any member of the score event type enumeration</param>
        /// <param name="parms">zero-based array of parameters relevant to the event being posted</param>
        /// <returns>Success if no error occurred while posting the event</returns>
        public CsoundStatus SendScoreEvent(ScoreEventType type, double[] parms)
        {
            return Int2StatusEnum(NativeMethods.csoundScoreEvent(Engine, (char)type, parms, parms.Length));
        }

        /// <summary>
        /// Posts an event based upon the provided type and numeric parameters to be played at an absolute time.
        /// Array position in parms is zero-based whereas csound documentation usually presumes one-based nomenclature.
        /// P2 (start time; parms[1]) is added to the provided timeOffset argument - presumably resulting in a future moment.
        /// This signature can be used when all arguments are numeric.
        /// Internally calls csoundScoreEventAbsolute.
        /// </summary>
        /// <param name="type">any member of the score event type enumeration</param>
        /// <param name="parms">zero-based array of parameters relevant to the event being posted</param>
        /// <param name="timeOffset">offset in seconds or beats to use as a base for adding P2</param>
        /// <returns></returns>
        public CsoundStatus SendScoreEvent(ScoreEventType type, double[] parms, double timeOffset)
        {
            return Int2StatusEnum(NativeMethods.csoundScoreEventAbsolute(Engine, (char)type, ref parms, parms.Length, timeOffset));
        }

        /// <summary>
        /// Removes instances of one or more active instruments having the provided instrument number.
        /// How many active instances of the instrument are deactivated can be determined by the KillInstanceMode enumeration
        /// and whether release trails occur as the instrument is deactivated can be controlled by the allowRelease flag.
        /// Calls csoundKillInstance.
        /// </summary>
        /// <param name="instNbr">Number of the instrument to be released</param>
        /// <param name="mode">controls which, if any, instances of instNbr to remove (all, oldest, newest, etc)</param>
        /// <param name="allowRelease">if true, release phase of an instrument will still occur upon deactivation; if false, no release phase occurs</param>
        /// <returns></returns>
        public CsoundStatus DeactivateInstrumentInstance(double instNbr, KillInstanceMode mode, bool allowRelease)
        {
            return Int2StatusEnum(NativeMethods.csoundKillInstance(Engine, instNbr, IntPtr.Zero, (int)mode, (allowRelease ? 1 : 0)));
        }

        /// <summary>
        /// Removed instances of one or more active instruments having the provided instrument name.
        /// How many active instances of the instrument are deactivated can be determined by the KillInstanceMode enumeration
        /// and whether release trails occur as the instrument is deactivated can be controlled by the allowRelease flag.
        /// Calls csoundKillInstance.
        /// </summary>
        /// <param name="instrName">name of instrument to deactivate</param>
        /// <param name="mode">controls which, if any, instances of instr to remove</param>
        /// <param name="allowRelease">if true, release phase of an instrument will still occur upon deactivation; if false, no release phase occurs</param>
        /// <returns></returns>
        public CsoundStatus DeactivateInstrumentInstance(string instrName, KillInstanceMode mode, bool allowRelease)
        {
            IntPtr pName = Marshal.StringToHGlobalAnsi(instrName);
            int result = NativeMethods.csoundKillInstance(Engine, -1, pName, (int)mode, (allowRelease ? 1 : 0));
            Marshal.FreeHGlobal(pName);
            return Int2StatusEnum(result);
        }



        /******************************************************************************************************************/
        /*****************                              Events                                                            */

        /// <summary>
        /// 
        /// </summary>
        public event Csound6SenseEventCallbackHandler SenseEventsCallback
        {
            add
            {
                Csound6SenseEventCallbackHandler handler = m_callbackHandlers[_inputChannelEventKey] as Csound6SenseEventCallbackHandler;
                if (handler == null) SetSenseEventCallback(RawSenseEventsCallback);
                m_callbackHandlers.AddHandler(_senseEventKey, value);
            }
            remove
            {
                m_callbackHandlers.RemoveHandler(_senseEventKey, value);
            }
        }

        /// <summary>
        /// Registers a callback proxy (below) that transforms csound callbacks into .net events.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        internal GCHandle SetSenseEventCallback(SenseEventCallbackProxy callback)
        {
            GCHandle gch = FreezeCallbackInHeap(callback);
            NativeMethods.csoundRegisterSenseEventCallback(Engine, callback);
            return gch;
        }

        public void RawSenseEventsCallback(IntPtr csound, IntPtr userdata)
        {

        }

        /// <summary>
        /// Fires whenever an "invalue" opcode is executed in an instrument.
        /// 
        /// </summary>
        public event Csound6ChannelEventHandler InputChannelCallback
        {
            add
            {
                Csound6ChannelEventHandler handler = m_callbackHandlers[_inputChannelEventKey] as Csound6ChannelEventHandler;
                if (handler == null) SetInputChannelCallback(RawInputChannelEventCallback);
                m_callbackHandlers.AddHandler(_inputChannelEventKey, value);
            }
            remove
            {
                m_callbackHandlers.RemoveHandler(_inputChannelEventKey, value);
            }
        }

        /// <summary>
        /// Fires whenever an "outvalue" opcode is executed in an instrument.
        /// "outvalue" only supports sending control values (k-value) and strings (S-value)
        /// via OutputChannelCallbacks.
        /// Audio, PVC and var bus values have no csound opcode support for OutputChannelCallbacks.
        /// </summary>
        public event Csound6ChannelEventHandler OutputChannelCallback
        {
            add
            {
                Csound6ChannelEventHandler handler = m_callbackHandlers[_outputChannelEventKey] as Csound6ChannelEventHandler;
                if (handler == null) SetOutputChannelCallback(RawOutputChannelEventCallback);
                m_callbackHandlers.AddHandler(_outputChannelEventKey, value);
            }
            remove
            {
                m_callbackHandlers.RemoveHandler(_outputChannelEventKey, value);
            }
        }

        /***********************************************************************************************************************/
        /*                  Support routines to manage OutputChannelCallback and InputChannelCallback                          */

        /// <summary>
        /// Registers a callback proxy (below) that transforms csound callbacks into .net events.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        internal GCHandle SetInputChannelCallback(ChannelCallbackProxy callback)
        {
            GCHandle gch = FreezeCallbackInHeap(callback);
            NativeMethods.csoundSetInputChannelCallback(Engine, callback);
            return gch;
        }

        /// <summary>
        /// Registers a callback proxy (below) that transforms csound callbacks into .net events.
        /// </summary>
        /// <param name="callback"></param>
        /// <returns></returns>
        internal GCHandle SetOutputChannelCallback(ChannelCallbackProxy callback)
        {
            GCHandle gch = FreezeCallbackInHeap(callback);
            NativeMethods.csoundSetOutputChannelCallback(Engine, callback);
            return gch;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="csound"></param>
        /// <param name="name"></param>
        /// <param name="pValue"></param>
        /// <param name="pChannelType"></param>
        private void RawInputChannelEventCallback(IntPtr csound, string name, IntPtr pValue, IntPtr pChannelType)
        {
            var cstype = (CS_TYPE)Marshal.PtrToStructure(pChannelType, typeof(CS_TYPE));
            Csound6ChannelEventArgs args = null;
            ChannelDirection dir = (ChannelDirection)((cstype.argtype != 0) ? cstype.argtype : 3);
            switch (cstype.varTypeName[0])
            {
                case 'k':
                    args = new Csound6ChannelEventArgs(name, ChannelType.Control, dir, pValue);
                    args.Value = (double)Marshal.PtrToStructure(pValue, typeof(double));
                    break;
                case 'S':
                    args = new Csound6ChannelEventArgs(name, ChannelType.String, dir, pValue);
                    args.Value = Marshal.PtrToStringAnsi(pValue);
                    break;
                case 'a': //audio ksmps buffer: not supported by csound input callbacks
                case 'p': //pvs: not supported by csound input callbacks
                case 'v': //var??? no csound code supports this yet
                default:
                    //only S and k should be sending output channel callbacks.  Ignore for now and implement as csound adds
                    break;
            }
            Csound6ChannelEventHandler handler = m_callbackHandlers[_inputChannelEventKey] as Csound6ChannelEventHandler;
            if ((args != null) && (handler != null))
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="csound"></param>
        /// <param name="name"></param>
        /// <param name="pValue"></param>
        /// <param name="pChannelType"></param>
        private void RawOutputChannelEventCallback(IntPtr csound, string name, IntPtr pValue, IntPtr pChannelType)
        {
            var cstype = (CS_TYPE)Marshal.PtrToStructure(pChannelType, typeof(CS_TYPE));
            Csound6ChannelEventArgs args = null;
            ChannelDirection dir = (ChannelDirection) ((cstype.argtype != 0) ? cstype.argtype : 3);
            switch (cstype.varTypeName[0])
            {
                case 'k':
                    args = new Csound6ChannelEventArgs(name, ChannelType.Control, dir);
                   args.Value = (double)Marshal.PtrToStructure(pValue, typeof(double));
                    break;
                case 'S':
                    args = new Csound6ChannelEventArgs(name, ChannelType.String, dir);
                    args.Value = Marshal.PtrToStringAnsi(pValue); 
                    break;
                case 'a': //audio ksmps buffer: not supported by csound output callbacks
                case 'p': //pvs: not supported by csound output callbacks
                case 'v': //var??? no csound code supports this yet
                default:
                    //only S and k should be sending output channel callbacks.  Ignore: someday csound might be supporting some of these
                    break;
            }
            Csound6ChannelEventHandler handler = m_callbackHandlers[_outputChannelEventKey] as Csound6ChannelEventHandler;
            if ((args != null) && (handler != null))
            {
                handler(this, args);
            }
        }

    }
}
