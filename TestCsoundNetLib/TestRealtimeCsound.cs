using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using csound6netlib;

namespace TestCsoundNetLib
{
    [TestClass]
    public class TestRealtimeCsound
    {
        private StringBuilder m_messageText;
        private int m_count;
        private double m_outval;
        private bool m_perfTheadCalledBack;

        /// <summary>
        /// Tests access to channels defined in a score after compile but prior to performance( chn_k, chn_a, chn_S opcodes)
        /// as exist in SimpleRuntime.csd test file.
        /// Exercises csoundListChannels, csoundDeleteChannelList, csoundGetControlChannelHints, csoundSetControlChannelHints,
        /// </summary>
        [TestMethod]
        public void TestChannelDefinition()
        {
            m_messageText = new StringBuilder();
            m_count = 0;
            using (var cs = new Csound6NetRealtime())
            {
                cs.MessageCallback += TestMessageEvent;
                FileInfo csf = new FileInfo("csdFiles\\SimpleRuntime.csd");
                CsoundStatus result = cs.CompileArgs(new string[] { csf.FullName });
                Assert.AreEqual(CsoundStatus.Success, result);
                result = cs.Start();
                Assert.AreEqual(CsoundStatus.Success, result);
                var cch = new Csound6ControlChannel("chan1", ChannelDirection.Input, cs);
                Assert.AreEqual(ChannelDirection.Input, cch.Direction);
                Assert.AreEqual(ChannelBehavior.Exponential, cch.Behavior);
                Assert.AreEqual(ChannelType.Control, cch.Type);
                Assert.AreEqual(1000.0, cch.Default);
                Assert.AreEqual(500.0, cch.Minimum);
                Assert.AreEqual(2000.0, cch.Maximum);

                cch.Behavior = ChannelBehavior.Integer;
                cch.Maximum = 2500.0;
                cch.Minimum = 400.0;

                var cch1 = new Csound6ControlChannel("chan1", ChannelDirection.Input, cs);
                Assert.AreEqual(cch.Name, cch1.Name);
                Assert.AreEqual(cch.Minimum, cch1.Minimum);
                Assert.AreEqual(400.0, cch1.Minimum);
                Assert.AreEqual(2500.0, cch.Maximum);
                Assert.AreEqual(cch.Maximum, cch1.Maximum);
                Assert.AreEqual(ChannelBehavior.Integer, cch1.Behavior);

                var bus = new Csound6SoftwareBus(cs);
                IDictionary<string, ChannelInfo> chans = cs.GetChannelList();
                Assert.IsNotNull(chans);
                Assert.IsTrue(chans.Count > 0);
                foreach (string key in chans.Keys)//now in test for software bus. remove this part?
                {
                    ChannelInfo info = chans[key];
                    var chan = bus.AddChannel(info);
                    switch (chan.Type)
                    {
                        case ChannelType.String:
                            Assert.IsInstanceOfType(chan, typeof(Csound6StringChannel));
                            Assert.AreEqual(256, chan.DataSize);
                            break;
                        case ChannelType.Audio:
                            Assert.IsInstanceOfType(chan, typeof(Csound6AudioChannel));
                            Assert.AreEqual(35280, chan.DataSize);  //ksmps = 4410 * sizeof(MYFLT)
                            break;
                        case ChannelType.Control:
                            Assert.IsInstanceOfType(chan, typeof(Csound6ControlChannel));
                            Assert.AreEqual(8, chan.DataSize);
                            if (chan.Name.Equals(cch1.Name))
                            {
                                Assert.AreEqual(cch.Maximum, ((Csound6ControlChannel)chan).Maximum);
                                Assert.AreEqual(cch.Minimum, ((Csound6ControlChannel)chan).Minimum);
                                Assert.AreEqual(cch.Default, ((Csound6ControlChannel)chan).Default);
                            }
                            break;
                        case ChannelType.Pvs:
                        case ChannelType.Var:
                        default:
                            Assert.IsFalse(false, string.Format("Unsupported Channel type:{0}", chan.GetType()));
                            break;
                    }//end switch
                }//end foreach key
            }//end using csound
            string text = m_messageText.ToString(); //if need to analyze output...
        }

