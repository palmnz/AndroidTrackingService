using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

using CANLib;

namespace Tracking.Services
{
	/// <summary>
	/// Represents the current state of the tracking module.
	/// </summary>
	public class TrackingState
	{
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif

        /// <summary>
        /// Default value used to determine whether a position is within accepatable limits in meters.
        /// </summary>
        public const uint DefaultAcceptableError = 50;

		/// <summary>
		/// Operational state of the GPS.
		/// </summary>
		public enum GPS_STATE
		{
			/// <summary>
			/// Not receiving any input from the GPS unit.
			/// </summary>
			NO_GPS = 0,
			/// <summary>
			/// GPS reporting NO FIX.
			/// </summary>
			NO_FIX = 1,
			/// <summary>
			/// GPS reporting positions but the horizontal error exceeds the error threshold.
			/// </summary>
			BAD_FIX = 2,
			/// <summary>
			/// GPS reporting positions that are within the required tolerance.
			/// </summary>
			GOOD_FIX = 3
		}

		private GPS_STATE mGPSState = GPS_STATE.NO_GPS;
		/// <summary>
		/// Gets the current GPS state.
		/// </summary>
		public GPS_STATE State { get { return mGPSState; } }

		private uint mAcceptableError = DefaultAcceptableError;
		/// <summary>
		/// The value used to determine whether a position is within accepatable limits in meters.
		/// </summary>
		public uint AcceptableError
		{
			get { return mAcceptableError; }
			set { mAcceptableError = value; }
		}

		private double mLat = double.NaN;
		/// <summary>
		/// Current latitude in WGS84 decimal degrees. 
		/// </summary>
		public double Latitude { get { return mLat; } }

		private double mLong = double.NaN;
		/// <summary>
		/// Current longitude in WGS84 decimal degrees.
		/// </summary>
		public double Longitude { get { return mLong; } }

		private double mVelocity = double.NaN;
		/// <summary>
		/// Current horizontal velocity in meters/second.
		/// </summary>
		public double Velocity { get { return mVelocity; } }

		private uint mError = 0;
		/// <summary>
		/// Gets the GPS error.
		/// </summary>
		public uint Error { get { return mError; } }

		private bool mAlarm = false;
		/// <summary>
		/// Gets/Sets the alarm state.
		/// </summary>
		public bool Alarm
		{
			get { return mAlarm; }
			set { mAlarm = value; }
		}

		/// <summary>
		/// Sets the current location.
		/// </summary>
		/// <param name="latitude">Current latitude in WGS84 decimal degrees.</param>
		/// <param name="longitude">Current longitude in WGS84 decimal degrees.</param>
		/// <param name="velocity">Current horizontal velocity in meters/second.</param>
		/// <param name="error">Estimated horizontal positional error in meters (95% confidence level).</param>
		/// <remarks>
		/// When the TrackingState is packed into a byte array to be used as an RTT message valuse may be trncated as follows: -
		/// Latitude/Longitude will be truncated to single presision floating point numbers.
		/// The error values is packed into 6 bits in 3 meter increments giving a maximum value of 208 meters.
		/// </remarks>
		public void SetLocation(double latitude, double longitude, double velocity, uint error)
		{
			mLat = latitude;
			mLong = longitude;
			mVelocity = velocity;
			mError = error;

			mGPSState = (error > mAcceptableError) ? GPS_STATE.BAD_FIX : GPS_STATE.GOOD_FIX;

			// log bad fixes
			if (mGPSState == GPS_STATE.BAD_FIX)
				CNXLog.ErrorFormat("GPS Precision {0}, {1}, {2}M", latitude, longitude, error * 3); 
		}

		/// <summary>
		/// Used to indicate that there is no input from the GPS.
		/// </summary>
		public bool NoGps
		{
			set
			{
				if (value)
				{
					mLat = double.NaN;
					mLong = double.NaN;
					mVelocity = double.NaN;
					mError = 0;
					mGPSState = GPS_STATE.NO_GPS;
				}
			}
		}

		/// <summary>
		/// Used to indicate that the GPS is reporting no valid fix.
		/// </summary>
		public bool NoFix
		{
			set
			{
				if (value)
				{
					mLat = double.NaN;
					mLong = double.NaN;
					mVelocity = double.NaN;
					mError = 0;
					mGPSState = GPS_STATE.NO_FIX;
				}
			}
		}

