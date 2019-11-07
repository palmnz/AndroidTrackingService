using Android.App;
using Android.Preferences;
using Android.OS;
using Android.Content;
using Android.Content.PM;
using System;
using Android.Widget;
using System.Net;
using System.Threading.Tasks;

namespace Tracking.Services
{
	/// <summary>
	/// Parent setting activity, all ti does is load up the headers
	/// </summary>
	[Activity (Label = "@string/menu_settings", Icon="@drawable/ic_launcher", Theme = "@style/ThemeActionBar", ScreenOrientation = ScreenOrientation.Landscape)]
	public class SettingsActivity : PreferenceActivity, ISharedPreferencesOnSharedPreferenceChangeListener
	{
        protected override void OnCreate (Bundle savedInstanceState)
		{
			base.OnCreate (savedInstanceState);
			this.AddPreferencesFromResource (Resource.Xml.settings_general);
            this.AddPreferencesFromResource(Resource.Xml.pref_button);

            Preference pref = (Preference)FindPreference("ExitSettings");

            pref.PreferenceClick += Pref_PreferenceClick;
        }

        async void Pref_PreferenceClick(object sender, Preference.PreferenceClickEventArgs e)
        {
            if (TrackingService.trackingServiceStarted == true)
            {
                App.StopLocationService();
                await Task.Delay(500);
            }

            App.StartLocationService(this);

            Finish();
        }

        public static string GetDefaultString()
        {
            string theDefault = String.Empty;

            theDefault = Helpers.Settings.RTTServer.ToString() + "," +          //0
                         Helpers.Settings.TxPort.ToString() + "," +             //1
                         Helpers.Settings.RxPort.ToString() + "," +             //2
                         Helpers.Settings.CANServerName + "," +                 //3
                         Helpers.Settings.UseCANBridge + "," +                  //4
                         Helpers.Settings.CANInPort + "," +                     //5
                         Helpers.Settings.CANOutPort + "," +                    //6
                         Helpers.Settings.CommsAddress + "," +                  //7
                         Helpers.Settings.StationaryRate + "," +                //8
                         Helpers.Settings.MovingThreshold + "," +               //9
                         Helpers.Settings.MovingRate + "," +                    //10
                         Helpers.Settings.MovingHysteresis + "," +              //11
                         Helpers.Settings.VehicleConfigVersion + "," +          //12
                         Helpers.Settings.RoutePatternVersion + "," +           //13
                         Helpers.Settings.DeviceMask + "," +                    //14
                         Helpers.Settings.RequiresAP + "," +                    //15
                         Helpers.Settings.AllowedIP + "," +                     //16
                         Helpers.Settings.Bits + "," +                          //17
                         Helpers.Settings.AllowedIP2 + "," +                    //18
                         Helpers.Settings.Bits2 + "," +                         //19
                         Helpers.Settings.AllowedIP3 + "," +                    //20
                         Helpers.Settings.Bits3 + "," +                         //21
                         Helpers.Settings.AllowedIP4 + "," +                    //22
                         Helpers.Settings.Bits4 + "," +                         //23
                         Helpers.Settings.Ni + "," +                          //24
                         Helpers.Settings.From + "," +                          //25
                         Helpers.Settings.Target + "," +                        //26
                         Helpers.Settings.To + "," +                            //27
                         Helpers.Settings.Ni2 + "," +                         //28
                         Helpers.Settings.From2 + "," +                         //29
                         Helpers.Settings.Target2 + "," +                       //30
                         Helpers.Settings.To2 + "," +                           //31
                         Helpers.Settings.Ni3 + "," +                         //28
                         Helpers.Settings.From3 + "," +                         //29
                         Helpers.Settings.Target3 + "," +                       //30
                         Helpers.Settings.To3 + "," +                                //31
                         Helpers.Settings.Interval;                             //32


            return theDefault;
        }

