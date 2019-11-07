using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.IO;

using PairInt = System.Collections.ObjectModel.Pair<int, int>;

namespace Utility
{
    /// <summary>
    /// Summary description for UTM.
    /// </summary>
    public partial class UTM
	{
		public const double cDeg2Rad = Math.PI / 180.0, cRadius = 6378137.0, cECCSquared = 0.00669438, cRPScalar = 8191.0;

		static protected char UTMLetterDesignator( double nLat )
		{
		//This routine determines the correct UTM letter designator for the given latitude
		//returns 'Z' if latitude is outside the UTM limits of 84N to 80S
			//Written by Chuck Gantz- chuck.gantz@globalstar.com
			char LetterDesignator;

			if(		(84.0 >= nLat) && (nLat >= 72.0))	LetterDesignator = 'X';
			else if((72.0 > nLat) && (nLat >= 64.0))	LetterDesignator = 'W';
			else if((64.0 > nLat) && (nLat >= 56.0))	LetterDesignator = 'V';
			else if((56.0 > nLat) && (nLat >= 48.0))	LetterDesignator = 'U';
			else if((48.0 > nLat) && (nLat >= 40.0))	LetterDesignator = 'T';
			else if((40.0 > nLat) && (nLat >= 32.0))	LetterDesignator = 'S';
			else if((32.0 > nLat) && (nLat >= 24.0))	LetterDesignator = 'R';
			else if((24.0 > nLat) && (nLat >= 16.0))	LetterDesignator = 'Q';
			else if((16.0 > nLat) && (nLat >= 8.0))		LetterDesignator = 'P';
			else if(( 8.0 > nLat) && (nLat >= 0.0))		LetterDesignator = 'N';
			else if(( 0.0 > nLat) && (nLat >= -8.0))	LetterDesignator = 'M';
			else if((-8.0> nLat) && (nLat >= -16.0))	LetterDesignator = 'L';
			else if((-16.0 > nLat) && (nLat >= -24.0))	LetterDesignator = 'K';
			else if((-24.0 > nLat) && (nLat >= -32.0))	LetterDesignator = 'J';
			else if((-32.0 > nLat) && (nLat >= -40.0))	LetterDesignator = 'H';
			else if((-40.0 > nLat) && (nLat >= -48.0))	LetterDesignator = 'G';
			else if((-48.0 > nLat) && (nLat >= -56.0))	LetterDesignator = 'F';
			else if((-56.0 > nLat) && (nLat >= -64.0))	LetterDesignator = 'E';
			else if((-64.0 > nLat) && (nLat >= -72.0))	LetterDesignator = 'D';
			else if((-72.0 > nLat) && (nLat >= -80.0))	LetterDesignator = 'C';
			else LetterDesignator = 'Z'; //This is here as an error flag to show that the Latitude is outside the UTM limits

			return LetterDesignator;
		}

		static public bool IsValidLL4Zone( int nZoneNo, char chZoneLetter, double nLat, double nLong )
		{
			//Make sure the latitude is between -90.0 .. 90.0 and longitude is between -180.0 .. 179.9
			if( nLat < -90.0 || nLat > 90.0 || nLong < -180.0 || nLong >= 180.0 )
				return false;

			int nChkZoneNo = (int)((nLong + 180.0)/6.0) + 1;

			// Special zones
			if( nLat >= 72.0 && nLat < 84.0 )
			{
				if(      nLong >= 0.0  && nLong <  9.0 ) nChkZoneNo = 31;
				else if( nLong >= 9.0  && nLong < 21.0 ) nChkZoneNo = 33;
				else if( nLong >= 21.0 && nLong < 33.0 ) nChkZoneNo = 35;
				else if( nLong >= 33.0 && nLong < 42.0 ) nChkZoneNo = 37;
			}
			else if( nLat >= 56.0 && nLat < 64.0 && nLong >= 3.0 && nLong < 12.0 )
				nChkZoneNo = 32;

			//char chkZoneLetter = strUTMZone[strUTMZone.Length - 1];
			//int chkZoneNumber = int.Parse( strUTMZone.Substring( 0, strUTMZone.Length - 1 ) );

			return Math.Abs( nZoneNo - nChkZoneNo ) <= 1 && Math.Abs( chZoneLetter - UTMLetterDesignator( nLat ) ) <= 1;
		}

