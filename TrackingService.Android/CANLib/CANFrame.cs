using System;
using System.Text;

namespace CANLib
{
	/// <summary>
	/// CAN Frame
	/// </summary>
	public struct CANFrame
	{
		/// <summary>
		/// special address description flags for the CAN_ID
		/// </summary>
		[Flags]
		public enum IDFlags : uint
		{
			/// <summary>
			/// EFF/SFF is set in the MSB
			/// </summary>
			CAN_SFF_FLAG = 0x00000000,
			/// <summary>
			/// EFF/SFF is set in the MSB
			/// </summary>
			CAN_EFF_FLAG = 0x80000000,
			/// <summary>
			/// remote transmission request
			/// </summary>
			CAN_RTR_FLAG = 0x40000000,
			/// <summary>
			/// error message frame
			/// </summary>
			CAN_ERR_FLAG = 0x20000000,
		};
		/// <summary>
		/// valid bits in CAN ID for frame formats
		/// standard frame format (SFF)
		/// </summary>
		public const uint CAN_SFF_MASK = 0x000007FF;
		/// <summary>
		/// valid bits in CAN ID for frame formats
		/// extended frame format (EFF)
		/// </summary>
		public const uint CAN_EFF_MASK = 0x1FFFFFFF;
		/// <summary>
		/// valid bits in CAN ID for frame formats
		/// omit EFF, RTR, ERR flags
		/// </summary>
		public const uint CAN_ERR_MASK = 0x2FFFFFFF;
		/// <summary>
		/// valid bits in CAN ID for flags
		/// EFF, RTR, ERR flags
		/// </summary>
		public const uint CAN_FLAG_MASK = (uint)(IDFlags.CAN_EFF_FLAG | IDFlags.CAN_RTR_FLAG | IDFlags.CAN_ERR_FLAG);
		uint mMailboxId;
		byte[] mData;

		/// <summary>
		/// Gets/Sets the Message/MailBox Id of the frame.
		/// </summary>
		public uint MailboxId
		{
			get { return mMailboxId; }
			set { mMailboxId = value; }
		}

		/// <summary>
		/// Gets the length of the data payload of the frame.
		/// </summary>
		public int DataLength
		{
			get { return (mData == null) ? 0 : mData.Length; }
		}

		/// <summary>
		/// Gets/Sets the data payload.
		/// </summary>
		/// <remarks>The size of the array will be equal to the data length.</remarks>
		public byte[] Data
		{
			set
			{
				if (value == null)
					mData = null;
				else if (value.Length == 0)
					mData = null;
				else
				{
					mData = new byte[((value.Length > 8) ? 8 : value.Length)];
					Array.Copy(value, mData, mData.Length);
				}
			}
			get
			{
				if (mData == null)
					return null;

				return (byte[])mData.Clone();
			}
		}

		/// <summary>
		/// Copies an array to the payload of the frame.
		/// </summary>
		/// <param name="data">Payload</param>
		/// <param name="index">Starting index in the array</param>
		/// <param name="count">Number of bytes to copy</param>
		/// <returns>The number of bytes copied</returns>
		/// <remarks>The data will be truncated to a maximum of 8 bytes.</remarks>
		public int DataFromArray(byte[] data, int index, int count)
		{
			if ((data.Length - index) < 1)
				mData = null;
			else
			{
				mData = new byte[(count > 8) ? 8 : count];
				Array.Copy(data, index, mData, 0, mData.Length);
			}

			return DataLength;
		}

		/// <summary>
		/// Clears the data part of the frame.
		/// </summary>
		public void ClearData()
		{
			mData = null;
		}

		/// <summary>
		/// Gets/Sets the frame to/from an array.
		/// </summary>
		/// <remarks>
		/// When set the first four bytes (MailBox Id) of the array will be converted from network to host format.
		/// When returned the first four bytes (MailBox Id) of the array will have been converted to network format.
		/// </remarks>
		public byte[] WireFormatArray
		{
			get
			{
				int length = DataLength;
				byte[] array = new byte[5 + length];
				BitConverter.GetBytes(mMailboxId).CopyTo(array, 0);
				// mailbox msb goes first
				if (BitConverter.IsLittleEndian)
					Array.Reverse(array, 0, 4);
				array[4] = (byte)length;
				if (length > 0)
					Array.Copy(mData, 0, array, 5, length);

				return array;
			}
			set
			{
				int length = (value[4] > 8) ? 8 : value[4];
				// mailbox msb goes first
				if (BitConverter.IsLittleEndian)
					Array.Reverse(value, 0, 4);
				mMailboxId = BitConverter.ToUInt32(value, 0);

				if (length > 0)
				{
					mData = new byte[length];
					Array.Copy(value, 5, mData, 0, length);
				}
				else
					mData = null;
			}
		}
		public byte[] GetWireFormatArrayBigEndian()
		{
			int length = DataLength;
			byte[] array = new byte[5 + length];
			BitConverter.GetBytes(mMailboxId).CopyTo(array, 0);
			// mailbox msb goes first
			array[4] = (byte) length;
			if (length > 0)
				Array.Copy(mData, 0, array, 0, length);

			return array;
		}

