using System;
using System.Collections.ObjectModel;
using System.Threading;

using NMEAGPSClient;
using CANLib;
using BlockLib;

namespace Tracking.Services
{
	/// <summary>
	/// Responcible for maintaining the tracking states and reporting to the RTT server.
	/// </summary>
	public class TrackingStateManager
	{
		public enum TrackingState
		{
			/// <summary>
			/// In an alarm state.
			/// </summary>
			ALARM,
			/// <summary>
			/// Not moving, velocity is below a threshold.
			/// </summary>
			STATIONARY,
			/// <summary>
			/// The velocity is above a threshold.
			/// </summary>
			MOVING,
			/// <summary>
			/// Not getting any accurate GPS positions.
			/// In this state there is an assumption of being STATIONARY as well.
			/// </summary>
			LOST,
		};

		public enum TrackingEvent
		{
			/// <summary>
			/// Alarm event.
			/// </summary>
			ALARM_ACTIVE,
			/// <summary>
			/// New positional information.
			/// </summary>
			POSITION_MOVING,
			/// New positional information.
			/// </summary>
			POSITION_STATIONARY,
			/// <summary>
			/// GPS is no good.
			/// </summary>
			LOST_GPS,
			/// <summary>
			/// The minimum/moving reporting rate.
			/// </summary>
			TICK,
			/// <summary>
			/// The background/stationary reporting rate.
			/// </summary>
			TOCK,
			/// <summary>
			/// Change in catalogue status or contents
			/// </summary>
			CATALOGUE,
		};

#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif

        private volatile TrackingState mTrackingState = TrackingState.LOST;
		private RTTMesg mLocationMessage = RTTMesg.CreateGPSNone(false);
		private RTTMesg mResourceMessage = RTTMesg.CreateResourceBuildMessage(255, 255, 255, 255);
		//private RTTMesg mFirmwareMessage = null;
		//private RTTMesg mEquipmentMessage = null;
		private ReadOnlyCollection<DeviceCatalogueInfo> mCatalogue = null;

		private Timer mTimer;
		private const int MinFullReportSec = 15;

        /// <summary>
        /// Number of position reports that are made between full status reports.
        /// </summary>
        public static uint BackgroundTickerCount
		{
			get
			{
				return Math.Max(TrackingService.StationaryRate, MinFullReportSec) / TrackingService.MovingRate;
			}
		}
		/// <summary>
		/// At startup allow some time to collect equipment reports before making the first full report. 
		/// </summary>
		private uint mBackgroundTicker = BackgroundTickerCount;
		private CommsServer mCommsServer;
		private volatile bool mRequiresReport = true;
		private DeviceCatalogueInfo.EquipmentFlages mDeviceMask = DeviceCatalogueInfo.EquipmentFlages.None;
		private DeviceCatalogueInfo.EquipmentFlages mDeviceStatus = DeviceCatalogueInfo.EquipmentFlages.None;
		private Object mStateLock = new Object();
		private volatile bool mBeenMoving = false;

		public TrackingStateManager(CommsServer commsServer, NMEAGPSClient.NMEAGPSClient gpsClient)
		{
			mCommsServer = commsServer;
			mNMEAClient = gpsClient;
			mNMEAClient.RaiseGPSPositionChangedEvent += this.OnGPSPositionChangedEventHandler;
			mNMEAClient.RaiseGPSStatusChangedEvent += this.OnGPSStateChangedEventHandler;
			// set the initial position
			// initialise the timer
			mTimer = new Timer(new TimerCallback(OnTimedEvent), null, TrackingService.MovingRate * 1000, TrackingService.MovingRate * 1000);
        }

		/// <summary>
		/// Deals with changes of GPS state
		/// </summary>
		/// <param name="o">Event source object.</param>
		/// <param name="e">Event Parmeters</param>
		public void OnGPSStateChangedEventHandler(object o, EventArgs e)
		{
			try
			{
				NMEAGPSClient.NMEAGPSClient.GPSStatusEvent ev = (NMEAGPSClient.NMEAGPSClient.GPSStatusEvent)e;
				// work out what needs doing
				// as far as we are concerned only the failed states are interesting
				if ((ev.Status & NMEAGPSClient.NMEAGPSClient.GPS_STATUS.Fix) == NMEAGPSClient.NMEAGPSClient.GPS_STATUS.OFFLINE)
				{
					mLocationMessage.MessageId = (ev.Status == NMEAGPSClient.NMEAGPSClient.GPS_STATUS.OFFLINE) ? RTTMesg.RTTOutMsgIds.GPSNone : RTTMesg.RTTOutMsgIds.GPSNoFix;
					StateMachine(TrackingEvent.LOST_GPS);
				}
			}
			catch (Exception ex)
			{
				CNXLog.Error("TrackerState GPS state error.", ex);
			}
        }

