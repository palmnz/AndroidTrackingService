using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;

using Tracking.Services;

namespace CANLib
{
	/// <summary>
	/// Indicates the progress of the recieved block.
	/// </summary>
	public enum BlockState
	{
		/// <summary>
		/// Initial State.
		/// </summary>
		UNKNOWN = 0,
		/// <summary>
		/// A complete chunk1 has not been recieved.
		/// </summary>
		NO_CHUNK1,
		/// <summary>
		/// Still some chunks missing.
		/// </summary>
		INCOMPLETE,
		/// <summary>
		/// Block is complete but the publichsed CRC doesnt match the calculated CRC.
		/// </summary>
		CRC_MISSMATCH,
		/// <summary>
		/// The Block is complete and valid.
		/// </summary>
		COMPLETE,
		/// <summary>
		/// The completed Block is being re-transmitted.
		/// </summary>
		COMPLETE_REPEAT,
		/// <summary>
		/// The block transfere failed, used as a treminal state for transmitted blocks.
		/// </summary>
		FIALED
	};

	/// <summary>
	/// Block status changed event notification class.
	/// </summary>
	public class BlockStatusEventArgs : EventArgs
	{
		public BlockStatusEventArgs(byte id, BlockState state)
		{
			mState = state;
		}
		public BlockStatusEventArgs(byte id, BlockState state, uint transfered)
		{
			mState = state;
			mTransfered = transfered;
		}
		private BlockState mState;
		/// <summary>
		/// Get the new state of the block.
		/// </summary>
		public BlockState State
		{
			get { return mState; }
		}
		private uint mTransfered = 0;
		/// <summary>
		/// Gets the number of bytes transfered at the point the event was raised.
		/// </summary>
		public uint Transfered
		{
			get { return mTransfered; }
		}
		private byte mId = 0;
		/// <summary>
		/// Gets the Block Id of the block that generated the event.
		/// </summary>
		public byte Id
		{
			get { return mId; }
		}
	}

	/// <summary>
	/// Recieves small transient blocks over CAN without regard for the version.
	/// </summary>
	public class TransientBlockReciever : BaseBlockReciever
	{
		public TransientBlockReciever(int blockId, CANClient client) : base(blockId, 0, client, 200) { }

		private byte[] mBlockData;
		public byte[] BlockData
		{
			get { return mBlockData; }
		}

		protected override bool BlockCompleted()
		{
			bool result = false;
			try
			{
				if (base.BlockCompleted())
				{
					mBlockData = new byte[mSize];
					if (mSize > 0)
						Array.Copy(mMemStream.ToArray(), mBlockData, mSize);
					else
						OnRaiseBlockStatusEvent(new BlockStatusEventArgs(mBlockId, BlockState.COMPLETE));

					CNXLog.InfoFormat("TransientBlockReciever {0} Completed:\n\r{1}", (Block)mBlockId, HexDump.Dump(mBlockData));

					result = true;
				}
			}
			catch (Exception e)
			{
				CNXLog.Error("TransientBlockReciever.BlockCompleted:", e);
			}
			return result;
		}

		protected override void ProcessChunk1(byte[] data)
		{
			if (!mCRCOk)
			{
				base.ProcessChunk1(data);
				return;
			}

			ushort crc = 0;
			uint size = 0;

			// collect the CRC
			if (data.Length > 1)
			{
				// MSB CRC
				crc = data[1];
				crc <<= 8;
				// LSB CRC
				crc |= data[0];
			}

			// have we got the size yet
			if (data.Length > 6)
			{
				if (!BitConverter.IsLittleEndian)
					Array.Reverse(data, 3, 4);
				size = BitConverter.ToUInt32(data, 3);
			}

			// test for repeat block
			if (mCRC == crc && mSize == size && mBlockData != null)
				OnRaiseBlockStatusEvent(new BlockStatusEventArgs(mBlockId, BlockState.COMPLETE_REPEAT));
			else
				base.ProcessChunk1(data);
		}
	}

	/// <summary>
	/// Recieves data blocks over CAN using versioning to control what should be done.
	/// </summary>
	public class BlockReciever : BaseBlockReciever
	{
		private FileStream mFileStream;
		private string mPath;
		/// <summary>
		/// Gets the complete file name and path.
		/// </summary>
		public string Path { get { return mPath; } }
		private System.Threading.Timer mVersionTimer;

