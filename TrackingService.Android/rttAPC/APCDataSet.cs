using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;

namespace rttVehicle
{
	public sealed class APCDataSet
	{
#if( LINUX )
		public const string cCNXPath = "/var/lib/connexionz/apc/";
#else
		public const string cCNXPath = "T:\\APC\\";
#endif

#if( DEBUG )
		private const uint cMaxOpenSeconds = 600;
#else
		private const uint cMaxOpenSeconds = 21600;
#endif
		private const long cMaxLength = 10000, cHdrLength = 11; // hdr( 2 + 1 + 8 );
		private static readonly DateTime cDateOfBuild = new DateTime( 2017, 1, 1, 0, 0, 0, DateTimeKind.Utc );

		private enum DataType : byte
		{
			GPSDateTime = 0,
			IncDateTime = 1,
			//Initialize = 2,
			SensorEvent = 3,
			//PassengerEvent = 4,
			Timestamp = 5,
			TimestampLL = 6,
			TimestampLLOffset = 7,
			Initialize2 = 8,
			PassengerEvent2 = 9,
			PassengerEvent4 = 10,
			PassengerEvent6 = 11,
			PassengerEvent8 = 12
		};

		private uint m_nOpenTicks = 0, m_nAPCEventSeconds = 0, m_nMovingSeconds = 0, m_nOpenSeconds = 0, m_nTSSeconds = 0;
		private bool m_bHasFix = false, m_bThreadPoolActive = false;
		private double m_nLat = 0.0, m_nLong = 0.0;
		private LastCounts m_LastCounts = null;
		private ushort m_nComsAddress;
		private string m_strRTTServer;
		private Queue<FileInfo> m_Queue = new Queue<FileInfo>( 0x08 );
		FileInfo m_File = null;

		public static APCDataSet Create( ushort nComsAddress, string strRTTServer )
		{
			APCDataSet ds = new APCDataSet();
			ds.m_nComsAddress = nComsAddress;
			ds.m_strRTTServer = strRTTServer;

            //it is not able to create directory in Android yet
			//DirectoryInfo di = new DirectoryInfo( cCNXPath );
			//if( di.Exists )
			//{
			//	IEnumerable<FileInfo> query = di.GetFiles( "*.dat" ).OrderBy( FileInfo => FileInfo.Name );
			//	foreach( FileInfo fi in query )
			//	{
			//		if( fi.Length > cHdrLength )
			//			ds.m_Queue.Enqueue( fi );
			//		else
			//			fi.Delete();
			//	}
			//}
			//else
			//	di.Create();

			ds.m_LastCounts = LastCounts.Create( cCNXPath + "LastCounts.bin" );

			return ds;
		}

		public void NoGPS()
		{
			m_bHasFix = false;
			OnGPS( false, DateTime.MinValue );
		}

		public void NoGPS( DateTime dtGPSUtc )
		{
			m_bHasFix = false;
			OnGPS( false, dtGPSUtc );
		}

		public void UpdatePosition( double nLat, double nLong, bool bMoving, DateTime dtGPSUtc )
		{
			m_bHasFix = true;
			m_nLat = nLat;
			m_nLong = nLong;
			OnGPS( bMoving, dtGPSUtc );
		}

		public byte AddPassengerEvent( byte[] naBoardingsAlightings )
		{
			lock( m_Queue )
			{
				if( m_File == null || naBoardingsAlightings == null || naBoardingsAlightings.Length % 2 > 0 || naBoardingsAlightings.Length < 2 || naBoardingsAlightings.Length > 8 )
					return m_LastCounts.Loading;

				LastCounts.UpdateResult result = m_LastCounts.Update( naBoardingsAlightings );
				using( FileStream stream = m_File.Open( FileMode.Append, FileAccess.Write ) )
				{
					if( ( result & LastCounts.UpdateResult.AddInitialize ) == LastCounts.UpdateResult.AddInitialize )
					{
						UpdateSeconds( m_nOpenTicks, ref m_nAPCEventSeconds );
						byte nOffsetSec = GetOffsetSec( stream );
						stream.WriteByte( (byte)DataType.Initialize2 );
						stream.WriteByte( nOffsetSec );
					}

					if( ( result & LastCounts.UpdateResult.Change ) == LastCounts.UpdateResult.Change )
					{
						UpdateSeconds( m_nOpenTicks, ref m_nAPCEventSeconds );
						byte nOffsetSec = GetOffsetSec( stream );

						switch( naBoardingsAlightings.Length )
						{
							case 2:
								stream.WriteByte( (byte)DataType.PassengerEvent2 );
								break;
							case 4:
								stream.WriteByte( (byte)DataType.PassengerEvent4 );
								break;
							case 6:
								stream.WriteByte( (byte)DataType.PassengerEvent6 );
								break;
							case 8:
								stream.WriteByte( (byte)DataType.PassengerEvent8 );
								break;
						}
						stream.WriteByte( nOffsetSec );
						stream.Write( naBoardingsAlightings, 0, naBoardingsAlightings.Length );
					}
				}

				return m_LastCounts.Loading;
			}
		}

