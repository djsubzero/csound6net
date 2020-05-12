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
    public class Csound6CircularBuffer : IDisposable
    {
        private int m_size;
        private IntPtr m_buffer = IntPtr.Zero;
        private Csound6Net m_csound;

        public Csound6CircularBuffer(int size, int items, Csound6Net csound)
        {
            m_size = size;
            m_csound = csound;
            NativeMethods.csoundCreateCircularBuffer(csound.Engine, size, items);
        }

        ~Csound6CircularBuffer()
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
            if ((m_csound != null) && (m_buffer != IntPtr.Zero))
            {
                NativeMethods.csoundDestroyCircularBuffer(m_csound.Engine, m_buffer);
                m_buffer = IntPtr.Zero;
            }
            m_csound = null;//dereference
        }

        public int Size { get { return m_size; } }

        public void Clear()
        {
            NativeMethods.csoundFlushCircularBuffer(m_csound.Engine, m_buffer);
        }

        public void Read(ref double[] items)
        {
        }

        public void Write(double[] items)
        {
        }


        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr csoundCreateCircularBuffer([In] IntPtr csound, [In] int numelem, [In] int size);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundDestroyCircularBuffer([In] IntPtr csound, [In] IntPtr p);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int csoundPeekCircularBuffer([In] IntPtr csound, [In] IntPtr circular_buffer, IntPtr dest, [In] int items);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int csoundReadCircularBuffer([In] IntPtr csound, IntPtr circular_buffer, IntPtr dest, int items);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int csoundWriteCircularBuffer([In] IntPtr csound, IntPtr p, [In] IntPtr inp, int items);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundFlushCircularBuffer([In] IntPtr csound, IntPtr p);
        }
    }
}
