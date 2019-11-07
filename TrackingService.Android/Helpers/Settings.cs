
using System.Globalization;
using Android.Content;
using Android.Preferences;
using System;
using Android.App;

namespace Helpers
{
  /// <summary>
  /// This is the Settings static class that can be used in your Core solution or in any
  /// of your client applications. All settings are laid out the same exact way with getters
  /// and setters. 
  /// </summary>
  public static class Settings
  {
		private static SettingsHelper appSettings;
		private static SettingsHelper AppSettings
        {
          get
          {
				    return appSettings ?? (appSettings = new SettingsHelper());
          }
        }

    #region Setting Constants

        public const string RTTServerKey = "RTTServer";
        private static readonly string RTTServerDefault = "192.168.30.92";

        public const string TxPortKey = "TxPort";
        private static readonly string TxPortDefault = "2947";

        public const string RxPortKey = "RxPort";
        private static readonly string RxPortDefault = "2947";

        public const string CANServerNameKey = "CANServerName";
        private static readonly string CANServerNameDefault = "192.168.43.100";

        public const string UseCANBridgeKey = "UseCANBridge";
        private static readonly bool UseCANBridgeDefault = true;

        public const string CANInPortKey = "CANInPort";
        private static readonly string CANInPortDefault = "3005";

        public const string CANOutPortKey = "CANOutPort";
        private static readonly string CANOutPortDefault = "3005";

        public const string CommsAddressKey = "CommsAddress";
        private static readonly string CommsAddressDefault = "11";

        public const string StationaryRateKey = "StationaryRate";
        private static readonly string StationaryRateDefault = "100";

        public const string MovingThresholdKey = "MovingThreshold";
        private static readonly string MovingThresholdDefault = "0.75";

        public const string MovingRateKey = "MovingRate";
        private static readonly string MovingRateDefault = "15";

        public const string MovingHysteresisKey = "MovingHysteresis";
        private static readonly string MovingHysteresisDefault = "4";

        public const string VehicleRouteReasourcePathKey = "VehicleRouteReasourcePath";
        private static readonly string VehicleRouteReasourcePathDefault = "/rtt/realtime/resource/RoutePatternForVehicle.zip";

        public const string VehicleConfigPathKey = "VehicleConfigPath";
        private static readonly string VehicleConfigPathDefault = "/rtt/realtime/resource/VehicleConfig.zip";

        public const string FirmwarePathKey = "FirmwarePath";
        private static readonly string FirmwarePathDefault = "/rtt/realtime/resource/";

        public const string VehicleConfigVersionKey = "VehicleConfigVersion";
        private static readonly string VehicleConfigVersionDefault = "0";

        public const string RoutePatternVersionKey = "RoutePatternVersion";
        private static readonly string RoutePatternVersionDefault = "0";

        public const string VersionPathKey = "VersionPath";
        private static readonly string VersionPathDefault = "/etc/connexionz/connexionz-version";

        public const string DeviceMaskKey = "DeviceMask";
        private static readonly string DeviceMaskDefault = "0";

        public const string FirmwareExtensionKey = "FirmwareExtension";
        private static readonly string FirmwareExtensionDefault = ".bin";

        public const string UpdateScriptFileNameKey = "UpdateScriptFileName";
        private static readonly string UpdateScriptFileNameDefault = "ctm-update";

        public const string VarPathKey = "VarPath";
        private static readonly string VarPathDefault = "/var/lib/connexionz/";

        public const string ServiceAlertPathKey = "ServiceAlertPath";
        private static readonly string ServiceAlertPathDefault = "/rtt/realtime/resource/ServiceAlertForVehicle.zip";

        public const string DriverConfigPathKey = "DriverConfigPath";
        private static readonly string DriverConfigPathDefault = "/rtt/realtime/resource/DriverConfig.zip";

        public const string ExitSettingsKey = "ExitSettings";
        private static readonly bool ExitSettingsDefault = true;

        public const string RequiresAPKey = "RequiresAP";
        private static readonly bool RequiresAPDefault = true;

        public const string AllowedIPKey = "AllowedIP";
        private static readonly string AllowedIPDefault = "192.168.30.0";

