using System;
using System.Text;

using Tracking.Services;

namespace CANLib
{
	[Serializable]
	/// <summary>
	/// Represents Device Catalogue frame.
	/// </summary>
	public class DeviceCatalogueInfo : IComparable<DeviceCatalogueInfo>, IEquatable<DeviceCatalogueInfo>
	{
		/// <summary>
		/// Id's of the product/device types
		/// </summary>
		public enum Product : byte
		{
			/// <summary>
			/// Radio gateway
			/// </summary>
			CCM300 = 0x06,
			/// <summary>
			/// Cellular gateway
			/// </summary>
			CTM100 = 0x07,
			/// <summary>
			/// Tracking Mobile Data Terminal based on Technexion THB/TAM3517
			/// </summary>
			MDTCTM = 0x0b,
			/// <summary>
			/// Tracking Media player based on Pandaboard.
			/// </summary>
			CTM_CMP = 0x0c,
			/// <summary>
			/// Tracking Medius
			/// </summary>
			MEDIUS_CTM = 0x0d,
			/// <summary>
			/// CPC CAN - J1708 interface
			/// </summary>
			CPCJ1708 = 0x80,
			/// <summary>
			/// Next stop announcement unit
			/// </summary>
			NSA100 = 0x81,
			/// <summary>
			/// CPC Serial Line CAN bridge
			/// </summary>
			CPCSLCAN = 0x82,
			/// <summary>
			/// Media player based on Pandaboard.
			/// </summary>
			CMP = 0x83,
			/// <summary>
			/// Mobile Data Terminal based on Technexion THB/TAM3517
			/// </summary>
			MDTTHB = 0x84,
			/// <summary>
			/// Non tracking Medius
			/// </summary>
			MEDIUS = 0x85,
		};

		/// <summary>
		/// The status and mask types
		/// </summary>
		[FlagsAttribute]
		public enum EquipmentFlages : uint
		{
			/// <summary>
			/// None or unknown equipment
			/// </summary>
			None = 0,
			/// <summary>
			/// Farebox/Ticket machine
			/// </summary>
			Farebox = 0x01,
			/// <summary>
			/// Vehicle headsign
			/// </summary>
			Headsign = 0x02,
			/// <summary>
			/// Next stop unit.
			/// </summary>
			NextStop = 0x04,
			/// <summary>
			/// Next stop display.
			/// </summary>
			NextStopDisplay = 0x08,
			/// <summary>
			/// APC unit.
			/// </summary>
			PassengerCounter = 0x10,
			/// <summary>
			/// Traffic priority transponder.
			/// </summary>
			TrafficPriority = 0x20,
			/// <summary>
			/// Media player.
			/// </summary>
			MediaPlayer = 0x40,
			/// <summary>
			/// MDT, none tracking vairiant.
			/// </summary>
			MDT = 0x80,
		};

		/// <summary>
		/// The product/device type.
		/// </summary>
		public Product ProductId = 0;
		private byte mBuildNo = 0;
		/// <summary>
		/// The build or version number of the device.
		/// </summary>
		public byte BuildNo
		{
			get
			{
				byte ret = 0;
				lock (this) { ret = mBuildNo; }
				return ret;
			}
			set
			{
				lock (this) { mBuildNo = value; }
			}
		}
		public EquipmentFlages mMask = EquipmentFlages.None;		
		/// <summary>
		/// Equipment mask of connected equipment
		/// </summary>
		public EquipmentFlages Mask
		{
			get
			{
				EquipmentFlages ret = 0;
				lock (this) { ret = mMask; }
				return ret;
			}
			set
			{
				lock (this) { mMask = value; }
			}
		}
		private volatile EquipmentFlages mStatus = EquipmentFlages.None;
		/// <summary>
		/// The status of the connected equipment
		/// </summary>
		public EquipmentFlages Status
		{
			get
			{
				EquipmentFlages ret = 0;
				lock (this) { ret = mStatus; }
				return ret;
			}
			set
			{
				lock (this) { mStatus = value; }
			}
		}
		[field: NonSerialized()]
		private volatile EquipmentFlages mPendingStatus = EquipmentFlages.None;

