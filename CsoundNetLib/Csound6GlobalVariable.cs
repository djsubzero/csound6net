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
    public class Csound6GlobalVariable : IDisposable
    {

        protected Csound6Net m_csound;
        protected int m_size;
        protected string m_name;

        public Csound6GlobalVariable(string _name, int _size, Csound6Net csound)
        {
            m_csound = csound;
            Size = _size;
            Name = _name;
            if (NativeMethods.csoundCreateGlobalVariable(csound.Engine, _name, (uint)_size) != 0)
            {

            }
        }

        ~Csound6GlobalVariable()
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
            if ((m_csound != null) && !string.IsNullOrWhiteSpace(m_name))
            {
                NativeMethods.csoundDestroyGlobalVariable(m_csound.Engine, m_name);
            }
            m_csound = null;
        }

        public string Name { get; private set; }

        public int Size { get; private set; }

        protected IntPtr Address { get { return NativeMethods.csoundQueryGlobalVariableNoCheck(m_csound.Engine, Name); } }

        protected void Resize(int size)
        {
            NativeMethods.csoundDestroyGlobalVariable(m_csound.Engine, Name);
            NativeMethods.csoundCreateGlobalVariable(m_csound.Engine, Name, (uint)size);
            Size = size;
        }

        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int csoundCreateGlobalVariable([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name, [In] uint size);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern int csoundDestroyGlobalVariable([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr csoundQueryGlobalVariable([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr csoundQueryGlobalVariableNoCheck([In] IntPtr csound, [In, MarshalAs(UnmanagedType.LPStr)] string name);
        }
    }


    public class Csound6GlobalVariable<T> : Csound6GlobalVariable 
    {
        public Csound6GlobalVariable(string name, Csound6Net csound)
            : base( name, Marshal.SizeOf(typeof(T)), csound)
        {
        }

        public T Value
        {
            get {
                return (T)Marshal.PtrToStructure(Address, typeof(T));
            }
            set {
                    Marshal.StructureToPtr((T)value, Address, false);
            }
        }
    }

    public class Csound6GlobalStringVariable : Csound6GlobalVariable
    {

        public Csound6GlobalStringVariable(string name, Csound6Net csound)
            : base(name, 10, csound)
        {
        }

        public string Value
        {
            get
            {
                return (Address != IntPtr.Zero) ? Marshal.PtrToStringAnsi(Address) : string.Empty;
            }
            set
            {
                int len = value.Length+1;
                if (len > Size) Resize(len);
                byte[]s = new byte[len];
                
                ASCIIEncoding.ASCII.GetBytes(value, 0, value.Length, s, 0);
                s[s.Length - 1] = 0;
                Marshal.Copy(s, 0, Address, len);
            }
        }
    }

}