		/// <summary>
		/// Initialise a BlockReceiver from a block frame.
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="version">Current product version.</param>
		/// <param name="client">The CAN client to use for frame transferes.</param>
		public BlockReciever(CANLib.CANFrame frame, int version, CANNativeClient client) : base(frame, version,client)
		{
			mVersionTimer = new System.Threading.Timer(new TimerCallback(OnTimedEvent), null, 60 * 1000, 60 * 1000);
		}

		/// <summary>
		/// Initialise a BlockReceiver.
		/// </summary>
		/// <param name="blockId">Id of the blocks to be processed.</param>
		/// <param name="version">Current product version.</param>
		/// <param name="client">The CAN client to use for frame transferes.</param>
		public BlockReciever(int blockId, int version, CANClient client) : base(blockId, version, client)
		{
			mVersionTimer = new System.Threading.Timer(new TimerCallback(OnTimedEvent), null, 60 * 1000, 60 * 1000);
		}

		private void OnTimedEvent(object state)
		{
			SendBlockQueryResponce();
		}

		private void CreateBlockFile()
		{
			try
			{
				mPath = String.Format("{0}blk{1}",((Environment.OSVersion.Platform == System.PlatformID.Unix) ? LinuxBlockFilePath : MSBlockFilePath), mBlockId);
				//string path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData

				// delete any old file from a previous instance and overwrite with a new one.
				//mFileStream = new FileStream(mPath, FileMode.Create, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete, 8, FileOptions.RandomAccess);
				mFileStream = new FileStream(mPath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 8192, FileOptions.RandomAccess);
			}
			catch (Exception e)
			{
				CNXLog.ErrorFormat("Block Reciever failed for path {0}. {1}", mPath, e.Message);
			}
		}

		protected override bool BlockCompleted()
		{
			try
			{
				//CNXLog.InfoFormat("Testing block {0}", mBlockId);
				//if (mMemStream.Length == mSize && mBytesWritten >= mSize)
				//{
				//    mMemStream.Seek(0, SeekOrigin.Begin);
				//    ushort crc = CRC16.CRCStream(mMemStream);
				//    CNXLog.InfoFormat("CRC check calculated {0} should be {1}.", crc, mCRC);
				//    mCRCOk = (crc == mCRC);
					if (base.BlockCompleted() && mSize > 0)
					{
						//CNXLog.InfoFormat("Block {0} complete.", mBlockId);
						// transfere to permanat storage
						CreateBlockFile();
						mMemStream.WriteTo(mFileStream);
						mFileStream.Flush();
						mFileStream.Close();
					}
				//}
			}
			catch (Exception e)
			{
				CNXLog.ErrorFormat("BlockComplete {0} {1}.", (Block)mBlockId, e.Message);
			}

			return mCRCOk;
		}
	}

	/// <summary>
	/// Manages the construction and reporting of a data download block over CAN
	/// </summary>
	public abstract class BaseBlockReciever
	{
		/// <summary>
		/// What to do when a complete block has arrived.
		/// Provided by derived class.
		/// </summary>
		/// <returns></returns>
		protected virtual bool BlockCompleted()
		{
			try
			{
				CNXLog.InfoFormat("Testing {0} length {1}", (Block)mBlockId, mMemStream.Length);
				if (mSize == 0)
					mCRCOk = true;
				else if (mMemStream.Length >= mSize && mBytesWritten >= mSize)
				{
					mMemStream.Seek(0, SeekOrigin.Begin);
					ushort crc = CRC16.CRCStream(mMemStream, (int)mSize);
					CNXLog.InfoFormat("CRC check calculated {0:X4} should be {1:X4}.", crc, mCRC);
					mCRCOk = (crc == mCRC);
				}
				if (mCRCOk)
					CNXLog.InfoFormat("{0} complete.", (Block)mBlockId);
				SendBlockQueryResponce();
			}
			catch (Exception e)
			{
				CNXLog.ErrorFormat("BlockComplete {0} {1}.", (Block)mBlockId, e.Message);
			}

			return mCRCOk;
		}


#if ANDROID
        public static log4droid CNXLog = new log4droid() {Logger = "CNXLogger"};
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif
        public const string MSBlockFilePath = @"c:\vm-share\";
		public const string LinuxBlockFilePath = @"/var/lib/connexionz/";
		protected MemoryStream mMemStream;
		protected ushort mCRC = 0;
		protected volatile bool mCRCOk = false;
		protected CANLib.BlockFlags mFlags = CANLib.BlockFlags.None;
		protected uint mLastIndexWritten = 0;
		protected volatile uint mBytesWritten = 0;
		protected CANClient mClient;
		protected Object mBlockLock = new Object();
		protected Object mStatusLock = new Object();

