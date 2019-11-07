using System;
using Android.App;
using Android.Util;
using Android.Content;
using Android.OS;
using Android.Locations;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using System.Globalization;
using NMEAGPSClient;
using CANLib;
using BlockLib;
using WiFiHotSpotManager;
using Android.Net.Wifi;
using System.Linq;
using System.Text.RegularExpressions;
using Com.Connexionz.Portforwarder;
using System.Threading.Tasks;


namespace Tracking.Services
{
    [Service]
	public class TrackingService : Service, ILocationListener, GpsStatus.INmeaListener
    {
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
        public static log4droid ServerMsgLog = new log4droid() { Logger = "CNXServerMessage" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif

        private ushort mCommsAddress = 0;
        private int mTxPort = 2947;
        private int mRxPort = 2947;
        private string mRTTServer = null;
        private string mCANServerName = "can0";
        private int mInPort = 2860;
        private int mOutPort = 2861;
        private bool mUseCANBridge = false;
        private string mGPSPort = "COM1";
        private bool mRequiresAP = true;
        private string mAllowedIP = "192.168.30.121";
        private int mBits = 32;
        private string mAllowedIP2 = "192.168.30.122";
        private int mBits2 = 24;
        private string mAllowedIP3 = "192.168.30.123";
        private int mBits3 = 16;
        private string mAllowedIP4 = "192.168.30.124";
        private int mBits4 = 8;
        private string mNi = "eth0";
        private int mFrom = 8181;
        private string mTarget = "Target 1";
        private int mTo = 8181;
        private string mNi2 = "eth0";
        private int mFrom2 = 2222;
        private string mTarget2 = "Target 2";
        private int mTo2 = 2222;
        private string mNi3 = "eth0";
        private int mFrom3 = 3333;
        private string mTarget3 = "Target 3";
        private int mTo3 = 3333;
        private int mInterval = 5;

        TcpForwarderEntry tcpForwarderEntry;
        WifiApManager wifiApManager;

        private NMEAGPSClient.NMEAGPSClient mGPSClient = null;

        private System.Threading.Timer  mHouseKeepingTimer;

		public const uint DefaultMovingRate = 10;
		public const uint DefaultStationaryRate = 3 * 60;
		public const int DefaultMovingHysteresis = 4;
		public const double DefaultMovingThreshold = 0.75;
		public const int DefaultMaxUptime = (18 * 60 * 60);

        public const string VehicleRouteReasourcePath = "/rtt/realtime/resource/RoutePatternForVehicle.zip";
        public const string VehicleConfigPath = "/rtt/realtime/resource/VehicleConfig.zip";
        public const string ServiceAlertPath = "/rtt/realtime/resource/ServiceAlertForVehicle.zip";
        public const string DriverConfigPath = "/rtt/realtime/resource/DriverConfig.zip";
        public const string FirmwareExtension = ".bin";
        public const string FirmwarePath = "/rtt/realtime/resource/";

        private static string mGatewayRebootProcess = @"sshpass";
	
		public static string GatewayRebootProcess
		{
			get { return mGatewayRebootProcess; }
			set { mGatewayRebootProcess = value; }
		}

		private static string mGatewayRebootArgs = @"-p cnxw1f1 ssh -p220 -o StrictHostKeyChecking=no admin@192.168.100.1 reboot";
		/// <summary>
		/// Process arguments to involke to restart the communications gateway.
		/// </summary>
		public static string GatewayRebootArgs
		{
			get { return mGatewayRebootArgs; }
			set { mGatewayRebootArgs = value; }
		}

		private static double mMovingThreshold = DefaultMovingThreshold;
		/// <summary>
		/// Threshold velocity that is considered 'Moving' in meters/second. Velocities less than this value are considered staionary.
		/// </summary>
		public static double MovingThreshold
		{
			get { return mMovingThreshold; }
			set { mMovingThreshold = value; }
		}

		private static uint mStationaryRate = DefaultStationaryRate;
		/// <summary>
		/// Rate that position updates should be sent to the RTT server when stationary in seconds.
		/// </summary>
		public static uint StationaryRate
		{
			get { return mStationaryRate; }
			set { mStationaryRate = value; }
		}

		private static uint mMovingRate = DefaultMovingRate;
		/// <summary>
		/// Rate that position updates should be sent to the RTT server when moving in seconds.
		/// </summary>
		public static uint MovingRate
		{
			get { return mMovingRate; }
			set { mMovingRate = value; }
		}
        private static int mProductCode = 7;
        public static int ProductCode
        {
            get { return mProductCode; }
        }

        private static int mVersion = 0;
        public static int Version
        {
            get { return mVersion; }
        }

        private static volatile byte mPassengerLoading = 0;
        public static byte PassengerLoading { get { return mPassengerLoading; } set { mPassengerLoading = value; } }
 
        private string mLastNMEASentenceType = string.Empty;

        /// <summary>
        /// Process to involke to restart the communications gateway.
        /// </summary>

        public static bool trackingServiceStarted;
        public static volatile CommsServer mCommsServer;
        private CANClient mCANClient;
        private CANCommsServer mCANServer;
        private BlockTransferManager mVehicleConfigManager = null;
        private BlockTransferManager mRoutePatternManager = null;
        private BlockTransferManager mServiceAlertManager = null;
        private BlockTransferManager mDriverConfigManager = null;
        private List<BlockTransferManager> mFirmwareList = new List<BlockTransferManager>();
        private BootstrapUpgrader mBootstrapUpgrader;
        private TrackingStateManager mTrackingManager;
        private TransientBlockReciever mDriverIdReciever = null;
        private TransientBlockReciever mTxTMessageReciever = null;
        private static ManualResetEvent mQuitEvent = new ManualResetEvent(false);
        private APCWrapper mApc = null;
        private int mHouseKeepingTimeout;

        public const uint DefaultAcceptableError = 50;
 
        private static string[] mSettings;
        public static string VarPath
        {
            get { return mSettings[10]; }
        }

        public static string myIp = String.Empty;

        public TrackingService() 
		{
		}

		// Set our location manager as the system location service
		protected LocationManager LocMgr = Android.App.Application.Context.GetSystemService ("location") as LocationManager;

        readonly string logTag = "TrackingService";
		IBinder binder;

		public override void OnCreate ()
		{
			base.OnCreate ();
			Log.Debug (logTag, "OnCreate called in the Location Service");

            mSettings = SettingsActivity.GetDefaultString().Split(',');

 
            //set up the properties of TrackingService instance
            mRTTServer = mSettings[0];
            mTxPort = Convert.ToInt16(mSettings[1]);
            mRxPort = Convert.ToInt16(mSettings[2]);
            mCANServerName = mSettings[3];
            mUseCANBridge = Convert.ToBoolean(mSettings[4]); //? can Convert.ToBoolean be able convert the "false" to false?
            mInPort = Convert.ToInt16(mSettings[5]);
            mOutPort = Convert.ToInt16(mSettings[6]);
            mCommsAddress = Convert.ToUInt16(mSettings[7]);
            mStationaryRate = Convert.ToUInt16(mSettings[8]);
            mMovingThreshold = Convert.ToDouble(mSettings[9]);
            mMovingRate = Convert.ToUInt16(mSettings[10]);
            mRequiresAP = Convert.ToBoolean(mSettings[15]);
            mAllowedIP = mSettings[16];
            mBits = Convert.ToUInt16(mSettings[17]);
            mAllowedIP2 = mSettings[18];
            mBits2 = Convert.ToUInt16(mSettings[19]);
            mAllowedIP3 = mSettings[20];
            mBits3 = Convert.ToUInt16(mSettings[21]);
            mAllowedIP4 = mSettings[22];
            mBits4 = Convert.ToUInt16(mSettings[23]);
            mNi = mSettings[24];
            mFrom = Convert.ToUInt16(mSettings[25]);
            mTarget = mSettings[26];
            mTo = Convert.ToUInt16(mSettings[27]);
            mNi2 = mSettings[28];
            mFrom2 = Convert.ToUInt16(mSettings[29]);
            mTarget2 = mSettings[30];
            mTo2 = Convert.ToUInt16(mSettings[31]);
            mNi3 = mSettings[32];
            mFrom3 = Convert.ToUInt16(mSettings[33]);
            mTarget3 = mSettings[34];
            mTo3 = Convert.ToUInt16(mSettings[35]);
            mInterval = Convert.ToUInt16(mSettings[36]);
            mHouseKeepingTimeout = mInterval > 0 ? mInterval * 1000 : Timeout.Infinite;

            tcpForwarderEntry = new TcpForwarderEntry();

            if (mRequiresAP)
            {
                wifiApManager = new WifiApManager(this);
                wifiApManager.ShowWritePermissionSettings(true);

                bool result = StartWiFiHotSpot();
                if (result == false)
                {
                    int n = 0;
                    do
                    {
                        result = StartWiFiHotSpot();
                        n++;
                    } while (n < 3 && result == false);
                }
            }
            else
            {
                wifiApManager = null;
            }

            StartTrackingService();

            StartPortForwarding();

            // Create a timer with two second interval.
            mHouseKeepingTimer = new System.Threading.Timer(OnHouseKeepingEvent, this, mHouseKeepingTimeout, mHouseKeepingTimeout);
            // Hook up the Elapsed event for the timer. 
        }

        // This gets called when StartService is called in our App class
        [Obsolete("deprecated in base class")]
		public override StartCommandResult OnStartCommand (Intent intent, StartCommandFlags flags, int startId)
		{
			Log.Debug (logTag, "TrackingService started");

			return StartCommandResult.Sticky;
		}

		// This gets called once, the first time any client bind to the Service
		// and returns an instance of the TrackingServiceBinder. All future clients will
		// reuse the same instance of the binder
		public override IBinder OnBind (Intent intent)
		{
			Log.Debug (logTag, "Client now bound to service");

			binder = new TrackingServiceBinder (this);
			return binder;
		}

		// Handle location updates from the location manager
		public void StartLocationUpdates () 
		{
            Log.Debug(logTag, string.Format("You are about to get location updates via {0}", "LocationManager.GpsProvider"));
            LocMgr.RequestLocationUpdates(LocationManager.GpsProvider, 1000, 0, this);
            LocMgr.AddNmeaListener(this);
            Log.Debug(logTag, "Now sending location updates");
        }

        public override void OnDestroy ()
		{
			base.OnDestroy ();
			Log.Debug (logTag, "Service has been terminated");

            // Stop getting updates from the location manager:
            LocMgr.RemoveUpdates(this);
            LocMgr.RemoveNmeaListener(this);
            StopListeningToGPS();
            StopCommsServer();
            ResetModuleVariables();
            StopPortForwarding();
            mHouseKeepingTimer.Change(Timeout.Infinite, Timeout.Infinite);
            mHouseKeepingTimer.Dispose();
        }

		#region ILocationListener implementation
		// ILocationListener is a way for the Service to subscribe for updates
		// from the System location Service

		public void OnLocationChanged (Android.Locations.Location location)
		{
            Log.Debug(logTag, "OnLocationChanged");
        }

		public void OnProviderDisabled (string provider)
		{
            Log.Debug(logTag, "OnProviderDisabled");
        }

		public void OnProviderEnabled (string provider)
		{
            Log.Debug(logTag, "OnProviderEnabled");
        }

		public void OnStatusChanged (string provider, Availability status, Bundle extras)
		{
            Log.Debug(logTag, "OnStatusChanged");
        }

        public void OnNmeaReceived(long timestamp, string nmea)
        {
            if (mRequiresAP)
            {
                bool result = StartWiFiHotSpot();
                if (result == false)
                {
                    int n = 0;
                    do
                    {
                        result = StartWiFiHotSpot();
                        n++;
                    } while (n < 3 && result == false);
                }
            }
            else
            {
                wifiApManager = null;
            }

            mHouseKeepingTimer.Change(mHouseKeepingTimeout, mHouseKeepingTimeout);

            ((NMEA0183ServiceReciever)mGPSClient).PassNMEAStringToQueue(nmea);
            CNXLog.Info(nmea);
        }

        private void StartTrackingService()
        {
            int n = 0;
            do
            {
                myIp = GetLocalIPAddressString();
                n++;

                if (myIp == String.Empty || myIp == null)
                {
                    Thread.Sleep(100);
                }
            } while (n < 10 && (myIp == String.Empty || myIp == null));


            if (myIp == String.Empty || myIp == null)
            {
                throw new Exception("Failed to retrieve the IP address of device");
            }

            // kick off GPS
            StartListeningToGPS();

            // start a CAN Client
            if (mUseCANBridge)
                mCANClient = new CANLib.CANBridgeClient(mCANServerName, mInPort, mOutPort);
            else
                mCANClient = new CANLib.CANNativeClient(mCANServerName);

            // create a GPS - CAN relay
            mCANServer = new CANCommsServer(mCANClient, mGPSClient);
            // kick off comms with the RTT server
            InitialiseCommsServer();

            //talk
            mTrackingManager = new TrackingStateManager(mCommsServer, mGPSClient);
            mCANServer.RaiseEquipmentChangedEvent += mTrackingManager.OnEquipmentChangedEventHandler;
            mCANClient.RaiseFrameReceivedEvent += mTrackingManager.FrameReceivedEventHandler;
            mCANClient.RaiseFrameReceivedEvent += FrameReceivedEventHandler;

            // add this modules device details to the device catalogue.
            AddSelfToCatalogue();

            // do APC
            APCWrapper apc = new APCWrapper(mRTTServer, mCommsAddress);
            mCANClient.RaiseFrameReceivedEvent += apc.FrameReceivedEventHandler;
            mGPSClient.RaiseGPSPositionChangedEvent += apc.OnGPSChangedEventHandler;
            mGPSClient.RaiseGPSStatusChangedEvent += apc.OnGPSChangedEventHandler;

            // prepare to recieve transient messages.
            mDriverIdReciever = new TransientBlockReciever((int)Block.DriverLogon, mCANClient);
            mDriverIdReciever.RaiseBlockStatusEvent += OnDriverIdEventHandler;
            mTxTMessageReciever = new TransientBlockReciever((int)Block.MessageToSystem, mCANClient);
            mTxTMessageReciever.RaiseBlockStatusEvent += OnTxTMessageEventHandler;

            // prepare block manager
            StartDeviceConfigurationManagement(new byte[] { 255, 255, 255, 255 });
            mCANServer.ForceEquipmentChangedEvent();

            trackingServiceStarted = true;
        }
 
        private void InitialiseCommsServer()
        {
            CommsServer commsServer = new CommsServer(mCommsAddress, mRTTServer, mTxPort, mRxPort);
            if (mCommsServer == null)
                mCommsServer = commsServer;
            else
                Interlocked.Exchange<CommsServer>(ref mCommsServer, commsServer);
            //mCommsServer.Start();
            StartListeningToCommsServer();
            mCommsServer.Start();
        }

        private void StartListeningToCommsServer()
        {
            mCommsServer.RaiseDatagramReceivedEvent += OnDatagramReceivedEventHandler;
            mCommsServer.RaiseCommsOutageEvent += OnCommsTimeoutEventHandler;
        }

        protected void OnDatagramReceivedEventHandler(object sender, DatagramReceivedEventArgs a)
        {
            CANFrame frame = new CANFrame();
            if (a.Datagram.Length < 4)
            {
                ServerMsgLog.WarnFormat("UDP out-of-range, length {0}", a.Datagram.Length);
                return;
            }

			uint msgId = 0;

            try
            {
                // work out what it all meens
                // first assume that the datgram is a CNX CAN message
                if (BitConverter.IsLittleEndian)
                    Array.Reverse(a.Datagram, 0, 4);
				msgId = BitConverter.ToUInt32(a.Datagram, 0);

                switch (msgId)
                {
                    case (uint)CNXMsgIds.TripProgress:
                        frame.MailboxId = msgId;
                        frame.DataFromArray(a.Datagram, 4, a.Datagram.Length - 4);
                        TripProgressType msgType;
                        int pathId;
                        int position;
                        int? tripNo;
                        CNXCANMsgHelper.UnpackTripProgress(frame, out msgType, out pathId, out position, out tripNo);
                        object[] args = { msgType, pathId, position, tripNo };
                        ServerMsgLog.WarnFormat("UDP Trip Progress {0}, RouteTag {1}, Progress {2}, tripNo {3}.", args);
                        mCANClient.Send(frame);
                        break;
                    case (uint)CNXMsgIds.DeviceCatalogue:
                        ServerMsgLog.WarnFormat("UDP Re-Cataloge message (0x101).");
                        // clear our catalogue
                        mCANServer.ClearDeviceCatalogue();
                        // put our self into the new catalogue
                        AddSelfToCatalogue();
                        // reset catalogues on all equipment.
                        frame.ClearData();
                        frame.MailboxId = (uint)CANLib.CNXMsgIds.DeviceCatalogue;
                        mCANClient.Send(frame);
                        mCANClient.Send(CNXCANMsgHelper.PackIdentifiers(mCommsServer.CommsAddress, mCommsServer.CompanyTag));
                        break;
                    case (uint)CNXMsgIds.TripNone:
                    case (uint)CNXMsgIds.TripOffRoute:
                        frame.MailboxId = msgId;
                        mCANClient.Send(frame);
                        ServerMsgLog.WarnFormat("UDP {0}.", (CNXMsgIds)msgId);
                        break;
                    case (uint)CNXMsgIds.TripOnRoute:
                        frame.MailboxId = msgId;
                        frame.DataFromArray(a.Datagram, 4, a.Datagram.Length - 4);
                        ServerMsgLog.WarnFormat("UDP {0}.", frame.ToString());
                        mCANClient.Send(frame);
                        break;
                    case (uint)RTTMesg.RTTInMsgIds.FirmwareBuildNos:
                        ServerMsgLog.WarnFormat("UDP firmware version (0x1001) length {0}\n{1}.", a.Datagram.Length, CommsServer.HexDump(a.Datagram, a.Datagram.Length));
                        if (a.Datagram.Length > 5)
                            StartFirmwareManagement(a.Datagram, 4);
                        break;
                    case (uint)RTTMesg.RTTInMsgIds.ResourceBuildNos:
                        ServerMsgLog.WarnFormat("UDP configuration message (0x1002) length {0}\n{1}.", a.Datagram.Length, CommsServer.HexDump(a.Datagram, a.Datagram.Length));
                        if (a.Datagram.Length > 5)
                        {
                            byte[] versions = new byte[a.Datagram.Length - 4];
                            Array.Copy(a.Datagram, 4, versions, 0, versions.Length);
                            StartDeviceConfigurationManagement(versions);
                        }
                        break;
                    case (uint)RTTMesg.RTTInMsgIds.CompanyTag:
                        mCommsServer.CompanyTag = a.Datagram[4];
                        ServerMsgLog.WarnFormat("UDP CompanyTag {0}", mCommsServer.CompanyTag);
                        mCANClient.Send(CNXCANMsgHelper.PackIdentifiers(mCommsServer.CommsAddress, mCommsServer.CompanyTag));
                        break;
                    case (uint)RTTMesg.RTTInMsgIds.DriverLogonOK:
                        ServerMsgLog.WarnFormat("UDP {0}.", (RTTMesg.RTTInMsgIds)msgId);
                        frame.MailboxId = (uint)CNXMsgIds.LogonResult;
                        frame.Data = new byte[1];
                        frame.Data[0] = (byte)LogonState.LogonOK;
                        mCANClient.Send(frame);
                        break;
                    case (uint)RTTMesg.RTTInMsgIds.DriverLogonFail:
                        ServerMsgLog.WarnFormat("UDP {0}.", (RTTMesg.RTTInMsgIds)msgId);
                        frame.MailboxId = (uint)CNXMsgIds.LogonResult;
                        frame.Data = new byte[1];
                        frame.Data[0] = (byte)LogonState.LogonFailed;
                        mCANClient.Send(frame);
                        break;
                    case (uint)RTTMesg.RTTInMsgIds.DriverLogoff:
                        ServerMsgLog.WarnFormat("UDP {0}.", (RTTMesg.RTTInMsgIds)msgId);
                        BeginSendTransientBlock(Block.DriverLogon, new byte[] { 0 });
                        break;
                    case (uint)RTTMesg.RTTInMsgIds.DriverMesg2:
                        ServerMsgLog.WarnFormat("UDP {0}\n{1}.", (RTTMesg.RTTInMsgIds)msgId, CommsServer.HexDump(a.Datagram, a.Datagram.Length));
                        //TransientBlock block = new TransientBlock(mCANClient, (byte)Block.MessageToDriver);
                        byte[] blockData = new byte[a.Datagram.Length - 4];
                        Array.Copy(a.Datagram, 4, blockData, 0, blockData.Length);
                        //block.Send(blockData, false);
                        BeginSendTransientBlock(Block.MessageToDriver, blockData);
						break;
					case (uint)RTTMesg.RTTInMsgIds.VehicleInfo:
						mCommsServer.CompanyTag = a.Datagram[4];
						mCommsServer.VehicleId = Encoding.ASCII.GetString(a.Datagram, 5, a.Datagram.Length - 5);
						ServerMsgLog.WarnFormat("UDP VehicleInfo {0} {1}", mCommsServer.CompanyTag, mCommsServer.VehicleId);
						mCANClient.Send(CNXCANMsgHelper.PackIdentifiers(mCommsServer.CommsAddress, mCommsServer.CompanyTag));
						break;
					case (uint)RTTMesg.RTTInMsgIds.FlushData:
						ServerMsgLog.WarnFormat("UDP Flush APC");
						frame.MailboxId = (uint)CNXMsgIds.ResetLoading;
						frame.DataFromArray(a.Datagram, 4, a.Datagram.Length - 4);
						mApc.FrameReceivedEventHandler(null, new FrameReceivedEventArgs(frame));
						mCANClient.Send(frame);
						// force daily restart as requested by Iris
						//if (mTracker.mIris != null)
						//	mTracker.mIris.Restart();
                        break;
                    default:
                        ServerMsgLog.WarnFormat("UDP Unknown id {0} length {1}\n{2}.", msgId, a.Datagram.Length, CommsServer.HexDump(a.Datagram, a.Datagram.Length));
                        break;
                }
            }
            catch (Exception e)
            {
				ServerMsgLog.ErrorFormat("Datagram {0} Handler Error : {1}", msgId, e.ToString());
            }
        }


        private void OnCommsTimeoutEventHandler(object sender, int timeout)
        {
            try
            {
                if (mGPSPort.StartsWith("can"))
                {
                    CNXLog.ErrorFormat("Comms timeout {0}s.", timeout);
                    return;
                }
            }
            catch (Exception) { }
            try
            {
                System.Diagnostics.Process process = new System.Diagnostics.Process();
                if (timeout > CommsServer.MaxCommsOutageSeconds)
                {
                    CNXLog.ErrorFormat("Comms timeout {0}s. Rebooting gateway {1} {2}", timeout, mGatewayRebootProcess, mGatewayRebootArgs);
                    process.StartInfo.FileName = mGatewayRebootProcess;
                    process.StartInfo.Arguments = mGatewayRebootArgs;
                    process.Start();
                }

            }
            catch (Exception e)
            {
                CNXLog.Error(string.Format("Comms timeout reboot {0} {1}", mGatewayRebootProcess, mGatewayRebootArgs), e);
            }
            try
            {
                CNXLog.ErrorFormat("Comms timeout {0}s. Rebooting system.", timeout);

                //this statement will cause the S8 reboot
                //BootstrapUpgrader.BeginReboot();
                CNXLog.ErrorFormat("Comms timeout {0}s. Restarting CommsServer", timeout);
                ThreadPool.QueueUserWorkItem((o) =>
                {
                    //RestartCommsServer();
                    mCommsServer.Stop();
                    mCommsServer.Start();
                }
                );
            }
            catch (Exception e)
            {
                CNXLog.Error("Comms timeout server restart", e);
            }
        }

        private void BeginSendTransientBlock(Block id, byte[] data)
        {
            ThreadPool.QueueUserWorkItem((o) =>
            {
                //TransientBlock block = new TransientBlock(mCANClient, (byte)id);
                TransientBlock.BlockInfo blockInfo = new TransientBlock.BlockInfo(mCANClient, (byte)id, data);
                TransientBlock.SendBlock(blockInfo);
            }
            );
        }

        private void StartDeviceConfigurationManagement(byte[] resourceVersions)
        {
            string url;
            try
            {
                if (resourceVersions.Length > 0)
                {
                    if (mRoutePatternManager == null)
                    {
                        url = string.Format("http://{0}{1}", mRTTServer, VehicleRouteReasourcePath);
                        //url = string.Format("http://{0}{1}", mRTTServer, "");
                        mRoutePatternManager = new BlockTransferManager(url, (int)Block.RouteConfig, resourceVersions[0], mCANClient);
                        // register the tracking state manager to changes
                        mRoutePatternManager.RaiseStateChangedEvent += mTrackingManager.OnBlockChangedEventHandler;
                    }
                    else
                        mRoutePatternManager.BlockVersion = resourceVersions[0];
                }
                if (resourceVersions.Length > 1)
                {
                    if (mVehicleConfigManager == null)
                    {
                        url = string.Format("http://{0}{1}", mRTTServer, VehicleConfigPath);
                        //url = string.Format("http://{0}{1}", mRTTServer, "");
                        mVehicleConfigManager = new BlockTransferManager(url, (int)Block.VehicleConfig, resourceVersions[1], mCANClient);
                        // register the tracking state manager to changes
                        mVehicleConfigManager.RaiseStateChangedEvent += mTrackingManager.OnBlockChangedEventHandler;
                    }
                    else
                        mVehicleConfigManager.BlockVersion = resourceVersions[1];
                }
                if (resourceVersions.Length > 2)
                {
                    if (mServiceAlertManager == null)
                    {
                        url = string.Format("http://{0}{1}", mRTTServer, ServiceAlertPath);
                        //url = string.Format("http://{0}{1}", mRTTServer, "");
                        mServiceAlertManager = new BlockTransferManager(url, (int)Block.ServiceAlert, resourceVersions[2], mCANClient);
                        // register the tracking state manager to changes
                        mServiceAlertManager.RaiseStateChangedEvent += mTrackingManager.OnBlockChangedEventHandler;
                    }
                    else
                        mServiceAlertManager.BlockVersion = resourceVersions[2];
                }
                if (resourceVersions.Length > 3)
                {
                    if (mDriverConfigManager == null)
                    {
                        url = string.Format("http://{0}{1}", mRTTServer, DriverConfigPath);
                        //url = string.Format("http://{0}{1}", mRTTServer, "");
                        mDriverConfigManager = new BlockTransferManager(url, (int)Block.DriverConfig, resourceVersions[3], mCANClient);
                        // register the tracking state manager to changes
                        mDriverConfigManager.RaiseStateChangedEvent += mTrackingManager.OnBlockChangedEventHandler;
                    }
                    else
                        mDriverConfigManager.BlockVersion = resourceVersions[3];
                }
            }
            catch (Exception e)
            {
                CNXLog.ErrorFormat("StartDeviceConfigurationManagement error {0}", e.ToString());
            }
        }

        private void StartFirmwareManagement(byte[] versionData, int startIndex)
        {
            try
            {
                // the vesion data is in id,version pairs
                // test against our cataloge to see if anything needs upgrading.
                for (int i = startIndex; i < versionData.Length; i += 2)
                {
                    // check if there is version information for this module
                    if (versionData[i] == mProductCode)
                    {
                        // it's for us, should we be doing a self upgrade!
                        // allow a 128 window to allow for version wrap round.
                        if (mVersion < versionData[i + 1])
                        {
                            CNXLog.InfoFormat("Doing self upgrade from {0} to {1}.", mVersion, versionData[i + 1]);
                            if (mBootstrapUpgrader == null)
                            {
                                //object[] args = { mRTTServer, Properties.Settings.Default.FirmwarePath, mProductCode, Properties.Settings.Default.FirmwareExtension };
                                object[] args = { mRTTServer, FirmwarePath, mProductCode, FirmwareExtension };
                                //object[] args = { mRTTServer, "", mProductCode, "" };
                                string url = string.Format("http://{0}{1}{2}{3}", args);
                                mBootstrapUpgrader = new BootstrapUpgrader(url);
                                mBootstrapUpgrader.BeginUpgrade();
                            }
                            else if (mBootstrapUpgrader.Status == BootstrapUpgrader.UpgradeState.Idle || mBootstrapUpgrader.Status == BootstrapUpgrader.UpgradeState.UpgradeComplete)
                                mBootstrapUpgrader.BeginUpgrade();
                        }
                    }
                    // Product Ids < 0x7f are not CAN upgradable so ignore them
                    if (versionData[i] < 0x7f)
                        continue;

                    DeviceCatalogueInfo info = mCANServer.DeviceCatalogue.FindDevice((DeviceCatalogueInfo.Product)versionData[i]);
                    if (info != null)
                    {
                        if (info.BuildNo < versionData[i + 1])
                        {
                            BlockTransferManager txManager = null;
                            // need to organise an upgrade
                            foreach (BlockTransferManager btm in mFirmwareList)
                                if (btm.BlockId == (byte)info.ProductId)
                                {
                                    txManager = btm;
                                    btm.BlockVersion = versionData[i + 1];
                                    break;
                                }
                            if (txManager == null)
                            {
                                //object[] args = { mRTTServer, Properties.Settings.Default.FirmwarePath, versionData[i], Properties.Settings.Default.FirmwareExtension };
                                object[] args = { mRTTServer, FirmwarePath, versionData[i], FirmwareExtension };
                                //object[] args = { mRTTServer, "", versionData[i], "" };
                                string url = string.Format("http://{0}{1}{2}{3}", args);
                                mFirmwareList.Add(new BlockTransferManager(url, versionData[i], versionData[i + 1], mCANClient));
                            }
                        }
                    }
                }

            }
            catch (Exception e)
            {
                CNXLog.ErrorFormat("StartFirmwareManagement error {0}", e.ToString());
            }
        }

        private void AddSelfToCatalogue()
        {
            try
            {
                DeviceCatalogueInfo dci = mCANServer.DeviceCatalogue.FindDevice((DeviceCatalogueInfo.Product)mProductCode);
                if (dci == null)
                    ResetDeviceCatalogue(mCANServer, (DeviceCatalogueInfo.Product)mProductCode, (byte)mVersion);
                // flush the catalouge on upgrade.
                else if (dci.BuildNo != (byte)mVersion)
                    ResetDeviceCatalogue(mCANServer, (DeviceCatalogueInfo.Product)mProductCode, (byte)mVersion);
            }
            catch (Exception e)
            {
                CNXLog.ErrorFormat("AddSelfToCatalogue Error : {0}", e.ToString());
            }

            CNXLog.WarnFormat("Device Catalogue {0}", mCANServer.DeviceCatalogue.ToString());
        }

        private static void ResetDeviceCatalogue(CANCommsServer canServer, DeviceCatalogueInfo.Product prodId, byte ver)
        {
            try
            {
                canServer.DeviceCatalogue.Clear();
                DeviceCatalogueInfo dci = new DeviceCatalogueInfo()
                {
					BuildNo = ver,
                    ProductId = (DeviceCatalogueInfo.Product)mProductCode
                };
                canServer.DeviceCatalogue.UpdateDeviceCatalogue(dci);
                canServer.PersistCatalogue();
            }
            catch (Exception e)
            {
                CNXLog.ErrorFormat("ResetDeviceCatalogue Error : {0}", e.ToString());
            }
        }


        private void StartListeningToGPS()
        {
            mGPSClient = new NMEA0183ServiceReciever();
            mGPSClient.MovingThreshold = mMovingThreshold;
        }

        private void StopListeningToGPS()
        {
            mGPSClient.Dispose();
        }

        private void FrameReceivedEventHandler(object o, FrameReceivedEventArgs a)
        {
            switch ((CNXMsgIds)a.Frame.MailboxId)
            {
                case CNXMsgIds.DriverMessageAck:
                    mCommsServer.Send(RTTMesg.CreateDriverMessageAck(BitConverter.ToUInt16(a.Frame.Data, 0)));
                    break;
                case CNXMsgIds.DriverStatus:
                    CNXLog.WarnFormat("Driver status recieved {0}.", (CANLib.DriverStatus)a.Frame.Data[0]);
                    RTTMesg.RTTOutMsgIds state = RTTMesg.RTTOutMsgIds.UNKNOWN;
                    switch ((CANLib.DriverStatus)a.Frame.Data[0])
                    {
                        case DriverStatus.Break:
                            state = RTTMesg.RTTOutMsgIds.DriverStatusOnBreak;
                            break;
                        case DriverStatus.Normal:
                            state = RTTMesg.RTTOutMsgIds.DriverStatusNormal;
                            break;
                        case DriverStatus.OutOfVehicle:
                            state = RTTMesg.RTTOutMsgIds.DriverStatusOutOfVehicle;
                            break;
                        default:
                            CNXLog.WarnFormat("CNXMsgIds.DriverStatus {0} out of range.", a.Frame.Data[0]);
                            break;
                    }
                    if (state != RTTMesg.RTTOutMsgIds.UNKNOWN)
                        mCommsServer.Send(RTTMesg.CreateDriverStatus(state));
                    break;
                default:
                    break;
            }
        }

        private void OnDriverIdEventHandler(object sender, BlockStatusEventArgs stateEvent)
        {
            try
            {
                CNXLog.InfoFormat("DriverId {0}", stateEvent.State);
                if (stateEvent.State == BlockState.COMPLETE || stateEvent.State == BlockState.COMPLETE_REPEAT)
                {
                    TransientBlockReciever reciever = (TransientBlockReciever)sender;
                    if (reciever.BlockData.Length == 0)
                        mCommsServer.Send(RTTMesg.CreateDriverStatus(RTTMesg.RTTOutMsgIds.DriverLogoff));
                    else if (reciever.BlockData[0] == 0)
                        mCommsServer.Send(RTTMesg.CreateDriverStatus(RTTMesg.RTTOutMsgIds.DriverLogoff));
                    else
                    {
                        string driverId = Encoding.ASCII.GetString(reciever.BlockData);
                        CNXLog.WarnFormat("DriverId {0}", driverId);
                        mCommsServer.Send(RTTMesg.CreateDriverLogonMessage(driverId));
                    }
                }
            }
            catch (Exception e)
            {
                CNXLog.Error("OnDriverIdEventHandler", e);
            }
        }

        private void OnTxTMessageEventHandler(object sender, BlockStatusEventArgs stateEvent)
        {
            try
            {
                if (stateEvent.State == BlockState.COMPLETE || stateEvent.State == BlockState.COMPLETE_REPEAT)
                {
                    TransientBlockReciever reciever = (TransientBlockReciever)sender;
                    string txtMesg = Encoding.ASCII.GetString(reciever.BlockData);
                    CNXLog.InfoFormat("TxT Message {0}", txtMesg);
                    mCommsServer.Send(RTTMesg.CreateDriverTxTMessage(txtMesg));
                }
            }
            catch (Exception e)
            {
                CNXLog.Error("OnTxTMessageEventHandler", e);
            }
        }

        private void StopCommsServer()
        {
            StopListeningToCommsServer();
            mCommsServer.Stop();
        }

        private void StopListeningToCommsServer()
        {
            mCommsServer.RaiseCommsOutageEvent -= OnCommsTimeoutEventHandler;
            mCommsServer.RaiseDatagramReceivedEvent -= OnDatagramReceivedEventHandler;
        }

        private void ResetModuleVariables()
        {
            mTrackingManager.ResetTimerVariable();
            mCANServer.RaiseEquipmentChangedEvent -= mTrackingManager.OnEquipmentChangedEventHandler;
            mCANClient.RaiseFrameReceivedEvent -= mTrackingManager.FrameReceivedEventHandler;
            mCANClient.RaiseFrameReceivedEvent -= FrameReceivedEventHandler;

            mCANClient = null;
            mCANServer = null;
            mTrackingManager = null;
            trackingServiceStarted = false;
        }

        public string GetLocalIPAddressString()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }

