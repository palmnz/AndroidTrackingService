using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using rttSVOX;
using Utility;
using System.Threading;

namespace rttVehicle
{
	internal sealed class Trip
	{
		private Vehicle m_Vehicle;

		private short m_nRouteTag;
		private int m_nRP, m_nNextStopIndex;
		private int? m_nTripNo;
		private ushort? m_nServiceStart;
		private string m_strRouteNo, m_strRouteNoForAudio, m_strSAPrefix, m_strSAPrefixForAudio, m_strServiceAlert, m_strServiceAlert2;
		private bool m_bOffRoute = false;
		private double m_nRouteLength;
		private Queue<Platform> m_Queue;
		private ReadOnlyCollection<string> m_colNextStop;
		private Platform m_Platform;
		private byte? m_nFareSet = null;
		private Vehicle.AVMode m_ConnMode, m_SAMode;
		private Vehicle.VehicleConfig m_configV;

        private bool  m_fFerry;

		public static Trip Create( Vehicle vehicle, short nRouteTag, int nRP, int? nTripNo, ushort? nServiceStart,
			Vehicle.RoutePatternConfig configRP, Vehicle.ServiceAlertConfig configSA, Vehicle.VehicleConfig configV, bool fFerry )
		{

			Trip trip = null;

			try
			{
				double nRouteLength;
				string strProjectTag, strRouteNo, strRPcsv;
				if( !configRP.GetContent( BaseN.IntToS64( nRouteTag ), out strProjectTag, out strRouteNo, out nRouteLength, out strRPcsv ) )
					return null;

				StringReader sr = new StringReader( strRPcsv );

				Queue<Platform> queue = new Queue<Platform>( 0x80 );
				Collection<string> colNextStop = new Collection<string>();
				double nLastEasting = 0.0, nLastNorthing = 0.0, nLastRadius = 0.0;
				int nLastRP = 0, nNextStopIndex = 0;
				string str = sr.ReadLine(), strLastDestination = null, strLastHeadSign = null;
				byte? nLastFareSet = null;
				byte nServiceDay = 0;
				ushort nScheduledMin = 0;

				if( nServiceStart.HasValue )
				{
					nServiceDay = (byte)( nServiceStart.Value & 0x07 );
					nScheduledMin = (ushort)( nServiceStart.Value >> 3 );
				}

				while( str != null )
				{
					string[] stra = str.Split( '~' );

					Platform platform = new Platform();
					platform.MinDistanceFromPlatform = double.MaxValue;
					platform.Arrived = platform.ExternalAudioActivated = false;
					platform.Name = stra[0];
					//platform.Position.X = BaseN.S32ToInt( stra[1] ) + nLastEasting;
					//platform.Position.Y = BaseN.S32ToInt( stra[2] ) + nLastNorthing;
					platform.Radius = BaseN.S32ToInt( stra[3] ) + nLastRadius;
					platform.RPEnd = BaseN.S64ToInt( stra[4] ) + nLastRP;
					platform.DistanceToEnd = nRouteLength * ( platform.RPEnd - nLastRP ) / UTM.cRPScalar;
					if( stra.Length > 5 && stra[5].Length > 0 )
						switch( stra[5][0] )
						{
							case '1':
								platform.InternalAudio = true;
								platform.Timepoint = false;
								break;
							case '2':
								platform.InternalAudio = false;
								platform.Timepoint = true;
								break;
							case '3':
								platform.InternalAudio = platform.Timepoint = true;
								break;
							default: //legacy @
								platform.InternalAudio = true;
								platform.Timepoint = false;
								break;
						}
					else
						platform.InternalAudio = platform.Timepoint = false;
					platform.Destination = stra.Length > 6 && stra[6].Length > 0 ? stra[6] : strLastDestination;
					platform.HeadSign = stra.Length > 7 && stra[7].Length > 0 ? stra[7] : strLastHeadSign;
					platform.FareSet = stra.Length > 8 && stra[8].Length > 0 ? byte.Parse( stra[8] ) : nLastFareSet;
					platform.NSAReminderSec = platform.InternalAudio ? configRP.NSAReminderSec : (ushort)0;
					platform.NSAReminderDistanceMin = platform.InternalAudio ? ( platform.Radius * 2.0 + 50.0 ) : 0.0;
					platform.NSAReminderDistanceMax = platform.InternalAudio ? ( platform.Radius * 2.0 + 200.0 ) : 0.0;
                    platform.DepartureEnabled = false;
                    platform.IgnoreServerRP = false;

					colNextStop.Add( platform.Name );
					if( nRP <= platform.RPEnd )
					{
						if( nServiceDay > 0 && stra.Length > 9 )
						{
							string strNSAConn = string.Empty, strNSDConn = string.Empty;
							for( int n = 9; n < stra.Length; ++n )
							{
								bool bMatch = false;
								string[] straDest = stra[n].Split( '§' );
								for( int m = 2; !bMatch && m < straDest.Length; )
								{
									byte nServiceMask = (byte)BaseN.S64ToInt( straDest[m] );
									if( ( nServiceDay & nServiceMask ) > 0 )
									{
										ushort nScheduledMinStart = (ushort)BaseN.S64ToInt( straDest[m + 1] );
										ushort nScheduledMinEnd = (ushort)BaseN.S64ToInt( straDest[m + 2] );
										if( nScheduledMinStart <= nScheduledMin && nScheduledMin <= nScheduledMinEnd )
										{
											bMatch = true;
											strNSAConn += vehicle.GetRouteNoForAudio( straDest[0] ) + " " + straDest[1] + ". ";
											strNSDConn += straDest[0] + " " + straDest[1] + ", ";
										}
									}
									m += 3;
								}
							}
							if( strNSAConn.Length > 0 )
							{
								platform.NSAConn = strNSAConn;
								platform.NSDConn = strNSDConn.Substring( 0, strNSDConn.Length - 2 ) + ". ";
							}
						}
						queue.Enqueue( platform );
					}
					else
						++nNextStopIndex;

					//nLastEasting = platform.Position.X;
					//nLastNorthing = platform.Position.Y;
					nLastRadius = platform.Radius;
					nLastRP = platform.RPEnd;
					strLastDestination = platform.Destination;
					strLastHeadSign = platform.HeadSign;
					nLastFareSet = platform.FareSet;

					str = sr.ReadLine();
				}
				sr.Close();

				trip = new Trip();
				trip.m_Vehicle = vehicle;
				trip.m_nRouteTag = nRouteTag;
				trip.m_nRP = nRP;
				trip.m_nTripNo = nTripNo;
				trip.m_nServiceStart = nServiceStart;
				trip.m_strRouteNo = strRouteNo;
				trip.m_strRouteNoForAudio = vehicle.GetRouteNoForAudio( strRouteNo );
				trip.m_ConnMode = configRP.ConnMode;
				trip.m_SAMode = configSA.SAMode;
				if( trip.m_SAMode != Vehicle.AVMode.None )
				{
					trip.m_strSAPrefix = string.Format( configV.SAPrefixFmt, strRouteNo );
					trip.m_strSAPrefixForAudio = string.Format( configV.SAPrefixFmt, trip.m_strRouteNoForAudio );
					trip.m_strServiceAlert = configSA.GetContent( strProjectTag, strRouteNo );
					if( trip.m_strServiceAlert != null )
						trip.m_strServiceAlert2 = trip.m_strServiceAlert.Replace( "\r\n", " " ).Replace( '\n', ' ' );
				}
				trip.m_configV = configV;
                trip.m_fFerry = fFerry;
				trip.m_nRouteLength = nRouteLength;
				trip.m_Queue = queue;
				trip.m_colNextStop = new ReadOnlyCollection<string>( colNextStop );
				trip.m_nNextStopIndex = nNextStopIndex;
				trip.m_Platform = trip.m_Queue.Dequeue();
				trip.UpdateDistanceToEnd();

				//
				trip.OnNewPlatform( false, true );
				switch( configRP.FareboxFields )
				{
					case Vehicle.FareboxFields.RouteNo:
						vehicle.QueueRequest( new RequestEventArgs( RequestType.Farebox, new Farebox( strRouteNo, null ) ) );
						break;
					case Vehicle.FareboxFields.RouteNoTripNo:
						vehicle.QueueRequest( new RequestEventArgs( RequestType.Farebox, new Farebox( strRouteNo, nTripNo ) ) );
						break;
					case Vehicle.FareboxFields.TripNo:
						if( nTripNo.HasValue )
							vehicle.QueueRequest( new RequestEventArgs( RequestType.Farebox, new Farebox( null, nTripNo ) ) );
						break;
				}
			}
			catch( Exception ) {}

			return trip;
		}

