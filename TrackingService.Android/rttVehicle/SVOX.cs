using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
//using System.IO.Packaging;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace rttSVOX
{
	public class InactiveEventArgs : EventArgs
	{
		protected string m_strErrorMessage = null;

		public InactiveEventArgs() { }

		public InactiveEventArgs( string strErrorMessage )
		{
			m_strErrorMessage = strErrorMessage;
		}

		public string ErrorMessage
		{
			get { return m_strErrorMessage; }
		}
	}

	public static class SVOX
	{
		private enum PilType { None, Lexicon, PreProc };

#if( LINUX )
		public const string cSVOXPath = "/usr/lib/svox/";
		internal const string cDLL = cSVOXPath + "libsvox.so";
#else
		public const string cSVOXPath = "D:\\SVOX\\svox_420\\";
		internal const string cDLL = cSVOXPath + "svoxdll.dll";
#endif

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_Initialize( string systemDataPath, ref IntPtr outSystem );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_Terminate( ref IntPtr inoutSystem );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_LoadLingware( IntPtr system, string lingwareDataPath, string lingwareFileName, ref IntPtr outLingware );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_UnloadLingware( IntPtr system, ref IntPtr inoutLingware );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_NewEngine( IntPtr system, string engineDataPath, ref IntPtr outEngine );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_CloseEngine( IntPtr system, ref IntPtr inoutEngine );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_NewChannel( IntPtr engine, string voice, ref IntPtr outChannel, ref int outSampleRate );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_CloseChannel( IntPtr engine, ref IntPtr inoutChannel );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_SynthString( IntPtr engine, IntPtr channel, string str, int strEnc );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_SynthFinish( IntPtr engine, IntPtr channel );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_GetSystemStatusMessage( IntPtr system, StringBuilder outMessage );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_Abort( IntPtr engine, IntPtr channel );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_GetNrOfVoices( IntPtr system, ref int outNrOfVoices );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_PutSynthModifInt( IntPtr engine, IntPtr channel, int nID, int nVal );

		[DllImport( cDLL, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi )]
		internal static extern int SVOX_SetOutputFile( IntPtr engine, IntPtr channel, string strFileName );

		//
		public static event EventHandler<InactiveEventArgs> Inactive;

		internal static readonly object cLockSettings = new object();
		internal static Queue<string> m_Queue = new Queue<string>( 8 );
		internal static bool m_bThreadPoolActive = false;
		internal static int m_nVolume = 100;
		internal static string m_strOutputFile = null;


		public static void Synth( string strSynth )
		{
			if( strSynth == null )
				throw new ArgumentNullException();
#if( DEBUG )
			if( strSynth.StartsWith( "OF:" ) )
			{
				string strOutputFile = strSynth.Substring( 3 ).Trim();
				if( strOutputFile.Length == 0 )
					strOutputFile = null;
				OutputFile = strOutputFile;
				return;
			}
#endif
			lock( m_Queue )
			{
				if( m_bThreadPoolActive )
					m_Queue.Enqueue( strSynth );
				else
				{
					m_bThreadPoolActive = true;
					ThreadPool.QueueUserWorkItem( new WaitCallback( RequestSynth ), strSynth );
				}
			}
		}

		public static void Abort()
		{
			lock( m_Queue )
			{
				if( m_bThreadPoolActive )
				{
					m_Queue.Clear();
					m_Queue.Enqueue( null );
				}
			}
		}

		public static void Clear()
		{
			lock( m_Queue )
			{
				m_Queue.Clear();
			}
		}

		public static string OutputFile
		{
			get { lock( cLockSettings ) { return m_strOutputFile; } }
			set { lock( cLockSettings ) { m_strOutputFile = value; } }
		}

		public static int Volume
		{
			get { lock( cLockSettings ) { return m_nVolume; } }
			set { lock( cLockSettings ) { m_nVolume = value; } }
		}

		internal static void Initialize( ref IntPtr hSystem, ref IntPtr hLingware, ref IntPtr hEngine, ref IntPtr hChannel )
		{
			if( hSystem == IntPtr.Zero && SVOX_Initialize( cSVOXPath, ref hSystem ) != 0 )
				throw new InvalidOperationException( "Could not initialize SVOX" );

			int nSampleRate = 0, nVolume;
			string strOutputFile;
			lock( cLockSettings )
			{
				nVolume = m_nVolume;
				strOutputFile = m_strOutputFile;
			}
			if( ( hLingware == IntPtr.Zero && SVOX_LoadLingware( hSystem, "", "", ref hLingware ) != 0 ) ||
				( hEngine == IntPtr.Zero && SVOX_NewEngine( hSystem, "", ref hEngine ) != 0 ) ||
				( hChannel == IntPtr.Zero && SVOX_NewChannel( hEngine, "", ref hChannel, ref nSampleRate ) != 0 ) ||
				( nVolume != 100 && SVOX_PutSynthModifInt( hEngine, hChannel, 2, nVolume ) != 0 ) ||
				( strOutputFile != null && SVOX_SetOutputFile( hEngine, hChannel, strOutputFile ) != 0 )
				)
			{
				StringBuilder sbMessage = new StringBuilder( 200 );
				SVOX_GetSystemStatusMessage( hSystem, sbMessage );

				if( hEngine != IntPtr.Zero )
					SVOX_CloseEngine( hSystem, ref hEngine );
				if( hLingware != IntPtr.Zero )
					SVOX_UnloadLingware( hSystem, ref hLingware );
				if( hSystem != IntPtr.Zero )
					SVOX_Terminate( ref hSystem );

				throw new InvalidOperationException( sbMessage.ToString() );
			}
		}

		internal static void RequestSynth( object parm )
		{
			string strSynth = (string)parm;
			try
			{
				IntPtr hSystem = IntPtr.Zero, hLingware = IntPtr.Zero, hEngine = IntPtr.Zero, hChannel = IntPtr.Zero;
				byte nIdleCount = 0xFF;
				bool? bAbort = null;
				do
				{
					if( nIdleCount == 0xFF )
					{
						if( hChannel == IntPtr.Zero )
							Initialize( ref hSystem, ref hLingware, ref hEngine, ref hChannel );

						System.Diagnostics.Debug.WriteLine( "RequestSynth: " + strSynth );
						SVOX_SynthString( hEngine, hChannel, strSynth, 0 );
					}
					else
					{
						if( nIdleCount == 0x04 )
						{
							SVOX_CloseChannel( hEngine, ref hChannel );
							EventHandler<InactiveEventArgs> handler = Inactive;
							if( handler != null )
								handler( null, new InactiveEventArgs() );
						}
						Thread.Sleep( 1000 );
					}

					lock( m_Queue )
					{
						if( m_Queue.Count > 0 )
						{
							nIdleCount = 0xFF;
							strSynth = m_Queue.Dequeue();
							if( strSynth == null )
							{
								m_Queue.Clear();
								bAbort = true;
							}
						}
						else if( ++nIdleCount > 0x80 )
							bAbort = false;

						if( bAbort.HasValue )
						{
							if( hChannel != IntPtr.Zero )
							{
								if( bAbort.Value )
									SVOX_Abort( hEngine, hChannel );
								SVOX_CloseChannel( hEngine, ref hChannel );
							}
							SVOX_CloseEngine( hSystem, ref hEngine );
							SVOX_UnloadLingware( hSystem, ref hLingware );
							SVOX_Terminate( ref hSystem );

							m_bThreadPoolActive = false;
						}
					}
				}
				while( !bAbort.HasValue );
			}
			catch( Exception e )
			{
				lock( m_Queue )
				{
					m_Queue.Clear();
					m_bThreadPoolActive = false;
				}

				EventHandler<InactiveEventArgs> handler = Inactive;
				if( handler != null )
					handler( null, new InactiveEventArgs( "Error: " + e.Message ) );
			}
		}

		//public static void UnpackPil( Stream stream, string strUnpackPath )
		//{
		//	using( Package pkg = Package.Open( stream, FileMode.Open, FileAccess.Read ) )
		//		UnpackPil( pkg, strUnpackPath );
		//}

		//public static void UnpackPil( Package pkg, string strUnpackPath )
		//{
		//	foreach( PackagePart part in pkg.GetParts() )
		//	{
		//		string strName = part.Uri.OriginalString.Substring( 1 );
		//		string[] straNameParts = strName.Split( '.' );
		//		string strExt = straNameParts[straNameParts.Length - 1];
		//		if( strExt == "txt" )
		//		{
		//			string[] straNameParts2 = strName.Split( '-' );
		//			PilType type = PilType.None;
		//			if( straNameParts2[0] == "lxen" )
		//				type = PilType.Lexicon;
		//			else if( straNameParts2[0] == "ppen" )
		//				type = PilType.PreProc;

		//			if( type != PilType.None )
		//			{
		//				byte[] buffer = new byte[part.GetStream().Length];
		//				part.GetStream().Read( buffer, 0, buffer.Length );
		//				using( FileStream stream = File.Open( strUnpackPath + strName, FileMode.Create, FileAccess.Write ) )
		//					stream.Write( buffer, 0, buffer.Length );

		//				ProcessStartInfo info = new ProcessStartInfo();
		//				info.CreateNoWindow = true;
		//				info.ErrorDialog = false;
		//				info.FileName = SVOX.cSVOXPath + "svoxuserpil";
		//				info.UseShellExecute = false;
		//				info.WorkingDirectory = SVOX.cSVOXPath;
		//				switch( type )
		//				{
		//					case PilType.Lexicon:
		//						info.Arguments = string.Format( "-f svox.pil -l {0}{1}.txt {1}.pil", strUnpackPath, straNameParts[0] );
		//						break;
		//					case PilType.PreProc:
		//						info.Arguments = string.Format( "-f svox.pil -p {0} {1}{2}.txt {2}.pil", straNameParts2[1].Substring( 2 ), strUnpackPath, straNameParts[0] );
		//						break;
		//				}
		//				ThreadPool.QueueUserWorkItem( ( o ) =>
		//				{
		//					Process process = Process.Start( (ProcessStartInfo)o );
		//					process.WaitForExit( 30000 );
		//					process.Close();
		//				}, info
		//				);
		//			}
		//		}
		//	}
		//}
	}
}
