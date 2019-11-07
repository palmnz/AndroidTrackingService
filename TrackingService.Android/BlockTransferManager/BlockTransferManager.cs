using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Timers;
using System.IO;

using CANLib;
using Tracking.Services;

namespace BlockLib
{
	public class BlockTransferManager
	{
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif
        public enum BlockTransferState
		{
			/// <summary>
			/// Unknown state.
			/// </summary>
			None,
			/// <summary>
			/// Trying to aquire the block version from the device.
			/// </summary>
			AquiringDeviceVersion,
			/// <summary>
			/// Trying to get the block resource.
			/// </summary>
			AquiringResource,
			/// <summary>
			/// Trying to get the block resource failed, will automatically retry.
			/// </summary>
			AquiringResourceFailed,
			/// <summary>
			/// A block transfere is in process.
			/// </summary>
			SynchronisingBlock,
			/// <summary>
			/// The device has been synchronised with the block resource.
			/// </summary>
			DeviceSynchronised,
		};

		public enum BlockEvent
		{
			Timeout,
			ServerVersion,
			Frame,
			AquireComplete,
			AquireFailed,
			SendComplete,
			SendFailed,
		};

		/// <summary>
		/// State event notification class.
		/// </summary>
		public class StateChangedEventArgs : EventArgs
		{
			public StateChangedEventArgs(BlockTransferState state)
			{
				State = state;
			}
			public BlockTransferState State;
		}

		/// <summary>
		/// Subscribe to the transfer state events.
		/// </summary>
		public event EventHandler<StateChangedEventArgs> RaiseStateChangedEvent;

		private volatile BlockTransferState mState = BlockTransferState.None;
		/// <summary>
		/// Gets the transfere state of the block.
		/// </summary>
		public BlockTransferState State { get { return mState; } }
		private byte mBlockId;
		/// <summary>
		/// Gets the Id of the block that is being managed.
		/// </summary>
		public byte BlockId { get { return mBlockId; } }
		private int mBlockVersion;
		/// <summary>
		/// Gets the block version that the is being sent to the device
		/// On the next heartbeat Set will begin a new transfer if the current and new versions are different.
		/// </summary>
		public int BlockVersion
		{
			get { return mBlockVersion; }
			set
			{
				if (mBlockVersion != value)
				{
					// make sure there isn't a transfere inprogress
					//if (mState == BlockTransferState.None || mState == BlockTransferState.DeviceSynchronised)
						mBlockVersion = value;
					mState = BlockTransferState.AquiringDeviceVersion;
					// kick off a new transfer
					BlockStateMachine(BlockEvent.ServerVersion);
				}
			}
		}
		private int mDeviceVersion = 255;
		/// <summary>
		/// Gets the block version that the device is reporting.
		/// </summary>
		/// <remarks>If no version has been reported 255 is returned.</remarks>
		public int DeviceVersion { get { return mDeviceVersion; } }

		private CANClient mClient;
		private string mResource;
		private System.Threading.Timer mBlockQueryTimer;
		private byte[] mBlockData;
		private ushort mBlockCRC = ushort.MinValue;
		private ushort mDeviceCRC = ushort.MaxValue;
		private uint mLastOffset = 0;
		//private int mLastResourceOffset = 0;
		private ResumableResourceDownload mResDownLoader;

		public BlockTransferManager(string resource, byte blockId, int version, CANClient client)
		{
			mResource = resource;
			mBlockId = blockId;
			mBlockVersion = version;
			mClient = client;

			Uri uri = new Uri(resource);
			if (uri.Scheme != "file")
				mResDownLoader = new ResumableResourceDownload(mResource);

			// first get the device version
			mState = BlockTransferState.AquiringDeviceVersion;

			// subscribe to CAN frame events
			mClient.RaiseFrameReceivedEvent += FrameReceivedEventHandler;
			CNXLog.WarnFormat("Starting BlockTransferManager url {0}, Id {1}, ver {2}", resource, blockId, version);
			SendBlockRequest();

			// start time for cataloge and status reporting
			mBlockQueryTimer = new System.Threading.Timer(OnTimedEvent, null, 0, (2 * 60 * 1000));
		}

		public void Close()
		{
			mBlockQueryTimer.Dispose();
			mClient.RaiseFrameReceivedEvent -= FrameReceivedEventHandler;
		}

		/// <summary>
		/// Transferes the block regardless of device version.
		/// </summary>
		public void ForceTransfer()
		{
			if (mState == BlockTransferState.AquiringDeviceVersion || mState == BlockTransferState.None)
			{
				mState = BlockTransferState.AquiringDeviceVersion;
				BlockStateMachine(BlockEvent.AquireFailed);
			}
		}