		private void SynthInternal()
		{
			string strParm = m_configV.NSAPrefix + m_Platform.Name;
			if( ( m_ConnMode & Vehicle.AVMode.Audio ) != Vehicle.AVMode.None && m_Platform.NSAConn != null )
				strParm += m_configV.ConnPrefix + m_Platform.NSAConn;
			m_Vehicle.QueueRequest( new RequestEventArgs( RequestType.SynthInternal, strParm ) );
		}

		private void OnNewPlatform( bool bAudio, bool bResetHint = false )
		{
			//SynthInternal
			if( bAudio )
				SynthInternal();

			//DisplayNextStop
			if( m_nNextStopIndex > 0 )
			{
				if( ( m_ConnMode & Vehicle.AVMode.Display ) != Vehicle.AVMode.None && m_Platform.NSDConn != null )
					m_Vehicle.QueueRequest( new RequestEventArgs( RequestType.DisplayNextStop, new string[] { m_Platform.Name, m_configV.ConnPrefix + m_Platform.NSDConn }, RequestSubType.NextStopConn ) );
				else
					m_Vehicle.QueueRequest( new RequestEventArgs( RequestType.DisplayNextStop, m_Platform.Name, RequestSubType.NextStop ) );
			}
			else if( ( m_SAMode & Vehicle.AVMode.Display ) != Vehicle.AVMode.None && m_strServiceAlert2 != null )
				m_Vehicle.QueueRequest( new RequestEventArgs( RequestType.DisplayNextStop, m_strSAPrefix + m_strServiceAlert2, RequestSubType.ServiceAlert ) );

			//DisplayNextStopExtra
			{
				NextStopExtra nse;
				if( ( m_ConnMode & Vehicle.AVMode.MediaPlayer ) != Vehicle.AVMode.None && ( m_SAMode & Vehicle.AVMode.MediaPlayer ) != Vehicle.AVMode.None )
					nse = new NextStopExtra( bResetHint, m_nRouteTag, m_strRouteNo, m_Platform.Destination, m_nNextStopIndex, m_colNextStop, m_configV.ConnPrefix, m_Platform.NSDConn, m_strSAPrefix, m_strServiceAlert );
				else if( ( m_ConnMode & Vehicle.AVMode.MediaPlayer ) != Vehicle.AVMode.None )
					nse = new NextStopExtra( bResetHint, m_nRouteTag, m_strRouteNo, m_Platform.Destination, m_nNextStopIndex, m_colNextStop, m_configV.ConnPrefix, m_Platform.NSDConn, null, null );
				else if( ( m_SAMode & Vehicle.AVMode.MediaPlayer ) != Vehicle.AVMode.None )
					nse = new NextStopExtra( bResetHint, m_nRouteTag, m_strRouteNo, m_Platform.Destination, m_nNextStopIndex, m_colNextStop, null, null, m_strSAPrefix, m_strServiceAlert );
				else
					nse = new NextStopExtra( bResetHint, m_nRouteTag, m_strRouteNo, m_Platform.Destination, m_nNextStopIndex, m_colNextStop, null, null, null, null );

				m_Vehicle.QueueRequest( new RequestEventArgs( RequestType.DisplayNextStopExtra, nse ) );
			}

			//HeadSign
			if( m_Platform.HeadSign != null )
				m_Vehicle.QueueRequest( new RequestEventArgs( RequestType.HeadSign, m_Platform.HeadSign ) );

			//FareSet
			if( m_Platform.FareSet.HasValue && m_Platform.FareSet != m_nFareSet )
			{
				m_nFareSet = m_Platform.FareSet;
				m_Vehicle.QueueRequest( new RequestEventArgs( RequestType.FareSet, m_nFareSet.Value ) );
			}
		}