        private bool StartWiFiHotSpot()
        {
            bool result = true;

            if (wifiApManager.IsWifiApEnabled() == false)
            {
                WifiConfiguration config = new WifiConfiguration();
                config = wifiApManager.GetWifiApConfiguration();

                result = wifiApManager.SetWifiApEnabled(config, true);
            }

            return result;
        }

        private void StartPortForwarding()
        {
            long[] ipRanges = new long[8];

            if (IsIPValid(mAllowedIP))
            {
                GetIps(mAllowedIP, mBits).CopyTo(ipRanges, 0);
            }
            if (IsIPValid(mAllowedIP2))
            {
                GetIps(mAllowedIP2, mBits2).CopyTo(ipRanges, 2);
            }
            if (IsIPValid(mAllowedIP3))
            {
                GetIps(mAllowedIP3, mBits3).CopyTo(ipRanges, 4);
            }
            if (IsIPValid(mAllowedIP4))
            {
                GetIps(mAllowedIP4, mBits4).CopyTo(ipRanges, 6);
            }

            Task.Run(() => //cpu-bound work
            {
                if (!ForwardingManager.Instance.IsEnabled)
                { 
                    tcpForwarderEntry.ClearIPPortSetings();
                    tcpForwarderEntry.PopulateAllowedIpsRange(ipRanges);
                    
                    //Check if properties of Rules are valid
                    if (this.mFrom > 0 && this.mTo > 0 && IsIPValid(mTarget))
                    {
                        //Set IP and Port properties for port forwarding 1
                        if (mNi == "")
                        {
                            mNi = "eth0";
                        }
                        tcpForwarderEntry.SetIPPorts(true, false, "Rule 1", mNi, mFrom, mTarget, mTo);
                    }

                    if (this.mFrom2 > 0 && this.mTo2 > 0 && IsIPValid(mTarget2))
                    {
                        //Set IP and Port properties for port forwarding 2
                        if (mNi2 == "")
                        {
                            mNi2 = "eth0";
                        }
                        tcpForwarderEntry.SetIPPorts(true, false, "Rule 2", mNi2, mFrom2, mTarget2, mTo2);
                    }

                    if (this.mFrom3 > 0 && this.mTo3 > 0 && IsIPValid(mTarget3))
                    {
                        //Set IP and Port properties for port forwarding 2
                        if (mNi3 == "")
                        {
                            mNi3 = "eth0";
                        }
                        tcpForwarderEntry.SetIPPorts(true, false, "Rule 3", mNi3, mFrom3, mTarget3, mTo3);
                    }

                    DateTime start = new DateTime();
                    bool ok = false;
  
                    do
                    {
                        if (DateTime.Now.Subtract(start).Seconds >= 20 || start.Second == 0)
                        {
                            start = DateTime.Now;
                            tcpForwarderEntry.StartForwarder();
                            ok = tcpForwarderEntry.RunService;
                        }
                        
                    } while (ok == false);
                }
            }).ConfigureAwait(false);
        }