		//static public Point LL2UTM( int nZoneNo, double nLat, double nLong )
		//{
		//	Point pt = new Point();
		//	//converts lat/long to UTM coords.  Equations from USGS Bulletin 1532 
		//	//East Longitudes are positive, West longitudes are negative. 
		//	//North latitudes are positive, South latitudes are negative
		//	//nLat and nLong are in decimal degrees
		//	//Written by Chuck Gantz- chuck.gantz@globalstar.com

		//	double k0 = 0.9996;

		//	double LongOrigin;
		//	double eccPrimeSquared = (cECCSquared)/(1.0-cECCSquared);
		//	double N, T, C, A, M;

		//	double LatRad = nLat * cDeg2Rad;
		//	double LongRad = nLong * cDeg2Rad;
		//	double LongOriginRad;

		//	LongOrigin = (nZoneNo - 1)*6.0 - 180.0 + 3.0;  //+3 puts origin in middle of zone
		//	LongOriginRad = LongOrigin * cDeg2Rad;

		//	N = cRadius / Math.Sqrt( 1.0 - cECCSquared * Math.Sin( LatRad ) * Math.Sin( LatRad ) );
		//	T = Math.Tan( LatRad ) * Math.Tan( LatRad );
		//	C = eccPrimeSquared * Math.Cos( LatRad ) * Math.Cos( LatRad );
		//	A = Math.Cos( LatRad ) * ( LongRad - LongOriginRad );

		//	M = cRadius*((1.0	- cECCSquared/4.0	- 3.0*cECCSquared*cECCSquared/64.0	- 5.0*cECCSquared*cECCSquared*cECCSquared/256.0)*LatRad
		//		- ( 3.0 * cECCSquared / 8.0 + 3.0 * cECCSquared * cECCSquared / 32.0 + 45.0 * cECCSquared * cECCSquared * cECCSquared / 1024.0 ) * Math.Sin( 2.0 * LatRad )
		//		+ ( 15.0 * cECCSquared * cECCSquared / 256.0 + 45.0 * cECCSquared * cECCSquared * cECCSquared / 1024.0 ) * Math.Sin( 4.0 * LatRad )
		//		- ( 35.0 * cECCSquared * cECCSquared * cECCSquared / 3072.0 ) * Math.Sin( 6.0 * LatRad ) );
			
		//	pt.X = (double)(k0*N*(A+(1.0-T+C)*A*A*A/6.0
		//		+ (5.0-18.0*T+T*T+72.0*C-58.0*eccPrimeSquared)*A*A*A*A*A/120.0)
		//		+ 500000.0);

		//	pt.Y = (double)(k0*(M+N*Math.Tan(LatRad)*(A*A/2.0+(5.0-T+9.0*C+4.0*C*C)*A*A*A*A/24
		//		+ (61.0-58.0*T+T*T+600.0*C-330.0*eccPrimeSquared)*A*A*A*A*A*A/720.0)));

		//	if(nLat < 0.0)
		//		pt.Y += 10000000.0; //10000000 meter offset for southern hemisphere

		//	return pt;
		//}

#if( REALTIME || PUBLIC )
		public static System.Drawing.Point getCoordinate( int nZoneNo, double nLat, double nLong )
		{
			Point pt = LL2UTM( nZoneNo, nLat, nLong );
			return new System.Drawing.Point( (int)pt.X, (int)pt.Y * -1 );
		}

		public static System.Drawing.Point getCoordinate( int nZoneNo, string strPoint )
		{
			string[] straPosition = strPoint.Split( ' ' );
			Point pt = LL2UTM( nZoneNo, double.Parse( straPosition[1] ), double.Parse( straPosition[0] ) );
			return new System.Drawing.Point( (int)pt.X, (int)pt.Y * -1 );
		}

