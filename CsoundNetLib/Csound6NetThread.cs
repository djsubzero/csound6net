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
    /// <summary>
    /// Delegate as a template for passing a thread process to csoundCreateThread.
    /// </summary>
    /// <param name="userdata">an object to receive when a thread starts up</param>
    /// <returns></returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    internal delegate IntPtr Csound6ThreadedProcess(IntPtr userdata);

    /// <summary>
    /// The entry point for a threaded process.
    /// Defines the signature that will be called for a client process when the threaded
    /// process represented by a Csound6NetThread starts up.
    /// </summary>
    public interface ICsound6Runnable
    {
        int Run(object userdata);
    }

    /// <summary>
    /// Encapsulates the Csound 6 threading system for use by .net programs.
    /// Use this, instead of .net async/await patterns, if you wish to use csound
    /// threading (pthreads) exclusively.
    /// See Csound6PerformanceThread for an example of how to use this class.
    /// So that a C# or VB method can be used in a csound thread, a Run(object userdata)
    /// method is defined through the ICsoundRunnable interface.  This is what
    /// the csound thread ultimatedly executes.
    /// </summary>
    public class Csound6NetThread : IDisposable
    {
        private IntPtr m_thread = IntPtr.Zero;

       /**
        * \addtogroup THREADING
        * @{
        */
        /// <summary>
        /// Creates a Csound6Thread using the provided runnable.
        /// Convenience constructor for the common use case where the runnable 
        /// object is also the user data object.
        /// Unlike in the "c" api, merging the threaded action and the data into
        /// a single object is a more convenient way to work.
        /// </summary>
        /// <param name="runnable"></param>
        public Csound6NetThread(ICsound6Runnable runnable)
            : this(runnable, runnable)
        {
        }

        /// <summary>
        /// Creates a Csound6NetThread and starts it up passing userdata to the Run
        /// method provided by runnable.
        /// </summary>
        /// <param name="runnable">A threadable program with a Run(userdata) method implemented</param>
        /// <param name="userdata">any object to be passed to the newly initiated thread when it starts up</param>
        public Csound6NetThread(ICsound6Runnable runnable, object userdata)
        {
            Runnable = runnable;
            UserData = userdata;
            GCHandle gch = GCHandle.Alloc(userdata);//GCHandle will be freed by RunInternal upon startup
            m_thread = NativeMethods.csoundCreateThread(RunInternal, GCHandle.ToIntPtr(gch));//need to put this as pointer
        }

        ~Csound6NetThread()
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
            Join();//will wait for thread to run out
            if (disposing)
            {//don't have managed resources currently
            }
        }

        /// <summary>
        /// Indicates that this thread is running.
        /// </summary>
        public bool IsRunning { get { return m_thread != IntPtr.Zero; } }

        /// <summary>
        /// Any object which implements the ICsound6Runnable interface.
        /// This object's Run(object userdata) method will be called when this objects
        /// thread starts up.
        /// </summary>
        public ICsound6Runnable Runnable;

        /// <summary>
        /// The object being passed to Runnable's Run method.
        /// </summary>
        public object UserData;

        /// <summary>
        /// Waits until the csound thread has completed and returned by joining it.
        /// Called by the Dispose method and destructor, so there is no need to call this directly
        /// from client code.
        /// </summary>
        /// <returns></returns>
        public IntPtr Join()
        {
            IntPtr r = IntPtr.Zero;
            if (m_thread != IntPtr.Zero)
            {
                r = NativeMethods.csoundJoinThread(m_thread);
                m_thread = IntPtr.Zero;
            }
            return r;
        }
        /**
         * @}
         */



        /*
         * The routine which interfaces with csound's csoundCreateThread function directly
         * and causes the C# implementation of the thread to execute.
         * Receives the marshalled pointer to userdata, restores it to an object and calls Runnable.Run.
         */
        private IntPtr RunInternal(IntPtr pUserdata)
        {

            GCHandle gch = GCHandle.FromIntPtr(pUserdata);
            object userdata = gch.Target;
            gch.Free();//release handle how that object has been safely reconstituted after journey through csound thread startup.
            return (Runnable != null) ? (IntPtr)Runnable.Run(userdata) : IntPtr.Zero;//now do the client's real work...
        }

        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr csoundCreateThread(Csound6ThreadedProcess process, IntPtr userdata);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr csoundGetCurrentThreadId();

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr csoundJoinThread(IntPtr thread);
        }
    }

    /// <summary>
    /// Encapsulates the csound mutex functions as a C# class.
    /// See implementation of Csound6PerformanceThread for examples of usage.
    /// </summary>
    public class Csound6Mutex : IDisposable
    {
        private IntPtr m_mutex = IntPtr.Zero;

        /**
         * \addtogroup THREADING
         * @{
         */

        /// <summary>
        /// Mutexes can be faster than the more general purpose monitor objects
        /// returned by csoundCreateThreadLock() on some platforms, and can also
        /// be recursive, but the result of unlocking a mutex that is owned by
        /// another thread or is not locked is undefined.
        /// If 'isRecursive' is non-zero, the mutex can be re-locked multiple
        /// times by the same thread, requiring an equal number of unlock calls;
        /// otherwise, attempting to re-lock the mutex results in undefined behavior.
        /// </summary>
        public Csound6Mutex()
            : this(false)
        {
        }

        /// <summary>
        /// Creates and returns a mutex object.
        /// If 'isRecursive' is non-zero, the mutex can be re-locked multiple
        /// times by the same thread, requiring an equal number of unlock calls;
        /// otherwise, attempting to re-lock the mutex results in undefined behavior.
        /// </summary>
        /// <param name="isRecursive">make recursive if true and platform supports; false, then not recursive</param>
        public Csound6Mutex(bool isRecursive)
        {
            m_mutex = NativeMethods.csoundCreateMutex(isRecursive ? 1 : 0);
        }

        ~Csound6Mutex()
        {
            Dispose(false);
        }

        /// <summary>
        /// Destroys the indicated mutex object by calling csoundDestroyMutex
        /// Guards against non-idempotent calls to csoundDestroyMutex as could
        /// otherwise result in undefined behavior per csound doc.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {//don't have managed resources currently
            }
            if (m_mutex != IntPtr.Zero)
            {
                NativeMethods.csoundDestroyMutex(m_mutex);
                m_mutex = IntPtr.Zero;
            }
        }


        /// <summary>
        /// Acquires the indicated mutex object; if it is already in use by
        /// another thread, the function waits until the mutex is released by
        /// the other thread.
        /// </summary>
        public void Lock()
        {
            NativeMethods.csoundLockMutex(m_mutex);
        }

        /// <summary>
        /// Acquires the indicated mutex object and returns zero, unless it is
        /// already in use by another thread, in which case a non-zero value is
        /// returned immediately, rather than waiting until the mutex becomes available.
        /// Note: this function may be unimplemented on Windows.
        /// </summary>
        /// <returns></returns>
        public int LockNoWait()
        {
            return (m_mutex != IntPtr.Zero) ? NativeMethods.csoundLockMutexNoWait(m_mutex) : -1;
        }

        /// <summary>
        /// Releases the indicated mutex object, which should be owned by
        /// the current thread, otherwise the operation of this function is
        /// undefined. A recursive mutex needs to be unlocked as many times
        /// as it was locked previously.
        /// </summary>
        public void Unlock()
        {
            if (m_mutex != IntPtr.Zero)
            {
                NativeMethods.csoundUnlockMutex(m_mutex);
            }
        }
        /**
         * @}
         */


        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr csoundCreateMutex(int isRecursive);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundLockMutex(IntPtr mutex);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int csoundLockMutexNoWait(IntPtr mutex);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundUnlockMutex(IntPtr mutex);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundDestroyMutex(IntPtr mutex);

        }
    }

    /// <summary>
    /// Encapsulates csound's thread lock functions as a C# class.
    /// See implementation of Csound6PerformanceThread for examples of usage.
    /// </summary>
    public class Csound6ThreadLock : IDisposable
    {
        private IntPtr m_threadLock = IntPtr.Zero;
        /**
         * \addtogroup THREADING
         * @{
         */

        /// <summary>
        /// Creates and returns a monitor object, or NULL if not successful.
        /// The object is initially in signaled (notified) state.
        /// </summary>
        public Csound6ThreadLock()
        {
            m_threadLock = NativeMethods.csoundCreateThreadLock();
        }

        ~Csound6ThreadLock()
        {
            Dispose(false);
        }

        /// <summary>
        /// Destroys the indicated monitor object by calling csoundDestroyThreadLock.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            if (m_threadLock != IntPtr.Zero)
            {
                Notify();
                NativeMethods.csoundDestroyThreadLock(m_threadLock);
                m_threadLock = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Notifies the indicated monitor object.
        /// </summary>
        public void Notify()
        {
            if (m_threadLock != IntPtr.Zero)
            {
                NativeMethods.csoundNotifyThreadLock(m_threadLock);
            }
        }

        /// <summary>
        /// Waits on the indicated monitor object for the indicated period.
        /// The function returns either when the monitor object is notified,
        /// or when the period has elapsed, whichever is sooner; in the first case,
        /// zero is returned.
        /// If 'milliseconds' is zero and the object is not notified, the function
        /// will return immediately with a non-zero status.
        /// </summary>
        /// <param name="milliseconds"></param>
        /// <returns></returns>
        public int Wait(int milliseconds)
        {
            return NativeMethods.csoundWaitThreadLock(m_threadLock, (uint)milliseconds);
        }

        /// <summary>
        /// Waits on the indicated monitor object until it is notified.
        /// This function is similar to csoundWaitThreadLock() with an infinite
        /// wait time, but may be more efficient.
        /// </summary>
        public void WaitNoTImeout()
        {
            NativeMethods.csoundWaitThreadLockNoTimeout(m_threadLock);
        }

        /**
       * @}
       */

        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr csoundCreateThreadLock();

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundNotifyThreadLock(IntPtr threadLock);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int csoundWaitThreadLock(IntPtr threadlock, uint milliseconds);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundWaitThreadLockNoTimeout(IntPtr threadlock);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern void csoundDestroyThreadLock(IntPtr threadLock);
        }
    }


    /// <summary>
    /// Encapsulates csound's thread barrier functions as a C# class
    /// </summary>
    public class Csound6ThreadBarrier : IDisposable
    {
        private IntPtr m_barrier = IntPtr.Zero;
        /**
         * \addtogroup THREADING
         * @{
         */

        /// <summary>
        /// Create a Thread Barrier. 
        /// Max value defaults to 2: one master and one child thread.
        /// </summary>
        public Csound6ThreadBarrier()
            : this(2) //1 child thread + master as default
        {
        }

        /// <summary>
        /// Create a Thread Barrier with provided max value.
        /// </summary>
        /// <param name="max">Should be equal to number of child threads using the barrier plus one for the master thread</param>
        public Csound6ThreadBarrier(int max)
        {
            m_barrier = NativeMethods.csoundCreateBarrier((uint)max);
        }

        ~Csound6ThreadBarrier()
        {
            Dispose(false);
        }

        /// <summary>
        /// Destroys this Thread Barrier.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
            if (m_barrier != IntPtr.Zero)
            {
                NativeMethods.csoundDestroyBarrier(m_barrier);
                m_barrier = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Wait on the thread barrier.
        /// </summary>
        /// <returns></returns>
        public int Wait()
        {
            return NativeMethods.csoundWaitBarrier(m_barrier);
        }

        /**
         * @}
         */

        private class NativeMethods
        {
            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr csoundCreateBarrier(uint max);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int csoundWaitBarrier(IntPtr barrier);

            [DllImport(Csound6Net._dllVersion, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int csoundDestroyBarrier(IntPtr barrier);

        }
    }

}
