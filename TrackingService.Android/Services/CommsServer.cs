using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Diagnostics;

using BlockLib;

namespace Tracking.Services
{
	/// <summary>
	/// Frame reception event notification class.
	/// </summary>
	public class DatagramReceivedEventArgs : EventArgs
	{
		public DatagramReceivedEventArgs(byte[] datagram) : this(datagram, datagram.Length)
		{
		}
		public DatagramReceivedEventArgs(byte[] datagram, int length)
		{
			if (datagram != null)
			{
				mDatagram = new byte[length];
				Array.Copy(datagram, mDatagram, length);
			}
		}
		private byte[] mDatagram = null;
		/// <summary>
		/// The datagram recieved.
		/// </summary>
		public byte[] Datagram
		{
			get { return mDatagram; }
		}
	}

	/// <summary>
	/// Frame reception timout event notification class.
	/// </summary>
	public class CommsTimeoutEventArgs : EventArgs
	{
		public CommsTimeoutEventArgs(int secondsSinceReception)
		{
			mOutageSeconds = secondsSinceReception;
		}
		public int mOutageSeconds;
	}

	/// <summary>
	/// Manages communications with the RTT server.
	/// </summary>
	public class CommsServer
	{
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif
        protected Thread mRxThread;
		protected Thread mTxThread;
		protected UdpClient mUDPTxClient;
		protected UdpClient mUDPRxClient;
		protected ushort mMsgNumber = 0;
		protected byte mShortMsgNumber = 0;
		protected DateTime mLastMsg;
		//protected CTM100GPIO mGPIO;
		protected volatile bool mKeepReceiving = true;
		protected BlockTransferManager mTxtMsgBlockHandler;
		protected BlockTransferManager mLogonBlockHandler;
		private int mOutageSeconds = 0;

        /// <summary>
        /// The default port to use for server communications
        /// </summary>
        public const string DefaultEmergencyServer = "ww3.connexionz.co.nz";

		/// <summary>
		/// The default port to use for server communications
		/// </summary>
		public const int DefaultCommsServerPort = 54321;

		/// <summary>
		/// The default number of retries for acknowledged service. 
		/// </summary>
		public const int DefaultRetries = 3;

		/// <summary>
		/// The default timeout in seconds before retrying an acknowledged service message. 
		/// </summary>
		public const int DefaultRetryTimeout = 10;

		/// <summary>
		/// The default port to use for 
		/// </summary>
		public const string EmergencyCNXServer = "connexionz.co.nz";

		/// <summary>
		/// How long to allow a communications outage for before taking action.
		/// Currently 6 minutes.
		/// </summary>
		public static int MaxCommsOutageSeconds = 6 * 60;

		private ushort mCommsAddress;
		/// <summary>
		/// The address used to identify this unit.
		/// </summary>
		public ushort CommsAddress
		{
			get { return mCommsAddress; }
			set { mCommsAddress = value; }
		}

		private byte mCompanyTag;
		/// <summary>
		/// The company tag for the company this vehicle belongs to.
		/// </summary>
		public byte CompanyTag
		{
			get { return mCompanyTag; }
			set { mCompanyTag = value; }
		}

		private string mVehicleId;
		/// <summary>
		/// The ID of the vehicle, rquires CompanyTag to make a unique ID.
		/// </summary>
		public string VehicleId
		{
			get { return mVehicleId; }
			set { mVehicleId = value; }
		}

		private string mRTTServer;
		/// <summary>
		/// The address/name of the RTT server that we'll be communicating with.
		/// </summary>
		public string RTTServer
		{
			get { return mRTTServer; }
			set { mRTTServer = value; }
		}

		private int mRTTTxPort;
		/// <summary>
		/// The port to use to communicate with the RTT server.
		/// </summary>
		public int RTTTxPort
		{
			get { return mRTTTxPort; }
			set { mRTTTxPort = value; }
		}

