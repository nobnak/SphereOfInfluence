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

		public static float ToSeconds(this long tick) {
			return (float)(new System.TimeSpan(tick).TotalSeconds);
		}
		public static long ToTicks(this float seconds) {
			return (long)(System.Math.Round(System.TimeSpan.TicksPerSecond * seconds));
		}
		public static System.DateTimeOffset ToDateTime(this long tick) {
			return new System.DateTimeOffset(tick, CurrTime.Offset);
		}

		public static System.DateTimeOffset CurrTime => System.DateTimeOffset.Now;
		public static long CurrTick => CurrTime.Ticks;
		public static float CurrRelativeSeconds => CurrTick.RelativeSeconds();

		public static float RelativeSeconds(this long tick) {
			return (tick - TICK_REF_TIME).ToSeconds();
		}
		public static long TickFromRelativeSeconds(this float seconds) {
			return TICK_REF_TIME + seconds.ToTicks();
		}
	}
}
