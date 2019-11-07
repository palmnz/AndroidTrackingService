using System;

using Tracking.Services;

namespace NMEAGPSClient
{
	/// <summary>
	/// Provides time, position, velocity data.
	/// </summary>
	public partial struct GPSPosition
	{
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif

        private bool mInvalid;
		/// <summary>
		/// Indicates whether the data is invalid or stale.
		/// </summary>
		public bool Invalid
		{
			get { return mInvalid; }
		}

		/// <summary>
		/// Invalidates the GPSPosition structure
		/// </summary>
		public void Invalidate()
		{
			mInvalid = true;
			mPosition.Reset(double.NaN, double.NaN);

			mTag = String.Empty;
			mTimeStamp = DateTime.UtcNow;
			mTimeError = Double.MaxValue;
			mAltitude = Double.NaN;
			mHorizontalErrorEstimate = 0.0;
			mVerticalErrorEstimate = Double.MaxValue;
			mCourseOverGround = Double.NaN;
			mSpeedOverGround = Double.MinValue;
			mClimbSink = Double.NaN;
			mEstimatedErrorCourseOverGround = Double.MaxValue;
			mEstimatedErrorSpeedOverGround = Double.MaxValue;
			mEstimatedErrorClimbSink = Double.MaxValue;
			mNMEAMode = NMEAMODE.NO_FIX;
		}

		private string mTag;
		/// <summary>
		/// A tag identifying the last sentence received.
		/// For NMEA devices this is just the NMEA sentence name; the talker-ID portion may be useful for distinguishing among results produced by different NMEA talkers in the same wire.
		/// </summary>
		/// <remarks>An Empty string indicates no valid sentences have been recieved.</remarks>
		public string Tag
		{
			get { return mTag; }
		}

		private DateTime mTimeStamp;
		/// <summary>
		/// Date and time of the last update.
		/// </summary>
		/// <remarks>Null will be returned if there is no valid time stamp.</remarks>
		public DateTime TimeStamp
		{
			get { return mTimeStamp; }
		}

		private double mTimeError;
		/// <summary>
		/// Estimated timestamp error (seconds, 95% confidence).
		/// </summary>
		/// <remarks>NaN will be returned if there is no valid value.</remarks>
		public double TimeError
		{
			get { return mTimeError; }
		}

		private Position mPosition;
		/// <summary>
		/// Current WGS84 position.
		/// </summary>
		public Position CurrentPosition
		{
			get { return mPosition; }
		}
    
		private double mAltitude;
		/// <summary>
		/// Altitude in meters above mean sea level. If the mode field is not 3D this is an estimate and should be treated as unreliable.
		/// </summary>
		/// <remarks>NaN will be returned if there is no valid value.</remarks>
		public double Altitude
		{
			get { return mAltitude; }
		}
    
		private double mHorizontalErrorEstimate;
		/// <summary>
		/// Horizontal error estimate in meters.
		/// </summary>
		/// <remarks>NaN will be returned if there is no valid value.</remarks>
		public double HorizontalErrorEstimate
		{
		  get { return mHorizontalErrorEstimate; }
		}

		private double mVerticalErrorEstimate;
		/// <summary>
		/// Vertical error estimate in meters.
		/// </summary>
		/// <remarks>NaN will be returned if there is no valid value.</remarks>
		public double VerticalErrorEstimate
		{
		  get { return mVerticalErrorEstimate; }
		}

		private double mCourseOverGround;
		/// <summary>
		/// Course in degrees from true north.
		/// </summary>
		/// <remarks>NaN will be returned if there is no valid value.</remarks>
		public double CourseOverGround
		{
		  get { return mCourseOverGround; }
		}

		private double mSpeedOverGround;
		/// <summary>
		/// Speed in meters/sec.
		/// </summary>
		/// <remarks>NaN will be returned if there is no valid value.</remarks>
		public double SpeedOverGround
		{
			get { return mSpeedOverGround; }
		}

