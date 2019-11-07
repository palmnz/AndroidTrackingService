using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
//using System.IO.Packaging;
using System.Threading;
using System.Xml;
using System.Xml.XPath;
using rttSVOX;
using Utility;

using PairString = System.Collections.ObjectModel.Pair<string, string>;

namespace rttVehicle
{
	public enum RequestType{ DisplayNextStop, Farebox, FareSet, HeadSign, SynthInternal, SynthExternal, DisplayNextStopExtra, SignalPriority, FerryPlatformArrival, FerryPlatformDeparture };
	internal enum RequestSubType { None, NextStop, NextStopConn, ServiceAlert, Stopping };
	public sealed class RequestEventArgs : EventArgs
	{
		private RequestType m_Type;
		private RequestSubType m_SubType;
		private object m_Value;

		internal RequestEventArgs( RequestType type, object value, RequestSubType subtype = RequestSubType.None )
		{
			m_Type = type;
			m_SubType = subtype;
			m_Value = value;
		}

		internal RequestSubType SubType
		{
			get { return m_SubType; }
		}

		public RequestType Type
		{
			get { return m_Type; }
		}

		public object Value
		{
			get { return m_Value; }
			internal set{ m_Value = value; }
		}
	}
	public enum Resource { RoutePatternForVehicle, VehicleConfig, ServiceAlertForVehicle, DriverConfig };

	public sealed class DriverMsgCategory
	{
		string m_strCategory;
		ReadOnlyCollection<string> m_colMsg;

		public DriverMsgCategory( string strCategory, ReadOnlyCollection<string> colMsg )
		{
			m_strCategory = strCategory;
			m_colMsg = colMsg;
		}

		public string Category
		{
			get{ return m_strCategory; }
		}

		public ReadOnlyCollection<string> Messages
		{
			get{ return m_colMsg; }
		}
	}

	public sealed class Farebox
	{
		private string m_strRouteNo;
		private int? m_nTripNo;

		public Farebox( string strRouteNo, int? nTripNo )
		{
			m_strRouteNo = strRouteNo;
			m_nTripNo = nTripNo;
		}

		public string RouteNo
		{
			get { return m_strRouteNo; }
		}

		public int? TripNo
		{
			get { return m_nTripNo; }
		}
	}

	public sealed class NextStopExtra
	{
		private bool m_bResetHint;
		private string m_strRouteNo, m_strDestination, m_strConnPrefix, m_strSAPrefix, m_strConnection, m_strServiceAlert;
		private short m_nRouteTag;
		private int m_nNextStopIndex;
		private ReadOnlyCollection<string> m_colNextStop;

		public NextStopExtra( bool bResetHint, short nRouteTag, string strRouteNo, string strDestination, int nNextStopIndex, ReadOnlyCollection<string> colNextStop, string strConnPrefix, string strConnection, string strSAPrefix, string strServiceAlert )
		{
			m_bResetHint = bResetHint;
			m_nRouteTag = nRouteTag;
			m_strRouteNo = strRouteNo;
			m_strDestination = strDestination;
			m_strConnPrefix = strConnPrefix;
			m_strConnection = strConnection;
			m_strSAPrefix = strSAPrefix;
			m_strServiceAlert = strServiceAlert;
			m_nNextStopIndex = nNextStopIndex;
			m_colNextStop = colNextStop;
		}

		public string Connection
		{
			get { return m_strConnection; }
		}

		public string ConnectionPrefix
		{
			get { return m_strConnPrefix; }
		}

		public string Destination
		{
			get { return m_strDestination; }
		}

		public int NextStopIndex
		{
			get { return m_nNextStopIndex; }
		}

		public ReadOnlyCollection<string> NextStopList
		{
			get { return m_colNextStop; }
		}

		public bool ResetHint
		{
			get { return m_bResetHint; }
		}

		public string RouteNo
		{
			get { return m_strRouteNo; }
		}

		public short RouteTag
		{
			get { return m_nRouteTag; }
		}

		public string ServiceAlert
		{
			get { return m_strServiceAlert; }
		}

		public string ServiceAlertPrefix
		{
			get { return m_strSAPrefix; }
		}
	}

	public struct ResourcePath
	{
		private Resource m_Resource;
		private string m_strPath;

		public ResourcePath( Resource Resource, string strPath )
		{
			m_Resource = Resource;
			m_strPath = strPath;
		}

		public Resource Resource
		{
			get { return m_Resource; }
		}

