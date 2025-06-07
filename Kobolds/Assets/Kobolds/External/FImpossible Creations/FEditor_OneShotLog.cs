using System;
using System.Diagnostics;
using UnityEngine;

namespace FIMSpace
{
	public static class FEditor_OneShotLog
	{
		public static bool CanDrawLog(
			string id, int delayToNextCallInSeconds = int.MaxValue, int callLimitBeforeTimeMove = 1,
			int logSeparation = 0)
		{
			// Detecting if editor was closed since last call
			var session = Process.GetCurrentProcess().Id;

			var lastSessionId = PlayerPrefs.GetInt(id + "s", 0);

			if (lastSessionId != session)
			{
				PlayerPrefs.SetInt(id + "s", session);

				// Resetting when session detected
				PlayerPrefs.SetString(id + "acc", DateTime.Now.ToBinary().ToString());
				PlayerPrefs.SetInt(id + "counter", 0);
				PlayerPrefs.SetInt(id + "sep", logSeparation);

				if (delayToNextCallInSeconds == int.MaxValue) return true;
			}
			else
			{
				if (delayToNextCallInSeconds == int.MaxValue) return false;
			}

			var dateBinary = PlayerPrefs.GetString(id + "acc");
			var errorCounter = PlayerPrefs.GetInt(id + "counter");
			var separations = PlayerPrefs.GetInt(id + "sep");
			long dateBin;

			if (long.TryParse(dateBinary, out dateBin))
			{
				var lastAccessTime = DateTime.FromBinary(dateBin);

				if (DateTime.Now.Subtract(lastAccessTime).TotalSeconds >
					delayToNextCallInSeconds) // Last time was more than 10 hours so we are resetting error counter helper
				{
					PlayerPrefs.SetInt(id + "counter", 0);
					errorCounter = 0;
					PlayerPrefs.SetString(id + "acc", DateTime.Now.ToBinary().ToString());
				}

				separations++;
				PlayerPrefs.SetInt(id + "sep", separations);
				if (separations >= logSeparation)
				{
					separations = 0;
					PlayerPrefs.SetInt(id + "sep", separations);

					errorCounter++;
					PlayerPrefs.SetInt(id + "counter", errorCounter);

					if (errorCounter - 1 < callLimitBeforeTimeMove) return true;
				}
			}
			else
			{
				return false;
			}

			return false;
		}

		/// <summary>
		///     Not logging in build
		/// </summary>
		public static bool EditorCanDrawLog(
			string id, int delayToNextCallInSeconds = int.MaxValue, int callLimitBeforeTimeMove = 1,
			int logSeparation = 0)
		{
#if UNITY_EDITOR
			return CanDrawLog(id, delayToNextCallInSeconds, callLimitBeforeTimeMove, logSeparation);
#else
            return false;
#endif
		}
	}
}