		private double mClimbSink;
		/// <summary>
		/// Current rate of climb as in meters per second.
		/// Some GPSes (not SiRF-based) do not report this, in that case a value is computed using the altitude from the last fix (if available).
		/// </summary>
		/// <remarks>NaN will be returned if there is no valid value.</remarks>
		public double ClimbSink
		{
			get { return mClimbSink; }
		}

		private double mEstimatedErrorCourseOverGround;
		/// <summary>
		/// Error estimate for course (in degrees, 95% confidence).
		/// </summary>
		/// <remarks>NaN will be returned if there is no valid value.</remarks>
		public double EstimatedErrorCourseOverGround
		{
			get { return mEstimatedErrorCourseOverGround; }
		}

		private double mEstimatedErrorSpeedOverGround;
		/// <summary>
		///	Error estimate for speed (in meters/sec, 95% confidence).
		/// </summary>
		private double EstimatedErrorSpeedOverGround
		{
			get { return mEstimatedErrorSpeedOverGround; }
		}

		private double mEstimatedErrorClimbSink;
		/// <summary>
		///	Error estimate for climb/sink (in meters/sec, 95% confidence).
		/// </summary>
		private double EstimatedErrorClimbSink
		{
			get { return mEstimatedErrorClimbSink; }
		}

		/// <summary>
		/// NO_MODE no mode value yet seen or reported.
		/// NO_FIX no fix.
		/// TWO_DIMENSION latitude/longitude positioning (no altitude).
		/// THREE_DIMENSION latitude/longitude/altitude positioning.
		/// </summary>
		public enum NMEAMODE { NO_MODE = 0, NO_FIX = 1, TWO_DIMENSION = 2, THREE_DIMENSION = 3 };
		private NMEAMODE mNMEAMode;
		/// <summary>
		/// The NMEA mode.
		/// </summary>
		/// <remarks>This field was not reported at protocol levels 2 and lower.</remarks>
		public NMEAMODE NMEAMode
		{
			get { return mNMEAMode; }
		}

		/// <summary>
		/// Parses a GPSD 'o' report.
		/// Attempts to return a complete time/position/velocity report as a unit.
		/// Any field for which data is not available being reported as ?.
		/// If there is no fix, the response is simply "O=?", otherwise a tag and timestamp are always reported.
		/// </summary>
		/// <param name="sentence">GPSD 'o' report.</param>
		/// <exception cref="System.Exception">Thrown if the sentence is not a GPSD report or sentence is not an 'O' report.</exception>
		public void Parse(string sentence)
		{
			// break up the sentence into fields
			Char[] splitChars = {' '};
			string[] fields = sentence.Split(splitChars);
			// validate the sentence
			if (!fields[0].StartsWith("O="))
				throw new Exception("Invalid sentence, not an 'O' report.");
			// make sure there is a complete report
			if (fields[0] == "O=?")
				// need to invalidate all data
				fields = new string[] { "O=?", "?","?","?","?","?","?","?","?","?","?","?","?","?","?" };
			if (fields.Length != 15)
				throw new Exception("Invalid sentence, invalid field count.");

			// looks good to go
			mTag = fields[0].Substring(2);
			// check that this is a fresh update
			double newStamp = (fields[1] == "?")? Double.NaN : Double.Parse(fields[1]);
			mInvalid = (newStamp == double.NaN);
			mTimeStamp = new DateTime((long)newStamp);
			mTimeError = (fields[2] == "?")? Double.NaN : Double.Parse(fields[2]);
			mPosition.Reset((fields[3] == "?")? Double.NaN : Double.Parse(fields[3]), (fields[4] == "?")? Double.NaN : Double.Parse(fields[4]));
			mAltitude = (fields[5] == "?")? Double.NaN : Double.Parse(fields[5]);
			mHorizontalErrorEstimate = (fields[6] == "?")? 0.0 : Double.Parse(fields[6]);
			mVerticalErrorEstimate = (fields[7] == "?")? Double.NaN : Double.Parse(fields[7]);
			mCourseOverGround = (fields[8] == "?")? Double.NaN : Double.Parse(fields[8]);
			mSpeedOverGround = (fields[9] == "?")? Double.NaN : Double.Parse(fields[9]);
			mClimbSink = (fields[10] == "?")? Double.NaN : Double.Parse(fields[10]);
			mEstimatedErrorCourseOverGround = (fields[11] == "?")? Double.MaxValue : Double.Parse(fields[11]);
			mEstimatedErrorSpeedOverGround = (fields[12] == "?")? Double.MaxValue : Double.Parse(fields[12]);
			mEstimatedErrorClimbSink = (fields[13] == "?")? Double.MaxValue : Double.Parse(fields[13]);
			mNMEAMode = (NMEAMODE)((fields[14] == "?")? 0 : Int32.Parse(fields[14]));
		}