		/// <summary>
		/// Deals with changes of GPS position
		/// </summary>
		/// <param name="o">Event source object.</param>
		/// <param name="e">Event Parmeters</param>
		public void OnGPSPositionChangedEventHandler(object o, EventArgs e)
		{
			//mNMEAClient = (NMEAGPSClient.NMEAGPSClient)o;
			//mNMEAClient.RaiseGPSPositionChangedEvent -= this.OnGPSPositionChangedEventHandler;
			try
			{
                NMEAGPSClient.NMEAGPSClient client = (NMEAGPSClient.NMEAGPSClient)o;
				if (client.TravellingState == NMEAGPSClient.NMEAGPSClient.MovingState.Moving)
                {
                    mBeenMoving = true;
                }
					
				//GPSPosition position = ((NMEAGPSClient.NMEAGPSClient.GPSPositionEvent)e).Position;
				//// set the mew position
				//mMobileMessage.TrackingState.SetLocation(position.CurrentPosition.Latitude, position.CurrentPosition.Longitude, position.SpeedOverGround, (uint)position.HorizontalErrorEstimate);
				//TrackingEvent tEv = (client.TravellingState == NMEAGPSClient.NMEAGPSClient.MovingState.Moving) ? TrackingEvent.POSITION_MOVING : TrackingEvent.POSITION_STATIONARY;
				//mPositionEvt = tEv;
				//StateMachine(tEv);
			}
			catch (Exception ex)
			{
				CNXLog.Error("TrackerState GPS position error {0}", ex);
			}
        }

		private NMEAGPSClient.NMEAGPSClient mNMEAClient = null;

		private void UpdatePosition()
		{
			if (mNMEAClient != null)
			{
				try
				{
					if ((mNMEAClient.Status & NMEAGPSClient.NMEAGPSClient.GPS_STATUS.Fix) == NMEAGPSClient.NMEAGPSClient.GPS_STATUS.Fix)
					{
                        GPSPosition position = mNMEAClient.PositionInformation;
                        mLocationMessage.SetLocation((uint)position.HorizontalErrorEstimate, position.CurrentPosition.Latitude, position.CurrentPosition.Longitude, position.SpeedOverGround);
                        TrackingEvent tEv = (mNMEAClient.TravellingState == NMEAGPSClient.NMEAGPSClient.MovingState.Moving) ? TrackingEvent.POSITION_MOVING : TrackingEvent.POSITION_STATIONARY;
                        StateMachine(tEv);
                    }
                    else
					{
						mLocationMessage.MessageId = (mNMEAClient.Status == NMEAGPSClient.NMEAGPSClient.GPS_STATUS.OFFLINE) ? RTTMesg.RTTOutMsgIds.GPSNone : RTTMesg.RTTOutMsgIds.GPSNoFix;
						StateMachine(TrackingEvent.LOST_GPS);
						//CNXLog.WarnFormat("Setting state for No Fix {0}", mMobileMessage);(mNMEAClient.Status == NMEAGPSClient.NMEAGPSClient.GPS_STATUS.OFFLINE)
					}
				}
				catch (Exception ex)
				{
					CNXLog.Error("TrackerState GPS position error {0}", ex);
				}
            }
		}

		/// <summary>
		/// Deals with frames comming in from CAN
		/// </summary>
		/// <param name="o">Event source object.</param>
		/// <param name="a">Event Parmeters</param>
		public void FrameReceivedEventHandler(object o, FrameReceivedEventArgs a)
		{
			try
			{
				switch ((CNXMsgIds)a.Frame.MailboxId)
				{
					case CNXMsgIds.DuressState:
						if (CNXCANMsgHelper.DuressFrame(a.Frame))
						{
							CNXLog.WarnFormat("TrackerState Duress Active frame recieved.");
							mLocationMessage.Alarm = true;
							StateMachine(TrackingEvent.ALARM_ACTIVE);
						}
						break;
					default:
						break;
				}
			}
			catch (Exception e)
			{
				CNXLog.Error(string.Format("TrackerState Frame error {0}", a.Frame.ToString()), e);
			}
		}

