using System;

using NMEAGPSClient;
using CANLib;
using rttVehicle;

namespace Tracking.Services
{
	/// <summary>
	/// Deals with all the APC events and data.
	/// </summary>
	public class APCWrapper
	{
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif
        private APCDataSet mAPCDataSet;
		private byte[] mCounts = new byte[8];
		private int mDoors = 1;

        public APCWrapper(string rttServerURL, ushort commsAddress)
		{
		    mAPCDataSet = APCDataSet.Create(commsAddress, rttServerURL);
        }

		public void OnGPSChangedEventHandler(object o, EventArgs e)
		{
			NMEAGPSClient.NMEAGPSClient gpsClient = (NMEAGPSClient.NMEAGPSClient)o;
		
			try
			{
				// work out what needs changing
				//CNXLog.InfoFormat("APCWrapper GPS {0}", gpsClient.Status);
				//CNXLog.InfoFormat("APCWrapper GPS {0} {1}", gpsClient.Status, ((gpsClient.Status & NMEAGPSClient.NMEAGPSClient.GPS_STATUS.Fix) == NMEAGPSClient.NMEAGPSClient.GPS_STATUS.Fix));
				if ((gpsClient.Status & NMEAGPSClient.NMEAGPSClient.GPS_STATUS.Fix) == NMEAGPSClient.NMEAGPSClient.GPS_STATUS.Fix)
				{
					if (gpsClient.PositionInformation.NMEAMode == GPSPosition.NMEAMODE.TWO_DIMENSION || gpsClient.PositionInformation.NMEAMode == GPSPosition.NMEAMODE.THREE_DIMENSION)
					{
						bool moving = (gpsClient.TravellingState == NMEAGPSClient.NMEAGPSClient.MovingState.Moving);
						mAPCDataSet.UpdatePosition(gpsClient.Position.Latitude, gpsClient.Position.Longitude, moving, gpsClient.PositionInformation.TimeStamp);
                    }
					else
						mAPCDataSet.NoGPS();
				}
				else
					// nothing
					mAPCDataSet.NoGPS();
			}
			catch (Exception ex)
			{
				CNXLog.Error(string.Format("APC UpdatePosition error {0}", gpsClient.ToString()), ex);
			}
		}

		public void FrameReceivedEventHandler(object o, FrameReceivedEventArgs a)
		{
			try
			{
				byte loading;
				switch ((CNXMsgIds)a.Frame.MailboxId)
				{
					case CNXMsgIds.PassengerCountEvent:
						CNXLog.Info(a.Frame.ToString());
						mAPCDataSet.AddSensorEvent(a.Frame.Data[0], a.Frame.Data[1]);
						break;
					case CNXMsgIds.ResetLoading:
						CNXLog.Info(a.Frame.ToString());
						if (a.Frame.DataLength == 8)
						{
							mAPCDataSet.FlushData(a.Frame.Data);
							CNXLog.WarnFormat("APC data flushed.");
						}
						if (mAPCDataSet.ZeroLoading())
						{
                            TrackingService.PassengerLoading = mAPCDataSet.Loading;
							CNXLog.WarnFormat("Loading Zeroed.");
						}
						break;
					case CNXMsgIds.PassengerBoardings:
						loading = mAPCDataSet.AddPassengerEvent(a.Frame.Data);
                        TrackingService.PassengerLoading = loading;
						CNXLog.WarnFormat("Boarding {0}. Frame {1}.", mAPCDataSet.Loading, a.Frame.ToString());
						break;
                    case CNXMsgIds.PassengerLoad:
                        // theres an assumption here that the counts will turn up in door order
                        int door = a.Frame.Data[0];
                        int count = ((door - 1) << 1);
                        mCounts[count++] = a.Frame.Data[1];
                        mCounts[count] = a.Frame.Data[2];
                        if (door > mDoors)
                            // dont tell APC about higher doors until lower door counts arrive.
                            mDoors = door;
                        else
                        {
                            byte[] counts = new byte[mDoors * 2];
                            Array.Copy(mCounts, counts, counts.Length);
                            loading = mAPCDataSet.AddPassengerEvent(counts);
                            TrackingService.PassengerLoading = loading;
                            CNXLog.WarnFormat("Loading {0}. Frame {1}.", mAPCDataSet.Loading, a.Frame.ToString());
                        }
                        break;
					default:
						break;
				}
			}
			catch (Exception e)
			{
				CNXLog.Error(string.Format("APC Frame error {0}", a.Frame.ToString()), e);
			}
		}
	}
}