		/// <summary>
		/// Parses a raw NMEA GGA (gps fix information) sentence.
		/// This is the prefered sentence to use for positions.
		/// It is only generated by NMEA 2.0 GPS units. All modern units should be version 2.0 complient.
		/// </summary>
		/// <remarks>
		/// GGA - essential fix data which provide 3D location and accuracy data.
		/// $GPGGA,123519,4807.038,N,01131.000,E,1,08,0.9,545.4,M,46.9,M,,*47
		/// Where:
		///		GGA				Global Positioning System Fix Data
		///		123519			Fix taken at 12:35:19 UTC
		///		4807.038,N		Latitude 48 deg 07.038' N
		///		01131.000,E		Longitude 11 deg 31.000' E
		///		1				Fix quality:	0 = invalid
		///										1 = GPS fix (SPS)
		///										2 = DGPS fix
		///										3 = PPS fix
		///										4 = Real Time Kinematic
		///										5 = Float RTK
		///										6 = estimated (dead reckoning) (2.3 feature)
		///										7 = Manual input mode
		///										8 = Simulation mode
		///		08				Number of satellites being tracked
		///		0.9				Horizontal dilution of position
		///		545.4,M			Altitude, Meters, above mean sea level
		///		46.9,M			Height of geoid (mean sea level) above WGS84 ellipsoid
		///		(empty field)	time in seconds since last DGPS update
		///		(empty field)	DGPS station ID number
		///		*47				the checksum data, always begins with *
		/// </remarks>
		/// <param name="sentence">NMEA GGA Senetence.</param>
		/// <returns>True if the sentence is successfully parsed.</returns>
		public bool ParseGGA(string sentence)
		{
			bool success = true;
			// The sentence should be a comma seperated list of values
			// Some may be empty!
			// Start by stripping the leading and trailing data.
			Char[] splitChars = { ',' };
			//string[] words = sentence.Substring(1, sentence.IndexOf('*')).Split(splitChars, StringSplitOptions.None);
			string[] words = sentence.Split(splitChars, StringSplitOptions.None);

            // save the sentence type
            mTag = words[0];

			// Do we have enough words to parse the fix status?
			//if (words.Length > 6 && words[6].Length != 0)
			//	// Get the fix code
			//	mNMEAMode = words[6].Equals("0", StringComparison.OrdinalIgnoreCase) ? NMEAMODE.NO_FIX : NMEAMODE.THREE_DIMENSION;
			//else
			//{
			//	// The fix status is invalid
			//	mNMEAMode = NMEAMODE.NO_MODE;
			//	success = false;
			//}

			// Only use GGA sentence to augment missing information from RMC sentence. Do not include time and position.
			// Do we have enough data to parse the location?
			//if (words.Length > 5 && words[2].Length != 0 && words[3].Length != 0 && words[4].Length != 0 && words[5].Length != 0)
			//    mPosition = ParseNMEAPosition(words[2], words[3], words[4], words[5]);
			//else
			//    success = false;

			// do we have enough info to process DOP?
			if (words.Length > 8 && words[8].Length != 0)
			{
				if (double.TryParse(words[8], out mHorizontalErrorEstimate))
					mHorizontalErrorEstimate *= 5.0;	// multiply DOP by the best accuracy of the GPS unit
				else
					mHorizontalErrorEstimate = 0.0;
			}

			return success;
		}