        public const string BitsKey = "Bits";
        private static readonly string BitsDefault = "24";

        public const string AllowedIP2Key = "AllowedIP2";
        private static readonly string AllowedIP2Default = "192.168.30.123";

        public const string Bits2Key = "Bits2";
        private static readonly string Bits2Default = "32";

        public const string AllowedIP3Key = "AllowedIP3";
        private static readonly string AllowedIP3Default = "192.168.30.123";

        public const string Bits3Key = "Bits3";
        private static readonly string Bits3Default = "32";

        public const string AllowedIP4Key = "AllowedIP4";
        private static readonly string AllowedIP4Default = "192.168.30.123";

        public const string Bits4Key = "Bits4";
        private static readonly string Bits4Default = "32";

        public const string NiKey = "Ni";
        private static readonly string NiDefault = "Network Interface";

        public const string FromKey = "From";
        private static readonly string FromDefault = "8181";

        public const string TargetKey = "Target";
        private static readonly string TargetDefault = "192.168.43.100";

        public const string ToKey = "To";
        private static readonly string ToDefault = "8181";

        public const string Ni2Key = "Ni2";
        private static readonly string Ni2Default = "Network Interface2";

        public const string From2Key = "From2";
        private static readonly string From2Default = "2222";

        public const string Target2Key = "Target2";
        private static readonly string Target2Default = "192.168.43.100";

        public const string To2Key = "To2";
        private static readonly string To2Default = "22";

        public const string Ni3Key = "Ni3";
        private static readonly string Ni3Default = "Ni3";

        public const string From3Key = "From3";
        private static readonly string From3Default = "8181";

        public const string Target3Key = "Target3";
        private static readonly string Target3Default = "192.168.43.100";

        public const string To3Key = "To3";
        private static readonly string To3Default = "8181";

        public const string IntervalKey = "Interval";
        private static readonly string IntervalDefault = "5";

        #endregion


        /// <summary>
        /// Gets a value indicating whether to use kilometeres.
        /// </summary>
        /// <value><c>true</c> if use kilometeres; otherwise, <c>false</c>.</value>
        public static bool UseKilometeres 
		{
			get 
			{
				return CultureInfo.CurrentCulture.Name != "en-US";
			}
		}



        public static string Interval
        {
            get
            {
                return AppSettings.GetValueOrDefault(IntervalKey, IntervalDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(IntervalKey, value))
                    AppSettings.Save();
            }
        }

        public static string Ni
        {
            get
            {
                return AppSettings.GetValueOrDefault(NiKey, NiDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(NiKey, value))
                    AppSettings.Save();
            }
        }

        public static string From
        {
            get
            {
                return AppSettings.GetValueOrDefault(FromKey, FromDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(FromKey, value))
                    AppSettings.Save();
            }
        }
        public static string Target
        {
            get
            {
                return AppSettings.GetValueOrDefault(TargetKey, TargetDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(TargetKey, value))
                    AppSettings.Save();
            }
        }

        public static string To
        {
            get
            {
                return AppSettings.GetValueOrDefault(ToKey, ToDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(ToKey, value))
                    AppSettings.Save();
            }
        }

        public static string Ni2
        {
            get
            {
                return AppSettings.GetValueOrDefault(Ni2Key, Ni2Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(Ni2Key, value))
                    AppSettings.Save();
            }
        }

        public static string From2
        {
            get
            {
                return AppSettings.GetValueOrDefault(From2Key, From2Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(From2Key, value))
                    AppSettings.Save();
            }
        }
        public static string Target2
        {
            get
            {
                return AppSettings.GetValueOrDefault(Target2Key, Target2Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(Target2Key, value))
                    AppSettings.Save();
            }
        }

        public static string To2
        {
            get
            {
                return AppSettings.GetValueOrDefault(To2Key, To2Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(To2Key, value))
                    AppSettings.Save();
            }
        }

        public static string Ni3
        {
            get
            {
                return AppSettings.GetValueOrDefault(Ni3Key, Ni3Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(Ni3Key, value))
                    AppSettings.Save();
            }
        }