        /*
         * Tests that channels are formed when software bus is inistantiated
         * and that they fit the definitions presented in the score.
         */
        [TestMethod]
        public void TestSoftwareBus()
        {
            using (var cs = new Csound6NetRealtime())
            {
                var result = cs.Compile(new string[] { "csdFiles\\SimpleRuntime.csd" });
                Assert.AreEqual(CsoundStatus.Success, result);

                var bus = cs.GetSoftwareBus();
                Assert.IsNotNull(bus);
                Assert.IsTrue(bus.Count > 0);
                Assert.AreEqual(8, bus.Count);
                foreach (var channel in bus.Channels)
                {
                    switch (channel.Type)
                    {
                        case ChannelType.Control:
                            Assert.IsTrue(channel.Name.StartsWith("chan"));
                            Assert.IsInstanceOfType(channel, typeof(Csound6ControlChannel));
                            Assert.IsInstanceOfType(channel.Value, typeof(double));
                            Assert.AreEqual(channel.Value, bus[channel.Name]);
                            break;
                        case ChannelType.String:
                            Assert.IsInstanceOfType(channel, typeof(Csound6StringChannel));
                            Assert.IsInstanceOfType(channel.Value, typeof(string));
                            Assert.IsTrue(channel.Name.StartsWith("schan"));
                            Assert.AreEqual(channel.Value, bus[channel.Name]);
                            break;
                        case ChannelType.Audio:
                            Assert.IsInstanceOfType(channel, typeof(Csound6AudioChannel));
                            Assert.IsTrue(channel.Name.StartsWith("achan"));
                            Assert.IsInstanceOfType(channel.Value, typeof(double[]));
                            var values = channel.Value as double[];
                            Assert.AreEqual(cs.Ksmps, values.Length);
                           break;
                        case ChannelType.Pvs:
                           Assert.Fail("PVS data type not yet supported in .net");
                           break; 
                        default:
                            Assert.Fail(string.Format("Software bus has unsupported channel {0} of type: {1}", channel.Name, channel.Type));
                            break;
                    }
                }
            }
        }

        /*
         * Tests performance time access to channels
         */
        [TestMethod]
        public void TestPerfTimeChannels()
        {
            m_messageText = new StringBuilder();
            m_count = 0;
            m_outval = -1;
            using (var cs = new Csound6NetRealtime())
            {
                var result = cs.Compile(new string[] { "csdFiles\\SimpleRuntime.csd" });
                Assert.AreEqual(CsoundStatus.Success, result);
                if (result == CsoundStatus.Success)
                {
                    var bus = cs.GetSoftwareBus();
                    while (!cs.PerformKsmps())
                    {
                        double[] samps = bus["achan1"] as double[];
                        double[] sampInv = new double[samps.Length];
                        if (cs.ScoreTime <= 0.1)
                        {
                            foreach (double samp in samps)
                            {
                                Assert.IsTrue(samp == 0.0);
                            }
                            bool hasPvs = bus.HasChannel("0");
                            if (hasPvs)
                            {
                                object o = bus["0"];
                            }
                        }
                        else if (cs.ScoreTime == 0.2)
                        {
                            double prev = -1.0;
                            for (int i = 0; i < 26; i++)
                            {
                                Assert.IsTrue(samps[i] > prev);
                                prev = samps[i];
                            }
                            for (int i = 26; i < 75; i++)
                            {
                                Assert.IsTrue(prev > samps[i]);
                                prev = samps[i];
                            }
                            for (int i = 0; i < sampInv.Length; i++) sampInv[i] = -samps[i];
                            bus["achan2"] = sampInv;
                        }
                        else if (cs.ScoreTime == 0.3)
                        {
                            double[] samp2 = bus["achan2"] as double[];
                            double[] samp3 = bus["achan3"] as double[]; //instrument will have put samp2 into achan3 during 0.2 second; now available in 0.3 sec
                            for (int i = 0; i < samp3.Length; i++) Assert.AreEqual(samp2[i], samp3[i]);
                        }
                    }
                }
                cs.Cleanup();
            }
        }