		private int mRTTRxPort;
		/// <summary>
		/// The port to use to communicate with the RTT server.
		/// </summary>
		public int RTTRxPort
		{
			get { return mRTTRxPort; }
			set { mRTTRxPort = value; }
		}

		private string mEmergencyServer;
		/// <summary>
		/// The address/name of the Emergency server if we loose the RTT server.
		/// </summary>
		public string EmergencyServer
		{
			get { return mEmergencyServer; }
			set { mEmergencyServer = value; }
		}

		private int mEmergencyPort = DefaultCommsServerPort;
		/// <summary>
		/// The port to use to communicate with the server.
		/// </summary>
		public int EmergencyPort
		{
			get { return mEmergencyPort; }
			set { mEmergencyPort = value; }
		}

		/// <summary>
		/// Subscribe to the UDP received events.
		/// </summary>
		public event EventHandler<DatagramReceivedEventArgs> RaiseDatagramReceivedEvent;

		/// <summary>
		/// Subscribe to the rollover events.
		/// </summary>
		public event EventHandler<EventArgs> RaiseMessageCountRolloverEvent;

		/// <summary>
		/// Subscribe to the comms timeout events.
		/// </summary>
		public event EventHandler<int> RaiseCommsOutageEvent;

		/// <summary>
		/// Initialises the communications server and starts communications.
		/// </summary>
		/// <param name="commsAddress">The address used to identify this unit.</param>
		/// <param name="server">The address/name of the RTT server that we'll be communicating with.</param>
		/// <param name="txPort">The port to use to send messages to the server.</param>
		/// <param name="rxPort">The port to use to recieve messages from the server.</param>
		public CommsServer(UInt16 commsAddress, string server, int txPort, int rxPort)
		{
			mCommsAddress = commsAddress;
			RTTServer = server;
			mRTTTxPort = txPort;
			mRTTRxPort = rxPort;

            // start listening to 
            //mGPIO = CTM100GPIO.CreateCTM100GPIO();
            //mGPIO.RestartModem();
            //mGPIO.Led1 = true;
            //mGPIO.Led2 = true;
        }

		/// <summary>
		/// Starts up the communications server.
		/// </summary>
		/// <remarks>
		/// If the server is already working all current activities will be stopped and restarted.
		/// Any mesages in the process of being sent or recieved may be lost.
		/// </remarks>
		public void Start()
		{
			try
			{
				// create a client for the RTT server
				mUDPRxClient = new UdpClient(mRTTRxPort);
				mUDPTxClient = new UdpClient();
				//mUDPTxClient.Ttl = 10;
				mUDPTxClient.Client.SendTimeout = 12000;
				mLastMsg = DateTime.Now;

				mKeepReceiving = true;
                //mRxThread = new Thread(new ThreadStart(RxMethod));
                ////mRxThread.IsBackground = true;
                //mRxThread.Start();

                ThreadStart mUDPReceive = new ThreadStart(RxMethod);
                mRxThread = new Thread(mUDPReceive);
                mRxThread.Start();

            }
			catch (Exception e)
			{
				// may get here if the client cant connect
				if (!e.GetType().Equals(typeof(SocketException)))
					CNXLog.WarnFormat("CommsServer.Start {0}", e.ToString());
			}
		}

		/// <summary>
		/// Stops the communications server.
		/// </summary>
		/// <remarks>Any mesages in the process of being sent or recieved may be lost.</remarks>
		public void Stop()
		{
			mKeepReceiving = false;
			mUDPRxClient.Close();
			mRxThread.Join(MaxCommsOutageSeconds * 1000);
			if (mUDPTxClient != null)
				mUDPTxClient.Close();
		}