		/// <summary>
		/// Subscribe to the block status events.
		/// </summary>
		public event EventHandler<BlockStatusEventArgs> RaiseBlockStatusEvent;

		/// <summary>
		/// Gets the stream of the block.
		/// </summary>
		/// <remarks>The stream will be null if the status of the block is not COMPLETE.</remarks>
		public Stream Stream
		{
			get
			{
				return (BlockStatus == BlockState.COMPLETE) ? (Stream)mMemStream : null;
			}
		}

		/// <summary>
		/// Tests the block integrity.
		/// </summary>
		public BlockState BlockStatus
		{
			get
			{
				// have we had a valid size yet?
				if (mSize == 0)
					return BlockState.NO_CHUNK1;

				// have we got all the chunks?
				if (mBytesWritten < mSize)
					return BlockState.INCOMPLETE;

				return mCRCOk ? BlockState.COMPLETE : BlockState.CRC_MISSMATCH;
			}
		}

		/// <summary>
		/// Indicates whether version information is correct.
		/// </summary>
		public bool VersionAvailable { get { return ((mFlags & CANLib.BlockFlags.VersionPresent) != 0); } }
		protected byte mBlockId;
		/// <summary>
		/// Gets the BlockId that this instance is managing.
		/// </summary>
		public byte BlockId { get { return mBlockId; } }
		protected uint mSize = 0;
		protected byte mBlockVersion = 0;
		/// <summary>
		/// Gets the block version as provided in Chunk1.
		/// </summary>
		/// <remarks>A value of 0 will be returned if the version is 0 or no version has been recieved yet.</remarks>
		public byte BlockVersion { get { return mBlockVersion; } }

		protected byte mProdVersion = 0xff;
		/// <summary>
		/// Gets/Sets the product version.
		/// </summary>
		/// <remarks>A value of 0 will be returned if the version is 0 or no version has been set.</remarks>
		public int ProductVersion
		{
			get { return mProdVersion; }
			set { mProdVersion = (byte)(value & 0xff); }
		}

		/// <summary>
		/// Gets the size of the largest continuous block.
		/// </summary>
		public uint Progress { get { return mLastIndexWritten; } }

		/// <summary>
		/// Initialise a BlockReceiver from a block frame.
		/// </summary>
		/// <param name="frame"></param>
		/// <param name="version">Current product version.</param>
		/// <param name="client">The CAN client to use for frame transferes.</param>
		public BaseBlockReciever(CANLib.CANFrame frame, int version, CANNativeClient client)
		{
			// initialise ID
			mBlockId = (byte)(frame.MailboxId & 0xff);
			// product version
			mProdVersion = (byte)(version & 0xff);

			// subscribe to frame events
			if (client != null)
				mClient = client;
			else
				mClient = new CANNativeClient("can0");
			mClient.RaiseFrameReceivedEvent += FrameReceivedEventHandler;

			// create a stream for the block
			CreateMemoryStream();

			ProcessChunk(frame);
		}

		/// <summary>
		/// Initialise a BlockReceiver.
		/// </summary>
		/// <param name="blockId">Id of the blocks to be processed.</param>
		/// <param name="version">Current product version.</param>
		/// <param name="client">The CAN client to use for frame transferes.</param>
		public BaseBlockReciever(int blockId, int version, CANClient client)
		{
			// initialise ID
			mBlockId = (byte)(blockId & 0xff);
			// product version
			mProdVersion = (byte)(version & 0xff);

			// subscribe to frame events
			mClient = client;
			mClient.RaiseFrameReceivedEvent += FrameReceivedEventHandler;

			// create a stream for the block
			CreateMemoryStream();
		}

