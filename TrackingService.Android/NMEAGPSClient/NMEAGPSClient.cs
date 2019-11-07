using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Threading;

using Tracking.Services;

namespace NMEAGPSClient
{
	public interface IHysteresis
	{
		/// <summary>
		/// Gets the damped value or sets a new sample.
		/// </summary>
		double Value { get; set; }
	}

	/// <summary>
	/// Provides simple damping hysteresis
	/// </summary>
	public class SimpleHysteresis : IHysteresis
	{
		private double mDampedValue = 0;

		private double mDampling = 3.0;
		/// <summary>
		/// The damping factor applied to the values.
		/// </summary>
		public double Damping
		{
			get { return mDampling; }
			set { mDampling = value; }
		}

		/// <summary>
		/// Gets the average value or sets a new sample.
		/// </summary>
		public double Value
		{
			get { return mDampedValue; }
			set { mDampedValue = (mDampedValue + value) / mDampling; }
		}
	}

	/// <summary>
	/// Provides avveraging hysteresis over a number of samples.
	/// </summary>
	public class AveragingHysteresis : IHysteresis
	{
		private double[] mSamples;
		private int mIndex;

		/// <summary>
		/// Default number of samples to use in the averaging calculation.
		/// </summary>
		public const int DefaultAverageHysteresisCount = 4;

		/// <summary>
		/// Number of samples to use in the averaging calculation.
		/// </summary>
		public int SampleSize
		{
			get { return mSamples.Length; }
			set
			{
				// create new sample array
				double[] samples = new double[value];
				// initailise all elements with the average value.
				double val = Value;
				for (int i = 0; i < samples.Length; ++i)
					samples[i] = val;
				mSamples = samples;
				mIndex = 0;
			}
		}

		/// <summary>
		/// Gets the average value or sets a new sample.
		/// </summary>
		public double Value
		{
			get
			{
				double average = 0.0;
				foreach (double val in mSamples)
					average += val;
				return average / mSamples.Length;
			}
			set
			{
				mSamples[mIndex] = value;
				if (++mIndex == mSamples.Length)
					mIndex = 0;
			}
		}

		/// <summary>
		/// Initaialises a new AveragingHysteresis
		/// </summary>
		/// <remarks>The default sample size is used.</remarks>
		public AveragingHysteresis()
		{
			mSamples = new double[DefaultAverageHysteresisCount];
		}

		/// <summary>
		/// Initaialises a new AveragingHysteresis
		/// </summary>
		/// <param name="sampleSize"></param>
		public AveragingHysteresis(int sampleSize)
		{
			mSamples = new double[sampleSize];
		}
	}

	/// <summary>
	/// Listens to the output of a serial connected GPS and diceminates its output.
	/// </summary>
	public class NMEAGPSClient : EventArgs, IDisposable
	{
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif
        //		private System.Object mPositionLock = new System.Object();
        protected System.Object mSatelliteLock = new System.Object();
		protected string[] mPort;


		/// <summary>
		/// Event class containing the GPSPosition for the event.
		/// </summary>
		public class GPSPositionEvent : EventArgs
		{
			public GPSPositionEvent(GPSPosition position)
			{
				Position = position;
			}

			public GPSPosition Position;
		}

		/// <summary>
		/// Event class containing the GPSSatellite for the event.
		/// </summary>
		public class GPSSatelliteEvent : EventArgs
		{
			public GPSSatelliteEvent(GPSSatellite satellite)
			{
				Satellite = satellite;
			}

			public GPSSatellite Satellite;
		}

		/// <summary>
		/// Event class containing the GPSStatus for the event.
		/// </summary>
		public class GPSStatusEvent : EventArgs
		{
			public GPSStatusEvent(GPS_STATUS status)
			{
				Status = status;
			}

			public GPS_STATUS Status;
		}

		/// <summary>
		/// Raised when the GPS position has been updated.
		/// </summary>
		public event EventHandler RaiseGPSPositionChangedEvent;
		/// <summary>
		/// Raised when the GPS Satellite has been updated.
		/// </summary>
		public event EventHandler RaiseGPSSatelliteChangedEvent;
		/// <summary>
		/// Raised when the GPS status has changed.
		/// </summary>
		public event EventHandler RaiseGPSStatusChangedEvent;

		/// <summary>
		/// Represents the culture used to process all NMEA GPS data, including numbers and dates.
		/// </summary>
		public static readonly CultureInfo NMEACultureInfo = CultureInfo.InvariantCulture;

		/// <summary>
		/// The default timeout in milliseconds for recieving messages from the GPS
		/// </summary>
		public const int DefaultRxTimeout = 3000;

		protected int mRxTimeout = DefaultRxTimeout;
		/// <summary>
		/// The amount of time in milliseconds to wait for messages from GPSD before taking recovery action.
		/// A value of 0 or -1 indicates an infinite time-out period.
		/// </summary>
		protected int RxTimeout
		{
			get { return mRxTimeout; }
			set { mRxTimeout = value; }
		}

