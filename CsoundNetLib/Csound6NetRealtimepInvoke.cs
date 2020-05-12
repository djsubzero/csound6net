using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.ComponentModel;

namespace csound6netlib
{

    //See Csound6Net.cs for copyright and license for this class

    /// <summary>
    /// Provides the pInvoke linkages to csound64.dll for realtime audio/midi signatures.
    /// Access is only available via C#/.net wrapper routines for safety and convenience.
    /// Comments are primarily taken from csound.h to recall how to use each function.
    /// </summary>
    public partial class Csound6NetRealtime
    {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate void ChannelCallbackProxy(IntPtr csound, string channelName, IntPtr pData, IntPtr pCsType);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate void SenseEventCallbackProxy(IntPtr csound, IntPtr userdata);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        internal delegate int KeyboardCallbackProxy(IntPtr userData, IntPtr p, uint type);

        /// <summary>
        /// Private proxy class used during marshalling of actual ChannelInfo 
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private class ChannelInfoProxy
        {
            public IntPtr name;
            public int type;
            [MarshalAs(UnmanagedType.Struct)]
            public ChannelHintsProxy hints;
        }

        /// <summary>
        /// Private proxy class used during marshalling of Channel Hints for Krate channels.
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct ChannelHintsProxy
        {
            public ChannelHintsProxy(ChannelHints hints)
            {
                behav = (int)hints.behav;
                dflt = hints.dflt; min = hints.min; max = hints.max;
                x = hints.x; y = hints.y; height = hints.height; width = hints.width;
                attributes = IntPtr.Zero;
            }
            public int behav;
            public double dflt;
            public double min;
            public double max;
            public int x;
            public int y;
            public int width;
            public int height;
            public IntPtr attributes;
        }

        /// <summary>
        /// Based upon best practice recommendations, the unmanaged csound api signatures are wrapped in a separate class
        /// called "NativeMethods" and given private class with internal method visibility to limit access just to the class
        /// which wraps these methods and presents managed signatures to the outside world.
        /// </summary>
        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern Int32 csoundListChannels([In] IntPtr csound, [Out] out IntPtr ppChannels);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void csoundDeleteChannelList([In] IntPtr csound, [In] IntPtr ppChannels);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void csoundSetInputChannelCallback(IntPtr csound, ChannelCallbackProxy channelCB);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void csoundSetOutputChannelCallback([In] IntPtr csound, [In] ChannelCallbackProxy channelCB);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern int csoundScoreEvent([In] IntPtr csound, [In] char type, double[] fields, [In] int numFields);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern int csoundScoreEventAbsolute([In] IntPtr csound, [In] char type, ref double[] fields, [In] int numFields, [In] double time_ofs);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern void csoundInputMessage([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string message);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern int csoundKillInstance([In] IntPtr csound, [In] double instr, [In] IntPtr instrName, int mode, int allow_release);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern int csoundRegisterSenseEventCallback([In] IntPtr csound, SenseEventCallbackProxy senseEventProxy);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void csoundKeyPress([In] IntPtr csound, [In] char c);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern int csoundRegisterKeyboardCallback([In] IntPtr csound, [In] KeyboardCallbackProxy keyboardCallback, [In] IntPtr userData, [In] uint type);

            [DllImport(_dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            internal static extern void csoundRemoveKeyboardCallback([In] IntPtr csound, KeyboardCallbackProxy keyboardCallback);

        }
    }
}