		/// <summary>
		/// Updates the BuildNo, Mask and Status.
		/// </summary>
		/// <param name="info">Device details to update this device to.</param>
		/// <returns>True if any data was changed, false if no fields needed updating.</returns>
		/// <remarks>
		/// The Status information is staged to allow for any gitter.
		/// Use the <see cref="PulseAlive"/> method to propogate the Status to the public interface.
		/// </remarks>
		public DeviceCatalogue.CatalogueChangeType Update(DeviceCatalogueInfo info)
		{
			DeviceCatalogue.CatalogueChangeType ret = DeviceCatalogue.CatalogueChangeType.NONE;

			lock (this)
			{
				if (mBuildNo != info.mBuildNo || mMask != info.mMask)
				{
					mBuildNo = info.BuildNo;
					mMask |= info.mMask;
					ret = DeviceCatalogue.CatalogueChangeType.EQUIPMENT;
				}
				mPendingStatus |= info.mStatus;
			}

			return ret;
		}

		/// <summary>
		/// Used to generate a heart beat.
		/// The pending status is propogated to the public interface and the pending status is cleared.
		/// </summary>
		/// <returns>True if the status has changed.</returns>
		public bool PulseAlive()
		{
			bool ret = false;
			lock (this)
			{
				ret = (mStatus != mPendingStatus);
				mStatus = mPendingStatus;
				mPendingStatus = EquipmentFlages.None;
			}

			return ret;
		}

		public override string ToString()
		{
			object[] args = new object[] { ProductId, BuildNo, Mask, Status };
			if (Enum.IsDefined(typeof(Product), ProductId))
				return String.Format("{0}v{1}, devs {3}.", args);
			return String.Format("Id 0x{0:x}v{1},devs {3}.", args);
		}

		#region IComparable<DeviceCatalogueInfo> Members

		public int CompareTo(DeviceCatalogueInfo other)
		{
			return ProductId.CompareTo(other.ProductId);
		}

		#endregion

		#region IEquatable<DeviceCatalogueInfo> Members

		public bool Equals(DeviceCatalogueInfo other)
		{
			return (ProductId == other.ProductId) && (BuildNo == other.BuildNo) && (Mask == other.Mask) && (Status == other.Status);
		}

		#endregion
	}

	/// <summary>
	/// Enumeration of Block transfer flags
	/// </summary>
	[Flags]
	public enum BlockFlags : byte
	{
		/// <summary>
		/// No flags.
		/// </summary>
		None = 0,
		/// <summary>
		/// Indicates the version is present in the frame.
		/// </summary>
		VersionPresent = 0x01,
	};

	/// <summary>
	/// Enumeration of the Passenger Event Mask types
	/// </summary>
	[Flags]
	public enum PassengerCountEventMask : byte
	{
		/// <summary>
		/// Front.
		/// </summary>
		Front = 0x01,
		/// <summary>
		/// Rear.
		/// </summary>
		Rear = 0x02,
		/// <summary>
		/// Third.
		/// </summary>
		Third = 0x04,
		/// <summary>
		/// Forth.
		/// </summary>
		Forth = 0x08,
		/// <summary>
		/// Wheelchair.
		/// </summary>
		Wheelchair = 0x40,
		/// <summary>
		/// Bike.
		/// </summary>
		Bike = 0x80,
	};

	/// <summary>
	/// GPS status reported in GPS CAN frames
	/// </summary>
	public enum CANGPSState : byte
	{
		/// <summary>
		/// Invalid frame data.
		/// </summary>
		Invalid = 0xff,
		/// <summary>
		/// No GPS connected
		/// </summary>
		NoGPS = 0,
		/// <summary>
		/// Recieving no fix from GPS
		/// </summary>
		NoFix = 1,
		/// <summary>
		/// Recieving a fix with a high error estimate.
		/// </summary>
		PoorFix = 2,
		/// <summary>
		/// Recieving good positions.
		/// </summary>
		GoodFix = 3,
	};

	/// <summary>
	/// Enumeration of the Door Count types
	/// </summary>
	public enum DoorCountType : byte
	{
		/// <summary>
		/// Front door count.
		/// </summary>
		Front = 1,
		/// <summary>
		/// Rear door count.
		/// </summary>
		Rear = 2,
		/// <summary>
		/// Third door count.
		/// </summary>
		Third = 3,
		/// <summary>
		/// Forth door count.
		/// </summary>
		Forth = 4,
	};