        /// <summary>
        /// Parses a raw NMEA GSA (gps fix information) sentence.
        /// This is the prefered sentence to use for positions.
        /// It is only generated by NMEA 2.0 GPS units. All modern units should be version 2.0 complient.
        /// </summary>
        /// <remarks>
        /// GSA - essential fix data which provide 3D location and accuracy data.
        /// $GPGSA,A,3,04,05,,09,12,,,24,,,,,2.5,1.3,2.1*39
        /// Where:
        ///		GSA         Satellite status
        //      A           Auto selection of 2D or 3D fix(M = manual)
        //      3           3D fix - values include: 1 = no fix
        //                                  2 = 2D fix
        //                                  3 = 3D fix
        //                  04,05... PRNs of satellites used for fix(space for 12)
        //                  2.5      PDOP(dilution of precision)
        //                  1.3      Horizontal dilution of precision(HDOP)
        //                  2.1      Vertical dilution of precision(VDOP)
        //                  *39      the checksum data, always begins with*
        /// </remarks>
        /// <param name="sentence">NMEA GGA Senetence.</param>
        /// <returns>True if the sentence is successfully parsed.</returns>
        public bool ParseGSA(string sentence)
        {
            bool success = true;
            // The sentence should be a comma seperated list of values
            // Some may be empty!
            // Start by stripping the leading and trailing data.
            Char[] splitChars = { ',' };
            //string[] words = sentence.Substring(1, sentence.IndexOf('*')).Split(splitChars, StringSplitOptions.None);
            string[] words = sentence.Split(splitChars, StringSplitOptions.None);

            // save the sentence type
            mTag = words[0];

            // Do we have enough words to parse the fix status?
            if (words.Length > 2 && words[2].Length != 0)
                // Get the fix code
                mNMEAMode = words[2].Equals("1", StringComparison.OrdinalIgnoreCase) ? NMEAMODE.NO_FIX : (words[2].Equals("2", StringComparison.OrdinalIgnoreCase) ? NMEAMODE.TWO_DIMENSION : NMEAMODE.THREE_DIMENSION);
            else
            {
                // The fix status is invalid
                mNMEAMode = NMEAMODE.NO_MODE;
                success = false;
            }

            return success;
        }

