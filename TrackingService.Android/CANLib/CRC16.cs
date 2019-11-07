using System.IO;

namespace CANLib
{
	/// <summary>
	/// Provides static members to calculate CRC16 (16 bit) checksums over various object types.
	/// </summary>
	/// <remarks>The calculation uses a CRC16 polynomial of 0xA001.</remarks>
	public static class CRC16
	{
		public const ushort CRC16Poly = 0xA001;
		public const ushort CRC16START = 0xFFFF;
		private static ushort[] mCRC16LookUp =
		{
			/*[0x00]=*/ 0x0000, 0xC0C1, 0xC181, 0x0140, 0xC301, 0x03C0, 0x0280, 0xC241,
			/*[0x08]=*/ 0xC601, 0x06C0, 0x0780, 0xC741, 0x0500, 0xC5C1, 0xC481, 0x0440,
			/*[0x10]=*/ 0xCC01, 0x0CC0, 0x0D80, 0xCD41, 0x0F00, 0xCFC1, 0xCE81, 0x0E40,
			/*[0x18]=*/ 0x0A00, 0xCAC1, 0xCB81, 0x0B40, 0xC901, 0x09C0, 0x0880, 0xC841,
			/*[0x20]=*/ 0xD801, 0x18C0, 0x1980, 0xD941, 0x1B00, 0xDBC1, 0xDA81, 0x1A40,
			/*[0x28]=*/ 0x1E00, 0xDEC1, 0xDF81, 0x1F40, 0xDD01, 0x1DC0, 0x1C80, 0xDC41,
			/*[0x30]=*/ 0x1400, 0xD4C1, 0xD581, 0x1540, 0xD701, 0x17C0, 0x1680, 0xD641,
			/*[0x38]=*/ 0xD201, 0x12C0, 0x1380, 0xD341, 0x1100, 0xD1C1, 0xD081, 0x1040,
			/*[0x40]=*/ 0xF001, 0x30C0, 0x3180, 0xF141, 0x3300, 0xF3C1, 0xF281, 0x3240,
			/*[0x48]=*/ 0x3600, 0xF6C1, 0xF781, 0x3740, 0xF501, 0x35C0, 0x3480, 0xF441,
			/*[0x50]=*/ 0x3C00, 0xFCC1, 0xFD81, 0x3D40, 0xFF01, 0x3FC0, 0x3E80, 0xFE41,
			/*[0x58]=*/ 0xFA01, 0x3AC0, 0x3B80, 0xFB41, 0x3900, 0xF9C1, 0xF881, 0x3840,
			/*[0x60]=*/ 0x2800, 0xE8C1, 0xE981, 0x2940, 0xEB01, 0x2BC0, 0x2A80, 0xEA41,
			/*[0x68]=*/ 0xEE01, 0x2EC0, 0x2F80, 0xEF41, 0x2D00, 0xEDC1, 0xEC81, 0x2C40,
			/*[0x70]=*/ 0xE401, 0x24C0, 0x2580, 0xE541, 0x2700, 0xE7C1, 0xE681, 0x2640,
			/*[0x78]=*/ 0x2200, 0xE2C1, 0xE381, 0x2340, 0xE101, 0x21C0, 0x2080, 0xE041,
			/*[0x80]=*/ 0xA001, 0x60C0, 0x6180, 0xA141, 0x6300, 0xA3C1, 0xA281, 0x6240,
			/*[0x88]=*/ 0x6600, 0xA6C1, 0xA781, 0x6740, 0xA501, 0x65C0, 0x6480, 0xA441,
			/*[0x90]=*/ 0x6C00, 0xACC1, 0xAD81, 0x6D40, 0xAF01, 0x6FC0, 0x6E80, 0xAE41,
			/*[0x98]=*/ 0xAA01, 0x6AC0, 0x6B80, 0xAB41, 0x6900, 0xA9C1, 0xA881, 0x6840,
			/*[0xA0]=*/ 0x7800, 0xB8C1, 0xB981, 0x7940, 0xBB01, 0x7BC0, 0x7A80, 0xBA41,
			/*[0xA8]=*/ 0xBE01, 0x7EC0, 0x7F80, 0xBF41, 0x7D00, 0xBDC1, 0xBC81, 0x7C40,
			/*[0xB0]=*/ 0xB401, 0x74C0, 0x7580, 0xB541, 0x7700, 0xB7C1, 0xB681, 0x7640,
			/*[0xB8]=*/ 0x7200, 0xB2C1, 0xB381, 0x7340, 0xB101, 0x71C0, 0x7080, 0xB041,
			/*[0xC0]=*/ 0x5000, 0x90C1, 0x9181, 0x5140, 0x9301, 0x53C0, 0x5280, 0x9241,
			/*[0xC8]=*/ 0x9601, 0x56C0, 0x5780, 0x9741, 0x5500, 0x95C1, 0x9481, 0x5440,
			/*[0xD0]=*/ 0x9C01, 0x5CC0, 0x5D80, 0x9D41, 0x5F00, 0x9FC1, 0x9E81, 0x5E40,
			/*[0xD8]=*/ 0x5A00, 0x9AC1, 0x9B81, 0x5B40, 0x9901, 0x59C0, 0x5880, 0x9841,
			/*[0xE0]=*/ 0x8801, 0x48C0, 0x4980, 0x8941, 0x4B00, 0x8BC1, 0x8A81, 0x4A40,
			/*[0xE8]=*/ 0x4E00, 0x8EC1, 0x8F81, 0x4F40, 0x8D01, 0x4DC0, 0x4C80, 0x8C41,
			/*[0xF0]=*/ 0x4400, 0x84C1, 0x8581, 0x4540, 0x8701, 0x47C0, 0x4680, 0x8641,
			/*[0xF8]=*/ 0x8201, 0x42C0, 0x4380, 0x8341, 0x4100, 0x81C1, 0x8081, 0x4040
		};

