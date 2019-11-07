using System;

namespace NMEAGPSClient
{
	public partial struct GPSPosition
	{
		/// <summary>
		/// WGS84 Position.
		/// </summary>
		/// <remarks>Invalid positions are indicated by NaN values of Latitude/Longitude.</remarks>
		public struct Position
		{
			private double mLat;
			private double mLong;

			/// <summary>
			/// Initialises a new position
			/// </summary>
			/// <param name="latitude">Latitude in decimal degrees</param>
			/// <param name="longitude">Longitude in decimal degrees</param>
			public Position(double latitude, double longitude)
			{
				mValid = Validate(latitude, longitude);
				if (mValid)
				{
					mLat = latitude;
					mLong = longitude;
				}
				else
				{
					mLat = Double.NaN;
					mLong = Double.NaN;
				}
			}

			/// <summary>
			/// Changes the position that this object represents
			/// </summary>
			/// <param name="latitude">New Latitude in decimal degrees</param>
			/// <param name="longitude">New Longitude in decimal degrees</param>
			public void Reset(double latitude, double longitude)
			{
				mValid = Validate(latitude, longitude);
				if (mValid)
				{
					mLat = latitude;
					mLong = longitude;
				}
				else
				{
					mLat = Double.NaN;
					mLong = Double.NaN;
				}
			}

			/// <summary>
			/// Latitude in decimal degrees
			/// </summary>
			/// <remarks>Invalid positions are indicated by NaN values of Latitude/Longitude.</remarks>
			public double Latitude
			{
				get { return mLat; }
			}

			/// <summary>
			/// Longitude in decimal degrees
			/// </summary>
			/// <remarks>Invalid positions are indicated by NaN values of Latitude/Longitude.</remarks>
			public double Longitude
			{
				get { return mLong; }
			}

			/// <summary>
			/// Provides the string representation of the position represented by this object.
			/// </summary>
			/// <returns>Latitude, Longitude in decimal degrees.</returns>
			public override string ToString()
			{
				 return ToString(false);
			}

			/// <summary>
			/// Provides the string representation of the position represented by this object.
			/// </summary>
			/// <param name="northingWesting">
			/// If true, formats the position in Northing/Westing notation.
			/// Otherwise formats the position as +- from the meridian.
			/// </param>
			/// <returns>Latitude, Longitude in decimal degrees.</returns>
			public string ToString(bool northingWesting)
			{
				if (!IsValid)
					return "Invalid Position";

				if (northingWesting)
					return String.Format("{0:F}{1}, {2}{3:F}", Math.Abs(mLat), (mLat < 0)?"S":"N", Math.Abs(mLong), (mLong < 0)?"E":"W");
				else 
					return String.Format("{0}, {1}", mLat, mLong);
			}

			private bool mValid;
			/// <summary>
			/// Indicates whether this position is valid.
			/// </summary>
			public bool IsValid
			{
				get { return mValid; }
			}

			private static bool Validate(double latitude, double longitude)
			{
				if (latitude == double.NaN)
					return false;
				if (latitude > 180.0 || latitude < -180.0)
					return false;
				if (longitude == double.NaN)
					return false;
				if (longitude > 180.0 || longitude < -180.0)
					return false;

				return true;
			}
		}
	}
}