        /// <summary>
        /// Parses a raw NMEA RMC (recommended minimum data for gps) sentence.
        /// This provides the least information for positions but is guarenteed to be generated by all GPS units.
        /// </summary>
        /// <remarks>
        /// $GPRMC,hhmmss.ss,A,llll.ll,a,yyyyy.yy,a,x.x,x.x,ddmmyy,x.x,a*hh
        /// 1 = UTC of position fix
        /// 2 = Data status (Navigation receiver warning A = OK, V = warning)
        /// 3 = Latitude of fix
        /// 4 = N or S
        /// 5 = Longitude of fix
        /// 6 = E or W
        /// 7 = Speed over ground in knots
        /// 8 = Track made good in degrees True
        /// 9 = UT date
        /// 10 = Magnetic variation degrees (Easterly var. subtracts from true course)
        /// 11 = E or W
        /// 12 = Checksum
        /// </remarks>
        /// <param name="sentence">NMEA RMC Senetence.</param>
        /// <returns>True if the sentence is successfully parsed.</returns>
        public bool ParseRMC(string sentence)
		{
			bool success = true;
			// The sentence should be a comma seperated list of values
			// Some may be empty!
			// Start by stripping the leading and trailing data.
			Char[] splitChars = { ',' };
			//string[] words = sentence.Substring(1, sentence.IndexOf('*')).Split(splitChars, StringSplitOptions.None);
			string[] words = sentence.Split(splitChars, StringSplitOptions.None);

            // save the sentence type
            mTag = words[0];

			// Do we have enough words to parse the fix status?
			if (words.Length > 2 && words[2].Length != 0)
			{
                // Get the fix flag
                //if (words[2].Equals("A", StringComparison.OrdinalIgnoreCase))
                //    mNMEAMode = NMEAMODE.TWO_DIMENSION;
                //else
                //{
                //    mNMEAMode = NMEAMODE.NO_FIX;
                //    return false;
                //}

                if (!words[2].Equals("A", StringComparison.OrdinalIgnoreCase))
                {
                    mNMEAMode = NMEAMODE.NO_FIX;
                    return false;
                }
            }
			else
			{
				// The fix status is invalid
				mNMEAMode = NMEAMODE.NO_MODE;
				success = false;
			}

			// Do we have enough words to parse the UTC date/time?
			if (words.Length > 9)
			{
				if (words[1].Length != 0 && words[9].Length != 0)
				{
					try
					{
						// time part
						string utcTimeWord = words[1];
						int utcHours = int.Parse(utcTimeWord.Substring(0, 2), NMEAGPSClient.NMEACultureInfo);                 // AA
						int utcMinutes = int.Parse(utcTimeWord.Substring(2, 2), NMEAGPSClient.NMEACultureInfo);               // BB
						int utcSeconds = int.Parse(utcTimeWord.Substring(4, 2), NMEAGPSClient.NMEACultureInfo);               // CC
						int utcMilliseconds = 0;
						if (utcTimeWord.Length > 6)
							utcMilliseconds = Convert.ToInt32(float.Parse(utcTimeWord.Substring(6)) * 1000, NMEAGPSClient.NMEACultureInfo);    // DDDD

						// date part
						string utcDateWord = words[9];
						int utcDay = int.Parse(utcDateWord.Substring(0, 2), NMEAGPSClient.NMEACultureInfo);
						int utcMonth = int.Parse(utcDateWord.Substring(2, 2), NMEAGPSClient.NMEACultureInfo);
						int utcYear = int.Parse(utcDateWord.Substring(4, 2), NMEAGPSClient.NMEACultureInfo) + 2000;

						// make the date/time
						mTimeStamp = new DateTime(utcYear, utcMonth, utcDay, utcHours, utcMinutes, utcSeconds, utcMilliseconds, DateTimeKind.Utc);
						//CNXLog.InfoFormat("ParseRMC DateTime {0} time word {1} date word {2}", mTimeStamp.ToString(), words[1], words[9]);
					}
					catch (Exception e)
					{
						// The UTC date/time is invalid
						CNXLog.ErrorFormat("ParseRMC DateTime error {0} {1}", sentence, e);
						mTimeStamp = DateTime.MaxValue;
						success = false;
					}
				}
				else
				{
					// The UTC date/time is invalid
					CNXLog.ErrorFormat("ParseRMC DateTime sentence length error {0} {1}", sentence);
					mTimeStamp = DateTime.MaxValue;
					success = false;
				}
			}
			else
			{
				// The UTC date/time is invalid
				CNXLog.ErrorFormat("ParseRMC DateTime sentence length error {0} {1}", sentence);
				mTimeStamp = DateTime.MaxValue;
				success = false;
			}

			// Do we have enough data to parse the location?
			if (words.Length > 6 && words[3].Length != 0 && words[4].Length != 0 && words[5].Length != 0 && words[6].Length != 0)
				mPosition = ParseNMEAPosition(words[3], words[4], words[5], words[6]);

            // Do we have enough info to process speed?
            if (words.Length > 7 && words[7].Length != 0)
				// The speed is the sixth word, expressed in knots                
				// Conversion, 1,852 Knot to meter
				mSpeedOverGround = ParseVelocity(words[7]);
			else
				mSpeedOverGround = double.MinValue;

			// do we have enough info to process the bearing?
			if (words.Length > 8 && words[8].Length != 0)
			{
				// The bearing is the seventh word
				if (!double.TryParse(words[8], out mCourseOverGround))
					mCourseOverGround = double.NaN;
			}
			else
				mCourseOverGround = double.NaN;

			return success;
		}

