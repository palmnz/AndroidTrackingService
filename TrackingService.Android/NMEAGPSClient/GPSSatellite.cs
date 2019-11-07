using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace NMEAGPSClient
{
	/// <summary>
	/// Provides satellite data.
	/// </summary>
	public struct GPSSatellite
	{
		private int mSatelliteCount;
		/// <summary>
		/// Gets the number of satellites in the current constelation.
		/// </summary>
		public int Count
		{
			get { return mSatelliteCount; }
		}

		private Satellite[] mConstelation;

		/// <summary>
		/// Structure containing satellite data.
		/// </summary>
		public struct Satellite
		{
			/// <summary>
			/// Satellite PRN.
			/// </summary>
			public int PRN;
			/// <summary>
			/// Satellite Elevation in whole degrees 0-90.
			/// </summary>
			public int Elevation;
			/// <summary>
			/// Satellite Azimuth in whole degrees 0-359.
			/// </summary>
			public int Azimuth;
			/// <summary>
			/// Satellite signal strength in decibels.
			/// </summary>
			public int SignalStrength;
			/// <summary>
			/// Whether this satellite was used to calculate the last position.
			/// </summary>
			public bool UsedForPosition;

			/// <summary>
			/// Parses the data from a space seperated string.
			/// </summary>
			/// <param name="satData">Data formated as 'PRN Elevation Azimuth SignalStrength UsedForPosition'.</param>
			public void Parse(string satData)
			{
				Char[] spaceSplit = { ' ' };

				// break up individual satellite fields
				string[] satFields = satData.Split(spaceSplit, StringSplitOptions.RemoveEmptyEntries);
				if (satFields.Length == 5)
				{
					PRN = Int32.Parse(satFields[0]);
					Elevation = Int32.Parse(satFields[1]);
					Azimuth = Int32.Parse(satFields[2]);
					SignalStrength = Int32.Parse(satFields[3]);
					UsedForPosition = (satFields[4] == "1");
				}
				else
					// incomplete data set
					InvalidateData();
			}

			private void InvalidateData()
			{
				PRN = -1;
				Elevation = -1;
				Azimuth = -1;
				SignalStrength = -1;
				UsedForPosition = false;
			}
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

		private double mTimeStamp;
		/// <summary>
		/// Seconds since the Unix epoch, UTC. May have a fractional part of up to .01sec precision.
		/// </summary>
		/// <remarks>NaN will be returned if there is no valid time stamp.</remarks>
		public double TimeStamp
		{
			get { return mTimeStamp; }
		}

		/// <summary>
		/// Parses a GPSD 'y' report.
		/// Returns Y=, followed by a sentence tag, followed by a timestamp (seconds since the Unix epoch, UTC) and a count not more than 12,
		/// followed by that many quintuples of satellite PRNs,
		/// elevation/azimuth pairs (elevation an integer formatted as %d in range 0-90, azimuth an integer formatted as %d in range 0-359),
		/// signal strengths in decibels,
		/// and 1 or 0 according as the satellite was or was not used in the last fix.
		/// Each number is followed by one space.
		/// </summary>
		/// <param name="sentence">GPSD 'y' report.</param>
		/// <exception cref="System.Exception">Thrown if the sentence is not a GPSD report or sentence is not a 'Y' report.</exception>
		public void Parse(string sentence)
		{
			if (mConstelation == null)
				mConstelation = new Satellite[12];

			Char[] spaceSplit = { ' ' };
			Char[] colonSplit = { ':' };
			// break up the sentence into fields
			string[] fields = sentence.Split(spaceSplit);
			// validate the sentence
			if (!fields[0].StartsWith("Y="))
				throw new Exception("Invalid sentence, not an 'Y' report.");
			// make sure there is a complete report
			if (fields[0] == "Y=?")
				// need to invalidate all data
				fields = new string[] { "Y=?", "?" };

			// looks good to go
			mTag = fields[0].Substring(2);
			mTimeStamp = (fields[1] == "?") ? Double.NaN : Double.Parse(fields[1]);
			string[] satellites = sentence.Split(colonSplit, StringSplitOptions.RemoveEmptyEntries);

			// reset constelation count
			mSatelliteCount = 0;

			// first field has already been processed
			for (int i = 1; i < satellites.Length; ++i)
			{
				mConstelation[i - 1].Parse(satellites[i]);
				++mSatelliteCount;
			}
		}

		/// <summary>
		/// Parses a raw NMEA GSA (Overall Satellite data) sentence
		/// </summary>
		/// <param name="sentence">NMEA GSA Senetence.</param>
		/// <exception cref="System.Exception">Thrown if the sentence is not a GSA sentence.</exception>
		public void ParseGSA(string sentence)
		{
		}

		/// <summary>
		/// Satellite at this index.
		/// </summary>
		/// <param name="index">An index position in the current constelation</param>
		/// <returns></returns>
		public Satellite this[int index]
		{
			get
			{
				if (index < mSatelliteCount)
					return mConstelation[index];
				else
					throw new IndexOutOfRangeException("Satellite constelation");
			}
		}

		/// <summary>
		/// Gets the number of satellites used for the last position fix.
		/// </summary>
		public byte Used
		{
			get
			{
				byte count = 0;
				foreach (Satellite s in mConstelation)
					if (s.UsedForPosition)
						++count;

				return count;
			}
		}
	}
}
