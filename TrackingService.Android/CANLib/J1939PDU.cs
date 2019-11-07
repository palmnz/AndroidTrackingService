using System;

namespace CANLib
{
	/// <summary>
	/// Can Id representation of J1939 Protocol Data Unit (PDU)
	/// Priority	Reserved	Data page	PDU format	PDU specific	Source Address
	///	3 bits		1 bit		1 bit		8 bits		8 bits			8 bits
	/// </summary>
	public class J1939PDU
	{
		public const uint EDPMask = 0x02000000;
		public const uint EPMask = 0x01000000;
		public const uint PGNUpperByteMask = 0x02ffffff;
		public const uint PDUFormatMask = 0xff0000;
		public const uint PDUSpecificMask = 0xff00;
		public const uint PDU01MaxPFValue = 0xef0000;
		const uint LastByteMask = 0xff;
		const byte FlagByteMask = 0x03;
		const uint TP_BAMMask = 0xec00;
		const uint TP_CMMMask = 0xeb00;
		public byte Priority = 0;
		public bool Reserved = false;
		public bool EP = false;
		public byte PF = 0;
		public byte Destination { get { return PF; } }
		public byte PS = 0;
		public byte SA = 0;
		public ushort PGN = 0;
		public uint ExtendedPGN = 0;
		public uint Address = 0;

		/// <summary>
		/// PDU format has detination addressing (PDU1)
		/// </summary>
		public bool PDU1 { get { return (PF < 240); } }
		/// <summary>
		/// PDU format is broardcast (PDU2)
		/// </summary>
		public bool PDU2 { get { return (PF > 239); } }

		/// <summary>
		/// CAN ID is J1939 format (probably ISO-15765)
		/// </summary>
		/// <param name="canId">CAN frame Id</param>
		/// <remarks>
		/// Priority	Reserved	Data page	PDU format	PDU specific	Source Address
		///	3 bits		1 bit		1 bit		8 bits		8 bits			8 bits
		///	Extended Data Page bit 25	Data Page bit 24
		///		CAN ID bit 25			CAN ID bit 24		Description
		///				0					0			SAE J1939 page 0 PGNs
		///				0					1			SAE J1939 page 1 PGNs
		///				1					0			SAE J1939 reserved
		///				1					1			ISO 15765-3 defined
		/// </remarks>
		/// <returns>True if PDU format byte is broadcast</returns>
		public static bool IsJ1939(uint canId)
		{
			return ((canId & (EPMask | EDPMask)) < (EPMask | EDPMask));
		}

		/// <summary>
		/// PDU format is broardcast or global address
		/// </summary>
		/// <param name="canId">CAN frame Id</param>
		/// <remarks>
		/// Priority	Reserved	Data page	PDU format	PDU specific	Source Address
		///	3 bits		1 bit		1 bit		8 bits		8 bits			8 bits
		/// </remarks>
		/// <returns>True if PDU specific (destination address) byte is broadcast (0xff)</returns>
		public static bool IsNotPrivate(uint canId)
		{
			return (((canId & PDUFormatMask) > PDU01MaxPFValue) || ((canId & PDUSpecificMask) == PDUSpecificMask));
		}

		/// <summary>
		/// PDU format is broardcast transport protocol
		/// </summary>
		/// <param name="canId">CAN frame Id</param>
		/// <remarks>
		/// Priority	Reserved	Data page	PDU format	PDU specific	Source Address
		///	3 bits		1 bit		1 bit		8 bits		8 bits			8 bits
		/// </remarks>
		/// <returns>True if PDU specific (destination address) byte is broadcast (0xff)</returns>
		public static bool IsPDU2(uint canId)
		{
			return ((canId & PDUFormatMask) > PDU01MaxPFValue);
		}

		/// <summary>
		/// PDU format is broardcast (PDU2)
		/// </summary>
		/// <param name="canId">CAN frame Id</param>
		/// <remarks>
		/// Priority	Reserved	Data page	PDU format	PDU specific	Source Address
		///	3 bits		1 bit		1 bit		8 bits		8 bits			8 bits
		/// </remarks>
		/// <returns>True if PDU format byte is broadcast</returns>
		public static bool IsPDU1Broadcast(uint canId)
		{
			return (((canId & PDUFormatMask) <= PDU01MaxPFValue) && ((canId & PDUSpecificMask) == PDUSpecificMask));
		}

		/// <summary>
		/// PDU format is destination (PDU1)
		/// </summary>
		/// <param name="canId">CAN frame Id</param>
		/// <remarks>
		/// Priority	Reserved	Data page	PDU format	PDU specific	Source Address
		///	3 bits		1 bit		1 bit		8 bits		8 bits			8 bits
		/// </remarks>
		/// <returns>True if PDU format byte is destination</returns>
		public static bool IsPDU1(uint canId)
		{
			return ((canId & PDUFormatMask) <= PDU01MaxPFValue);
		}

