using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Represents an handle to an actual table (function) in the csound engine.
    /// Any value read from or written to is to the actual function table in csound itself.
    /// A copy of a table's current contents can be taken for local examination, but it is not
    /// possible to reload that copy back into csound (except cell by cell using the indexer).
    /// Access the tables live values via the getter/setter via this class's indexer:
    /// double cell = csndTable[n] or csndTable[n] = doublevalue where n is >= 0 and less than Length.
    /// </summary>
    public class Csound6Table
    {
        private Csound6Net m_csound64;
        private int m_cachedLength = -1;

        /**
         * \addtogroup TABLES
         */
        /// <summary>
        /// Creates a table (proxy) to front an actual table (function) in csound.
        /// This object can be used to manage table values in csound or to take copies for local examination.
        /// </summary>
        /// <param name="fNbr">the function number as defined in a csound score (more than 1)</param>
        /// <param name="csound64"></param>
        public Csound6Table(int fNbr, Csound6Net csound64)
        {
            m_csound64 = csound64;
            Fnumber = fNbr;
            if ((fNbr > 0) && (csound64 != null))
            {
                m_cachedLength = NativeMethods.csoundTableLength(m_csound64.Engine, Fnumber);
            }
        }

        /**
        * \ingroup TABLES
        */
        /// <summary>
        /// The function number of the table as known to csound: should be greater than 0 and less
        /// than or equal to the number currently defined in a score at the moment of querying.
        /// </summary>
        public int Fnumber { get; private set; }

        /**
        * \ingroup TABLES
        */ 
        /// <summary>
        /// Indicates whether this Csound6Table is actaully associated with an underlying csound table.
        /// </summary>
        public bool IsDefined
        {
            get { return (Length >= 0); }
        }

      /**
       * \ingroup TABLES
       */
        /// <summary>
        /// Indicates how many cells this table/function contains in csound.
        /// If negative, then the table is not currently defined in a score at the moment of query.
        /// </summary>
        public int Length { get { return m_cachedLength; } }
        
       /**
        * \ingroup TABLES
        */
        /// <summary>
        /// Allows access to indivitual cells of a table using array notation (mytable[100] = newValue;)
        /// Idiomatic C# way to call csoundTableGet and csoundTableSet.
        /// </summary>
        /// <param name="index">the position in the table (zero based) to get or set</param>
        /// <returns>for getter, the value at the requested position</returns>
        public Double this[int index]
        {
            get
            {   
                return NativeMethods.csoundTableGet(m_csound64.Engine, Fnumber, index) ;
            }
            set 
            { 
                NativeMethods.csoundTableSet(m_csound64.Engine, Fnumber, index, value); 
            }
        }

        /**
         * \ingroup TABLES
         */
        /// <summary>
        /// Retrieves the current contents of csounds table represented by this table
        /// into a managed double array for use in the .net environment.
        /// This threadsafe version uses the csoundTableCopyOut function which is new for csound 6.0.
        /// It is preferred over the Copy method which uses the legacy csoundGetTable method
        /// which is not threadsafe.
        /// The results are identical if threading doesn't mess up the legacy version.
        /// </summary>
        /// <returns></returns>
        public Double[] CopyOut()
        {
            Double[] copy = new Double[Length];
            IntPtr dest = Marshal.AllocHGlobal(sizeof(double) * copy.Length);
            NativeMethods.csoundTableCopyOut(m_csound64.Engine, Fnumber, dest);
            Marshal.Copy(dest, copy, 0, copy.Length);
            Marshal.FreeHGlobal(dest);
            return copy;
        }

        /**
         * \ingroup TABLES
         */
        /// <summary>
        /// Copys the provided array of doubles into an existing table in csound.
        /// This threadsave version uses the csoundTableCopyIn function which is new with csound 6.0
        /// and is the preferred way to modify an existing table.
        /// Needless to say, results are unpredictable if the array and the table
        /// are mismatched in length.
        /// </summary>
        /// <param name="values"></param>
        public void CopyIn(double[] values)
        {
            IntPtr source = Marshal.AllocHGlobal(sizeof(double) * values.Length);
            Marshal.Copy(values, 0, source, values.Length);
            NativeMethods.csoundTableCopyIn(m_csound64.Engine, Fnumber, source);
            Marshal.FreeHGlobal(source);
        }

       /**
        * \ingroup TABLES
        */
        /// <summary>
        /// Produces a managed copy of this table containing its current contents in csound 
        /// at the time of copying.
        /// This version uses the legacy csoundGetTable function which is not threadsafe.
        /// Use the threadsafe CopyOut and CopyIn methods which are new with csound 6.0.
        /// </summary>
        /// <returns>an array of doubles exactly mirroring this function's current contents at the time of copying</returns>
        public Double[] Copy()
        {
            Double[] copy = new Double[Length];
            IntPtr unmanagedAdr = new IntPtr();
            int result = NativeMethods.csoundGetTable(m_csound64.Engine, out unmanagedAdr, Fnumber);
            Marshal.Copy(unmanagedAdr, copy, 0, Length);
            return copy;
        }


    /****************************************************************************************************************/
    /***********************  private pInvoke definitions for accessing csound's "C" API   **************************/
    /****************************************************************************************************************/
    /****************************************************************************************************************/

        private class NativeMethods
        {
            /* Returns the length of a function table (not including the guard point),
             *  or -1 if the table does not exist.
             */
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundTableLength([In] IntPtr csound, [In] Int32 table);

            /* Returns the value of a slot in a function table.
             * The table number and index are assumed to be valid.
             */
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Double csoundTableGet([In] IntPtr csound, [In] Int32 table, [In] Int32 index);

            /* Sets the value of a slot in a function table.
             * The table number and index are assumed to be valid.
             */
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundTableSet([In] IntPtr csound, [In] Int32 table, [In] Int32 index, [In] Double value);

            /* Stores pointer to function table 'tableNum' in *tablePtr, and returns the table length (not including the guard point).
             * If the table does not exist, *tablePtr is set to NULL and -1 is returned.
             */
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern Int32 csoundGetTable([In] IntPtr csound, out IntPtr tablePtr, [In] Int32 tableNum);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundTableCopyIn([In] IntPtr csound, [In] int table, [In] IntPtr src);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundTableCopyOut([In] IntPtr csound, int table, IntPtr dest);
        }

    }
}
