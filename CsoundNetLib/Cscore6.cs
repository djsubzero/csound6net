using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate void CscoreCallback(IntPtr csound);

    public partial class Cscore6 : Csound6Net
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="argv"></param>
        /// <param name="inFile"></param>
        /// <param name="outFile"></param>
        public Cscore6(FileInfo inFile, FileInfo outFile)
            : this(inFile, outFile, null, CsoundInitFlag.NoFlags, null)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="argv"></param>
        /// <param name="inFile"></param>
        /// <param name="outFile"></param>
        /// <param name="hostdata"></param>
        /// <param name="flags"></param>
        public Cscore6(FileInfo inFile, FileInfo outFile, object hostdata, CsoundInitFlag flags, Csound6MessageEventHandler logger)
            :base(hostdata, flags, logger)
        {
             InitializeCscore(inFile, outFile);
        }

        /// <summary>
        /// Prepares an instance of Csound for Cscore processing outside of running an orchestra (i.e. "standalone Cscore").
        /// It is an alternative to csoundPreCompile(), csoundCompile(), and csoundPerform*()
        /// and should not be used with these functions.
        /// You must call this function before using the interface in "cscore.h" when you do not wish
        /// to compile an orchestra.
        /// Pass it the already open FILE* pointers to the input and output score files.
        /// </summary>
        /// <param name="ifile"></param>
        /// <param name="ofile"></param>
        /// <returns>CsoundStatus.Success on success and CsoundStaus.InitializationError or other error code if it fails.</returns>
        /// <returns></returns>
        public CsoundStatus InitializeCscore(FileInfo inFile, FileInfo outFile)
        {
            IntPtr pInFile = BridgeToCpInvoke.cfopen(inFile.FullName, "r");
            IntPtr pOutFile = BridgeToCpInvoke.cfopen(outFile.FullName, "w");
            CsoundStatus result = Int2StatusEnum(NativeMethods.csoundInitializeCscore(Engine, pInFile, pOutFile));
            if ((int)result < 0) throw new Csound6NetException(Csound6NetException.CscoreFailed, "Init", result);
            return result;
        }

        /// <summary>
        /// Sets an external callback for Cscore processing.
        /// Pass NULL to reset to the internal cscore() function (which does nothing).
        /// This callback is retained after a csoundReset() call.
        /// </summary>
        /// <param name="cscCallback"></param>
        public GCHandle RegisterCscoreCallback(CscoreCallback cscCallback)
        {
            GCHandle gch = FreezeCallbackInHeap(cscCallback);
            NativeMethods.csoundSetCscoreCallback(Engine, cscCallback);
            return gch;
        }
    }
}
