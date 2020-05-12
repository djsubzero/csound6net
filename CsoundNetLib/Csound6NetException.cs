using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace csound6netlib
{
/*
 * C S O U N D 6 N E T
 * Dot Net Wrappers for building C#/VB hosts for Csound 6 via the Csound API.
 * Copyright (C) 2013 Richard Henninger
 *
 */
    /// <summary>
    /// Base exception for converting csound failures into .net exceptions.
    /// </summary>
    [Serializable]
    public class Csound6NetException : Exception
    {

        internal const string InitFailed = "InitFailed";
        internal const string CreateFailed = "CreateFailed";
        internal const string PerformKsmpsFailed = "PerformKsmpsFailed";
        internal const string PerformBufferFailed = "PerformBufferFailed";
        internal const string CsoundEngineMismatch = "CsoundEngineMismatch";
        internal const string ScoreFailed = "ScoreFailed";
        internal const string SortScoreFailed = "SortScoreFailed";
        internal const string ExtractFileFailed = "ExtractFileFailed";
        internal const string CscoreFailed = "CscoreFailed";
        internal const string ChannelAccessFailed = "ChannelAccessFailed";
        internal const string CompileFailed = "CompileFailed";
        internal const string StartFailed = "StartFailed";
        internal const string PerformFailed = "PerformFailed";
        internal const string CleanupFailed = "CleanupFailed";

        /// <summary>
        /// Main constructor for creating a Csound6NetException.
        /// Messages from csound typically are communicated on the console or into a logfile,
        /// so the message here may not be all that descriptive.
        /// The status code is the value returned from csound cast as an enum member.
        /// </summary>
        /// <param name="msg">The text of the exception</param>
        /// <param name="status">the raw csound failure return value cast as an enumeration</param>
        public Csound6NetException(string msg, CsoundStatus _status) 
            :base(string.Format(((msg.IndexOf(" ") < 0) ? Csound6Net.c_rm.GetString(msg) : msg), _status))
        {
            Status = _status;
        }

        public Csound6NetException(string msg, string value)
            : base(string.Format(((msg.IndexOf(" ") < 0) ? Csound6Net.c_rm.GetString(msg) : msg), value))
        {
            Status = CsoundStatus.UndefinedError;
        }

        public Csound6NetException(string msg, string value, CsoundStatus _status)
            : base(string.Format(((msg.IndexOf(" ") < 0) ? Csound6Net.c_rm.GetString(msg) : msg), value, _status))
        {
            Status = _status;
        }

        public CsoundStatus Status;

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);//override required, but nothing worth serializing in this subclass
        }

    }

    [Serializable]
    public class Csound6CompilerException : Csound6NetException
    {
        public Csound6CompilerException(string msg, CsoundStatus _status)
            : base(msg, _status)
        {
        }
    }

    [Serializable]
    public class Csound6ScoreException : Csound6NetException
    {
        public Csound6ScoreException(string msg, CsoundStatus _status)
            : base(msg, _status)
        {
        }
    }

    [Serializable]
    public class Csound6PerformanceException : Csound6NetException
    {
        public Csound6PerformanceException(string msg, CsoundStatus _status)
            : base(msg, _status)
        {
        }
   }

}
