using CsvHelper;
using System;
using System.Collections.Generic;
using System.Globalization;
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

        UDPSocket c = new UDPSocket();

        Dictionary<int, string> imu_dict =
               new Dictionary<int, string>();

        StreamWriter textWriter;
        
        int numsamples = 0;

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
                int i = ele1.Key;
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

        public string WriteQuarternion(float q0, float q1, float q2, float q3)
        {
            string outstr = "";
            string formatstr = "{0:+0.0000000;-0.0000000} ";
            outstr += string.Format(formatstr, q0);
            outstr += string.Format(formatstr, q1);
            outstr += string.Format(formatstr, q2);
            outstr += string.Format(formatstr, q3);
            return outstr;

        }
        public string eEeParser(DataAvailableEventArgs e, int sampleNumber)
        {            
            TimeSpan t = DateTime.UtcNow - starttime;
            string output =  t.TotalSeconds.ToString() + " ";
            string consolestring = "\r";
            foreach (KeyValuePair<int, string> ele1 in imu_dict)
            {
                int i = ele1.Key-1;

                string imu = ele1.Value;
                consolestring += string.Format("Imu ({1}): {0} ", imu, i+1);
                float q0, q1, q2, q3;
                q0 = e.ImuSamples[i, 0, sampleNumber];
                q1 = e.ImuSamples[i, 1, sampleNumber];
                q2 = e.ImuSamples[i, 2, sampleNumber];
                q3 = e.ImuSamples[i, 3, sampleNumber];

                consolestring += string.Format("Q: {0:+0.0000;-0.0000}, {1:+0.0000;-0.0000}, {2:+0.0000;-0.0000}, {3:+0.0000;-0.0000}", q0,q1,q2,q3);

                /*output += q0.ToString() + " ";
                output += q1.ToString() + " ";
                output += q2.ToString() + " ";
                output += q3.ToString() + " ";                */
                output += WriteQuarternion(q0,q1,q2,q3);
                output += e.AccelerometerSamples[i,0, sampleNumber].ToString() + " ";
                output += e.AccelerometerSamples[i,1, sampleNumber].ToString() + " ";
                output += e.AccelerometerSamples[i,2, sampleNumber].ToString() + " ";
                output += e.GyroscopeSamples[i,0, sampleNumber].ToString() + " ";
                output += e.GyroscopeSamples[i,1, sampleNumber].ToString() + " ";
                output += e.GyroscopeSamples[i,2, sampleNumber].ToString() + " ";
                output += e.MagnetometerSamples[i,0, sampleNumber].ToString() + " ";
                output += e.MagnetometerSamples[i,1, sampleNumber].ToString() + " ";
                output += e.MagnetometerSamples[i,2, sampleNumber].ToString() + " ";
                output += "0.0 "; //barometer
                output += "0.0 "; //linAccx
                output += "0.0 "; //linAccy
                output += "0.0 "; //linAccz
                output += "0.0 "; //altitude 
                consolestring += "||";
            }
            Console.Write(consolestring);
            //Console.WriteLine("Parsed output: "+ output);
            return output;       
        
        }

        public Program()
        {
            Console.SetWindowSize(200, 20);
            ConfigureDaq(); 

            imu_dict.Add(11, "TORAX");
            imu_dict.Add(12, "HUMERUS");
            imu_dict.Add(13, "RADIUS");

            StartServer();
   
            bool UPPER = true;
            bool UPPER2 = true;
            var lowbodyfile = @"D:\frekle\Documents\githbu\imu_driver\socket_publisher\gait1992_imu.csv";
            var uppbodyfile = @"D:\frekle\Documents\githbu\imu_driver\socket_publisher\mobl2016_imu.csv";
            var up2bodyfile = @"D:\frekle\Documents\githbu\imu_driver\socket_publisher\myfile.csv";
            string dasfile;
            if (UPPER2)
                dasfile = up2bodyfile;
            else if (UPPER)
                dasfile = uppbodyfile;
            else
                dasfile = lowbodyfile;

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
                        // Console.WriteLine("FAKEDAQ: LOOP");
                        var line = reader.ReadLine();
                        //Console.WriteLine("FAKEDAQ: rowread:" + line);
                        var values = line.Split(',');

                        e = DistrDaq(values);

                        e.ScanNumber = 1;
                        Capture_DataAvailable(null, e);

                        System.Threading.Thread.Sleep(500);
                    }
                }
            }
            else
            {
                textWriter = new StreamWriter(@"D:\frekle\Documents\githbu\imu_driver\socket_publisher\myfile.csv");
                //nope. too complicated.

                //var writer = new CsvWriter(textWriter, CultureInfo.InvariantCulture);
                //writer.Configuration.Delimiter = ",";
                //writer.WriteHeader<>();
                string myheader = "";
                foreach (string imuS in imulist)
                {
                    foreach (string imucolumn in imutable)
                    {
                        myheader += imuS + imucolumn + ",";

                    }

                }
                textWriter.WriteLine(myheader);

                Console.WriteLine("Starting capture");
                daqSystem.StartCapturing(DataAvailableEventPeriod.ms_10); // Available: 100, 50, 25, 10            
            }
            Console.WriteLine("Finished! press any key to send bye signal");
            Console.ReadKey();
            c.Send("BYE!");
            Console.WriteLine("Bye sent!");
            if (!FAKEDAQ)
                textWriter.Close();
            Console.ReadKey();
        }

        string[] imutable = { "_q1",       "_q2",       "_q3",      "_q4",        "_ax",
            "_ay",       "_az",       "_gx",      "_gy",        "_gz",
            "_mx",       "_my",       "_mz",      "_barometer", "_linAcc_x",
            "_linAcc_y", "_linAcc_z", "_altitude" };

        string[] imulist = {"thorax","humerus","radius" };

        private void StartServer()
        {
            string client_ip = "127.0.0.1";
            int client_port = 8080;
            Console.WriteLine("Will spit data as udp in {0}, {1}", client_ip, client_port.ToString());
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
            var new_config = new CaptureConfiguration { SamplingRate = SamplingRate.Hz_2000, IMU_AcqType = ImuAcqType.Fused6xData_142Hz };
            //var old_config = new CaptureConfiguration { SamplingRate = SamplingRate.Hz_2000, IMU_AcqType = ImuAcqType.RawData };
            //daqSystem.ConfigureCapture(old_config);
            daqSystem.ConfigureCapture(new_config);
        }

        private void Capture_DataAvailable(object sender, DataAvailableEventArgs e)
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
            string imulinestr = eEeParser(e, 0);
            if (!FAKEDAQ)
                textWriter.WriteLine(imulinestr.Replace(' ', ','));
            if ( numsamples> 0) // a bit more than a second at 142hz
                c.Send(imulinestr);
            //Console.WriteLine("Values has been sent");

        }

        private void Device_StateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            Console.WriteLine(e);
        }
    }

}
