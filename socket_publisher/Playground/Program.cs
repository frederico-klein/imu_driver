using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using Waveplus.DaqSys;
using Waveplus.DaqSysInterface;
using WaveplusLab.Shared.Definitions;

namespace Playground
{
    class Program
    {
        static void Main()
        {
            new Program();
        }

        bool FAKEDAQ = false;
        DaqSystem daqSystem;
        DateTime starttime = DateTime.UtcNow;
        DateTime previoustime = DateTime.UtcNow;

        UDPSocket c = new UDPSocket();

        Dictionary<int, string> imu_dict =
               new Dictionary<int, string>();

        int numframes = 0;

        TextWriter tw = new StreamWriter("output.raw", true);

        readonly List<KeyValuePair<string, Quaternion>> ra = Allcases();

        public DataAvailableEventArgs DistrDaq(string[] trow)
        {
            DataAvailableEventArgs e = new DataAvailableEventArgs();
            e.Samples = new float[32, 32000];
            e.ImuSamples = new float[32, 4, 32000];
            e.AccelerometerSamples = new float[32, 3, 32000];
            e.GyroscopeSamples = new float[32, 3, 32000];
            e.MagnetometerSamples = new float[32, 3, 32000];
            string[] row = trow.Skip(1).ToArray();
            float time = float.Parse(trow[0]); 
 
            int j = 0;
            foreach (KeyValuePair<int, string> ele1 in imu_dict)
                {
                int i = ele1.Key - 1; // needs proofing
                string imu = ele1.Value;
                Debug.Print("Evaling imu( {0} ): {1}", i.ToString(), imu);
                // now there is a fixed sequence which i must follow
                //q1,q2,q3,q4
                //ax,ay,az
                //gx,gy,gz
                //mx,my,mz
                //barometer
                //linAcc(x,y,z)
                //altitude
                int I = j * 18;
                Debug.Print("I: {0}, j: {1}",I.ToString(), j.ToString() );
                j++;
                e.ImuSamples[i, 0, 0] = float.Parse(row[I + 0]);
                e.ImuSamples[i, 1, 0] = float.Parse(row[I + 1]);
                e.ImuSamples[i, 2, 0] = float.Parse(row[I + 2]);
                e.ImuSamples[i, 3, 0] = float.Parse(row[I + 3]);
                e.AccelerometerSamples[i, 0, 0] = float.Parse(row[I + 4]);
                e.AccelerometerSamples[i, 1, 0] = float.Parse(row[I + 5]);
                e.AccelerometerSamples[i, 2, 0] = float.Parse(row[I + 6]);
                e.GyroscopeSamples[i, 0, 0] = float.Parse(row[I + 7]);
                e.GyroscopeSamples[i, 1, 0] = float.Parse(row[I + 8]);
                e.GyroscopeSamples[i, 2, 0] = float.Parse(row[I + 9]);
                e.MagnetometerSamples[i, 0, 0] = float.Parse(row [I + 10]);
                e.MagnetometerSamples[i, 1, 0] = float.Parse(row [I + 11]);
                e.MagnetometerSamples[i, 2, 0] = float.Parse(row [I + 12]);
            }
            return e;
        }

        public static Vector3 ToEulerAngles(Quaternion q)
        {
            Vector3 angles = new Vector3();

            // roll / x
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch / y
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
            {
                if (sinp>0)
                    angles.Y = (float)Math.PI / 2;
                else
                    angles.Y = -(float)Math.PI / 2;
            }
            else
            {
                angles.Y = (float)Math.Asin(sinp);
            }

            // yaw / z
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }

        public static Quaternion ToQuaternion(Vector3 v)
        {

            float cy = (float)Math.Cos(v.Z * 0.5);
            float sy = (float)Math.Sin(v.Z * 0.5);
            float cp = (float)Math.Cos(v.Y * 0.5);
            float sp = (float)Math.Sin(v.Y * 0.5);
            float cr = (float)Math.Cos(v.X * 0.5);
            float sr = (float)Math.Sin(v.X * 0.5);

            return new Quaternion
            {
                W = (cr * cp * cy + sr * sp * sy),
                X = (sr * cp * cy - cr * sp * sy),
                Y = (cr * sp * cy + sr * cp * sy),
                Z = (cr * cp * sy - sr * sp * cy)
            };

        }

        static Quaternion Getsign(int i)
        {
            float a, b, c, d;

            if (i >= 8)
            {
                d = 1f;
                i -= 8;
            }
            else
            {
                d = -1f;
            }
            if (i >= 4)
            {
                c = 1f;
                i -= 4;
            }
            else
            {
                c = -1f;
            }
            if (i >= 2)
            {
                b = 1f;
                i -= 2;
            }
            else
            {
                b = -1f;
            }

            if (i >= 1)
            {
                a = 1f;
                //i -= 1;
            }
            else
            {
                a = -1;
            }
            Quaternion q = new Quaternion(a, b, c, d);
            Console.WriteLine(q.ToString());
            return q;
        }


