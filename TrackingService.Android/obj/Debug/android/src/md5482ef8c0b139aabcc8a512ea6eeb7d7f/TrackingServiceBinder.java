package md5482ef8c0b139aabcc8a512ea6eeb7d7f;


public class TrackingServiceBinder
	extends android.os.Binder
	implements
		mono.android.IGCUserPeer
{
/** @hide */
	public static final String __md_methods;
	static {
		__md_methods = 
			"";
		mono.android.Runtime.register ("Tracking.Services.TrackingServiceBinder, tracking.Android, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", TrackingServiceBinder.class, __md_methods);
	}


	public TrackingServiceBinder ()
	{
		super ();
		if (getClass () == TrackingServiceBinder.class)
			mono.android.TypeManager.Activate ("Tracking.Services.TrackingServiceBinder, tracking.Android, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "", this, new java.lang.Object[] {  });
	}

	public TrackingServiceBinder (md5482ef8c0b139aabcc8a512ea6eeb7d7f.TrackingService p0)
	{
		super ();
		if (getClass () == TrackingServiceBinder.class)
			mono.android.TypeManager.Activate ("Tracking.Services.TrackingServiceBinder, tracking.Android, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", "Tracking.Services.TrackingService, tracking.Android, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null", this, new java.lang.Object[] { p0 });
	}

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