		/// <summary>
		/// Sends an un-acknowledged message to the RTT Server
		/// </summary>
		/// <param name="msg"></param>
		public void Send(BasicMobileMessage msg)
		{
			if (msg == null)
			{
				CNXLog.InfoFormat("{0} Could not send null message.", DateTime.Now.TimeOfDay.ToString());
				return;
			}

			TimeSpan span = DateTime.Now.Subtract(mLastMsg);

			msg.CommsAddress = mCommsAddress;
			msg.MessageNumber = mShortMsgNumber++;
			byte[] message = msg.GetBytes(true);
			try
			{
				int count = mUDPTxClient.Send(message, message.Length, mRTTServer, mRTTTxPort);
				if (count != message.Length)
					CNXLog.WarnFormat("Could not send UDP message to server {0}:{1}.", mRTTServer, mRTTTxPort);
				else
					mLastMsg = DateTime.Now;
			}
			catch (Exception e)
			{
				CNXLog.WarnFormat("{0} Can't connect to server {1}:{2}.", mLastMsg, mRTTServer, mRTTTxPort);
				CNXLog.Warn(e.ToString());
				// may get here if the client cant connect
				if (!e.GetType().Equals(typeof(SocketException)))
					CNXLog.Error(e.ToString());
				// consider taking action
				//if (TimeSpan.Compare(span, MaxCommsOutage) > 0)
				//{
					// reset last message to stop any thrashing
					mLastMsg = DateTime.Now;
					CNXLog.InfoFormat("{0} : Restarting UDPClient to {1}:{2}.", mLastMsg, mRTTServer, mRTTTxPort);
					// replace UDP client
					mUDPTxClient.Close();
					mUDPTxClient = new UdpClient();
				//}
			}
			if (mShortMsgNumber == 0)
				OnMessageCountRolloverEvent(new EventArgs());
		}

		/// <summary>
		/// Sends an un-acknowledged message to the RTT Server
		/// </summary>
		/// <param name="msg"></param>
		public void Send(RTTMesg msg)
		{
			if (msg == null)
			{
				CNXLog.ErrorFormat("{0} Could not send null message.", DateTime.Now.TimeOfDay.ToString());
				return;
			}

			TimeSpan span = DateTime.Now.Subtract(mLastMsg);


			msg.CommsAddress = mCommsAddress;
			if (msg.RequiresMesgNo)
				msg.MessageNumber = mShortMsgNumber++;

			byte[] message = msg.GetBytes(true);
			try
			{
				CNXLog.DebugFormat("Sending {0}\n\r{1}", msg.MessageId, HexDump(message));
                int count = mUDPTxClient.Send(message, message.Length, mRTTServer, mRTTTxPort);
                if (count != message.Length)
					CNXLog.ErrorFormat("Could not send UDP message to server {0}:{1}.", mRTTServer, mRTTTxPort);
				else
					mLastMsg = DateTime.Now;
            }
			catch (Exception e)
			{
				CNXLog.WarnFormat("{0} Can't connect to server {1}:{2}.", mLastMsg, mRTTServer, mRTTTxPort);
				CNXLog.Warn(e.ToString());
				// may get here if the client cant connect
				if (!e.GetType().Equals(typeof(SocketException)))
					CNXLog.Error(e.ToString());
				// consider taking action
				//if (TimeSpan.Compare(span, MaxCommsOutage) > 0)
				//{
					// reset last message to stop any thrashing
					mLastMsg = DateTime.Now;
					CNXLog.WarnFormat("{0} : Restarting UDPClient to {1}:{2}.", mLastMsg, mRTTServer, mRTTTxPort);
					// replace UDP client
					mUDPTxClient.Close();
					mUDPTxClient = new UdpClient();
				//}
			}
			if (mShortMsgNumber == 0)
				OnMessageCountRolloverEvent(new EventArgs());
		}

		protected void RxMethod()
		{
			IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, mRTTRxPort);
			CNXLog.WarnFormat("CommsServer->RxMethod Starting");

