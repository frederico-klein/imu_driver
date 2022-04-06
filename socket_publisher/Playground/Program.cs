using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Threading;
using Waveplus.DaqSys;
using Waveplus.DaqSysInterface;
using WaveplusLab.Shared.Definitions;

namespace Playground
{
    class Program
    {
        //bool FAKEDAQ;
        //string dasfile;
        string outfile;

        DaqSystem daqSystem;
        readonly DateTime starttime = DateTime.UtcNow;

        readonly string[] imutable = { "_q1",       "_q2",       "_q3",      "_q4",        "_ax",
            "_ay",       "_az",       "_gx",      "_gy",        "_gz",
            "_mx",       "_my",       "_mz",      "_barometer", "_linAcc_x",
            "_linAcc_y", "_linAcc_z", "_altitude" };

        UDPSocket c = new UDPSocket();

        //Dictionary<int, string> imu_dict = new Dictionary<int, string>();

        //StreamWriter textWriter;

        int numsamples = 0;
        
        static int Main(string[] args)
        {
            CultureInfo ci = new CultureInfo("en-UK");

            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            // Display the number of command line arguments.
            Console.WriteLine(args.Length);

            string csvoutputfilename = "myfile.csv";
            string ip = "127.0.0.1";
            int port = 8080;
            bool FAKÉ = false;
            string csvinputfilename = "";
            int period = 10; // in milliseconds
            bool RAW = false;

            Dictionary<int, string> imu_dict = new Dictionary<int, string>
            {
                { 11, "TORAX" },
                { 12, "HUMERUS" },
                { 13, "RADIUS" }
            };

            if (args.Length == 0)
            {
                Console.WriteLine("Assuming I'm connecting to DAQ.");
                new Program(false, "", csvoutputfilename, ip, port, imu_dict, period, RAW);
                return 0;
            }
            //var command = args[0];

            foreach (var command in args)

                switch (command)
                {
                    case "--help":
                        PrintHelp();
                        return 0;
                        //break;
                    case string s when s.StartsWith("--fake"):
                        csvinputfilename = s.Substring(s.IndexOf("--fake=")+7); 
                        FAKÉ = true;
                        break;
                    case string s when s.StartsWith("--ip"):
                        ip = s.Substring(s.IndexOf("--ip=") + 5);
                        break;
                    case string s when s.StartsWith("--port"):
                        port = Int32.Parse(s.Substring(s.IndexOf("--port=") + 7));
                        break;
                    case string s when s.StartsWith("--period"): // Available: 100, 50, 25, 10 
                        period = Int32.Parse(s.Substring(s.IndexOf("--period=") + 9));
                        break;  //not that easy to change
                    default:
                        //new Program(false,);
                        Console.WriteLine("Invalid command");
                        return -1;
                        //break;
                }

            new Program(FAKÉ, csvinputfilename, csvoutputfilename, ip, port, imu_dict, period, RAW);

            return 0;
        }

        static void PrintHelp()
        {
            Console.WriteLine("Usage: ");
            Console.WriteLine(" Playground.exe fake -f c:/mypath/somefile.csv ");
            Console.WriteLine(" or ");
            Console.WriteLine(" Playground.exe ");

        }

