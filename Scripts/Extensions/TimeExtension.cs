using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SphereOfInfluenceSys.Extensions.TimeExt {

	public static class TimeExtension {

		public static readonly long TICK_REF_TIME;

		static TimeExtension() {
			var now = CurrTime;
			now -= now.TimeOfDay;
			TICK_REF_TIME = now.Ticks;
		}
		public static System.DateTimeOffset CurrTime => System.DateTimeOffset.Now;
		public static long CurrTick => CurrTime.Ticks;

		public static float RelativeSeconds => CurrTick.RelativeSecondsFromTick();
		public static float RelativeSecondsFromTick(this long tick) {
			return (float)(new System.TimeSpan(tick - TICK_REF_TIME).TotalSeconds);
		}
		public static long TickFromRelativeSeconds(this float seconds) {
			return TICK_REF_TIME 
				+ (long)(System.Math.Round(System.TimeSpan.TicksPerSecond * seconds));
		}
	}
}