	/// <summary>
	/// Enumeration of the Trip Progress message types
	/// </summary>
	public enum TripProgressType : byte
	{
		/// <summary>
		/// No active trip (trip not running).
		/// </summary>
		Inactive = 0x00,
		/// <summary>
		/// Active running trip is off route.
		/// </summary>
		ActiveOffRoute = 0x02,
		/// <summary>
		/// Active running trip on route.
		/// </summary>
		ActiveOnRoute = 0x03,
		/// <summary>
		/// Unknown message type.
		/// </summary>
		Unknown = 0x80,
	};

	/// <summary>
	/// Enumeration of the Duress State types
	/// </summary>
	public enum DuressStateType : byte
	{
		/// <summary>
		/// No active state.
		/// </summary>
		Inactive = 0x00,
		/// <summary>
		/// Emergency/Panic/Driver Duress.
		/// </summary>
		Duress = 0x01,
	};

	/// <summary>
	/// Enumeration of the Digital Input State types
	/// </summary>
	public enum DigitalInputStateType : byte
	{
		/// <summary>
		/// No active state.
		/// </summary>
		Inactive = 0x00,
		/// <summary>
		/// Active/Enabled.
		/// </summary>
		Active = 0x01,
	};

	/// <summary>
	/// Enumeration of trip running state
	/// </summary>
	public enum RunningStateType : byte
	{
		/// <summary>
		/// Normal running state.
		/// No Priority required
		/// </summary>
		Normal = 0x00,
		/// <summary>
		/// Early running state.
		/// No Priority required
		/// </summary>
		Early = 0x01,
		/// <summary>
		/// Reserved
		/// </summary>
		Reserved = 0x02,
		/// <summary>
		/// Late running state.
		/// Priority required
		/// </summary>
		Late = 0x03,
	};

	public enum Block : byte
	{
		/// <summary>
		/// Route Configutation file
		/// </summary>
		RouteConfig = 0x06,
		/// <summary>
		/// Vehicle Configutation file
		/// </summary>
		VehicleConfig = 0x07,
		/// <summary>
		/// Transient next stop display text file
		/// </summary>
		DisplayNextStop = 0x08,
		/// <summary>
		/// Transient message to driver
		/// </summary>
		/// <remarks>First two bytes are message Id</remarks>
		MessageToDriver = 0x09,
		/// <summary>
		/// Transient message to system
		/// </summary>
		MessageToSystem = 0x0a,
		/// <summary>
		/// Driver logon Id
		/// 1-20 Character, optionally null terminated string.
		/// Logoff is a single null character.
		/// </summary>
		DriverLogon = 0x0b,
		/// <summary>
		/// Driver Configutation file
		/// </summary>
		ServiceAlert = 0x0c,
		/// <summary>
		/// Driver Configutation file
		/// </summary>
		DriverConfig = 0x0d,
		/// <summary>
		/// Vehicle Diagnostic DTCs
		/// </summary>
		DTC = 0x0e,
		/// <summary>
		/// <summary>
		/// Vehicle Diagnostic Values
		/// </summary>
		DiagnosticValues = 0x0f,
		/// NSA100 firmware update file
		/// </summary>
		NSA100 = 0x81,
		/// <summary>
		/// CPC J1708 intreface update file.
		/// </summary>
		CPCJ1708 = 0x80,
		/// <summary>
		/// CPC serial line bridge update file.
		/// </summary>
		CPCSLCAN = 0x82,
		/// <summary>
		/// Media player firmware update file.
		/// </summary>
		CMP = 0x83,
	};

	/// <summary>
	/// Trip progress against schedule
	/// </summary>
	public enum TripPriorityProgress : byte
	{
		/// <summary>
		/// Trip is running to schedule.
		/// </summary>
		Normal = 0,
		/// <summary>
		/// Trip is running ahead of schedule.
		/// </summary>
		Early = 1,
		/// <summary>
		/// Reserverd
		/// </summary>
		Reserved = 2,
		/// <summary>
		/// Trip is running behind schedule, request priority.
		/// </summary>
		LatePriority = 3,
	};

	/// <summary>
	/// Logon result
	/// </summary>
	public enum LogonState : byte
	{
		/// <summary>
		/// Logon OK
		/// </summary>
		LogonOK = 0,
		/// <summary>
		/// Logoff.
		/// </summary>
		Logoff = 1,
		/// <summary>
		/// Logon failed
		/// </summary>
		LogonFailed = 2,
	};

