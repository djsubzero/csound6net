using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using csound6netlib;

namespace csound6netRuntime.console
{

    /// <summary>
    /// Simple replication of standard csound api example of using channels (software bus)
    /// to communicate to a csound instance in a separate thread from a console application.
    /// Csound examples of threaded channel access in "c" use a structure
    /// and a threadable performance function.
    /// In an object oriented environment such as C# provides, these can be
    /// merged into a single class with properties and a Run method.
    /// That approach is demonstrated here in the UserData class.
    /// The UserData class in this example adds an indexed property for easy channel access.
    /// The supplied example plays typed in pitches until 60 seconds are up or
    /// until something other than a positive number is supplied.
    /// See the project's Runtime.csd file to see and instrument using chnget which is played for 60 seconds
    /// as it responds to positive numbers enterred from the console keyboard.
    /// Command line arguments should be provided as for any run of csound like: -d -f -odac0 "Runtime.csd"
    /// </summary>
    class Csound6RuntimeConsole 
    {
        static int Main(string[] args)
        {
            Console.Out.WriteLine();
            var channels = new UserData();//empty constructor creates its own Csound instance.

            CsoundStatus result = channels.Csound.Compile(args); //args include the csd file to perform (example plays for one minute)
            if (result == CsoundStatus.Success)
            {
                var t = new Csound6NetThread(channels);//This sets the thread in motion until Hz not a positive integer or score completes.
                                                       //  Now we can communicate with it from here via channels or callbacks

                double hz = 1;                         //  So, we'll send whatever frequency is typed in from the keyboard via "pitch" channel
                while (hz > 0) 
                {
                    Console.Out.Write("\nEnter a pitch in Hz (or 0 to exit) and press enter:");
                    string s = Console.In.ReadLine();
                    if (Double.TryParse(s, out hz) && (hz > 0))
                    {
                        channels["pitch"] = hz;// channel implementations use new csound 6 thread-safe signatures for channel access.
                    }
                }
                channels.Done = true;//aborts performance loop (could use mutexes for airtight concurrent access)
                t.Join();            //good practice for orderly shutdown, but destructor of thread would pretty much do this anyway.
                while (t.IsRunning) ;//normally not running when Join returns
                t.Dispose();
            }

            return (((int)result) >= 0) ? 0 : (int)result;
        }

    }


}