		/// <summary>
		/// Gets/Sets the encoded state of the current tracking module state.
		/// </summary>
		/// <remarks>
		/// The GPS state is encoded into the lowest two bits.
		/// The MSB is the alarm state.
		/// The bits 2 - 6 contain the horizontal error in meters divided by 5 giving a resolution of 5 meters.
		/// Errors greater than 155M will be reported as 155M.
		/// </remarks>
		public byte EncodedState
		{
			get
			{
				byte state = (byte)mGPSState;
				byte error = (mError > (31 * 5)) ? (byte)31 : (byte)(mError / 5);
				error <<= 2;
				state = (byte)(state | error);
				state = (mAlarm) ? (byte)(state | 0x80) : (byte)(state & 0x7f);

				return state;
			}
			set
			{
				mGPSState = (GPS_STATE)(value & 0x03);
				mError = (uint)(((value & 0x7f) >> 2) * 5);
				mAlarm = (value & 0x80) != 0;
			}
		}

		/// <summary>
		/// Gets the tracking state and position as an array of bytes.
		/// </summary>
		/// <param name="encodeForWire">Whether to ensure the values conatined in the array are encoded for IP transmission (Big Endian).</param>
		/// <remarks>The velocity is only incerted if the error is within limits.</remarks>
		public byte[] GetBytes(bool encodeForWire)
		{
			byte[] bytes = null;

			if (mGPSState == GPS_STATE.NO_GPS || mGPSState == GPS_STATE.NO_FIX)
			{
				bytes = new byte[1];
				bytes[0] = EncodedState;
			}
			else
			{
				bool includeVelocity = (mGPSState == GPS_STATE.GOOD_FIX);
				bytes = new byte[(includeVelocity) ? 10 : 9];
				try
				{
					// get the encoded state
					bytes[0] = EncodedState;
					int index = 1;
					// Latitude
					BitConverter.GetBytes((float)mLat).CopyTo(bytes, index);
					if (BitConverter.IsLittleEndian && encodeForWire)
						Array.Reverse(bytes, index, 4);
					index += 4;
					//Longitude
					BitConverter.GetBytes((float)mLong).CopyTo(bytes, index);
					if (BitConverter.IsLittleEndian && encodeForWire)
						Array.Reverse(bytes, index, 4);
					index += 4;
					if (includeVelocity)
					{
						// Velocity, encode as Kilometers/hour
						double v = mVelocity * 3.6;
						if (v == Double.NaN || mGPSState == GPS_STATE.BAD_FIX)
							bytes[index] = 0xff;
						else if (v > 254)
							bytes[index] = 254;
						else
							bytes[index] = (byte)Math.Round(v);
					}
				}
				catch (Exception e)
				{
					CNXLog.ErrorFormat(e.ToString());
				}
			}

			return bytes;
		}

		/// <summary>
		/// Gets the message as an array of bytes ready to send to the server
		/// </summary>
		/// <param name="bytes">The array to insert the message into.</param>
		/// <param name="startIndex">Starting index for inserting the message.</param>
		/// <returns>Number of bytes inserted.</returns>
		/// <param name="encodeForWire">Whether to ensure the values conatined in the array are encoded for IP transmission (Big Endian).</param>
		/// <remarks>A negative value indicates that the message would not fit into the array and is the number or bytes that the array is short.</remarks>
		public int GetBytes(byte[] bytes, int startIndex, bool encodeForWire)
		{
			byte[] ourBytes = GetBytes(encodeForWire);

			int length = (bytes.Length - startIndex) - ourBytes.Length;
			if (length < 0)
				return length;

			ourBytes.CopyTo(bytes, startIndex);

			return ourBytes.Length;
		}

