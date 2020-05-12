using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using csound6netlib;


namespace csound6netRuntime.console
{
    /// <summary>
    /// An object oriented version combining a "userdata structure" and a thread execution function
    /// as used in api examples showing the running a background csound instance.
    /// This class elaborates a little the example to make channel communication with the
    /// background instance more natural/convienient by adding a .net indexer to access channels by name.
    /// The class creates its own csound instance when created and exposes that for accessing
    /// csound properties and functions as needed.
    /// The whole class can be passed to Csound6NetThread as the function and userdata combined.
    /// Csound6Thread includes a constructor that assumes that the Runner and data are the same object.
    /// </summary>
    public class UserData : ICsound6Runnable
    {
        /// <summary>
        /// Creates a threadable userdata class including its own Csound instance
        /// and registers its own (null) logger.
        /// </summary>
        public UserData()
            : this(new Csound6NetRealtime(LoggingMessageHandler))
        {
        }

        /// <summary>
        /// Creates a threadable userdata class using the provided csound instance
        /// </summary>
        /// <param name="_csound"></param>
        public UserData(Csound6NetRealtime _csound)
        {
            Csound = _csound;
            Done = false;
        }

        /// <summary>
        /// Provides access to the csound instance used by this userdata object.
        /// If created by this object as the only csound reference,
        /// destruction of this object will also destroy the csound instance automatically.
        /// </summary>
        public Csound6NetRealtime Csound;

        /// <summary>
        /// Set to true to stop an active performing loop in the Run method. 
        /// When running and this value being false, performance will continue until a score completes.
        /// </summary>
        public bool Done;

        /// <summary>
        /// Convenience indexer for accessing software bus channel values by name.
        /// </summary>
        /// <param name="channel">the name of a valid channel on the software bus in the current score</param>
        /// <returns></returns>
        public object this[string channel]
        {
            get { return Csound.GetSoftwareBus()[channel]; }
            set { Csound.GetSoftwareBus()[channel] = value; }
        }

        /// <summary>
        /// The method which will be executing when this userdata object is used as
        /// an ICsoundRunnable object for a Csound6NetThread instance.
        /// The method completes either when an active score completes or a holder of an
        /// instance of this class sets the done property to true.
        /// </summary>
        /// <param name="userdata"></param>
        /// <returns></returns>
        public int Run(object userdata)
        {
            UserData udata = userdata as UserData;
            if (udata != null)
            {
                while (!udata.Done && !udata.Csound.PerformKsmps()) ;
            }
            return 0;
        }

        /* /////////////////////////////////////////////////////////////////////////////////////////////////////////////////// */

        /* Defaults to not showing csound init chatter.  Replace null with Console.Out
         * or implement -O as demonstrated in Csound6Console to see initialization chatter
         * interspersed between requests for frequencies.
         */
        static TextWriter _writer = null;


        public static void LoggingMessageHandler(object sender, Csound6MessageEventArgs args)
        {
            if (_writer != null)
            {
                _writer.Write(args.Message.Replace("\n", _writer.NewLine));
            }
        }


    }
}
