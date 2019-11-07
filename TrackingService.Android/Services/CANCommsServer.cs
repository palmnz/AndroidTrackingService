using System;
using System.Threading;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using NMEAGPSClient;
using CANLib;

namespace Tracking.Services
{
	/// <summary>
	/// Equipment event notification class.
	/// </summary>
	public class EquipmentChangedEventArgs : EventArgs
	{
		public EquipmentChangedEventArgs(DeviceCatalogueInfo.EquipmentFlages mask, DeviceCatalogueInfo.EquipmentFlages status)
		{
			mMask = mask;
			mStatus = status;
		}
		private DeviceCatalogueInfo.EquipmentFlages mMask;
		public DeviceCatalogueInfo.EquipmentFlages Mask { get { return mMask; } }
		private DeviceCatalogueInfo.EquipmentFlages mStatus;
		public DeviceCatalogueInfo.EquipmentFlages Status { get { return mStatus; } }
	}

	public class CANCommsServer
	{
		public const int CatalogueTime = 2 * 60 * 1000;
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif
        private CANLib.CANClient mCANClient;
		private NMEAGPSClient.NMEAGPSClient mGpsClient;
		private NMEAGPSClient.NMEAGPSClient.GPS_STATUS mGPSStatus;
		private Timer mCatalogueTimer;
		private DeviceCatalogue mDeviceCatalogue;
		//private List<DeviceCatalogueInfo> mDeviceCatalogue = new List<DeviceCatalogueInfo>(2);
		/// <summary>
		/// Gets the device cataloge.
		/// </summary>
		public DeviceCatalogue DeviceCatalogue
		{
			get { return mDeviceCatalogue; }
		}

		/// <summary>
		/// Gets the amalgamated equipment mask
		/// </summary>
		public DeviceCatalogueInfo.EquipmentFlages EquipmentMask { get { return mDeviceCatalogue.EquipmentMask; } }
		/// <summary>
		/// Gets the amalgamated equipment status
		/// </summary>
		public DeviceCatalogueInfo.EquipmentFlages EquipmentStatus { get { return mDeviceCatalogue.EquipmentStatus; } }
		private byte mRoutePatternVersion = 0;
		/// <summary>
		/// Gets the vehicle Route Pattern Version
		/// </summary>
		public byte RoutePatternVersion { get { return mRoutePatternVersion; } }
		private byte mVehicleConfigVersion = 0;
		/// <summary>
		/// Gets the vehicle configuration Version
		/// </summary>
		public byte VehicleConfigVersion { get { return mVehicleConfigVersion; } }
		/// <summary>
		/// Subscribe to the cataloge events.
		/// </summary>
		public event EventHandler<EquipmentChangedEventArgs> RaiseEquipmentChangedEvent;

		private const string mCatalogueFilename = "device-cataloge";

		public CANCommsServer(CANClient client, NMEAGPSClient.NMEAGPSClient gpsClient)
		{
			mCANClient = client;
			mGpsClient = gpsClient;

            // make a device catalogue
            mDeviceCatalogue = CreateCatalogue(TrackingService.VarPath + mCatalogueFilename);

            // start time for cataloge and status reporting
            mCatalogueTimer = new Timer(OnTimedEvent, null, CatalogueTime, CatalogueTime);

            // subscribe to gps events
            mGpsClient.RaiseGPSPositionChangedEvent += GPSPositionChangedEventHandler;
			// get the current gps status
			mGPSStatus = mGpsClient.Status;
			mGpsClient.RaiseGPSStatusChangedEvent += GPSStatusChangedEventHandler;

			// subscribe to CAN frame events
			mCANClient.RaiseFrameReceivedEvent += FrameReceivedEventHandler;
		}

		private static DeviceCatalogue CreateCatalogue(string filename)
		{
			DeviceCatalogue catalogue = null;

			//try
			//{

			//	using (Stream stream = new FileStream(filename, FileMode.Open, FileAccess.Read, FileShare.None))
			//	{
			//		// load existing catalogue
			//		try
			//		{
			//			IFormatter formatter = new BinaryFormatter();
			//			catalogue = (DeviceCatalogue)formatter.Deserialize(stream);

			//			//System.Xml.Serialization.XmlSerializer x = new System.Xml.Serialization.XmlSerializer(typeof(DeviceCatalogue), new Type[] { typeof(DeviceCatalogueInfo) });
			//			//catalogue = (DeviceCatalogue)x.Deserialize(stream);

			//			catalogue.ResetStatus();
			//			CNXLog.WarnFormat("CreateCatalogue : from ({0}), {1}.", filename, catalogue.ToString());
			//		}
			//		catch (FileNotFoundException)
			//		{
			//			CNXLog.WarnFormat("CreateCatalogue : Could not open catalogue ({0}), Creating a new one.", filename);
			//		}
			//	}
			//}
			//catch (Exception e)
			//{
			//	CNXLog.Error("CreateCatalogue", e);
			//}

			// may have a failure or no persitant catalogue
			if (catalogue == null)
				catalogue = new DeviceCatalogue();

			return catalogue;
		}

