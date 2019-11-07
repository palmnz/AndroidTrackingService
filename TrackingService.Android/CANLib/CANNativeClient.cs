using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.Sockets;

namespace CANLib
{
	public class CANNativeClient : CANClient, IDisposable
	{
		//internal const string cDLL = "/opt/vm-share/NetBeansProjects/cannative/dist/Debug/GNU-Linux-x86/libcannative.so";
		internal const string cDLL = "/usr/lib/libcannative.so";

		[DllImport(cDLL, CharSet = CharSet.Ansi)]
		internal static extern int can_open(string canIfaceName);

		[DllImport(cDLL, CharSet = CharSet.Ansi)]
		internal static extern int can_buffer_open(string canIfaceName, uint bufferSize);

		[DllImport(cDLL, CharSet = CharSet.Ansi)]
		internal static extern void can_close(int canFd);

		[DllImport(cDLL, CharSet = CharSet.Ansi)]
		internal static extern int can_send(int canFd, uint canId, byte can_dlc, byte[] data);

		[DllImport(cDLL, CharSet = CharSet.Ansi)]
		internal static extern int can_recieve(int canFd, ref uint canId, ref byte can_dlc, byte[] data);

		[DllImport(cDLL, CharSet = CharSet.Ansi)]
		internal static extern int can_read(int canFd, byte[] data, int dataLength);

		[DllImport(cDLL, CharSet = CharSet.Ansi)]
		internal static extern int can_getbuffersize(int canFd);

		[DllImport(cDLL, CharSet = CharSet.Ansi)]
		internal static extern int can_setbuffersize(int canFd, int size);

		[DllImport(cDLL, CharSet = CharSet.Ansi)]
		internal static extern int can_setfilter(int canFd, uint[] can_ids, uint[] masks, uint numFilter);

		/// <summary>
		/// Subscribe to the frame received events.
		/// </summary>
		//public event EventHandler<FrameReceivedEventArgs> RaiseFrameReceivedEvent;

		private Thread mRxThread;
		private volatile bool mKeepReceiving = true;
		internal int mCanFd;
		private object sendLock = new object();

		/// <summary>
		/// Creates a CAN client ready for sending and recieving CAN frames.
		/// </summary>
		/// <param name="ifaceName">The CAN interfave name to bind to.</param>
		/// <remarks>This is a Linux only implementation and requires a native library.</remarks>
		public CANNativeClient(string ifaceName) : this(ifaceName, 0)
		{
		}

		/// <summary>
		/// Creates a CAN client ready for sending and recieving CAN frames.
		/// </summary>
		/// <param name="ifaceName">The CAN interfave name to bind to.</param>
		/// <param name="bufferSize">recieve buffer size to set.</param>
		/// <remarks>This is a Linux only implementation and requires a native library.</remarks>
		public CANNativeClient(string ifaceName, uint bufferSize)
		{
			if (bufferSize == 0)
				mCanFd = can_open(ifaceName);
			else
				mCanFd = can_buffer_open(ifaceName, bufferSize);
			if (mCanFd < 0)
			{
				CNXLog.FatalFormat("CANNativeClient can_buffer_open({0}, 0x1000) returned {1}", ifaceName, mCanFd);
				throw new SocketException(mCanFd);
			}

			// organise a decent buffersize for the frames
			int buffersize = can_getbuffersize(mCanFd);
			CNXLog.InfoFormat("CAN buffer size {0}.", buffersize);
			mRxThread = new Thread(new ThreadStart(ReceiveFrame))
			{
				Priority = ThreadPriority.AboveNormal,
				IsBackground = true
			};
			mRxThread.Start();
		}

		/// <summary>
		/// Sends the frame on the CAN bus.
		/// </summary>
		/// <param name="frame">The frame to send.</param>
		/// <returns>Total number of bytes put onto the CAN bus.</returns>
		public override int Send(CANFrame frame)
		{
			// synchronise sends
			//int sent = 0;
			//lock (sendLock)
			//{
			//    int retries = 3;
			//    for (sent = can_send(mCanFd, frame.MailboxId, (byte)frame.DataLength, frame.Data); sent < 4 && --retries > 0; sent = can_send(mCanFd, frame.MailboxId, (byte)frame.DataLength, frame.Data))
			//        Thread.Sleep(1);
			//}

			//return sent;

			// synchronise sends
			int sent = -1;
			uint canId = frame.MailboxId;
			// test 11 or 29 bit addressing
			if (canId > 0x7ff)
				canId |= (uint)CANFrame.IDFlags.CAN_EFF_FLAG;

			lock (sendLock)
			{
				try
				{
					sent = can_send(mCanFd, canId, (byte)frame.DataLength, frame.Data);
				}
				catch (Exception e)
				{
					CNXLog.Error("CANNative.Send", e);
				}
			}

			return sent;
		}

		///// <summary>
		///// Event firing method.
		///// </summary>
		///// <param name="frame">The CAN frame just received.</param>
		//protected virtual void OnRaiseFrameReceivedEvent(FrameReceivedEventArgs frameEvent)
		//{
		//    // copy the event handler to avoid mid process subscribe/un-subscribe
		//    EventHandler<FrameReceivedEventArgs> handler = RaiseFrameReceivedEvent;

		//    // Check if there are any Subscribers
		//    if (handler != null)
		//    {
		//        // Call the Event
		//        handler(this, frameEvent);
		//    }
		//}

		private void ReceiveFrame()
		{
			byte[] buffer = new byte[8];
			CANFrame frame = new CANFrame();
			int rxLen = 0;
			byte length = 0;
			uint canId = 0;

			while (mKeepReceiving)
			{
				try
				{
					rxLen = can_recieve(mCanFd, ref canId, ref length, buffer);
					if (rxLen > 0)
					{
						// populate a CAN frame
						frame.MailboxId = canId;
						frame.DataFromArray(buffer, 0, length);
						base.OnRaiseFrameReceivedEvent(new FrameReceivedEventArgs(frame));
					}
				}
				catch (Exception e)
				{
					// stuffed.
					mKeepReceiving = false;
					CNXLog.Error("CANNative.ReceiveFrame", e);
					break;
				}
			}
			Console.WriteLine("CAN native end recieve loop");
		}

		private void ReceiveFrames()
		{
			const int length = 16;
			byte[] buffer = new byte[length];
			CANFrame frame = new CANFrame();
			int rxLen = 0;

			while (mKeepReceiving)
			{
				try
				{
					rxLen = can_read(mCanFd, buffer, length);
					if (rxLen > 0)
					{
						frame.WireFormatArray = buffer;
						base.OnRaiseFrameReceivedEvent(new FrameReceivedEventArgs(frame));
					}
				}
				catch (Exception e)
				{
					// stuffed.
					mKeepReceiving = false;
					CNXLog.Error("CANNative.ReceiveFrames", e);
					break;
				}
			}
			Console.WriteLine("CAN native end recieve loop");
		}

		public override void Close()
		{
			mKeepReceiving = false;
			can_close(mCanFd);
		}

		#region IDisposable Members

		public void Dispose()
		{
			Close();
		}

		#endregion
	}
}