		/// <summary>
		/// Parses an array of bytes initialising this instance from the data.
		/// </summary>
		/// <param name="bytes">The array containing the data.</param>
		/// <param name="startIndex">Starting position in the array to start parsing from.</param>
		/// <param name="encodeFromWire">Whether the data was recieved across an IP network (Big Endian).</param>
		/// <remarks>
		/// The first byte should be the status byte the latitude/lonitude data would optionally follow this.
		/// </remarks>
		public void Parse(byte[] bytes, int startIndex, bool encodeFromWire)
		{
			// first byte, GPS state and error
			EncodedState = bytes[startIndex];

			// test for latitude/longitude
			if ((bytes.Length - startIndex) > 8)
			{
				if (encodeFromWire && BitConverter.IsLittleEndian)
				{
					Array.Reverse(bytes, startIndex + 1, 4);
					Array.Reverse(bytes, startIndex + 5, 4);
				}
				mLat = (double)BitConverter.ToSingle(bytes, startIndex + 1);
				mLong = (double)BitConverter.ToSingle(bytes, startIndex + 5);
				mVelocity = (double)bytes[startIndex + 9] / 3.6;
			}
			else
			{
				// no lat/long so default them
				mLat = double.NaN;
				mLong = double.NaN;
				mVelocity = double.NaN;
			}
		}

		/// <summary>
		/// Clones a TrackingState object.
		/// </summary>
		/// <param name="ts">The TrackingState object to clone.</param>
		/// <returns>The cloned TrackingState.</returns>
		public static TrackingState Clone(TrackingState ts)
		{
			TrackingState _ts = new TrackingState();
			_ts.mGPSState = ts.mGPSState;
			_ts.mError = ts.mError;
			_ts.mAlarm = ts.mAlarm;
			_ts.mLat = ts.mLat;
			_ts.mLong = ts.mLong;

			return _ts;
		}

		public override string ToString()
		{
			string ret = string.Format("Alarm {0}, {1}", (mAlarm ? "Active" : "Clear"), mGPSState.ToString());
			if (mGPSState > GPS_STATE.NO_FIX)
			{
				object[] args = new object[]{mLat, mLong, mVelocity, mError};
				ret += string.Format(", Pos {0:F4}, {1:F4} V={2:F2} Err {3}", args);
			}

			return ret;
		}
	}

	/// <summary>
	/// Class used to carry versioning information about the connected equipment.
	/// </summary>
	public class Versioning
	{
		private ReadOnlyCollection<DeviceCatalogueInfo> mCatalogue;

		public ReadOnlyCollection<DeviceCatalogueInfo> DeviceCatalogue
		{
			get { return mCatalogue; }
			set { mCatalogue = value; }
		}

		private byte mRouteResourceVersion = 0;
		/// <summary>
		/// Gets/Sets the version of the route based resources.
		/// </summary>
		public byte RouteResourceVersion
		{
			get { return mRouteResourceVersion; }
			set { mRouteResourceVersion = value; }
		}

		private byte mConfigResourceVersion = 0;
		/// <summary>
		/// Gets/Sets the version of the vehicle configuration.
		/// </summary>
		public byte ConfigResourceVersion
		{
			get { return mConfigResourceVersion; }
			set { mConfigResourceVersion = value; }
		}

		public Versioning(byte configVer, byte routeVer, ReadOnlyCollection<DeviceCatalogueInfo> catalogue)
		{
			mConfigResourceVersion = configVer;
			mRouteResourceVersion = routeVer;
			mCatalogue = catalogue;
		}

		public Versioning(byte configVer, byte routeVer)
		{
			mConfigResourceVersion = configVer;
			mRouteResourceVersion = routeVer;
			mCatalogue = null;
		}

		/// <summary>
		/// Gets the versioning as an array of bytes.
		///   0|  RESROUTE VER | Optional Vehicle resource route pattern version
		///   1| RESCONFIG VER | Optional Vehicle resource configuration version
		/// ...|     PRODID    | Optional Product Id
		/// ...|    PRODVER    | Optional Product version
		/// ...
		/// ...
		/// ...
		/// </summary>
		/// <param name="encodeForWire">Whether to ensure the values conatined in the array are encoded for IP transmission (Big Endian).</param>
		public byte[] GetBytes(bool encodeForWire)
		{
			// sort out the length, route + config + number of catalogue pairs
			int length = 2;
			if (mCatalogue != null)
				length += mCatalogue.Count * 2;

			byte[] bytes = new byte[length];

			int index = 0;
			bytes[index++] = mRouteResourceVersion;
			bytes[index++] = mConfigResourceVersion;
			if (mCatalogue != null)
			{
				foreach (DeviceCatalogueInfo dci in mCatalogue)
				{
					bytes[index++] = (byte)dci.ProductId;
					bytes[index++] = dci.BuildNo;
				}
			}
			
			return bytes;
		}

