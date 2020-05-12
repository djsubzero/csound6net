using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

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


namespace csound6netlib
{


    /// <summary>
    /// Provides access to stdio.h routines needed in csound.h calls: specifically,
    /// fopen (and related), and formatted print routines.
    /// </summary>
    [CLSCompliant(true)]
    public class BridgeToCpInvoke
    {

        internal const string _msvcrtVersion = "msvcr110.dll"; //should always match the current msvcrt version sent with csound

        /// <summary>
        /// Takes a format and valist (which must have created elsewhere (often within csound
        /// itself - as in MessageCallbacks) and produces a string with variables from va_list
        /// formatted into the format string according to 'c' conventions rather than C# conventions
        /// which are quite different.
        /// </summary>
        /// <param name="format">a format string following c/c++ conventions</param>
        /// <param name="valist">a previously created va_list macros typically passed in from csound itself</param>
        /// <returns>the formatted string or null if the method failed to produce a string</returns>
        public static string cvsprintf(string format, IntPtr valist)
        {
            int size = NativeMethods._vscprintf(format, valist);//predict how big a buffer we will need to hold format and its arguments
            StringBuilder text = new StringBuilder(size+1);//needs padding: builders usually expand, but not when c-code fills them...
            int cnt = NativeMethods.vsprintf_s(text, text.Capacity, format, valist); //Convert to a string
            return (cnt >= 0) ? text.ToString() : string.Empty; //and return (cnt < 0 means error: just return null string
        }

        /// <summary>
        /// Opens a file using c-style stdio.h.
        /// Specifically, it is used in csound calls to get a FILE* as required as input arguments
        /// by several function headers in csound.h.
        /// Use the returned IntPtr where FILE* is indicated.
        /// </summary>
        /// <param name="name">the short file path to the file to open</param>
        /// <param name="mode">same mode arguments as needed by f</param>
        /// <returns></returns>
        public static IntPtr cfopen(string name, string mode)
        {
            return NativeMethods.fopen(name, mode);
        }

        /// <summary>
        /// Closes a file previously opened via cfopen.
        /// </summary>
        /// <param name="pFILE">the IntPtr (FILE*) returned by cfopen when the file was opened</param>
        /// <returns></returns>
        public static int cfclose(IntPtr pFILE)
        {
            return NativeMethods.fclose(pFILE);
        }

        /// <summary>
        /// Calls the crt-lib fgets function for reading ascii characters from an file opened with cfopen.
        /// Reads up to the next newline character or the provided limit.
        /// </summary>
        /// <param name="pFILE">the IntPtr provided when the file to be read was opened</param>
        /// <param name="maxchrs">the maximum number of characters to read if a newline isn't found</param>
        /// <returns>the string to be read or the next maxchrs characters</returns>
        public static string cfgets(IntPtr pFILE, int maxchrs)
        {
            StringBuilder cBuffer = new StringBuilder(maxchrs);
            IntPtr pStr = NativeMethods.fgets(cBuffer, maxchrs, pFILE);
            return ((pStr != null) && (pStr != IntPtr.Zero)) ? cBuffer.ToString() : null;
        }

        /// <summary>
        /// Converts nodes in the provided path to their non-space dos-style equivalent.
        /// Satisfies csound file readings need to process paths without spaces which, after all,
        /// is would not be platorm independent.
        /// Calls the GetShortPathName function in kernal32.dll.
        /// </summary>
        /// <param name="longname"></param>
        /// <returns></returns>
        public static string wGetShortPathName(string longname)
        {
            StringBuilder shortname = new StringBuilder(longname.Length);
            Int32 shortLength = NativeMethods.GetShortPathName(longname, shortname, longname.Length);
            string result = shortname.ToString();
            return result;
        }

        private class NativeMethods
        {
            [DllImport(_msvcrtVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr fopen([In, MarshalAs(UnmanagedType.LPStr)] string name, [In, MarshalAs(UnmanagedType.LPStr)] string mode);

            [DllImport(_msvcrtVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern IntPtr fgets(StringBuilder str, Int32 maxchars, IntPtr pFile);


            [DllImport(_msvcrtVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 fclose(IntPtr pFile);

            [DllImport(_msvcrtVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern Int32 _vscprintf([In, MarshalAs(UnmanagedType.LPStr)] string format, IntPtr valist);

            [DllImport(_msvcrtVersion, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern Int32 vsprintf_s(StringBuilder str, int len, string format, IntPtr valist);

            [DllImport("Kernel32.dll", CharSet = CharSet.Ansi, BestFitMapping = false, ThrowOnUnmappableChar = true)]
            internal static extern Int32 GetShortPathName([In, MarshalAs(UnmanagedType.LPStr)] string longname,  [MarshalAs(UnmanagedType.LPStr)] StringBuilder shortname, [In] Int32 length);
        }

    }
}
