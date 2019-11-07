namespace CANLib
{
	public enum CNXMsgIds : uint
	{
		/// <summary>
		/// The ‘Manufacturing Test’ message is used in manufacturing test arrangements to confirm CAN operation.
		/// A 'Testjig' will periodically transmit the message, and the unit under test (UUT) is expected to receive the message, before reporting success.
		/// The 'Manufacturing Test' message has no data contents.
		/// Optionally, the UUT may reply with a test status; this message is not currently defined or implemented.
		/// </summary>
		ManufacturingTest = 0x100,
		/// <summary>
		/// The ‘Device Catalogue’ message is used to invoke a re-catalog of the devices on the vehicles CAN network.
		/// Each device connected to the CAN network is expected to respond with a 0x102-Product Id message.
		/// The 'Device Catalogue' message has no data contents.
		/// </summary>
		DeviceCatalogue = 0x101,
		/// <summary>
		/// The ‘Product Id’ message is used to announce the presence of a device on the CAN network.
		/// It is sent on startup, at 1 minute intervals, and in response to the 0x101-Device Catalogue message.
		/// The Equipment Mask and Equipment Status items are used to indicate the category of equipment supported by the device, and its operational status.
		/// The Product Id message shall contain 2, 4, 6 or 8 bytes of data.
		/// Bit	Description
		/// 0	Farebox
		/// 1	Headsign
		/// 2	NextStop
		/// 3	NextStop Display
		/// 4	Passenger Counter
		/// 5	Traffic Priority
		///
		///   7 6 5 4 3 2 1 0
		/// 0|    PROD       | Product ID
		/// 1|    BUILD      | Build Number
		/// 2|  EQUIPMASKa   | Equipment mask a
		/// 3| EQUIPSTATUSa  | Equipment status a
		/// 4|  EQUIPMASKb   | Equipment mask b
		/// 5| EQUIPSTATUSb  | Equipment status b
		/// 6|  EQUIPMASKc   | Equipment mask c
		/// 7| EQUIPSTATUSc  | Equipment status c
		/// </summary>
		ProductId = 0x102,
		/// <summary>
		/// The ‘Trip Progress’ message is used to inform the next stop announcement unit of changes to the vehicles current trip state.
		/// The message data is sourced from the server, and is typically sent once every two minutes,
		/// or upon certain changes (On/Off Route, Active/Inactive Trip, PathId change).
		/// The Path, Position and Trip Number elements are all optional, and will only be included on an as-required basis.
		///   7 6 5 4 3 2 1 0
		/// 0|     TYPE      | Message type
		///  |0|0|0|0|0|0|0|0| Trip Inactive
		///  |0|0|0|0|0|0|1|0| Off Route, Trip Active
		///  |0|0|0|0|0|0|1|1| On Route, Trip Active
		/// 1|    PATHa      | Path (LSB)
		/// 2|  POSNa  |PATHb| Position (LSB) Path (MSB)
		/// 3|     POSNb     | Position (MSB)
		/// 4|    TRIPNOa    | Trip number (LS)
		/// 5|    TRIPNOb    | 
		/// 6|    TRIPNOc    | Trip number (MS)
		/// </summary>
		TripProgress = 0x200,
		/// <summary>
		/// The ‘Fareset’ message is used to inform the ticket machine interface of changes to the vehicles current fareset.
		/// The message data is typically sourced from the next-stop announcement unit, and is sent upon changes.
		///   7 6 5 4 3 2 1 0
		/// 0|   FARESET     | Fareset
		/// </summary>
		Fareset = 0x201,
		/// <summary>
		/// The ‘Destination’ message is used to inform the head sign of changes to the vehicles current trip state.
		/// The message data is typically sourced from the next-stop announcement unit, and is typically sent change.
		/// The destination code is typically a 4 digit ASCII numeric string,
		/// although other lengths and hexadecimal may also be supported.
		///   7 6 5 4 3 2 1 0
		/// 0|    DESTa      | Destination (LS)
		///  |               | 
		/// n|    DESTn      | Destination (MS)
		/// </summary>
		Destination = 0x202,
		/// <summary>
		/// The ‘Route and Trip’ message is used to inform the farebox of changes to the vehicles current trip state.
		/// The message data is typically sourced from the next-stop announcement unit, and is typically sent upon change.
		/// The route code is typically a 4 digit ASCII alpha-numeric string, and should be null padded from the right for fewer characters.
		/// The Trip number is optional, and may be absent.
		/// In instances where the Trip Number is required, but the Route is not, the Route field should be filled with nulls.
		///   7 6 5 4 3 2 1 0
		/// 0|    ROUTEa     | Route (LS)
		/// 1|    ROUTEb     | 
		/// 2|    ROUTEc     |
		/// 3|    ROUTEd     | Route (MS)
		/// 4|    TRIPNOa    | Trip number (LS)
		/// 5|    TRIPNOb    | 
		/// 6|    TRIPNOc    | Trip number (MS)
		/// </summary>
		RouteTrip = 0x203,
		/// <summary>
		/// The ‘Trip None’ message replaces 0x200 Trip Progress
		/// Used to inform the next stop function of changes to the vehicles current trip state.
		/// The message contains no body.
		/// </summary>
		TripNone = 0x204,
		/// <summary>
		/// The ‘Trip Off Route’ message replaces 0x200 Trip Progress
		/// Used to inform the next stop function of changes to the vehicles current trip state.
		/// The message contains no body.
		/// </summary>
		TripOffRoute = 0x205,
		/// <summary>
		/// The ‘Trip On Route’ message replaces 0x200 Trip Progress.
		/// Used to inform the next stop function of changes to the vehicles current trip state.
		/// The message data is sourced from the server, and is typically sent once every two minutes, or upon certain changes,
		/// (On/Off Route, Active/Inactive Trip, PathId change).
		///   7 6 5 4 3 2 1 0
		/// 0|    PATHa      | Path (LSB)
		/// 1|  POSNa  |PATHb| Position (LSB) Path (MSB)
		/// 2|     POSNb     | Position (MSB)
		/// 3|    TRIPNOa    | Trip number (LS)
		/// 4|    TRIPNOb    | 
		/// 5|    TRIPNOc    | Trip number (MS)
		/// 6|     SVCSTa    | Service Start (LS)
		/// 7| | |  SVCSTb   | Service Start (MS)
		///  |0|0|           | Normal Running, Priority not required
		///  |0|1|           | Early Running, Priority not required
		///  |1|0|           | Reserved
		///  |1|1|           | Late Running, Priority Required
		/// </summary>
		TripOnRoute = 0x206,
		/// <summary>
		/// The ‘Logon Result’ message is generated by the server in response to a Logon/DriverId block.
		/// Parameter		Range	Description
		///	Logon State		0		Logon OK
		///					1		Logoff
		///					2		Logon Fail
		///
		///   7 6 5 4 3 2 1 0
		/// 0|  LOGONSTATE   | Logon State
		/// </summary>
		LogonResult = 0x207,
		/// <summary>
		/// The ‘GPS’ message is used to communicate current latitude, longitude and speed to any units requiring it.
		/// The message parameters are:
		/// Latitude in degrees * 100000, encoded into 25 bits. This gives approximately 1m resolution. The 25 bit value is a signed integer.
		/// Longitude in degrees * 100000, encoded into 26 bits. This gives approximately 1m resolution. The 26 bit value is a signed integer.
		/// Speed in m/s * 11, encoded into 9 bits. ie 1 bit is 1/11 m/s or 11 = 1 m/s.
		/// Very high speeds are capped at the maximum value.
		///   7 6 5 4 3 2 1 0
		/// 0|       |  FLAGS| Message type
		///  |       |0|0|0|0| No GPS
		///  |       |0|0|0|1| No Fix (Reported from GPS Rx)
		///  |       |0|0|1|0| Poor Fix (Filtered)
		///  |       |0|0|1|1| Good Fix
		///  | LATa  |x|x|x|x| Latitude (LS)
		/// 1|     LATb      | Latitude
		/// 2|     LATc      | Latitude
		/// 3|LONGa |  LATd  | Lonitude (LS), Latitude (MS)
		/// 4|     LONGb     | Longitude
		/// 5|     LONGc     | Longitude
		/// 6|S|    LONGd    | Speed (LS), Longitude (MS)
		/// 7|     SPEEDb    | Speed (MS)
		/// </summary>
		GPS = 0x210,
		/// <summary>
		/// The ‘Date Time’ message is used to communicate current date and time information to any units requiring it.
		/// This is derived from the GPS receiver, and will be sent once per GPS fix.
		/// No message will be sent when the GPS receiver has no fix.
		/// Parameter	Range	Description
		///	Day			1-31	Day of month
		/// Month		1-12	Month of year
		/// Year		0-99	Two digit year
		/// Time		0-86400	Seconds since midnight
		///
		///   7 6 5 4 3 2 1 0
		/// 0|       DAY     | Day of month
		/// 1|      MONTH    | Month of year
		/// 2|      YEAR     | Year (epoc 2000)
		/// 3|      TIMEa    | Time (LS)
		/// 4|      TIMEb    | 
		/// 5|x|x|x|x|x|x|x|T| Time (MS)
		/// </summary>
		DateTime = 0x211,
		/// <summary>
		/// The 'Digital Input State' message is used to communicate changes to digital or auxiliary inputs.
		/// 
		///   7 6 5 4 3 2 1 0
		/// 0|     FLAGS     | Flags
		///  | | | | | | | |1| Emergency/Panic/Driver Duress active
		///  | | | | | | | |0| Emergency/Panic/Driver Duress inactive
		///  | | | | | | |1| | Reserved active
		///  | | | | | | |0| | Reserved inactive
		///  | | | | | |1| | | Reserved active
		///  | | | | | |0| | | Reserved inactive
		///  | | | | |1| | | | Reserved active
		///  | | | | |0| | | | Reserved inactive
		///  | | | |1| | | | | Reserved active
		///  | | | |0| | | | | Reserved inactive
		///  | |1| | | | | | | Reserved active
		///  | |0| | | | | | | Reserved inactive
		///  |1| | | | | | | | Reserved active
		///  |0| | | | | | | | Reserved inactive
		/// 1|     MASK      | 
		///  | | | | | | | |1| Emergency/Panic/Driver Duress State Present
		///  | | | | | | | |0| Emergency/Panic/Driver Duress State Absent
		///  | | | | | | |1| | Reserved State Present
		///  | | | | | | |0| | Reserved State Absent
		///  | | | | | |1| | | Reserved State Present
		///  | | | | | |0| | | Reserved State Absent
		///  | | | | |1| | | | Reserved State Present
		///  | | | | |0| | | | Reserved State Absent
		///  | | | |1| | | | | Reserved State Present
		///  | | | |0| | | | | Reserved State Absent
		///  | |1| | | | | | | Reserved State Present
		///  | |0| | | | | | | Reserved State Absent
		///  |1| | | | | | | | Reserved State Present
		///  |0| | | | | | | | Reserved State Absent
		/// </summary>
		DigitalInputState = 0x212,
		/// <summary>
		/// The ‘Identifiers’ message is used to inform CAN devices of the various identifiers used on/by the vehicle.
		/// It is transmitted from the communications gateway on an as-required basis,
		/// typically initiated from the server on the RadioServer or CellularServer service start, or upon a vehicle reset event.
		/// The message parameters are:
		/// Parameter		Range		Description
		/// Comms Address	1-65535		Coms Address used by the Communication gateway (Radio or Cellular modem).
		/// CompanyTag		1-9			Company Identifier (optional) only present if known.
		///
		/// The ‘Unit info request’ message utilises the long frame type and includes some reserved bits (which should be set to 0).
		/// The frame format is:
		///   7 6 5 4 3 2 1 0
		/// 0|    COMMSa     | Comms Address (LSB)
		/// 1|    COMMSb     | Comms Address (MSB)
		/// 2|   COMPANYID   | Company Identifier
		/// </summary>
		Identifiers = 0x213,
		/// <summary>
		/// The 'Passenger Count Event' message is used to communicate changes to door state or auxiliary APC inputs.
		/// 
		///   7 6 5 4 3 2 1 0
		/// 0|     FLAGS     | Flags
		///  | | | | | | | |1| Front Door Closed
		///  | | | | | | | |0| Front Door Open
		///  | | | | | | |1| | Rear Door Closed
		///  | | | | | | |0| | Rear Door Open
		///  | | | | | |1| | | Third Door Closed
		///  | | | | | |0| | | Third Door Open
		///  | | | | |1| | | | Fourth Door Closed
		///  | | | | |0| | | | Fourth Door Open
		///  | | |0|0| | | | | Reserved
		///  | |0| | | | | | | Wheelchair Ramp Retracted
		///  | |1| | | | | | | Wheelchair Ramp Extended
		///  |0| | | | | | | | Bike Rack Retracted
		///  |1| | | | | | | | Bike Rack Extended
		/// 1|     MASK      | 
		///  | | | | | | | |1| Front Door State Present
		///  | | | | | | | |0| Front Door State Absent
		///  | | | | | | |1| | Rear Door State Present
		///  | | | | | | |0| | Rear Door State Absent
		///  | | | | | |1| | | Third Door State Present
		///  | | | | | |0| | | Third Door State Absent
		///  | | | | |1| | | | Fourth Door State Present
		///  | | | | |0| | | | Fourth Door State Absent
		///  | | |0|0| | | | | Reserved
		///  | |1| | | | | | | Wheelchair Ramp State Present
		///  | |0| | | | | | | Wheelchair Ramp State Absent
		///  |1| | | | | | | | Bike Rack State Present
		///  |0| | | | | | | | Bike Rack State Absent
		/// </summary>
		PassengerCountEvent = 0x220,
		/// <summary>
		/// The 'Passenger Load' message is used to update current boarding and alighting counts for a particular door.
		/// The message parameters are:
		/// Parameter	Range	Description
		/// Door		1		Front Door
		///				2		Rear Door
		///				3		Reserved for Third Door (not implemented)
		///				4		Reserved for Forth Door (not implemented)
		/// Boarding's	0-255	Rollover cumulative count of passenger boarding's for the specified door.
		/// Alighting's	0-255	Rollover cumulative count of passenger alighting's for the specified door.
		///
		///   7 6 5 4 3 2 1 0
		/// 0|     DOOR      | 
		/// 1|     BOARD     | Boardings
		/// 2|    ALIGHT     | Alightings
		/// </summary>
		PassengerLoad = 0x221,
		/// <summary>
		/// The reset loading message is used to zero the internal estimate of passenger loading.
		/// This allows for the case where APC has an offset between the boardings and alightings.
		/// The intention is to send this CAN message when we know the bus is actually empty.
		/// There is no body to this message.
		/// </summary>
		ResetLoading = 0x223,
		/// <summary>
		/// The ‘Passenger Boardings’ message is used to update current boarding and alighting counts for each door or any discontinuity that may occur in the passenger counts, due to device initialisation.
		/// The message parameters are:
		/// Parameter	Range		Description
		/// Boardings	0-255		Rollover cumulative count of passenger boardings for specified door.
		/// Alightings	0-255		Rollover cumulative count of passenger alightings for specified door.
		///
		///   7 6 5 4 3 2 1 0
		/// 0|    BOARD1     | Boardings
		/// 1|    ALIGHT1    | Alightings
		/// X|    BOARDX     | Boardings
		/// Y|    ALIGHTX    | Alightings
		/// </summary>
		PassengerBoardings = 0x224,
		/// <summary>
		/// The ‘Duress State’ message is used to communicate current state of the 'Panic switch.
		/// If a panic switch is fitted, the message should be repeated once every 10 seconds, and upon change of state.
		/// Parameter		Range	Description
		///	Duress State	0		Normal
		///					1		Driver is under duress - 'Panic!'
		///
		///   7 6 5 4 3 2 1 0
		/// 0|x|x|x|x|x|x|x|D| Duress State
		/// </summary>
		DuressState = 0x230,
		/// <summary>
		/// The ‘Driver Status’ message is sent by the MDT upon initiation by the driver.
		/// Parameter		Range	Description
		///	DriverStatus	1		Normal
		///					2		Out of Vehicle
		///					3		On a Break
		///
		///   7 6 5 4 3 2 1 0
		/// 0|  DRIVERSTATUS | Driver Status
		/// </summary>
		DriverStatus = 0x240,
		/// The ‘Driver Message Acknowledge’ message is sent by the MDT when the driver reads the corresponding message.
		/// 
		///   7 6 5 4 3 2 1 0
		/// 0|    MSGTAGa    | Message Tag (LS)
		/// 1|    MSGTAGb    | Message Tag (MS)
		/// </summary>
		DriverMessageAck = 0x241,
		/// <summary>
		/// Message ID's above 0x400 are for file and other block transfere opperations.
		/// The Block Id is the lower 8 bits of the MID
		/// The 11 bit Message ID field is partitioned to provide 256 Block Id's and 1024 Mailbox ID's,
		/// as per the following diagram.
		/// 10 9 8 7 6 5 4 3 2 1 0
		///  0|     MAILBOXID     |
		///  1|0|0|  BLOCKID      | Block Query
		///  1|0|1|  BLOCKID      | Block Query Response
		///  1|1|0|  BLOCKID      | Chunk 1
		///  1|1|1|  BLOCKID      | Chunk N
		///  The ‘Block Query’ message is used to request the current block state of an identified block.
		///  There are no message parameters.
		///  The Block Query Response' message is sent in response to the 'Block Query'.
		/// </summary>
		BlockQuery = 0x400,
		/// <summary>
		/// Message ID's above 0x400 are for file and other block transfere opperations.
		/// The Block Id is the lower 8 bits of the MID 
		/// The 11 bit Message ID field is partitioned to provide 256 Block Id's and 1024 Mailbox ID's,
		/// as per the following diagram.
		/// 10 9 8 7 6 5 4 3 2 1 0
		///  0|     MAILBOXID     |
		///  1|0|0|  BLOCKID      | Block Query
		///  1|0|1|  BLOCKID      | Block Query Response
		///  1|1|0|  BLOCKID      | Chunk 1
		///  1|1|1|  BLOCKID      | Chunk N
		///  The ‘Block Query Response’ message is sent in response to the 'Block Query' message.
		///  The message parameters are:
		///  Parameter			Range		Description
		///  Active CRC			2 Bytes		This is the CRC value of the currently active block.
		///									0 if no block is active.
		///  Offset Missing		4 Bytes		The offset of the first 40 byte chunk in the currently downloading block that has missing data.
		///									0xFFFFFFFF if no data is missing, or no download is in progress.
		///	 Version			1 Byte		Current resource version of the block/resource.
		///	
		///	The ‘Block Query Response’ message may contain 0 (if no active or downloading block),
		///	4 (Downloading, but not active block),
		///	6 (Active Block) or
		///	7 (Active Block with Resource Version) bytes of data.
		///
		///   7 6 5 4 3 2 1 0
		/// 0|     OFS a     | Offset LSB
		/// 1|     OFS b     | Offset
		/// 2|     OFS c     | Offset
		/// 3|     OFS d     | Offset MSB
		/// 4|      CRCa     | Active CRC (LS)
		/// 5|      CRCb     | Active CRC (MS)
		/// 6|      VER      | Version
		/// </summary>
		BlockQueryResponse = 0x500,
		/// <summary>
		/// The ‘Chunk 1’ message is used to initiate a block transfer and must be sent before any 'Chunk N' data is sent for a new block.
		/// It is possible for a 'Chunk 1' message to be sent without the size, version or flags fields; these fields being sent at a later time with another 'Chunk 1' message.
		/// The Block Id is the lower 8 bits of the MID.
		/// The message parameters are:
		/// Parameter	Range		Description
		/// CRC			2 Bytes		CRC of the whole block
		/// Size		4 Bytes		Total size of the whole block.
		/// Version		1 Byte		Resource version of the block/resource. The Version may or may not be present.
		/// 
		///   7 6 5 4 3 2 1 0
		/// 0|     CRCa      | Downloading CRC (LS)
		/// 1|     CRCb      | Downloading CRC (MS)
		/// 2|     FLAGS     | Flags
		///  |0|0|0|0|0|0|0|0| Version not present in Block Data
		///  |0|0|0|0|0|0|0|1| Version present in Block Data
		///  |0|0|0|0|0|0|0| | Filename not present in Block Data
		///  |0|0|0|0|0|0|1| | Filename present in Block Data
		/// 3|     SIZEa     | Size LSB
		/// 3|     SIZEb     | Size
		/// 3|     SIZEc     | Size
		/// 3|     SIZEd     | Size MSB
		/// 7|      VER      | Version
		/// </summary>
		BlockChunk1 = 0x600,
		/// <summary>
		/// The ‘Chunk N’ message is used to transfer any block data.
		/// Typically, a series of these will occur in quick succession, after a number of bytes of data have been received by the communication gateway.
		/// The Block Id is the lower 8 bits of the MID.
		/// The message parameters are:
		/// Parameter	Range		Description
		/// Offset		4 bytes		Address into in the block of the data.
		/// Data		4 bytes		4 bytes of data, which are to be added to the block.
		///
		///   7 6 5 4 3 2 1 0
		/// 0|     OFS a     | Offset LSB
		/// 1|     OFS b     | Offset
		/// 2|     OFS c     | Offset
		/// 3|     OFS d     | Offset MSB
		/// 4|     DATA1     | Data
		/// 5|     DATA2     | Data
		/// 6|     DATA3     | Data
		/// 7|     DATA4     | Data
		/// </summary>
		BlockChunkN = 0x700,
	};
}
