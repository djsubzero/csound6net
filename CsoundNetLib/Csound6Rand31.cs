using System;
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
    /// Simple linear congruential random number generator:
    /// a very fast and basic Pseudo Random Number Generator with a range of
    /// any positive value in a 32-bit integer (0-Int32.MaxValue).
    /// </summary>
    public class Csound6Rand31
    {
        private Int32 m_seed; //this class owns the evolving 31-bit seed as it transforms

        /// <summary>
        /// Creates a PRNG using the current date/time as a seed
        /// </summary>
        public Csound6Rand31()
            :this (0) 
        {
        }

        /// <summary>
        /// Seeds the PRNG with the provided seed.
        /// The initial value of *seedVal must be in the range 1 to 2147483646.
        /// If seed is negative or zero, the current date/time is used as a seed.
        /// </summary>
        /// <param name="seed">Any integer value between 1 to 2147483646</param>
        public Csound6Rand31(int seed)
        {
            m_seed = (seed & 0x7fffffff);
            if (m_seed == 0) m_seed = (int)(Csound6Net.GetRandomSeedFromTime() & 0x7fffffff);//keep random uint within range for int
        }

        /// <summary>
        /// The next random number in its series.
        /// Updates newSeed to be previousSeed * 742938285 % 2147483647
        /// </summary>
        /// <returns>new seed (new random number)</returns>
        public int next()
        {
            return NativeMethods.csoundRand31(out m_seed);
        }

        private class NativeMethods
        {
            /* Simple linear congruential random number generator: (*seedVal) = (*seedVal) * 742938285 % 2147483647
             * The initial value of *seedVal must be in the range 1 to 2147483646.
             * Returns the next number from the pseudo-random sequence, in the range 1 to 2147483646.
             */
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundRand31(out Int32 seedVal);
        }


    }
}
