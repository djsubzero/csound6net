using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

/*
    Adapted to C# by Richard Henninger from csPerfThread.hpp/cpp by Istvan Varga
    Copyright (C) 2013 by Richard Henninger and is part of Csound6NetLib which
    is licensed under the same terms and disclaimers as Csound indicates below.


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

/**
 * CsoundPerformanceThread(Csound *)
 * CsoundPerformanceThread(CSOUND *)
 *
 * Performs a score in a separate thread until the end of score is reached,
 * the playback (which is paused by default) is stopped by calling
 * CsoundPerformanceThread::Stop(), or an error occurs.
 * The constructor takes a Csound instance pointer as argument; it assumes
 * that csoundCompile() was called successfully before creating the
 * performance thread. Once the playback is stopped for one of the above
 * mentioned reasons, the performance thread calls csoundCleanup() and
 * returns.

  An example program using the CsoundPerformanceThread class

#include <stdio.h>
#include "csound.hpp"
#include "csPerfThread.hpp"

public static int main(string[] args)
{
    using (var cs = new Csound6NetRealtime();
    {
        CsoundStatus result = cs.Compile(args);

        if(result == CsoundStatus.Success)
        {
            var perfThread = new Csound6PerformanceThread(cs);
            perfThread.Play(); // Starts performance
            while(perfThread.Status() == CsoundSuccess); 
                // nothing to do here... but you could process input events, graphics etc
            perfThread.Stop();  // Stops performance. In fact, performance should have
                                // already finished, so this is just an example of how
                                //to stop if you need
          //  perfThread.Join();  // no need to call join since Dispose and destructor will do this anyway.
                                  //Join() just calls Dispose anyway.
        }
        else{
            printf("csoundCompile returned an error\n");
            return 1;
        }
    }
    return 0;
}
 */

namespace csound6netlib
{

    public class Csound6ThreadedProcessEventArgs : EventArgs
    {
        public Csound6ThreadedProcessEventArgs()
        {
        }

        public Csound6ThreadedProcessEventArgs(Csound6NetRealtime _csound)
        {
            Csound = _csound;
        }

        public Csound6NetRealtime Csound;
    }

    /// <summary>
    /// Delegate for receiving events just before each call to csoundPerformKsmps while
    /// the performance thread is executing.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void Csound6ThreadedProcessEventHandler(object sender, Csound6ThreadedProcessEventArgs e);

