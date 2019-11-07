using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Tracking.Services;

namespace CANLib
{
	[Serializable]
	public class DeviceCatalogue
	{
		[field: NonSerialized()]
#if ANDROID
        private static log4droid CNXLog = new log4droid() {Logger = "CNXLogger"};
#else
        private static log4net.ILog CNXLog = log4net.LogManager.GetLogger("CNXLogger");
#endif
        /// <summary>
        /// Indicates what type of change occured during an update.
        /// </summary>
        public enum CatalogueChangeType
		{
			/// <summary>
			/// No change occured. Equipment and status remained the same.
			/// </summary>
			NONE,
			/// <summary>
			/// New equipment or version change.
			/// </summary>
			EQUIPMENT,
			/// <summary>
			/// The device status changed.
			/// </summary>
			STATUS,
		}

		private List<DeviceCatalogueInfo> mDeviceCatalogue = new List<DeviceCatalogueInfo>();
		/// <summary>
		/// Gets the device cataloge.
		/// </summary>
		public ReadOnlyCollection<DeviceCatalogueInfo> Catalogue
		{
			get { return mDeviceCatalogue.AsReadOnly(); }
		}

		private DeviceCatalogueInfo.EquipmentFlages mEquipmentMask;
		/// <summary>
		/// Gets the amalgamated equipment mask
		/// </summary>
		public DeviceCatalogueInfo.EquipmentFlages EquipmentMask { get { return mEquipmentMask; } }
		private DeviceCatalogueInfo.EquipmentFlages mEquipmentStatus;
		/// <summary>
		/// Gets the amalgamated equipment status
		/// </summary>
		public DeviceCatalogueInfo.EquipmentFlages EquipmentStatus { get { return mEquipmentStatus; } }

		/// <summary>
		/// Acts as a heart beat for the device cataloge to make sure the status is still fresh.
		/// </summary>
		/// <returns>True if the catalogue was changed.</returns>
		public CatalogueChangeType AgeCatalogue()
		{
			// set all the device status fields to aged.
			foreach (DeviceCatalogueInfo info in mDeviceCatalogue)
				info.PulseAlive();

			return (RefreshEquipmentFlags() ? CatalogueChangeType.STATUS : CatalogueChangeType.NONE);
		}

		/// <summary>
		/// Finds a catalogue entry with matching producId.
		/// </summary>
		/// <param name="productId">Product to search for.</param>
		/// <returns>Catalogue entry if found otherwise null.</returns>
		public DeviceCatalogueInfo FindDevice(DeviceCatalogueInfo.Product productId)
		{
			foreach (DeviceCatalogueInfo info in mDeviceCatalogue)
				if (info.ProductId == productId)
					return info;

			return null;
		}

		/// <summary>
		/// Updates the device catalogue to include the new item or update the details of an existing item.
		/// </summary>
		/// <param name="info">Device details to be used to update the catalogue.</param>
		/// <returns>The type of change that occured</returns>
		public CatalogueChangeType UpdateDeviceCatalogue(DeviceCatalogueInfo info)
		{
			CatalogueChangeType catalogueUpdated = CatalogueChangeType.NONE;
			try
			{
				if (info.ProductId > 0 && info.BuildNo > 0)
				{
					DeviceCatalogueInfo existing = FindDevice(info.ProductId);

					if (existing == null)
					{
						mDeviceCatalogue.Add(info);
						catalogueUpdated = CatalogueChangeType.EQUIPMENT;
					}
					else
						// update entry
						catalogueUpdated = existing.Update(info);

					// update the equipment mask and status
					if (catalogueUpdated != CatalogueChangeType.NONE)
						RefreshEquipmentFlags();
				}
			}
			catch (Exception e)
			{
				CNXLog.Error("UpdateDeviceCatalogue", e);
			}

			return catalogueUpdated;
		}

		public bool RefreshEquipmentFlags()
		{
			DeviceCatalogueInfo.EquipmentFlages mask = DeviceCatalogueInfo.EquipmentFlages.None;
			DeviceCatalogueInfo.EquipmentFlages status = DeviceCatalogueInfo.EquipmentFlages.None;

			foreach (DeviceCatalogueInfo dev in mDeviceCatalogue)
			{
				mask |= dev.Mask;
				status |= dev.Status;
			}

			bool changed = (mEquipmentMask != mask || mEquipmentStatus != status);

			mEquipmentMask = mask;
			mEquipmentStatus = status;

			return changed;
		}

		/// <summary>
		/// Clears all entries in the catalogue.
		/// Used when equipment is changed on the vehicle.
		/// </summary>
		public void Clear()
		{
			mDeviceCatalogue.Clear();
		}

		/// <summary>
		/// Sets all the device status entries to failed
		/// </summary>
		public void ResetStatus()
		{
			foreach (DeviceCatalogueInfo dev in mDeviceCatalogue)
				dev.Status = DeviceCatalogueInfo.EquipmentFlages.None;
		}

		public override string ToString()
		{
			return String.Format("{0} devices, Mask {1}, Status {2}", mDeviceCatalogue.Count, mEquipmentMask, mEquipmentStatus);
		}
	}
}
