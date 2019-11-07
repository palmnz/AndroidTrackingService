using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;

using Tracking.Services;

namespace BlockLib
{
	/// <summary>
	/// Download resources with the ability to resume failed atempts.
	/// </summary>
	public class ResumableResourceDownload
	{
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif
        private string mEtag;

		private uint mLastByteRead = 0;
		/// <summary>
		/// Gets the number of bytes aquired so far. 
		/// </summary>
		public uint ReadCount { get { return mLastByteRead; } }

		private byte[] mData = null;
		/// <summary>
		/// Gets the resource as a binary array or null if the resource is incomplete.
		/// </summary>
		public byte[] Data
		{
			get 
			{
				if (mData == null)
					return null;

				return (mLastByteRead == mData.LongLength) ? mData : null;
			}
		}
		private bool mResumable = false;
		/// <summary>
		/// Indicates whether the resource is being served from a resumable source.
		/// </summary>
		public bool IsResumable { get { return mResumable; } }

		private string mUrl;
		/// <summary>
		/// The URL of the resource
		/// </summary>
		public string Url { get { return mUrl; } }

		private Uri mUri;
		/// <summary>
		/// The URL of the resource
		/// </summary>
		public Uri Uri { get { return mUri; } }

		private string mAgent = null;
		/// <summary>
		/// The URL of the resource
		/// </summary>
		public string UserAgent { get { return mAgent; } set { mAgent = value; } }

		/// <summary>
		/// Gets the size of the resource or 0 if the aquisision has no been started.
		/// </summary>
		public uint ContentSize
		{
			get
			{
				if (mData == null)
					return 0;

				return (uint)mData.LongLength;
			}
		}

		public ResumableResourceDownload(string url)
		{
			mUrl = Uri.UnescapeDataString(url);
			mUri = new Uri(Uri.EscapeUriString(mUrl));
			CNXLog.InfoFormat("URI created {0}, scheme {1} from {2}", mUri, mUri.Scheme, mUrl);
			ClearResource();
		}

		/// <summary>
		/// Clears any existing resource and state information ready for re-aquision.
		/// </summary>
		public void ClearResource()
		{
			mData = null;
			mEtag = null;
			mResumable = false;
			mLastByteRead = 0;
		}

		/// <summary>
		/// Begins a new download or resumes a failed one.
		/// </summary>
		/// <returns>The resource as a binary array or null if the resource is incomplete.</returns>
		/// <remarks></remarks>
		public byte[] AquireResource()
		{
			byte[] data = Data;

			if (data == null)
			{
				if (mResumable)
					ResumeAttempt();
				else
					FirstAttempt();
				data = Data;
			}

			return data;
		}

		private static bool SetAgent(HttpWebRequest req, string agent)
		{
			bool agentSet = false;

			if (agent != null)
			{
				try
				{
					req.UserAgent = agent;
					agentSet = true;
				}
				catch (Exception)
				{}
				if (!agentSet)
				{
					try
					{
						req.Headers[HttpRequestHeader.UserAgent] = agent;
						agentSet = true;
					}
					catch (Exception)
					{}
				}
				if (!agentSet)
				{
					try
					{
						int i;
						for (i = 0; i < req.Headers.Count; ++i)
						{
							string key = req.Headers.AllKeys[i];
							if (key.Equals("User-Agent"))
								break;
						}
						if (i > req.Headers.Count)
						{
							req.Headers.Add(HttpRequestHeader.UserAgent, agent);
							agentSet = true;
						}
					}
					catch (Exception)
					{}
				}
			}

			return agentSet;
		}

