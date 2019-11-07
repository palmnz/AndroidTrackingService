using System;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

using Tracking.Services;

namespace CANLib
{
	/// <summary>
	/// Persists the last commands sent to preiferal devices
	/// </summary>
	[Serializable]
	public class DeviceCodeHistory
	{
#if ANDROID
        private static log4droid CNXLog = new log4droid() {Logger = "CNXLogger"};
#else
        private static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif

        public string HeadSign = null;
		public byte Fareset = 0;
		public string FareboxRouteNo = null;
		public int? FareboxTripNo = null;
		[field: NonSerialized()]
		public string Filename = null;

		public static DeviceCodeHistory CreateDeviceCodeHistory(string filename)
		{
			DeviceCodeHistory dch = null;
			Stream stream = null;

			// load existing codes
			try
			{

				IFormatter formatter = new BinaryFormatter();
				stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None);
				dch = (DeviceCodeHistory)formatter.Deserialize(stream);
				CNXLog.WarnFormat("CreateDeviceCodeHistory : from ({0}), {1}.", filename, dch.ToString());
			}
			catch (FileNotFoundException)
			{
				CNXLog.WarnFormat("CreateDeviceCodeHistory : Could not open persistent storage ({0}), Creating a new one.", filename);
			}
			catch (Exception e)
			{
				CNXLog.Error("CreateDeviceCodeHistory", e);
			}
			finally
			{
				if (null != stream)
					stream.Close();
			}

			// may have a failure or no persitant store
			if (dch == null)
				dch = new DeviceCodeHistory();

			dch.Filename = filename;

			return dch;
		}

		public static void PersistDeviceCodeHistory(DeviceCodeHistory codeHistory)
		{
			lock (codeHistory)
			{
				Stream stream = null;
				try
				{
					IFormatter formatter = new BinaryFormatter();
					stream = new FileStream(codeHistory.Filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None);
					formatter.Serialize(stream, codeHistory);
					CNXLog.WarnFormat("Persisted DeviceCodeHistory {0}.", codeHistory.ToString());
				}
				catch (Exception e)
				{
					CNXLog.Error("PersistDeviceCodeHistory", e);
				}
				finally
				{
					if (null != stream)
					{
						stream.Flush();
						stream.Close();
					}
				}
			}
		}

		public override string ToString()
		{
			object[] args = { HeadSign, Fareset, FareboxRouteNo, FareboxTripNo };
			return String.Format("Device codes headsign {0}, fareset {1}, Route No {2} Trip No {3}.", args);
		}
	}
}