        [TestMethod]
        public void TestChannelDirectAccess()
        {
            using (var cs = new Csound6NetRealtime())
            {
                var result = cs.Compile(new string[] { "csdFiles\\SimpleRuntime.csd" });
                Assert.AreEqual(CsoundStatus.Success, result);
                if (result == CsoundStatus.Success)
                {
                    var bus = cs.GetSoftwareBus();
                    var chan2 = bus.GetChannel("chan2") as Csound6ControlChannel;
                    chan2.SetValueDirect(chan2.Default);
                    double val = chan2.GetValueDirect();
                    Assert.AreEqual(chan2.Default, val);

                    while (!cs.PerformKsmps())
                    {
                        Assert.AreEqual(val, chan2.GetValueDirect());
                        chan2.SetValueDirect(++val);
                    }
                }
            }
        }

        /*
         * Tests callbacks from the invalue and outvalue opcodes.
         * Asserts are all in the callbacks; no significant Assertions needed in main routine.
         */
        [TestMethod]
        public void TestOutInCallbacks()
        {
            m_messageText = new StringBuilder();
            m_count = 0;
            m_outval = -1;
            using (var cs = new Csound6NetRealtime())
            {
                cs.OutputChannelCallback += TestOutputCallback;
                cs.InputChannelCallback += TestInputCallback;
                var result = cs.Compile(new string[] {"csdFiles\\SimpleRuntime.csd"});
                Assert.AreEqual(CsoundStatus.Success, result);
                if (result == CsoundStatus.Success)
                {
                    while (!cs.PerformKsmps()) ;
                }
                cs.Cleanup();
            }
        }

        /*
         * Tests Csound6PerformanceThread class using csound threads (mutex and locks) and its commands:
         * Play, Stop, Pause, TogglePause, SetScoreOffset, SendScoreEvent and SendInputMessage.
         * Uses audio dac0: should hear xanadu for 4 seconds with a low note added, a short pause
         * and then the final 15 seconds of xanadu with an added low note crescendoing.
         * Indirectly tests csoundScoreEvent, csoundInputMessage and csoundGet/SetScoreOffset and 
         */
        [TestMethod]
        public void TestPerformanceThread()
        {
            using (var cs = new Csound6NetRealtime())
            {
                var result = cs.Compile(new string[] { "-odac0", "csdFiles\\xanadu.csd" });
                Assert.AreEqual(CsoundStatus.Success, result);
                m_perfTheadCalledBack = false;
                using (var pt = new Csound6PerformanceThread(cs))
                {
                    pt.ProcessCallback += TestPerfThreadCallback;
                    Assert.IsFalse(pt.IsRunning);//test initial state
                    Assert.IsTrue(pt.IsPaused);
                    Assert.IsTrue(pt.Status == CsoundStatus.Success);
                    pt.Play();
                    pt.SendScoreEvent(false, ScoreEventType.Note, new double[] { 3, 0, 15, 0, 5.10, 1.4, 0.8 });
                    Thread.Sleep(1000);
                    Assert.IsTrue(pt.IsRunning);
                    Assert.IsFalse(pt.IsPaused);
                    Assert.IsTrue(pt.Status == CsoundStatus.Success);
                    Thread.Sleep(3000);
                    pt.Pause();
                    Thread.Sleep(1000);
                    Assert.IsTrue(pt.IsRunning);
                    Assert.IsTrue(pt.IsPaused);
                    pt.SetScoreOffset(44.00);
                    pt.TogglePause();
                    Thread.Sleep(1000);
                    pt.SendInputMessage("i3 0 20 0 5.10 1.4 0.8");
                    Thread.Sleep(18000);
                    Assert.IsTrue(pt.IsRunning);
                    Assert.IsFalse(pt.IsPaused);
                    pt.Stop();
                    Thread.Sleep(1500);//wait for threads do settle
                    Assert.IsTrue(m_perfTheadCalledBack);//insure that we executed the callback beyound the first ksmps
                    Assert.IsFalse(pt.IsRunning);//flags eventually got set
                    Assert.IsTrue(pt.Status == CsoundStatus.Completed);//finished all events
                    Assert.IsTrue(cs.ScoreTime > 60.0);//last input message should extend past 60 seconds.
                }
            }
        }

