using System;
using System.IO.Ports;
using System.Threading;
using System.Text;

namespace NMEAGPSClient
{
	public class NMEA0183ServiceReciever : NMEAGPSClient
	{
		//private NMEASentenceReader mNMEASentenceReader = new NMEASentenceReader();
		private long mChecksumErrors = 0;
		/// <summary>
		/// The number of checksum errors encountered.
		/// </summary>
		protected long ChecksumErrorCount
		{
			get { return mChecksumErrors; }
		}

		private long mSentencesRecieved = 0;
		/// <summary>
		/// The number of sentences recieved from the GPS.
		/// </summary>
		protected long SentenceCount
		{
			get { return mSentencesRecieved; }
		}

		private long mValidSentences = 0;
		/// <summary>
		/// The number of sentences recieved from the GPS.
		/// </summary>
		protected long ValidSentenceCount
		{
			get { return mValidSentences; }
		}

		private string mLastNMEASentence = string.Empty;
		/// <summary>
		/// Retruns the last sentence from the GPS
		/// </summary>
		public string LastNMEASentence
		{
			get { return mLastNMEASentence; }
		}

		private string mLastNMEASentenceType = string.Empty;
		/// <summary>
		/// Retruns the last sentence type from the GPS
		/// </summary>
		public string LastNMEASentenceType
		{
			get { return mLastNMEASentenceType; }
		}

        public NMEA0183ServiceReciever()
        {
            mGPSPosition.Invalidate();
        }

		private void ProcessNewSentence(string sentence)
		{
            if (sentence.Length < 6)
			{
                OnRaiseGPSStatusChangedEvent(new GPSStatusEvent(GPS_STATUS.OFFLINE));
                CNXLog.ErrorFormat("NMEAClient short sentence {0}", sentence);
				return;
			}

			try
			{
                //CNXLog.InfoFormat("NMEAClient ProcessNewSentence {0}", sentence);
                bool updateAvailable = false;
                string sentenceType = "";

                if (sentence.Contains("RMC"))
                {
                    sentenceType = "RMC";
                }
                else if (sentence.Contains("GGA"))
                {
                    sentenceType = "GGA";
                }
                else if (sentence.Contains("GSA"))
                {
                    sentenceType = "GSA";
                }

                // Determine the type of sentence.
                // The NMEA specification states that the first two letters of a sentence may change.
                // For example, for "$GPGSV" there may be variations such as "$__GSV" where the first two letters change.
                // As a result, we need only test the last three characters.

                // Is this a GPRMC sentence?
                if (sentenceType.Equals("RMC", StringComparison.Ordinal))
                {
                    updateAvailable = mGPSPosition.ParseRMC(sentence);
                    if (!updateAvailable)
                    {
                        OnRaiseGPSPositionChangedEvent(new GPSPositionEvent(mGPSPosition));
                    }
                }
                else if (sentenceType.Equals("GGA", StringComparison.Ordinal))
                {
                    // dont update on GGA, only augment error estimates.
                    updateAvailable = mGPSPosition.ParseGGA(sentence);
                }
                else if (sentenceType.Equals("GSA", StringComparison.Ordinal))
                {
                    updateAvailable = mGPSPosition.ParseGSA(sentence);
                }

                if (updateAvailable)
				{
					mLastNMEASentenceType = sentenceType;

                    // report any status changes
                    GPS_STATUS status = GPS_STATUS.OFFLINE;
					switch (mGPSPosition.NMEAMode)
					{
						case GPSPosition.NMEAMODE.NO_FIX:
							status = GPS_STATUS.Connected;
							mTravellingState = MovingState.Unknown;
							break;
						case GPSPosition.NMEAMODE.NO_MODE:
							status = GPS_STATUS.Connected | GPS_STATUS.GPSResponding;
							mTravellingState = MovingState.Unknown;
							break;
						case GPSPosition.NMEAMODE.TWO_DIMENSION:
						case GPSPosition.NMEAMODE.THREE_DIMENSION:
							status = GPS_STATUS.Connected | GPS_STATUS.GPSResponding | GPS_STATUS.Fix;
							// set the travelling state
							mAverageVelocity.Value = mGPSPosition.SpeedOverGround;

                            //lat = mG
							mTravellingState = (mAverageVelocity.Value > mMovingThreshold) ? MovingState.Moving : MovingState.Stationary;
                            break;
					}
					if (status != mStatus)
					{
						mStatus = status;
						OnRaiseGPSStatusChangedEvent(new GPSStatusEvent(mStatus));
						//Console.WriteLine("Fired Status Changed.");
					}
					// only update from RMC sentences as other sentences dont have date & time.
					if (sentenceType.Equals("RMC", StringComparison.Ordinal))
					{
						//Console.WriteLine("Firing Position Changed.");
						//CNXLog.InfoFormat("GPS State {0}, {1}", mStatus, mGPSPosition.CurrentPosition.ToString());
						//if (mGPSPosition.Tag.EndsWith("RMC", StringComparison.Ordinal))
						if ((mStatus & GPS_STATUS.Fix) == GPS_STATUS.Fix)
						{
							//CNXLog.InfoFormat(mGPSPosition.CurrentPosition.ToString());
							OnRaiseGPSPositionChangedEvent(new GPSPositionEvent(mGPSPosition));
							//CNXLog.InfoFormat("OnRaiseGPSPositionChangedEvent completed.");
							//CNXLog.InfoFormat(mGPSPosition.CurrentPosition.ToString());
							//Console.WriteLine("Fired Position Changed.");
						}
					}
				}
				//CNXLog.InfoFormat("GPS State {0}, {1}", mStatus, mGPSPosition.CurrentPosition.ToString());
			}
			catch (Exception e)
			{
				CNXLog.Error(String.Format("NMEAClient ProcessNewSentence {0}", sentence), e);
			}
		}