        public void OnSharedPreferenceChanged (ISharedPreferences sharedPreferences, string key)
		{
			switch (key) {
            case Helpers.Settings.RTTServerKey:
                this.SetRTTServer();
                break;
            case Helpers.Settings.TxPortKey:
                this.SetTxPort();
                break;
            case Helpers.Settings.RxPortKey:
                this.SetRxPort();
                break;
            case Helpers.Settings.CANServerNameKey:
                this.SetCANServerName();
                break;
            case Helpers.Settings.UseCANBridgeKey:
                this.SetUseCANBridge();
                break;
            case Helpers.Settings.CANInPortKey:
                this.SetCANInPort();
                break;
            case Helpers.Settings.CANOutPortKey:
                this.SetCANOutPort();
                break;
            case Helpers.Settings.CommsAddressKey:
                this.SetCommsAddress();
                break;
            case Helpers.Settings.StationaryRateKey:
                this.SetStationaryRate();
                break;
            case Helpers.Settings.MovingThresholdKey:
                this.SetMovingThreshold();
                break;
            case Helpers.Settings.MovingRateKey:
                this.SetMovingRate();
                break;
            case Helpers.Settings.MovingHysteresisKey:
                this.SetMovingHysteresis();
                break;
            //case Helpers.Settings.VehicleRouteReasourcePathKey:
            //    this.SetVehicleRouteReasourcePath();
            //    break;
            //case Helpers.Settings.VehicleConfigPathKey:
            //    this.SetVehicleConfigPath();
            //    break;
            //case Helpers.Settings.FirmwarePathKey:
            //    this.SetFirmwarePath();
            //    break;
            case Helpers.Settings.VehicleConfigVersionKey:
                this.SetVehicleConfigVersion();
                break;
            case Helpers.Settings.RoutePatternVersionKey:
                this.SetRoutePatternVersion();
                break;
            //case Helpers.Settings.VersionPathKey:
            //    this.SetVersionPath();
            //    break;
            case Helpers.Settings.DeviceMaskKey:
                this.SetDeviceMask();
                break;
            case Helpers.Settings.RequiresAPKey:
                    this.SetRequiresAP();
                break;
            case Helpers.Settings.Bits4Key:
                this.SetBits4();
                break;
            case Helpers.Settings.AllowedIP4Key:
                this.SetAllowedIP4();
                break;
            case Helpers.Settings.Bits3Key:
                this.SetBits3();
                break;
            case Helpers.Settings.AllowedIP3Key:
                this.SetAllowedIP3();
                break;
            case Helpers.Settings.Bits2Key:
                this.SetBits2();
                break;
            case Helpers.Settings.AllowedIP2Key:
                this.SetAllowedIP2();
                break;
            case Helpers.Settings.BitsKey:
                this.SetBits();
                break;
            case Helpers.Settings.AllowedIPKey:
                this.SetAllowedIP();
                break;
            case Helpers.Settings.NiKey:
                this.SetNi();
                break;
            case Helpers.Settings.FromKey:
                this.SetFrom();
                break;
            case Helpers.Settings.TargetKey:
                this.SetTarget();
                break;
            case Helpers.Settings.ToKey:
                this.SetTo();
                break;
            case Helpers.Settings.Ni2Key:
                this.SetNi2();
                break;
            case Helpers.Settings.From2Key:
                this.SetFrom2();
                break;
            case Helpers.Settings.Target2Key:
                this.SetTarget2();
                break;
            case Helpers.Settings.To2Key:
                this.SetTo2();
                break;
            case Helpers.Settings.Ni3Key:
                this.SetNi3();
                break;
            case Helpers.Settings.From3Key:
                this.SetFrom3();
                break;
            case Helpers.Settings.Target3Key:
                this.SetTarget3();
                break;
            case Helpers.Settings.To3Key:
                this.SetTo3();
                break;
            case Helpers.Settings.IntervalKey:
                this.SetInterval();
                break;


                    //case Helpers.Settings.FirmwareExtensionKey:
                    //    this.SetFirmwareExtension();
                    //    break;
                    //case Helpers.Settings.UpdateScriptFileNameKey:
                    //    this.SetUpdateScriptFileName();
                    //    break;
                    //case Helpers.Settings.VarPathKey:
                    //    this.SetVarPath();
                    //    break;
                    //case Helpers.Settings.ServiceAlertPathKey:
                    //    this.SetServiceAlertPath();
                    //    break;
                    //case Helpers.Settings.DriverConfigPathKey:
                    //    this.SetDriverConfigPath();
                    //    break;
            }
		}
        private void SetInterval()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.IntervalKey);
            var interval = Helpers.Settings.Interval;
            pref.Summary = string.Format(interval.ToString());
        }


        private void SetNi()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.NiKey);
            var ni = Helpers.Settings.Ni;
            pref.Summary = string.Format(ni.ToString());
        }

        private void SetFrom()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.FromKey);
            var from = Helpers.Settings.From;
            pref.Summary = string.Format(from.ToString());
        }

        private void SetTarget()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.TargetKey);
            var target = Helpers.Settings.Target;
            pref.Summary = string.Format(target.ToString());
        }

        private void SetTo()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.ToKey);
            var to = Helpers.Settings.To;
            pref.Summary = string.Format(to.ToString());
        }

        private void SetNi2()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.Ni2Key);
            var ni2 = Helpers.Settings.Ni2;
            pref.Summary = string.Format(ni2.ToString());
        }

        private void SetFrom2()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.From2Key);
            var from2 = Helpers.Settings.From2;
            pref.Summary = string.Format(from2.ToString());
        }

        private void SetTarget2()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.Target2Key);
            var target2 = Helpers.Settings.Target2;
            pref.Summary = string.Format(target2.ToString());
        }

        private void SetTo2()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.To2Key);
            var to2 = Helpers.Settings.To2;
            pref.Summary = string.Format(to2.ToString());
        }

        private void SetNi3()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.Ni3Key);
            var ni3 = Helpers.Settings.Ni3;
            pref.Summary = string.Format(ni3.ToString());
        }

        private void SetFrom3()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.From3Key);
            var from3 = Helpers.Settings.From3;
            pref.Summary = string.Format(from3.ToString());
        }

        private void SetTarget3()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.Target3Key);
            var target3 = Helpers.Settings.Target3;
            pref.Summary = string.Format(target3.ToString());
        }

        private void SetTo3()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.To3Key);
            var to3 = Helpers.Settings.To3;
            pref.Summary = string.Format(to3.ToString());
        }

        private void SetAllowedIP()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.AllowedIPKey);
            var allowedIP = Helpers.Settings.AllowedIP;
            pref.Summary = string.Format(allowedIP.ToString());
        }

        private void SetBits()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.BitsKey);
            var bits = Helpers.Settings.Bits;
            pref.Summary = string.Format(bits.ToString());
        }

        private void SetAllowedIP2()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.AllowedIP2Key);
            var allowedIP2 = Helpers.Settings.AllowedIP2;
            pref.Summary = string.Format(allowedIP2.ToString());
        }

        private void SetBits2()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.Bits2Key);
            var bits2 = Helpers.Settings.Bits2;
            pref.Summary = string.Format(bits2.ToString());
        }

        private void SetAllowedIP3()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.AllowedIP3Key);
            var allowedIP3 = Helpers.Settings.AllowedIP3;
            pref.Summary = string.Format(allowedIP3.ToString());
        }

        private void SetBits3()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.Bits3Key);
            var bits3 = Helpers.Settings.Bits3;
            pref.Summary = string.Format(bits3.ToString());
        }

        private void SetAllowedIP4()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.AllowedIP4Key);
            var allowedIP4 = Helpers.Settings.AllowedIP4;
            pref.Summary = string.Format(allowedIP4.ToString());
        }

        private void SetBits4()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.Bits4Key);
            var bits4 = Helpers.Settings.Bits4;
            pref.Summary = string.Format(bits4.ToString());
        }

        private void SetRequiresAP()
        {
            var pref = (CheckBoxPreference)this.FindPreference(Helpers.Settings.RequiresAPKey);
            var requiresAP = Helpers.Settings.RequiresAP;
            pref.Summary = string.Format(requiresAP.ToString());
        }

        private void SetDriverConfigPath()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.DriverConfigPathKey);
            var driverConfigPath = Helpers.Settings.DriverConfigPath;
            pref.Summary = string.Format(driverConfigPath.ToString());
        }

        private void SetServiceAlertPath()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.ServiceAlertPathKey);
            var serviceAlertPath = Helpers.Settings.ServiceAlertPath;
            pref.Summary = string.Format(serviceAlertPath.ToString());
        }
        private void SetVarPath()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.VarPathKey);
            var varPath = Helpers.Settings.VarPath;
            pref.Summary = string.Format(varPath.ToString());
        }

        private void SetUpdateScriptFileName()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.UpdateScriptFileNameKey);
            var updateScriptFileName = Helpers.Settings.UpdateScriptFileName;
            pref.Summary = string.Format(updateScriptFileName.ToString());
        }

        private void SetFirmwareExtension()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.FirmwareExtensionKey);
            var firmwareExtension = Helpers.Settings.FirmwareExtension;
            pref.Summary = string.Format(firmwareExtension.ToString());
        }

        private void SetDeviceMask()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.DeviceMaskKey);
            var deviceMask = Helpers.Settings.DeviceMask;
            pref.Summary = string.Format(deviceMask.ToString());
        }

        private void SetVersionPath()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.VersionPathKey);
            var versionPath = Helpers.Settings.VersionPath;
            pref.Summary = string.Format(versionPath.ToString());
        }

        private void SetRoutePatternVersion()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.RoutePatternVersionKey);
            var routePatternVersion = Helpers.Settings.RoutePatternVersion;
            pref.Summary = string.Format(routePatternVersion.ToString());
        }

        private void SetVehicleConfigVersion()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.VehicleConfigVersionKey);
            var vehicleConfigVersion = Helpers.Settings.VehicleConfigVersion;
            pref.Summary = string.Format(vehicleConfigVersion.ToString());
        }

        private void SetFirmwarePath()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.FirmwarePathKey);
            var firmwarePath = Helpers.Settings.FirmwarePath;
            pref.Summary = string.Format(firmwarePath.ToString());
        }

        private void SetVehicleConfigPath()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.VehicleConfigPathKey);
            var vehicleConfigPath = Helpers.Settings.VehicleConfigPath;
            pref.Summary = string.Format(vehicleConfigPath.ToString());
        }
        private void SetVehicleRouteReasourcePath()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.VehicleRouteReasourcePathKey);
            var vehicleRouteReasourcePath = Helpers.Settings.VehicleRouteReasourcePath;
            pref.Summary = string.Format(vehicleRouteReasourcePath.ToString());
        }

        private void SetMovingHysteresis()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.MovingHysteresisKey);
            var movingHysteresis = Helpers.Settings.MovingHysteresis;
            pref.Summary = string.Format(movingHysteresis.ToString());
        }

        private void SetMovingRate()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.MovingRateKey);
            var movingRate = Helpers.Settings.MovingRate;
            pref.Summary = string.Format(movingRate.ToString());
        }

        private void SetMovingThreshold()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.MovingThresholdKey);
            var movingThreshold = Helpers.Settings.MovingThreshold;
            pref.Summary = string.Format(movingThreshold.ToString());
        }

        private void SetStationaryRate()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.StationaryRateKey);
            var stationaryRate = Helpers.Settings.StationaryRate;
            pref.Summary = string.Format(stationaryRate.ToString());
        }

        private void SetCommsAddress()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.CommsAddressKey);
            var commsAddress = Helpers.Settings.CommsAddress;
            pref.Summary = string.Format(commsAddress.ToString());
        }

        private void SetCANOutPort()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.CANOutPortKey);
            var canOutPort = Helpers.Settings.CANOutPort;
            pref.Summary = string.Format(canOutPort.ToString());
        }

        private void SetCANInPort()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.CANInPortKey);
            var canInPort = Helpers.Settings.CANInPort;
            pref.Summary = string.Format(canInPort.ToString());
        }

        private void SetUseCANBridge()
        {
            var pref = (CheckBoxPreference)this.FindPreference(Helpers.Settings.UseCANBridgeKey);
            var useCANBridge = Helpers.Settings.UseCANBridge;
            pref.Summary = string.Format(useCANBridge.ToString());
        }

        private void SetCANServerName()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.CANServerNameKey);
            var canServerName = Helpers.Settings.CANServerName;
            pref.Summary = string.Format(canServerName.ToString());
        }

        private void SetRxPort()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.RxPortKey);
            var rxPort = Helpers.Settings.RxPort;
            pref.Summary = string.Format(rxPort.ToString());
        }

        private void SetTxPort()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.TxPortKey);
            var txPort = Helpers.Settings.TxPort;
            pref.Summary = string.Format(txPort.ToString());
        }

        private void SetRTTServer()
        {
            var pref = (EditTextPreference)this.FindPreference(Helpers.Settings.RTTServerKey);
            var rttServer = Helpers.Settings.RTTServer;
            pref.Summary = string.Format(rttServer.ToString());
        }

        protected override void OnStart ()
		{
			base.OnStart ();

            this.SetNi();
            this.SetFrom();
            this.SetTarget();
            this.SetTo();
            this.SetNi2();
            this.SetFrom2();
            this.SetTarget2();
            this.SetTo2();
            this.SetNi3();
            this.SetFrom3();
            this.SetTarget3();
            this.SetTo3();
            this.SetInterval();
            this.SetAllowedIP();
            this.SetBits();
            this.SetAllowedIP2();
            this.SetBits2();
            this.SetAllowedIP3();
            this.SetBits3();
            this.SetAllowedIP4();
            this.SetBits4();
            this.SetRequiresAP();
            this.SetDeviceMask();
            this.SetRoutePatternVersion();
            this.SetVehicleConfigVersion();
            this.SetMovingHysteresis();
            this.SetMovingRate();
            this.SetMovingThreshold();
            this.SetStationaryRate();
            this.SetCommsAddress();
            this.SetCANOutPort();
            this.SetCANInPort();
            this.SetUseCANBridge();
            this.SetCANServerName();
            this.SetRxPort();
            this.SetTxPort();
            this.SetRTTServer();
        }

		protected override void OnPause ()
		{
			base.OnPause ();
			this.PreferenceManager.SharedPreferences.UnregisterOnSharedPreferenceChangeListener (this);
		}

		protected override void OnResume ()
		{
			base.OnResume ();
			this.PreferenceManager.SharedPreferences.RegisterOnSharedPreferenceChangeListener (this);
		}
    }
}