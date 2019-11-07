using System;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Util;
using Android.Widget;

/// <summary>
/// Singleton class for Application wide objects. 
/// </summary>
namespace Tracking.Services
{
	public class App
	{
		// events
		public event EventHandler<ServiceConnectedEventArgs> TrackingServiceConnected = delegate {};

        // declarations
        protected readonly string logTag = "App";
		protected static TrackingServiceConnection trackingServiceConnection;

        // properties
        public static App Current
		{
			get { return current; }
		} private static App current;
		
		public TrackingService TrackingService
		{
			get {
				if (trackingServiceConnection.Binder == null)
					throw new Exception ("Service not bound yet");
				// note that we use the ServiceConnection to get the Binder, and the Binder to get the Service here
				return trackingServiceConnection.Binder.Service;
			}
		}
        #region Application context

        static App()
        {
            current = new App();
        }

        protected App () 
		{
            //allSettings = new string[2];
            // create a new service connection so we can get a binder to the service
            trackingServiceConnection = new TrackingServiceConnection (null);

            // this event will fire when the Service connectin in the OnServiceConnected call 
            trackingServiceConnection.ServiceConnected += (object sender, ServiceConnectedEventArgs e) => {

                Log.Debug (logTag, "Service Connected");
                // we will use this event to notify MainActivity when to start updating the UI
                this.TrackingServiceConnected ( this, e );
            };
        }

        public static void StartLocationService(Context context)
        {
			// Starting a service like this is blocking, so we want to do it on a background thread
			new Task ( () => { 
				
                // Start our main service
                Log.Debug ("App", "Calling StartService");
                Android.App.Application.Context.StartService (new Intent (Android.App.Application.Context, typeof(TrackingService)));

                // bind our service (Android goes and finds the running service by type, and puts a reference
                // on the binder to that service)
                // The Intent tells the OS where to find our Service (the Context) and the Type of Service
                // we're looking for (TrackingService)
                Intent trackingServiceIntent = new Intent (Android.App.Application.Context, typeof(TrackingService));
                Log.Debug ("App", "Calling service binding");

                // Finally, we can bind to the Service using our Intent and the ServiceConnection we
                // created in a previous step.
                Android.App.Application.Context.BindService (trackingServiceIntent, trackingServiceConnection, Bind.AutoCreate);
			} ).Start ();

            Toast.MakeText(context, "The Tracking Service has started", ToastLength.Long).Show();
        }

        public static void StopLocationService ()
        {
            // Check for nulls in case StartLocationService task has not yet completed.
            Log.Debug("App", "StopLocationService");

            // Unbind from the TrackingService; otherwise, StopSelf (below) will not work:
            if (trackingServiceConnection != null)
            {
                Log.Debug("App", "Unbinding from TrackingService");
                Android.App.Application.Context.UnbindService(trackingServiceConnection);
            }

            // Stop the TrackingService:
            if (Current.TrackingService != null)
            { 
                Log.Debug("App", "Stopping the TrackingService");
                Current.TrackingService.StopSelf();
            }
        }

        public void MainActivity_SettingsPassed(object sender, string [] e)
        {
           if (e.Length != 0)
           {
                //allSettings = e;
           }
        }
        #endregion

    }
}