		public string Path
		{
			get { return m_strPath; }
		}
	}

	public struct ResourceVersion
	{
		private Resource m_Resource;
		private byte? m_nVersion;

		public ResourceVersion( Resource Resource, byte? nVersion )
		{
			m_Resource = Resource;
			m_nVersion = nVersion;
		}

		public Resource Resource
		{
			get { return m_Resource; }
		}

		public byte? Version
		{
			get { return m_nVersion; }
		}
	}

	public sealed class Vehicle : IDisposable
	{
#if( LINUX )
		private const string cCNXPath = "/var/lib/connexionz/";
#else
		private const string cCNXPath = "T:\\";
#endif
		private const string cVehicleInfoPath = cCNXPath + "VehicleInfo.txt";

		[FlagsAttribute]
		internal enum FareboxFields : byte { None = 0, FareSet = 1, RouteNo = 2, TripNo = 4, RouteNoTripNo = 6 };
		[FlagsAttribute]
		internal enum AVMode : byte { None = 0, Audio = 1, Display = 2, MediaPlayer = 4 };

		private DriverConfig m_configD = new DriverConfig();
		private RoutePatternConfig m_configRP = new RoutePatternConfig();
		private ServiceAlertConfig m_configSA = new ServiceAlertConfig();
		private VehicleConfig m_configV = new VehicleConfig();

        private bool m_fFerry;

		private string m_strHeadSign = null, m_strLastSA = null, m_strNSDStopping = null, m_strNSDStoppingConcat = null;
		private string[] m_straLastNSD = { " ", null };

		private GPSFix m_Fix = null;
		private double m_nSpeedWeighted = 2.0;
		private Queue<TripStatus> m_qTS = new Queue<TripStatus>( 2 );
		private Queue<RequestEventArgs> m_qRQ = new Queue<RequestEventArgs>( 6 );
		private bool m_bTSThreadPoolActive = false, m_bRQThreadPoolActive = false, m_bStopping = false, m_bSignalPriority = false;

		private byte m_nRouteNoFmt = 0, m_nCompanyTag = 1;
		private ushort m_nComsAddress = 0;
		private string m_strVehicleID = null;
		private PairStringCollection m_colRouteNoForAudio = new PairStringCollection();

		public event EventHandler<RequestEventArgs> Request;

		public Vehicle(bool  fFerry = false)
		{
			VehicleinfoLoad();

			foreach( Resource res in Enum.GetValues( typeof( Resource ) ) )
			{
				XmlDocument doc = new XmlDocument();
				string strPath = cCNXPath + res.ToString() + ".xml";
				if( File.Exists( strPath ) )
				{
					doc.Load( strPath );
					switch( res )
					{
						case Resource.DriverConfig:
							m_configD = new DriverConfig( doc );
							break;
						case Resource.RoutePatternForVehicle:
							m_configRP = new RoutePatternConfig( doc );
							break;
						case Resource.ServiceAlertForVehicle:
							m_configSA = new ServiceAlertConfig( doc );
							break;
						case Resource.VehicleConfig:
							m_configV = new VehicleConfig( doc );
							break;
					}
				}

				switch( res )
				{
					case Resource.DriverConfig:
						break;
					case Resource.RoutePatternForVehicle:
						break;
					case Resource.ServiceAlertForVehicle:
						break;
					case Resource.VehicleConfig:
						m_nRouteNoFmt = m_configV.RouteNoFmt;
						m_strNSDStopping = m_configV.NSDStopping;
						m_strNSDStoppingConcat = m_configV.NSDStoppingConcat;
						break;
				}
			}

            m_fFerry = fFerry;
		}

		public ReadOnlyCollection<string> GetDriverList()
		{
			DriverConfig config = null;
			Interlocked.Exchange<DriverConfig>( ref config, m_configD );

			return new ReadOnlyCollection<string>( config.GetDriverContent( m_nCompanyTag ) );
		}

		public ReadOnlyCollection<DriverMsgCategory> GetDriverMessages()
		{
			DriverConfig config = null;
			Interlocked.Exchange<DriverConfig>( ref config, m_configD );

			return new ReadOnlyCollection<DriverMsgCategory>( config.GetMsgContent( m_nCompanyTag ) );
		}

		public byte GetResourceVersion( Resource res )
		{
			byte nVersion = 0;
			switch( res )
			{
				case Resource.DriverConfig:
					nVersion = m_configD.Version;
					break;
				case Resource.RoutePatternForVehicle:
					nVersion = m_configRP.Version;
					break;
				case Resource.ServiceAlertForVehicle:
					nVersion = m_configSA.Version;
					break;
				case Resource.VehicleConfig:
					nVersion = m_configV.Version;
					break;
			}

			return nVersion;
		}

