using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
    /// Managed version of Unmanaged structure: RTCCLOCK as used in Timer Routines
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    internal class RTCLOCK
    {
        long starttime_real;
        long starttime_CPU;
    }

    /// <summary>
    /// 
    /// </summary>
    public class Csound6Timer
    {
        private RTCLOCK m_rtClock = new RTCLOCK();

        public Csound6Timer()
        {
            Reset();
        }

        /// <summary>
        /// the elapsed real time (in seconds) since this timer was created.
        /// </summary>
        public double RealTime
        {
            get { return NativeMethods.csoundGetRealTime(m_rtClock); }
        }

        /// <summary>
        /// the elapsed CPU time (in seconds) since this timer was created.
        /// </summary>
        public double CPUTime
        {
            get { return NativeMethods.csoundGetCPUTime(m_rtClock); }
        }

        /// <summary>
        /// (Re)Initializes this timer back to a beginning state.
        /// </summary>
        public void Reset()
        {
            NativeMethods.csoundInitTimerStruct(m_rtClock);
        }

        private class NativeMethods
        {
            /// <summary>
            /// Initialise a timer structure.
            /// </summary>
            /// <param name="rtClock"></param>
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundInitTimerStruct([MarshalAs(UnmanagedType.LPStruct)]RTCLOCK rtClock);

            /// <summary>
            /// 
            /// </summary>
            /// <param name="rtClock"></param>
            /// <returns>the elapsed real time (in seconds) since the specified timer structure was initialised.</returns>
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern double csoundGetRealTime([MarshalAs(UnmanagedType.LPStruct)] RTCLOCK rtClock);

            /// <summary>
            /// 
            /// </summary>
            /// <param name="rtClocck"></param>
            /// <returns>the elapsed CPU time (in seconds) since the specified timer structure was initialised.</returns>
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern double csoundGetCPUTime([MarshalAs(UnmanagedType.LPStruct)] RTCLOCK rtClocck);
        }
    }

}