		public void AddSensorEvent( byte nSensorFlags, byte nSensorMask )
		{
			lock( m_Queue )
			{
				if( m_File == null )
					return;

				UpdateSeconds( m_nOpenTicks, ref m_nAPCEventSeconds );
				using( FileStream stream = m_File.Open( FileMode.Append, FileAccess.Write ) )
				{
					byte nOffsetSec = GetOffsetSec( stream );
					stream.WriteByte( (byte)DataType.SensorEvent );
					stream.WriteByte( nOffsetSec );
					stream.WriteByte( nSensorFlags );
					stream.WriteByte( nSensorMask );
				}
			}
		}

		public void FlushData( byte[] bufUtc )
		{
			lock( m_Queue )
			{
				if( m_File == null && bufUtc != null && bufUtc.Length == 8 )
					try
					{
						long ft = (long)BitConverter.ToUInt64( NetworkOrder( bufUtc ), 0 );
						DateTime dtUtc = DateTime.FromFileTimeUtc( ft );
						NoGPS( dtUtc );
					}
					catch( Exception ) { }

				if( m_File == null )
					return;

				using( FileStream stream = m_File.Open( FileMode.Append, FileAccess.Write ) )
					AddTimestamp( stream );

				m_Queue.Enqueue( m_File );
				m_File = null;

				ProcessQueue();
			}
		}

		//
		private void AddTimestamp( FileStream stream )
		{
			byte nOffsetSec = GetOffsetSec( stream );
			if( m_bHasFix )
			{
				stream.WriteByte( (byte)DataType.TimestampLL );
				stream.WriteByte( nOffsetSec );
				stream.Write( NetworkOrder( BitConverter.GetBytes( (int)( m_nLat * 100000.0 ) ) ), 0, 4 );
				stream.Write( NetworkOrder( BitConverter.GetBytes( (int)( m_nLong * 100000.0 ) ) ), 0, 4 );
			}
			else
			{
				stream.WriteByte( (byte)DataType.Timestamp );
				stream.WriteByte( nOffsetSec );
			}
		}

		private void OnGPS( bool bMoving, DateTime dtGPSUtc )
		{
			lock( m_Queue )
			{
				if( m_File != null )
				{
					if( bMoving )
						UpdateSeconds( m_nOpenTicks, ref m_nMovingSeconds );

					bool bWriteTS;
					if( m_nMovingSeconds >= m_nTSSeconds || m_nAPCEventSeconds >= m_nTSSeconds )
						bWriteTS = UpdateSeconds( m_nOpenTicks, ref m_nTSSeconds, 30 );
					else
						bWriteTS = UpdateSeconds( m_nOpenTicks, ref m_nTSSeconds, 600 );

					if( bWriteTS )
					{
						using( FileStream stream = m_File.Open( FileMode.Append, FileAccess.Write ) )
							AddTimestamp( stream );

						m_File.Refresh();
						if( m_File.Length > cMaxLength || m_nOpenSeconds > cMaxOpenSeconds )
						{
							m_Queue.Enqueue( m_File );
							m_File = null;
						}
					}
				}
				else if( dtGPSUtc > cDateOfBuild )
				{
					m_File = new FileInfo( cCNXPath + dtGPSUtc.ToString( "yyyyMMddHHmmss" ) + ".dat" );
					using( FileStream stream = m_File.Create() )
					{
						m_nOpenTicks = (uint)Environment.TickCount;
						m_nAPCEventSeconds = m_nMovingSeconds = m_nOpenSeconds = m_nTSSeconds = 0;
						stream.Write( NetworkOrder( BitConverter.GetBytes( m_nComsAddress ) ), 0, 2 );

						double nDT = dtGPSUtc.ToOADate();
						stream.WriteByte( (byte)DataType.GPSDateTime );
						stream.Write( NetworkOrder( BitConverter.GetBytes( (int)nDT - 2 ) ), 0, 4 );
						stream.Write( NetworkOrder( BitConverter.GetBytes( (int)( ( nDT - Math.Floor( nDT ) ) * 24.0 * 60.0 * 60.0 * 300.0 ) ) ), 0, 4 );
					}
				}

				ProcessQueue();
			}
		}