		public static System.Drawing.Point getCoordinate( int nZoneNo, double nLat, double nLong, double nNudgeBearing, double nNudgeMeters )
		{
			Point pt = LL2UTM( nZoneNo, nLat, nLong );
			if( nNudgeMeters != 0.0 )
			{
				double nRadians = Math.PI * nNudgeBearing / 180.0;
				pt.X  -= nNudgeMeters * Math.Sin( nRadians );
				pt.Y -= nNudgeMeters * Math.Cos( nRadians );

			}
			return new System.Drawing.Point( (int)pt.X, (int)pt.Y * -1 );
		}
#endif
	}

#if( REALTIME || PUBLIC )
	public partial class UTM
	{
		public static Point GetCenterPoint( Collection<Point> colPoint, ref double nRadius )
		{
			if( colPoint.Count == 2 )
			{
				Vector v = ( colPoint[1] - colPoint[0] ) / 2.0;
				nRadius = Math.Abs( v.Length );

				return colPoint[0] + v;
			}

			PairInt pairIndex = new PairInt();
			double nMaxLength2 = 0.0;
			for( int n = 0; n < colPoint.Count; ++n )
			{
				for( int m = n + 1; m < colPoint.Count; ++m )
				{
					Vector v = colPoint[n] - colPoint[m];
					if( nMaxLength2 < v.LengthSquared )
					{
						nMaxLength2 = v.LengthSquared;
						pairIndex.First = n;
						pairIndex.Second = m;
					}
				}
			}
			Point ptCenter = colPoint[pairIndex.First] + ( colPoint[pairIndex.Second] - colPoint[pairIndex.First] ) / 2.0;

			foreach( Point pt in colPoint )
			{
				Vector v = ptCenter - pt;
				if( nRadius < v.Length )
				{
					nRadius = ( nRadius + v.Length ) / 2.0;
					ptCenter = (Point)( ( (Vector)ptCenter * nRadius + (Vector)pt * ( v.Length - nRadius ) ) / v.Length );
				}
			}

			return ptCenter;
		}

		public static Point GetEastingNorthing( int nZoneNo, string strPoint )
		{
			string[] straPosition = strPoint.Split( ' ' );
			return LL2UTM( nZoneNo, double.Parse( straPosition[1] ), double.Parse( straPosition[0] ) );
		}

		public static string[] parsePolyLine( string strMif )
		{
			string[] stra;

			StringReader srMif = new StringReader( strMif );
			string strLine;
			do { strLine = srMif.ReadLine(); }
			while( strLine != null && ( strLine.Length < 5 || strLine.Substring( 0, 5 ) != "Pline" ) );

			if( strLine == null )
				stra = new string[0];
			{
				int nItems = int.Parse( strLine.Substring( 5 ) );
				stra = new string[nItems];
				for( int nItem = 0; nItem < nItems; ++nItem )
					stra[nItem] = srMif.ReadLine();
			}

			srMif.Close();

			return stra;
		}

		public static string Mif2Path( int nZoneNo, string strMif )
		{
			string strPath = string.Empty;
			System.Drawing.Point ptBase = new System.Drawing.Point(), ptLast = new System.Drawing.Point(), ptChange = new System.Drawing.Point();
			int nLastInsert = 0;

			foreach( string str in parsePolyLine( strMif ) )
			{
				System.Drawing.Point pt = getCoordinate( nZoneNo, str );

				if( ptBase.IsEmpty )
					ptBase = pt;
				else
				{
					if( !ptLast.IsEmpty )
					{
						if( 179.0 > Math.Abs( -180.0 + Math.Abs( Math.Atan2( pt.Y - ptLast.Y, pt.X - ptLast.X ) - Math.Atan2( ptLast.Y - ptBase.Y, ptLast.X - ptBase.X ) ) / cDeg2Rad ) )
							ptBase = ptLast;
						else
							strPath = strPath.Substring( 0, nLastInsert );
					}

					ptChange = pt - (System.Drawing.Size)ptBase;
					ptLast = pt;
				}

				nLastInsert = strPath.Length;

				if( nLastInsert == 0 )
				{
					strPath = "M" + pt.X;
					if( pt.Y >= 0 )
						strPath += " ";
					strPath += pt.Y + "l";
				}
				else
				{
					if( ptChange.X >= 0 )
						strPath += " ";
					strPath += ptChange.X;

					if( ptChange.Y >= 0 )
						strPath += " ";
					strPath += ptChange.Y;
				}
			}

			return strPath;
		}	
	}

	public class MidMifRegions
	{
		public List<RegionX> m_listRegion = new List<RegionX>();
		protected int m_nUTMZoneNo;