		//public byte OnNewResource( ResourcePath resPath )
		//{
		//	byte nVersion = 0;
		//	string strFile = resPath.Resource.ToString() + ".xml";

		//	using( Package pkg = Package.Open( resPath.Path, FileMode.Open, FileAccess.Read ) )
		//	{
		//		XmlDocument doc = new XmlDocument();
		//		doc.Load( pkg.GetPart( new Uri( "/" + strFile, UriKind.Relative ) ).GetStream() );
		//		switch( resPath.Resource )
		//		{
		//			case Resource.DriverConfig:
		//			{
		//				DriverConfig config = new DriverConfig( doc );
		//				nVersion = config.Version;
		//				if( nVersion != m_configD.Version )
		//				{
		//					doc.Save( cCNXPath + strFile );
		//					Interlocked.Exchange<DriverConfig>( ref m_configD, config );
		//				}
		//				break;
		//			}
		//			case Resource.RoutePatternForVehicle:
		//			{
		//				RoutePatternConfig config = new RoutePatternConfig( doc );
		//				nVersion = config.Version;
		//				if( nVersion != m_configRP.Version )
		//				{
		//					doc.Save( cCNXPath + strFile );
		//					Interlocked.Exchange<RoutePatternConfig>( ref m_configRP, config );
		//				}
		//				break;
		//			}
		//			case Resource.ServiceAlertForVehicle:
		//			{
		//				ServiceAlertConfig config = new ServiceAlertConfig( doc );
		//				nVersion = config.Version;
		//				if( nVersion != m_configSA.Version )
		//				{
		//					doc.Save( cCNXPath + strFile );
		//					Interlocked.Exchange<ServiceAlertConfig>( ref m_configSA, config );
		//				}
		//				break;
		//			}
		//			case Resource.VehicleConfig:
		//			{
		//				VehicleConfig config = new VehicleConfig( doc );
		//				nVersion = config.Version;
		//				bool bChg = false;
		//				if( nVersion != m_configV.Version )
		//				{
		//					bChg = true;
		//					doc.Save( cCNXPath + strFile );
		//					Interlocked.Exchange<VehicleConfig>( ref m_configV, config );
		//				}

		//				if( bChg )
		//				{
		//					lock( m_colRouteNoForAudio )
		//					{
		//						m_nRouteNoFmt = config.RouteNoFmt;
		//						m_colRouteNoForAudio.Clear();
		//					}
		//					Interlocked.Exchange<string>( ref m_strNSDStopping, config.NSDStopping );
		//					Interlocked.Exchange<string>( ref m_strNSDStoppingConcat, config.NSDStoppingConcat );

		//					SVOX.UnpackPil( pkg, cCNXPath );
		//				}
		//				break;
		//			}
		//		}
		//	}

		//	return nVersion;
		//}

		//
		public void NoGPS()
		{
			Interlocked.Exchange<GPSFix>( ref m_Fix, null );
		}

		public void UpdatePosition( double nLat, double nLong, double nSpeed )
		{
			m_nSpeedWeighted = ( m_nSpeedWeighted + nSpeed * 2.0 ) / 3.0;
			Interlocked.Exchange<GPSFix>( ref m_Fix, new GPSFix( nLat, nLong, nSpeed, m_nSpeedWeighted ) );
		}

		public void Dispose()
		{
			lock( m_qTS )
			{
				if( m_bTSThreadPoolActive )
				{
					m_qTS.Clear();
					m_qTS.Enqueue( null );
				}
			}

			lock( m_qRQ )
			{
				if( m_bRQThreadPoolActive )
				{
					m_qRQ.Clear();
					m_qRQ.Enqueue( null );
				}
			}
		}

		public void NoTrip()
		{
			lock( m_qTS )
			{
				if( m_bTSThreadPoolActive )
					m_qTS.Enqueue( new TripStatus() );
			}
		}

		public void SetOffRoute()
		{
			lock( m_qTS )
			{
				if( m_bTSThreadPoolActive )
					m_qTS.Enqueue( new TripStatus( true ) );
			}
		}

		public void SetStopping( bool bEnable )
		{
			QueueRequest( new RequestEventArgs( RequestType.DisplayNextStop, bEnable, RequestSubType.Stopping ) );
		}

