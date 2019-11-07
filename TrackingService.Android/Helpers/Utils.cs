
using System;
using Android.Content.PM;
using Android.OS;
using System.Globalization;

namespace Helpers
{
	public static class Utils
	{

		public static string DateString
		{
			get
			{
        var day = CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(DateTime.Now.DayOfWeek);
				var month = CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(DateTime.Now.Month);
				var dayNum = DateTime.Now.Day;
				if(Helpers.Settings.UseKilometeres)
					return day + " " + dayNum + " " + month;

				return day  + " " + month+ " " + dayNum;
			}
		}

	}
}