        static Vector3 Coco(int i)
        {
            if (i == 0)
                return new Vector3(0, 1, 2);
            if (i == 1)
                return new Vector3(0, 2, 1);
            if (i == 2)
                return new Vector3(1, 0, 2);
            if (i == 3)
                return new Vector3(1, 2, 0);
            if (i == 4)
                return new Vector3(2, 0, 1);
            if (i == 5)
                return new Vector3(2, 1, 0);
            throw new Exception("i>5!");
        }


        static List<KeyValuePair<string, Quaternion>> Allcases()
        {
            List<KeyValuePair<string, Quaternion>> ra = new List<KeyValuePair<string, Quaternion>> { };

            for (int i = 0; i < 6; i++)
            {
                for (int d = 0; d < 16; d++) // 4 bits so 16 possibilities, to get the signs we will need an auxiliary function
                {
                    Vector3 v = Coco(i);
                    Quaternion qm = Getsign(d);
                    //now elementwise multiplication of qm
                    //my v3 is a,b,c

                    ra.Add(new KeyValuePair<string, Quaternion>(string.Format("{{3}} {{{0}}} {{{1}}} {{{2}}} ", v.X, v.Y, v.Z), qm));
                    ra.Add(new KeyValuePair<string, Quaternion>(string.Format("{{{0}}} {{{1}}} {{{2}}} {{3}} ", v.X, v.Y, v.Z), qm));

                }

            }
            return ra;
        }

        static string PermutatedQuarternion(KeyValuePair<string, Quaternion> item, Quaternion q)
        {
            Console.WriteLine("getme sequence: {0}", item.Key);
            Console.WriteLine("getme quaternion signs: {0}", item.Value);
            string qs = string.Format(item.Key, q.X * item.Value.X, q.Y * item.Value.Y, q.Z * item.Value.Z, q.W * item.Value.W);
            //Console.WriteLine("{0}:::: {1},,,, {2}", item.Key, item.Value);
            Console.WriteLine(qs);
            return qs;
        }

        //0========================================================================================================================
        public string eEeParser(DataAvailableEventArgs e, int sampleNumber)
        {
            numframes++;
            TimeSpan t = DateTime.UtcNow - starttime;
            string output =  t.TotalSeconds.ToString() + " ";
            foreach (KeyValuePair<int, string> ele1 in imu_dict)
            {
                int i = ele1.Key-1;

                string imu = ele1.Value;
                Debug.Print("imu {0} (index in e array: {1}) NAME: {2}", ele1.Key, i , imu );
                float q0, q1, q2, q3;
                q0 = e.ImuSamples[i, 0, sampleNumber];
                q1 = e.ImuSamples[i, 1, sampleNumber];
                q2 = e.ImuSamples[i, 2, sampleNumber];
                q3 = e.ImuSamples[i, 3, sampleNumber];
               
                Debug.Print("quarternions             : {0}, {1}, {2}, {3}", q0,q1,q2,q3);

                /*output += false ? "0.0 " : q0.ToString() + " ";
                output += false ? "0.0 " : q1.ToString() + " ";
                output += false ? "0.0 " : q2.ToString() + " ";
                output += false ? "0.0 " : q3.ToString() + " ";                */
                Quaternion q = new Quaternion(q0, q1, q2, q3);
                int index = 0 + (numframes / 100);
                if (index > (numframes - 1) / 100)
                    Debug.Print("new position now! {0}", index);
                if (index > ra.Count())
                    throw new Exception("reached the end");
                output += PermutatedQuarternion(ra[index], q);

                output += !false ? "0.0 " : e.AccelerometerSamples[i,0, sampleNumber].ToString() + " ";
                output += !false ? "0.0 " : e.AccelerometerSamples[i,1, sampleNumber].ToString() + " ";
                output += !false ? "0.0 " : e.AccelerometerSamples[i,2, sampleNumber].ToString() + " ";
                output += !false ? "0.0 " : e.GyroscopeSamples[i,0, sampleNumber].ToString() + " ";
                output += !false ? "0.0 " : e.GyroscopeSamples[i,1, sampleNumber].ToString() + " ";
                output += !false ? "0.0 " : e.GyroscopeSamples[i,2, sampleNumber].ToString() + " ";
                output += !false ? "0.0 " : e.MagnetometerSamples[i,0, sampleNumber].ToString() + " ";
                output += !false ? "0.0 " : e.MagnetometerSamples[i,1, sampleNumber].ToString() + " ";
                output += !false ? "0.0 " : e.MagnetometerSamples[i,2, sampleNumber].ToString() + " ";
                output += "0.0 "; //barometer
                output += "0.0 "; //linAccx
                output += "0.0 "; //linAccy
                output += "0.0 "; //linAccz
                output += "0.0 "; //altitude 
            }
            //Debug.Print("Parsed output: "+ output);
            return output;       
        
        }