		/// <summary>
		/// Deals with changes of GPS state
		/// </summary>
		/// <param name="o">Event source object.</param>
		/// <param name="blockState">Event Parmeters</param>
		public void OnBlockChangedEventHandler(object o, BlockTransferManager.StateChangedEventArgs blockState)
		{
			CNXLog.InfoFormat("TrackingState Block event {0}", blockState.State);
			if (mResourceMessage == null)
			{
				CNXLog.WarnFormat("TrackingState no resource message!");
				return;
			}
			try
			{
				// deal with any newly completed blocks
				if (blockState.State == BlockTransferManager.BlockTransferState.DeviceSynchronised)
				{
					// get  the Block details
					BlockTransferManager btm = (BlockTransferManager)o;
					switch ((Block)btm.BlockId)
					{
						// get new route config version
						case Block.RouteConfig:
							if (mResourceMessage.RoutePatternVersion != (byte)btm.DeviceVersion)
							{
								mResourceMessage.RoutePatternVersion = (byte)btm.DeviceVersion;
								StateMachine(TrackingEvent.CATALOGUE);
							}
							break;
						// get new vehicle config 
						case Block.VehicleConfig:
							if (mResourceMessage.VehicleConfigVersion != (byte)btm.DeviceVersion)
							{
								mResourceMessage.VehicleConfigVersion = (byte)btm.DeviceVersion;
								StateMachine(TrackingEvent.CATALOGUE);
							}
							break;
						// get new Service Alert 
						case Block.ServiceAlert:
							if (mResourceMessage.ServiceAlertVersion != (byte)btm.DeviceVersion)
							{
								mResourceMessage.ServiceAlertVersion = (byte)btm.DeviceVersion;
								StateMachine(TrackingEvent.CATALOGUE);
							}
							break;
						// get new diriver config
						case Block.DriverConfig:
							if (mResourceMessage.DriverConfigVersion != (byte)btm.DeviceVersion)
							{
								mResourceMessage.DriverConfigVersion = (byte)btm.DeviceVersion;
								StateMachine(TrackingEvent.CATALOGUE);
							}
							break;
					}
				}
			}
			catch (Exception ex)
			{
				CNXLog.Error("TrackerState Block error.", ex);
			}
		}

		public void OnEquipmentChangedEventHandler(object o, EquipmentChangedEventArgs catalogeEvent)
		{
			CNXLog.WarnFormat("TrackingState Equipment event {0} <{1}> ", catalogeEvent.Mask, catalogeEvent.Status, ((CANCommsServer)o).DeviceCatalogue.ToString());
			try
			{
				mDeviceMask = catalogeEvent.Mask;
				mDeviceStatus = catalogeEvent.Status;
				CANCommsServer canServer = (CANCommsServer)o;
				mCatalogue = canServer.DeviceCatalogue.Catalogue;
				StateMachine(TrackingEvent.CATALOGUE);
			}
			catch (Exception ex)
			{
				CNXLog.Error("TrackerState Equipment error.", ex);
			}
		}

		private void OnTimedEvent(object state)
		{
			//push the passenger loading
			//RTTMesg rttMsg = RTTMesg.CreateAPCLoading(TrackingService.PassengerLoading);
   //         TrackingService.mCommsServer.Send(rttMsg);

			// push the position state
			UpdatePosition();
			// check for background time.
			if (--mBackgroundTicker == 0)
			{
				// reset the counter
				mBackgroundTicker = BackgroundTickerCount;
				StateMachine(TrackingEvent.TOCK);
			}
			else
            {
                StateMachine(TrackingEvent.TICK);
                MakeMinimalReport();
            }
				
		}

		private volatile bool mFullReportRequired = true;

		private void MakeFullReport()
		{
			MakeMinimalReport();
			if (mFullReportRequired)
			{
				RTTMesg message;
				// set versioning and catalogue data
				if (mDeviceMask != DeviceCatalogueInfo.EquipmentFlages.None)
				{
					message = RTTMesg.CreateEquipmentMessage(mDeviceMask, mDeviceStatus);
					CNXLog.WarnFormat("Sending {0}", message);
					mCommsServer.Send(message);
				}
				if (mCatalogue != null)
				{
					message = RTTMesg.CreateFirmwareMessage(mCatalogue);
					CNXLog.WarnFormat("Sending {0}", message);
					mCommsServer.Send(message);
				}
				if (mResourceMessage != null)
				{
					if (mResourceMessage.IsResourceBuildPopulated)
					{
						CNXLog.WarnFormat("Sending {0}", mResourceMessage);
						mCommsServer.Send(mResourceMessage);
					}
					else
						CNXLog.WarnFormat("ResourceMessage not populated {0}", mResourceMessage);
				}
				else
					CNXLog.WarnFormat("No ResourceMessage to send");

				mFullReportRequired = (mDeviceMask == DeviceCatalogueInfo.EquipmentFlages.None) || (mCatalogue == null) || (!mResourceMessage.IsResourceBuildPopulated);
			}
		}

		private void MakeUnscheduledMinimalReport()
		{
			// restart the timer
			mTimer.Change(TrackingService.MovingRate * 1000, TrackingService.MovingRate * 1000);
			MakeMinimalReport();
		}