		/// <summary>
		/// Initialise a BlockReceiver.
		/// </summary>
		/// <param name="blockId">Id of the blocks to be processed.</param>
		/// <param name="version">Current product version.</param>
		/// <param name="client">The CAN client to use for frame transferes.</param>
		/// <param name="initBlockSize">Initial amount of memory to reserve for the block.</param>
		public BaseBlockReciever(int blockId, int version, CANClient client, uint initBlockSize)
		{
			// initialise ID
			mBlockId = (byte)(blockId & 0xff);
			// product version
			mProdVersion = (byte)(version & 0xff);

			// subscribe to frame events
			mClient = client;
			mClient.RaiseFrameReceivedEvent += FrameReceivedEventHandler;

			// create a stream for the block
			CreateMemoryStream(initBlockSize);
		}

		private void CreateMemoryStream()
		{
			CreateMemoryStream(8096);
		}

		protected void CreateMemoryStream(uint capacity)
		{
			try
			{
				// release existing stream
				if (mMemStream != null)
				{
					mMemStream.Close();
					mMemStream.Dispose();
				}

				// create a stream for the block
				mMemStream = new MemoryStream((int)capacity);
			}
			catch (Exception e)
			{
				CNXLog.ErrorFormat("BaseBlockReciever:CreateMemoryStream capacity {0} : {1}", capacity, e.ToString());
			}

			mBytesWritten = 0;
			mLastIndexWritten = 0;
		}

		/// <summary>
		/// Processes the CAN frame and writes any valid data to the block.
		/// </summary>
		/// <param name="frame">
		/// Block frame.
		/// If the frame was a block query the responce is provided.
		/// Where no responce is required the frame mailbox will be set to 0.
		/// </param>
		/// <returns>The lowest complete data offset.</returns>
		public uint ProcessChunk(CANFrame frame)
		{
			if (mBlockId != (byte)(frame.MailboxId & 0xff))
				return 0;

			try
			{
				// make sure we have a file
				if (mMemStream == null)
					CreateMemoryStream();
				if (mMemStream == null)
					return 0;

				// save the current state
				BlockState state = BlockStatus;

				// process block type
				switch (frame.MailboxId & 0xf00)
				{
					// Chunk 1
					case 0x600:
						lock (mBlockLock)
						{
							ProcessChunk1(frame.Data);
						}
						break;
					// Chunk N
					case 0x700:
						BlockState newState = state;
						lock (mBlockLock)
						{
							ProcessChunkN(frame.Data);
							newState = BlockStatus;
						}
						if (newState != state)
							OnRaiseBlockStatusEvent(new BlockStatusEventArgs(mBlockId, newState));
						break;
					// Block Query
					case 0x400:
						SendBlockQueryResponce();
						break;
					default:
						break;
				}

				//BlockState newState = BlockStatus;
				//if (newState != state)
				//    OnRaiseBlockStatusEvent(new BlockStatusEventArgs(newState));
			}
			catch (Exception e)
			{
				CNXLog.ErrorFormat("ProcessChunk frame {0} failed {1}.", frame.ToString(), e.ToString());
			}

			return mLastIndexWritten;
		}

		/// <summary>
		/// Puts a BlockQueryResponce frame onto the CAN bus.
		/// </summary>
		public void SendBlockQueryResponce()
		{
			CANFrame resp = BuildQueryResponce();
			mClient.Send(resp);
		}

		protected virtual void ProcessChunk1(byte[] data)
		{
			// collect the CRC
			if (data.Length > 1)
			{
				ushort crc = 0;

				// MSB CRC
				crc = data[1];
				crc <<= 8;
				// LSB CRC
				crc |= data[0];

				if (crc != mCRC)
				{
					mCRC = crc;

					// may need a new block file
					if (mCRCOk)
						CreateMemoryStream();

					mCRCOk = false;
				}
			}

			// try the flags field
			if (data.Length > 2)
				mFlags = (CANLib.BlockFlags)data[2];

			// have we got the version
			if (data.Length > 7 && (mFlags & CANLib.BlockFlags.VersionPresent) != 0)
				mBlockVersion = data[7];

			// have we got the size yet
			if (data.Length > 6)
			{
				if (!BitConverter.IsLittleEndian)
					Array.Reverse(data, 3, 4);
				uint size = BitConverter.ToUInt32(data, 3);
				if (size != mSize)
				{
					mSize = size;

					// deal with null blocks
					if (size == 0)
					{
						mLastIndexWritten = 0;
						mCRCOk = true;
						BlockCompleted();
					}
					else
					{
						// may need a new block file
						if (mCRCOk)
							CreateMemoryStream(size);

						mCRCOk = false;
					}
				}
			}
		}