		private void FrameReceivedEventHandler(object o, FrameReceivedEventArgs a)
		{
			if (a.Frame.MailboxId == (uint)CNXMsgIds.BlockQueryResponse + (uint)mBlockId)
			{
				byte[] data = a.Frame.Data;

				if (data.Length > 3)
				{
					if (!BitConverter.IsLittleEndian)
						Array.Reverse(data, 0, 4);
					mLastOffset = BitConverter.ToUInt32(data, 0);
				}
				// crc supplied
				if (data.Length > 5)
				{
					if (!BitConverter.IsLittleEndian)
						Array.Reverse(data, 4, 2);
					mDeviceCRC = BitConverter.ToUInt16(data, 4);
				}
				// do we have some version information
				if (data.Length > 6)
					mDeviceVersion = data[6];

				object[] arg = new object[] { (Block)(a.Frame.MailboxId & 0xff), mLastOffset, mLastOffset, mDeviceCRC, mBlockCRC, mDeviceVersion, mBlockVersion };
				CNXLog.InfoFormat("BlockQueryResponse {0}, offset 0x{1:x} ({2}), reported crc 0x{3:x}, block crc 0x{4:x}, reported version {5}, block version {6}.", arg);

				BlockStateMachine(BlockEvent.Frame);
			}
		}

		private void OnTimedEvent(object source /*, ElapsedEventArgs a*/)
		{
			BlockStateMachine(BlockEvent.Timeout);
		}

		private void SendBlockRequest()
		{
			CANFrame frame = new CANFrame();
			frame.MailboxId = (uint)CNXMsgIds.BlockQuery + (uint)mBlockId;
			mClient.Send(frame);
		}

		/// <summary>
		/// Indicates whether the block is up to date.
		/// </summary>
		public bool Synchronised
		{
			get
			{
				if (mDeviceVersion == mBlockVersion)
					return true;
				if (mDeviceVersion != 255 && mBlockVersion == 255)
					return true;
				if (mBlockData != null)
					return (mLastOffset >= mBlockData.LongLength && mBlockCRC == mDeviceCRC);
				return false;
			}
		}