		private void MakeMinimalReport()
		{
			mCommsServer.Send(mLocationMessage);
			// clear the alarm flag after sending the message
			mLocationMessage.Alarm = false;
			mRequiresReport = false;
			mBeenMoving = false;
		}

		private void StateMachine(TrackingEvent trackingEvent)
		{
			// events are from multiple threads
			// synchronise activity
			lock (mStateLock)
			{
				try
				{
					TrackingState newState = mTrackingState;
					switch (mTrackingState)
					{
						case TrackingState.ALARM:
							switch (trackingEvent)
							{
								case TrackingEvent.ALARM_ACTIVE:
									// refresh the alarm state
									mLocationMessage.Alarm = true;
									break;
								case TrackingEvent.POSITION_MOVING:
									newState = TrackingState.MOVING;
									break;
								case TrackingEvent.POSITION_STATIONARY:
									newState = TrackingState.STATIONARY;
									break;
								case TrackingEvent.LOST_GPS:
									newState = TrackingState.LOST;
									mRequiresReport = true;
									break;
								case TrackingEvent.TICK:
									//MakeMinimalReport();
									break;
								case TrackingEvent.TOCK:
									mFullReportRequired = true;
									MakeFullReport();
									break;
								case TrackingEvent.CATALOGUE:
									mFullReportRequired = true;
									break;
								default:
									break;
							}
							break;
						case TrackingState.MOVING:
							switch (trackingEvent)
							{
								case TrackingEvent.ALARM_ACTIVE:
									// refresh the alarm state
									mLocationMessage.Alarm = true;
									newState = TrackingState.ALARM;
									MakeUnscheduledMinimalReport();
									break;
								case TrackingEvent.POSITION_MOVING:
									break;
								case TrackingEvent.POSITION_STATIONARY:
									newState = TrackingState.STATIONARY;
									break;
								case TrackingEvent.LOST_GPS:
									newState = TrackingState.LOST;
									mRequiresReport = true;
									break;
								case TrackingEvent.TICK:
									//MakeMinimalReport();
									break;
								case TrackingEvent.TOCK:
									MakeFullReport();
									break;
								case TrackingEvent.CATALOGUE:
									mFullReportRequired = true;
									break;
								default:
									break;
							}
							break;
						case TrackingState.STATIONARY:
							switch (trackingEvent)
							{
								case TrackingEvent.ALARM_ACTIVE:
									// refresh the alarm state
									mLocationMessage.Alarm = true;
									newState = TrackingState.ALARM;
									MakeUnscheduledMinimalReport();
									break;
								case TrackingEvent.POSITION_MOVING:
									newState = TrackingState.MOVING;
									mRequiresReport = true;
									break;
								case TrackingEvent.POSITION_STATIONARY:
									break;
								case TrackingEvent.LOST_GPS:
									newState = TrackingState.LOST;
									mRequiresReport = true;
									break;
								case TrackingEvent.TICK:
									//if (mRequiresReport || mBeenMoving)
										//MakeMinimalReport();
									break;
								case TrackingEvent.TOCK:
									mFullReportRequired = true;
									MakeFullReport();
									break;
								case TrackingEvent.CATALOGUE:
									mFullReportRequired = true;
									break;
								default:
									break;
							}
							break;
						case TrackingState.LOST:
							switch (trackingEvent)
							{
								case TrackingEvent.ALARM_ACTIVE:
									// refresh the alarm state
									mLocationMessage.Alarm = true;
									newState = TrackingState.ALARM;
									MakeUnscheduledMinimalReport();
									break;
								case TrackingEvent.POSITION_MOVING:
									newState = TrackingState.MOVING;
									mRequiresReport = true;
									break;
								case TrackingEvent.POSITION_STATIONARY:
									newState = TrackingState.STATIONARY;
									mRequiresReport = true;
									break;
								case TrackingEvent.LOST_GPS:
									break;
								case TrackingEvent.TICK:
									//if (mRequiresReport || mBeenMoving)
										//MakeMinimalReport();
									break;
								case TrackingEvent.TOCK:
									MakeFullReport();
									break;
								case TrackingEvent.CATALOGUE:
									mFullReportRequired = true;
									break;
								default:
									break;
							}
							break;
						default:
							break;
					}
					mTrackingState = newState;
					//CNXLog.InfoFormat("TrackingState: {0}", mTrackingState);
				}
				catch (Exception e)
				{
					CNXLog.Error("TrackingState: ", e);
				}
			}
		}

        public void ResetTimerVariable()
        {
            mTimer.Dispose();
            mTimer = null;
        }
	}
}