		public void SetWireFormatArrayBigEndian(byte[] data)
		{
			int length = (data[4] > 8) ? 8 : data[4];
			// mailbox msb goes first
			mMailboxId = BitConverter.ToUInt32(data, 0);

			if (length > 0)
			{
				mData = new byte[length];
				Array.Copy(data, 5, mData, 0, length);
			}
			else
				mData = null;
		}

		public byte[] GetWireFormatArrayLittleEndian()
		{
			int length = DataLength;
			byte[] array = new byte[5 + length];
			BitConverter.GetBytes(mMailboxId).CopyTo(array, 0);
			// mailbox msb goes first
			Array.Reverse(array, 0, 4);
			array[4] = (byte)length;
			if (length > 0)
				Array.Copy(mData, 0, array, 0, length);

			return array;
		}

		public void SetWireFormatArrayLittleEndian(byte[] data)
		{
			int length = (data[4] > 8) ? 8 : data[4];
			// mailbox msb goes first
			Array.Reverse(data, 0, 4);
			mMailboxId = BitConverter.ToUInt32(data, 0);

			if (length > 0)
			{
				mData = new byte[length];
				Array.Copy(data, 5, mData, 0, length);
			}
			else
				mData = null;
		}

		/// <summary>
		/// Gets/Sets the frame to/from an array.
		/// </summary>
		/// <remarks>
		/// The array representation of the frame is in host endian format.
		/// </remarks>
		public byte[] Bytes
		{
			get
			{
				int length = DataLength;
				byte[] array = new byte[5 + length];
				BitConverter.GetBytes(mMailboxId).CopyTo(array, 0);
				// mailbox msb goes first
				array[4] = (byte)length;
				if (length > 0)
					Array.Copy(mData, 0, array, 0, length);

				return array;
			}

			set
			{
				int length = (value[4] > 8) ? 8 : value[4];
				// mailbox msb goes first
				mMailboxId = BitConverter.ToUInt32(value, 0);

				if (length > 0)
				{
					mData = new byte[length];
					Array.Copy(value, 5, mData, 0, length);
				}
				else
					mData = null;
			}
		}

		/// <summary>
		/// Creates a DeviceCatalogueInfo object from the frame data.
		/// </summary>
		/// <returns>A DeviceCatalogueInfo representation of the frame.</returns>
		/// <remarks>If the frame isnt a CNXMsgIds.ProductId type frame null is returned.</remarks>
		public DeviceCatalogueInfo ToDeviceCatalogueInfo()
		{
			if ((CNXMsgIds)MailboxId != CNXMsgIds.ProductId || mData.Length < 2)
				return null;

			DeviceCatalogueInfo dci = new DeviceCatalogueInfo()
			{
				ProductId = (DeviceCatalogueInfo.Product)mData[0],
				BuildNo = mData[1]
			};
			if (mData.Length > 3)
			{
				dci.Mask = (DeviceCatalogueInfo.EquipmentFlages)mData[2];
				dci.Status = (DeviceCatalogueInfo.EquipmentFlages)mData[3];
			}

			if (mData.Length > 5)
			{
				dci.Mask |= (DeviceCatalogueInfo.EquipmentFlages)((uint)mData[4] << 8);
				dci.Status |= (DeviceCatalogueInfo.EquipmentFlages)((uint)mData[5] << 8);
			}

			if (mData.Length > 7)
			{
				dci.Mask |= (DeviceCatalogueInfo.EquipmentFlages)((uint)mData[6] << 16);
				dci.Status |= (DeviceCatalogueInfo.EquipmentFlages)((uint)mData[7] << 16);
			}

			return dci;
		}

		/// <summary>
		/// Initialises the frame from DeviceCatalogueInfo
		/// </summary>
		/// <param name="dci">The cateloge information to initialise the frame from.</param>
		public void FromDeviceCatalogueInfo(DeviceCatalogueInfo dci)
		{
			MailboxId = (uint)CNXMsgIds.ProductId;

			// allocate the data payload
			if ((uint)dci.Mask > 0x0000ffff)
				mData = new byte[8];
			else if ((uint)dci.Mask > 0x000000ff)
				mData = new byte[6];
			else if ((uint)dci.Mask > 0)
				mData = new byte[4];
			else
				mData = new byte[2];

			mData[0] = (byte)dci.ProductId;
			mData[1] = dci.BuildNo;

			if (mData.Length > 3)
			{
				mData[2] = (byte)dci.Mask;
				mData[3] = (byte)dci.Status;
			}

			if (mData.Length > 5)
			{
				mData[4] = (byte)(((uint)dci.Mask >> 8) & 0xff);
				mData[5] = (byte)(((uint)dci.Status >> 8) & 0xff);
			}

			if (mData.Length > 7)
			{
				mData[6] = (byte)(((uint)dci.Mask >> 16) & 0xff);
				mData[7] = (byte)(((uint)dci.Status >> 16) & 0xff);
			}
		}