		public void PersistCatalogue()
		{
			PersistCatalogue(mDeviceCatalogue, TrackingService.VarPath + mCatalogueFilename);
		}

		public static void PersistCatalogue(DeviceCatalogue catalogue, string filename)
		{
			lock (catalogue)
			{
				//try
				//{
				//	using (Stream stream = new FileStream(filename, FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
				//	{
				//		try
				//		{
				//			if (catalogue.Catalogue.Count > 0)
				//			{
				//				IFormatter formatter = new BinaryFormatter();
				//				formatter.Serialize(stream, catalogue);
				//				CNXLog.WarnFormat("Persisted Catalogue {0}.", catalogue.ToString());
				//			}
				//		}
				//		catch (Exception ee)
				//		{
				//			CNXLog.Error("PersistCatalogue", ee);
				//		}
				//	}
				//}
				//catch (Exception e)
				//{
				//	CNXLog.Error("PersistCatalogue", e);
				//}
			}
		}

		private const int TestCatalogueCycles = 10;
		private int mTestCatalogueCount = 0;
		private void OnTimedEvent(object sourcea)
		{
			CANFrame frame = new CANFrame();

			try
			{
				// now config Block Requests
				frame.MailboxId = (uint)CNXMsgIds.BlockQuery + (uint)Block.RouteConfig;
				mCANClient.Send(frame);
				frame.MailboxId = (uint)CNXMsgIds.BlockQuery + (uint)Block.VehicleConfig;
				mCANClient.Send(frame);
				frame.MailboxId = (uint)CNXMsgIds.BlockQuery + (uint)Block.ServiceAlert;
				mCANClient.Send(frame);
				frame.MailboxId = (uint)CNXMsgIds.BlockQuery + (uint)Block.DriverConfig;
				mCANClient.Send(frame);

				// test the status every few cycles
				if (++mTestCatalogueCount > TestCatalogueCycles)
				{
					// age the device catalogue states
					if (mDeviceCatalogue.AgeCatalogue() == CANLib.DeviceCatalogue.CatalogueChangeType.STATUS)
					{
						EquipmentChangedEventArgs args = new EquipmentChangedEventArgs(mDeviceCatalogue.EquipmentMask, mDeviceCatalogue.EquipmentStatus);
						OnRaiseEquipmentChangedEvent(args);
					}
					mTestCatalogueCount = 0;
				}
			}
			catch (Exception e)
			{
				CNXLog.Error(e.ToString());
			}
		}

		public void ForceEquipmentChangedEvent()
		{
			EquipmentChangedEventArgs args = new EquipmentChangedEventArgs(mDeviceCatalogue.EquipmentMask, mDeviceCatalogue.EquipmentStatus);
			OnRaiseEquipmentChangedEvent(args);
		}

		private void GPSPositionChangedEventHandler(object sender, EventArgs e)
		{
			try
			{
                NMEAGPSClient.NMEAGPSClient.GPSPositionEvent gpsPosition = (NMEAGPSClient.NMEAGPSClient.GPSPositionEvent)e;
				GPSPosition position = gpsPosition.Position;
				NMEAGPSClient.NMEAGPSClient nmeaClient = (NMEAGPSClient.NMEAGPSClient)sender;
				// make sure we have valid data
                byte gpsState = (byte)position.NMEAMode;
                if (position.HorizontalErrorEstimate > TrackingService.DefaultAcceptableError)
                {
                    gpsState &= 0xfe;
                }
					
                // publish the new position to CAN clients.
                CANFrame frame = CNXCANMsgHelper.PackGPSFrame(gpsState, position.CurrentPosition.Latitude, position.CurrentPosition.Longitude, position.SpeedOverGround);
				int count = mCANClient.Send(frame);
				
                if ((gpsState == (byte)CANGPSState.GoodFix) || (gpsState == (byte)CANGPSState.PoorFix))
                {
                    // publish the new time.
                    frame = CNXCANMsgHelper.PackDateTime(position.TimeStamp);
                    count = mCANClient.Send(frame);
                }

                CNXLog.Debug("GPSPositionChangedEventHandler");
            }
			catch (Exception ex)
			{
				CNXLog.Error("CAN GPSPositionChangedEventHandler", ex);
			}
		}