		private void ProcessChunkN(byte[] data)
		{
			// dont bother if the block is complete
			if (mCRCOk)
				return;

			// dont bother if the block is complete
			if (mCRCOk)
				return;

			// make sure there is data
			if (data.Length < 5)
				return;

			// get the offset
			if (!BitConverter.IsLittleEndian)
				Array.Reverse(data, 0, 4);
			uint offset = BitConverter.ToUInt32(data, 0);

			int dataLen = data.Length - 4;

			// seek to the offset
			mMemStream.Seek((long)offset, SeekOrigin.Begin);
			mMemStream.Write(data, 4, dataLen);
			mBytesWritten += (uint)dataLen;

			if (offset != mLastIndexWritten)
				CNXLog.WarnFormat("{0} Missed {1} bytes at 0x{2:x}.", (Block)mBlockId, offset - mLastIndexWritten, offset);

			mLastIndexWritten = offset + (uint)dataLen;

			if (mLastIndexWritten >= mSize && mSize > 0)
				BlockCompleted();
		}

		private CANFrame BuildQueryResponce()
		{
			byte[] data;

			CANFrame frame = new CANFrame();
			try
			{
				frame.MailboxId = 0x500 + (uint)mBlockId;

				// decide what the offset value should be
				uint offset = mLastIndexWritten;
				ushort crc = 0;
				// no active or downloading block
				if (mLastIndexWritten == 0 && mCRC == 0)
					offset = 0;
				else if (BlockStatus == BlockState.COMPLETE)
				{
					offset = 0xffffffff;
					crc = mCRC;
				}

				data = new byte[7];
				// populate the offset
				BitConverter.GetBytes(offset).CopyTo(data, 0);
				if (!BitConverter.IsLittleEndian)
					Array.Reverse(data, 0, 4);
				// populate the crc
				BitConverter.GetBytes(crc).CopyTo(data, 4);
				if (!BitConverter.IsLittleEndian)
					Array.Reverse(data, 4, 2);
				// populate the current version
				data[6] = mProdVersion;

				frame.Data = data;
				CNXLog.InfoFormat("BuildQueryResponce {0} {1}", BlockStatus, frame.ToString());
			}
			catch (Exception e)
			{
				CNXLog.ErrorFormat("BuildQueryResponce {0} {1}", (Block)mBlockId, e.ToString());
			}

			return frame;
		}

		private void FrameReceivedEventHandler(object o, FrameReceivedEventArgs a)
		{
			if ((a.Frame.MailboxId & 0xff) == mBlockId)
				ProcessChunk(a.Frame);
		}

		/// <summary>
		/// Closes the current block file stream.
		/// </summary>
		/// <remarks>
		/// The reciever can still be used.
		/// Any new chuncks will start building a new file.
		/// </remarks>
		public void Close()
		{
			mClient.RaiseFrameReceivedEvent -= FrameReceivedEventHandler;
			mMemStream.Close();
			mMemStream = null;
			mCRC = 0;
			mCRCOk = false;
			mFlags = 0;
			mLastIndexWritten = 0;
		}

		/// <summary>
		/// Event firing method.
		/// </summary>
		/// <param name="frame">The new state of the Block Reciever.</param>
		protected virtual void OnRaiseBlockStatusEvent(BlockStatusEventArgs stateEvent)
		{
			//    Thread thread = new Thread(delegate()
			//    {
			//        // copy the event handler to avoid mid process subscribe/un-subscribe
			//        EventHandler<BlockStatusEventArgs> handler = RaiseBlockStatusEvent;

			//        // Check if there are any Subscribers
			//        if (handler != null)
			//        {
			//            // Call the Event
			//            handler(this, stateEvent);
			//        }
			//    }
			//    );
			//    thread.IsBackground = true;
			//    thread.Start();

			ThreadPool.QueueUserWorkItem((o) =>
			{
				// copy the event handler to avoid mid process subscribe/un-subscribe
				EventHandler<BlockStatusEventArgs> handler = RaiseBlockStatusEvent;

				// Check if there are any Subscribers
				if (handler != null)
				{
					// Call the Event
					handler(this, stateEvent);
				}
			}
			);
		}
	}
}