		/// <summary>
		/// Calculates the CRC of the data byte.
		/// </summary>
		/// <param name="data">Value to generate the CRC for.</param>
		/// <param name="crc">The initial value or previous CRC value.</param>
		/// <returns>The CRC after processing the data value.</returns>
		public static ushort CRC(byte data, ushort crc)
		{
			return (ushort)(mCRC16LookUp[(crc & 0xFF) ^ data] ^ (crc >> 8));
		}

		/// <summary>
		/// Calculates the CRC across an array.
		/// </summary>
		/// <param name="data">The array to calculate the CRC for.</param>
		/// <param name="offset">Starting index into the array to start calculating the CRC.</param>
		/// <param name="length">Number of bytes to run through the CRC calculation.</param>
		/// <param name="seed">Starting value fo the CRC calculation.</param>
		/// <returns>The CRC value of the data.</returns>
		public static ushort CRCArray(ref byte[] data, int offset, int length, ushort seed)
		{
			int end = offset + length;

			for (int i = offset; i < end; ++i)
				seed = CRC(data[i], seed);

			return seed;
		}

		/// <summary>
		/// Calculates the CRC across an array.
		/// </summary>
		/// <param name="data">The array to calculate the CRC for.</param>
		/// <param name="offset">Starting index into the array to start calculating the CRC.</param>
		/// <param name="length">Number of bytes to run through the CRC calculation.</param>
		/// <returns>The CRC value of the data.</returns>
		/// <remarks>A default CRC seed value of <code>CRC16START</code> is used.</remarks>
		public static ushort CRCArray(ref byte[] data, int offset, int length)
		{
			return CRCArray(ref data, offset, length, CRC16START);
		}

		/// <summary>
		/// Calculates the CRC across an array.
		/// </summary>
		/// <param name="data">The array to calculate the CRC for.</param>
		/// <returns>The CRC value of the data.</returns>
		/// <remarks>
		/// The CRC is processed for the whole array.
		/// A default CRC seed value of <code>CRC16START</code> is used.
		/// </remarks>
		public static ushort CRCArray(ref byte[] data)
		{
			return CRCArray(ref data, 0, data.Length, CRC16START);
		}

		/// <summary>
		/// Calculates the CRC across a data stream.
		/// </summary>
		/// <param name="stream">The stream to calculate the CRC for.</param>
		/// <param name="length">Number of bytes to run through the CRC calculation.</param>
		/// <param name="seed">Starting value fo the CRC calculation.</param>
		/// <returns>The CRC value of the stream.</returns>
		/// <remarks>The method will return if an end of stream is encounered before <code>length</code> bytes have been processed.</remarks>
		public static ushort CRCStream(Stream stream, int length, ushort seed)
		{
			for (int i = 0; i < length; ++i)
			{
				int val = stream.ReadByte();
				if (val == -1)
					break;
				seed = CRC((byte)(val & 0xff), seed);
			}

			return seed;
		}

		/// <summary>
		/// Calculates the CRC across a data stream.
		/// </summary>
		/// <param name="stream">The stream to calculate the CRC for.</param>
		/// <param name="length">Number of bytes to run through the CRC calculation.</param>
		/// <returns>The CRC value of the stream.</returns>
		/// <remarks>
		/// The method will return if an end of stream is encounered before <code>length</code> bytes have been processed.
		/// A default CRC seed value of <code>CRC16START</code> is used.
		/// </remarks>
		public static ushort CRCStream(Stream stream, int length)
		{
			return CRCStream(stream, length, CRC16START);
		}

		/// <summary>
		/// Calculates the CRC across a data stream.
		/// </summary>
		/// <param name="stream">The stream to calculate the CRC for.</param>
		/// <param name="seed">Starting value fo the CRC calculation.</param>
		/// <returns>The CRC value of the stream.</returns>
		/// <remarks>The CRC will be calculated for each byte in the stream until end of stream is encountered.</remarks>
		public static ushort CRCStream(Stream stream, ushort seed)
		{
			while (true)
			{
				int val = stream.ReadByte();
				if (val == -1)
					break;
				seed = CRC((byte)(val & 0xff), seed);
			}

			return seed;
		}

		/// <summary>
		/// Calculates the CRC across a data stream.
		/// </summary>
		/// <param name="stream">The stream to calculate the CRC for.</param>
		/// <param name="seed">Starting value fo the CRC calculation.</param>
		/// <returns>The CRC value of the stream.</returns>
		/// <remarks>
		/// The CRC will be calculated for each byte in the stream until end of stream is encountered.
		/// A default CRC seed value of <code>CRC16START</code> is used.
		/// </remarks>
		public static ushort CRCStream(Stream stream)
		{
			return CRCStream(stream, CRC16START);
		}
	}
}