			while (mKeepReceiving)
			{
				try
				{
                    IAsyncResult asyncResult = mUDPRxClient.BeginReceive(null, null);
                    asyncResult.AsyncWaitHandle.WaitOne(MaxCommsOutageSeconds * 1000);
                    if (asyncResult.IsCompleted)
                    {
                        byte[] udpData = mUDPRxClient.EndReceive(asyncResult, ref remoteEP);
                        mOutageSeconds = 0;
                        OnRaiseDatagramReceivedEvent(new DatagramReceivedEventArgs(udpData));
                    }
                    else
                    {
                        CNXLog.WarnFormat("CommsServer->RxMethod timeout");
                        OnRaiseCommsOutageEvent(null);
                    }
                }
                catch (ObjectDisposedException ode)
				{
					// re-open the socket if we aren't shutting down
					if (mKeepReceiving)
					{
						//mUDPRxClient = new UdpClient(mRTTRxPort);
						CNXLog.WarnFormat("RxMethod : {0}", ode.ToString());
					}
				}
				catch (SocketException se)
				{
					// re-open the socket if we aren't shutting down
					if (mKeepReceiving)
					{
						//mUDPRxClient = new UdpClient(mRTTRxPort);
						CNXLog.WarnFormat("RxMethod : {0}", se.ToString());
					}
				}
				catch (Exception e)
				{
					CNXLog.WarnFormat("RxMethod : {0}", e.ToString());
				}
			}
			CNXLog.WarnFormat("CommsServer->RxMethod Ending");
		}

		/// <summary>
		/// Event firing method.
		/// </summary>
		/// <param name="frame">The Datagram just received.</param>
		protected virtual void OnRaiseDatagramReceivedEvent(DatagramReceivedEventArgs datagramEvent)
		{
			ThreadPool.QueueUserWorkItem((o) =>
			{
				// copy the event handler to avoid mid process subscribe/un-subscribe
				EventHandler<DatagramReceivedEventArgs> handler = RaiseDatagramReceivedEvent;

				// Check if there are any Subscribers
				if (handler != null)
				{
					// Call the Event
					handler(this, datagramEvent);
				}
            }
			);
		}

		/// <summary>
		/// Event firing method.
		/// </summary>
		/// <param name="args">Message count has just rolled over.</param>
		protected virtual void OnMessageCountRolloverEvent(EventArgs args)
		{
			ThreadPool.QueueUserWorkItem((o) =>
			{
                // copy the event handler to avoid mid process subscribe/un-subscribe
                EventHandler<EventArgs> handler = RaiseMessageCountRolloverEvent;

                // Check if there are any Subscribers
                if (handler != null)
                {
                    // Call the Event
                    handler(this, args);
                }

                //When this is called, Sony survived but not Rockchip
                //RaiseMessageCountRolloverEvent(this, args);

            }
			);
		}

		/// <summary>
		/// Event firing method.
		/// </summary>
		protected virtual void OnRaiseCommsOutageEvent(object state)
		{
			mOutageSeconds += MaxCommsOutageSeconds;
			// copy the event handler to avoid mid process subscribe/un-subscribe
			EventHandler<int> handler = RaiseCommsOutageEvent;

			// Check if there are any Subscribers
			if (handler != null)
			{
				// Call the Event
				handler(this, mOutageSeconds);
			}
		}

		static public string HexDump(byte[] buffer)
		{
			return HexDump(buffer, buffer.Length);
		}

		static public string HexDump(byte[] buffer, int count)
		{
			int rowSize = 8;
			StringBuilder dump = new StringBuilder();

			for (int i = 0; i < count; i += rowSize)
			{
				for (int c = 0; c < rowSize && ((i + c) < count); ++c)
					dump.AppendFormat("{0:X2} ", buffer[i + c]);
				dump.Append('\t');
				for (int c = 0; c < rowSize && ((i + c) < count); ++c)
				{
					if (buffer[i + c] < 32 || buffer[i + c] > 127)
						dump.Append(". ");
					else
						dump.AppendFormat("{0} ", Encoding.ASCII.GetString(buffer, i + c, 1));
				}
				dump.Append("\r\n");
			}
			return dump.ToString();
		}
    }
}