		private void GPSStatusChangedEventHandler(object sender, EventArgs e)
		{
			NMEAGPSClient.NMEAGPSClient.GPSStatusEvent gpsStatus = (NMEAGPSClient.NMEAGPSClient.GPSStatusEvent)e;
			mGPSStatus = gpsStatus.Status;
			if (mGPSStatus < NMEAGPSClient.NMEAGPSClient.GPS_STATUS.Fix)
			{
				GPSPosition.Position position = ((NMEAGPSClient.NMEAGPSClient)sender).Position;
				// publish the new position to CAN clients.
				CANFrame frame = CNXCANMsgHelper.PackGPSFrame((byte)mGPSStatus, position.Latitude, position.Longitude, 0);
				int count = mCANClient.Send(frame);
			}

            CNXLog.Debug("GPSStatusChangedEventHandler");
        }

		private void FrameReceivedEventHandler(object o, FrameReceivedEventArgs a)
		{
			try
			{
				switch (a.Frame.MailboxId)
				{
					case (uint)CNXMsgIds.ProductId:
						CNXLog.InfoFormat("DeviceCataloge {0}", a.Frame.ToString());
						ThreadPool.QueueUserWorkItem(new WaitCallback(UpdateDeviceCatalogue), a.Frame);
						break;
					case (uint)CNXMsgIds.Fareset:
						CNXLog.InfoFormat("Frame detected Fareset {0}.", a.Frame.Data[0]);
						break;
					case (uint)CNXMsgIds.Destination:
						CNXLog.InfoFormat("Frame detected Destination {0}.", CNXCANMsgHelper.UnpackDestinationFrameData(a.Frame.Data));
						break;
					case (uint)CNXMsgIds.RouteTrip:
						string route = string.Empty;
						int? trip = null;
						CNXCANMsgHelper.UnpackFareboxData(a.Frame.Data, out route, out trip);
						CNXLog.InfoFormat("Frame detected Farebox route code {0} trip No {0}.", route, trip);
						break;
					default:
						break;
				}

                CNXLog.Debug("FrameReceivedEventHandler");
            }
			catch (Exception e)
			{
				CNXLog.ErrorFormat("CANCommsServer Frame Rx {0}.", e.ToString());
			}
		}

		private void UpdateDeviceCatalogue(object stateInfo)
		{
			CANFrame frame = (CANFrame)stateInfo;

			DeviceCatalogueInfo dci = frame.ToDeviceCatalogueInfo();
			if (dci == null)
				return;

			CNXLog.InfoFormat("DeviceCataloge {0}", dci.ToString());
			DeviceCatalogue.CatalogueChangeType catalogeUpdated = mDeviceCatalogue.UpdateDeviceCatalogue(dci);
			if (catalogeUpdated != DeviceCatalogue.CatalogueChangeType.NONE)
			{
				CNXLog.InfoFormat("DeviceCataloge Updating calalogue.");
				if (catalogeUpdated == DeviceCatalogue.CatalogueChangeType.EQUIPMENT)
					// persist the catalogue
					PersistCatalogue();

				EquipmentChangedEventArgs a = new EquipmentChangedEventArgs(mDeviceCatalogue.EquipmentMask, mDeviceCatalogue.EquipmentStatus);
				OnRaiseEquipmentChangedEvent(a);
			}
		}

		/// <summary>
		/// Clears all entries in the catalogue.
		/// Used when equipment is changed on the vehicle.
		/// </summary>
		public void ClearDeviceCatalogue()
		{
			//ThreadPool.QueueUserWorkItem((o) =>
			//{
				// clear the catalogue
				mDeviceCatalogue.Clear();
				// persist the catalogue
				PersistCatalogue();
			//}
			//);
		}

		/// <summary>
		/// Event firing method.
		/// </summary>
		/// <param name="frame">Equipment Mask/Status.</param>
		protected virtual void OnRaiseEquipmentChangedEvent(EquipmentChangedEventArgs catalogeEvent)
		{
			ThreadPool.QueueUserWorkItem((o) =>
			{
				// copy the event handler to avoid mid process subscribe/un-subscribe
				RaiseEquipmentChangedEvent?.Invoke(this, catalogeEvent);
			}
			);
		}

		public void Close()
		{
			mGpsClient.RaiseGPSPositionChangedEvent -= GPSPositionChangedEventHandler;
			mGpsClient.RaiseGPSStatusChangedEvent -= GPSStatusChangedEventHandler;
			mCANClient.RaiseFrameReceivedEvent -= FrameReceivedEventHandler;
		}
	}
}