		private bool ProcessSentence(string sentence)
		{
			// check that the sentence is valid
			bool updateAvailable = ValidateNMEASentence(sentence);

			if (!updateAvailable)
				return false;

			// Determine the type of sentence.
			// The NMEA specification states that the first two letters of a sentence may change.
			// For example, for "$GPGSV" there may be variations such as "$__GSV" where the first two letters change.
			// As a result, we need only test the last three characters.

			string sentenceType = sentence.Substring(0, 6);

			// Is this a GPRMC sentence?
			if (sentenceType.EndsWith("RMC", StringComparison.Ordinal))
			{
				updateAvailable = mGPSPosition.ParseRMC(sentence);
				//if (updateAvailable)
				//    CNXLog.InfoFormat(mGPSPosition.CurrentPosition.ToString());
			}
			else if (sentenceType.EndsWith("GGA", StringComparison.Ordinal))
				// dont update on GGA, only augment error estimates.
				updateAvailable = mGPSPosition.ParseGGA(sentence);
			//else if (mLastNMEASentenceType.EndsWith("GLL", StringComparison.Ordinal))
			//{
			//    // Yes.  Convert it using the fast pre-parseed constructor
			//    return new GpgllSentence(sentence.Sentence, sentence.CommandWord, sentence.Words, sentence.ExistingChecksum);
			//}
			//else if (mLastNMEASentenceType.EndsWith("GSV", StringComparison.Ordinal))
			//{
			//    // Yes.  Convert it using the fast pre-parseed constructor
			//    return new GpgsvSentence(sentence.Sentence, sentence.CommandWord, sentence.Words, sentence.ExistingChecksum);
			//}
			//else if (mLastNMEASentenceType.EndsWith("GSA", StringComparison.Ordinal))
			//{
			//    // Yes.  Convert it using the fast pre-parseed constructor
			//    return new GpgsaSentence(sentence.Sentence, sentence.CommandWord, sentence.Words, sentence.ExistingChecksum);
			//}

			if (updateAvailable)
				mLastNMEASentenceType = sentenceType;

			return updateAvailable;
		}

		/// <summary>
		/// Provides validation that the sentence is a complete and valid NMEA sentence
		/// </summary>
		/// <param name="sentence">The sentence to be validated.</param>
		/// <returns>True if the sentence is a valid NMEA formated sentence.</returns>
		/// <remarks>
		/// NMEA sentences are of the form $GPRMC,11,22,33,44,55*CC
		/// A valid sentence starts with a $.
		/// Has comma seperated data values.
		/// Has a * indicating the end of the data and position of the checksum.
		/// The two checksum characters are correct.
		/// </remarks>
		public bool ValidateNMEASentence(string sentence)
		{
			// Does it begin with a dollar sign?
			if (!sentence.StartsWith("$", StringComparison.Ordinal))
			{
				CNXLog.WarnFormat("NMEAClient Validation failed, no $.");
				return false;
			}

			// make sure there are data fields
			if (sentence.IndexOf(",", StringComparison.Ordinal) == -1)
			{
				CNXLog.WarnFormat("NMEAClient Validation failed, no data fields.");
				return false;
			}

			// Next, get the index of the asterisk
			int asteriskIndex = sentence.IndexOf("*", StringComparison.Ordinal);

			// Is an asterisk present?
			if (asteriskIndex == -1)
			{
				CNXLog.WarnFormat("NMEAClient Validation failed, no *.");
				return false;
			}

			// The checksum is calculated over the command and data portion of the sentence
			byte checksum = (byte)sentence[1];
			for (int index = 2; index < asteriskIndex; ++index)
				checksum ^= (byte)sentence[index];

			// The checksum is the two-character hexadecimal value
			string calculatedChecksum = checksum.ToString("X2", NMEACultureInfo);
			string sentenceChecksum = sentence.Substring(asteriskIndex + 1, 2);

			if (!calculatedChecksum.Equals(sentenceChecksum, StringComparison.Ordinal))
			{
				++mChecksumErrors;
				CNXLog.WarnFormat("NMEAClient checksum errors {0}, sentence ratio {1}/{2}.", mChecksumErrors, mValidSentences, mSentencesRecieved);
				return false;
			}

			return true;
		}

        public void PassNMEAStringToQueue(string nmea)
        {
            // Queue the task.
            ThreadPool.QueueUserWorkItem((o) =>
            {
                ProcessNewSentence(nmea);
            }
            );
        }
    }
}