		/// <summary>
		/// Gets the versioning as an array of bytes.
		///   0|  RESROUTE VER | Optional Vehicle resource route pattern version
		///   1| RESCONFIG VER | Optional Vehicle resource configuration version
		/// ...|     PRODID    | Optional Product Id
		/// ...|    PRODVER    | Optional Product version
		/// ...
		/// ...
		/// ...
		/// </summary>
		/// <param name="bytes">The array to insert the message into.</param>
		/// <param name="startIndex">Starting index for inserting the message.</param>
		/// <returns>Number of bytes inserted.</returns>
		/// <param name="encodeForWire">Whether to ensure the values conatined in the array are encoded for IP transmission (Big Endian).</param>
		/// <remarks>
		/// A negative value indicates that the message would not fit into the array and is the number or bytes that the array is short.
		/// </remarks>
		public int GetBytes(ref byte[] bytes, int startIndex, bool encodeForWire)
		{
			byte[] ourBytes = GetBytes(encodeForWire);

			int length = (bytes.Length - startIndex) - ourBytes.Length;
			if (length < 0)
				return length;

			ourBytes.CopyTo(bytes, startIndex);

			return ourBytes.Length;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("Config Ver {0}, Route Ver {1}, Firmware pairs", mConfigResourceVersion, mRouteResourceVersion);
			if (mCatalogue != null)
			{
				foreach (DeviceCatalogueInfo dci in mCatalogue)
				{
					sb.Append(' ');
					sb.Append(dci.ProductId);
					sb.Append(' ');
					sb.Append(dci.BuildNo);
				}
			}
			return sb.ToString();
		}
	}

	/// <summary>
	/// Class for composing/decomposing basic messages exchanged with the RTT Server.
	/// </summary>
	/// <remarks>All messages are transfered in 'wire endian' i.e. Big Endian.</remarks>
	public class BasicMobileMessage
	{
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif
        private byte mEquipemntMask = 0;
		private byte mEquipemntStatus = 0;
		private Versioning mVersioning = null;

		private TrackingState mTrackingState = new TrackingState();
		/// <summary>
		/// Gets/sets the tracking state of the message. This includes the GPS State, GPS error and optionally the location
		/// </summary>
		public TrackingState TrackingState
		{
			get { return mTrackingState; }
			set { mTrackingState = TrackingState.Clone(value); }
		}

		private byte mMessageNumber = 0;
		/// <summary>
		/// The message number of this message used for sequencing and accounting.
		/// </summary>
		public byte MessageNumber
		{
			get { return mMessageNumber; }
			set { mMessageNumber = value; }
		}

		private ushort mCommsAddress = 0xffff;
		/// <summary>
		/// The CommsAddress of this message, used to identify the source.
		/// </summary>
		public ushort CommsAddress
		{
			get { return mCommsAddress; }
			set { mCommsAddress = value; }
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendFormat("Addr {0}({0:x4}) Num {2} {3}", mCommsAddress, mCommsAddress, mMessageNumber, mTrackingState);
			if (mVersioning != null)
			{
				if (mVersioning.DeviceCatalogue != null)
				{
					foreach (DeviceCatalogueInfo dci in mVersioning.DeviceCatalogue)
						sb.AppendFormat("{2}{0} Ver {1}", dci.ProductId, dci.BuildNo, Environment.NewLine);
				}
				sb.AppendFormat("{2}Equip {0},{1}.", (DeviceCatalogueInfo.EquipmentFlages)mEquipemntMask, (DeviceCatalogueInfo.EquipmentFlages)mEquipemntStatus, Environment.NewLine);
			}
			else if (mEquipemntMask != 0)
				sb.AppendFormat(" Equip {0} {1}.", (DeviceCatalogueInfo.EquipmentFlages)mEquipemntMask, (DeviceCatalogueInfo.EquipmentFlages)mEquipemntStatus);

			return sb.ToString();
		}

