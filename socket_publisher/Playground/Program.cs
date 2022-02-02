using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Waveplus.DaqSys;
using Waveplus.DaqSysInterface;
using WaveplusLab.Shared.Definitions;

namespace Playground
{
    class Program
    {
        static void Main()
        {
            /*
             * 1. [Server] Start TCP server, wait for connections
             * 2. [Matlab] Start matlab program that connects
             *  a.[Matlab] Send integer denoting sensor count
             * 3. [Server] When receiving connection, start capture with requested sensor count
             * 4. [Server] Send data to socket as soon as it's received from sensors
             * 5. [Matlab] Read data from socket, GOTO 4
             */

            new Program();

        }
        bool FAKEDAQ = true;
        DaqSystem daqSystem;
        TcpListener listener;
        UdpClient listener2;
        NetworkStream networkStream;
        IPEndPoint someone;
        UDPSocket c = new UDPSocket();
        string[] imu_names;

        byte[] buffer = new byte[5000];

        public DataAvailableEventArgs DistrDaq(string[] row)
        {
            DataAvailableEventArgs e = new DataAvailableEventArgs();
            for (int i =0; i< imu_names.Length; i++)
            {
                string imu = imu_names[i];
                Console.WriteLine("evaling imu( "+ i.ToString() + " ):" + imu);
                // now there is a fixed sequence which i must follow
                //q1,q2,q3,q4
                //ax,ay,az
                //gx,gy,gz
                //mx,my,mz
                //barometer
                //linAcc(x,y,z)
                //altitude
                int I = i * 17;
                e.ImuSamples[i, 0, 0] = float.Parse(row[I + 0]);
                e.ImuSamples[i, 1, 0] = float.Parse(row[I + 1]);
                e.ImuSamples[i, 2, 0] = float.Parse(row[I + 2]);
                e.ImuSamples[i, 3, 0] = float.Parse(row[I + 3]);
                e.AccelerometerSamples[i, 0, 0] =float.Parse(row[I + 4]);
                e.AccelerometerSamples[i, 1, 0] =float.Parse(row[I + 5]);
                e.AccelerometerSamples[i, 2, 0] =float.Parse(row[I + 6]);
                e.GyroscopeSamples[i, 0, 0] = float.Parse(row[I + 7]);
                e.GyroscopeSamples[i, 1, 0] = float.Parse(row[I + 8]);
                e.GyroscopeSamples[i, 2, 0] = float.Parse(row[I + 9]);
                e.MagnetometerSamples[i, 0, 0] = float.Parse(row [I + 10]);
                e.MagnetometerSamples[i, 1, 0] = float.Parse(row [I + 11]);
                e.MagnetometerSamples[i, 2, 0] = float.Parse(row [I + 12]);
                //e.Barometer, linAcc, altitude // no existe!
                

            }
            return e;
        }
        public string eEeParser(DataAvailableEventArgs e, int sampleNumber)
        {
            string output = "";
            for (int i = 0; i < imu_names.Length; i++)
            {
                output += e.ImuSamples[i, 0, sampleNumber].ToString();
                output += e.ImuSamples[i, 1, sampleNumber].ToString();
                output += e.ImuSamples[i, 2, sampleNumber].ToString();
                output += e.ImuSamples[i, 3, sampleNumber].ToString();
                output += e.AccelerometerSamples[i,0, sampleNumber].ToString();
                output += e.AccelerometerSamples[i,1, sampleNumber].ToString();
                output += e.AccelerometerSamples[i,2, sampleNumber].ToString();
                output += e.GyroscopeSamples[i,0, sampleNumber].ToString();
                output += e.GyroscopeSamples[i,1, sampleNumber].ToString();
                output += e.GyroscopeSamples[i,2, sampleNumber].ToString();
                output += e.MagnetometerSamples[i,0, sampleNumber].ToString();
                output += e.MagnetometerSamples[i,1, sampleNumber].ToString();
                output += e.MagnetometerSamples[i,2, sampleNumber].ToString();
                output += "0.0"; //barometer
                output += "0.0"; //linAccx
                output += "0.0"; //linAccy
                output += "0.0"; //linAccz
                output += "0.0"; //altitude
            }
            Console.WriteLine("Parsed output: "+ output);
            return output;       
        
        }

