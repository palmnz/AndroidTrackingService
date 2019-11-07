using System;
using System.Threading;

using CANLib;
using Tracking.Services;

namespace BlockLib
{
	public class TransientBlock : IDisposable
	{
		public class BlockInfo
		{
			public ushort mCrc;
			public byte[] mBlockData;
			public int mVersion = -1;
			public byte mBlockId;
			public CANClient mClient;
			public int mRepeat = 0;
			public uint mSendOffset = 0;

			public BlockInfo(CANClient client, byte blockId, int repeat, byte[] blockData)
			{
				mRepeat = repeat;
				InitCommon(client, blockId, blockData);
			}
			public BlockInfo(CANClient client, byte blockId, byte[] blockData)
			{
				InitCommon(client, blockId, blockData);
			}

			private void InitCommon(CANClient client, byte blockId, byte[] blockData)
			{
				mBlockData = blockData;
				//calculate the crc
				mCrc = CRC16.CRCArray(ref mBlockData);
				mClient = client;
				mBlockId = blockId;
			}
		}

		protected byte mBlockId;
		protected CANClient mClient;
		protected Thread mWorker;
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif
        protected BlockInfo mCurrentBlock;

		public BlockInfo CurrentBlock { get { return mCurrentBlock; } }

		public TransientBlock(CANClient client, byte blockId)
		{
			if (client == null)
			{
				throw new NullReferenceException(string.Format("TransientBlock {0} - client NULL", (Block)blockId));
			}
			mBlockId = blockId;
			mClient = client;
		}

		private object mSendLock = new object();

		/// <summary>
		/// Begins a block transfere.
		/// </summary>
		/// <param name="block">The block data to transfere.</param>
		public void Send(byte[] block, bool overrideCurrentTransfere)
		{
			lock (mSendLock)
			{
				try
				{
					// create a block information class
					mCurrentBlock = new BlockInfo(mClient, mBlockId, block);
					SendBlock(mCurrentBlock);
				}
				catch (Exception e)
				{
					CNXLog.WarnFormat("Failed to Start transfere for block {0} {1}", mBlockId, e.Message);
				}
			}
		}

		/// <summary>
		/// Begins a block transfere.
		/// </summary>
		/// <param name="block">The block data to transfere.</param>
		/// <param name="overrideCurrentTransfere">If a current transfere is in progress it will be aborted and a new transfere started.</param>
		/// <param name="offset">Block offset to start from.</param>
		/// <param name="repeat">Number of times to repeat</param>
		public void AsyncSend(byte[] block, bool overrideCurrentTransfere, uint offset = 0, int repeat = 0)
		{
			// create a block information class
			lock (mSendLock)
			{
				try
				{
					mCurrentBlock = new BlockInfo(mClient, mBlockId, repeat, block);
					if (mWorker != null)
					{
						// see if there is an active transfere in progress
						if ((mWorker.ThreadState & ThreadState.Running) == ThreadState.Running)
						{
							if (overrideCurrentTransfere)
								mWorker.Abort();
							else
								mWorker.Join();
						}
					}
					//CNXLog.InfoFormat("Starting Async transfere for block {0}, CRC {1} {2}", mCurrentBlock.mBlockId, mCurrentBlock.mCrc, mCurrentBlock.ToString());
					mWorker = new Thread(SendBlock);
					mWorker.Start(mCurrentBlock);
				}
				catch (Exception e)
				{
					CNXLog.WarnFormat("Failed to Start transfere for block {0} {1}", mBlockId, e.Message);
				}
			}
		}

		public static void SendBlock(object stateInfo)
		{
			if (stateInfo == null)
			{
				CNXLog.ErrorFormat("SendBlock - stateInfo NULL");
				return;
			}
			BlockInfo blockInfo = (BlockInfo)stateInfo;
			//CNXLog.InfoFormat("Running transfere for block {0}, CRC {1} {2}", blockInfo.mBlockId, blockInfo.mCrc, blockInfo.ToString());

			try
			{
				// build a chunk 1 message
				CANFrame chunk1 = BlockTransferManager.BuildChunk1(blockInfo.mBlockId, blockInfo.mCrc, (uint)blockInfo.mBlockData.Length, blockInfo.mVersion, 0);
				// send chunk 1
				//CNXLog.InfoFormat("Sending Block {0} Chunck 1 {1}.", blockInfo.mBlockId, chunk1.ToString());
				blockInfo.mClient.Send(chunk1);

				// transfer the block
				CANFrame frame = new CANFrame();
				frame.MailboxId = (uint)CNXMsgIds.BlockChunkN + (uint)blockInfo.mBlockId;

				byte[] data = new byte[8];
				for (uint offset = 0; offset < blockInfo.mBlockData.Length; offset += 4)
				{
					// set the offset
					BitConverter.GetBytes(offset).CopyTo(data, 0);
					if (!BitConverter.IsLittleEndian)
						Array.Reverse(data, 0, 4);
					uint remainder = (uint)(blockInfo.mBlockData.LongLength - offset);
					int count = (int)((remainder > 4) ? 4 : remainder);
					Array.Copy(blockInfo.mBlockData, offset, data, 4, count);
					frame.DataFromArray(data, 0, 4 + count);
					int sent = blockInfo.mClient.Send(frame);
					//CNXLog.InfoFormat("Sending Block {0} Chunck ({1}) {2}.", blockInfo.mBlockId, offset, frame.ToString());
					Thread.Yield();
					Thread.Sleep(10);
					if (offset % 16 == 0)
					{
						//Console.WriteLine("Block pause.");
						Thread.Yield();
						Thread.Sleep(10);
					}
				}
				// allow time for the block to be consumed
				Thread.Sleep(41);
				if (blockInfo.mRepeat != 0)
				{
					frame.MailboxId = (uint)CNXMsgIds.BlockQuery + (uint)blockInfo.mBlockId;
					frame.ClearData();
					blockInfo.mClient.Send(frame);
				}
			}
			catch (ThreadAbortException)
			{
				// normal behaviour if the transfere is canceled
				CNXLog.WarnFormat("Block {0} transfere cancled.", (Block)blockInfo.mBlockId);
			}
			catch (Exception e)
			{
				CNXLog.ErrorFormat("SendBlock {0} {1}", (Block)blockInfo.mBlockId, e.ToString());
			}
		}

		#region IDisposable Members

		public void Dispose()
		{
			if (mWorker != null)
				mWorker.Abort();
		}

		#endregion
	}
}