		/// <summary>
		/// Gets the message as an array of bytes ready to send to the server.
		/// </summary>
		/// <param name="encodeForWire">Whether to ensure the values conatined in the array are encoded for IP transmission (Big Endian).</param>
		/// <returns>An array containing the encoded message.</returns>
		/// <remarks>
		/// Message lengths and content are variable. These are the various encodings based on GPS state: -
		/// GPS_STATE.NO_GPS, GPS_STATE.NO_FIX, GPS_STATE.BAD_FIX
		/// Bytes 0 - 3 are madetory.
		/// Bytes 4 - 5 are optional.
		/// Bytes 6 - 7 are optional.
		/// Byte 8 onwards are further optional bytes and contain product Id/version pairs.
		/// Messages may be 4, 6, 8 or longer
		///     7 6 5 4 3 2 1 0
		///   0|    COMMSa     | Communications Address MSB
		///   1|    COMMSb     | Communications Address LSB
		///   2|    NUMBER     | Message Number
		///   3|    STATUS     | Bit 7 is the alarm state. The GPS state is the lowest two bits. Bits 2-5 are the horizontal error in meters divided by 5 giving a resolution of 5 meters.
		///    |A| ERROR   |0|0| No GPS
		///    |A| ERROR   |0|1| No Fix (Reported from GPS Rx)
		///    |A| ERROR   |1|0| Poor Fix (Filtered)
		///    |A| ERROR   |1|1| Good Fix
		///   4|   EQUIPMASK   | Optional Equipment mask
		///   5|  EQUIPSTATUS  | Optional Equipment status
		///   6|  RESROUTE VER | Optional Vehicle resource route pattern version
		///   7| RESCONFIG VER | Optional Vehicle resource configuration version
		/// ...|     PRODID    | Optional Product Id
		/// ...|    PRODVER    | Optional Product version
		/// ...
		/// ...
		/// ...
		/// 
		/// GPS_STATE.GOOD_FIX
		/// Bytes 0 - 3 are madetory.
		/// Bytes 9 - 10 are optional.
		/// Bytes 11 - 12 are optional.
		/// Byte 13 onwards are further optional bytes and contain product Id/version pairs.
		/// Messages may be 9, 11, 12 or longer.
		///     7 6 5 4 3 2 1 0
		///   0|    COMMSa     | Communications Address MSB
		///   1|    COMMSb     | Communications Address LSB
		///   2|    NUMBER     | Message Number
		///   3|    STATUS     | Bit 7 is the alarm state. The GPS state is the lowest two bits. Bits 2-5 are the horizontal error in meters divided by 5 giving a resolution of 5 meters.
		///    |A| ERROR   |0|0| No GPS
		///    |A| ERROR   |0|1| No Fix (Reported from GPS Rx)
		///    |A| ERROR   |1|0| Poor Fix (Filtered)
		///    |A| ERROR   |1|1| Good Fix
		///   4|      LATa     | Latitude MSB
		///   5|      LATb     | Latitude
		///   6|      LATc     | Latitude
		///   7|      LATd     | Latitude LSB
		///   4|      LONa     | Longitude MSB
		///   5|      LONb     | Longitude
		///   6|      LONc     | Longitude
		///   7|      LONd     | Longitude LSB
		///   8|     SPEED     | Speed Kilometers/hour (0xFF indicates an invalid speed)
		///   9|   EQUIPMASK   | Optional Equipment mask
		///  10|  EQUIPSTATUS  | Optional Equipment status
		///  11|  RESROUTE VER | Optional Vehicle resource route pattern version
		///  12| RESCONFIG VER | Optional Vehicle resource configuration version
		/// ...|     PRODID    | Optional Product Id
		/// ...|    PRODVER    | Optional Product version
		/// ...
		/// ...
		/// ...
		/// </remarks>
		public byte[] GetBytes(bool encodeForWire)
		{
			// Populate the array
			return GetBytes(MessageNumber, CommsAddress, encodeForWire);
		}