		public void UpdateRP( short nRouteTag, int nRP, int? nTripNo = null, ushort? nServiceStart = null, bool bSignalPriority = false )
		{
			UpdateRP( new TripStatus( nRouteTag, nRP, nTripNo, nServiceStart, bSignalPriority ) );
		}

		private void UpdateRP( TripStatus ts )
		{
			lock( m_qTS )
			{
				if( m_bTSThreadPoolActive )
					m_qTS.Enqueue( ts );
				else
				{
					m_bTSThreadPoolActive = true;
					ThreadPool.QueueUserWorkItem( new WaitCallback( OnTripStatus ), ts );
				}
			}
		}

		private void OnTripStatus( object parm )
		{
			Debug.WriteLine( string.Format( "{0} {1}", DateTime.Now.ToString( "HH:mm:ss.fff" ), "OnTripStatus" ) );

			RoutePatternConfig configRP = null;
			ServiceAlertConfig configSA = null;
			VehicleConfig configV = null;
			Interlocked.Exchange<RoutePatternConfig>( ref configRP, m_configRP );
			Interlocked.Exchange<ServiceAlertConfig>( ref configSA, m_configSA );
			Interlocked.Exchange<VehicleConfig>( ref configV, m_configV );

			bool bContinue = true, bDispose = false;
			Trip trip = null;
			TripStatus ts = (TripStatus)parm;
			Timeout timeoutFix = new Timeout( TimeSpan.FromMinutes( 2 ) ), timeoutOnRoute = new Timeout( TimeSpan.FromSeconds( 500 ) );

			do
			{
				if( ts != null )
				{
					if( ts.NoTrip )
						trip = null;
					else if( !ts.OffRoute )
					{
						timeoutOnRoute.Reset();
						if( trip == null || ts.RouteTag != trip.RouteTag || ts.RP < trip.RP || ts.TripNo != trip.TripNo || ts.ServiceStart != trip.ServiceStart )
							trip = Trip.Create( this, ts.RouteTag, ts.RP, ts.TripNo, ts.ServiceStart, m_configRP, m_configSA, m_configV, m_fFerry );
						else
							trip.ProgressRP( ts.RP );

						EnableSignalPriority( ts.SignalPriority );
					}
					else if( trip != null )
					{
						trip.OffRoute = true;
						EnableSignalPriority( false );
					}
				}

				if( trip != null )
				{
					GPSFix fix = m_Fix;

					bool bEndTrip = false;
					if( timeoutOnRoute.HasExpired )
						bEndTrip = true;
					else if( fix == null || !UTM.IsValidLL4Zone( configV.UTMZoneNo, configV.UTMZoneLetter, fix.Lat, fix.Long ) )
						bEndTrip = timeoutFix.HasExpired;
					else
					{
						timeoutFix.Reset();
						//if( m_fFerry || !trip.OffRoute )
						//	bEndTrip = !trip.UpdatePosition( UTM.LL2UTM( configV.UTMZoneNo, fix.Lat, fix.Long ), fix.Speed, fix.SpeedWeighted );
					}

					if( bEndTrip )
						trip = null;
					else
						Thread.Sleep( 999 );
				}

				lock( m_qTS )
				{
					if( m_qTS.Count == 0 )
						ts = null;
					else
					{
						ts = m_qTS.Dequeue();
						if( ts == null )
							bDispose = true;
					}

					if( bDispose || ( trip == null && ts == null ) )
						bContinue = m_bTSThreadPoolActive = false;
				}
			}
			while( bContinue );

			if( !bDispose )
			{
				QueueRequest( new RequestEventArgs( RequestType.DisplayNextStop, null ) );
				QueueRequest( new RequestEventArgs( RequestType.DisplayNextStopExtra, null ) );
				if( configV.HeadSignDefault != null )
					QueueRequest( new RequestEventArgs( RequestType.HeadSign, configV.HeadSignDefault ) );
				EnableSignalPriority( false );
			}
		}

		internal void EnableSignalPriority( bool bEnable )
		{
			if( m_bSignalPriority != bEnable )
			{
				m_bSignalPriority = bEnable;
				QueueRequest( new RequestEventArgs( RequestType.SignalPriority, bEnable ) );
			}
		}

		internal void QueueRequest( RequestEventArgs args )
		{
			System.Diagnostics.Debug.Assert( args != null );
			lock( m_qRQ )
			{
				if( m_bRQThreadPoolActive )
					m_qRQ.Enqueue( args );
				else
				{
					m_bRQThreadPoolActive = true;
					ThreadPool.QueueUserWorkItem( new WaitCallback( RaiseRequest ), args );
				}
			}
		}