		public override string ToString()
		{
			string message = null;
			StringBuilder strData = new StringBuilder();

			try
			{
				if ((mMailboxId & 0xffffff00) == (uint)CNXMsgIds.BlockQueryResponse)
				{
                    Block block;
                    uint offset;
                    ushort crc;
                    byte version;
					if (CNXCANMsgHelper.UnpackBlockQueryResponce(this, out block, out offset, out crc, out version))
					{
						object[] arg = new object[] { block, offset, crc, version };
						message = String.Format("Block {0}, offset 0x{1:x} ({1}), CRC 0x{2:x}, version {3}.", arg);
						return message;
					}
				}
				switch ((CNXMsgIds)mMailboxId)
				{
					case CNXMsgIds.PassengerCountEvent:
						foreach (PassengerCountEventMask mask in Enum.GetValues(typeof(PassengerCountEventMask)))
						{
							if ((mData[1] & (byte)mask) != 0)
							{
								if ((byte)mask > (byte)PassengerCountEventMask.Forth)
									strData.AppendFormat("{0} {1} ", mask, ((mData[0] & (byte)mask) == 0) ? "retracted," : "extended,");
								else
									strData.AppendFormat("{0} door {1} ", mask, ((mData[0] & (byte)mask) == 0) ? "open," : "closed,");
							}
							//if (mask == PassengerCountEventMask.Bike)
							//    break;
						}
						message = strData.ToString();
						break;
					case CNXMsgIds.PassengerLoad:
						message = String.Format("{0} door {1} boarding, {2} alighting.", (DoorCountType)mData[0], mData[1], mData[2]);
						break;
					case CNXMsgIds.PassengerBoardings:
						strData.Append("Pasenger Loading: ");
						for (int i = 0; i < DataLength; i += 2)
							strData.AppendFormat("Door {0} on {1}, off {2}. ", (i / 2), mData[i], mData[i + 1]);
						message = strData.ToString();
						break;
					case CNXMsgIds.DateTime :
						message = String.Format("{0} {1}", (CNXMsgIds)mMailboxId, CNXCANMsgHelper.UnpackDateTimeFrame(this).ToString());
						break;
					case CNXMsgIds.GPS:
                        message = String.Format("GPS {0}", CNXCANMsgHelper.UnpackGPSState(this).ToString());
                        break;
                    case CNXMsgIds.DuressState:
                        message = String.Format("Duress state {0}.", (DuressStateType)mData[0]);
                        break;
					case CNXMsgIds.TripOnRoute:
						short pathId;
						int position;
						int? tripNo;
						ushort? serviceStart;
						RunningStateType runningState;
						CNXCANMsgHelper.UnpackTripOnRoute(this, out pathId, out position, out tripNo, out serviceStart, out runningState);
						object[] TripArgs = { pathId, position, tripNo, serviceStart, runningState };
						message = String.Format("TripOnRoute pathId:{0} Poistion:{1} TripNo:{2} Service:{3} Running:{4}", TripArgs);
						break;
					case CNXMsgIds.ProductId:
						DeviceCatalogueInfo dci = ToDeviceCatalogueInfo();
						message = dci.ToString();
						break;
					case CNXMsgIds.BlockChunk1:
					case CNXMsgIds.BlockChunkN:
					case CNXMsgIds.BlockQuery:
					case CNXMsgIds.BlockQueryResponse:
					case CNXMsgIds.Destination:
					case CNXMsgIds.DeviceCatalogue:
					case CNXMsgIds.DigitalInputState:
					case CNXMsgIds.Fareset:
					case CNXMsgIds.ManufacturingTest:
					case CNXMsgIds.RouteTrip:
					case CNXMsgIds.TripProgress:
					case CNXMsgIds.TripOffRoute:
					case CNXMsgIds.TripNone:
						message = string.Format("{0} {1}", (CNXMsgIds)mMailboxId, DefaultToString());
						break;
					default:
						message = DefaultToString();
						break;
				}
			}
			catch (Exception e)
			{
				Console.WriteLine(e.ToString());
			}
			return message;
		}

		public string DefaultToString()
		{
			if (mData != null)
			{
				StringBuilder strData = new StringBuilder();
				foreach (byte dat in mData)
					strData.AppendFormat(" {0:x2}", dat);
				strData.Append('>');
				foreach (byte dat in mData)
				{
					strData.Append(' ');
					strData.Append(((dat < 32 || dat > 127) ? '.' : (char)dat));
				}

				return string.Format("<0x{0:x} [{1}] {2}", MailboxId, DataLength, strData.ToString());
			}
			
			return string.Format("{0} <0x{1:x} [0] >", (IDFlags)(MailboxId & CAN_FLAG_MASK), MailboxId);
		}
	}
}