		private void BlockStateMachine(BlockEvent blockEvent)
		{
			BlockTransferState entryState = mState;

			switch (entryState)
			{
				case BlockTransferState.None:
					switch (blockEvent)
					{
						case BlockEvent.Timeout:
							mState = BlockTransferState.AquiringDeviceVersion;
							SendBlockRequest();
							break;
						case BlockEvent.AquireFailed:
							mState = BlockTransferState.AquiringResource;
							BeginFetchingResource();
							break;
						case BlockEvent.ServerVersion:
						case BlockEvent.Frame:
							if (Synchronised)
								mState = BlockTransferState.DeviceSynchronised;
							else
							{
								mState = BlockTransferState.AquiringResource;
								BeginFetchingResource();
							}
							break;
						case BlockEvent.AquireComplete:
						case BlockEvent.SendFailed:
							mState = BlockTransferState.SynchronisingBlock;
							BeginSendingBlock();
							break;
						case BlockEvent.SendComplete:
							mState = BlockTransferState.AquiringDeviceVersion;
							SendBlockRequest();
							break;
					}
					break;
				case BlockTransferState.AquiringDeviceVersion:
					switch (blockEvent)
					{
						case BlockEvent.Timeout:
							mState = BlockTransferState.AquiringDeviceVersion;
							SendBlockRequest();
							break;
						case BlockEvent.AquireFailed:
							mState = BlockTransferState.AquiringResource;
							BeginFetchingResource();
							break;
						case BlockEvent.ServerVersion:
						case BlockEvent.Frame:
							if (Synchronised)
								mState = BlockTransferState.DeviceSynchronised;
							else
							{
								mState = BlockTransferState.AquiringResource;
								BeginFetchingResource();
							}
							break;
						case BlockEvent.AquireComplete:
						case BlockEvent.SendFailed:
							mState = BlockTransferState.SynchronisingBlock;
							BeginSendingBlock();
							break;
						case BlockEvent.SendComplete:
							mState = BlockTransferState.AquiringDeviceVersion;
							SendBlockRequest();
							break;
					}
					break;
				case BlockTransferState.AquiringResource:
					switch (blockEvent)
					{
						case BlockEvent.Timeout:
						case BlockEvent.ServerVersion:
						case BlockEvent.Frame:
							break;
						case BlockEvent.AquireFailed:
							mState = BlockTransferState.AquiringResourceFailed;
							break;
						case BlockEvent.AquireComplete:
						case BlockEvent.SendFailed:
							mState = BlockTransferState.SynchronisingBlock;
							BeginSendingBlock();
							break;
						case BlockEvent.SendComplete:
							mState = BlockTransferState.AquiringDeviceVersion;
							SendBlockRequest();
							break;
					}
					break;
				case BlockTransferState.AquiringResourceFailed:
					switch (blockEvent)
					{
						case BlockEvent.Timeout:
						case BlockEvent.ServerVersion:
							mState = BlockTransferState.AquiringResource;
							BeginFetchingResource();
							break;
						case BlockEvent.AquireFailed:
							break;
						case BlockEvent.Frame:
							if (Synchronised)
								mState = BlockTransferState.DeviceSynchronised;
							break;
						case BlockEvent.AquireComplete:
						case BlockEvent.SendFailed:
							mState = BlockTransferState.SynchronisingBlock;
							BeginSendingBlock();
							break;
						case BlockEvent.SendComplete:
							mState = BlockTransferState.AquiringDeviceVersion;
							SendBlockRequest();
							break;
					}
					break;
				case BlockTransferState.SynchronisingBlock:
					switch (blockEvent)
					{
						case BlockEvent.Timeout:
							break;
						case BlockEvent.ServerVersion:
						case BlockEvent.AquireFailed:
							mState = BlockTransferState.AquiringResource;
							BeginFetchingResource();
							break;
						case BlockEvent.Frame:
							if (Synchronised)
								mState = BlockTransferState.DeviceSynchronised;
							break;
						case BlockEvent.AquireComplete:
						case BlockEvent.SendFailed:
							mState = BlockTransferState.SynchronisingBlock;
							BeginSendingBlock();
							break;
						case BlockEvent.SendComplete:
							mState = BlockTransferState.AquiringDeviceVersion;
							SendBlockRequest();
							break;
					}
					break;
				case BlockTransferState.DeviceSynchronised:
					switch (blockEvent)
					{
						case BlockEvent.Timeout:
							SendBlockRequest();
							break;
						case BlockEvent.ServerVersion:
						case BlockEvent.Frame:
							if (!Synchronised)
							{
								mState = BlockTransferState.AquiringResource;
								BeginFetchingResource();
							}
							break;
						case BlockEvent.AquireComplete:
						case BlockEvent.SendComplete:
							break;
					}
					break;
				default:
					mState = BlockTransferState.AquiringDeviceVersion;
					break;
			}

			if (entryState != mState)
			{
				CNXLog.InfoFormat("Block {0} transfer from {1} to {2}", mBlockId, entryState, mState);
				OnRaiseStateChangedEvent(new StateChangedEventArgs(mState));
			}
        }

		private void BeginFetchingResource()
		{
			ThreadPool.QueueUserWorkItem(new WaitCallback(FetchResource));
		}

		private void FetchResource()
		{
			FetchResource(null);
		}

		private void FetchResource(Object stateInfo)
		{
			bool result = false;
			try
			{
				Uri uri = new Uri(mResource);
				if (uri.Scheme == "file")
				{
					mBlockData = GetResourceFromUri(mResource);
				}
				else
				{
					mBlockData = mResDownLoader.AquireResource();
				}
				if (mBlockData != null)
				{
					// got the data, now calculate the CRC
					mBlockCRC = CRC16.CRCArray(ref mBlockData);
					CNXLog.InfoFormat("Block resource aquired length {0} CRC 0x{1:x} ({2})", mBlockData.Length, mBlockCRC, mBlockCRC);
					result = true;
				}
			}
			catch (Exception e)
			{
				CNXLog.Error("FetchResource:", e);
			}

			BlockStateMachine(result ? BlockEvent.AquireComplete : BlockEvent.AquireFailed);
		}

		private void BeginSendingBlock()
		{
			ThreadPool.QueueUserWorkItem(new WaitCallback(SendBlock));
		}

		private void SendBlock()
		{
			SendBlock(null);
		}