        public DataAvailableEventArgs DistrDaq(string[] trow, Dictionary<int, string> imu_dict)
        {
            int RANGE = 32;
            int RANGE_EMG = 32000;
            DataAvailableEventArgs e = new DataAvailableEventArgs
            {
                Samples = new float[32, RANGE_EMG],
                ImuSamples = new float[32, 4, RANGE],
                AccelerometerSamples = new float[32, 3, RANGE],
                GyroscopeSamples = new float[32, 3, RANGE],
                MagnetometerSamples = new float[32, 3, RANGE]
            };
            string[] row = trow.Skip(1).ToArray();
            float time = float.Parse(trow[0]); 
 
            int j = 0;
            foreach (KeyValuePair<int, string> ele1 in imu_dict)
                {
                int i = ele1.Key-1; // Because there is no IMU 0
                string imu = ele1.Value;
                //Console.WriteLine("FAKEDAQ: Evaling imu( {0} ): {1}", i.ToString(), imu);
                // now there is a fixed sequence which i must follow
                //q1,q2,q3,q4
                //ax,ay,az
                //gx,gy,gz
                //mx,my,mz
                //barometer
                //linAcc(x,y,z)
                //altitude
                int I = j * 18;
                //Console.WriteLine("FAKEDAQ: I: {0}, j: {1}",I.ToString(), j.ToString() );
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

        public Quaternion GetQuaternion(DataAvailableEventArgs e, int sampleNumber, int imuNumber)
        {
            Quaternion q = new Quaternion
            {
                W = e.ImuSamples[imuNumber, 0, sampleNumber],
                X = e.ImuSamples[imuNumber, 1, sampleNumber],
                Y = e.ImuSamples[imuNumber, 2, sampleNumber],
                Z = e.ImuSamples[imuNumber, 3, sampleNumber]
            };
            return q;
        }

        public Vector3 GetGyro(DataAvailableEventArgs e, int sampleNumber, int imuNumber)
        {
            Vector3 vec = new Vector3
            {
                X = e.GyroscopeSamples[imuNumber, 0, sampleNumber],
                Y = e.GyroscopeSamples[imuNumber, 1, sampleNumber],
                Z = e.GyroscopeSamples[imuNumber, 2, sampleNumber]
            };
            return vec;
        }
        public Vector3 GetAccelerometer(DataAvailableEventArgs e, int sampleNumber, int imuNumber)
        {
            Vector3 vec = new Vector3
            {
                X = e.AccelerometerSamples[imuNumber, 0, sampleNumber],
                Y = e.AccelerometerSamples[imuNumber, 1, sampleNumber],
                Z = e.AccelerometerSamples[imuNumber, 2, sampleNumber]
            };
            return vec;
        }
        public Vector3 GetMagnetometer(DataAvailableEventArgs e, int sampleNumber, int imuNumber)
        {
            Vector3 vec = new Vector3
            {
                X = e.MagnetometerSamples[imuNumber, 0, sampleNumber],
                Y = e.MagnetometerSamples[imuNumber, 1, sampleNumber],
                Z = e.MagnetometerSamples[imuNumber, 2, sampleNumber]
            };
            return vec;
        }

        public string WriteVector(Vector3 vec, int precision = 5)
        {
            //return string.Format("{0:+0.0000;-0.0000} {1:+0.0000;-0.0000} {2:+0.0000;-0.0000} ", vec.X, vec.Y, vec.Z);
            string format_string = BuildFormatString(0, precision) +
                   BuildFormatString(1, precision) +
                   BuildFormatString(2, precision);
            return string.Format(format_string, vec.X, vec.Y, vec.Z);
        }

        public string WriteQuaternion(Quaternion q, int precision=5)
        {
            string format_string = BuildFormatString(0, precision) + 
                BuildFormatString(1, precision) + 
                BuildFormatString(2, precision) + 
                BuildFormatString(3, precision);

            return string.Format(format_string, q.W, q.X, q.Y, q.Z);
        }

        public string BuildFormatString(int n, int precision)
        {
            string pre = new string('0', precision);
            return "{" + n.ToString() + ":+0." + pre + ";-0." + pre + "} "; 
        }

        public string eEeParser(DataAvailableEventArgs e, int sampleNumber, Dictionary<int, string> imu_dict)
        {            
            TimeSpan t = DateTime.UtcNow - starttime;
            string output =  string.Format("{0:0.0000000;-0.0000000} ", t.TotalSeconds);
            string consolestring = "\r"+output+"||";
            foreach (KeyValuePair<int, string> ele1 in imu_dict)
            {
                int i = ele1.Key-1;
                string imu = ele1.Value;

                consolestring += string.Format("Imu ({1}): {0} ", imu, i+1);

                Quaternion q = GetQuaternion(e,sampleNumber,i);
                Vector3 gyro = GetGyro(e, sampleNumber, i);
                Vector3 acc = GetAccelerometer(e, sampleNumber, i);
                Vector3 mag = GetMagnetometer(e, sampleNumber, i);
                Vector3 linAcc = new Vector3(0, 0, 0);

                string qs = WriteQuaternion(q);
                consolestring += qs;
                
                output += qs;
                output += WriteVector(gyro);
                output += WriteVector(acc);
                output += WriteVector(mag);
                output += "0.0 "; //barometer
                output += WriteVector(linAcc);
                output += "0.0 "; //altitude 
                
                consolestring += "||";
            }
            Console.Write(consolestring);
            //Console.WriteLine("Parsed output: "+ output);
            return output;       
        
        }

        public Program(bool FAKEDAQ, string dasfile, string csvoutputfilename, string ip, int port, Dictionary<int, string> imu_dict, int period, bool RAW)
        {
            //FAKEDAQ = FAKEDAQin;
            Console.SetWindowSize(200, 20);
            ConfigureDaq(imu_dict,FAKEDAQ, RAW);

            StartServer(ip, port);

            CalculateOwnQuaternion OC = new CalculateOwnQuaternion();

   
            if (FAKEDAQ)
            {
                Console.WriteLine("Starting capture");
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
                        Console.Write("<<< FAKEDAQ >>>");
                        var line = reader.ReadLine();
                        //Console.WriteLine("FAKEDAQ: rowread:" + line);
                        var values = line.Split(',');

                        e = DistrDaq(values, imu_dict);

                        e.ScanNumber = 1;
                        Capture_DataAvailable(null, e, imu_dict,FAKEDAQ);

                        System.Threading.Thread.Sleep(period);
                    }
                }
            }
            else
            {
                Console.WriteLine("Starting capture");
                switch (period)
                {
                    case 100:
                        daqSystem.StartCapturing(DataAvailableEventPeriod.ms_100); // Available: 100, 50, 25, 10            
                        break;
                    case 50:
                        daqSystem.StartCapturing(DataAvailableEventPeriod.ms_50); // Available: 100, 50, 25, 10            
                        break;
                    case 25:
                        daqSystem.StartCapturing(DataAvailableEventPeriod.ms_25); // Available: 100, 50, 25, 10            
                        break;
                    case 10:
                        daqSystem.StartCapturing(DataAvailableEventPeriod.ms_10); // Available: 100, 50, 25, 10            
                        break;
                    default:
                        Console.WriteLine("Period (in ms) needs to be  100, 50, 25, 10! Using default 10ms.");
                        daqSystem.StartCapturing(DataAvailableEventPeriod.ms_10); // Available: 100, 50, 25, 10            
                        break;
                }
            }
            Console.WriteLine("Finished! press any key to send bye signal");
            Console.ReadKey();
            c.Send("BYE!");
            Console.WriteLine("Bye sent!");

            using (StreamWriter textWriter = CreateCSV(csvoutputfilename, imu_dict))
            {
                textWriter.Write(outfile);
            }
            
            Console.ReadKey();
        }

