using System;

using Tracking.Services;
namespace CANLib
{
	/// <summary>
	/// Frame reception event notification class.
	/// </summary>
	public class FrameReceivedEventArgs : EventArgs
	{
		public FrameReceivedEventArgs(CANFrame frame)
		{
			mFrame = frame;
		}
		private CANFrame mFrame;
		public CANFrame Frame
		{
			get { return mFrame; }
		}
	}

	public abstract class CANClient
	{

#if ANDROID
        public static log4droid CNXLog = new log4droid() {Logger = "CNXLogger"};
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif

        /// <summary>
        /// Subscribe to the frame received events.
        /// </summary>
        public event EventHandler<FrameReceivedEventArgs> RaiseFrameReceivedEvent;

		/// <summary>
		/// Sends the frame on the CAN bus.
		/// </summary>
		/// <param name="frame">The frame to send.</param>
		/// <returns>Total number of bytes put onto the CAN bus.</returns>
		public abstract int Send(CANFrame frame);

		/// <summary>
		/// Closes the client
		/// </summary>
		public abstract void Close();

		/// <summary>
		/// Event firing method.
		/// </summary>
		/// <param name="frame">The CAN frame just received.</param>
		protected virtual void OnRaiseFrameReceivedEvent(FrameReceivedEventArgs frameEvent)
		{
			//ThreadPool.QueueUserWorkItem((o) =>
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
			//);

			EventHandler<FrameReceivedEventArgs> handler = RaiseFrameReceivedEvent;
			if (handler != null)
			{
				// Call the Event
				handler(this, frameEvent);
			}
		}

	}
}