		//m_bStopping, m_strHeadSign, m_straLastNSD, m_strLastSA set only in this method
		private void RaiseRequest( object parm )
		{
			bool bContinue = true;
			RequestEventArgs args = (RequestEventArgs)parm;
			do
			{
				bool bRaise = true;
				switch( args.Type )
				{
					case RequestType.DisplayNextStop:
						switch( args.SubType )
						{
							case RequestSubType.None:
								m_straLastNSD[0] = " ";
								m_straLastNSD[1] = m_strLastSA = null;
								args.Value = m_bStopping ? m_strNSDStopping : m_straLastNSD[0];
								break;
							case RequestSubType.ServiceAlert:
								m_strLastSA = args.Value.ToString();
								if( m_bStopping )
									bRaise = false;
								break;
							default:
								switch( args.SubType )
								{
									case RequestSubType.NextStop:
										m_straLastNSD[0] = args.Value.ToString();
										m_straLastNSD[1] = null;
										m_strLastSA = null;
										break;
									case RequestSubType.NextStopConn:
										m_straLastNSD = (string[])args.Value;
										m_strLastSA = null;
										break;
									case RequestSubType.Stopping:
										m_bStopping = (bool)args.Value;
										break;
								}

								if( m_bStopping )
								{
									if( m_straLastNSD[0].Length > 1 || (  m_straLastNSD[0].Length > 0 && m_straLastNSD[0][0] != ' ' ) )
										args.Value = m_strNSDStopping + m_strNSDStoppingConcat + m_straLastNSD[0];
									else
										args.Value = m_strNSDStopping;
								}
								else if( m_strLastSA != null )
									args.Value = m_strLastSA;
								else if( m_straLastNSD[1] != null )
									args.Value = m_straLastNSD[0] + m_straLastNSD[1];
								else
									args.Value = m_straLastNSD[0];
								break;
						}
						break;
					case RequestType.HeadSign:
					{
						if( m_strHeadSign != args.Value.ToString() )
							m_strHeadSign = args.Value.ToString();
						else
							bRaise = false;
						break;
					}
				}

				if( bRaise )
				{
					EventHandler<RequestEventArgs> handler = Request;
					if( handler != null )
						handler( null, args );
				}

				lock( m_qRQ )
				{
					if( m_qRQ.Count == 0 )
						args = null;
					else
						args = m_qRQ.Dequeue();

					if( args == null )
						bContinue = m_bRQThreadPoolActive = false;
				}
			}
			while( bContinue );
		}

		//
#if( DEBUG )
		public string GetRouteNoForAudio( string strRouteNo )
#else
		internal string GetRouteNoForAudio( string strRouteNo )
#endif
		{
			if( strRouteNo.Length == 0 )
				return string.Empty;

			string str = null;
			lock( m_colRouteNoForAudio )
			{
				if( m_colRouteNoForAudio.Contains( strRouteNo ) )
					str = m_colRouteNoForAudio[strRouteNo].Second;
			}

			if( str == null )
			{
				bool bSplitChars = false;
				int nRouteNo;
				if( !int.TryParse( strRouteNo, out nRouteNo ) )
					str = strRouteNo;
				else if( strRouteNo[0] == '0' || nRouteNo > 999 )
					bSplitChars = true;
				else if( nRouteNo <= 99 )
					str = strRouteNo;
				else if( nRouteNo % 100 == 0 )
					str = strRouteNo[0] + " hundred";
				else if( strRouteNo[1] == '0' )
					str = strRouteNo[0] + "O" + strRouteNo[2];
				else if( m_nRouteNoFmt == 0 )
					str = strRouteNo[0] + " " + strRouteNo.Substring( strRouteNo.Length - 2, 2 );
				else// if( m_nRouteNoFmt == 1 )
					bSplitChars = true;

				if( bSplitChars )
				{
					str = string.Empty;
					foreach( char ch in strRouteNo.ToUpper() )
					{
						if( str.Length == 0 )
							str += ch;
						else
							str += " " + ch;
					}
				}

				str += ",";
				lock( m_colRouteNoForAudio )
				{
					if( !m_colRouteNoForAudio.Contains( strRouteNo ) )
						m_colRouteNoForAudio.Add( new PairString( strRouteNo, str ) );
				}
			}

			return str;
		}