        public static string From3
        {
            get
            {
                return AppSettings.GetValueOrDefault(From3Key, From3Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(From3Key, value))
                    AppSettings.Save();
            }
        }
        public static string Target3
        {
            get
            {
                return AppSettings.GetValueOrDefault(Target3Key, Target3Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(Target3Key, value))
                    AppSettings.Save();
            }
        }

        public static string To3
        {
            get
            {
                return AppSettings.GetValueOrDefault(To3Key, To3Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(To3Key, value))
                    AppSettings.Save();
            }
        }

        public static string AllowedIP
        {
            get
            {
                return AppSettings.GetValueOrDefault(AllowedIPKey, AllowedIPDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(AllowedIPKey, value))
                    AppSettings.Save();
            }
        }

        public static string Bits
        {
            get
            {
                return AppSettings.GetValueOrDefault(BitsKey, BitsDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(BitsKey, value))
                    AppSettings.Save();
            }
        }

        public static string AllowedIP2
        {
            get
            {
                return AppSettings.GetValueOrDefault(AllowedIP2Key, AllowedIP2Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(AllowedIP2Key, value))
                    AppSettings.Save();
            }
        }

        public static string Bits2
        {
            get
            {
                return AppSettings.GetValueOrDefault(Bits2Key, Bits2Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(Bits2Key, value))
                    AppSettings.Save();
            }
        }

        public static string AllowedIP3
        {
            get
            {
                return AppSettings.GetValueOrDefault(AllowedIP3Key, AllowedIP3Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(AllowedIP3Key, value))
                    AppSettings.Save();
            }
        }

        public static string Bits3
        {
            get
            {
                return AppSettings.GetValueOrDefault(Bits3Key, Bits3Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(Bits3Key, value))
                    AppSettings.Save();
            }
        }

        public static string AllowedIP4
        {
            get
            {
                return AppSettings.GetValueOrDefault(AllowedIP4Key, AllowedIP4Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(AllowedIP4Key, value))
                    AppSettings.Save();
            }
        }

        public static string Bits4
        {
            get
            {
                return AppSettings.GetValueOrDefault(Bits4Key, Bits4Default);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(Bits4Key, value))
                    AppSettings.Save();
            }
        }

        public static bool RequiresAP
        {
            get
            {
                return AppSettings.GetValueOrDefault(RequiresAPKey, RequiresAPDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(RequiresAPKey, value))
                    AppSettings.Save();
            }
        }

        public static bool ExitSettings
        {
            get
            {
                return AppSettings.GetValueOrDefault(ExitSettingsKey, ExitSettingsDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(ExitSettingsKey, value))
                    AppSettings.Save();
            }
        }

        public static string DriverConfigPath
        {
            get
            {
                return AppSettings.GetValueOrDefault(DriverConfigPathKey, DriverConfigPathDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(DriverConfigPathKey, value))
                    AppSettings.Save();
            }
        }

        public static string ServiceAlertPath
        {
            get
            {
                return AppSettings.GetValueOrDefault(ServiceAlertPathKey, ServiceAlertPathDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(ServiceAlertPathKey, value))
                    AppSettings.Save();
            }
        }

        public static string VarPath
        {
            get
            {
                return AppSettings.GetValueOrDefault(VarPathKey, VarPathDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(VarPathKey, value))
                    AppSettings.Save();
            }
        }

        public static string UpdateScriptFileName
        {
            get
            {
                return AppSettings.GetValueOrDefault(UpdateScriptFileNameKey, UpdateScriptFileNameDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(UpdateScriptFileNameKey, value))
                    AppSettings.Save();
            }
        }

        public static string FirmwareExtension
        {
            get
            {
                return AppSettings.GetValueOrDefault(FirmwareExtensionKey, FirmwareExtensionDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(FirmwareExtensionKey, value))
                    AppSettings.Save();
            }
        }

        public static string DeviceMask
        {
            get
            {
                return AppSettings.GetValueOrDefault(DeviceMaskKey, DeviceMaskDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(DeviceMaskKey, value))
                    AppSettings.Save();
            }
        }

        public static string VersionPath
        {
            get
            {
                return AppSettings.GetValueOrDefault(VersionPathKey, VersionPathDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(VersionPathKey, value))
                    AppSettings.Save();
            }
        }