        public Program()
        {
            TextWriterTraceListener tr1 = new TextWriterTraceListener(System.Console.Out);
            Debug.Listeners.Add(tr1);

            var logindex = 0;
            string currfile;
            do {
                currfile = "output" + logindex.ToString() + ".log";
                logindex++;
            }
            while (File.Exists(currfile));
            TextWriterTraceListener tr2 = new TextWriterTraceListener(System.IO.File.CreateText(currfile));
            //TextWriterTraceListener tr2 = new TextWriterTraceListener(System.IO.File.CreateText("Output.log"));
            Debug.Listeners.Add(tr2);

            Debug.Flush();

            if (FAKEDAQ)
                Debug.Print("RUNNING WITH FAKE INPUT");
            else
                Debug.Print("RUNNING WITH REAL INPUT");
            //Console.ReadKey();
            ConfigureDaq(); 

            imu_dict.Add(11, "TORAX");
            imu_dict.Add(12, "HUMERUS");
            imu_dict.Add(13, "RADIUS");

            StartServer();
   
            bool UPPER = true;
            var lowbodyfile = @"D:\frekle\Documents\githbu\imu_driver\socket_publisher\gait1992_imu.csv";
            var uppbodyfile = @"D:\frekle\Documents\githbu\imu_driver\socket_publisher\mobl2016_imu.csv";
            string dasfile;
            if (UPPER)
                dasfile = uppbodyfile;
            else
                dasfile = lowbodyfile;
            if (FAKEDAQ)
                Debug.Print("das file {0}", dasfile);

            if (FAKEDAQ)
            {
                Debug.Print("Starting FAKE capture");
                using (var reader = new StreamReader(dasfile))
                {
                    DataAvailableEventArgs e = new DataAvailableEventArgs();
          
                    reader.ReadLine();
                    reader.ReadLine();
                    reader.ReadLine();
                    reader.ReadLine();

                    reader.ReadLine(); // labels
                    while (FAKEDAQ && !reader.EndOfStream)
                    {
                        Debug.Print("LOOP");
                        var line = reader.ReadLine();
                        Debug.Print("rowread:" + line);
                        var values = line.Split(',');

                        e = DistrDaq(values);

                        e.ScanNumber = 1;
                        Capture_DataAvailable(null, e);

                        //System.Threading.Thread.Sleep(25);
                        System.Threading.Thread.Sleep(1); //original
                    }
                }
            }
            else
            {
                Debug.Print("Starting capture");
                daqSystem.StartCapturing(DataAvailableEventPeriod.ms_25); // Available: 100, 50, 25, 10            
            }
            Debug.Print("Finished!");
            Console.ReadKey();
            c.Send("BYE!");
            tw.Close();
            Debug.Flush();
        }

        private void StartServer()
        {
            string client_ip = "127.0.0.1";
            int client_port = 8080;
            Debug.Print("Will spit data as udp in {0}, {1}", client_ip, client_port.ToString());
            // creates udp server
            c.Client(client_ip, client_port);

        }

        private void ConfigureDaq()
        {
            // Create daqSystem object and assign the event handlers
            daqSystem = new DaqSystem();
            daqSystem.StateChanged += Device_StateChanged;
            daqSystem.DataAvailable += Capture_DataAvailable;

            // Configure sensors
            // .InstalledSensors = 16, not the number of sensed sensors
            // Configure sensors from channel 1-8 as EMG sensors, 9-16 as IMU sensors
            for (int EMGsensorNumber = 0; EMGsensorNumber < daqSystem.InstalledSensors - 8; EMGsensorNumber++)
            {
                Debug.Print("Configuring EMG sensor #" + EMGsensorNumber);
                daqSystem.ConfigureSensor(
                    new SensorConfiguration { SensorType = SensorType.EMG_SENSOR },
                    EMGsensorNumber
                );
            }

            for (int IMUsensorNumber = 8; IMUsensorNumber < daqSystem.InstalledSensors; IMUsensorNumber++)
            {
                Debug.Print("Configuring IMU sensor #" + IMUsensorNumber);
                daqSystem.ConfigureSensor(
                    new SensorConfiguration { SensorType = SensorType.INERTIAL_SENSOR },
                    IMUsensorNumber
                );
            }

            Debug.Print("Configuring capture");
            var new_config = new CaptureConfiguration { SamplingRate = SamplingRate.Hz_2000, IMU_AcqType = ImuAcqType.Fused9xData_142Hz };
            //var old_config = new CaptureConfiguration { SamplingRate = SamplingRate.Hz_2000, IMU_AcqType = ImuAcqType.RawData };
            //daqSystem.ConfigureCapture(old_config);
            daqSystem.ConfigureCapture(new_config);
        }