		/// <summary>
		/// Gets the message as an array of bytes ready to send to the server.
		/// </summary>
		/// <param name="messageNumber">This message number</param>
		/// <param name="commsAddress">The CommsAddress of the message.</param>
		/// <param name="encodeForWire">Whether to ensure the values conatined in the array are encoded for IP transmission (Big Endian).</param>
		/// <returns>An array containing the encoded message.</returns>
		/// <remarks>
		/// Message lengths and content and variable. These are the various encodings based on GPS state: -
		/// GPS_STATE.NO_GPS, GPS_STATE.NO_FIX, GPS_STATE.BAD_FIX
		/// Bytes 0 - 3 are madetory.
		/// Bytes 4 - 5 are optional.
		/// Bytes 6 - 7 are optional.
		/// Byte 8 onwards are further optional bytes and contain product Id/version pairs.
		/// Messages may be 4, 6, 8 or longer
		///     7 6 5 4 3 2 1 0
		///   0|    COMMSa     | Communications Address MSB
		///   1|    COMMSb     | Communications Address LSB
		///   2|    NUMBER     | Message Number
		///   3|    STATUS     | Bit 7 is the alarm state. The GPS state is the lowest two bits. Bits 2-5 are the horizontal error in meters divided by 5 giving a resolution of 5 meters.
		///    |A| ERROR   |0|0| No GPS
		///    |A| ERROR   |0|1| No Fix (Reported from GPS Rx)
		///    |A| ERROR   |1|0| Poor Fix (Filtered)
		///    |A| ERROR   |1|1| Good Fix
		///   4|   EQUIPMASK   | Optional Equipment mask
		///   5|  EQUIPSTATUS  | Optional Equipment status
		///   6|  RESROUTE VER | Optional Vehicle resource route pattern version
		///   7| RESCONFIG VER | Optional Vehicle resource configuration version
		/// ...|     PRODID    | Optional Product Id
		/// ...|    PRODVER    | Optional Product version
		/// ...
		/// ...
		/// ...
		/// 
		/// GPS_STATE.GOOD_FIX
		/// Bytes 0 - 3 are madetory.
		/// Bytes 9 - 10 are optional.
		/// Bytes 11 - 12 are optional.
		/// Byte 13 onwards are further optional bytes and contain product Id/version pairs.
		/// Messages may be 9, 11, 12 or longer.
		///     7 6 5 4 3 2 1 0
		///   0|    COMMSa     | Communications Address MSB
		///   1|    COMMSb     | Communications Address LSB
		///   2|    NUMBER     | Message Number
		///   3|    STATUS     | Bit 7 is the alarm state. The GPS state is the lowest two bits. Bits 2-5 are the horizontal error in meters divided by 5 giving a resolution of 5 meters.
		///    |A| ERROR   |0|0| No GPS
		///    |A| ERROR   |0|1| No Fix (Reported from GPS Rx)
		///    |A| ERROR   |1|0| Poor Fix (Filtered)
		///    |A| ERROR   |1|1| Good Fix
		///   4|      LATa     | Latitude MSB
		///   5|      LATb     | Latitude
		///   6|      LATc     | Latitude
		///   7|      LATd     | Latitude LSB
		///   4|      LONa     | Longitude MSB
		///   5|      LONb     | Longitude
		///   6|      LONc     | Longitude
		///   7|      LONd     | Longitude LSB
		///   8|     SPEED     | Speed Kilometers/hour (0xFF indicates an invalid speed)
		///   9|   EQUIPMASK   | Optional Equipment mask
		///  10|  EQUIPSTATUS  | Optional Equipment status
		///  11|  RESROUTE VER | Optional Vehicle resource route pattern version
		///  12| RESCONFIG VER | Optional Vehicle resource configuration version
		/// ...|     PRODID    | Optional Product Id
		/// ...|    PRODVER    | Optional Product version
		/// ...
		/// ...
		/// ...
		/// </remarks>
		public byte[] GetBytes(byte messageNumber, ushort commsAddress, bool encodeForWire)
		{
			const int equipmentLength = 2;
			const int headerLength = 3;

			// get the tracking state
			byte[] trackingState = mTrackingState.GetBytes(encodeForWire);
			byte[] versioning = null;
			if (mVersioning != null)
				versioning = mVersioning.GetBytes(encodeForWire);

			// get an appropriate sized array for the message.
			int messageLen = trackingState.Length + headerLength;
			//	(mEquipemntMask == 0) ? trackingState.Length + 3 : trackingState.Length + 5;
			// equipment status must be included if versioning is included
			if (versioning != null)
				messageLen += versioning.Length + equipmentLength;
			else if (mEquipemntMask != 0)
				messageLen += equipmentLength;
			byte[] message = new byte[messageLen];

			int index = 0;
			// set the Communications Address
			byte[] b = BitConverter.GetBytes(commsAddress);
			if (encodeForWire && BitConverter.IsLittleEndian)
				Array.Reverse(b);
			b.CopyTo(message, index);
			index += 2;
			// copy the messageNumber
			message[index++] = messageNumber;

			// copy the tracking state to the new array.
			trackingState.CopyTo(message, index);
			index += trackingState.Length;

			// set the equipment mask
			if (versioning != null || mEquipemntMask != 0)
			{
				message[index++] = mEquipemntMask;
				message[index++] = mEquipemntStatus;
			}

			// set the versioning
			if (versioning != null)
				versioning.CopyTo(message, index);

			return message;
		}