        public static string RoutePatternVersion
        {
            get
            {
                return AppSettings.GetValueOrDefault(RoutePatternVersionKey, RoutePatternVersionDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(RoutePatternVersionKey, value))
                    AppSettings.Save();
            }
        }

        public static string VehicleConfigVersion
        {
            get
            {
                return AppSettings.GetValueOrDefault(VehicleConfigVersionKey, VehicleConfigVersionDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(VehicleConfigVersionKey, value))
                    AppSettings.Save();
            }
        }


        public static string FirmwarePath
        {
            get
            {
                return AppSettings.GetValueOrDefault(FirmwarePathKey, FirmwarePathDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(FirmwarePathKey, value))
                    AppSettings.Save();
            }
        }

        public static string VehicleConfigPath
        {
            get
            {
                return AppSettings.GetValueOrDefault(VehicleConfigPathKey, VehicleConfigPathDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(VehicleConfigPathKey, value))
                    AppSettings.Save();
            }
        }

        public static string VehicleRouteReasourcePath
        {
            get
            {
                return AppSettings.GetValueOrDefault(VehicleRouteReasourcePathKey, VehicleRouteReasourcePathDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(VehicleRouteReasourcePathKey, value))
                    AppSettings.Save();
            }
        }

        public static string MovingHysteresis
        {
            get
            {
                return AppSettings.GetValueOrDefault(MovingHysteresisKey, MovingHysteresisDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(MovingHysteresisKey, value))
                    AppSettings.Save();
            }
        }

        public static string MovingRate
        {
            get
            {
                return AppSettings.GetValueOrDefault(MovingRateKey, MovingRateDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(MovingRateKey, value))
                    AppSettings.Save();
            }
        }

        public static string MovingThreshold
        {
            get
            {
                return AppSettings.GetValueOrDefault(MovingThresholdKey, MovingThresholdDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(MovingThresholdKey, value))
                    AppSettings.Save();
            }
        }

        public static string StationaryRate
        {
            get
            {
                return AppSettings.GetValueOrDefault(StationaryRateKey, StationaryRateDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(StationaryRateKey, value))
                    AppSettings.Save();
            }
        }
        public static string CommsAddress
        {
            get
            {
                return AppSettings.GetValueOrDefault(CommsAddressKey, CommsAddressDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(CommsAddressKey, value))
                    AppSettings.Save();
            }
        }

        public static string CANOutPort
        {
            get
            {
                return AppSettings.GetValueOrDefault(CANOutPortKey, CANOutPortDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(CANOutPortKey, value))
                    AppSettings.Save();
            }
        }
        public static string CANInPort
        {
            get
            {
                return AppSettings.GetValueOrDefault(CANInPortKey, CANInPortDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(CANInPortKey, value))
                    AppSettings.Save();
            }
        }

        public static bool UseCANBridge
        {
            get
            {
                return AppSettings.GetValueOrDefault(UseCANBridgeKey, UseCANBridgeDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(UseCANBridgeKey, value))
                    AppSettings.Save();
            }
        }

        public static string CANServerName
        {
            get
            {
                return AppSettings.GetValueOrDefault(CANServerNameKey, CANServerNameDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(CANServerNameKey, value))
                    AppSettings.Save();
            }
        }

        public static string RxPort
        {
            get
            {
                return AppSettings.GetValueOrDefault(RxPortKey, RxPortDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(RxPortKey, value))
                    AppSettings.Save();
            }
        }

        public static string TxPort
        {
            get
            {
                return AppSettings.GetValueOrDefault(TxPortKey, TxPortDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(TxPortKey, value))
                    AppSettings.Save();
            }
        }
        /// <summary>
        /// Gets or sets the RTTServer. 
        /// </summary>
        /// <value>The rttServer.</value>
        public static string RTTServer
        {
            get
            {
                return AppSettings.GetValueOrDefault(RTTServerKey, RTTServerDefault);
            }
            set
            {
                //if value has changed then save it!
                if (AppSettings.AddOrUpdateValue(RTTServerKey, value))
                    AppSettings.Save();
            }
        }

        private class SettingsHelper
		{
			public static ISharedPreferences SharedPreferences { get; set; }
		    private static ISharedPreferencesEditor SharedPreferencesEditor { get; set; }
		    private readonly object locker = new object();