		internal void VehicleinfoLoad()
		{
			if( File.Exists( cVehicleInfoPath ) )
				using( TextReader tr = File.OpenText( cVehicleInfoPath ) )
				{
					m_nCompanyTag = byte.Parse( tr.ReadLine() );
					m_nComsAddress = ushort.Parse( tr.ReadLine() );
					m_strVehicleID = tr.ReadLine();
				}
		}

		internal void VehicleinfoSave()
		{
			using( TextWriter tw = File.CreateText( cVehicleInfoPath ) )
			{
				tw.WriteLine( m_nCompanyTag.ToString() );
				tw.WriteLine( m_nComsAddress.ToString() );
				if( m_strVehicleID != null )
					tw.WriteLine( m_strVehicleID );
			}
		}

		//
		public byte CompanyTag
		{
			get { return m_nCompanyTag; }
			set
			{
				if( m_nCompanyTag != value )
				{
					m_nCompanyTag = value;
					VehicleinfoSave();
				}
			}
		}

		public ushort ComsAddress
		{
			get { return m_nComsAddress; }
			set
			{
				if( m_nComsAddress != value )
				{
					m_nComsAddress = value;
					VehicleinfoSave();
				}
			}
		}

		public string VehicleID
		{
			get { return m_strVehicleID; }
			set
			{
				if( m_strVehicleID != value )
				{
					m_strVehicleID = value;
					VehicleinfoSave();
				}
			}
		}

#if( DEBUG )
		public XmlDocument DocRP
		{
			get { return m_configRP.Doc; }
		}

		public int UTMZoneNo
		{
			get { return m_configV.UTMZoneNo; }
		}
#endif

		internal sealed class DriverConfig
		{
			private XmlDocument m_Doc = null;
			private byte m_nVersion = 0;

			internal DriverConfig()
			{
				XmlDocument m_Doc = new XmlDocument();
				m_Doc.LoadXml( "<DC/>" );
			}

			internal DriverConfig( XmlDocument doc )
			{
				m_Doc = doc;
				m_nVersion = (byte)BaseN.S64ToInt( m_Doc.DocumentElement.GetAttribute( "V", string.Empty ) );
			}

			internal Collection<string> GetDriverContent( byte nCompanyTag )
			{
				Collection<string> col = new Collection<string>();

				XPathNodeIterator it = (XPathNodeIterator)m_Doc.CreateNavigator().Evaluate( "//C[@c='" + nCompanyTag.ToString() + "']/D" );
				while( it.MoveNext() )
					col.Add( it.Current.GetAttribute( "d", string.Empty ) );

				return col;
			}

			internal Collection<DriverMsgCategory> GetMsgContent( byte nCompanyTag )
			{
				Collection<DriverMsgCategory> col = new Collection<DriverMsgCategory>();

				XPathNodeIterator it = (XPathNodeIterator)m_Doc.CreateNavigator().Evaluate( "//C[@c='" + nCompanyTag.ToString() + "']/T" );
				while( it.MoveNext() )
				{
					string strCategory = it.Current.GetAttribute( "N", string.Empty );
					Collection<string> colMsg = new Collection<string>();

					if( it.Current.MoveToFirstChild() )
					{
						do
						{
							colMsg.Add( it.Current.GetAttribute( "m", string.Empty ) );
						} while( it.Current.MoveToNext() );

						it.Current.MoveToParent();
					}

					col.Add( new DriverMsgCategory( strCategory, new ReadOnlyCollection<string>( colMsg ) ) );
				}

				return col;
			}

			internal byte Version
			{
				get { return m_nVersion; }
			}
		}

		internal sealed class RoutePatternConfig
		{
			private XmlDocument m_Doc = null;
			private byte m_nVersion = 0;
			private FareboxFields m_FareboxFields = FareboxFields.None;
			private AVMode m_ConnMode = AVMode.None;
			private ushort m_nNSAReminderSec = 0;

			internal RoutePatternConfig()
			{
				XmlDocument m_Doc = new XmlDocument();
				m_Doc.LoadXml( "<R/>" );
			}