	/// <summary>
	/// Driver Status
	/// </summary>
	public enum DriverStatus : byte
	{
		/// <summary>
		/// Normal
		/// </summary>
		Normal = 1,
		/// <summary>
		/// Driver is on a break.
		/// </summary>
		Break = 2,
		/// <summary>
		/// Driver is out of the vehicle.
		/// </summary>
		OutOfVehicle = 3,
	};

	static public class CNXCANMsgHelper
	{
#if ANDROID
        private static log4droid CNXLog = new log4droid() {Logger = "CNXLogger"};
#else
        private static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif

        /// <summary>
        /// Builds a Duress frame.
        /// </summary>
        /// <param name="duress">Whether the duress is active.</param>
        /// <returns>Frame ready to be be put on the CAN bus.</returns>
        public static CANFrame DuressFrame(bool duress)
		{
			CANFrame frame = new CANFrame()
			{
				MailboxId = (uint)CNXMsgIds.DuressState
			};
			byte[] data = new byte[1];
			data[0] = duress ? (byte)0x01 : (byte)0x00;
			frame.Data = data;
			return frame;
		}

		/// <summary>
		/// Unpacks a duress frame
		/// </summary>
		/// <param name="frame">The frame to unpack.</param>
		/// <returns>The duress state.</returns>
		/// <remarks>An inactive state is returened for invalid or badly formatted frames.</remarks>
		public static bool DuressFrame(CANFrame frame)
		{
			if (frame.MailboxId != (uint)CNXMsgIds.DuressState || frame.DataLength == 0)
				return false;

			return (frame.Data[0] & 0x01) == 0x01;
		}

		public static CANFrame PackDateTime(DateTime dt)
		{
			CANFrame frame = new CANFrame()
			{
				MailboxId = (uint)CNXMsgIds.DateTime
			};
			byte[] data = new byte[6];
			// Day of month
			data[0] = (byte)dt.Day;
			// Month
			data[1] = (byte)dt.Month;
			// Year
			// I know, time bomb
			data[2] = (byte)(dt.Year - 2000);
			// Time 0-86400	Seconds since midnight
			int time = (dt.Hour * 3600) + (dt.Minute * 60) + dt.Second;
			byte[] intArray = BitConverter.GetBytes(time);
			// make sure byte 0 is lsb.
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intArray);
			Array.Copy(intArray, 0, data, 3, 3);

			frame.Data = data;
			//CNXLog.InfoFormat("PackDateTime DateTime {0} frame {1}", dt.ToString(), frame.DefaultToString());

			return frame;
		}

