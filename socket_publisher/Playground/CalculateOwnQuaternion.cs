using System;
using CsvHelper;

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
	public class CalculateOwnQuaternion
	{
		public CalculateOwnQuaternion()
		{
			Console.Write("Hello World!");
		}


		public class DeadReckoning_Phil
		{
			//from this guy http://philstech.blogspot.com/2014/09/fast-quaternion-integration-for.html
			float timeDelta;
			Quaternion Q_orientation_last, Q_orientation_current;

			public DeadReckoning_Phil(float timeDelta)
            {
                this.timeDelta = timeDelta;
				this.Q_orientation_last = new Quaternion { 
					W = 1,
					X = 0,
					Y = 0,
					Z = 0,
				};
				this.Q_orientation_current = new Quaternion
				{
					W = 1,
					X = 0,
					Y = 0,
					Z = 0,
				};
			}

            public Quaternion QuaternionFromGyro(Vector3 gyro)
			{
				float t_2 = timeDelta * 0.5f;
				Quaternion q = new Quaternion
				{
					X = gyro.X * t_2,
					Y = gyro.Y * t_2,
					Z = gyro.Z * t_2,
				};
				q.W = 1.0f - 0.5f * (q.X * q.X + q.Y * q.Y + q.Z * q.Z);
				return q;
			}

			public void Update(Vector3 gyro)
			{
				//Q_orientation_current = Q_gyro * Q_orientation_last
				//WHat? I wanted to see the code, but he deleted it. THe post is from 2014 after all...
				// to rotate a quaternion you kinda need to conjugate it, people call it different ways
				// https://math.stackexchange.com/questions/331539/combining-rotation-quaternions
				//from msdn it feels like this is it: https://docs.microsoft.com/en-us/dotnet/api/system.numerics.quaternion.concatenate?view=net-6.0#system-numerics-quaternion-concatenate(system-numerics-quaternion-system-numerics-quaternion)
				Q_orientation_last = Q_orientation_current;
				Q_orientation_current = System.Numerics.Quaternion.Concatenate(QuaternionFromGyro(gyro), Q_orientation_current);

			}
		}
	}
}