        StreamWriter CreateCSV(string file, Dictionary<int, string> imu_dict)
        {
            StreamWriter textWriter = new StreamWriter(file);
            //textWriter = new StreamWriter(@"D:\frekle\Documents\githbu\imu_driver\socket_publisher\myfile.csv");
            //nope. too complicated.

            //var writer = new CsvWriter(textWriter, CultureInfo.InvariantCulture);
            //writer.Configuration.Delimiter = ",";
            //writer.WriteHeader<>();
            string myheader = "";
            foreach (var imuS in imu_dict)
            {
                foreach (string imucolumn in imutable)
                {
                    myheader += imuS.Value + imucolumn + ",";

                }

            }
            textWriter.WriteLine(myheader);
            return textWriter;
        }

        private void StartServer(string ip, int port)
        {
            //string client_ip = "127.0.0.1";
            //int client_port = 8080;
            Console.WriteLine("Will spit data as udp in {0}, {1}", ip, port.ToString());
            //       Console.WriteLine("Will spit data as udp in {0}, {1}", client_ip, client_port.ToString());
            // creates udp server
            c.Client(ip, port);

        }

        private void ConfigureDaq(Dictionary<int, string> imu_dict, bool FAKEDAQ, bool RAW)
        {
            // Create daqSystem object and assign the event handlers
            daqSystem = new DaqSystem();
            daqSystem.StateChanged += Device_StateChanged;
            void func(object x, DataAvailableEventArgs y) => Capture_DataAvailable(x, y, imu_dict, FAKEDAQ);
            //daqSystem.DataAvailable += Capture_DataAvailable;
            daqSystem.DataAvailable += func;

            // Configure sensors
            // .InstalledSensors = 16, not the number of sensed sensors
            // Configure sensors from channel 1-8 as EMG sensors, 9-16 as IMU sensors
            for (int EMGsensorNumber = 0; EMGsensorNumber < daqSystem.InstalledSensors - 8; EMGsensorNumber++)
            {
                Console.WriteLine("Configuring EMG sensor #" + EMGsensorNumber);
                daqSystem.ConfigureSensor(
                    new SensorConfiguration { SensorType = SensorType.EMG_SENSOR },
                    EMGsensorNumber
                );
            }

            for (int IMUsensorNumber = 8; IMUsensorNumber < daqSystem.InstalledSensors; IMUsensorNumber++)
            {
                Console.WriteLine("Configuring IMU sensor #" + IMUsensorNumber);
                daqSystem.ConfigureSensor(
                    new SensorConfiguration { SensorType = SensorType.INERTIAL_SENSOR },
                    IMUsensorNumber
                );
            }

            Console.WriteLine("Configuring capture");
            var config = new CaptureConfiguration();
            if (RAW)
                { config = new CaptureConfiguration { SamplingRate = SamplingRate.Hz_2000, IMU_AcqType = ImuAcqType.Fused6xData_142Hz }; }
            else
                { config = new CaptureConfiguration { SamplingRate = SamplingRate.Hz_2000, IMU_AcqType = ImuAcqType.RawData }; }
            //daqSystem.ConfigureCapture(old_config);
            daqSystem.ConfigureCapture(config);
        }

        private void Capture_DataAvailable(object sender, DataAvailableEventArgs e, Dictionary<int, string> imu_dict, bool FAKEDAQ)
        {
            int samplesPerChannel = e.ScanNumber; // what's this?
            //Console.WriteLine("Scan number: " + e.ScanNumber);

            for (int sampleNumber = 0; sampleNumber < samplesPerChannel; sampleNumber = sampleNumber + 1) // This loops captures data from sensor # sampleNumber+1
            {
                //Console.Write(" sampleNumber: "+ sampleNumber.ToString());
                // this is only necessary if we want the most accurate possible IMU values, with the N*14 delayed sample that you can read 
                //eEeParser(e, sampleNumber);
            }
            //Console.WriteLine(".");
            numsamples++;
            string imulinestr = eEeParser(e, 0, imu_dict);
            if (!FAKEDAQ)
                //textWriter.WriteLine(imulinestr.Replace(' ', ','));
                outfile+= imulinestr.Replace(' ', ',');
            if ( numsamples> 200) // a bit more than a second at 142hz
                c.Send(imulinestr);
            //Console.WriteLine("Values has been sent");

        }

        private void Device_StateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            Console.WriteLine(e);
        }
    }

}