		public static DateTime UnpackDateTimeFrame(CANFrame frame)
		{
			if (frame.MailboxId != (uint)CNXMsgIds.DateTime)
				return new DateTime(0, DateTimeKind.Unspecified);

			//DateTime dt = new DateTime(0, DateTimeKind.Utc);
			//dt.AddDays((double)frame.Data[0]);
			//dt.AddMonths((int)frame.Data[1]);
			//// I know, time bomb
			//dt.AddYears((int)frame.Data[2] + 1999);
			//// sort the time bit out
			byte[] intArray = new byte[4];
			Array.Copy(frame.Data, 3, intArray, 0, 3);
			// check endianess
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intArray);
			//dt.AddSeconds((double)BitConverter.ToInt32(intArray, 0));
			int seconds = BitConverter.ToInt32(intArray, 0);
			int utcHours = seconds / 3600;
			seconds -= (utcHours * 3600);
			int utcMinutes = seconds / 60;
			seconds -= (utcMinutes * 60);
			DateTime dt = new DateTime((int)frame.Data[2] + 2000, frame.Data[1], frame.Data[0], utcHours, utcMinutes, seconds, 0, DateTimeKind.Utc);
			//CNXLog.InfoFormat("UnpackDateTime DateTime {0} frame {1}", dt.ToString(), frame.DefaultToString());
			return dt;
		}

		public static CANFrame PackProductId(byte prodId, byte buildNo, uint mask, uint status)
		{
			CANFrame frame = new CANFrame()
			{
				MailboxId = (uint)CNXMsgIds.ProductId
			};
			byte[] data;

			// allocate the data payload
			if (mask > 0x0000ffff)
				data = new byte[8];
			else if (mask > 0x000000ff)
				data = new byte[6];
			else if (mask > 0)
				data = new byte[4];
			else
				data = new byte[2];

			data[0] = prodId;
			data[1] = buildNo;

			if (data.Length > 3)
			{
				data[2] = (byte)mask;
				data[3] = (byte)status;
			}

			if (data.Length > 5)
			{
				data[4] = (byte)(((uint)mask >> 8) & 0xff);
				data[5] = (byte)(((uint)status >> 8) & 0xff);
			}

			if (data.Length > 7)
			{
				data[6] = (byte)(((uint)mask >> 16) & 0xff);
				data[7] = (byte)(((uint)status >> 16) & 0xff);
			}

			frame.Data = data;

			return frame;
		}

		public static CANFrame PackDestinationFrame(string dest)
		{
            CANFrame frame = new CANFrame()
            {
                MailboxId = (uint)CNXMsgIds.Destination
            };
			ASCIIEncoding ascii = new ASCIIEncoding();
			frame.Data = ascii.GetBytes(dest);			

			return frame;
		}

		public static string UnpackDestinationFrame(CANFrame frame)
		{
			if (frame.MailboxId == (uint)CNXMsgIds.Destination)
				return UnpackDestinationFrameData(frame.Data);

			return null;
		}

		public static string UnpackDestinationFrameData(byte[] data)
		{
			ASCIIEncoding ascii = new ASCIIEncoding();

			return ascii.GetString(data);
		}

		public static CANFrame PackFareSetFrame(byte fareSet)
		{
			CANFrame frame = new CANFrame()
			{
				MailboxId = (uint)CNXMsgIds.Fareset
			};
			byte[] data = new byte[1];
			data[0] = fareSet;
			frame.Data = data;			

			return frame;
		}

		/// <summary>
		/// Makes a TripRoute frame.
		/// </summary>
		/// <param name="routeCode">Up to 4 characters for the route number/code. May be null if no route code is required</param>
		/// <param name="tripNo">The trip number (limited to 24 bits).</param>
		/// <returns>The packed frame.</returns>
		public static CANFrame PackFarebox(string routeCode, int? tripNo)
		{
			CANFrame frame = new CANFrame()
			{
				MailboxId = (uint)CNXMsgIds.RouteTrip
			};

			// calculate the payload size
			byte[] data = new byte[(tripNo.HasValue ? 7 : 4)];

			// fill the mandetory route code
			for (int i = 0; i < 4; ++i)
				data[i] = 0;

			// add the route code
			if (routeCode != null)
			{
				ASCIIEncoding ascii = new ASCIIEncoding();
				byte[] rc = ascii.GetBytes(routeCode);
				Array.Copy(rc, data, (rc.Length > 4 ? 4 : rc.Length));
			}

			// add the tripNo
			if (tripNo.HasValue)
			{
				byte[] intArray = BitConverter.GetBytes(tripNo.Value);
				// make sure byte 0 is lsb.
				if (!BitConverter.IsLittleEndian)
					Array.Reverse(intArray);
				Array.Copy(intArray, 0, data, 4, 3);
			}

			frame.Data = data;

			return frame;
		}

		public static void UnpackFarebox(CANFrame frame, out string routeCode, out int? tripNo)
		{
			if (frame.MailboxId == (uint)CNXMsgIds.RouteTrip)
				UnpackFareboxData(frame.Data, out routeCode, out tripNo);
			else
			{
				routeCode = null;
				tripNo = null;
			}
		}

		public static void UnpackFareboxData(byte[] data, out string routeCode, out int? tripNo)
		{
			ASCIIEncoding ascii = new ASCIIEncoding();
			routeCode = ascii.GetString(data, 0, 4);

			if (data.Length > 4)
			{
				byte[] intArray = new byte[4];
				Array.Copy(data, 4, intArray, 0, data.Length - 4);
				if (!BitConverter.IsLittleEndian)
					Array.Reverse(intArray);
				tripNo = BitConverter.ToInt32(intArray, 0);
			}
			else
				tripNo = null;
		}

		/// <summary>
		/// Extracts trip progress information from the frame.
		/// </summary>
		/// <param name="frame">Trip progress frame.</param>
		/// <param name="msgType">Trip progress message type.</param>
		/// <param name="pathId">Id of the current path.</param>
		/// <param name="position">Position along the path.</param>
		/// <returns>True on success, false if the frame isnt a TripProgress frame or the data payload is the wrong size.</returns>
		public static bool UnpackTripProgress(CANFrame frame, out TripProgressType msgType, out int pathId, out int position, out int? tripNo)
		{
			pathId = 0;
			position = 0;
			tripNo = null;

			// validate message type and size
			if ((CNXMsgIds)frame.MailboxId != CNXMsgIds.TripProgress)
			{
				msgType = TripProgressType.Unknown;
				return false;
			}

			byte[] data = frame.Data;
			msgType = (TripProgressType)data[0];

			if (msgType != TripProgressType.ActiveOnRoute)
				return true;

			byte[] intArray = new byte[4];
			
			if (data.Length > 2)
			{
				intArray[0] = data[1];
				intArray[1] = (byte)(data[2] & 0x07);
				intArray[2] = 0;
				intArray[3] = 0;
				if (!BitConverter.IsLittleEndian)
					Array.Reverse(intArray);
				pathId = BitConverter.ToInt32(intArray, 0);
			}

			if (data.Length > 3)
			{
				intArray[0] = (byte)(data[2] >> 3);
				intArray[0] |= (byte)(data[3] << 5);
				intArray[1] = (byte)(data[3] >> 3);
				intArray[2] = 0;
				intArray[3] = 0;
				if (!BitConverter.IsLittleEndian)
					Array.Reverse(intArray);
				position = BitConverter.ToInt32(intArray, 0);
			}

			if (data.Length > 6)
			{
				intArray[0] = data[4];
				intArray[1] = data[5];
				intArray[2] = data[6];
				intArray[3] = 0;
				if (!BitConverter.IsLittleEndian)
					Array.Reverse(intArray);
				tripNo = BitConverter.ToInt32(intArray, 0);
			}

			return true;
		}

		/// <summary>
		/// Extracts trip progress information from the frame.
		/// </summary>
		/// <param name="frame">Trip progress frame.</param>
		/// <param name="pathId">Id of the current path.</param>
		/// <param name="position">Position along the path.</param>
		/// <param name="tripNo">Trip number</param>
		/// <param name="serviceStart">Service start</param>
		/// <param name="runningState">Running state</param>
		/// <returns>True on success, false if the frame isnt a TripProgress frame or the data payload is the wrong size.</returns>
		public static bool UnpackTripOnRoute(CANFrame frame, out short pathId, out int position, out int? tripNo, out ushort? serviceStart, out RunningStateType runningState)
		{
			pathId = 0;
			position = 0;
			tripNo = null;
			serviceStart = null;
			runningState = RunningStateType.Normal;

			// validate message type and size
			if ((CNXMsgIds)frame.MailboxId != CNXMsgIds.TripOnRoute || frame.DataLength < 8)
				return false;

			byte[] data = frame.Data;
			byte[] intArray = new byte[4];
			byte[] shortArray = new byte[2];

			shortArray[0] = data[0];
			shortArray[1] = (byte)(data[1] & 0x07);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(shortArray);
			pathId = BitConverter.ToInt16(shortArray, 0);

			intArray[0] = (byte)(data[1] >> 3);
			intArray[0] |= (byte)(data[2] << 5);
			intArray[1] = (byte)(data[2] >> 3);
			intArray[2] = 0;
			intArray[3] = 0;
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intArray);
			position = BitConverter.ToInt32(intArray, 0);

			intArray[0] = data[3];
			intArray[1] = data[4];
			intArray[2] = data[5];
			intArray[3] = 0;
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intArray);
			tripNo = BitConverter.ToInt32(intArray, 0);

			shortArray[0] = data[6];
			shortArray[1] = (byte)(data[7] & 0x3f);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(shortArray);
			serviceStart = BitConverter.ToUInt16(shortArray, 0);

			runningState = (RunningStateType)(data[7] >> 6);

			return true;
		}

		public static CANFrame PackIdentifiers(ushort commsAddress, byte companyTag)
		{

			CANFrame frame = new CANFrame()
			{
				MailboxId = (uint)CNXMsgIds.Identifiers
			};
			byte[] data = new byte[3];
			byte[] shortArray = BitConverter.GetBytes(commsAddress);
			// make sure byte 0 is lsb.
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(shortArray);
			Array.Copy(shortArray, 0, data, 0, 2);

			data[2] = companyTag;
			frame.Data = data;

			return frame;
		}

		public static bool UnpackIdentifiers(CANFrame frame, out ushort commsAddress, out byte companyTag)
		{
			commsAddress = 0;
			companyTag = 0;

			if ((CNXMsgIds)frame.MailboxId != CNXMsgIds.Identifiers || frame.DataLength < 3)
				return false;

			byte[] data = frame.Data;
			byte[] shortArray = new byte[2];
			shortArray[0] = data[0];
			shortArray[1] = data[1];
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(shortArray);
			commsAddress = BitConverter.ToUInt16(shortArray, 0);

			if (frame.DataLength > 2)
				companyTag = data[2];

			return true;
		}

		/// <summary>
		/// Builds a CAN frame for the GPS message.
		/// </summary>
		/// <param name="gpsState"></param>
		/// <param name="latitude">WGS84 latitude.</param>
		/// <param name="lonitude">WGS84 longitude.</param>
		/// <param name="speed">Speed in m/s.</param>
		/// <returns>A CAN frame containing the data.</returns>
		public static CANFrame PackGPSFrame(byte gpsState, double latitude, double longitude, double speed)
		{
			byte[] data;

			CANFrame frame = new CANFrame()
			{
				MailboxId = (uint)CNXMsgIds.GPS
			};
			// check on GPS state, failed states are only one byte messages
			if ((gpsState & 0x02) == 0)
			{
				data = new byte[1];
				data[0] = (byte)(gpsState & 0x0f);
				frame.Data = data;

				return frame;
			}

			//// keep tabs on the sign bits
			//bool latIsNeg = (latitude < 0); 
			//bool lonIsNeg = (longitude < 0); 
			// latitude is made into a 25 bit value.
			int lat = (int)Math.Truncate(latitude * 100000);
			// clear bits above bit 24
			lat &= 0x1ffffff;
			// longitude is made into a 26 bit value.
			int lon = (int)Math.Truncate(longitude * 100000);
			// clear bits above bit 24
			lon &= 0x3ffffff;
			// speed is made into a 9 bit value scaled by 11.
			int spd = (int)Math.Truncate(speed * 11);
			if (spd > 511)
				// cap top speed to 9 bits (511)
				spd = 511;
			// build the data array for the compressed data
			data = new byte[8];
			// get the bytes for latitude
			byte[] intArray = BitConverter.GetBytes(lat);
			// make sure byte 0 is lsb.
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intArray);
			// first byte is status and latitude lsb
			data[0] = (byte)(intArray[0] << 4);
			data[0] |= (byte)(gpsState & 0x0f);
			// next byte is bit 4 - 11 of latitude
			data[1] = (byte)(intArray[0] >> 4);
			data[1] |= (byte)(intArray[1] << 4);
			// next byte is bit 12 - 19 of latitude
			data[2] = (byte)(intArray[1] >> 4);
			data[2] |= (byte)(intArray[2] << 4);
			// next byte is bit 20 - 25 of latitude and bit 0 - 2 of longitude
			data[3] = (byte)(intArray[2] >> 4);
			data[3] |= (byte)(intArray[3] << 4);
			intArray = BitConverter.GetBytes(lon);
			// make sure byte 0 is lsb.
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intArray);
			data[3] |= (byte)(intArray[0] << 5);
			// next byte is bit 3 - 10 of longitude
			data[4] = (byte)(intArray[0] >> 3);
			data[4] |= (byte)(intArray[1] << 5);
			// next byte is bit 11 - 18 of longitude
			data[5] = (byte)(intArray[1] >> 3);
			data[5] |= (byte)(intArray[2] << 5);
			// next byte is bit 19 - 24 of longitude and speed lsb
			data[6] = (byte)(intArray[2] >> 3);
			data[6] |= (byte)(intArray[3] << 5);
			intArray = BitConverter.GetBytes(spd);
			// make sure byte 0 is lsb.
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intArray);
			data[6] |= (byte)(intArray[0] << 7);
			// next byte is bit 1 - 8 of speed
			data[7] = (byte)(intArray[0] >> 1);
			data[7] |= (byte)(intArray[1] << 7);
			frame.Data = data;

			return frame;
		}

		/// <summary>
		/// Unpacks a GPS frame.
		/// </summary>
		/// <param name="frame">The frame to be unpacked.</param>
		/// <param name="gpsState">Set to GPS state.</param>
		/// <param name="latitude">Set to latitude</param>
		/// <param name="longitude">Set to longitude</param>
		/// <param name="speed">Set to speed</param>
		/// <returns>True if the frame was successfully unpacked. False if malformed.</returns>
		public static bool UnpackGPSFrame(CANFrame frame, out byte gpsState, out double latitude, out double longitude, out double speed)
		{
			// check we have been given a GPS frame with some data.
			if (frame.MailboxId != (uint)CNXMsgIds.GPS || frame.DataLength == 0)
			{
				gpsState = 0;
				latitude = 0.0;
				longitude = 0.0;
				speed = 0.0;

				return false;
			}

			byte[] data = frame.Data;

			// check on GPS state, failed states are only one byte messages
			if ((data[0] & 0x02) == 0)
			{
				gpsState = (byte)(data[0] & 0x0f);
				latitude = 0.0;
				longitude = 0.0;
				speed = 0.0;

				return true;
			}

			// make sure other GPS states have a full frame of data
			if (frame.DataLength < 8)
			{
				gpsState = 0;
				latitude = 0.0;
				longitude = 0.0;
				speed = 0.0;

				return false;
			}

			// good to unpack a full GPS frame
			// first byte is status and latitude lsb
			gpsState = (byte)(data[0] & 0x0f);
			byte[] intArray = new byte[4];
			intArray[0] = (byte)(data[0] >> 4);
			intArray[0] |= (byte)(data[1] << 4);
			intArray[1] = (byte)(data[1] >> 4);
			intArray[1] |= (byte)(data[2] << 4);
			intArray[2] = (byte)(data[2] >> 4);
			intArray[2] |= (byte)(data[3] << 4);
			// if MS bit is 1 value is -ve and the bit needs extending to 32 bit value
			intArray[3] = (byte)(((data[3] & 0x10) == 0) ? 0 : 0xff);
			// check on endieness, make sure byte 0 is lsb.
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intArray);
			// latitude is scaled by 100000
			int lat = BitConverter.ToInt32(intArray, 0);
			latitude = lat;
			latitude /= 100000;

			// longitude starts bit 5 of byte 3
			intArray[0] = (byte)(data[3] >> 5);
			intArray[0] |= (byte)(data[4] << 3);
			intArray[1] = (byte)(data[4] >> 5);
			intArray[1] |= (byte)(data[5] << 3);
			intArray[2] = (byte)(data[5] >> 5);
			intArray[2] |= (byte)(data[6] << 3);
			intArray[3] = (byte)((data[6] >> 5) & 0x03);
			// if MS bit is 1 value is -ve and the bit needs extending to 32 bit value
			if ((intArray[3] & 0x02) != 0)
				intArray[3] |= 0xfe;
			// check on endieness, make sure byte 0 is lsb.
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intArray);
			// longitude is scaled by 100000
			longitude = BitConverter.ToInt32(intArray, 0);
			longitude /= 100000;

			// speed starts at bit 7 byte 6
			intArray[0] = (byte)(data[6] >> 7);
			intArray[0] |= (byte)(data[7] << 1);
			intArray[1] = (byte)(data[7] >> 7);
			intArray[2] = 0;
			intArray[3] = 0;
			// check on endieness, make sure byte 0 is lsb.
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(intArray);
			// speed is scaled by 11
			speed = BitConverter.ToInt32(intArray, 0);
			speed /= 11;

			return true;
		}

		public static CANGPSState UnpackGPSState(CANFrame frame)
		{
			// check we have been given a GPS frame with some data.
			if (frame.MailboxId != (uint)CNXMsgIds.GPS || frame.DataLength == 0)
				return CANGPSState.Invalid;

			return (CANGPSState)(frame.Data[0] & 0x03);
		}

		public static bool UnpackBlockQueryResponce(CANFrame frame, out Block blockType, out uint offset, out ushort crc, out byte version)
		{
			blockType = (Block)0;
			offset = 0;
			crc = 0;
			version = 255;

			if ((frame.MailboxId & 0xffffff00) != (uint)CNXMsgIds.BlockQueryResponse)
				return false;

			blockType = (Block)(frame.MailboxId & 0x0ff);
			byte[] data = frame.Data;

			if (data.Length > 3)
			{
				if (!BitConverter.IsLittleEndian)
					Array.Reverse(data, 0, 4);
				offset = BitConverter.ToUInt32(data, 0);
			}
			// crc supplied
			if (data.Length > 5)
			{
				if (!BitConverter.IsLittleEndian)
					Array.Reverse(data, 4, 2);
				crc = BitConverter.ToUInt16(data, 4);
			}
			// do we have some version information
			if (data.Length > 6)
				version = data[6];

			return true;
		}
	}
}