    /// <summary>
    /// C#/.net implementation of Istvan Vargas' CsoundPerformanceThread C++ class and its
    /// associated helper classes.
    /// /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public class Csound6PerformanceThread : IDisposable, ICsound6Runnable
    {
        private Csound6NetRealtime m_csound;
        private Queue<Csound6PerfThreadMessage> m_queue;
        private Csound6Mutex m_queueLock;
        private Csound6ThreadLock m_pauseLock;
        private Csound6ThreadLock m_flushLock;
        private Csound6NetThread m_performanceThread;

        /**
         * \addtogroup PERFTHREAD
         * @{
         */

        /// <summary>
        /// Constructor for creating a performance thread.
        /// </summary>
        /// <param name="csound"></param>
        public Csound6PerformanceThread(Csound6NetRealtime csound)
        {
            m_csound = csound;
            IsPaused = true;
            IsRunning = false;
            Status = CsoundStatus.MemoryAllocationFailure;
            m_queue = new Queue<Csound6PerfThreadMessage>();
            m_queueLock = new Csound6Mutex();
            if (m_queueLock != null) m_pauseLock = new Csound6ThreadLock();
            if (m_pauseLock != null) m_flushLock = new Csound6ThreadLock();
            if (m_flushLock != null) Status = CsoundStatus.Success;
        }

        ~Csound6PerformanceThread()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///  Waits until the thread routine has returned (if it hasn't yet) to thread is
        ///  certain to be disposed.
        ///  Then releases any resources associated with the performance thread object
        ///  including locks, mutexes and the queue.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (m_queue != null) m_queue.Clear();
                if (m_queueLock != null) m_queueLock.Dispose();
                if (m_pauseLock != null) m_pauseLock.Dispose();
                if (m_flushLock != null) m_flushLock.Dispose();
                if (m_performanceThread != null) m_performanceThread.Dispose();
                m_queueLock = null;
                m_pauseLock = null;
                m_flushLock = null;
                m_performanceThread = null;
                m_queue = null;
            }
        }

        public event Csound6ThreadedProcessEventHandler ProcessCallback;

        internal Csound6NetRealtime Csound { get { return m_csound; } }

        /// <summary>
        /// Indicates that the thread is in a "paused" state: accepting messages, but no creating samples.
        /// Sample creation would resume by sending a Play message.
        /// </summary>
        public bool IsPaused;

        /// <summary>
        /// 
        /// </summary>
        public bool IsRunning;

        /// <summary>
        /// The status value from the most recent call to Csound by this performance thread
        /// </summary>
        public CsoundStatus Status;

        /// <summary>
        /// This is the threaded Csound6PerformanceThread's executable.
        /// By implementing the ICsoundRunnable interface, it can be called when a threaded
        /// version of this object starts up.
        /// Although it is public so Csound6NetThread can activate it, there is never a need
        /// for any external client object to call it directly on its own.
        /// For a Csound6PerformanceThread class, it starts up its own internal Perform loop to last
        /// for the duration of the compiled score or the calling of this class's Stop() method.
        /// </summary>
        /// <param name="userdata">the object received from the client process; here: the Csound6PerformanceThread itself</param>
        /// <returns>the exit code from the csound engine when done</returns>
        public int Run(object userdata)
        {
            var retval = CsoundStatus.BasicError;
            var pt = userdata as Csound6PerformanceThread;
            if (pt != null)
            {
                var p = new PerformScore(pt);
                retval = p.Perform();
            }
            return (int)retval;
        }

        /// <summary>
        /// Pauses performance (can be continued by calling Play()).
        /// </summary>
        public void Pause()
        {
            QueueMessage(new PauseMessage(this));
        }
        
        /// <summary>
        /// Continues performance if it was paused.
        /// </summary>
        public void Play()
        {
            if (m_performanceThread == null) StartPerformanceThread();
            QueueMessage(new PlayMessage(this));
        }

        /// <summary>
        /// Pauses performance unless it is already paused, in which case it is continued.
        /// </summary>
        public void TogglePause()
        {
            QueueMessage(new TogglePauseMessage(this));
        }

        /// <summary>
        /// Stops performance (cannot be continued).
        /// </summary>
        public void Stop()
        {
            QueueMessage(new StopMessage(this));
        }

        /// <summary>
        /// Sends a score event of type 'opcod' (e.g. 'i' for a note event), with
        ///  'pcnt' p-fields in array 'p' (p[0] is p1). If absp2mode is non-zero,
        ///  the start time of the event is measured from the beginning of
        ///  performance, instead of the default of relative to the current time.
        /// </summary>
        /// <param name="absp2mode"></param>
        /// <param name="opcode"></param>
        /// <param name="p"></param>
        public void SendScoreEvent(bool absp2mode, ScoreEventType opcode, double[] p)
        {
            QueueMessage(new ScoreEventMessage(this, absp2mode, opcode, p));
        }

        /// <summary>
        /// Sends a score event as a string, similarly to line events (-L).
        /// </summary>
        /// <param name="msg"></param>
        public void SendInputMessage(string msg)
        {
            QueueMessage(new InputStringMessage(this, msg));
        }

        /// <summary>
        /// Sets the playback time pointer to the specified value (in seconds).
        /// </summary>
        /// <param name="seconds"></param>
        public void SetScoreOffset(double seconds)
        {
            QueueMessage(new SetScoreOffsetMessage(this, seconds));
        }

        /// <summary>
        /// Provided for completeness with c++ version; not really needed in C# version.
        /// Just calls Dispose() and returns Status.
        /// Same effect can be accomplished by letting the system run Dispose upon completion 
        /// of a "using" block or calling Dispose() directly.
        /// </summary>
        /// <returns></returns>
        public CsoundStatus Join()
        {
            Dispose();//release resources (idempotent, so caller can dispose, too...)
            return Status;
        }

        /// <summary>
        /// Waits until all pending messages (pause, send score event, etc.)
        /// are actually received by the performance thread.
        /// </summary>
        public void FlushMessageQueue()
        {
            if (m_queue.Count > 0)
            {
                m_flushLock.WaitNoTImeout();
                m_flushLock.Notify();
            }
        }
        /**
         * @}
         */

        /********************************************************************************************************/

        /*
         * Initiates the performance thread.  csPerfThread starts thread in constructor, but in C#,
         * this won't work because this class passes itself as user data and "this" pointers
         * can't be pinned in memory until the constructor completes.
         * We call it conditionally when Play() is called instead.
         */
        private bool StartPerformanceThread()
        {
            if (m_performanceThread == null)
            {
                m_performanceThread = new Csound6NetThread(this, this);
            }
            IsRunning = ((m_performanceThread != null) && m_performanceThread.IsRunning);
            return IsRunning;
        }

        /*
         * Main loop for processing messages from client to performance thread.
         * Processes all queued messages before each call to PerformKsmps.
         * When Stop() message received or if an error or completion state occurs,
         * this method exits causing the performance thread to complete.
         */
        private CsoundStatus Perform()
        {
            bool done = false;
            while (!done)
            {
                done = DrainQueue();
                if (!done)
                {
                    var handle = ProcessCallback;
                    if (handle != null) handle(this, new Csound6ThreadedProcessEventArgs(m_csound));
                    done = m_csound.PerformKsmps();
                }
            };
            if (Status == CsoundStatus.Success) Status = CsoundStatus.Completed;
            m_csound.Cleanup();
            m_queueLock.Lock();
            m_queue.Clear();
            m_flushLock.Notify();
            m_queueLock.Unlock();
            IsRunning = false;
            return Status;
        }

        /*
         * Refactored out of Perform() to make logic easier to understand.
         */
        private bool DrainQueue()
        {
            bool done = false;
            while (m_queue.Count > 0)
            {
                m_queueLock.Lock();
                while (m_queue.Count > 0)
                {
                    var msg = m_queue.Dequeue();
                    done = msg.Run();
                    if (done) break;
                } //loop over when empty (done = false) or Stop (done=true)
                if (IsPaused) m_pauseLock.Wait(0); //Wait if event set IsPaused
                //or if running, mark queue as empty
                m_flushLock.Notify();
                m_queueLock.Unlock();
                if (!done)
                {
                    if (IsPaused)
                    {
                        m_pauseLock.WaitNoTImeout();
                        m_pauseLock.Notify();
                    }
                }
            } //keep processing if queue refilled while pausing
            return done;
        }

        /*
         * Enqueues a message from a client process so that it will be received by
         * by the performance thread for processing.
         * Called internally by public convenience methods like Play(), Pause() and Stop()
         */
        private void QueueMessage(Csound6PerfThreadMessage msg)
        {
            if (Status == CsoundStatus.Success)
            {
                m_queueLock.Lock();
                m_queue.Enqueue(msg);
                m_flushLock.Wait(0);
                m_pauseLock.Notify();
                m_queueLock.Unlock();
            }
        }

        private class PerformScore
        {
            private Csound6PerformanceThread m_pt;
            internal PerformScore(Csound6PerformanceThread pt)
            {
                m_pt = pt;
            }

            internal CsoundStatus Perform()
            {
                return m_pt.Perform();
            }
        }

    }

}