		/// <summary>
		/// Adds the equipment mask and status to the mobile message.
		/// </summary>
		/// <param name="mask">Equipment mask as per CAN messaging.</param>
		/// <param name="status">Equipment status as per CAN messaging.</param>
		public void SetDeviceCataloge(byte mask, byte status)
		{
			mEquipemntMask = mask;
			mEquipemntStatus = status;
		}

		/// <summary>
		/// Clears the device catalog information from the mobile message.
		/// </summary>
		public void ClearDeviceCataloge()
		{
			mEquipemntMask = 0;
			mEquipemntStatus = 0;
		}

		/// <summary>
		/// Adds the versioning to the mobile message.
		/// </summary>
		/// <param name="versioning">Versioning class containing the versioning information.</param>
		public void SetVersioning(Versioning versioning)
		{
			mVersioning = versioning;
		}

		/// <summary>
		/// Adds the versioning to the mobile message.
		/// </summary>
		public void SetVersioning(byte configVer, byte routeVer, ReadOnlyCollection<DeviceCatalogueInfo> catalogue)
		{
			mVersioning = new Versioning(configVer, routeVer, catalogue);
		}

		/// <summary>
		/// Clears the device catalog information from the mobile message.
		/// </summary>
		public void ClearVersioning()
		{
			mVersioning = null;
		}

		/// <summary>
		/// Parses an array of bytes initialising this instance from the data.
		/// </summary>
		/// <param name="bytes">The array containing the data.</param>
		/// <param name="startIndex">Starting position in the array to start parsing from.</param>
		/// <param name="encodeFromWire">Whether the data was recieved across an IP network (Big Endian).</param>
		/// <remarks>
		/// Message lengths and content are: -
		/// 4 byte
		///		2 byte communications address.
		///		1 byte message count.
		///		1 byte Vehicle State (Alarm state, error, GPS status).
		/// 6 byte
		///		2 byte communications address.
		///		1 byte message count.
		///		1 byte Vehicle State (Alarm state, error, GPS status).
		///		1 byte equipment mask.
		///		1 byte equipment status.
		/// 12 byte
		///		2 byte communications address.
		///		1 byte message count.
		///		1 byte Vehicle State (Alarm state, error, GPS status).
		///		4 byte latitude.
		///		4 byte longitude.
		/// 13 byte
		///		2 byte communications address.
		///		1 byte message count.
		///		1 byte Vehicle State (Alarm state, error, GPS status).
		///		4 byte latitude.
		///		4 byte longitude.
		///		1 byte speed
		/// 14 byte
		///		2 byte communications address.
		///		1 byte message count.
		///		1 byte Vehicle State (Alarm state, error, GPS status).
		///		4 byte latitude.
		///		4 byte longitude.
		///		1 byte equipment mask.
		///		1 byte equipment status.
		/// 15 byte
		///		2 byte communications address.
		///		1 byte message count.
		///		1 byte Vehicle State (Alarm state, error, GPS status).
		///		4 byte latitude.
		///		4 byte longitude.
		///		1 byte speed
		///		1 byte equipment mask.
		///		1 byte equipment status.
		///	16 byte or more
		///		2 byte
		/// </remarks>
		public void Parse(byte[] bytes, int startIndex, bool encodeFromWire)
		{
			// make a copy of the array so that we can play with it
			byte[] ourCopy = (byte[])bytes.Clone();

			if (encodeFromWire && BitConverter.IsLittleEndian)
				Array.Reverse(ourCopy, startIndex, 2);
			mCommsAddress = BitConverter.ToUInt16(ourCopy, startIndex);
			mMessageNumber = bytes[startIndex + 2];
			mTrackingState.Parse(ourCopy, startIndex + 3, encodeFromWire);
		}
	}
}