        public Program()
        {
            ConfigureDaq();
            imu_names = new string[] {"a","b","c","d",
                                      "e","f","g","h"  };  //lower body
            //imu_names = new string[] {"a","b","c","d",
            //                          "e","f","g","h"  }; //upper body


            //float[,,] googogo = new float[32, 3, 20000];
            //1
            float[,] Samples1 = new float[32, 20000];
            float[,,] ImuSamples1 = new float[32, 4, 20000];
            float[,,] AccelerometerSamples1 = new float[32, 3, 20000];
            float[,,] GyroscopeSamples1 = new float[32, 3, 20000];
            float[,,] MagnetometerSamples1 = new float[32, 3, 20000];
            float[,] FootSwSamples1 = new float[2, 20000];
            float[] SyncSamples1 = new float[20000];
            short[,] SensorStates1 = new short[32, 20000];
            short[,] FootSwSensorStates1 = new short[2, 20000];
            int[] SensorRFLostPackets1 = new int[32];
            int[] imuCalibrationStep1 = new int[32];
            //2
            float[,] Samples2 = new float[32, 20000];
            float[,,] ImuSamples2 = new float[32, 4, 20000];
            float[,,] AccelerometerSamples2 = new float[32, 3, 20000];
            float[,,] GyroscopeSamples2 = new float[32, 3, 20000];
            float[,,] MagnetometerSamples2 = new float[32, 3, 20000];
            float[,] FootSwSamples2 = new float[2, 20000];
            short[,] SensorStates2 = new short[32, 20000];
            short[,] FootSwSensorStates2 = new short[2, 20000];
            int[] SensorRFLostPackets2 = new int[32];
            int[] imuCalibrationStep2 = new int[32];


            // float[,] Samples ;
            // float[,,] ImuSamples ;
            // float[,,] AccelerometerSamples ;
            // float[,,] GyroscopeSamples ;
            // float[,,] MagnetometerSamples ;
            // float[] SyncSamples ;
            // short[,] SensorStates ;
            // float[,] FootSwSamples ;
            // short[,] FootSwSensorStates ;
            // int[] SensorRFLostPackets ;
            // int[] imuCalibrationStep ;
            // int SensorUSBLostPackets;
            // int DataTransferRate;


            StartServer();
            networkStream = WaitForClient();

            Console.WriteLine("Starting capture");
            daqSystem.StartCapturing(DataAvailableEventPeriod.ms_10); // Available: 100, 50, 25, 10

            if (FAKEDAQ)
            {
                // Maybe i should read the csv?
                using (var reader = new StreamReader(@"C:\Users\frekle\source\repos\docker-opensimrt\connect\gait1992_imu.csv"))
                {
                    DataAvailableEventArgs e = new DataAvailableEventArgs();
                    //float[,] datatable = new float[17 * 8, 32000];
                    while (FAKEDAQ && !reader.EndOfStream)
                    {
                        Console.WriteLine("LOOP");
                        var line = reader.ReadLine();
                        var values = line.Split(';');
                        //// now i need to distribute the values to e
                        ///
                        e = DistrDaq(values);
                        //System.Reflection.FieldInfo field = typeof(DaqSystem).GetField("_dataSyncBuffer1", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        //DataSyncBuffer spoofedBuffer1 = (DataSyncBuffer)field.GetValue(daqSystem);
                        // here i want to change the
                        //GyroscopeSamples1[0, 0, 0] = 14.0F;
                        //spoofedBuffer1.AccelerometerSamples = googogo;
                        //field.SetValue(daqSystem, spoofedBuffer1);
                        //e.Samples = Samples1;
                        //e.GyroscopeSamples = GyroscopeSamples1;
                        //e.AccelerometerSamples = AccelerometerSamples1;
                        e.ScanNumber = 4;
                        Capture_DataAvailable(null, e);


                        System.Threading.Thread.Sleep(50);
                    }
                }
            }
            Console.ReadKey();
        }

        private void StartServer()
        {
            // Start server
            IPAddress localAdd = IPAddress.Parse("127.0.0.1");
            someone = new IPEndPoint(localAdd.Address, 5001);
            int port = 5000;
            int port2 = 5001;
            listener = new TcpListener(localAdd, port);
            listener2 = new UdpClient(port2);
            listener.Start();
            Console.WriteLine("Listening on " + localAdd + " " + port);
            Console.WriteLine("Also spitting info in udp in " + localAdd + " " + port2);

            // now the third horriblest serverino:
            c.Client("127.0.0.1", 8080);


        }