			internal RoutePatternConfig( XmlDocument doc )
			{
				m_Doc = doc;
				m_nVersion = (byte)BaseN.S64ToInt( m_Doc.DocumentElement.GetAttribute( "V", string.Empty ) );

				{
					byte n = 0;
					string str = m_Doc.DocumentElement.GetAttribute( "F", string.Empty );
					if( str.Length == 0 || !byte.TryParse( str, out n ) )
						m_FareboxFields = FareboxFields.None;
					else
						m_FareboxFields = (FareboxFields)n;
				}
				{
					byte n = 0;
					string str = m_Doc.DocumentElement.GetAttribute( "C", string.Empty );
					if( str.Length == 0 || !byte.TryParse( str, out n ) )
						m_ConnMode = AVMode.None;
					else
						m_ConnMode = (AVMode)n;
				}

				{
					string str = m_Doc.DocumentElement.GetAttribute( "R", string.Empty );
					if( str.Length == 0 )
						m_nNSAReminderSec = 0;
					else
						m_nNSAReminderSec = (ushort)( 60 * BaseN.S64ToInt( str ) );
				}
			}

			internal bool GetContent( string strRouteTag64, out string strProjectTag, out string strRouteNo, out double nRouteLength, out string strRPcsv )
			{
				bool bOK = false;

				strProjectTag = strRouteNo = strRPcsv = null;
				nRouteLength = 0.0;

				XPathNodeIterator it = (XPathNodeIterator)m_Doc.CreateNavigator().Evaluate( "//P[@T='" + strRouteTag64 + "']" );
				if( it.MoveNext() )
				{
					bOK = true;
					strProjectTag = it.Current.GetAttribute( "p", string.Empty ); if( strProjectTag.Length == 0 ) strProjectTag = "1";
					strRouteNo = it.Current.GetAttribute( "N", string.Empty );
					nRouteLength = BaseN.S64ToInt( it.Current.GetAttribute( "L", string.Empty ) );
					strRPcsv = it.Current.Value;
				}

				return bOK;
			}

#if( DEBUG )
			internal XmlDocument Doc
			{
				get { return m_Doc; }
			}
#endif

			internal FareboxFields FareboxFields
			{
				get { return m_FareboxFields; }
			}

			internal AVMode ConnMode
			{
				get { return m_ConnMode; }
			}

			internal byte Version
			{
				get { return m_nVersion; }
			}

			internal ushort NSAReminderSec
			{
				get { return m_nNSAReminderSec; }
			}
		}

		internal sealed class ServiceAlertConfig
		{
			private XmlDocument m_Doc = null;
			private byte m_nVersion = 0;
			private AVMode m_SAMode = AVMode.None;

			internal ServiceAlertConfig()
			{
				XmlDocument m_Doc = new XmlDocument();
				m_Doc.LoadXml( "<S/>" );
			}

			internal ServiceAlertConfig( XmlDocument doc )
			{
				m_Doc = doc;
				m_nVersion = (byte)BaseN.S64ToInt( m_Doc.DocumentElement.GetAttribute( "V", string.Empty ) );

				byte n = 0;
				string str = m_Doc.DocumentElement.GetAttribute( "E", string.Empty );
				if( str.Length == 0 || !byte.TryParse( str, out n ) )
					m_SAMode = AVMode.None;
				else
					m_SAMode = (AVMode)n;
			}

			internal string GetContent( string strProjectTag, string strRouteNo )
			{
				string strContent = null;

				XPathNodeIterator it = (XPathNodeIterator)m_Doc.CreateNavigator().Evaluate( "//P[@p='" + strProjectTag + "']/R[@N='" + strRouteNo + "']" );
				if( it.MoveNext() )
					strContent = it.Current.Value;

				return strContent;
			}

			internal AVMode SAMode
			{
				get { return m_SAMode; }
			}

			internal byte Version
			{
				get { return m_nVersion; }
			}
		}

		internal sealed class VehicleConfig
		{
			private byte m_nVersion = 0;
			private int m_nUTMZoneNo = -1; //invalid
			private char m_chUTMZoneLetter = 'Z'; //invalid
			private string m_strHeadSignDefault = null;
			private byte m_nRouteNoFmt = 0;
			private string m_strNSAPrefix = "Next Stop, ";
			private string m_strConnPrefix = ". Connecting to ";
			private string m_strNSDStopping = "Stop Requested";
			private string m_strNSDStoppingConcat = ": ";
			private string m_strSAPrefixFmt = "Service Alert, Route {0}: ";

			internal VehicleConfig() {}

