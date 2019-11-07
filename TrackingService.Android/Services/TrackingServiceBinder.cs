using System;
using Android.OS;

namespace Tracking.Services
{
	//This is our Binder subclass, the LocationServiceBinder
	public class TrackingServiceBinder : Binder
	{
		public TrackingService Service
		{
			get { return this.service; }
		} protected TrackingService service;

		public bool IsBound { get; set; }
			
		// constructor
		public TrackingServiceBinder(TrackingService service)
		{
			this.service = service;
		}
	}
}