		/// <summary>
		/// Diseminates NMEA2000 Position, Rapid Update CAN frames.
		/// </summary>
		/// <remarks>
		/// Position, Rapid Update PGN 129025
		/// First two bytes of a NMEA2000 frame indicate the 
		/// <field>
		/// name = "Latitude"
		/// type = "int"
		/// offset = "0"
		/// length = "32"
		/// signed = "yes"
		/// units = "deg"
		/// scaling = "0.0000001"
		/// </field>
		/// <field>
		/// name = "Longitude"
		/// type = "int"
		/// offset = "32"
		/// length = "32"
		/// signed = "yes"
		/// units = "deg"
		/// scaling = "0.0000001"
		/// </field>
		/// </remarks>
		public void ParsePositionRapidUpdate(byte[] data)
		{
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(data, 0, 4);
				Array.Reverse(data, 4, 4);
			}
			double lat = BitConverter.ToInt32(data, 0);
			lat *= 0.0000001;
			double lon = BitConverter.ToInt32(data, 4);
			lon *= 0.0000001;
			mPosition.Reset(lat, lon);
			mNMEAMode = NMEAMODE.TWO_DIMENSION;
			//Console.WriteLine("Position {0}", mPosition.ToString());
		}

		/// <summary>
		/// Diseminates NMEA2000 Date and Time Status CAN frames.
		/// </summary>
		/// <remarks>
		/// Date and Time Status PGN 129033 size 8
		/// <Fields>
		///		<Field>
		/// 		<Order>1</Order>
		///			<Id>date</Id>
		/// 		<Name>Date</Name>
		///			<Description>Days since January 1, 1970</Description>
		/// 		<BitLength>16</BitLength>
		///			<BitOffset>0</BitOffset>
		/// 		<BitStart>0</BitStart>
		///			<Units>days</Units>
		/// 		<Type>Date</Type>
		///			<Resolution>1</Resolution>
		///			<Signed>false</Signed>
		///		</Field>
		///		<Field>
		///			<Order>2</Order>
		///			<Id>time</Id>
		///			<Name>Time</Name>
		///			<Description>Seconds since midnight</Description>
		///			<BitLength>32</BitLength>
		///			<BitOffset>16</BitOffset>
		///			<BitStart>0</BitStart>
		///			<Units>s</Units>
		///			<Type>Time</Type>
		///			<Resolution>0.0001</Resolution>
		///			<Signed>false</Signed>
		///		</Field>
		///		<Field>
		///			<Order>3</Order>
		///			<Id>localOffset</Id>
		///			<Name>Local Offset</Name>
		///			<Description>Minutes</Description>
		///			<BitLength>16</BitLength>
		///			<BitOffset>48</BitOffset>
		///			<BitStart>0</BitStart>
		///			<Units>minutes</Units>
		///			<Type>Integer</Type>
		///			<Resolution>1</Resolution>
		///			<Signed>true</Signed>
		///		</Field>
		///	</Fields>
		/// </remarks>
		public void ParseDateAndTime(byte[] data)
		{
			if (!BitConverter.IsLittleEndian)
			{
				Array.Reverse(data, 0, 2);
				Array.Reverse(data, 2, 4);
			}
			ushort days = BitConverter.ToUInt16(data, 0);
			uint seconds = BitConverter.ToUInt32(data, 2);
			double totalSeconds = (days * (24 * 60 * 60));
			totalSeconds += (seconds / 10000);
			//CNXLog.WarnFormat("DateTime days {0}, since 1970-01-01 seconds since midnight {1}", days, seconds / 10000);
			try
			{
				DateTime epoc = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
				mTimeStamp = epoc.AddSeconds(totalSeconds);
				//CNXLog.WarnFormat("Now {0} calculated {1}", DateTime.UtcNow, mTimeStamp);
			}
			catch (Exception e)
			{
				throw new Exception(string.Format("Days {0}, Seconds {1}, total {2}.", days, seconds, totalSeconds), e);
			}
			//Console.WriteLine("Date Time {0}, seconds since 1970-01-01 {1}", mTimeStamp.ToString(), totalSeconds);
		}

