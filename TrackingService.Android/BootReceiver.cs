using System;
using Android.App;
using Android.Content;
using Android.Util;
using Android.Widget;


namespace Tracking.Services
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Android.Content.Intent.ActionBootCompleted })]
    public class TrackingServiceBootReceiver : BroadcastReceiver
    {
        public static readonly string logTag = typeof(TrackingServiceBootReceiver).FullName;

        public override void OnReceive(Context context, Intent intent)
        {
            if (TrackingService.trackingServiceStarted == false)
            {
                App.StartLocationService(context);
            }
        }
    }
}