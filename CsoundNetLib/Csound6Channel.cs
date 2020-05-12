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
    /// Abstract class providing the common behavior used by any channel implementation
    /// including its name, data size and direction.
    /// Channels are usually created directly in events (for callbacks) or by the Csound6SoftwareBus class.
    /// They are only meaningful if an instrument references them via channel opcodes.
    /// Subclasses should and do use the new thread-safe calls to set and get values from a channle.
    /// The legacy direct GetChannelPointer is provided but discouraged because it is not thread safe.
    /// </summary>
    [CLSCompliant(true)]
    public abstract class Csound6Channel : IDisposable
    {
        internal const int ChannelTypeMask = 15;
        private IntPtr m_pChannel = IntPtr.Zero;
        protected Csound6Net m_csound;

    /**
     * \addtogroup CHANNELS
     * @{
     */
        /// <summary>
        /// Provides the current unmanaged memory size in bytes of the channel defined by the provided name
        /// </summary>
        /// <param name="csound">A reference to the Csound6NetRealtime where the channel is defined</param>
        /// <param name="name">The name of the channel as defined to csound</param>
        /// <returns>the size in bytes of the named channel</returns>
        public static int GetChannelDataSize(Csound6NetRealtime csound, string name)
        {
            return NativeMethods.csoundGetChannelDatasize(csound.Engine, name);
        }

        /// <summary>
        /// Returns the channel type for the channel defined by the provided name
        /// </summary>
        /// <param name="csound">A reference to the Csound6NetRealtime where the channel is defined</param>
        /// <param name="name">The name of the channel as defined to csound</param>
        /// <returns>A member of the ChannelType enumeration corresponding to the provided name</returns>
        public static ChannelType GetChannelTypeForName(Csound6NetRealtime csound, string name)
        {
            IntPtr pNothing = IntPtr.Zero;
            int type = NativeMethods.csoundGetChannelPtr(csound.Engine, out pNothing, name, 0);
            if (type > 0) return (ChannelType)(type & ChannelTypeMask);
            return ChannelType.None; //doesn't exist or memory error
        }

        /// <summary>
        /// Populates abstract superclass values supplied as from concrete channel subclasses.
        /// </summary>
        /// <param name="_name">Name the channel is known by to csound</param>
        /// <param name="_direction">one or both members (Ored to gether) of the ChannelDirection enumeration</param>
        /// <param name="csound">A reference to the Csound6NetRealtime used that contains this channel</param>
        public Csound6Channel(string _name, ChannelDirection _direction, Csound6Net csound)
        {
            m_csound = csound;
            Name = _name;
            Direction = _direction;
        }

        ~Csound6Channel()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_csound = null;
            }
            if (m_pChannel != IntPtr.Zero)
            {
                //m_pChannel is not pointing to memory we manage, so no need to release it.
                m_pChannel = IntPtr.Zero;//drop reference to csound unmanaged bus memory, csound will release on its own
            }
        }


        /// <summary>
        /// The name of the channel as known to csound
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Indicates the size, in bytes, of the data area used for this channel.
        /// Strings can vary but are 256 typically and audio arrays will vary by ksmps on a given run
        /// </summary>
        public int DataSize {  get { return NativeMethods.csoundGetChannelDatasize(m_csound.Engine, Name); } } 

        /// <summary>
        /// Indicates whether a channel is used for input, output or both
        /// </summary>
        public ChannelDirection Direction { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        internal IntPtr GetLock() { return NativeMethods.csoundGetChannelLock(m_csound.Engine, Name); }

        /// <summary>
        /// Indicates the type of data found on a channel.
        /// Subclasses should supply the value which is appropriate for its implementation.
        /// </summary>
        public abstract ChannelType Type {get;}

        /// <summary>
        /// Legacy channel access method.  Should be avoided in favor of csound 6's new thread-safe
        /// access methods as used in subclasses.
        /// Used internally by Get/SetValueDirect methods in subclasses ideally called from
        /// within the same thread between calls to PerformKsmps or PerformBuffer.
        /// If used between different threads (not recommended - use threadsafe property "Value" instead),
        /// you should acquire and use a lock (see GetLock method) to arbitrate potential race conditions.
        /// </summary>
        /// <returns>a pointer to unmanaged memory where the channel's data begins</returns>
        internal IntPtr GetChannelPointer()
        {
            if (m_pChannel == IntPtr.Zero)
            {
                int flags = ((int)Type) + (int)(((uint)Direction) << 4);
                CsoundStatus result = Csound6Net.Int2StatusEnum(NativeMethods.csoundGetChannelPtr(m_csound.Engine, out m_pChannel, Name, flags));
                if (((int)result) < 0) throw new Csound6NetException(Csound6NetException.ChannelAccessFailed, Name, result);
            }
            return m_pChannel;
        }

        /// <summary>
        /// Returns a control channel's ChannelInfo containing its name, type and direction.
        /// </summary>
        /// <returns></returns>
        public virtual ChannelInfo GetInfo()
        {
            var info = new ChannelInfo(Name, Type, Direction);
            return info;
        }

        /// <summary>
        /// Gets or sets the current value associated with this channel.
        /// Value must be of the type associated with a channel.
        /// </summary>
        public abstract object Value { get; set; }

        /**
         * @}
         */

        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int csoundGetChannelDatasize([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int csoundGetChannelPtr([In] IntPtr csound, out IntPtr pChannel, [In, MarshalAs(UnmanagedType.LPStr)] string name, [In] Int32 flags);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr csoundGetChannelLock([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name);
        }


    }

    /***********************************************************************************************************************************/

    /// <summary>
    /// Provides the concrete implementation for a csound Control Rate Channel which
    /// manages a single MYFLT (double) in csound which can be shared between it and 
    /// as host program.
    /// It is the only channel type which has a hints structure containing defaults,
    /// minimum and maximum values, etc.
    /// </summary>
    [CLSCompliant(true)]
    public class Csound6ControlChannel : Csound6Channel
    {
        protected ChannelHints m_hint;
        /**
         * \addtogroup CHANNELS
         * @{
         */

        /// <summary>
        /// Creates a control channel object using the provided name, direction and csound instanace
        /// </summary>
        /// <param name="name"></param>
        /// <param name="direction"></param>
        /// <param name="csound"></param>
        public Csound6ControlChannel(string name, ChannelDirection direction, Csound6Net csound)
            : base(name, direction, csound)
        {
            m_hint = GetControlChannelHints();
        }



        /// <summary>
        /// Creates a control channel object using the provided name, direction, channel hints and csound instance
        /// </summary>
        /// <param name="name"></param>
        /// <param name="direction"></param>
        /// <param name="hints"></param>
        /// <param name="csound"></param>
        public Csound6ControlChannel(string name, ChannelDirection direction, ChannelHints hints, Csound6Net csound)
            : base(name, direction, csound)
        {
            m_hint = hints;
            SetControlChannelHints(hints);
        }

        //~Csound6ControlChannel() //not needed since nothing to do calling Dispose(false)
        //{
        //    Dispose(false);
        //}

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                m_hint = null;
            }//no managed resources
        }



        /// <summary>
        /// The attributes field from Channel Hints
        /// </summary>
        public string Attributes
        {
            get { return GetControlChannelHints().attributes; }
            set { m_hint.attributes = value; SetControlChannelHints(m_hint); }
        }

        /// <summary>
        /// From ControlChannelHints: the kind of line between successive values: Integer, linear, exponential.
        /// </summary>
        public ChannelBehavior Behavior
        {
            get { return GetControlChannelHints().behav; }
            set { m_hint.behav = value; SetControlChannelHints(m_hint); }
        }

        /// <summary>
        /// From ControlChannelHints: the default value used for this control channel.
        /// </summary>
        public Double Default
        {
            get { return GetControlChannelHints().dflt; }
            set { m_hint.dflt = value; SetControlChannelHints(m_hint); }
        }

        /// <summary>
        /// Provides a ChannelInfo object representing csound's controlChannelInfo_t structure
        /// for this channel.
        /// Most of this data is available as properties on any channel.
        /// Unlike other channels, a control channel's info class includes a ChannelHints object.
        /// </summary>
        /// <returns></returns>
        public override ChannelInfo GetInfo()
        {
            var info = base.GetInfo();
            info.Hints = m_hint;
            return info;
        }

        /// <summary>
        /// From ControlChannelHints: the maximum value recommended for this control channel.
        /// </summary>
        public Double Maximum
        {
            get { return GetControlChannelHints().max; }
            set { m_hint.max = value; SetControlChannelHints(m_hint); }
        }

        /// <summary>
        /// From ControlChannelHints: the minimum value recommended for this control channel.
        /// </summary>
        public Double Minimum
        {
            get { return GetControlChannelHints().min; }
            set { m_hint.min = value; SetControlChannelHints(m_hint); }
        }

        /// <summary>
        /// The type of channel: for Control Channels, always ChannelType.Control
        /// </summary>
        public override ChannelType Type { get { return ChannelType.Control; } }

        /// <summary>
        /// Reports or modifies the current value of a control channel's value as seen by csound.
        /// For control channels, this must be a double value.
        /// </summary>
        public override object Value
        {
            get
            {
                int err = 0;
                double val = NativeMethods.csoundGetControlChannel(m_csound.Engine, Name, ref err);
                return (object)((err >= 0) ? val : 0.0);
            }
            set
            {
                NativeMethods.csoundSetControlChannel(m_csound.Engine, Name, (Double)value);
            }
        }

        /// <summary>
        /// Provides slightly quicker, but not threadsafe, access to a channel's current value
        /// as opposed to this class's "Value" property.
        /// Saves a hashtable lookup of the channel by name in csound's memory.
        /// Uses csoundGetChannelPtr to acquire and keep the pointer to this channel's location
        /// in csound's unmanaged memory.
        /// Best used from the same thread such as when updating channels between calls to
        /// PerformKsmps or PerformBuffer.
        /// </summary>
        /// <returns>the current control value stored in this channel</returns>
        public double GetValueDirect()
        {
            var v = new double[1];
            IntPtr pData = GetChannelPointer();//will throw exception if fails
            Marshal.Copy(pData, v, 0, 1);
            return v[0];
        }

        /// <summary>
        /// Provides slightly quicker, but not threadsafe, updates to a channel's current value
        /// as opposed to this class's threadsafe "Value" property.
        /// Saves a hashtable lookup of the channel by name in csound's memory.
        /// Uses csoundGetChannelPtr to acquire and keep the pointer to this channel's location
        /// in csound's unmanaged memory.
        /// Best used from the same thread such as when updating channels between calls to
        /// PerformKsmps or PerformBuffer.
        /// </summary>
        /// <param name="value"></param>
        public void SetValueDirect(double value)
        {
            var v = new double[1];
            v[0] = value;
            IntPtr pData = GetChannelPointer();//will throw exception if fails
            Marshal.Copy(v, 0, pData, 1);
        }

        /// <summary>
        /// Returns the current content of this channel's controlChannelHints_t structure.
        /// Most of these values are available as properties of this control channel's object.
        /// </summary>
        /// <returns>A managed version of the unmanaged controlChannelHints_t structure for this channel</returns>
        public ChannelHints GetControlChannelHints()
        {
            if (m_hint == null)
            {
                IntPtr pHints = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(NativeMethods.ChannelHintsProxy)));
                NativeMethods.csoundGetControlChannelHints(m_csound.Engine, Name, pHints);
                var proxy = (NativeMethods.ChannelHintsProxy)Marshal.PtrToStructure(pHints, typeof(NativeMethods.ChannelHintsProxy));
                m_hint = new ChannelHints((ChannelBehavior)proxy.behav, proxy.dflt, proxy.min, proxy.max);
                m_hint.x = proxy.x; m_hint.y = proxy.y; m_hint.height = proxy.height; m_hint.width = proxy.width;
                m_hint.attributes = (proxy.attributes != IntPtr.Zero) ? Marshal.PtrToStringAnsi(proxy.attributes) : null;
                Marshal.FreeHGlobal(pHints);
            }
            return m_hint;
        }

        /// <summary>
        /// Updates this channel's controlChannelHints_t structure with the provided ChannelHints
        /// contents.  Use to revise any or all of the values of a control channels hints properties.
        /// Used internally to update individual properties.
        /// </summary>
        /// <param name="hints">A ChannelHints object with the desired current values</param>
        public void SetControlChannelHints(ChannelHints hints)
        {
            m_hint = hints;
            var xprox = new NativeMethods.ChannelHintsProxy(hints);
            int result = NativeMethods.csoundSetControlChannelHints(m_csound.Engine, Name, xprox);
        }
        /**
         * @}
         */

        private class NativeMethods
        {
            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
            internal struct ChannelHintsProxy
            {
                public ChannelHintsProxy(ChannelHints hints)
                {
                    behav = (int)hints.behav;
                    dflt = hints.dflt; min = hints.min; max = hints.max;
                    x = hints.x; y = hints.y; height = hints.height; width = hints.width;
                    attributes = IntPtr.Zero;
                }

                internal int behav;
                internal double dflt;
                internal double min;
                internal double max;
                internal int x;
                internal int y;
                internal int width;
                internal int height;
                internal IntPtr attributes;
            }


            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern double csoundGetControlChannel([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name, ref int err);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundSetControlChannel(IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name, [In] double val);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int csoundGetControlChannelHints([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name, IntPtr hints);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int csoundSetControlChannelHints([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name, [In, MarshalAs(UnmanagedType.Struct)] ChannelHintsProxy hints);
        }
    }

    /***********************************************************************************************************************************/

    /// <summary>
    /// Represents an audio channel to csound.
    /// An audio channel is an array or doubles with the size of the current value of ksmps.
    /// </summary>
    public class Csound6AudioChannel : Csound6Channel
    {
        private IntPtr m_buffer = IntPtr.Zero;
        private int m_bufsiz = 0;

        public Csound6AudioChannel(string name, ChannelDirection direction, Csound6Net csound)
            : base(name, direction, csound)
        {
            m_bufsiz = csound.Ksmps;
            m_buffer = Marshal.AllocHGlobal(sizeof(double) * m_bufsiz);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {//no managed resources
            }
            //unmanaged resources
            if (m_buffer != IntPtr.Zero) Marshal.FreeHGlobal(m_buffer);
            m_buffer = IntPtr.Zero;
            base.Dispose(disposing);
        }

        /// <summary>
        /// The type of channel: for Audio Channels, always ChannelType.Audio
        /// </summary>
        public override ChannelType Type { get { return ChannelType.Audio; } }

        /// <summary>
        /// Copies an audio channel's contents to/from a managed array for use in .net algorithms.
        /// This property is the threadsafe way to move values between .net and csound's unmanaged memory.
        /// </summary>
        public override object Value
        {
            get
            {
                
                Double[] dest = new Double[Resize(m_csound.Ksmps)];//include nchnls/nchnlss_i? no, not an output channel: just a single ksmps-sized buffer
                NativeMethods.csoundGetAudioChannel(m_csound.Engine, Name, m_buffer);
                Marshal.Copy(m_buffer, dest, 0, dest.Length);
                return (object)dest;
            }
            set
            {
                Double[] source = value as double[];
                Resize(m_csound.Ksmps);
                Marshal.Copy(source, 0, m_buffer, Math.Min(source.Length, m_bufsiz));
                NativeMethods.csoundSetAudioChannel(m_csound.Engine, Name, m_buffer);
            }
        }

        //Revises the amount of memory to use as a buffer in moving between managed and unmanaged memory
        private int Resize(int newsize)
        {
            if (newsize != m_bufsiz)
            {
                if (m_buffer != IntPtr.Zero) Marshal.FreeHGlobal(m_buffer);
                m_buffer = Marshal.AllocHGlobal(newsize * sizeof(double));
                m_bufsiz = newsize;
            }
            return newsize;
        }


        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundGetAudioChannel([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name, IntPtr samples);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundSetAudioChannel([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name, IntPtr samples);
        }
    }
    /***********************************************************************************************************************************/

    /// <summary>
    /// Represents a String Channel as defined to csound.
    /// </summary>
    public class Csound6StringChannel : Csound6Channel
    {
        /// <summary>
        /// Creates a string channel object to represent a string value in csound shared with a host program
        /// </summary>
        /// <param name="name"></param>
        /// <param name="direction"></param>
        /// <param name="csound"></param>
        public Csound6StringChannel(string name, ChannelDirection direction, Csound6Net csound)
            : base(name, direction, csound)
        {
        }

        /// <summary>
        /// Copies a string channel's contents to/from  csound's unmanaged memory into a managed .net string.
        /// This property is threadsafe within csound.
        /// </summary>
        public override object Value
        {
            get
            {
                StringBuilder value = new StringBuilder(DataSize);
                NativeMethods.csoundGetStringChannel(m_csound.Engine, Name, value);
                return (object)value.ToString();
            }
            set
            {
                string val = Value.ToString();
                val = ((val.Length < DataSize) ? val : val.Substring(0, DataSize - 1));
                NativeMethods.csoundSetStringChannel(m_csound.Engine, Name, val);
            }
        }

        /// <summary>
        /// The type of channel: for String Channels, always ChannelType.String
        /// </summary>
        public override ChannelType Type { get { return ChannelType.String; } }

        /***********************************************************************************************************************************/

        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundGetStringChannel([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name, StringBuilder value);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundSetStringChannel([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name, [In, MarshalAs(UnmanagedType.LPStr)] string value);
        }
   }

    /***********************************************************************************************************************************/

    /// <summary>
    /// A channel for PVS structures to be shared via pvsin and pvsout, but not working yet in csound 6.00.1: revisit in 6.00.2 or 6.01
    /// </summary>
    //public class Csound6PvsChannel : Csound6Channel
    //{
    //    public Csound6PvsChannel(string name, ChannelDirection direction, Csound6Net csound)
    //        : base(name, direction, csound)
    //    {
    //    }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public override ChannelType Type { get { return ChannelType.Pvs; } }

    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public override object Value
    //    {
    //        get
    //        {
    //         //   IntPtr pPvs = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(PVSDATEXT)));
    //            PVSDATEXT pvs = new PVSDATEXT();
    //            NativeMethods.csoundGetPvsChannel(m_csound.Engine, pvs, Name);
    //            return null;
    //        }
    //        set
    //        {
                
    //        }
    //    }

    //   /**
    //    * PVSDATEXT is a variation on PVSDAT used in the pvs bus interface. (AUXCH* changed to pointer to float (not MYFLT)
    //    */
    //    [StructLayout(LayoutKind.Sequential)]
    //    internal class PVSDATEXT
    //    {
    //        public int N;
    //        public int sliding; /* Flag to indicate sliding case */
    //        public int NB;
    //        public int overlap;
    //        public int winsize;
    //        public int wintype;
    //        public int format;
    //        public uint framecount;
    //        public IntPtr frame; //pointer to floats: needs to be array of floats of framecount size? make proxy?
    //    } 


    //    private class NativeMethods
    //    {
    //        [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    //        internal static extern void csoundGetPvsChannel(IntPtr csound, [In, MarshalAs(UnmanagedType.LPStruct)] PVSDATEXT fout, [In, MarshalAs(UnmanagedType.LPStr)] string name);

    //        [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, BestFitMapping = false, ThrowOnUnmappableChar = true)]
    //        internal static extern void csoundSetPvsChannel(IntPtr csound, [Out, MarshalAs(UnmanagedType.LPStruct)] PVSDATEXT fin, [In, MarshalAs(UnmanagedType.LPStr)] string name);
    //    }
    //}



}
