using System;
using System.Collections.Generic;

using Tracking.Services;

namespace CANLib
{
	public class J1939TransportProtocol
	{
#if ANDROID
        protected static log4droid CNXLog = new log4droid() {Logger = "CNXLogger"};
#else
        protected static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif

        public class IncompleteTPMessageException : Exception
		{
			private const string msg = @"CM frame before last sequence frame";
			public IncompleteTPMessageException() : base(msg) { }
			public IncompleteTPMessageException(string message) : base(msg + " : " + message) { }
			public IncompleteTPMessageException(string message, Exception innerException) : base(msg + " : " + message, innerException) { }
		}

		public class TPMessage
		{
			public const int BytesPerDataPacket = 7;
			private byte mPackets = 0;
			private byte mLastPacket = 0;
			private uint mPGN = 0;
			public uint PGN { get { return mPGN; } }
			private ushort mSize = 0;
			private byte[] mMessage;
			public byte[] Message
			{
				get
				{
					byte[] msg = new byte[mSize];
					Array.Copy(mMessage, msg, mSize);
					return msg;
				}
			}
			/// <summary>
			/// Initialise a new message
			/// </summary>
			/// <param name="bam">Broadcast Announce Message frame data</param>
			/// <remarks>
			/// J1939 BAM format:
			///   7 6 5 4 3 2 1 0
			/// 0|    Control    | control byte
			/// 1|   SIZE LSB    | message size lsb
			/// 2|   SIZE MSB    | message size msb
			/// 3|    PACKETS    | packet count
			/// 4|   RESERVED    | reserved should be 0xff
			/// 5|    PGN LSB    | PGN lsb
			/// 6|      PGN      | PGN middle
			/// 7|    PGN MSB    | PGN msb
			/// </remarks>
			public TPMessage(byte[] bam)
			{
				mSize = (ushort)(bam[2] << 8);
				mSize += bam[1];
				mPackets = bam[3];
				mPGN = (uint)(bam[7] << 16);
				mPGN += (uint)(bam[6] << 8);
				mPGN += bam[5];
				mMessage = new byte[mPackets * BytesPerDataPacket];
			}

			/// <summary>
			/// New sequence message
			/// </summary>
			/// <param name="packet">Data Packet</param>
			/// <remarks>
			/// J1939 BAM format:
			///   7 6 5 4 3 2 1 0
			/// 0|   SEQUENCE    | sequence number
			/// 1|      DATA     | data byte
			/// 2|      DATA     | data byte
			/// 3|      DATA     | data byte
			/// 4|      DATA     | data byte
			/// 5|      DATA     | data byte
			/// 6|      DATA     | data byte
			/// 7|      DATA     | data byte
			/// </remarks>
			/// <param name="packet"></param>
			/// <returns>True if message is complete</returns>
			/// <exception cref="ArgumentOutOfRangeException">
			/// Throws exception on missing packet sequence 
			/// </exception>
			/// <exception cref="IndexOutOfRangeException">
			/// Throws exception on squence errors 
			/// </exception>
			public bool NextSequence(byte[] packet)
			{
				byte sequenceNo = packet[0];
				//Console.WriteLine("TP packet {0} of {1}.", sequenceNo, mPackets);
				if (sequenceNo > mPackets)
					throw new IndexOutOfRangeException(string.Format("Sequence {0} higher that BAM packet count {1}.", sequenceNo, mPackets));
				if (sequenceNo > (mLastPacket + 1))
					throw new ArgumentOutOfRangeException(string.Format("Missing packet detected last {0} this {1}", mLastPacket, sequenceNo));
				mLastPacket = sequenceNo;

				Array.Copy(packet, 1, mMessage, (sequenceNo - 1) * BytesPerDataPacket, BytesPerDataPacket);

				return (sequenceNo == mPackets);
			}

			public override string ToString()
			{
				return string.Format("PGN {0} (0x{0:x}) size {1}, packets {2}", mPGN, mSize, mPackets);
			}
		}

		public enum ControlByte : byte
		{
			/// <summary>
			/// Destination Specific Request To Send (RTS)
			/// </summary>
			RTS = 16,
			/// <summary>
			/// Destination Specific Clear To Send (CTS)
			/// </summary>
			CTS = 17,
			/// <summary>
			/// End Of Message Acknowledge
			/// </summary>
			EOM_ACK = 19,
			/// <summary>
			/// Broadcast Announce Message
			/// </summary>
			BAM = 32,
			/// <summary>
			/// Connection Abort
			/// </summary>
			Abort = 255,
		}
		private static Dictionary<byte, TPMessage> mTPMessages = new Dictionary<byte, TPMessage>();

		public static void TPCMFrame(byte sa, byte[] payload)
		{
			if (payload[0] != (byte)ControlByte.BAM)
				throw new ArgumentException(string.Format("Control byte {0}", (ControlByte)payload[0]));

			TPMessage msg = null;
			TPMessage tpMessage = new TPMessage(payload);

			lock (mTPMessages)
			{
				try
				{

					if (mTPMessages.TryGetValue(sa, out msg))
						mTPMessages.Remove(sa);
					mTPMessages.Add(sa, tpMessage);
					//Console.WriteLine(tpMessage);
				}
				catch (Exception e) { CNXLog.ErrorFormat("TPCMFrame {0}", e.ToString()); }
			}

			if (msg != null)
				throw new IncompleteTPMessageException(string.Format("SA {0} PGN {1}", sa, msg.PGN));
		}

		public static TPMessage TPDataFrame(byte sa, byte[] payload)
		{
			//Console.WriteLine("TPDataFrame SA {0} packet {1}", sa, payload[0]);

			TPMessage msg = null;

			lock (mTPMessages)
			{
				try
				{
					if (!mTPMessages.TryGetValue(sa, out msg))
						Console.WriteLine("Missing BAM for this SA {0}", sa);
				}
				catch (Exception e) { CNXLog.ErrorFormat("TPDataFrame {0}", e.ToString()); }
			}

			if (msg == null)
				throw new ArgumentOutOfRangeException(string.Format("Missing BAM for this SA {0}", sa));

			try
			{
				if (msg.NextSequence(payload))
				{
					lock (mTPMessages)
					{
						try
						{
							mTPMessages.Remove(sa);
						}
						catch (Exception e) { CNXLog.ErrorFormat("TPDataFrame {0}", e.ToString()); }
					}
					return msg;
				}
			}
			catch (Exception e)
			{
				lock (mTPMessages)
				{
					try
					{
						mTPMessages.Remove(sa);
					}
					catch (Exception ee) { CNXLog.ErrorFormat("TPDataFrame {0}", ee.ToString()); }
				}
				throw e;
			}

			return null;
		}
	}
}