        private void Capture_DataAvailable(object sender, DataAvailableEventArgs e)
        {
            int samplesPerChannel = e.ScanNumber; // what's this?
            Debug.Print("Scan number: " + e.ScanNumber);
            TimeSpan thistime = DateTime.UtcNow - previoustime;

            for (int sampleNumber = 0; sampleNumber < samplesPerChannel; sampleNumber = sampleNumber + 1) // This loops captures data from sensor # sampleNumber+1
            {
                /*
                Debug.Print("=====");
                Debug.Indent();
                Debug.Print("samplenumber: "+ sampleNumber.ToString());
                eEeParser(e, sampleNumber);
                Debug.Unindent();
                Debug.Print("=====");
                */

                //send to server here? or below?
            }
            numframes++;
            
            Debug.Print("time: {0}", thistime.TotalSeconds.ToString());

            string outputt = eEeParser(e, 0);
            Debug.Print("VALUES TO BE SENT: {0}",outputt);
            c.Send(outputt);
            tw.WriteLine(outputt);
            Debug.Print("Values have been sent.");
            previoustime = DateTime.UtcNow;
        }

        private void Device_StateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            Debug.Print(e.ToString());
        }
    }

}

/* 
 *  //Quaternion QQ = new Quaternion(q0, q1, q2, q3);
                Quaternion QQ = new Quaternion(q0, q1, q2, q3);
                Vector3 momo = ToEulerAngles(QQ);

                Debug.Print("momo {0}",momo);

                //Vector3 nuno                      = new Vector3(momo.Y,momo.Z,momo.X);
                //Vector3 BA                        = new Vector3(momo.Y,0, 0); 
                //Vector3 fgh                       = new Vector3(momo.Y,momo.Z, momo.X); 
                //Vector3 laotd                       = new Vector3(-momo.Z,-momo.Y, momo.X); 
                //Vector3 la2                         = new Vector3(momo.Z,-momo.Y, momo.X); 
                //Vector3 la3                         = new Vector3(momo.Z + (float)Math.PI / 2, -momo.Y, momo.X); 
                //Vector3 la4                         = new Vector3(momo.Z , -momo.Y + (float)Math.PI / 2, momo.X);
                Vector3 la5                         = new Vector3(momo.Z , momo.Y , momo.X + (float)Math.PI / 2);
                //Vector3 thisisnotgonnaworknowisit = new Vector3(momo.Z,momo.Y, momo.X); 
                //Vector3 babacapoop                = new Vector3(momo.Y,momo.X, momo.Z); 

                //Vector3 caca = new Vector3(0,0, momo.X); //// OMEDETOU, I GOT THE CORRECT ORIENTATION FOR ZED!
                //Vector3 ba = new Vector3(0,0, momo.Z);
                //Vector3 abba = new Vector3(0,0, momo.Y);
                //Vector3 toto = new Vector3(momo.Z,momo.X, momo.Y);
                //Vector3 nuno = new Vector3(momo.X,momo.Y,momo.Z);

                //Quaternion QQQ = ToQuaternion(nuno);
                //Quaternion QQQ = ToQuaternion(poop);
                Quaternion QQQ = ToQuaternion(la5);
                //Quaternion QQQ = ToQuaternion(caca); Z CORRECT
                //Quaternion QQQ = ToQuaternion(ba);
                //Quaternion QQQ = ToQuaternion(abba);
                //Quaternion QQQ = ToQuaternion(toto);

                //Quaternion QQQ = ToQuaternion(momo);

                Debug.Print("regenerated qrt: {0}", QQQ);

                Quaternion Q = new Quaternion(q0, q1, q2, q3); // i think x and z are flipped, this is a
                Quaternion Q1 = new Quaternion(q2, q1, -q0, q3); // i think x and z are flipped, this is a
                Quaternion Q2 = new Quaternion(-q2, q1, q0, q3); // i think x and z are flipped, this is a
                Quaternion Qr = new Quaternion(0, 0.7071f, 0.7071f, 0); // i think x and z are flipped, this is a
 
                Quaternion Q3 = Quaternion.Concatenate(QQ, Qr);

                //Debug.Print(e.ImuSamples);
                Debug.Print("Quaternion original: {0}",Q.ToString());
                Debug.Print("Quaternion: {0}",Q1.ToString());
                Debug.Print("Quaternion: {0}",Q2.ToString());
                Debug.Print("Quaternion: {0}",Q3.ToString());

                //foreach (int j in e.ImuSamples)
                //{
                //    Console.Write("{0} ", j);
                //}
                //

                ///currently this is not correct:
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 */