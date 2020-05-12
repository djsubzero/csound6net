using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    /// Defines return values as defined in csound.h.
    /// Used by Compile, Perform and Initialize.
    /// </summary>
    public enum CsoundStatus
    {
        ExitJumpSuccess=256,
        Completed=1,
        Success=0, 
        BasicError=-1,
        InitializationError=-2,
        PerformanceError=-3,
        MemoryAllocationFailure=-4,
        TerminatedBySignal=-5,
        UndefinedError=-6
    }

    /// <summary>
    /// The allowed values to pass into csound upon creation.  Only used the
    /// constructors of Csound6Net and its subclasses.
    /// NoFlags is the default.
    /// </summary>
    public enum CsoundInitFlag
    {
        NoFlags=0,
        NoSignalHandler=1,
        NoAtExit=2
    }

    /// <summary>
    /// Used in parameters to request progress indicators in ascii terminal scenarios.
    /// Less useful in a GUI where a real progress bar would better be used.
    /// </summary>
    public enum HeartbeatStyle {Unspecified=0, RotatingBar=1, Dot=2, FileSize=3, Beep=4 }

    /**
     * The following constants are used with csound->FileOpen2() and
     * csound->ldmemfile2() to specify the format of a file that is being
     * opened.  This information is passed by Csound to a host's FileOpen
     * callback and does not influence the opening operation in any other
     * way. Conversion from Csound's TYP_XXX macros for audio formats to
     * CSOUND_FILETYPES values can be done with csound->type2csfiletype().
     */
    public enum CsfType {
        UnifiedCsd = 1,    /* Unified Csound document */
        Orchestra,         /* the primary orc file (may be temporary) */
        Score,             /* the primary sco file (may be temporary)
                                  or any additional score opened by Cscore */
        OrcInclude,        /* a file #included by the orchestra */
        Sco_Inclue,        /* a file #included by the score */
        ScoreOut,          /* used for score.srt, score.xtr, cscore.out */
        Scot,              /* Scot score input format */
        Options,           /* for .csoundrc and -@ flag */
        ExtractParms,      /* extraction file specified by -x */

        /* audio file types that Csound can write (10-19) or read */
        RawAudio, Ircam, Aiff, Aifc, Wave, Au, Sd2, W64, Wavex, Flac,
        Caf, Wve, Ogg, Mpc2k, Rf64, Avr, Htk, Mat4, Mat5, Nist,
        Paf, Pvf, Sds, Svx, Voc, Xi,
        UnknownAudio,     /* used when opening audio file for reading or temp file written with <CsSampleB> */

        /* miscellaneous music formats */
        Soundfont,
        StandardMidi,     /* Standard MIDI file */
        MidiSysex,        /* Raw MIDI codes, eg. SysEx dump */

        /* analysis formats */
        Hetro, HetroT,
        Pvc,               /* original PVOC format */
        Pvcex,             /* PVOC-EX format */
        CVanal,
        Lpc, Ats, Loris, Sdof, Hrtf,

        /* Types for plugins and the files they read/write */
        VstPlugin,
        LadspaPlugin,
        Snapshot,

        /* Special formats for Csound ftables or scanned synthesis
           matrices with header info */
        FTablesText,        /* for ftsave and ftload  */
        FTablesBinary,      /* for ftsave and ftload  */
        XScanuMatrix,       /* for xscanu opcode  */

        /* These are for raw lists of numbers without header info */
        FloatsText,         /* used by GEN23, GEN28, dumpk, readk */
        FloatsBinary,       /* used by dumpk, readk, etc. */
        IntegerText,        /* used by dumpk, readk, etc. */
        IntegerBinary,      /* used by dumpk, readk, etc. */

        /* image file formats */
        ImagePng,

        /* For files that don't match any of the above */
        Postscript,          /* EPS format used by graphs */
        ScriptText,         /* executable script files (eg. Python) */
        OtherText,
        OtherBinary,

        /* This should only be used internally by the original FileOpen()
           API call or for temp files written with <CsFileB> */
        Unknown = 0
    }


    /// <summary>
    /// And-able and Or-able flags to pass as arguments to the MessageLevel property
    /// of Csound6Net and Csound6Parameters.
    /// MessageLevel is a bit-field which turns options on and off; at a low level it is an unsigned integer.
    /// Amps includes amplitude messages in output, Range indicates when samples go out of range (clipping),
    /// Warnings includes warning info, dB outputs amplitude data, Color uses attributes where supported,
    /// Benchmark provides statistical data.
    /// Default is shorthand for Benchmark | Warnings | Range | Amps.
    /// None turns off logging altogether.
    /// </summary>
    [Flags] public enum MessageLevel
    {
        None=0, Amps=1, Range=2, Warnings=4, Raw=32, dB=64, Color=96, Colour=96, Benchmark=128, Default=135
    }

    public enum MessageType { Default=0, Error= 1, Orchestra=2, Realtime=3, Warning=4 }
    public enum MessageColor {Default=-1, Black=0, Red=1, Green=2, Yellow=3, Blue=4, Magenta=5, Cyan=6, White=7 }


    [Flags] public enum MessageAttributes
    {
    /* message types (only one can be specified): Mask 0x7000 */
        Default  =  0x0000, /* standard message */
        Error    =  0x1000, /* error message (initerror, perferror, etc.) */
        Orch     =  0x2000, /* orchestra opcodes (e.g. printks) */
        Realtime =  0x3000, /* for progress display and heartbeat characters */
        Warning  =  0x4000, /* warning messages */
    /* format attributes (colors etc.), use the bitwise OR of any of these: */
        Bold     =  0x0008, 
        Underline=  0x0080,

    /* Forground font colors: Mask = 107 */
        Black    =  0x0100,
        Red      =  0x0101,
        Green    =  0x0102,
        Yellow   =  0x0103,
        Blue     =  0x0104,
        Magenta  =  0x0105,
        Cyan     =  0x0106,
        White    =  0x0107,

    /* Background colors: Mask = 270  */
        BgBlack  =  0x0200,
        BgRed    =  0x0210,
        BgGreen  =  0x0220,
        BgOrange =  0x0230,
        BgBlue   =  0x0240,
        BgMagenta=  0x0250,
        BgCyan   =  0x0260,
        BgGrey   =  0x0270
    }

    [Flags] public enum KillInstanceMode
    {
        None=0,
        OldestOnly=1,
        NewestOnly=2,
        Any=3,
        SpecificSubInstance=4,
        IndefiniteDuration=8
    }

    /// <summary>
    /// 
    /// </summary>
    public enum ChannelType {
        None=0, //error return type only, meaningless in input
        Control=1,
        Audio=2,
        String=3,
        Pvs=4,
        Var=5,
    }

    [Flags] public enum ChannelDirection
    {
        Input = 1,
        Output = 2
    }


    /// <summary>
    /// 
    /// </summary>
    public enum ChannelBehavior
    {
        None=0,
        Integer=1,
        Linear=2,
        Exponential=3
    }

    public enum ScoreEventType
    {
        Note='i',
        Table='f',
        Mute='q',
        End='e',
        Advance='a'
    }

    /// <summary>
    /// Provides a typesafe way to indicate the sample format of a generated sound file.
    /// Csound6 uses strings rather than int to indicate sample format now.  This enum maps
    /// to the set of meaningful strings for sample formats.
    /// Usually, AeFloat, AeShort (16bit) and Ae24bit will be used.
    /// </summary>
    public enum SampleFormat
    {
        AeDefault=-1,
        AeAlaw = 0, AeSchar=1, AeUchar=2, AeFloat=3, AeDouble=4, AeLong=5,
        AeShort=6, AeUlaw=7, Ae24bit=8, AeVorbis=0
    }

    /// <summary>
    /// Provides a typesafe way to indicate the sound file format of a generated sound file.
    /// Csound6 uses strings rather than int to incidate the file format now.
    /// This enum maps to the set of meaningful string for file formats.
    /// Usually, TypWav or TypeAiff or TypeRaw will be used.
    /// </summary>
    public enum SoundFileType
    {
        TypDefault=-1,
        TypWav=0, TypAiff=1, TypAu=2, TypRaw=3, TypPaf=4, TypSvx=5, TypNist=6,
        TypVoc=7, TypIrcam=8, TypW64=9, TypMat4=10, TypMat5=11, TypPvf=12, TypXi=13,
        TypHtk=14, TypSds=15, TypAvr=16, TypWavex=17, TypSd2=18, TypFlac=19, TypCaf=20,
        TypWve=21, TypOgg=22, TypMpc2k=23, TypRf64=24
    }


    /// <summary>
    /// Provides for mapping from enums to strings for Sound File and Sample formats.
    /// </summary>
    public class Csound6NetConstants
    {
        public static string[] SampleFormatNames =  {
            "alaw", "schar", "uchar", "float", "double", "long", "short",
            "ulaw", "24bit", "vorbis"
        };

        //table to convert from csound internal formats: AE_SHORT etc maps to these sndfile.h values
        internal static List<Sndfile> InternalSampleFormats = new List<Sndfile>(new Sndfile[] {
            Sndfile.SF_FORMAT_ALAW, Sndfile.SF_FORMAT_PCM_S8, Sndfile.SF_FORMAT_PCM_U8, Sndfile.SF_FORMAT_FLOAT,
            Sndfile.SF_FORMAT_DOUBLE, Sndfile.SF_FORMAT_PCM_32, Sndfile.SF_FORMAT_PCM_16,
            Sndfile.SF_FORMAT_ULAW, Sndfile.SF_FORMAT_PCM_24, Sndfile.SF_FORMAT_VORBIS
        });

        public static string[] SoundFileTypeNames = {
            "wav", "aiff", "au", "raw", "paf", "svx", "nist",
            "voc", "ircam", "w64", "mat4", "mat5", "pvf", "xi",
             "htk", "sds", "avr", "wavex", "sd2", "flac", "caf",
             "wve", "ogg", "mpc2k", "rf64"
        };


        /// <summary>
        /// Convert a SampleFormat enum into its equivalent string.
        /// </summary>
        /// <param name="sf">a SampleFormat value</param>
        /// <returns>The corresponding string expected by csound 6</returns>
        public static string AsFormatString(SampleFormat sf)
        {
            return (sf == SampleFormat.AeDefault) ? null : SampleFormatNames[(int)sf];
        }

        /// <summary>
        /// Convert a raw integer from csound (assumed to be valid sample format: AE_SHORT etc)
        /// into our indexed version used for strings.
        /// Permits a consistent enum for external C# use.
        /// </summary>
        /// <param name="csoundFmt"></param>
        /// <returns></returns>
        public static SampleFormat FromCsoundInternal(int csoundFmt)
        {
            Sndfile internalfmt = (Sndfile)csoundFmt;
            int pos = InternalSampleFormats.IndexOf(internalfmt);
            return (pos >= 0) ? (SampleFormat)pos : SampleFormat.AeDefault;
        }

        /// <summary>
        /// Convert a SoundFileType into its equivalent string.
        /// </summary>
        /// <param name="type">a SoundFileType value</param>
        /// <returns>The corresponding string expected by csound 6</returns>
        public static string AsSoundFileTypeString(SoundFileType type)
        {
            return (type == SoundFileType.TypDefault) ? null : SoundFileTypeNames[(int)type];
        }
    }

        //#define AE_CHAR         SF_FORMAT_PCM_S8
        //#define AE_SHORT        SF_FORMAT_PCM_16
        //#define AE_24INT        SF_FORMAT_PCM_24
        //#define AE_LONG         SF_FORMAT_PCM_32
        //#define AE_UNCH         SF_FORMAT_PCM_U8
        //#define AE_FLOAT        SF_FORMAT_FLOAT
        //#define AE_DOUBLE       SF_FORMAT_DOUBLE
        //#define AE_ULAW         SF_FORMAT_ULAW
        //#define AE_ALAW         SF_FORMAT_ALAW
        //#define AE_VORBIS       SF_FORMAT_VORBIS

        internal enum Sndfile {
            SF_FORMAT_PCM_S8    = 0x0001,		/* Signed 8 bit data */
            SF_FORMAT_PCM_16    = 0x0002,		/* Signed 16 bit data */
            SF_FORMAT_PCM_24    = 0x0003,		/* Signed 24 bit data */
            SF_FORMAT_PCM_32    = 0x0004,		/* Signed 32 bit data */

            SF_FORMAT_PCM_U8    = 0x0005,		/* Unsigned 8 bit data (WAV and RAW only) */

            SF_FORMAT_FLOAT     = 0x0006,		/* 32 bit float data */
            SF_FORMAT_DOUBLE    = 0x0007,		/* 64 bit float data */

            SF_FORMAT_ULAW      = 0x0010,		/* U-Law encoded. */
            SF_FORMAT_ALAW      = 0x0011,		/* A-Law encoded. */
            SF_FORMAT_VORBIS    = 0x0060,		/* Xiph Vorbis encoding. */
        }



}