        private void StopPortForwarding()
        {
            Task.Run(() => //cpu-bound work
            {
                if (ForwardingManager.Instance.IsEnabled)
                {
                    tcpForwarderEntry.StopForwarder();
                }
            }).ConfigureAwait(false);
        }

        private List<string> GetAllIps(string ips, int bits)
        {
            List<string> ipAddresses = new List<string>();


            return ipAddresses;
        }

        private long[] GetIps(string ip, int bits)
        {
            long[] ipRange = new long[2];

            if (!IsIPValid(ip) || bits == 0 || bits < 0)
            {
                ipRange[0] = 0;
                ipRange[1] = 0;
                return ipRange;
            }

            long mask = ~(long.MaxValue >> bits);

            // Convert the IP address to bytes.
            byte[] ipBytes = IPAddress.Parse(ip).GetAddressBytes();

            // BitConverter gives bytes in opposite order to GetAddressBytes().
            byte[] maskBytes = BitConverter.GetBytes(mask).Reverse().ToArray();

            byte[] startIPBytes = new byte[ipBytes.Length];
            byte[] endIPBytes = new byte[ipBytes.Length];

            // Calculate the bytes of the start and end IP addresses.
            for (int i = 0; i < ipBytes.Length; i++)
            {
                startIPBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
                endIPBytes[i] = (byte)(ipBytes[i] | ~maskBytes[i]);
            }

            // Convert the bytes to IP addresses.
            IPAddress startIP = new IPAddress(startIPBytes);
            IPAddress endIP = new IPAddress(endIPBytes);

            ipRange[0] = (ConvertIpToInt(startIP));
            ipRange[1] = (ConvertIpToInt(endIP));

            return ipRange;
        }

        private long ConvertIpToInt(IPAddress ipIn)
        {
            long ip = 0; ;
            string ipString = ipIn.ToString();
            string[] addressArry = ipString.Split('.');

            for (int i = 0; i < addressArry.Count(); i++)
            {

                int power = 3 - i;
                ip += (UInt32)((UInt32.Parse(addressArry[i]) % 256 * Math.Pow(256, power)));
            }

            return ip;
        }

        private bool IsIPValid(string ip)
        {
            Match match = Regex.Match(ip, @"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}");
            if (match.Success)
            {
                return true;
            }

            return false;
        }

        private void OnHouseKeepingEvent(Object source)
        {
            ((NMEA0183ServiceReciever)mGPSClient).PassNMEAStringToQueue("");
            CNXLog.Info("OnHouseKeepingEvent============>>>>>>> " + source.GetHashCode());
        }
        #endregion
    }
}