		private void ProcessQueue()
		{
			while( !m_bThreadPoolActive && m_Queue.Count > 0 )
			{
				FileInfo fi = m_Queue.Peek();
				fi.Refresh();

				if( fi.Exists && fi.Length > cHdrLength )
				{
					m_bThreadPoolActive = true;
					ThreadPool.QueueUserWorkItem( new WaitCallback( Submit ), fi );
				}
				else
					m_Queue.Dequeue();
			}
		}

		private void Submit( object parm )
		{
			FileInfo fi = (FileInfo)parm;

			try
			{
				bool bComplete = false;
				byte[] buf = null;
				using( FileStream stream = fi.OpenRead() )
				{
					buf = new BinaryReader( stream ).ReadBytes( (int)stream.Length );
				}

				HttpWebRequest request = (HttpWebRequest)WebRequest.Create( "http://" + m_strRTTServer + "/rtt/realtime/utility/vehiclemsg.aspx/apc" );
#if( DEBUG )
				request.Credentials = CredentialCache.DefaultCredentials;
#endif
				request.ContentType = "application/octet-stream";
				request.Method = "POST";
				request.KeepAlive = false;

				using( DeflateStream stream = new DeflateStream( request.GetRequestStream(), CompressionMode.Compress ) )
					stream.Write( buf, 0, buf.Length );

				using( HttpWebResponse response = (HttpWebResponse)request.GetResponse() )
				{
					bComplete = response.StatusCode == HttpStatusCode.OK;
				}

				if( bComplete )
				{
					using( FileStream stream = fi.OpenWrite() ) { };
					fi.Delete();
				}
			}
			catch( Exception e )
			{
				System.Diagnostics.Debug.WriteLine( string.Format( "{0} {1} - {2}", DateTime.Now.ToString( "HH:mm:ss.fff" ), "SubmitFile", e.Message ) );
			}

			Thread.Sleep( 5000 );
			m_bThreadPoolActive = false;
		}

		private byte GetOffsetSec( FileStream stream )
		{
			uint nOffsetSec = UpdateSeconds( m_nOpenTicks, ref m_nOpenSeconds );
			while( nOffsetSec > 65535 )
			{
				stream.WriteByte( (byte)DataType.IncDateTime );
				stream.WriteByte( 255 );
				stream.WriteByte( 255 );
				nOffsetSec -= 65535;
			}

			if( nOffsetSec > 255 )
			{
				stream.WriteByte( (byte)DataType.IncDateTime );
				stream.Write( NetworkOrder( BitConverter.GetBytes( (ushort)nOffsetSec ) ), 0, 2 );
				nOffsetSec = 0;
			}

			return (byte)nOffsetSec;
		}

		//
		internal static uint ElapsedSeconds( uint nBaseTicks )
		{
			uint nTicks = (uint)Environment.TickCount;

			uint nElapsedSec;
			if( nTicks < nBaseTicks )
				nElapsedSec = nTicks / 1000 + ( uint.MaxValue - nBaseTicks ) / 1000;
			else
				nElapsedSec = nTicks / 1000 - nBaseTicks / 1000;

			return nElapsedSec;
		}

		internal static uint UpdateSeconds( uint nBaseTicks, ref uint nSeconds )
		{
			uint nChangeSec = ElapsedSeconds( nBaseTicks ) - nSeconds;
			nSeconds += nChangeSec;

			return nChangeSec;
		}

		internal static bool UpdateSeconds( uint nBaseTicks, ref uint nSeconds, uint nMinSeconds )
		{
			bool bUpdated = false;

			uint nChangeSec = UpdateSeconds( nBaseTicks, ref nSeconds );
			if( nChangeSec < nMinSeconds )
				nSeconds -= nChangeSec;
			else
				bUpdated = true;

			return bUpdated;
		}

