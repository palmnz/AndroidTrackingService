using System;
using Android.App;
using Android.Widget;
using Android.OS;
using Android.Util;
using Android.Locations;
using Android.Content;
using Android.Content.PM;
using Android.Views;
using System.Text;
using Android.Preferences;

namespace Tracking.Services
{
	[Activity (Label = "Tracking Service", MainLauncher = true, Theme = "@style/Theme.Transparent", ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.ScreenLayout)]
	public class MainActivity : Activity//View.IOnTouchListener
    {
        bool isStarted = false;
        static readonly string logTag = typeof(MainActivity).FullName;
        static readonly string SERVICE_STARTED_KEY = "has_service_been_started";

        ISharedPreferences prefs_first = null;

        bool firstRun = true;
        int count = 0;
        int initialCount = 0;

        private TrackingServiceBootReceiver _receiver;
        #region Lifecycle

        //Lifecycle stages
        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);
			Log.Debug (logTag, "OnCreate: Tracking Service is becoming active");

            //retrieve the object providing startup properties
            prefs_first = PreferenceManager.GetDefaultSharedPreferences(this); 

            ////check if the app is first installed
            firstRun = prefs_first.GetBoolean("firstrun", true);

 
            if (firstRun == true)
            {
                RegisterBootReceiver();
                prefs_first.Edit().PutBoolean("firstrun", false).Commit();

                var intent = new Intent(this, typeof(SettingsActivity));
                StartActivity(intent);

                //this call will result in the OnDestroy() to be called
                Finish();
                return;
            }

            if (TrackingService.trackingServiceStarted == false)
            {  
                App.StartLocationService(this);
                    
            }
            else
            {
                Toast.MakeText(this, "The Tracking Service is running", ToastLength.Long).Show();
            }
             
            //if the Tracking Service started, then we will detect if user intent to bring the menu
            if (TrackingService.trackingServiceStarted == true)
            {
                count = prefs_first.GetInt("thecount", 0);
                count = count + 1;
                prefs_first.Edit().PutInt("thecount", count).Commit();
                if (count >= 6)
                {
                    //trying to bring up the menu
                    count = 0;
                    prefs_first.Edit().PutInt("thecount", count).Commit();

                    var intent = new Intent(this, typeof(SettingsActivity));
                    StartActivity(intent);
                }
            }


            //this call will result in the OnDestroy() to be called
            Finish();
        }

        protected override void OnPause()
		{
			Log.Debug (logTag, "OnPause: Location app is moving to background");
			base.OnPause();
		}

	
		protected override void OnResume()
		{
			Log.Debug (logTag, "OnResume: Location app is moving into foreground");
			base.OnResume();
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutBoolean(SERVICE_STARTED_KEY, isStarted);
            base.OnSaveInstanceState(outState);
        }


        protected override void OnDestroy ()
		{
			Log.Debug (logTag, "OnDestroy: Tracking Sevice is becoming inactive");
			base.OnDestroy ();
        }


		#endregion

		#region Android Location Service methods
        private void RegisterBootReceiver()
        {
            IntentFilter filter = new IntentFilter(TrackingServiceBootReceiver.logTag);
            filter.AddCategory(Intent.CategoryDefault);
            _receiver = new TrackingServiceBootReceiver();
            RegisterReceiver(_receiver, filter);
            Log.Info(logTag, "TrackingServiceBootReceiver");
        }
        #endregion
    }
}