		/// <summary>
		/// The PGN is in the extended special interest range
		/// </summary>
		/// <param name="canId"></param>
		/// <returns>True if it is a J1939 format and the extended page bit is set.</returns>
		public static bool IsExtendedPGN(uint canId)
		{
			return (IsJ1939(canId) && ((canId & EPMask) != 0));
		}

		/// <summary>
		/// Parses a raw CAN frame Id into Protocol Data Unit (PDU) fields.
		/// </summary>
		/// <param name="canId">CAN frame Id</param>
		/// <remarks>
		/// Priority	Reserved	Data page	PDU format	PDU specific	Source Address
		///	3 bits		1 bit		1 bit		8 bits		8 bits			8 bits
		/// </remarks>
		/// <returns>The parsed PDU elements or NULL</returns>
		public static J1939PDU ParsePDU(uint canId)
		{
			J1939PDU pdu = new J1939PDU()
			{
				Address = canId,
				Priority = (byte)(canId >> 26),
				Reserved = (canId & EDPMask) != 0,
				EP = (canId & EPMask) != 0,

				PF = (byte)((canId >> 16) & LastByteMask),
				PS = (byte)((canId >> 8) & LastByteMask),
				SA = (byte)(canId & LastByteMask),
				PGN = (ushort)((canId >> 8) & 0xffff),
				ExtendedPGN = ((canId >> 8) & 0x1ffff)
			};
			return pdu;
		}

		/// <summary>
		/// Formulates the CAN address and payload to make a J1939 request for a PGN
		/// </summary>
		/// <param name="priority">Priority to use.</param>
		/// <param name="pgn">PGN to be requested</param>
		/// <param name="sa">Source address</param>
		/// <param name="address">CAN address to use for the request.</param>
		/// <returns>Payload for the CAN message.</returns>
		public static byte[] RequestPGN(byte priority, ushort pgn, byte sa, out uint address)
		{
			byte[] payload = new byte[3];
			payload[0] = (byte)(pgn & 0xff);
			payload[1] = (byte)(pgn >> 8);
			payload[2] = 0;

			address = ((uint)(priority & 0x7)) << 26;
			address += (((ushort)PGNType.RQST) << 8);
			address |= 0xff00; // request from all sources
			address += sa;

			return payload;
		}

		/// <summary>
		/// Formulates the CAN address and payload to make a J1939 request for a PGN
		/// </summary>
		/// <param name="address">CAN address to use for the request.</param>
		/// <returns>Payload for the CAN message.</returns>
		/// <remarks>
		/// Uses a default priority of 6.
		/// Uses a default source address of 251 (On-Board Data Logger)
		/// </remarks>
		public static byte[] RequestPGN(ushort pgn, out uint address)
		{
			byte[] payload = new byte[3];
			payload[0] = (byte)(pgn & 0xff);
			payload[1] = (byte)(pgn >> 8);
			payload[2] = 0;

			address = 0x18eafffb;

			return payload;
		}

		//public static J1939PDU ParsePDU(string canId)
		//{
		//	byte[] b = HexStringToByteArray(canId);

		//	return ParsePDU(b);
		//}

		//public static J1939PDU ParsePDU(byte[] canId)
		//{
		//	J1939PDU pdu = new J1939PDU();
		//	pdu.Reserved = (canId[0] & 0x02) == 0x02;
		//	pdu.EP = (canId[0] & 0x01) == 0x01;
		//	pdu.Priority = (canId[3] >> 2);
		//	pdu.PF = canId[1];
		//	pdu.PS = canId[2];
		//	pdu.SA = canId[3];
		//	pdu.PGN = (ushort)(((canId[0] & FlagByteMask) << 8) + canId[1]);
		//	pdu.PGN = (ushort)((pdu.PGN << 8) + canId[2]);

		//	return pdu;
		//}

		public static byte[] HexStringToByteArray(string hexString)
		{
			if (hexString.Length % 2 != 0)
				hexString = '0' + hexString;
			int hexStringLength = hexString.Length;
			byte[] b = new byte[hexStringLength / 2];
			for (int i = 0; i < hexStringLength; i += 2)
			{
				int topChar = (hexString[i] > 0x40 ? hexString[i] - 0x37 : hexString[i] - 0x30) << 4;
				int bottomChar = hexString[i + 1] > 0x40 ? hexString[i + 1] - 0x37 : hexString[i + 1] - 0x30;
				b[i / 2] = Convert.ToByte(topChar + bottomChar);
			}
			return b;
		}

		public override string ToString()
		{
			object[] args = { PGN, Priority, (Reserved ? '1' : '0'), (EP ? '1' : '0'), PF, PS, SA };
			return string.Format("PGN:{0} {1} {2}{3} PF 0x{4:x} PS 0x{5:x} SA 0x{6:x}", args);
		}
	}
}
