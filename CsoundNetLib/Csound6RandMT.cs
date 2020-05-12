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
    /// Mersenne Twister (MT19937) pseudo-random number generator.
    /// Owns a pinned, unmanaged block of memory for csound to keep state of random number generator
    /// between calls
    /// </summary>
    public class Csound6RandMT : IDisposable
    {
        IntPtr m_pMtState = IntPtr.Zero;

        /// <summary>
        /// 
        /// </summary>
        public Csound6RandMT() :
            this((Int32)Csound6Net.GetRandomSeedFromTime())
        {
        }

        public Csound6RandMT(Int32 seedValue)
        {
            m_pMtState = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * 625);
            NativeMethods.csoundSeedRandMT(m_pMtState, IntPtr.Zero, (uint)seedValue);
        }

        ~Csound6RandMT()
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
            //no managed resources, so state of disposing mute.
            if (m_pMtState != IntPtr.Zero) Marshal.FreeHGlobal(m_pMtState);
            m_pMtState = IntPtr.Zero;
        }

        public Csound6RandMT(int[] initKey)
        {
            m_pMtState = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)) * 625);
            IntPtr pInitKey = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(UInt32)) * initKey.Length);
            Marshal.Copy(initKey, 0, pInitKey, initKey.Length);
            NativeMethods.csoundSeedRandMT(m_pMtState, pInitKey, (UInt32)initKey.Length);
            Marshal.FreeHGlobal(pInitKey);
        }

        public Int32 Next()
        {
            return (Int32)NativeMethods.csoundRandMT(m_pMtState);
        }



        private class NativeMethods
        {
            /* Initialise Mersenne Twister (MT19937) random number generator, using 'keyLength' unsigned 32 bit values from 'initKey' as seed.
             * If the array is NULL, the length parameter is used for seeding.
             */
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundSeedRandMT(IntPtr pMtState, IntPtr initKey, UInt32 keyLength);

            /* Returns next random number from MT19937 generator.
             * The PRNG must be initialised first by calling csoundSeedRandMT().
             */
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern UInt32 csoundRandMT(IntPtr pMtState);
        }

    }
}
