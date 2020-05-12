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
    public class TestCsound
    {
        private StringBuilder m_messageText;
        private int m_count;
        private bool m_fileopened = false;
        private static float m_timenow = 0;

        /*
         * Tests Win32 and CRT-lib via C# access: cfopen, cfgets, cfclose (using FILE* structure)
         */
        [TestMethod]
        public void TestBridge2C()
        {
            FileInfo fi = new FileInfo("csdfiles\\xanadu.csd");
            string shortname = BridgeToCpInvoke.wGetShortPathName(fi.FullName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(shortname));
            Assert.IsFalse(shortname.Contains(" "));

            IntPtr pFile = BridgeToCpInvoke.cfopen(shortname, "r");
            Assert.AreNotEqual(IntPtr.Zero, pFile);
            StringBuilder text = new StringBuilder();
            int count = 0;
            string line = null;
            do
            {
                line = BridgeToCpInvoke.cfgets(pFile, 120);
                if (line != null)
                {
                    text.Append(line);
                    count++;
                }
            } while (line != null);
            Assert.AreNotEqual(0, count);
            string result = text.ToString();
            Assert.IsTrue(result.EndsWith("</CsoundSynthesizer>\n"));
            int done = BridgeToCpInvoke.cfclose(pFile);
            Assert.AreEqual(0, done);
        }

        /*
         * Tests csoundInitialize, csoundCreate, csoundDestroy, csoundGetVersion, csoundGetApiVersion,
         * csoundGetStrVarMaxLen and csoundGetSizeOfMYFLT.
         * Also tests default attribute values for csoundGetSr, csoundGetKr, csoundGetKsmps, 
         * csoundGetNchnls, csoundGetNchnlsInput, csoundGet0DBFS, csoundGetHostData, csoundGetDebug
         * and csoundGetMessageLevel, csoundSetMessageLevel.
         */
        [TestMethod]
        public void TestCsoundDefaults()
        {
            using (Csound6Net cs = new Csound6Net())
            {
                int ver = cs.Version;
                int apiVer = cs.ApiVersion;
                Assert.AreNotSame(0, ver);
                Assert.AreEqual(6, (ver / 1000));
                Assert.AreNotSame(0, apiVer);
                Assert.AreEqual(300, apiVer);
                Assert.AreEqual(8, Csound6Net.SizeOfMYFLT);
                object o = cs.HostData;
                Assert.IsNull(o);
                Assert.AreNotEqual(0, cs.MessageLevel);
                Assert.AreEqual(MessageLevel.Default, cs.MessageLevel);
                cs.MessageLevel = MessageLevel.Benchmark | MessageLevel.Amps | MessageLevel.Warnings;
                Assert.AreEqual(128 + 5, (int)cs.MessageLevel);
                Assert.IsFalse(cs.IsDebugMode);
                Assert.AreEqual(44100.0, cs.Sr);
                Assert.AreEqual(4410.0, cs.Kr);
                Assert.AreEqual(10, cs.Ksmps);
                Assert.AreEqual(1, cs.Nchnls);
                Assert.AreEqual(1, cs.NchnlsInput);
                Assert.AreEqual(32768.0, cs.OdBFS);
            }

        }

        /*
         * Tests setting CsoundParameters properties and verifies that these are transferred
         * correctly to csound's OPARMS internal structure by retrieving a separate CSOUND_PARAMS structure.
         * Tests setting HostData and debug via parameters.
         */
        [TestMethod]
        public void TestCsoundParameters()
        {
            String x = "Host Data String";
            using (Csound6Net cs = new Csound6Net(x, CsoundInitFlag.NoFlags, null))
            {
                object o = cs.HostData;
                Assert.AreEqual(x, o);
                Assert.AreEqual("Host Data String", cs.HostData);
                Csound6Parameters opts = new Csound6Parameters(cs);
                Assert.AreEqual(cs.IsDebugMode, opts.IsDebugMode);
                Assert.AreEqual(cs.MessageLevel, opts.MessageLevel);
                opts.IsDebugMode = true;
                Assert.IsTrue(opts.IsDebugMode);
                Assert.IsTrue(cs.IsDebugMode);
                opts.IsDebugMode = false;
                Assert.IsFalse(cs.IsDebugMode);
                opts.MessageLevel = MessageLevel.Amps | MessageLevel.Warnings | MessageLevel.Range;
                Assert.AreEqual(7, (int)opts.MessageLevel);
                Assert.AreEqual(7, (int)cs.MessageLevel);
                opts.HardwareBufferFrames = 2048;
                opts.SoftwareBufferFrames = 512;
                opts.ZeroDBOverride = 1.0;
                opts.Tempo = 60;
                opts.SampleRateOverride = 48000;
                opts.ControlRateOverride = 48;
                opts.Heartbeat = HeartbeatStyle.FileSize;
                opts.IsRealtimeMode = true;
                opts.IsSampleAccurateMode = true;
                opts.IsUsingAsciiGraphs = false;
                opts.IsUsingCsdLineCounts = false;
                opts.MaximumThreadCount = 4;
                opts.IsSyntaxCheckOnly = !opts.IsSyntaxCheckOnly;
                opts.IsDisplayingGraphs = false;

                opts.MidiKey2Parameter = 4;
                opts.MidiKeyAsHertz2Parameter = 5;
                opts.MidiKeyAsOctave2Parameter = 6;
                opts.MidiKeyAsPitch2Parameter = 7;
                opts.MidiVelocity2Parameter = 8;
                opts.MidiVelocityAsAmplitude2Parameter = 9;
                opts.OutputChannelCountOverride = 2;
                opts.InputChannelCountOverride = 1;
                opts.IsAddingDefaultDirectories = false;

                opts.IsUsingCscore = !opts.IsUsingCscore;
                opts.WillBeepWhenDone = !opts.WillBeepWhenDone;
                opts.IsDoneWhenMidiDone = true;
                opts.IsDeferingGen01Load = !opts.IsDeferingGen01Load;

                //Have csound copy from oparms to CSOUND_PARAMS and confirm above values got to csound
                CSOUND_PARAMS copy = new CSOUND_PARAMS();
                opts.GetParams(copy);
                Assert.AreEqual(copy.hardware_buffer_frames, opts.HardwareBufferFrames);
                Assert.AreEqual(opts.SoftwareBufferFrames, copy.buffer_frames);
                Assert.AreEqual(opts.ZeroDBOverride, copy.e0dbfs_override);
                Assert.AreEqual(60, copy.tempo);
                Assert.AreEqual(48000.0, copy.sample_rate_override);
                Assert.AreEqual(opts.ControlRateOverride, copy.control_rate_override);
               Assert.AreEqual(3, copy.heartbeat); 
                Assert.AreEqual(1, copy.realtime_mode);
                Assert.AreEqual(1, copy.sample_accurate);
                Assert.AreEqual(0, copy.ascii_graphs);
               Assert.AreEqual(0, copy.csd_line_counts); 
                Assert.AreEqual(4, copy.midi_key);
                Assert.AreEqual(5, copy.midi_key_cps);
                Assert.AreEqual(6, copy.midi_key_oct);
                Assert.AreEqual(7, copy.midi_key_pch);
                Assert.AreEqual(8, copy.midi_velocity);
                Assert.AreEqual(9, copy.midi_velocity_amp);
                Assert.AreEqual(2, copy.nchnls_override);
                Assert.AreEqual(1, copy.nchnls_i_override);
                Assert.AreEqual(1, copy.no_default_paths);//double negative inverted boolean: paramater = false, no_default_paths = 1
                Assert.AreEqual(4, copy.number_of_threads);
                Assert.AreEqual(1, copy.syntax_check_only);
                Assert.AreEqual(1, copy.ring_bell);
                Assert.AreEqual(1, copy.use_cscore);
                Assert.AreEqual(0, copy.displays);
                Assert.AreEqual(1, copy.terminate_on_midi);
                Assert.AreEqual(1, copy.defer_gen01_load);
            }
        }


        /*
         * Tests setting arguments via csoundCompile and a csd CsOptions section and methods to verify these.
         * Tests setting and reading environment variables.
         * Confirms setting of HostData via constructor.
         * Tests Message and FileOpen event registration and processing.
         */
        [TestMethod]
        public void TestRuntimeProperties()
        {
            bool set = Csound6Net.SetGlobalEnv("RGHMUSIC", "Henninger");
            Assert.IsTrue(set);
            object o = new Object();
            using (Csound6Net cs = new Csound6Net(o, CsoundInitFlag.NoFlags, null))
            {
                try
                {
                    m_messageText = new StringBuilder();
                    m_count = 0;
                    cs.MessageCallback += TestMessageEvent;

                    m_fileopened = false;
                    cs.FileOpenCallback += TestFileOpenEvent;

                    FileInfo csdFile = new FileInfo("csdFiles\\Simple.csd");
                    string path = csdFile.FullName;
                    string shortpath = BridgeToCpInvoke.wGetShortPathName(csdFile.FullName); //test long strings
                    CsoundStatus result = cs.Compile(new string[] { shortpath });
                    Assert.AreEqual(CsoundStatus.Success, result);
                    object o1 = cs.HostData; //test marshalling of host data.
                    Assert.AreEqual(o, o1);
                    cs.HostData = "abc";
                    Assert.IsTrue("abc".Equals(cs.HostData));
                    string sfdir = cs.GetEnv("SFDIR");
                    Assert.IsNotNull(sfdir);
                    Assert.IsTrue(sfdir.Length > 0);
                    string garbage = cs.GetEnv("garbage");
                    Assert.IsTrue(string.IsNullOrWhiteSpace(garbage));
                    string rgh = cs.GetEnv("RGHMUSIC");
                    Assert.AreEqual("Henninger", rgh);
                    //test for sample rate, control rate, ksamps, filename from simple.csd...
                    Assert.AreEqual(44100.0, cs.Sr);
                    Assert.AreEqual(441.0, cs.Kr);
                    Assert.AreEqual(100, cs.Ksmps);
                    Assert.AreEqual(1.0, cs.OdBFS);
                    Assert.AreEqual(1, cs.Nchnls);
                    Assert.AreNotEqual(0, m_count);
                    Assert.AreEqual("simple.wav", cs.OutputFileName);

                    //confirm that all stdout text went here: for Simple.csd, chatter ends "SECTION 1:\n"
                    string text = m_messageText.ToString();
                    Assert.IsTrue(text.EndsWith("SECTION 1:\n"));
                    Assert.IsTrue(m_fileopened); //make sure we enterred the tests in TestFileOpenEvent

                }
                catch (Exception e)
                {
                    string x = e.Message;
                    Assert.Fail(x);
                }
            }
        }

        /*
         * Tests access to opcode definitions and compile via async methods.
         * Tests named gen algorithm access.
         */
        [TestMethod]
        public async Task TestOpcodeAccess()
        {
            using (Csound6Net cs = new Csound6Net())
            {
                FileInfo csdFile = new FileInfo("csdFiles\\Simple.csd");
                CsoundStatus result = await cs.CompileAsync(new string[] { csdFile.FullName }); //tests raw long strings for fopen
                Assert.AreEqual(CsoundStatus.Success, result);

                IDictionary<string, IList<OpcodeArgumentTypes>> opcodes = await cs.GetOpcodeListAsync();
                Assert.IsTrue(opcodes.Count > 1000);
                Assert.IsTrue(opcodes.ContainsKey("oscil"));
                Assert.IsTrue(opcodes["oscil"].Count > 2);
                Assert.AreEqual(5, opcodes["oscil"].Count);//there should be five oscil opcode instances
                foreach (OpcodeArgumentTypes op in opcodes["oscil"])
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(op.outypes));
                    Assert.IsFalse(string.IsNullOrWhiteSpace(op.intypes));
                    char otype = op.outypes[0];
                    Assert.IsTrue((otype == 'a') || (otype == 's'));
                    Assert.AreEqual(4, op.intypes.Length);
                    Assert.IsTrue(op.intypes.EndsWith("jo"));
                    if (otype == 's')
                    {
                        Assert.IsTrue(op.intypes.StartsWith("kk"));
                    }
                    else if (otype == 'a')
                    {
                        string start = op.intypes.Substring(0, 2);
                        Assert.IsTrue(start.Contains("a") || start.Contains("k"));
                    }
                    else Assert.Fail("opcode test: output type for oscil - expected a|s got {0}", otype);
                }
                //test that modules loaded:
                Assert.IsNotNull(opcodes["pan2"]);
                Assert.AreEqual(1, opcodes["pan2"].Count);
                Assert.AreEqual("aa", opcodes["pan2"][0].outypes);
                Assert.AreEqual("axo", opcodes["pan2"][0].intypes);
                Assert.IsTrue(opcodes.Keys.Contains("granule"));

                IDictionary<string, int> namedGens = cs.GetNamedGens();
                Assert.IsTrue(namedGens.Count > 0);
                foreach (string name in namedGens.Keys)
                {
                    Assert.IsTrue(name.Length > 0);
                    Assert.IsTrue(namedGens[name] > 0);
                }
            }
        }

        /*
         * Tests access to table values after creation from score: via copy and cell access.
         * Tests equivalence of classic and new threadsafe access (set/get) for tables.
         */
        [TestMethod]
        public void TestTableAccess()
        {

            Csound6Net cs = new Csound6Net();
            FileInfo csf = new FileInfo(@"csdFiles\xanadu.csd");
            CsoundStatus result = cs.CompileArgs(new string[] { csf.FullName });
            Assert.IsTrue(((int)result) >= 0);
            Assert.AreEqual(CsoundStatus.Success, cs.Start());//CompileArgs doesn't call start();
            bool done = cs.PerformKsmps(); //enough to get f1 to be created
            Assert.IsFalse(done);
            Csound6Table f1 = new Csound6Table(1, cs);
            Assert.IsTrue(f1.IsDefined);
            int len = f1.Length;
            Assert.AreEqual(8192, len); //xanadu tables are 8192 cells long.
            double[] f1dat = f1.Copy();//fetch f1 the legacy way
            Assert.IsNotNull(f1dat);
            Assert.AreEqual (len, f1dat.Length);
            Assert.AreEqual(f1dat[25], f1[25]);
            double[] f1safe = f1.CopyOut(); //fetch f1 the new threadsafe way
            f1[50] = .5;
            Assert.AreEqual(.5, f1[50]);
            f1[50] = f1dat[50];//swap out a few cells via this[index]
            Assert.AreEqual(f1dat[50], f1[50]);
            Assert.AreEqual(f1dat[25], f1[25]);
            for (int i = 0; i < f1.Length; i++)
            {
                Assert.AreEqual(f1dat[i], f1safe[i]);
            }
            //try out some of the other tables, test CopyIn.
            var f2 = new Csound6Table(2, cs);
            double[] f2dat = f2.CopyOut();
            Assert.AreEqual(f1dat.Length, f2dat.Length);
            for (int i = 0; i < 2048; i++)
            {
                Assert.AreEqual((int)(f1[i + 2048]*100000000), (int)(f2[i]*100000000));//f1 is sine, f2 is cosine
            }
            f1.CopyIn(f2dat);
            for (int i = 0; i < f1.Length; i++)
            {
                Assert.AreEqual(f1[i], f2[i]); //did CopyIn work?
            }
            f1.CopyIn(f1dat); //restore
            for (int i = 0; i < f1.Length; i++)
            {
                Assert.AreEqual(f1[i], f1safe[i]);
            }
            cs.Stop(); //shut csound down. (stop not relevant in single thread but call anyway to test.
            result = cs.Cleanup();
            Assert.IsTrue(result == CsoundStatus.Success);
            cs.Dispose();
        }

        /*
         * Tests feeding overrides to a csound instance, and compiling from strings (orc, sco)
         * Tests cleaning up and resetting csound to set up a new run.
         * Tests parsing to a tree, compiling from a tree and deleting the tree.
         * Tests advancing of CurrentTimeSamples property.
         */
        [TestMethod]
        public void TestCsound6Compiles()
        {
            string orc;
            string sco;
            string[] opts = "-R -W -d".Split(new char[] {' ', '\t'});
            using (var orcReader = new StreamReader("classicFiles\\Simple.orc"))
            {
                orc = orcReader.ReadToEnd();
            }
            using (var scoReader = new StreamReader("classicFiles\\Simple.sco"))
            {
                sco = scoReader.ReadToEnd();
            }
            using (var cs = new Csound6Net())
            {
                var parms = new Csound6Parameters(cs);
                Assert.IsTrue(parms.IsDisplayingGraphs);
                foreach (string opt in opts) Assert.IsTrue(parms.SetOption(opt) == CsoundStatus.Success);//try with object
                parms.RefreshParams();
                Assert.IsFalse(parms.IsDisplayingGraphs);
                cs.SetOutputFileName("simple1.wav", SoundFileType.TypWav, SampleFormat.AeFloat);
                Assert.AreEqual("simple1.wav", cs.OutputFileName);

                CsoundStatus result = cs.CompileOrc(orc);
                Assert.IsTrue(result == CsoundStatus.Success);

                int kcycles = 0;
                long lastSamp = 0;
                if (result == CsoundStatus.Success)
                {
                    result = cs.ReadScore(sco);
                    Assert.AreEqual(CsoundStatus.Success, result);
                    if (result == CsoundStatus.Success)
                    {
                        result = cs.Start();
                        while (!cs.PerformKsmps())
                        {
                            kcycles++;
                        }
                        Assert.IsTrue(kcycles > 0);
                        lastSamp = cs.CurrentTimeSamples;
                        Assert.IsTrue(lastSamp > 0L);
                        result = cs.Cleanup();
                        Assert.IsTrue(result == CsoundStatus.Success);
                    }
                }
                result = cs.Cleanup();
                Assert.AreEqual(CsoundStatus.Success, result);

                //Reset and try again with TREE-based signatures
                cs.Reset();
                //try parms via static method
                foreach (string opt in opts) Assert.IsTrue(Csound6Parameters.SetOption(cs, opt) == CsoundStatus.Success);
                cs.SetOutputFileName("simple2.wav", SoundFileType.TypWav, SampleFormat.AeFloat);
                IntPtr tree = cs.ParseOrc(orc);
                Assert.IsNotNull(tree);
                Assert.AreNotEqual(IntPtr.Zero, tree);
                result = cs.CompileTree(tree);
                Assert.AreEqual("simple2.wav", cs.OutputFileName);
                Assert.AreEqual(CsoundStatus.Success, result);
                cs.DeleteTree(tree);
                result = cs.ReadScore(sco);
                Assert.AreEqual(CsoundStatus.Success, cs.Start());
                long crntSamp = 0L;
                while (!cs.PerformBuffer())
                {
                    Assert.IsTrue(cs.CurrentTimeSamples > crntSamp);
                    crntSamp = cs.CurrentTimeSamples; 
                }
                Assert.AreEqual(lastSamp, cs.CurrentTimeSamples);//same file should yield same number of samples
                Assert.AreEqual(CsoundStatus.Success, cs.Cleanup());
            }//end using Csound6Net
        }

        /*
         * Tests non-trivial score performing using async signature.
         * Tests simple progress reporting.
         * Tests cancellation token able to abort process and shutdown via OperationCanceledException.
         */
        [TestMethod]
        public async Task TestPlayAsync()
        {
            using (Csound6Net cs = new Csound6Net())
            {
                var parms = cs.GetParameters();
                parms.MaximumThreadCount = 1;//tried cranking up to 4 cores, but this simple case actually slows down when > 1
                parms.IsRealtimeMode = true;
                var progress = new ProgressCounter();
                m_timenow = 0.0f; //should increase as progress is reported
                var cancel = new CancellationTokenSource();
                cancel.CancelAfter(500);//force a cancelation after 2 seconds of processing (full xanadu takes 7 seconds to render)
                try
                {
                    CsoundStatus result = await cs.PlayAsync(new FileInfo("csdFiles\\xanadu.csd"), progress, cancel.Token);
                    Assert.Fail("Reached unexpected completion in TestPlayAsync: expected cancellation not executed."); //shouldn't execute
                }
                catch (OperationCanceledException e)
                {
                    var token = e.CancellationToken;
                    Assert.AreEqual(cancel.Token, token);
                    Assert.IsTrue(token.IsCancellationRequested);
                }
                catch (Exception se)
                {
                    Assert.Fail("System Exception: {0}", se.Message);
                }
                Assert.IsTrue(cancel.IsCancellationRequested);
                Assert.IsTrue(m_timenow > 0.0f);//confirm that there was progress bar activity
                Assert.IsTrue(m_timenow > 2.0f); //confirm that we got at least the first 2 seconds processed.
                Assert.IsTrue(m_timenow < 40.0f); //xanadu lasts just under 60 seconds verify that final value well under that.
            }
        }

        /*
         * Tests PerformAsync after compile: and cancel (currently not working...)
         * Tests setting output file after compile
         */
        [TestMethod]
        public async Task TestPerformAsync()
        {
            using (var cs = new Csound6Net())
            {
                var cancel = new CancellationTokenSource();
                try
                {
                    FileInfo csf = new FileInfo(@"csdFiles\xanadu.csd");
                    CsoundStatus result = await cs.CompileAsync(new string[] { csf.FullName });
                    cs.SetOutputFileName("f:\\PerfXanadu.wav", SoundFileType.TypWav, SampleFormat.AeShort);
                    string s = cs.OutputFileName;
                    Assert.AreEqual(CsoundStatus.Success, result);
                    cancel.CancelAfter(1500);//cancel timeout avter 1.5 seconds of computation
                    Assert.IsFalse(cancel.Token.IsCancellationRequested);
                    result = await cs.PerformAsync(null, cancel.Token);
                    Assert.Fail("Reached unexpected completion in TestPlayAsync: expected cancellation not executed."); //shouldn't execute
                }
                catch (OperationCanceledException ce)
                {//Verify that cancelation timeout occurred in a timely way.
                    var token = ce.CancellationToken;
                    Assert.AreEqual(cancel.Token, token);
                    Assert.IsTrue(token.IsCancellationRequested);
                    Assert.IsTrue(cs.ScoreTime < 50.0);
                }
                catch (Csound6NetException e)
                {
                    Assert.Fail("TestSimplePerform Error: {0}", e.Message);
                }
                catch (Exception se)
                {
                    Assert.Fail("System Exception: {0}", se.Message);
                }
                var time = cs.ScoreTime;//Confirm that cancelation worked: goes 60 sec - time at cancel...
                Assert.IsTrue(cancel.Token.IsCancellationRequested);
                Assert.IsTrue(time < 50.0);
            }
        }

        /*
         * Tests async processing getting hardware enumeration of audio input and output devices.
         * Tests async, struct marshalling (by value strings), auto bool convertion from int and array of structs.
         */
        [TestMethod]
        public async Task TestAudioMidiDeviceAccess()
        {
            string midimod = "mme";
            string audiomod = "portaudio";
            using (Csound6Net cs = new Csound6Net())
            {
                var mods = cs.GetAudioModuleList();//Csound6Net initializes to "pa"; confirm this is true.
                int cnt = mods.Count;
                Assert.IsTrue(cnt > 0);
                Assert.IsTrue(mods.ContainsKey("pa"));
                Assert.IsFalse(mods.ContainsKey(audiomod));
                cs.SetAudioModule(audiomod); //"portaudio" won't be there yet... add it.
                mods = cs.GetAudioModuleList();
                Assert.AreEqual(cnt + 1, mods.Count);
                Assert.IsTrue(mods.ContainsKey(audiomod)); //confirm it was added

                IDictionary<string, CS_AUDIODEVICE> outputDevices = await cs.GetAudioDeviceListAsync(true);
                Assert.IsNotNull(outputDevices);
                Assert.AreNotEqual(0, outputDevices.Count);
                foreach (string id in outputDevices.Keys)
                {
                    Assert.IsNotNull(outputDevices[id]);
                    Assert.IsInstanceOfType(outputDevices[id], typeof(CS_AUDIODEVICE));
                    var dev = outputDevices[id];
                    Assert.IsTrue(dev.isOutput);
                    Assert.AreEqual(id, dev.device_id);
                    Assert.AreEqual(audiomod, dev.rt_module);
                    
                }
                IDictionary<string, CS_AUDIODEVICE> inputDevices = await cs.GetAudioDeviceListAsync(false);
                Assert.IsNotNull(inputDevices);
                Assert.AreNotEqual(0, inputDevices.Count);
                foreach (string id in inputDevices.Keys)
                {
                    Assert.IsNotNull(inputDevices[id]);
                    Assert.IsInstanceOfType(inputDevices[id], typeof(CS_AUDIODEVICE));
                    var dev = inputDevices[id];
                    Assert.IsFalse(dev.isOutput);
                    Assert.AreEqual(id, dev.device_id);
                }

                mods = cs.GetMidiModuleList();
                Assert.IsTrue(mods.Count > 0);
                Assert.IsTrue(mods.ContainsKey("portmidi"));//unlike for audio, csounds sets and keeps it.
                Csound6Parameters.SetOption(cs, "-+rtmidi=winmme");//none this has an effect... stays portaudio
                cs.SetMidiModule(midimod);
                mods = cs.GetMidiModuleList();
                //count not increased - winmme and portmidi considered same? although, devices will say winmme or anything else you put there.
                bool hasMME = mods.ContainsKey(midimod);

                var midiInput = await cs.GetMidiDeviceListAsync(false);
                Assert.IsNotNull(midiInput);
                Assert.AreNotEqual(0, midiInput.Count);
                foreach (string id in midiInput.Keys)
                {
                    Assert.IsNotNull(midiInput[id]);
                    Assert.IsInstanceOfType(midiInput[id], typeof(CS_MIDIDEVICE));
                    var dev = midiInput[id];
                    Assert.IsFalse(dev.isOutput);
                    Assert.AreEqual(id, dev.device_id);
                    Assert.AreEqual(midimod, dev.midi_module);
                } 

                var midiOutput = await cs.GetMidiDeviceListAsync(true);
                Assert.IsNotNull(midiOutput);
                Assert.AreNotEqual(0, midiOutput.Count);
                foreach (string id in midiOutput.Keys)
                {
                    Assert.IsNotNull(midiOutput[id]);
                    Assert.IsInstanceOfType(midiOutput[id], typeof(CS_MIDIDEVICE));
                    var dev = midiOutput[id];
                    Assert.IsTrue(dev.isOutput);
                    Assert.AreEqual(id, dev.device_id);
                }
            }
        }

        /*
         * Tests that localized resources can work at a basic level
         */
        [TestMethod]
        public void TestLocalization()
        {
            try
            {
                throw new Csound6NetException("CreateFailed", CsoundStatus.InitializationError);
            }
            catch (Csound6NetException e)
            {
                Assert.IsTrue(e.Message.EndsWith("InitializationError"));
            }
            try
            {
                throw new Csound6NetException("CsoundEngineMismatch", "Richard Henninger");
            }
            catch (Csound6NetException e1)
            {
                Assert.IsTrue(e1.Message.Contains(" Richard Henninger "));
            }
            try
            {
                throw new Csound6NetException("CscoreFailed", "My Score", CsoundStatus.MemoryAllocationFailure);
            }
            catch (Csound6NetException e2)
            {
                Assert.IsTrue(e2.Message.Contains(" My Score "));
                Assert.IsTrue(e2.Message.EndsWith("AllocationFailure"));
            }
        }

        /*
         * Tests RunAsync directly, via csound's proxy and as a utility.
         * Runs same process via three mechanisms and compares output which should be same.
         * Tests running a Utility both in direct creation and via GetUtility
         */
        [TestMethod]
        public async Task TestExternalRun()
        {
            var fi = new FileInfo("soundFiles\\rv_mono.wav");
            string pathToSoundfile = BridgeToCpInvoke.wGetShortPathName(fi.FullName);
            var p = new CsoundExternalProcess();
            p.ProgramName = "sndinfo";
            p.Arguments = new List<string>(new string[] {pathToSoundfile });
            m_count = 0;
            m_messageText = new StringBuilder();
            p.MessageCallback += TestMessageEvent;
            int result = await p.RunAsync(CancellationToken.None);
            Assert.AreEqual(0, result);
            Assert.IsTrue(m_count > 0);
            string text = m_messageText.ToString();
            Assert.IsTrue(text.Length > 0);
            Assert.IsTrue(text.Contains("srate 44100, monaural, 16 bit WAV"));

            using (var cs = new Csound6Net())
            {
                int count1 = m_count;
                m_count = 0;
                m_messageText = new StringBuilder();
                long done = await cs.RunCommandAsync(new string[] { "sndinfo", pathToSoundfile }, TestMessageEvent, CancellationToken.None);
                string text2 = m_messageText.ToString();
                Assert.AreEqual(0L, done);
                Assert.AreEqual(count1, m_count);
                Assert.AreEqual(text.Substring(0, 150), text2.Substring(0,150));//1st 150 chars should be the same - no variable statistics.

                int count2 = m_count;
                m_count = 0;
                m_messageText = new StringBuilder();
                var sndinfo = cs.GetUtility(Csound6Utility.SHOW_SOUNDFILE_METADATA);
                sndinfo.InputFile = new FileInfo(pathToSoundfile);
                int done2 = await sndinfo.RunAsync(TestMessageEvent, CancellationToken.None);
                Assert.AreEqual(0, done2);
                Assert.IsTrue(count2 >= m_count);//sometimes 13, sometimes 14 - leading crlf
                string text3 = m_messageText.ToString();
                Assert.AreEqual(text.Substring(0, 150), text3.Substring(0, 150));
            }
        }


        /*
         * Tests whether midi input file can be specified via api rather than via csd option.
         * Plays dac
         */
        [TestMethod]
        public void TestMidiFileUse()
        {
            using (var cs = new Csound6Net())
            {
                FileInfo fi = new FileInfo("pgmassign_advanced.wav");
                if (fi.Exists) fi.Delete();
                fi.Refresh();
                Assert.IsFalse(fi.Exists);
                FileInfo csf = new FileInfo(@"csdFiles\pgmassign_advanced.csd");
                var parms = cs.GetParameters();
                Assert.IsFalse(parms.IsDoneWhenMidiDone);
                parms.SetOption("-T");
                parms = cs.GetParameters();
                Assert.IsTrue(parms.IsDoneWhenMidiDone);
                cs.SetMidiFileInput("midiFiles/pgmassign_advanced.mid");
                cs.SetOutputFileName(fi.Name, SoundFileType.TypWav, SampleFormat.AeShort);
                CsoundStatus result = cs.Compile(new string[] { csf.FullName });
                Assert.AreEqual(CsoundStatus.Success, result);
                Assert.AreEqual(0.0, cs.ScoreTime);
                cs.Perform();
                var time = cs.ScoreTime;
                Assert.IsTrue(cs.ScoreTime > 0.0);
                Assert.IsTrue(cs.ScoreTime > 3.50);
                fi.Refresh();
                Assert.IsTrue(fi.Exists);
                Assert.IsTrue(fi.Length > 0);
                Assert.IsTrue(fi.Length > 1000);
            }
      }

        /*
         * Tests functionality of RAII objects which represent objects which can be used during csound runs.
         * Test Csound6Rand31, CsoundRandMT, Csound6Utility, Csound6Timer, Csound6GlobalVariable
         */
        [TestMethod]
        public void TestMiscellaneousObjects()
        {
            var rnd31 = new Csound6Rand31();
            Assert.IsInstanceOfType(rnd31, typeof(Csound6Rand31));
            ISet<int> vals = new HashSet<int>();
            for (int i = 0; i < 100; i++)
            {
               int val = rnd31.next();
               Assert.IsFalse(vals.Contains(val));
               vals.Add(val);
            }
            var rndMT = new Csound6RandMT();
            Assert.IsInstanceOfType(rndMT, typeof(Csound6RandMT));
            ISet<uint> uvals = new HashSet<uint>();
            for (int i = 0; i < 100; i++)
            {
               uint uval = (uint)rndMT.Next();
               Assert.IsFalse(uvals.Contains(uval));
               uvals.Add(uval);
            }

            var timer = new Csound6Timer();
            Thread.Sleep(100);
            double cpu = timer.CPUTime;
            double realt = timer.RealTime;
            Thread.Sleep(100);
            Assert.IsTrue(timer.CPUTime > cpu);
            double newreal = timer.RealTime;
        //    Assert.IsTrue(timer.RealTime > realt); //realtime isn't working in csound?

            using (var cs = new Csound6Net())
            {
                var gv1 = new Csound6GlobalVariable<int>("MyInt", cs);
                Assert.AreEqual(4, gv1.Size);
                gv1.Value = 255;
                int x = gv1.Value;
                Assert.AreEqual(255, x);
                var gv2 = new Csound6GlobalVariable<double>("MyDouble", cs);
                gv2.Value = 3.14;
                float y = (float)gv2.Value;
                Assert.AreEqual(3.14f, y);

                //Test strings, structs and classes
                var gv3 = new Csound6GlobalStringVariable("MyString", cs);
                gv3.Value = "";
                Assert.AreEqual(string.Empty, gv3.Value);
                
                string testString = "This is Richard's String";
                gv3.Value = testString;
                string s = gv3.Value;
                Assert.AreEqual(testString, s);

                var gv4 = new Csound6GlobalVariable<CSOUND_PARAMS>("MyParms", cs);
                gv4.Value = cs.GetParameters().GetParams(new CSOUND_PARAMS());
                Assert.AreEqual(135, (int)gv4.Value.message_level);
                var parms = gv4.Value;
                parms.hardware_buffer_frames = 2048;
                Assert.AreNotEqual(gv4.Value.hardware_buffer_frames, parms.hardware_buffer_frames);
                gv4.Value = parms;
                Assert.AreEqual(2048, gv4.Value.hardware_buffer_frames);
                Assert.AreEqual(135, (int)gv4.Value.message_level);
                Assert.AreEqual(1, gv4.Value.csd_line_counts);

            }

            using (var cs = new Csound6Net())
            {
                var utilities = cs.GetUtilities();
                Assert.IsTrue(utilities.Count > 0);
                foreach (string name in utilities.Keys)
                {
                    Assert.IsFalse(string.IsNullOrWhiteSpace(name));
                    var utility = utilities[name];
                    Assert.IsInstanceOfType(utility, typeof(Csound6Utility),string.Format("Utility <{0}> is not valid type, is type={1}", name, utility));
                    string desc = utility.Description;
                    Assert.IsFalse(string.IsNullOrWhiteSpace(desc));
                }
            }
        }

       
        //Support method for capturing csound messaging events: used in TestRuntimeProperties
        public void TestMessageEvent(object source, Csound6MessageEventArgs args)
        {
            m_messageText.Append(args.Message);
            m_count++;
        }

        //Support method for capturing FileOpen events: used in TestRuntimeProperties
        public void TestFileOpenEvent(object source, Csound6FileOpenEventArgs args)
        {
            Assert.IsNotNull(args.Path);
            Assert.IsTrue(args.Path.Length > 0);
            Assert.IsFalse(args.IsTemporary);
            if (args.IsWriting)
            {
                Assert.AreEqual(CsfType.Wave, args.FileType);
                Assert.IsTrue(args.Path.ToLower().EndsWith(".wav"));
            }
            else
            {
                Assert.AreEqual(CsfType.UnifiedCsd, args.FileType);
                Assert.IsTrue(args.Path.ToLower().EndsWith(".csd"));
            }
            m_fileopened = true;
        }

        //Support method for testing progress reporting: used in TestSimplePlayAsync
        private class ProgressCounter : IProgress<float>
        {
            public void Report(float value)
            {
                TestCsound.m_timenow = value;
            }
        }


    }
}
