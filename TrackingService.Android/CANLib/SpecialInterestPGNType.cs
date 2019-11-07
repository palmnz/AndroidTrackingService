namespace CANLib
{
	/// <summary>
	/// Parameter group numbers
	/// </summary>
	/// <remarks>
	/// Numbers less than 16 bits (65535) are the main PGNs
	/// Numbers greater than 16 bits (65535) belong to special intrest groups
	/// </remarks>
	public enum SpecialInterestPGNType : uint
	{
		/// <summary>Request/Command/Acknowledge group function</summary>
		NMEA = 126208,
		/// <summary>PGN List - Transmit/Receive PGN's group function</summary>
		PGNList = 126464,
		/// <summary>Alert</summary>
		Alert = 126983,
		/// <summary>Alert Response</summary>
		AlertResponse = 126984,
		/// <summary>Alert Text</summary>
		AlertText = 126985,
		/// <summary>Alert Configuration</summary>
		AlertConfiguration = 126986,
		/// <summary>Alert Threshold</summary>
		AlertThreshold = 126987,
		/// <summary>Alert Value</summary>
		AlertValue = 126988,
		/// <summary>System Time</summary>
		SystemTime = 126992,
		/// <summary>Product Information</summary>
		ProductInformation = 126996,
		/// <summary>Configuration Information</summary>
		ConfigurationInformation = 126998,
		/// <summary>Heading/Track Control</summary>
		HeadingTrackControl = 127237,
		/// <summary>Rudder</summary>
		Rudder = 127245,
		/// <summary>Vessel Heading</summary>
		VesselHeading = 127250,
		/// <summary>Rate of Turn</summary>
		RateOfTurn = 127251,
		/// <summary>Attitude</summary>
		Attitude = 127257,
		/// <summary>Magnetic Variation</summary>
		MagneticVariation = 127258,
		/// <summary>Engine Parameters, Rapid Update</summary>
		EngineParametersRapidUpdate = 127488,
		/// <summary>Engine Parameters, Dynamic</summary>
		EngineParametersDynamic = 127489,
		/// <summary>Transmission Parameters, Dynamic</summary>
		TransmissionParametersDynamic = 127493,
		/// <summary>Trip Parameters, Vessel</summary>
		TripParametersVessel = 127496,
		/// <summary>Trip Parameters, Small Craft</summary>
		TripParametersSmallCraft = 127497,
		/// <summary>Engine Parameters, Static</summary>
		EngineParametersStatic = 127498,
		/// <summary>Binary Switch Bank Status</summary>
		BinarySwitchBankStatus = 127501,
		/// <summary>Switch Bank Control</summary>
		SwitchBankControl = 127502,
		/// <summary>AC Input Status</summary>
		ACInputStatus = 127503,
		/// <summary>AC Output Status</summary>
		ACOutputStatus = 127504,
		/// <summary>Fluid Level</summary>
		FluidLevel = 127505,
		/// <summary>DC Detailed Status</summary>
		DCDetailedStatus = 127506,
		/// <summary>Charger Status</summary>
		ChargerStatus = 127507,
		/// <summary>Battery Status</summary>
		BatteryStatus = 127508,
		/// <summary>Inverter Status</summary>
		InverterStatus = 127509,
		/// <summary>Charger Configuration Status</summary>
		ChargerConfigurationStatus = 127510,
		/// <summary>Inverter Configuration Status</summary>
		InverterConfigurationStatus = 127511,
		/// <summary>AGS Configuration Status</summary>
		AGSConfigurationStatus = 127512,
		/// <summary>Battery Configuration Status</summary>
		BatteryConfigurationStatus = 127513,
		/// <summary>AGS Status</summary>
		AGSStatus = 127514,
		/// <summary>Speed</summary>
		Speed = 128259,
		/// <summary>Water Depth</summary>
		WaterDepth = 128267,
		/// <summary>Distance Log</summary>
		DistanceLog = 128275,
		/// <summary>Tracked Target Data</summary>
		TrackedTargetData = 128520,
		/// <summary>Position, Rapid Update</summary>
		PositionRapidUpdate = 129025,
		/// <summary>COG & SOG, Rapid Update</summary>
		COG_SOGRapidUpdate = 129026,
		/// <summary>Position Delta, High Precision Rapid Update</summary>
		PositionDeltaHighPrecisionRapidUpdate = 129027,
		/// <summary>Altitude Delta, High Precision Rapid Update</summary>
		AltitudeDeltaHighPrecisionRapidUpdate = 129028,
		/// <summary>GNSS Position Data</summary>
		GNSSPositionData = 129029,
		/// <summary>Time & Date</summary>
		TimeDate = 129033,
		/// <summary>AIS Class A Position Report</summary>
		AISClassAPositionReport = 129038,
		/// <summary>AIS Class B Position Report</summary>
		AISClassBPositionReport = 129039,
		/// <summary>AIS Class B Extended Position Report</summary>
		AISClassBExtendedPositionReport = 129040,
		/// <summary>Datum</summary>
		Datum = 129044,
		/// <summary>User Datum Settings</summary>
		UserDatumSettings = 129045,
		/// <summary>Cross Track Error</summary>
		CrossTrackError = 129283,
		/// <summary>Navigation Data</summary>
		NavigationData = 129284,
		/// <summary>Navigation - Route/WP information</summary>
		NavigationRouteWPinformation = 129285,
		/// <summary>Set & Drift, Rapid Update</summary>
		SetDriftRapidUpdate = 129291,
		/// <summary>Time to/from Mark</summary>
		TimeToFromMark = 129301,
		/// <summary>Bearing and Distance between two Marks</summary>
		BearingAndDistanceBetweenTwoMarks = 129302,
		/// <summary>GNSS Control Status</summary>
		GNSSControlStatus = 129538,
		/// <summary>GNSS DOPs</summary>
		GNSSDOPs = 129539,
		/// <summary>GNSS Sats in View</summary>
		GNSSSatsInView = 129540,
		/// <summary>GPS Almanac Data</summary>
		GPSAlmanacData = 129541,
		/// <summary>GNSS Pseudorange Noise Statistics</summary>
		GNSSPseudorangeNoiseStatistics = 129542,
		/// <summary>GNSS RAIM Output</summary>
		GNSSRAIMOutput = 129545,
		/// <summary>GNSS RAIM Settings</summary>
		GNSSRAIMSettings = 129546,
		/// <summary>GNSS Pseudorange Error Statistics</summary>
		GNSSPseudorangeErrorStatistics = 129547,
		/// <summary>DGNSS Corrections</summary>
		DGNSSCorrections = 129549,
		/// <summary>GNSS Differential Correction Receiver Interface</summary>
		GNSSDifferentialCorrectionReceiverInterface = 129550,
		/// <summary>GNSS Differential Correction Receiver Signal</summary>
		GNSSDifferentialCorrectionReceiverSignal = 129551,
		/// <summary>GLONASS Almanac Data</summary>
		GLONASSAlmanacData = 129556,
		/// <summary>AIS DGNSS Broadcast Binary Message</summary>
		AISDGNSSBroadcastBinaryMessage = 129792,
		/// <summary>AIS UTC and Date Report</summary>
		AISUTCDateReport = 129793,
		/// <summary>AIS Class A Static and Voyage Related Data</summary>
		AISClassAStaticAndVoyageRelatedData = 129794,
		/// <summary>AIS Addressed Binary Message</summary>
		AISAddressedBinaryMessage = 129795,
		/// <summary>AIS Acknowledge</summary>
		AISAcknowledge = 129796,
		/// <summary>AIS Binary Broadcast Message</summary>
		AISBinaryBroadcastMessage = 129797,
		/// <summary>AIS SAR Aircraft Position Report</summary>
		AISSARAircraftPositionReport = 129798,
		/// <summary>Radio Frequency/Mode/Power</summary>
		RadioFrequencyModePower = 129799,
		/// <summary>AIS UTC/Date Inquiry</summary>
		AISUTCDateInquiry = 129800,
		/// <summary>AIS Addressed Safety Related Message</summary>
		AISAddressedSafetyRelatedMessage = 129801,
		/// <summary>AIS Safety Related Broadcast Message</summary>
		AISSafetyRelatedBroadcastMessage = 129802,
		/// <summary>AIS Interrogation</summary>
		AISInterrogation = 129803,
		/// <summary>AIS Assignment Mode Command</summary>
		AISAssignmentModeCommand = 129804,
		/// <summary>AIS Data Link Management Message</summary>
		AISDataLinkManagementMessage = 129805,
		/// <summary>AIS Channel Management</summary>
		AISChannelManagement = 129806,
		/// <summary>AIS Class B Group Assignment</summary>
		AISClassBGroupAssignment = 129807,
		/// <summary>DSC Call Information</summary>
		DSCCallInformation = 129808,
		/// <summary>AIS Class B "CS" Static Data Report, Part A</summary>
		AISClassBStaticDataReportPartA = 129809,
		/// <summary>AIS Class B "CS" Static Data Report, Part A</summary>
		AISClassBStaticDataReportPartB = 129810,
		/// <summary>Loran-C TD Data</summary>
		LoranCTDData = 130052,
		/// <summary>Loran-C Range Data</summary>
		LoranCRangeData = 130053,
		/// <summary>Loran-C Signal Data</summary>
		LoranSignalData = 130054,
		/// <summary>Label</summary>
		Label = 130060,
		/// <summary>Channel Source Configuration</summary>
		ChannelSourceConfiguration = 130061,
		/// <summary>Route and WP Service - Database List</summary>
		RouteAndWPServiceDatabaseList = 130064,
		/// <summary>Route and WP Service - Route List</summary>
		RouteAndWPServiceRouteList = 130065,
		/// <summary>Route and WP Service - Route/WP-List Attributes</summary>
		RouteAndWPServiceRouteWPListAttributes = 130066,
		/// <summary>Route and WP Service - Route - WP Name & Position</summary>
		RouteAndWPServiceRouteWPNamePosition = 130067,
		/// <summary>Route and WP Service - Route - WP Name</summary>
		RouteAndWPServiceRouteWPName = 130068,
		/// <summary>Route and WP Service - XTE Limit & Navigation Method</summary>
		RouteAndWPServiceXTELimitNavigationMethod = 130069,
		/// <summary>Route and WP Service - WP Comment</summary>
		RouteAndWPServiceWPComment = 130070,
		/// <summary>Route and WP Service - Route Comment</summary>
		RouteAndWPServiceRouteComment = 130071,
		/// <summary>Route and WP Service - Database Comment</summary>
		RouteAndWPServiceDatabaseComment = 130072,
		/// <summary>Route and WP Service - Radius of Turn</summary>
		RouteAndWPServiceRadiusOfTurn = 130073,
		/// <summary>Route and WP Service - WP List - WP Name & Position</summary>
		RouteAndWPServiceWPListWPNamePosition = 130074,
		/// <summary>Wind Data</summary>
		WindData = 130306,
		/// <summary>Deprecated Environmental Parameters</summary>
		DeprecatedEnvironmentalParameters = 130310,
		/// <summary>Environmental Parameters</summary>
		EnvironmentalParameters = 130311,
		/// <summary>Temperature</summary>
		Temperature = 130312,
		/// <summary>Humidity</summary>
		Humidity = 130313,
		/// <summary>Humidity</summary>
		Humidity2 = 130314,
		/// <summary>Set Pressure</summary>
		SetPressure = 130315,
		/// <summary>Temperature- Extended Range</summary>
		TemperatureExtendedRange = 130316,
		/// <summary>Tide Station Data</summary>
		TideStationData = 130320,
		/// <summary>Salinity Station Data</summary>
		SalinityStationData = 130321,
		/// <summary>Current Station Data</summary>
		CurrentStationData = 130322,
		/// <summary>Meteorological Station Data</summary>
		MeteorologicalStationData = 130323,
		/// <summary>Moored Buoy Station Data</summary>
		MooredBuoyStationData = 130324,
		/// <summary>Payload Mass</summary>
		PayloadMass = 130560,
		/// <summary>Small Craft Status</summary>
		SmallCraftStatus = 130576,
		/// <summary>Direction Data</summary>
		DirectionData = 130577,
		/// <summary>Vessel Speed Components</summary>
		VesselSpeedComponents = 130578,
	};
}
