using System;
using System.IO;
using System.Threading;

using Tracking.Services;

namespace CANLib
{
	/// <summary>
	/// Suports product cataloue handling
	/// </summary>
	public class ProductCatalogue
	{
#if ANDROID
        private static log4droid CNXLog = new log4droid() {Logger = "CNXLogger"};
#else
        private static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif
		private CANClient mCANClient;
		private CANFrame mCatalogueFrame;
		private System.Threading.Timer mCatalogueTimer;
		private DeviceCatalogueInfo.EquipmentFlages mMask;
		private DeviceCatalogueInfo.EquipmentFlages mStatus;

		public DeviceCatalogueInfo.EquipmentFlages Mask
		{
			get { return mMask; }
			set
			{
				mMask = value;
				ModifyMaskStatus();
			}
		}
		public DeviceCatalogueInfo.EquipmentFlages Status
		{
			get { return mStatus; }
			set
			{
				mStatus = value;
				ModifyMaskStatus();
			}
		}

		private void ModifyMaskStatus()
		{
			byte[] data;

			// allocate the data payload
			if ((uint)mMask > 0x0000ffff)
				data = new byte[8];
			else if ((uint)mMask > 0x000000ff)
				data = new byte[6];
			else if ((uint)mMask > 0)
				data = new byte[4];
			else
				data = new byte[2];

			data[0] = mCatalogueFrame.Bytes[0];
			data[1] = mCatalogueFrame.Bytes[1];

			if (data.Length > 3)
			{
				data[2] = (byte)mMask;
				data[3] = (byte)mStatus;
			}

			if (data.Length > 5)
			{
				data[4] = (byte)(((uint)mMask >> 8) & 0xff);
				data[5] = (byte)(((uint)mStatus >> 8) & 0xff);
			}

			if (data.Length > 7)
			{
				data[6] = (byte)(((uint)mMask >> 16) & 0xff);
				data[7] = (byte)(((uint)mStatus >> 16) & 0xff);
			}

			mCatalogueFrame.Data = data;
		}

		/// <summary>
		/// Initialises a ProductCatalogue from a file.
		/// </summary>
		/// <param name="path">Path to product file.</param>
		/// <remarks>
		/// The file should be a text file with seperate lines containing the product Id and the version.
		/// The lines should be space seperated key value pairs as below.
		/// PRODUCT_ID 2
		/// VERSION 4
		/// </remarks>
		public ProductCatalogue(CANClient can, string path, DeviceCatalogueInfo.EquipmentFlages mask, DeviceCatalogueInfo.EquipmentFlages state)
		{
			mMask = mask;
			mStatus = state;
			mCatalogueFrame = PopulateProductFrameFromFile(path, mask, state);
			_ProductCatalogue(can);
		}

		/// <summary>
		/// Initialises a ProductCatalogue
		/// </summary>
		/// <param name="prodId">The ID of the product.</param>
		/// <param name="prodVersion">The current version of the product.</param>
		public ProductCatalogue(CANClient can, byte prodId, byte prodVersion, DeviceCatalogueInfo.EquipmentFlages mask, DeviceCatalogueInfo.EquipmentFlages state)
		{
			mMask = mask;
			mStatus = state;
			mCatalogueFrame = CNXCANMsgHelper.PackProductId(prodId, prodVersion, (uint)mask, (uint)state);
			_ProductCatalogue(can);
		}

		private void _ProductCatalogue(CANClient can)
		{
			mCANClient = can;
			// start time for cataloge and status reporting
			mCatalogueTimer = new System.Threading.Timer(new TimerCallback(OnTimedEvent), null, 60 * 1000, 60 * 1000);
		}

		private void OnTimedEvent(object state)
		{
			SendDeviceCatalogue();
		}

		/// <summary>
		/// Sends the catalogue frame.
		/// </summary>
		public void SendDeviceCatalogue()
		{
			mCANClient.Send(mCatalogueFrame);
		}

		/// <summary>
		/// Reads product information from a file.
		/// </summary>
		/// <param name="path">Path to product file.</param>
		/// <remarks>
		/// The file should be a text file with seperate lines containing the product Id and the version.
		/// The lines should be space seperated key value pairs as below.
		/// PRODUCT_ID 2
		/// VERSION 4
		/// </remarks>
		public static CANFrame PopulateProductFrameFromFile(string path)
		{
			return PopulateProductFrameFromFile(path, DeviceCatalogueInfo.EquipmentFlages.None, DeviceCatalogueInfo.EquipmentFlages.None);
		}

		/// <summary>
		/// Reads product information from a file.
		/// </summary>
		/// <param name="path">Path to product file.</param>
		/// <param name="mask">The device mask to set.</param>
		/// <param name="state">The device states for the masked devices.</param>
		/// <remarks>
		/// The file should be a text file with seperate lines containing the product Id and the version.
		/// The lines should be space seperated key value pairs as below.
		/// PRODUCT_ID 2
		/// VERSION 4
		/// </remarks>
		public static CANFrame PopulateProductFrameFromFile(string path, DeviceCatalogueInfo.EquipmentFlages mask, DeviceCatalogueInfo.EquipmentFlages state)
		{
			byte prodId;
			byte ver;
			ProductInfoFromFile(path, out prodId, out ver);
			CANFrame frame = CNXCANMsgHelper.PackProductId(prodId, ver, (uint)mask, (uint)state);
			CNXLog.WarnFormat("Product Frame {0}", frame.ToString());
			return frame;
		}

		/// <summary>
		/// Reads product information from a file.
		/// </summary>
		/// <param name="path">Path to product file.</param>
		/// <param name="productId">Product Id read from file.</param>
		/// <param name="version">Version read from file.</param>
		/// <remarks>
		/// The file should be a text file with seperate lines containing the product Id and the version.
		/// The lines should be space seperated key value pairs as below.
		/// PRODUCT_ID 2
		/// VERSION 4
		/// </remarks>
		public static void ProductInfoFromFile(string path, out byte productId, out byte version)
		{
			productId = 0;
			version = 0;

			char[] seperators = { ' ', '\t', ',', '=' };

			try
			{
				using (StreamReader sr = File.OpenText(path))
				{
					String line;
					while ((line = sr.ReadLine()) != null)
					{
						string[] args = line.Split(seperators, StringSplitOptions.RemoveEmptyEntries);

						for (int i = 0; i < args.Length; ++i)
						{
							switch (args[i])
							{
								case "PRODUCT_ID":
									productId = Byte.Parse(args[++i]);
									break;
								case "VERSION":
									version = Byte.Parse(args[++i]);
									break;
							}
						}
					}
				}
				CNXLog.WarnFormat("Loaded Product Frame ID {0}, Ver {1}", productId, version);
			}
			catch (Exception e)
			{
				CNXLog.Error("Failed to get versioning", e);
			}
		}

		public override string ToString()
		{
			return mCatalogueFrame.ToString();
		}
	}
}
