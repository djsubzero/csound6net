using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    [StructLayout(LayoutKind.Sequential)]
    public class CSOUND_PARAMS
    {
        public int debug_mode;     /* debug mode, 0 or 1 */
        public int buffer_frames;  /* number of frames in in/out buffers */
        public int hardware_buffer_frames; /* ibid. hardware */
        public int displays;       /* graph displays, 0 or 1 */
        public int ascii_graphs;   /* use ASCII graphs, 0 or 1 */
        public int postscript_graphs; /* use postscript graphs, 0 or 1 */
        public int message_level;     /* message printout control */
        public int tempo;             /* tempo (sets Beatmode)  */
        public int ring_bell;         /* bell, 0 or 1 */
        public int use_cscore;        /* use cscore for processing */
        public int terminate_on_midi; /* terminate performance at the end
                                        of midifile, 0 or 1 */
        public int heartbeat;         /* print heart beat, 0 or 1 */
        public int defer_gen01_load;  /* defer GEN01 load, 0 or 1 */
        public int midi_key;           /* pfield to map midi key no */
        public int midi_key_cps;       /* pfield to map midi key no as cps */
        public int midi_key_oct;       /* pfield to map midi key no as oct */
        public int midi_key_pch;       /* pfield to map midi key no as pch */
        public int midi_velocity;      /* pfield to map midi velocity */
        public int midi_velocity_amp;   /* pfield to map midi velocity as amplitude */
        public int no_default_paths;     /* disable relative paths from files, 0 or 1 */
        public int number_of_threads;   /* number of threads for multicore performance */
        public int syntax_check_only;   /* do not compile, only check syntax */
        public int csd_line_counts;     /* csd line error reporting */
        public int compute_weights;     /* use calculated opcode weights for
                                          multicore, 0 or 1  */
        public int realtime_mode;       /* use realtime priority mode, 0 or 1 */
        public int sample_accurate;     /* use sample-level score event accuracy */
        public double sample_rate_override; /* overriding sample rate */
        public double control_rate_override; /* overriding control rate */
        public int nchnls_override;     /* overriding number of out channels */
        public int nchnls_i_override;   /* overriding number of in channels */
        public double e0dbfs_override;  /* overriding 0dbfs */
    }
   
    /**
     * Audio Device information for a given audio module
     */
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
    public class CS_AUDIODEVICE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst=64)]
        public string device_name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string device_id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string rt_module;

        public int max_nchnls;

        [MarshalAs(UnmanagedType.Bool)]
        public bool isOutput;
    }

    /*
     * Midi divice information for a given midi module
     */
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public class CS_MIDIDEVICE
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string device_name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string interface_name;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string device_id;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string midi_module;

        [MarshalAs(UnmanagedType.Bool)]
        public bool isOutput;
    }

    /**
     * Real-time audio parameters structure
     */
    public class csRtAudioParams {

        public string devName;              /** device name (NULL/empty: default) */
        public int devNum;                  /** device number (0-1023), 1024: default */
        public int bufSamp_SW;              /** buffer fragment size (-b) in sample frames */
        public int bufSamp_HW;              /** total buffer size (-B) in sample frames */
        public int nChannels;               /** number of channels */
        public SampleFormat sampleFormat;   /** sample format (AE_SHORT etc.) */
        public float sampleRate;            /** sample rate in Hz */
    }

    [CLSCompliant(true)]
      public class ChannelInfo {
          public ChannelInfo(string _name, ChannelType _type, ChannelDirection _direction)
          {
              Name = _name;
              Type = _type;
              Direction = _direction;
          }
            public string       Name;
            public ChannelType  Type;
            public ChannelDirection Direction;
            public ChannelHints Hints;
        };

    /// <summary>
    /// This structure holds the parameter hints for control channels.
    /// </summary>
    [CLSCompliant(true)]
    public class ChannelHints
    {
        /// <summary>
        /// Creates an empty hint by calling main constructor with all zeros
        /// </summary>
        public ChannelHints() : this(ChannelBehavior.None, 0, 0, 0)
        {
        }

        /// <summary>
        /// Creates a channel hint initialized with the most common Control Channel values as provided.
        /// </summary>
        /// <param name="ibehav">Linear, Exponential or </param>
        /// <param name="idflt"></param>
        /// <param name="imin"></param>
        /// <param name="imax"></param>
        public ChannelHints(ChannelBehavior ibehav, double idflt, double imin, double imax)
        {
            behav = ibehav;
            dflt = idflt;
            min = imin;
            max = imax;
            x = 0;
            y = 0;
            width = 0;
            height = 0;
            attributes = null;
        }

        public ChannelBehavior behav;
        public double   dflt;
        public double   min;
        public double   max;
        public int      x;
        public int      y;
        public int      width;
        public int      height;
        public string   attributes;
    }




    /// <summary>
    /// Used in inval and outval callbacks
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
    public class CS_TYPE {
        [MarshalAs(UnmanagedType.LPStr)]
        public string varTypeName;
        [MarshalAs(UnmanagedType.LPStr)]
        public string varDescription;
        public int argtype;         // used to denote if allowed as in-arg=1, out-arg=2, or both=0 (we'll see both usually)
        internal IntPtr csvariable;   // struct csvariable* (*createVariable)(void*, void*); used to create variables - we can ignore
        internal IntPtr cstype;       // struct cstype** unionTypes;  will be NULL for anything we get
    }


    /// <summary>
    /// 
    /// </summary>
    public class OpcodeArgumentTypes
    {
        public string outypes;
        public string intypes;
    }


    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
    public class ORCTOKEN
    {
        public Int32    type;       //int              type
        public string   lexeme;     //char             *lexeme;
        public Int32    value;      //int              value;
        public double   fvalue;     //double           fvalue;
        public ORCTOKEN next;       //struct ORCTOKEN  *next;
    }

    
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
    public class TREE
    {
        public Int32    type;   //int           type;
        public ORCTOKEN value;  //ORCTOKEN     *value;
        public Int32    rate;   //int           rate;
        public Int32    len;    //int           len;
        public Int32    line;   //int           line;
        public Int32    locn;   //int           locn;
        public TREE     left;   //struct TREE  *left;
        public TREE     right;  //struct TREE  *right;
        public TREE     next;   //struct TREE   *next;
        internal IntPtr   markup; //        void          *markup;  // TEMPORARY - used by semantic checker to
                                // markup node adds OENTRY or synthetic var
                                // names to expression nodes should be moved
                                // to TYPE_TABLE
    }




}
