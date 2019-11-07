using System;
using System.Diagnostics;
using System.Threading;
using System.IO;

using BlockLib;
using CANLib;

namespace Tracking.Services
{
	/// <summary>
	/// Performs upgrades on the running unit.
	/// </summary>
	public class BootstrapUpgrader
	{
#if ANDROID
        public static log4droid CNXLog = new log4droid() { Logger = "CNXLogger" };
#else
        public static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif

        public enum UpgradeState
		{
			/// <summary>
			/// No upgrade has been started.
			/// </summary>
			Idle,
			/// <summary>
			/// Getting the upgrade reasources.
			/// </summary>
			AquiringReasource,
			/// <summary>
			/// Problem getting the reasources.
			/// </summary>
			UnableToAquireResource,
			/// <summary>
			/// In the process of doing the upgrade.
			/// </summary>
			UpgradeInProgress,
			/// <summary>
			/// The upgrade failed.
			/// </summary>
			UpgradeFailed,
			/// <summary>
			/// The last upgrade completed.
			/// </summary>
			UpgradeComplete,
		};

		private volatile UpgradeState mStatus = UpgradeState.Idle;
		/// <summary>
		/// The progress state of the upgrade.
		/// </summary>
		public UpgradeState Status { get { return mStatus; } }

		private string mFirmwarePath;
		/// <summary>
		/// Gets/Sets the URL to the upgrade reasource.
		/// </summary>
		/// <remarks>
		/// The property should be a fully qualified URL including the protocol prefix.
		/// For local files the path should be of the form <code>file:///local/file/path.ext</code>.
		/// Note the tripple '/'
		/// </remarks>
		public string FirmwarePath
		{
			get { return mFirmwarePath; }
			set { mFirmwarePath = value; }
		}

		private int mRetryTime = 120;
		/// <summary>
		/// The number of seconds to wait before retrying a failed upgrade step.
		/// </summary>
		/// <remarks>
		/// The default is 120 seconds between retries.
		/// A value of zero will prevent failed steps from being retried.
		/// </remarks>
		public int RetryTime
		{
			get { return mRetryTime; }
			set { mRetryTime = value; }
		}

		private Thread mUpgradeThread;

		public BootstrapUpgrader(string firmwarePath)
		{
			mFirmwarePath = firmwarePath;
		}
		
		/// <summary>
		/// Starts the upgrade process.
		/// </summary>
		/// <remarks>Any upgrade that is in progress will be restarted.</remarks>
		public void BeginUpgrade()
		{
			CNXLog.InfoFormat("Starting Upgrade.");
			if (mUpgradeThread != null)
				mUpgradeThread.Abort();

			mStatus = UpgradeState.AquiringReasource;
			mUpgradeThread = new Thread(UpgradeMethod);
			mUpgradeThread.IsBackground = true;
			mUpgradeThread.Start();
		}

		private void UpgradeMethod()
		{
			try
			{
				/*
				// get upgrade file
				byte[] upgradeBlock = BlockTransferManager.GetResourceFromUri(mFirmwarePath);
				if (upgradeBlock == null)
				{
					mStatus = UpgradeState.UnableToAquireResource;
					CNXLog.WarnFormat("Self upgrade failed - {0}", mStatus);
				}
				*/

				ResumableResourceDownload downloader = new ResumableResourceDownload(mFirmwarePath);
				byte[] upgradeBlock = null;
				while (upgradeBlock == null)
				{
					upgradeBlock = downloader.AquireResource();
					if (upgradeBlock == null)
						Thread.Sleep(30000);
				}

				// now save the block
				string path = string.Format("{0}blk{1}", ((Environment.OSVersion.Platform == PlatformID.Unix) ? BaseBlockReciever.LinuxBlockFilePath : BaseBlockReciever.MSBlockFilePath), TrackingService.ProductCode);

				using (FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 8192, FileOptions.RandomAccess))
				{
					fs.Write(upgradeBlock, 0, upgradeBlock.Length);
					fs.Flush();
					fs.Close();
				}

				// Go for the upgrade
				mStatus = UpgradeState.UpgradeInProgress;
				UpdateSystem(path);
			}
			catch (Exception e)
			{
				mStatus = UpgradeState.UpgradeFailed;
				CNXLog.WarnFormat("Self upgrade failed - {0}", e.ToString());
			}
			mStatus = UpgradeState.UpgradeComplete;
		}

		private static void UpdateSystem(string path)
		{
			// top idea would be to extract a tar ball with a package arch directory.
			// optionally there would be an update script file that would do the update.
			// without the update file the update can be left to Do an update and upgrade.
			try
			{
				// un-tar the block
				CNXLog.WarnFormat("Pre-upgrade {0}", path);
				Process process = new Process();
				process.StartInfo.FileName = "tar";
				process.StartInfo.Arguments = "-xvjf " + path + " -C /";
				process.Start();
				//process.BeginOutputReadLine();
				//string error = process.StandardError.ReadToEnd();
				process.WaitForExit();
				Thread.Sleep(3000);
				process.StartInfo.FileName = "sync";
				process.StartInfo.Arguments = null;
				process.Start();
				process.WaitForExit();
				CNXLog.WarnFormat("Post-upgrade {0}", path);
                // try to run the update script
                string updateScriptPath = "";// Properties.Settings.Default.UpdateScriptFileName;
				if (File.Exists(updateScriptPath))
				{
					try
					{
						CNXLog.WarnFormat("Executing {0}", updateScriptPath);
						process.StartInfo.FileName = "sh";
						process.StartInfo.Arguments = string.Format("-c \"{0}\"", updateScriptPath);
						process.Start();
						process.WaitForExit();
						CNXLog.WarnFormat("Upgrade script {0} complete exit code {1}.", updateScriptPath, process.ExitCode);
					}
					catch (Exception e)
					{
						CNXLog.Error(string.Format("Failed - UpdateSystem {0}, script file {1}", path, updateScriptPath), e);
					}
				}
				else
				{
					try
					{
						CNXLog.WarnFormat("No update script file {0} Updating packages instead", updateScriptPath);
						// do a package update to read the new packages
						process.StartInfo.FileName = "opkg";
						process.StartInfo.Arguments = "update";
						process.Start();
						process.WaitForExit();
						// do any upgrades
						process.StartInfo.Arguments = "--force-depends upgrade";
						process.Start();
						process.WaitForExit();
						CNXLog.WarnFormat("Package update complete exit code {0}.", process.ExitCode);
					}
					catch (Exception e)
					{
						CNXLog.Error(string.Format("Failed - UpdateSystem {0}, package update", path), e);
					}
				}
				// reboot to allow changes to take effect
				BeginReboot();
			}
			catch (Exception e)
			{
				CNXLog.Error(string.Format("Failed - UpdateSystem {0}", path), e);
			}
		}

		public static void BeginReboot()
		{
			ThreadPool.QueueUserWorkItem((o) =>
			{
				try
				{
					CNXLog.WarnFormat("UpdateSystem BeginReboot ...");
					Process process = new Process();
					process.StartInfo.FileName = "reboot";
					process.StartInfo.Arguments = null;
					process.Start();
				}
				catch (Exception e)
				{
					CNXLog.Error("System Reboot", e);
				}
			}
			);
		}
	}
}