		protected GPSPosition mGPSPosition = new GPSPosition();
		/// <summary>
		/// Gets the complete GPS positional information
		/// </summary>
		public GPSPosition PositionInformation
		{
			get
			{
				//lock (mPositionLock)
				//{
					return mGPSPosition;
				//}
			}
		}

		/// <summary>
		/// Gets the current GPS location
		/// </summary>
		public GPSPosition.Position Position
		{
			get
			{
				//lock (mPositionLock)
				//{
					return mGPSPosition.CurrentPosition;
				//}
			}
		}

		protected GPSSatellite mGPSSatellite = new GPSSatellite();
		/// <summary>
		/// Gets the GPS Satellite constalation information
		/// </summary>
		public GPSSatellite SatelliteInformation
		{
			get
			{
				lock (mSatelliteLock)
				{
					return mGPSSatellite;
				}
			}
		}

		protected string mGPS = String.Empty;
		/// <summary>
		/// Gets the GPS idetification string.
		/// </summary>
		public string GPS
		{
			get { return mGPS; }
		}

		[Flags]
		/// <summary>
		/// Status of the GPSClient.
		/// </summary>
		public enum GPS_STATUS : byte
		{
			/// <summary>
			/// Offline not connected to GPS, no valid data.
			/// </summary>
			OFFLINE = 0,
			/// <summary>
			/// Connected to GPS.
			/// </summary>
			Connected = 1,
			/// <summary>
			/// Valid fix reported.
			/// </summary>
			Fix = 2,
			/// <summary>
			/// GPS Activity.
			/// </summary>
			GPSResponding = 4,
		}

		protected GPS_STATUS mStatus = GPS_STATUS.OFFLINE;
		/// <summary>
		/// Gets the current status of the GPSClient.
		/// </summary>
		public GPS_STATUS Status
		{
			get { return mStatus; }
		}

		/// <summary>
		/// Default threshold velocity that is considered 'Moving' in meters/second. Velocities less than this value are considered staionary.
		/// </summary>
		public const double DefaultMovingThreshold = 0.75;

		protected double mMovingThreshold = DefaultMovingThreshold;
		/// <summary>
		/// Threshold velocity that is considered 'Moving' in meters/second. Velocities less than this value are considered staionary.
		/// </summary>
		public double MovingThreshold
		{
			get { return mMovingThreshold; }
			set { mMovingThreshold = value; }
		}

		/// <summary>
		/// Enumeration giving the travelling state.
		/// </summary>
		public enum MovingState
		{
			/// <summary>
			/// The state is unknown normally because there isnt any GPS information.
			/// </summary>
			Unknown,
			/// <summary>
			/// The horizontal velocity is greater than the moving threshold.
			/// </summary>
			Moving,
			/// <summary>
			/// The horizontal velocity is below the moving threshold.
			/// </summary>
			Stationary,
		}

		protected MovingState mTravellingState = MovingState.Unknown;
		public MovingState TravellingState
		{
			get { return mTravellingState; }
		}

		protected IHysteresis mAverageVelocity = new SimpleHysteresis();
		/// <summary>
		/// Average Speed in meters/sec.
		/// </summary>
		/// <remarks>NaN will be returned if there is no valid value.</remarks>
		public double AverageVelocity
		{
			get { return mAverageVelocity.Value; }
		}

		protected virtual void OnRaiseGPSPositionChangedEvent(GPSPositionEvent e)
		{
			RaiseGPSPositionChangedEvent?.Invoke(this, e);
		}

		protected virtual void OnRaiseGPSSatelliteChangedEvent(GPSSatelliteEvent e)
		{
			RaiseGPSSatelliteChangedEvent?.Invoke(this, e);
		}

		protected virtual void OnRaiseGPSStatusChangedEvent(GPSStatusEvent e)
		{
			RaiseGPSStatusChangedEvent?.Invoke(this, e);
		}

		protected void ProcessGPSTimeout()
		{
			mStatus = GPS_STATUS.OFFLINE;
			mGPSPosition.Invalidate();
			OnRaiseGPSStatusChangedEvent(new GPSStatusEvent(mStatus));
		}

		#region IDisposable Support
		protected bool mDisposedValue = false; // To detect redundant calls

		protected virtual void Dispose(bool disposing)
		{
			if (!mDisposedValue)
			{
				if (disposing)
				{
					// TODO: dispose managed state (managed objects).
				}

				// TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
				// TODO: set large fields to null.

				mDisposedValue = true;
			}
		}

		// TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
		// ~NMEAGPSClient() {
		//   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
		//   Dispose(false);
		// }

		// This code added to correctly implement the disposable pattern.
		public void Dispose()
		{
			// Do not change this code. Put cleanup code in Dispose(bool disposing) above.
			Dispose(true);
			// TODO: uncomment the following line if the finalizer is overridden above.
			// GC.SuppressFinalize(this);
		}
		#endregion
	}
}
