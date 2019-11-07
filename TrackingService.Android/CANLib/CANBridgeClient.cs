using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
//using System.Windows.Forms;

namespace CANLib
{
	/// <summary>
	/// Class used to send/recieve CAN frames.
	/// </summary>
	/// <remarks>
	/// The class makes use of the CANBridge (CAN - IP) native application to move frames.
	/// </remarks>
	public class CANBridgeClient : CANClient
	{
		/// <summary>
		/// Delegate to for the reception of CAN frames
		/// </summary>
		/// <param name="a">Received CAN frame wrapper.</param>
		/// <param name="o">Sender.</param>
		public delegate void FrameReceivedEventHandler(object o, FrameReceivedEventArgs a);

		/// <summary>
		/// Subscribe to the frame received events.
		/// </summary>
		//public event EventHandler<FrameReceivedEventArgs> RaiseFrameReceivedEvent;

		private Socket mRxSocket;
		private Socket mTxSocket;
		private int mInPort;
		private int mOutPort;
		private string mAddress;
		private EndPoint mRxEndPoint;
		private EndPoint mTxEndPoint;
		private Thread mRxThread;
		private bool mKeepReceiving = true;
		 
		/// <summary>
		/// Creates a client connection to the CAN bridge which passes CAN frames across an IP socket as datagrams.
		/// </summary>
		/// <param name="address">Server address.</param>
		/// <param name="inPort">Port to receive frames on.</param>
		/// <param name="outPort">Port to transmit frames on.</param>
		public CANBridgeClient(string address, int inPort, int outPort)
		{
			mInPort = inPort;
			mOutPort = outPort;
			mAddress = address;

			// associate with the CAN Bridge
            
			mTxEndPoint = new IPEndPoint(IPAddress.Parse(address), outPort);
			mRxEndPoint = new IPEndPoint(IPAddress.Any, inPort);
			mRxSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
			mRxSocket.Bind(mRxEndPoint);
			mTxSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            // set a decent number of threads in the thread pool
            int worker, completion;
			ThreadPool.GetMinThreads(out worker, out completion);
			CNXLog.InfoFormat("Workers {0} + 16, Completion {1}", worker, completion);
			ThreadPool.SetMinThreads(worker + 16, completion);
			mRxThread = new Thread(new ThreadStart(ReceiveFrames));
			//mRxThread.IsBackground = true;
			mRxThread.Start();
		}

		/// <summary>
		/// Sends a CAN frame across the bridge
		/// </summary>
		/// <param name="frame">The CAN frame to send.</param>
		public override int Send(CANFrame frame)
		{
            try
            {
                return mRxSocket.SendTo(frame.WireFormatArray, mTxEndPoint);
            }
            catch
            {
                CNXLog.Debug(frame.MailboxId.ToString("X"));
                return 0;
            }
		}

		/// <summary>
		/// Event firing method.
		/// </summary>
		/// <param name="frame">The CAN frame just received.</param>
		//protected virtual void OnRaiseFrameReceivedEvent(FrameReceivedEventArgs frameEvent)
		//{
		//    // copy the event handler to avoid mid process subscribe/un-subscribe
		//    EventHandler<FrameReceivedEventArgs> handler = RaiseFrameReceivedEvent;

		//    // Check if there are any Subscribers
		//    if (handler != null)
		//        // Call the Event
		//        handler(this, frameEvent);
		//}

		private void ReceiveFrames()
		{
			byte[] buffer = new byte[13];
			EndPoint ep = mRxEndPoint;
			CANFrame frame = new CANFrame();

			while (mKeepReceiving)
			{
				try
				{
					if (mRxSocket.ReceiveFrom(buffer, ref ep) > 0)
					{
                        CNXLog.Debug(BitConverter.ToString(buffer));
                        // populate a CAN frame
                        frame.WireFormatArray = buffer;
                        CNXLog.Debug(BitConverter.ToString(frame.Data));
                        CNXLog.Debug(frame.MailboxId.ToString("X"));
                        OnRaiseFrameReceivedEvent(new FrameReceivedEventArgs(frame));
					}
				}
				catch (SocketException se)
				{
					// may be OK to continue.
					CNXLog.WarnFormat("ReceiveFrames {0}.", se.Message);
				}
				catch (Exception e)
				{
					// stuffed.
					mKeepReceiving = false;
					CNXLog.ErrorFormat("ReceiveFrames {0}.", e.Message);
					break;
				}
			}

			mRxSocket.Close();
		}

		/// <summary>
		/// Closes the CANBridge
		/// </summary>
		public override void Close()
		{
			mKeepReceiving = false;
			// stop the worker thread by cloing the socket
			mRxSocket.Close();
		}
	}
}