		internal static byte[] NetworkOrder( byte[] buf )
		{
			if( BitConverter.IsLittleEndian )
				Array.Reverse( buf );
			return buf;
		}

		public bool ZeroLoading()
		{
			return m_LastCounts.ZeroLoading();
		}

		//
		public byte Loading
		{
			get { return m_LastCounts.Loading; }
		}

		private class LastCounts
		{
			[Flags]
			public enum UpdateResult : byte
			{
				None = 0x00,
				Change = 0x01,
				AddInitialize = 0x02,
			};

			private const byte cInitThreshold = 0x04;
			private const int cCounts = 8;

			private byte m_nLoading = 0xFF;
			private byte[] m_naBA = { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };
			private FileInfo m_File = null;

			public static LastCounts Create( string strPath )
			{
				LastCounts lc = new LastCounts();
				lc.m_File = new FileInfo( strPath );
				if( lc.m_File.Exists )
					try
					{
						using( BinaryReader rdr = new BinaryReader( lc.m_File.OpenRead() ) )
						{
							uint nElapsedSec = (uint)( DateTime.UtcNow - cDateOfBuild ).TotalSeconds - rdr.ReadUInt32();
							if( nElapsedSec < 300 )
							{
								lc.m_nLoading = rdr.ReadByte();
								rdr.Read( lc.m_naBA, 0, lc.m_naBA.Length );
							}
						}
					}
					catch( Exception ) { }

				return lc;
			}

			public UpdateResult Update( byte[] naBoardingsAlightings )
			{
				byte[] naBA = { 0, 0, 0, 0, 0, 0, 0, 0 };
				for( int n = 0; n < naBoardingsAlightings.Length; ++n ) naBA[n] = naBoardingsAlightings[n];

				if( m_nLoading != 0xFF )
				{
					int n = -1;
					while( ++n < cCounts && m_naBA[n] == naBA[n] );
					if( n == cCounts )
						return UpdateResult.None;
				}

				UpdateResult result = UpdateResult.Change;
				bool bAddInitialize = false;
				{
					int n = -1;
					while( ++n < cCounts && naBA[n] < cInitThreshold ) ;
					if( n == cCounts )
					{
						n = -1;
						while( ++n < cCounts && m_naBA[n] <= naBA[n] ) ;
						if( n < cCounts )
							bAddInitialize = true;
					}
				}
				if( bAddInitialize )
				{
					result |= UpdateResult.AddInitialize;
					int nLoading = 0;
					for( int n = 0; n < cCounts; ++n )
					{
						if( n % 2 == 0 )
							nLoading += naBA[n];
						else
							nLoading -= naBA[n];
					}
					m_nLoading = nLoading > 0 ? (byte)nLoading : (byte)0x00;
				}
				else if( m_nLoading == 0xFF )
					m_nLoading = 0x00;
				else
				{
					int nLoading = m_nLoading;
					for( int n = 0; n < cCounts; ++n )
					{
						if( n % 2 == 0 )
							nLoading += Chg( naBA[n], m_naBA[n] );
						else
							nLoading -= Chg( naBA[n], m_naBA[n] );
					}

					if( nLoading < 0 )
						m_nLoading = 0x00;
					else if( nLoading > 0xFE )
						m_nLoading = 0xFE;
					else
						m_nLoading = (byte)nLoading;
				}

				for( int n = 0; n < cCounts; ++n )
					m_naBA[n] = naBA[n];

				Save();

				return result;
			}

			private byte Chg( byte n1, byte n0 )
			{
				byte n;
				if( n1 < n0 )
					n = (byte)( n1 + ( 255 - n0 ) );
				else
					n = (byte)( n1 - n0 );

				return n;
			}

			private void Save()
			{
				try
				{
					using( BinaryWriter wri = new BinaryWriter( m_File.Create() ) )
					{
						wri.Write( (uint)( DateTime.UtcNow - cDateOfBuild ).TotalSeconds );
						wri.Write( m_nLoading );
						wri.Write( m_naBA );
					}
				}
				catch( Exception ) { }
			}

			public bool ZeroLoading()
			{
				if( m_nLoading == 0xFF )
					return false;

				m_nLoading = 0;
				Save();

				return true;
			}

			public byte Loading
			{
				get { return m_nLoading; }
			}
		}
	}
}