        /***************************************************************************************/

        //Support method for verifying performance thread callback's functioning.
        public void TestPerfThreadCallback(object sender, Csound6ThreadedProcessEventArgs e)
        {
            Assert.IsInstanceOfType(sender, typeof(Csound6PerformanceThread));
            var pt = sender as Csound6PerformanceThread;
            Assert.IsNotNull(e.Csound);
            Assert.IsInstanceOfType(e.Csound, typeof(Csound6NetRealtime));
            var cs = e.Csound as Csound6NetRealtime;
            if (cs.ScoreTime > 0.0)
            {
                Assert.IsTrue(pt.IsRunning);
                m_perfTheadCalledBack = true;
            }
        }

        //Support method for capturing csound messaging events: used in TestRuntimeProperties
        public void TestMessageEvent(object source, Csound6MessageEventArgs args)
        {
            m_messageText.Append(args.Message);
            m_count++;
        }

        private const string Sinvalue = "Input string with input kvalue";
        private const double kinvalue = 440.0;

        //Support method for processing events from input channels
        public void TestInputCallback(object sender, Csound6ChannelEventArgs args)
        {
            if ("chan1".Equals(args.Name))
            {
                Assert.AreEqual(ChannelType.Control, args.Type);
                args.SetCsoundValue((Csound6NetRealtime)sender, kinvalue);
            }
            else if ("schan2".Equals(args.Name))
            {
                args.SetCsoundValue((Csound6NetRealtime)sender, Sinvalue);
            }
            else Assert.Fail("Received unknown invalue channel: {0}", args.Name);

        }

        //Support method for processing events from output channels
        public void TestOutputCallback(object sender, Csound6ChannelEventArgs args)
        {
            if ("chan2".Equals(args.Name))
            {
                Assert.AreEqual(ChannelType.Control, args.Type);
                Assert.IsTrue(args.Direction == (ChannelDirection.Input | ChannelDirection.Output));
                double val = (double)args.Value;
                Assert.IsTrue(val > m_outval);
                m_outval = val;
            }
            else if ("schan1".Equals(args.Name))
            {
                Assert.AreEqual(ChannelType.String, args.Type);
                string val = args.Value as string;
                Assert.IsTrue(val.StartsWith("Csound 6"));
                int pos = val.IndexOf('=');
                Assert.IsTrue(pos > 0);
                string nbr = val.Substring(pos + 1);
                double value = 0.0;
                Assert.IsTrue(double.TryParse(nbr, out value));
                Assert.AreEqual((float)value, (float)m_outval);
            }
            else if ("schan3".Equals(args.Name))
            {
                Assert.AreEqual(ChannelType.String, args.Type);
                Assert.IsNotNull(args.Value);
                string val = args.Value.ToString();
                Assert.IsTrue(val.StartsWith(Sinvalue));
                int pos = val.IndexOf('=');
                Assert.IsTrue(pos > 0);
                string nbr = val.Substring(pos + 1);
                double value = 0.0;
                Assert.IsTrue(double.TryParse(nbr, out value));
                Assert.AreEqual((float)kinvalue, (float)value);
            }
            else Assert.Fail("Received unknown outvalue channel: {0}", args.Name);
        }


    }
}