		private void FirstAttempt()
		{
			try
			{
                WebRequest webReq = WebRequest.Create(mUri);
				CNXLog.InfoFormat("Resource Download FirstAttempt created WebRequest.");
                HttpWebRequest req = (HttpWebRequest)webReq;
				CNXLog.InfoFormat("Resource Download FirstAttempt made HttpWebRequest.");
				if (mAgent != null)
				{
					if (!SetAgent(req, mAgent))
						CNXLog.ErrorFormat("FirstAttempt Changing user-agent from {0} to {1}", req.UserAgent, mAgent);
				}

				using (WebResponse resp = req.GetResponse())
				{
					// allocate a block of memory for the data.
					uint length = (uint)resp.ContentLength;
					mData = new byte[length];
					// get the headers associatted with resuming
					try
					{
						string acceptRanges = resp.Headers[HttpResponseHeader.AcceptRanges];
						if (acceptRanges != null)
						{
							if (acceptRanges.Equals("bytes"))
							{
								mEtag = resp.Headers[HttpResponseHeader.ETag];
								mResumable = true;
							}
						}
						CNXLog.InfoFormat("Resource Download {0} {1} accept ranges {2}.", mUri, (mResumable ? "resumable" : "not resumable"), acceptRanges);
					}
					catch (Exception e)
					{
						// failures mean that the resource isnt comming from a resumable source
						mResumable = false;
						CNXLog.WarnFormat("Resource Download {0} not resumable", mUri, e.ToString());
					}

					using (BufferedStream buffStream = new BufferedStream(resp.GetResponseStream(), 1024))
					{
						try
						{
							ReadResponceStream(buffStream);
						}
						catch (Exception e)
						{
							// not too bad, we should live to resume another day.
							CNXLog.WarnFormat("ResumableResourceDownload.FirstAttempt came up short. {0} {1}", mUri, e.ToString());
						}
					}
				}
			}
			catch (Exception e)
			{
				CNXLog.ErrorFormat("ResumableResourceDownload.FirstAttempt un-recoverable {0} {1}", mUri, e.ToString());
				ClearResource();
			}
		}

		private bool ReadResponceStream(Stream stream)
		{
			const int readSize = 512;

			int read = (mData.Length > readSize) ? readSize : mData.Length;
			for (read = stream.Read(mData, (int)mLastByteRead, read); read > 0; read = stream.Read(mData, (int)mLastByteRead, read))
			{
				mLastByteRead += (uint)read;
				read = (int)(mData.LongLength - mLastByteRead);
				if (read > readSize)
					read = readSize;
				// fake disconnect for testing
				//if ((mLastByteRead % 4096) == 0)
				//    throw new Exception("Fake exception download interupter");
			}

			return (mLastByteRead == mData.LongLength);
		}

		private void ResumeAttempt()
		{
			try
			{
				HttpWebRequest req = (HttpWebRequest)WebRequest.Create(mUri);
				if (mAgent != null)
				{
					if (!SetAgent(req, mAgent))
						CNXLog.ErrorFormat("ResumeAttempt Changing user-agent from {0} to {1}", req.UserAgent, mAgent);
				}
                // set up headers for resume
				req.AddRange((int)mLastByteRead);
				req.Headers.Add(HttpRequestHeader.IfMatch, mEtag);
				using (WebResponse resp = req.GetResponse())
				{
					// check for an error
					HttpWebResponse httpResp = (HttpWebResponse)resp;
					CNXLog.InfoFormat("Resumed Download {0} status {1}.", mUri, httpResp.StatusCode);
					if (httpResp.StatusCode == HttpStatusCode.OK || httpResp.StatusCode == HttpStatusCode.PartialContent)
					{

						using (BufferedStream buffStream = new BufferedStream(resp.GetResponseStream(), 1024))
						{
							try
							{
								ReadResponceStream(buffStream);
							}
							catch (Exception e)
							{
								CNXLog.WarnFormat("ResumableResourceDownload.ResumeAttempt came up short {0} {1}", mUri, e.Message);
							}
						}
					}
					else
						// resume didnt work.
						ClearResource();
				}
			}
			catch (Exception e)
			{
				CNXLog.ErrorFormat("ResumableResourceDownload.ResumeAttempt un-recoverable {0} {1}", mUri, e.ToString());
				ClearResource();
			}
		}
	}

	/*
2011-02-02 22:49:16,770 ERROR - ResumableResourceDownload.FirstAttempt un-recoverable http://192.168.30.10/rtt/realtime/resource/RoutePatternForVehicle.zip System.Net.WebException: The request timed out
  at System.Net.HttpWebRequest.EndGetResponse (IAsyncResult asyncResult) [0x00000]
  at System.Net.HttpWebRequest.GetResponse () [0x00000]
  at BlockLib.ResumableResourceDownload.FirstAttempt () [0x00000]
	 * 
	 * sed -i 's:<value>192.168.30.10</value>:<value>12.233.207.165</value>:g' /home/tracker/CellularMobile.exe.config
	 */
}