			internal VehicleConfig( XmlDocument doc )
			{
				m_nVersion = (byte)BaseN.S64ToInt( doc.DocumentElement.GetAttribute( "V", string.Empty ) );

				string strUTMZone = doc.DocumentElement.GetAttribute( "U", string.Empty );
				m_nUTMZoneNo = int.Parse( strUTMZone.Substring( 0, strUTMZone.Length - 1 ) );
				m_chUTMZoneLetter = strUTMZone[strUTMZone.Length - 1];

				StringReader sr = new StringReader( doc.DocumentElement.FirstChild.Value );
				string str = sr.ReadLine();
				int nRow = -1;
				while( str != null )
				{
					switch( ++nRow )
					{
						case 0:
							if( str.Length > 0 )
								m_strHeadSignDefault = str;
							break;
						case 1:
							byte.TryParse( str, out m_nRouteNoFmt );
							break;
						case 2:
							m_strNSAPrefix = str.Split( '~' )[0];
							break;
						case 3:
							m_strConnPrefix = str.Split( '~' )[0];
							break;
						case 4:
						{
							string[] stra = str.Split( '~' );
							m_strNSDStopping = stra[0];
							m_strNSDStoppingConcat = stra[1];
							break;
						}
						case 5:
							m_strSAPrefixFmt = str.Split( '~' )[0];
							break;
					}

					str = sr.ReadLine();
				}
				sr.Close();
			}

			internal string HeadSignDefault
			{
				get { return m_strHeadSignDefault; }
			}

			internal int UTMZoneNo
			{
				get { return m_nUTMZoneNo; }
			}

			internal char UTMZoneLetter
			{
				get { return m_chUTMZoneLetter; }
			}

			internal byte RouteNoFmt
			{
				get { return m_nRouteNoFmt; }
			}

			internal string NSAPrefix
			{
				get { return m_strNSAPrefix; }
			}

			internal string ConnPrefix
			{
				get { return m_strConnPrefix; }
			}

			internal string NSDStopping
			{
				get { return m_strNSDStopping; }
			}

			internal string NSDStoppingConcat
			{
				get { return m_strNSDStoppingConcat; }
			}

			internal string SAPrefixFmt
			{
				get { return m_strSAPrefixFmt; }
			}

			internal byte Version
			{
				get { return m_nVersion; }
			}
		}

		private sealed class GPSFix
		{
			public double Lat, Long, Speed, SpeedWeighted;
			public GPSFix( double nLat, double nLong, double nSpeed, double nSpeedWeighted )
			{
				Lat = nLat;
				Long = nLong;
				Speed = nSpeed;
				SpeedWeighted = nSpeedWeighted;
			}
		}

		private sealed class PairStringCollection : KeyedCollection<string, PairString>
		{
			public PairStringCollection() : base( null, 4 ) {}

			protected override string GetKeyForItem( PairString item )
			{
				return item.First;
			}
		}

		internal struct Timeout
		{
			private DateTime m_dt;
			private TimeSpan m_ts;

			public Timeout( TimeSpan ts )
			{
				m_ts = ts;
				m_dt = DateTime.Now + m_ts;
			}

			public void Reset()
			{
				m_dt = DateTime.Now + m_ts;
			}

			public bool HasExpired
			{
				get { return m_dt <= DateTime.Now; }
			}
		}

		private sealed class TripStatus
		{
			private short m_nRouteTag = 0;
			private int m_nRP = 0;
			private int? m_nTripNo = null;
			private ushort? m_nServiceStart = null;
			public bool m_bOffRoute = false, m_bStopping = false, m_bSignalPriority = false;

			public TripStatus()
			{
			}

			public TripStatus( bool bOffRoute )
			{
				m_bOffRoute = bOffRoute;
			}

			public TripStatus( short nRouteTag, int nRP, int? nTripNo = null, ushort? nServiceStart = null, bool bSignalPriority = false )
			{
				m_nRouteTag = nRouteTag;
				m_nRP = nRP;
				m_nTripNo = nTripNo;
				m_nServiceStart = nServiceStart;
				m_bSignalPriority = bSignalPriority;
			}

			public bool NoTrip
			{
				get { return m_nRouteTag == 0 && !m_bOffRoute; }
			}

			public bool OffRoute
			{
				get { return m_bOffRoute; }
			}

			public short RouteTag
			{
				get { return m_nRouteTag; }
			}

			public int RP
			{
				get { return m_nRP; }
			}

			public ushort? ServiceStart
			{
				get { return m_nServiceStart; }
			}

			public bool SignalPriority
			{
				get { return m_bSignalPriority; }
			}

			public bool Stopping
			{
				get { return m_bStopping; }
			}

			public int? TripNo
			{
				get { return m_nTripNo; }
			}
		}
	}
}