		/// <summary>
		/// Parses NMEA2000 Course Over Ground Seep Over Ground
		/// </summary>
		/// <remarks>
		/// COG and SOG, Rapid Update PGN 129026 Size 8
		///	<Fields>
		///		<Field>
		///			<Order>1</Order>
		///			<Id>sid</Id>
		///			<Name>SID</Name>
		///			<BitLength>8</BitLength>
		///			<BitOffset>0</BitOffset>
		///			<BitStart>0</BitStart>
		///			<Signed>false</Signed>
		///		</Field>
		///		<Field>
		///			<Order>2</Order>
		///			<Id>cogReference</Id>
		///			<Name>COG Reference</Name>
		///			<BitLength>2</BitLength>
		///			<BitOffset>8</BitOffset>
		///			<BitStart>0</BitStart>
		///			<Type>Lookup table</Type>
		///			<Signed>false</Signed>
		///			<EnumValues>
		///				<EnumPair Value = '0' Name= 'True' />
		///				<EnumPair Value= '1' Name= 'Magnetic' />
		///			</EnumValues >
		///		</ Field >
		///		<Field>
		///			<Order>3</Order>
		///			<Id>reserved</Id>
		///			<Name>Reserved</Name>
		///			<Description>Reserved</Description>
		///			<BitLength>6</BitLength>
		///			<BitOffset>10</BitOffset>
		///			<BitStart>2</BitStart>
		///			<Type>Binary data</Type>
		///			<Signed>false</Signed>
		///		</Field>
		///		<Field>
		///			<Order>4</Order>
		///			<Id>cog</Id>
		///			<Name>COG</Name>
		///			<BitLength>16</BitLength>
		///			<BitOffset>16</BitOffset>
		///			<BitStart>0</BitStart>
		///			<Units>rad</Units>
		///			<Resolution>0.0001</Resolution>
		///			<Signed>false</Signed>
		///		</Field>
		///		<Field>
		///			<Order>5</Order>
		///			<Id>sog</Id>
		///			<Name>SOG</Name>
		///			<BitLength>16</BitLength>
		///			<BitOffset>32</BitOffset>
		///			<BitStart>0</BitStart>
		///			<Units>m/s</Units>
		///			<Resolution>0.01</Resolution>
		///			<Signed>false</Signed>
		///		</Field>
		///		<Field>
		///			<Order>6</Order>
		///			<Id>reserved</Id>
		///			<Name>Reserved</Name>
		///			<Description>Reserved</Description>
		///			<BitLength>16</BitLength>
		///			<BitOffset>48</BitOffset>
		///			<BitStart>0</BitStart>
		///			<Type>Binary data</Type>
		///			<Signed>false</Signed>
		///		</Field>
		///	</Fields>
		/// </remarks>
		/// <param name="data"></param>
		public void ParseCOGSOGBigEndian(byte[] data)
		{
			try
			{
				Array.Reverse(data, 0, 2);
				Array.Reverse(data, 2, 4);
				double cog = BitConverter.ToUInt16(data, 2);
				cog *= 0.0001;
				if (cog > Double.Epsilon)
					mCourseOverGround = cog;
				else
					mCourseOverGround = 0.0;
				double sog = BitConverter.ToUInt16(data, 4);
				sog *= 0.01;
				if (sog > Double.Epsilon)
					mSpeedOverGround = sog;
				else
					mSpeedOverGround = 0.0;
				//Console.WriteLine("COG/SOG {0} deg, {1} m/s", mCourseOverGround, mSpeedOverGround);
			}
			catch (Exception e)
			{
				CNXLog.Error("ParseCOGSOG", e);
				mSpeedOverGround = double.NaN;
				mCourseOverGround = double.NaN;
			}
		}