			public SettingsHelper()
			{
				SharedPreferences = PreferenceManager.GetDefaultSharedPreferences(Application.Context);
				SharedPreferencesEditor = SharedPreferences.Edit();

			}

		    /// <summary>
		    /// Gets the current value or the default that you specify.
		    /// </summary>
		    /// <typeparam name="T">Vaue of t (bool, int, float, long, string)</typeparam>
		    /// <param name="key">Key for settings</param>
		    /// <param name="defaultValue">default value if not set</param>
		    /// <returns>Value or default</returns>
		    public T GetValueOrDefault<T>(string key, T defaultValue = default(T))
		    {
			    lock (locker)
			    {
				    Type typeOf = typeof(T);
				    if (typeOf.IsGenericType && typeOf.GetGenericTypeDefinition() == typeof(Nullable<>))
				    {
					    typeOf = Nullable.GetUnderlyingType(typeOf);
				    }
				    object value = null;
				    var typeCode = Type.GetTypeCode(typeOf);
				    switch (typeCode)
				    {
				    case TypeCode.Boolean:
					    value = SharedPreferences.GetBoolean(key, Convert.ToBoolean(defaultValue));
					    break;
				    case TypeCode.Int64:
						    value = SharedPreferences.GetLong(key, Convert.ToInt64(defaultValue));
					    break;
				    case TypeCode.String:
                        value = SharedPreferences.GetString(key, Convert.ToString(defaultValue));
                        break;
				    case TypeCode.Int32:
					    value = SharedPreferences.GetInt(key, Convert.ToInt32(defaultValue));
                        break;
                    case TypeCode.UInt16:
                        value = SharedPreferences.GetInt(key, Convert.ToUInt16(defaultValue));
                        break;
				    case TypeCode.Single:
					    value = SharedPreferences.GetFloat(key, Convert.ToSingle(defaultValue));
                        break;
                    case TypeCode.Double:
                        value = SharedPreferences.GetFloat(key, Convert.ToSingle(defaultValue));
                        break;
				    case TypeCode.DateTime:
					    var ticks = SharedPreferences.GetLong(key, -1);
					    if (ticks == -1)
						    value = defaultValue;
					    else
						    value = new DateTime(ticks);
					    break;
				    }

				    return ((null != value) ? (T)value : defaultValue);
			    }
		    }

		    /// <summary>
		    /// Adds or updates a value
		    /// </summary>
		    /// <param name="key">key to update</param>
		    /// <param name="value">value to set</param>
		    /// <returns>True if added or update and you need to save</returns>
		    public bool AddOrUpdateValue(string key, object value)
		    {
			    lock (locker)
			    {
				    Type typeOf = value.GetType();
				    if (typeOf.IsGenericType && typeOf.GetGenericTypeDefinition() == typeof(Nullable<>))
				    {
					    typeOf = Nullable.GetUnderlyingType(typeOf);
				    }
				    var typeCode = Type.GetTypeCode(typeOf);
				    switch (typeCode)
				    {
				    case TypeCode.Boolean:
					    SharedPreferencesEditor.PutBoolean(key, Convert.ToBoolean(value));
					    break;
				    case TypeCode.Int64:
						    SharedPreferencesEditor.PutLong(key, Convert.ToInt64(value));
					    break;
				    case TypeCode.String:
					    SharedPreferencesEditor.PutString(key, Convert.ToString(value));
					    break;
				    case TypeCode.Int32:
					    SharedPreferencesEditor.PutInt(key, Convert.ToInt32(value));
					    break;
				    case TypeCode.Single:
					    SharedPreferencesEditor.PutFloat(key, Convert.ToSingle(value));
					    break;
				    case TypeCode.DateTime:
					    SharedPreferencesEditor.PutLong(key, ((DateTime)(object)value).Ticks);
					    break;
				    }
			    }

			    return true;
		    }

		    /// <summary>
		    /// Saves out all current settings
		    /// </summary>
		    public void Save()
		    {
			    lock (locker)
			    {
				    SharedPreferencesEditor.Commit();
			    }
		    }

            
        }

  }
}