		private void SendBlock(Object stateInfo)
		{
			bool sendComplete = false;
			try
			{
				// build a chunk 1 message
				CANFrame chunk1 = BuildChunk1(mBlockId, mBlockCRC, (uint)mBlockData.Length, mBlockVersion, 0);
				// send chunk 1
				mClient.Send(chunk1);

				// transfer the block
				CANFrame frame = new CANFrame();
				frame.MailboxId = (uint)CNXMsgIds.BlockChunkN + (uint)mBlockId;

				byte[] data = new byte[8];
                for (uint offset = 0; offset < mBlockData.Length; offset = mLastOffset > offset ? mLastOffset : offset + 4)
                //for (uint offset = 0; offset < mBlockData.Length; offset += 4)
				{
					// set the offset
					BitConverter.GetBytes(offset).CopyTo(data, 0);
					if (!BitConverter.IsLittleEndian)
						Array.Reverse(data, 0, 4);
					uint remainder = (uint)(mBlockData.LongLength - offset);
					int count = (int)((remainder > 4) ? 4 : remainder);
					Array.Copy(mBlockData, offset, data, 4, count);
					frame.DataFromArray(data, 0, 4 + count);
					int sent = mClient.Send(frame);
					// pause to give CPC's time to commit to flash
					// 40mS every 10 frames.
					if (offset % 10 == 0)
						Thread.Sleep(100);
					Thread.Sleep(1);
					for (int retry = 1; sent < 1 && retry < 10; ++retry)
					{
						Thread.Sleep(retry * 5);
						sent = mClient.Send(frame);
					}
				}
				// send chunk 1 for good luck
				Thread.Sleep(40);
				mClient.Send(chunk1);
				sendComplete = true;
				Thread.Sleep(40);
			}
			catch (Exception e)
			{
				CNXLog.ErrorFormat("SendBlock error - {0}", e.ToString());
			}
			BlockStateMachine(sendComplete ? BlockEvent.SendComplete : BlockEvent.SendFailed);
		}

		/// <summary>
		/// Builds a Chunck 1 frame based on supplied data.
		/// </summary>
		/// <param name="blockId">Id of the block.</param>
		/// <param name="crc">CRC of the complete block data.</param>
		/// <param name="size">Size of the block if known.</param>
		/// <param name="version">The version of the block or -1 if unknown.</param>
		/// <param name="flags">Any flags that should be included.</param>
		/// <returns>A chunk1 frame redy for transmission.</returns>
		/// <remarks>
		/// The chunck1 will be formatted based on the suppiled data. No size will be included if size is set to zeor.
		/// No version will be included unless the size is valid and the verion is +ve.
		/// </remarks>
		public static CANFrame BuildChunk1(byte blockId, ushort crc, uint size, int version, byte flags)
		{
			CANFrame frame = new CANFrame();
			frame.MailboxId = (uint)CNXMsgIds.BlockChunk1 + (uint)blockId;

			bool verIncluded = (version != -1);

			byte[] data = new byte[verIncluded ? 8 : 7];
			// set the flags
			if (verIncluded)
				flags |= (byte)CANLib.BlockFlags.VersionPresent;
			data[2] = flags;
			// set the CRC
			BitConverter.GetBytes(crc).CopyTo(data, 0);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(data, 0, 2);
			// set the size
			BitConverter.GetBytes(size).CopyTo(data, 3);
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(data, 3, 4);
			// set the version
			if (verIncluded)
				data[7] = (byte)(version & 0xff);

			frame.Data = data;

			return frame;
		}

		/// <summary>
		/// Event firing method.
		/// </summary>
		/// <param name="frame">The new state of the Block transfere.</param>
		protected virtual void OnRaiseStateChangedEvent(StateChangedEventArgs state)
		{
			ThreadPool.QueueUserWorkItem((o) =>
			{
				// copy the event handler to avoid mid process subscribe/un-subscribe
				EventHandler<StateChangedEventArgs> handler = RaiseStateChangedEvent;

				// Check if there are any Subscribers
				if (handler != null)
				{
					// Call the Event
					handler(this, state);
				}
			}
			);
		}

		public static byte[] GetResourceFromUri(string resource)
		{
			const int readSize = 512;
			bool success = false;
			byte[] blockData = null;

			CNXLog.InfoFormat("Getting resource {0}", resource);

			try
			{
				//HttpWebRequest req = (HttpWebRequest)WebRequest.Create(resource);
				//using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
				WebRequest req = WebRequest.Create(resource);
				using (WebResponse resp = req.GetResponse())
				{
					// allocate a block of memory for the data.
					uint length = (uint)resp.ContentLength;
					blockData = new byte[length];

					using (BufferedStream buffStream = new BufferedStream(resp.GetResponseStream(), 1024))
					{
						int offset = 0;
						int read = (blockData.Length > readSize) ? readSize : blockData.Length;
						for (read = buffStream.Read(blockData, offset, read); read > 0; read = buffStream.Read(blockData, offset, read))
						{
							offset += read;
							read = blockData.Length - offset;
							if (read > readSize)
								read = readSize;
						}

						success = (offset == resp.ContentLength);
					}
				}
			}
			catch (Exception e)
			{
				CNXLog.ErrorFormat("GetResourceFromUri {0} {1}", resource, e.ToString());
				success = false;
			}

			return (success ? blockData : null);
		}
	}
}