		private void UpdateDistanceToEnd()
		{
			m_Platform.DistanceToEnd = m_nRouteLength * ( m_Platform.RPEnd - m_nRP ) / UTM.cRPScalar;
		}

		public void ProgressRP( int nRP )
		{
			m_bOffRoute = false;
			m_nRP = nRP;

            if (m_Platform.IgnoreServerRP)
            {
                // Wait for a 'real' (GPS triggered) departure before allowing any server generated RP updates to push us past the platform
                //Console.WriteLine("Ignoring Server RP");
                return;
            }

            if (m_nRP > m_Platform.RPEnd)
			{
				do
				{
					++m_nNextStopIndex;
					m_Platform = m_Queue.Dequeue();
				}
				while( m_nRP > m_Platform.RPEnd );

                //Console.WriteLine("Server RP - dropping platform(s) - new platform {0}", m_nNextStopIndex);
				OnNewPlatform( false );
			}
			UpdateDistanceToEnd();

			System.Diagnostics.Debug.WriteLine( string.Format( "{0} {1} - {2} : {3}", DateTime.Now.ToString( "HH:mm:ss.fff" ), "ProgressRP", Math.Round( nRP / UTM.cRPScalar, 4 ), Math.Round( m_Platform.RPEnd / UTM.cRPScalar, 4 ) ) );
		}

