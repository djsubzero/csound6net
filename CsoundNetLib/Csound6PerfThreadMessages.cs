using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
/*
    Adapted to C# by Richard Henninger from csPerfThread.hpp/cpp by Istvan Varga
    Copyright (C) 2013 by Richard Henninger and is part of Csound6NetLib.

    csPerfThread.hpp/cpp: Copyright (C) 2005 Istvan Varga

    csPerfThread is part of Csound.

    The Csound Library and Csound6NetLib is free software; you can redistribute it
    and/or modify it under the terms of the GNU Lesser General Public
    License as published by the Free Software Foundation; either
    version 2.1 of the License, or (at your option) any later version.

    Csound is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public
    License along with Csound; if not, write to the Free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA
    02111-1307 USA
*/


namespace csound6netlib
{
    /// <summary>
    /// Abstract base class for all messages providing link to performance thread
    /// sending these messages as well as the csound instance associated with it.
    /// </summary>
    public abstract class Csound6PerfThreadMessage
    {
        private Csound6PerformanceThread m_pt;

        public Csound6PerfThreadMessage(Csound6PerformanceThread pt)
        {
            m_pt = pt;
        }

        /// <summary>
        /// Indicates/Sets Paused state of the the performance thread object that this message is tied to.
        /// </summary>
        protected bool IsPaused
        {
            get { return m_pt.IsPaused; }
            set { m_pt.IsPaused = value; }
        }

        /// <summary>
        /// Makes the Csound engine which is tied to executing these messages visible to these messages
        /// </summary>
        protected Csound6NetRealtime Csound { get { return m_pt.Csound; } }

        /// <summary>
        /// Indicates/sets the Status of the performance thread that this message is tied to.
        /// </summary>
        protected CsoundStatus Status { get { return m_pt.Status; } set { m_pt.Status = value; } }

        /// <summary>
        /// Subclasses (actual queue messages) implement their behavior by overriding this method.
        /// Subclasses indicate whether the thread is still alive by returning false (done != true)
        /// </summary>
        /// <returns>false if not done, true if performance should stop permanently (done)</returns>
        public abstract bool Run();
    }

    /// <summary>
    /// Unpause (resume) performance
    /// </summary>
    public class PlayMessage : Csound6PerfThreadMessage
    {
        public PlayMessage(Csound6PerformanceThread pt)
            : base(pt)
        {
        }
        /// <summary>
        /// Sets the PerformanceThread's IsPaused state to false - allowing processing to continue
        /// </summary>
        /// <returns></returns>
        public override bool Run()
        {
            IsPaused = false;
            return false;
        }
    }

    /// <summary>
    /// Pause perfomance (can be continued) - idempotent if already paused
    /// </summary>
    public class PauseMessage : Csound6PerfThreadMessage
    {
        public PauseMessage(Csound6PerformanceThread pt)
            : base(pt)
        {
        }

        /// <summary>
        /// Sets the IsPaused state of the associated performance thread to true thus stopping
        /// processing while preserving state such that performance can continue right after the
        /// sample where pausing began.
        /// </summary>
        /// <returns></returns>
        public override bool Run()
        {
            IsPaused = true;
            return false;
        }
    }

    /// <summary>
    /// Toggle pause mode
    /// </summary>
    public class TogglePauseMessage : Csound6PerfThreadMessage
    {
        public TogglePauseMessage(Csound6PerformanceThread pt)
            : base(pt)
        {
        }

        /// <summary>
        /// Reverses the state of the performance thread's IsPaused state.
        /// </summary>
        /// <returns></returns>
        public override bool Run()
        {
            IsPaused = !IsPaused;
            return false;
        }
    }

    /// <summary>
    /// Stop performance (cannot be continued)
    /// </summary>
    public class StopMessage : Csound6PerfThreadMessage
    {
        public StopMessage(Csound6PerformanceThread pt)
            : base(pt)
        {
        }

        /// <summary>
        /// Permanently halts processing causing the performance thread to clean up and
        /// destroy the thread.
        /// </summary>
        /// <returns></returns>
        public override bool Run()
        {
            return true;
        }
    }

    /// <summary>
    /// Score Event Message
    /// </summary>
    public class ScoreEventMessage : Csound6PerfThreadMessage
    {
        /// <summary>
        /// Score Event Message
        /// </summary>
        /// <param name="pt">performance thread sending this message</param>
        /// <param name="absp2mode">If true, start times measured from the beginning of performance; if false, current time</param>
        /// <param name="opcode">valid score opcode</param>
        /// <param name="p">zero mode array of parameter fields for event where p1 is p[0], p2 is p[1] etc</param>
        public ScoreEventMessage(Csound6PerformanceThread pt, bool absp2mode, ScoreEventType opcode, double[] p)
            : base(pt)
        {
            IsAbsoluteP2 = absp2mode;
            Opcode = opcode;
            Parameters = p;
        }

        public bool IsAbsoluteP2;
        public ScoreEventType Opcode;
        public double[] Parameters;

        /// <summary>
        /// Present the parameters as set by constructor or properties to the csound engine associated
        /// with this 
        /// </summary>
        /// <returns></returns>
        public override bool Run()
        {
            if (IsAbsoluteP2 && (Parameters.Length > 1))
            {
                double p2 = Parameters[1] - Csound.ScoreTime;
                if (p2 < 0.0)
                {
                    if ((Parameters.Length > 2) && (Parameters[2] >= 0.0) && (Opcode == ScoreEventType.Note || Opcode == ScoreEventType.Advance))
                    {
                        Parameters[2] += p2;
                        if (Parameters[2] <= 0.0) return false;
                    }
                    p2 = 0.0;
                }
                Parameters[1] = p2;
            }
            Status = Csound.SendScoreEvent(Opcode, Parameters);
            return Status != CsoundStatus.Success;
        }
    }

    /// <summary>
    /// Score Event message presented as a string in real time.
    /// </summary>
    public class InputStringMessage : Csound6PerfThreadMessage
    {
        public InputStringMessage(Csound6PerformanceThread pt, string msg)
            : base(pt)
        {
            Message = msg;
        }

        public string Message;

        public override bool Run()
        {
            Status = Csound.SendScoreEvent(Message);
            return Status != CsoundStatus.Success;
        }
    }

    /// <summary>
    /// Seek to the specified score time: Offset
    /// </summary>
    public class SetScoreOffsetMessage : Csound6PerfThreadMessage
    {
        public SetScoreOffsetMessage(Csound6PerformanceThread pt, double timeval)
            : base(pt)
        {
            Offset = timeval;
        }

        public double Offset;

        public override bool Run()
        {
            Csound.ScoreOffsetSeconds = Offset;
            return false;
        }
    }


}