		public MidMifRegions( int nUTMZoneNo )
		{
			m_nUTMZoneNo = nUTMZoneNo;
		}

		public int parse( string strMid, string strMif )
		{
			m_listRegion.Clear();

			int nRegions = 0;

			RegionX region = null;
			StringReader srMid = new StringReader( strMid );
			string strLine = srMid.ReadLine();
			while( strLine != null )
			{
				++nRegions;
				region = new RegionX();
				strLine = strLine.Replace( "\"", string.Empty );
				region.m_straAttributes = strLine.Split( ',' );
				m_listRegion.Add( region );

				strLine = srMid.ReadLine();
			}
			srMid.Close();

			int nItem = -1, nItems = 0, nRegion = -1;
			System.IO.StringReader srMif = new StringReader( strMif );
			strLine = srMif.ReadLine();
			while( strLine != null )
			{
				if( nItems > 0 )
				{
					if( nItem == -1 )
					{
						++nRegion;
						region = m_listRegion[nRegion];
						region.m_listPoint = new List<System.Drawing.Point>( nItems );
					}

					if( ++nItem < nItems )
						region.m_listPoint.Add( UTM.getCoordinate( m_nUTMZoneNo, strLine ) );
					else
					{
						nItem = -1;
						nItems = 0;
					}
				}
				else if( strLine.Length >= 6 && strLine.Substring( 0, 6 ) == "Region" )
					nItems = int.Parse( srMif.ReadLine() );

				strLine = srMif.ReadLine();
			}
			srMif.Close();

			return nRegions;
		}

		public string[] getRegionAttributes( int nRegion )
		{

			return m_listRegion[nRegion].m_straAttributes;
		}

		public int getRegionPoints( int nRegion )
		{

			return m_listRegion[nRegion].Points;
		}

		public int getRegionEasting( int nRegion, int nPoint )
		{
			return m_listRegion[nRegion].m_listPoint[nPoint].X;
		}

		public int getRegionNorthing( int nRegion, int nPoint )
		{
			return m_listRegion[nRegion].m_listPoint[nPoint].Y;
		}

		public class RegionX
		{
			public string[] m_straAttributes = null;
			public List<System.Drawing.Point> m_listPoint = null;

			public int Points
			{
				get { return m_listPoint == null ? 0 : m_listPoint.Count; }
			}
		}
	}
#endif

#if( REALTIME || VEHICLE )
	public static class BaseN
	{
		internal static readonly char[] cBase64 = new char[]
		{
			'*','+','0','1','2','3','4','5','6','7','8','9',
			'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
			'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z'
		};

		public static string IntToS64( int n )
		{
			string str = string.Empty;
			if( n != 0 )
			{
				int u = n > 0 ? n : n * -1;
				while( u > 0 )
				{
					int w = 0x3f & u;
					u = u >> 6;
					str += cBase64[w];
				}
				if( n < 0 ) str += '-';

				//System.Diagnostics.Debug.Assert( n == S64ToInt( str ) );
			}

			return str;
		}

		public static int S64ToInt( string str )
		{
			int n = 0;
			int nChar = -1;
			foreach( char ch in str )
			{
				if( ch == '-' )
					n *= -1;
				else
					n += Array.BinarySearch( cBase64, ch ) << ( 6 * ++nChar );
			}

			return n;
		}

		public static string IntToS32( int n )
		{
			string str = string.Empty;
			if( n != 0 )
			{
				int u = n > 0 ? n : n * -1;
				int offset = n > 0 ? 0x20 : 0;
				while( u > 0 )
				{
					int w = 0x1f & u;
					u = u >> 5;
					str += cBase64[w + offset];
				}

				//System.Diagnostics.Debug.Assert( n == S32ToInt( str ) );
			}

			return str;
		}

		public static int S32ToInt( string str )
		{
			int n = 0;
			int nChar = -1;
			foreach( char ch in str )
			{
				int w = Array.BinarySearch( cBase64, ch );
				if( w > 0x1F )
					n += ( w - 0x20 ) << ( 5 * ++nChar );
				else
					n -= w << ( 5 * ++nChar );
			}

			return n;
		}
	}
#endif
}