		//public bool UpdatePosition( Point ptUTM, double nSpeed, double nSpeedWeighted )
		//{
		//	bool bProcessAgain;
		//	do
		//	{
		//		bProcessAgain = false;
  //              double nDistance = (ptUTM - m_Platform.Position).Length;
  //              if (m_fFerry)
  //              {
  //                  /*
  //                   * arrival radius 200m
  //                   * departure radius 120m
  //                   * departure enable radius 100m
  //                   * 
  //                   * arrival event is fired when boat comes within 200m of the platform.
  //                   * departure event is fired when boat moves outside the 120m radius, having first ventured inside the 100m radius.
  //                   */
  //                  {
  //                      System.Diagnostics.Debug.WriteLine(string.Format("{0} {1}, {2:f1}({3:f1}) : {4} : {5} [ {6} , {7} ]{8}[{9}]", DateTime.Now.ToString("HH:mm:ss.fff"), "UpdatePosition",
  //                      nSpeed, nSpeedWeighted, Math.Round(nDistance - m_Platform.Radius), Math.Round(m_Platform.DistanceToEnd),
  //                      Math.Round(m_Platform.Radius, 3), Math.Round(m_Platform.RPEnd / UTM.cRPScalar, 4), m_Platform.Name, m_Platform.NSAReminderSec));
  //                  }

  //                  if (m_Queue.Count != 0)                                 // If !(Last Stop)
  //                      m_Platform.IgnoreServerRP = (nDistance <= 250);     //      Ignore server route-positions updates when we are near the platform (i.e. prefer this logic to server-side logic)
  //                  else
  //                      m_Platform.IgnoreServerRP = false;                  //      Don't ignore server RP's for last platform

  //                  if (nDistance <= 200)   // 'Arrival' Zone
  //                  {
  //                      if (!m_Platform.Arrived)
  //                      {   // Just Arrived
  //                          m_Vehicle.QueueRequest(new RequestEventArgs(RequestType.FerryPlatformArrival, new NextStopExtra(false, m_nRouteTag, m_strRouteNo, m_Platform.Destination, m_nNextStopIndex, m_colNextStop, null, null, null, null)));

  //                          m_Platform.Arrived = true;
  //                      }
  //                  }

  //                  if (nDistance <= 100)   // 'DepartureEnable' Zone
  //                  {
  //                      if (m_Queue.Count == 0)     // Last Stop
  //                          return false;           // End the trip

  //                      m_Platform.DepartureEnabled = true;
  //                  }

  //                  //Console.WriteLine("{0} Ferry distance to platform={1} arrived={2} d-enabled={3} ignore-rp={4}", m_nNextStopIndex, nDistance, m_Platform.Arrived, m_Platform.DepartureEnabled, m_Platform.IgnoreServerRP);

  //                  if (nDistance >= 120)   // 'Departed' Zone
  //                  {
  //                      if (m_Platform.DepartureEnabled)             // Cannot depart unless we have arrived!
  //                      {   // We have just Departed

  //                          m_Vehicle.QueueRequest(new RequestEventArgs(RequestType.FerryPlatformDeparture, new NextStopExtra(false, m_nRouteTag, m_strRouteNo, m_Platform.Destination, m_nNextStopIndex, m_colNextStop, null, null, null, null)));

  //                          // Move to next platform
  //                          m_Platform = m_Queue.Dequeue();
  //                          ++m_nNextStopIndex;