        private NetworkStream WaitForClient()
        {
            // Wait for client to connect
            TcpClient client = listener.AcceptTcpClient();
            Console.WriteLine("Client connected");

            // Get stream to read/write data
            return client.GetStream();
        }

        private void Send(double[] values)
        {
            Buffer.BlockCopy(values, 0, buffer, 0, values.Length * 8);
            networkStream.Write(buffer, 0, values.Length * 8);
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
            daqSystem.ConfigureCapture(
                new CaptureConfiguration { SamplingRate = SamplingRate.Hz_2000, IMU_AcqType = ImuAcqType.RawData }
            );
        }

        private void Capture_DataAvailable(object sender, DataAvailableEventArgs e)
        {
            int samplesPerChannel = e.ScanNumber; // what's this?
            Console.WriteLine("scan number ???" + e.ScanNumber);
            int channelsNumber = 16; // Number of output channels
            double[] values = new double[samplesPerChannel * channelsNumber]; // Change to add more sensors
            string output = "";
            for (int sampleNumber = 0; sampleNumber < samplesPerChannel; sampleNumber = sampleNumber + 1) // This loops captures data from sensor # sampleNumber+1
            {
                Console.WriteLine("EMGSensor #" + 1 + ": " + e.Samples[0, sampleNumber]);
                Console.WriteLine("EMGSensor #" + 2 + ": " + e.Samples[1, sampleNumber]);
                Console.WriteLine("EMGSensor #" + 3 + ": " + e.Samples[2, sampleNumber]);
                Console.WriteLine("EMGSensor #" + 4 + ": " + e.Samples[3, sampleNumber]);

                //values[sampleNumber * 4 + 0] = e.Samples[0, sampleNumber];
                //values[sampleNumber * 4 + 1] = e.Samples[1, sampleNumber];
                //values[sampleNumber * 4 + 2] = e.Samples[2, sampleNumber];
                //values[sampleNumber * 4 + 3] = e.Samples[3, sampleNumber];
                Console.WriteLine("IMUSensor #" + 13 + "Gyroscope X: " + e.GyroscopeSamples[12, 0, sampleNumber]);
                Console.WriteLine("IMUSensor #" + 13 + "Gyroscope Y: " + e.GyroscopeSamples[12, 1, sampleNumber]);
                Console.WriteLine("IMUSensor #" + 13 + "Gyroscope Z: " + e.GyroscopeSamples[12, 2, sampleNumber]);
                Console.WriteLine("IMUSensor #" + 13 + "Acceleration X: " + e.AccelerometerSamples[12, 0, sampleNumber]);
                Console.WriteLine("IMUSensor #" + 13 + "Acceleration Y: " + e.AccelerometerSamples[12, 1, sampleNumber]);
                Console.WriteLine("IMUSensor #" + 13 + "Acceleration Z: " + e.AccelerometerSamples[12, 2, sampleNumber]);
                Console.WriteLine("IMUSensor #" + 14 + "Gyroscope X: " + e.GyroscopeSamples[13, 0, sampleNumber]);
                Console.WriteLine("IMUSensor #" + 14 + "Gyroscope Y: " + e.GyroscopeSamples[13, 1, sampleNumber]);
                Console.WriteLine("IMUSensor #" + 14 + "Gyroscope Z: " + e.GyroscopeSamples[13, 2, sampleNumber]);
                Console.WriteLine("IMUSensor #" + 14 + "Acceleration X: " + e.AccelerometerSamples[13, 0, sampleNumber]);
                Console.WriteLine("IMUSensor #" + 14 + "Acceleration Y: " + e.AccelerometerSamples[13, 1, sampleNumber]);
                Console.WriteLine("IMUSensor #" + 14 + "Acceleration Z: " + e.AccelerometerSamples[13, 2, sampleNumber]);
                //values[sampleNumber * 22 + 0] = e.Samples[0, sampleNumber];
                //values[sampleNumber * 22 + 1] = e.Samples[1, sampleNumber];
                //values[sampleNumber * 22 + 2] = e.Samples[2, sampleNumber];
                //values[sampleNumber * 22 + 3] = e.Samples[3, sampleNumber];
                //values[sampleNumber * 22 + 4] = e.GyroscopeSamples[8, 0, sampleNumber];
                //values[sampleNumber * 22 + 5] = e.GyroscopeSamples[8, 1, sampleNumber];
                //values[sampleNumber * 22 + 6] = e.GyroscopeSamples[8, 2, sampleNumber];
                //values[sampleNumber * 22 + 7] = e.AccelerometerSamples[8, 0, sampleNumber];
                //values[sampleNumber * 22 + 8] = e.AccelerometerSamples[8, 1, sampleNumber];
                //values[sampleNumber * 22 + 9] = e.AccelerometerSamples[8, 2, sampleNumber];
                //values[sampleNumber * 22 + 10] = e.ImuSamples[8, 0, sampleNumber];
                //values[sampleNumber * 22 + 11] = e.ImuSamples[8, 1, sampleNumber];
                //values[sampleNumber * 22 + 12] = e.ImuSamples[8, 2, sampleNumber];
                //values[sampleNumber * 22 + 13] = e.GyroscopeSamples[9, 0, sampleNumber];
                //values[sampleNumber * 22 + 14] = e.GyroscopeSamples[9, 1, sampleNumber];
                //values[sampleNumber * 22 + 15] = e.GyroscopeSamples[9, 2, sampleNumber];
                //values[sampleNumber * 22 + 16] = e.AccelerometerSamples[9, 0, sampleNumber];
                //values[sampleNumber * 22 + 17] = e.AccelerometerSamples[9, 1, sampleNumber];
                //values[sampleNumber * 22 + 18] = e.AccelerometerSamples[9, 2, sampleNumber];
                //values[sampleNumber * 22 + 19] = e.ImuSamples[9, 0, sampleNumber];
                //values[sampleNumber * 22 + 20] = e.ImuSamples[9, 1, sampleNumber];
                //values[sampleNumber * 22 + 21] = e.ImuSamples[9, 2, sampleNumber];
                values[sampleNumber * 16 + 0] = e.Samples[0, sampleNumber];
                values[sampleNumber * 16 + 1] = e.Samples[1, sampleNumber];
                values[sampleNumber * 16 + 2] = e.Samples[2, sampleNumber];
                values[sampleNumber * 16 + 3] = e.Samples[3, sampleNumber];
                values[sampleNumber * 16 + 4] = e.GyroscopeSamples[12, 0, sampleNumber];
                values[sampleNumber * 16 + 5] = e.GyroscopeSamples[12, 1, sampleNumber];
                values[sampleNumber * 16 + 6] = e.GyroscopeSamples[12, 2, sampleNumber];
                values[sampleNumber * 16 + 7] = e.AccelerometerSamples[12, 0, sampleNumber];
                values[sampleNumber * 16 + 8] = e.AccelerometerSamples[12, 1, sampleNumber];
                values[sampleNumber * 16 + 9] = e.AccelerometerSamples[12, 2, sampleNumber];
                values[sampleNumber * 16 + 10] = e.GyroscopeSamples[13, 0, sampleNumber];
                values[sampleNumber * 16 + 11] = e.GyroscopeSamples[13, 1, sampleNumber];
                values[sampleNumber * 16 + 12] = e.GyroscopeSamples[13, 2, sampleNumber];
                values[sampleNumber * 16 + 13] = e.AccelerometerSamples[13, 0, sampleNumber];
                values[sampleNumber * 16 + 14] = e.AccelerometerSamples[13, 1, sampleNumber];
                values[sampleNumber * 16 + 15] = e.AccelerometerSamples[13, 2, sampleNumber];
                Console.WriteLine("values.Length:" + values.Length);
                Console.WriteLine("ScanNumber:" + e.ScanNumber);
                //DisplayArray(values, "wo");

                //values[sampleNumber * 8 + 4] = e.Samples[4, sampleNumber];
                //values[sampleNumber * 8 + 5] = e.Samples[5, sampleNumber];
                //values[sampleNumber * 8 + 6] = e.Samples[6, sampleNumber];
                //values[sampleNumber * 8 + 7] = e.Samples[7, sampleNumber];

                //
                output += eEeParser(e, sampleNumber);

            }
            Send(values);
            Byte[] sendBytes = GetBytes(values);
            listener2.Send(sendBytes, sendBytes.Length, someone);

            //servidor numbero 3!
            c.Send(output);

            //foreach (int value in values)
            //    Console.Write("{0}  ", value);
            //Console.WriteLine("Values has been sent");
        }

        static byte[] GetBytes(double[] values)
        {
            return values.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
        }

        private void Device_StateChanged(object sender, DeviceStateChangedEventArgs e)
        {
            Console.WriteLine(e);
        }
    }

}