		//public void ParseCOGSOGLittleEndian(byte[] data)
		public void ParseCOGSOG(byte[] data)
		{
			//if (BitConverter.IsLittleEndian)
			//	Array.Reverse(array, 0, 4);
			try
			{
				if (!BitConverter.IsLittleEndian)
				{
					Array.Reverse(data, 2, 2);
					Array.Reverse(data, 4, 2);
				}

				double cog = BitConverter.ToUInt16(data, 2);
				cog *= 0.0001;
				if (cog > Double.Epsilon)
					mCourseOverGround = cog;
				else
					mCourseOverGround = 0.0;
				double sog = BitConverter.ToUInt16(data, 4);
				sog *= 0.01;
				if (sog > Double.Epsilon)
					mSpeedOverGround = sog;
				else
					mSpeedOverGround = 0.0;
				//Console.WriteLine("COG/SOG {0} deg, {1} m/s", mCourseOverGround, mSpeedOverGround);
			}
			catch (Exception e)
			{
				CNXLog.Error("ParseCOGSOGLittleEndian", e);
				mSpeedOverGround = double.NaN;
				mCourseOverGround = double.NaN;
			}
		}

		/// <summary>
		/// Parses the NMEA GPS positional data
		/// </summary>
		/// <param name="NMEAWordLat">The latitude word from an NMEA GPS sentence.</param>
		/// <param name="NMEALatHemisphere">The hemisphere word, north or south from an NMEA GPS sentence.</param>
		/// <param name="NMEAWordLong">The longitude word from an NMEA GPS sentence.</param>
		/// <param name="NMEALongHemisphere">The hemisphere word, east or west from an NMEA GPS sentence.</param>
		/// <returns>The GIS position.</returns>
		/// <remarks>If the data can not be parsed an invalid position is returned.</remarks>
		public static GPSPosition.Position ParseNMEAPosition(string NMEAWordLat, string NMEALatHemisphere, string NMEAWordLong, string NMEALongHemisphere)
		{
			GPSPosition.Position position = new Position();

			try
			{
				// organise the bits of the latitude
				int degrees = int.Parse(NMEAWordLat.Substring(0, 2), NMEAGPSClient.NMEACultureInfo);
				double decimalMinutes = double.Parse(NMEAWordLat.Substring(2), NMEAGPSClient.NMEACultureInfo);
				bool isNorth = NMEALatHemisphere.Equals("N", StringComparison.Ordinal);

				// convert the notation into +/- decimal degrees
				double lat = degrees + (decimalMinutes / 60.0);
				if (!isNorth)
					lat *= -1.0;

				// organise the bits of the longitude
				degrees = int.Parse(NMEAWordLong.Substring(0, 3), NMEAGPSClient.NMEACultureInfo);
				decimalMinutes = double.Parse(NMEAWordLong.Substring(3), NMEAGPSClient.NMEACultureInfo);
				bool isWest = NMEALongHemisphere.Equals("W", StringComparison.Ordinal);

				// convert the notation into +/- decimal degrees
				double lon = degrees + (decimalMinutes / 60.0);
				if (isWest)
					lon *= -1.0;

				position.Reset(lat, lon);
			}
				// any exceptions will leave us with an invalid position so take no action.
			catch (Exception) {}

			return position;
		}

		/// <summary>
		/// Parses the velocity or speed from an NMEA word.
		/// </summary>
		/// <param name="NMEAWord">NMEA velocity word from an NMEA GPS sentence.</param>
		/// <returns>The velocity in meters/second.</returns>
		/// <remarks>
		/// The NMEA GPS velocity is in Knots/Hour.
		/// The one Knot is equal to 1,852 meters.
		/// NaN will be returned if the velocity is unknown or cant be parsed.
		/// </remarks>
		public static double ParseVelocity(string NMEAWord)
		{
			if (!Double.TryParse(NMEAWord, out double velocity))
				return double.NaN;

			if (velocity > Double.Epsilon)
				//velocity = (velocity / 1852) * 360.0;
				velocity *= 0.51444444;
			else
				velocity = 0.0;

			return velocity;
		}
	}
}