  //                          OnNewPlatform(m_Platform.InternalAudio);
  //                      }
  //                  }
  //              }
  //              else
  //              {   // Bus behaviour
		//		    m_Platform.DistanceToEnd -= nSpeed;
  //                  //Console.WriteLine("{0} Bus distance to platform={1}", m_nNextStopIndex, nDistance);

		//		    {
		//			    System.Diagnostics.Debug.WriteLine( string.Format( "{0} {1}, {2:f1}({3:f1}) : {4} : {5} [ {6} , {7} ]{8}[{9}]", DateTime.Now.ToString( "HH:mm:ss.fff" ), "UpdatePosition",
		//			    nSpeed, nSpeedWeighted, Math.Round( nDistance - m_Platform.Radius ), Math.Round( m_Platform.DistanceToEnd ),
		//			    Math.Round( m_Platform.Radius, 3 ), Math.Round( m_Platform.RPEnd / UTM.cRPScalar, 4 ), m_Platform.Name, m_Platform.NSAReminderSec ) );
		//		    }

		//		    if( nDistance <= m_Platform.Radius )
		//		    {
		//			    if( m_Queue.Count == 0 ) return false;

		//			    m_Platform.Arrived = true;
		//			    if( nSpeedWeighted > 2.0 ) //Moving
		//				    m_Platform.ExternalAudioActivated = false;
		//			    else if( nSpeedWeighted < 0.75 && !m_Platform.ExternalAudioActivated ) //Stationary
		//			    {
		//				    m_Platform.ExternalAudioActivated = true;
		//				    m_Vehicle.QueueRequest( new RequestEventArgs( RequestType.SynthExternal, m_strRouteNoForAudio + " " + m_Platform.Destination ) );

		//				    //SA
		//				    if( m_Platform.Timepoint )
		//				    {
		//					    if( ( m_SAMode & Vehicle.AVMode.Audio ) != Vehicle.AVMode.None && m_strServiceAlert != null )
		//						    m_Vehicle.QueueRequest( new RequestEventArgs( RequestType.SynthInternal, m_strSAPrefixForAudio + m_strServiceAlert ) );
		//					    if( ( m_SAMode & Vehicle.AVMode.Display ) != Vehicle.AVMode.None && m_strServiceAlert2 != null )
		//						    m_Vehicle.QueueRequest( new RequestEventArgs( RequestType.DisplayNextStop, m_strSAPrefix + m_strServiceAlert2, RequestSubType.ServiceAlert ) );
		//				    }
		//			    }
		//		    }
		//		    else if( m_Platform.Arrived || ( m_Platform.DistanceToEnd < 0.0 && ( nDistance - m_Platform.MinDistanceFromPlatform ) >= 60.0 ) )
		//		    {
		//			    if( m_Queue.Count == 0 ) return false;
		//			    ++m_nNextStopIndex;
		//			    m_Platform = m_Queue.Dequeue();

		//			    bProcessAgain = true;
		//			    OnNewPlatform( m_Platform.InternalAudio );
		//		    }
		//		    else
		//		    {
		//			    m_Platform.MinDistanceFromPlatform = Math.Min( nDistance, m_Platform.MinDistanceFromPlatform );

		//			    if( m_Platform.NSAReminderSec > 1 )
		//				    --m_Platform.NSAReminderSec;
		//			    else if( m_Platform.NSAReminderSec == 1 && m_Platform.DistanceToEnd > m_Platform.NSAReminderDistanceMin && m_Platform.DistanceToEnd < m_Platform.NSAReminderDistanceMax )
		//			    {
		//				    m_Platform.NSAReminderSec = 0;
		//				    SynthInternal();
		//			    }
		//		    }
  //              }
		//	}
		//	while( bProcessAgain );

		//	return true;
		//}

		public bool OffRoute
		{
			get { return m_bOffRoute; }
			set { m_bOffRoute = value; }
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

		public int? TripNo
		{
			get { return m_nTripNo; }
		}

		private struct Platform
		{
			public string Name, Destination, HeadSign, NSAConn, NSDConn;
			public byte? FareSet;
			public bool InternalAudio, ExternalAudioActivated, Arrived, Timepoint;
			//public Point Position;
			public double Radius, DistanceToEnd, MinDistanceFromPlatform;
			public int RPEnd;
			public ushort NSAReminderSec;
			public double NSAReminderDistanceMin, NSAReminderDistanceMax;

            public bool DepartureEnabled;
            public bool IgnoreServerRP;
		}
	}
}
