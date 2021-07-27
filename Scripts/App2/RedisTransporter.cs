using CloudStructures;
using CloudStructures.Converters;
using CloudStructures.Structures;
using nobnak.Gist;
using nobnak.Gist.ObjectExt;
using SphereOfInfluenceSys.Core.Structures;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using UnityEngine;
using WeSyncSys.Extensions.TimeExt;

namespace SphereOfInfluenceSys.App2 {

	public class RedisTransporter : MonoBehaviour {

		public const string CH_DataChanged = "DataChanged";

		public readonly static IValueConverter CONVERTER = new MessagePackConverter();

		[SerializeField]
		protected Events events = new Events();
		[SerializeField]
		protected Tuner settings = new Tuner();

		protected Validator validator = new Validator();

		protected TaskScheduler mainScheduler;
		protected RedisConnection redis;
		protected ISubscriber subsc;
		protected List<RouteData> routeDataUpdates = new List<RouteData>();

		#region interface
		public Tuner CurrTuner {
			get => settings.DeepCopy();
			set {
				settings = value.DeepCopy();
				validator.Invalidate();
			}
		}
		public void Notify(RouteData data) {
			Debug.Log($"Notify : {data}");
			subsc.Publish(CH_DataChanged,
				CONVERTER.Serialize<RouteData>(data),
				CommandFlags.FireAndForget);
		}

		public void NotifyOnChange(RedisConnection redis) {
			events.redisOnChange.Invoke(redis);
		}
		#endregion

		#region unity
		protected virtual void OnEnable() {
			mainScheduler = TaskScheduler.FromCurrentSynchronizationContext();

			validator.Reset();
			validator.Validation += () => {
				ReleaseRedis();
				CreateRedis();
			};

		}
		protected virtual void OnDisable() {
			ReleaseRedis();
		}
		protected virtual void Update() {
			validator.Validate();

			foreach (var d in routeDataUpdates) {
				foreach (var r in events.routers) {
					if (r.path.StartsWith(d.path))
						r.listeners.Invoke(d);
				}
			}
			routeDataUpdates.Clear();
		}
		#endregion

		#region member
		protected virtual void CreateRedis() {
			redis = new RedisConnection(
				new RedisConfig("App2", settings.server), CONVERTER);
			subsc = redis.GetConnection().GetSubscriber();

			subsc.Subscribe(CH_DataChanged, (ch, v) => {
				try {
					var data = CONVERTER.Deserialize<RouteData>(v);
					Debug.Log($"Upate notified : {data}");
					routeDataUpdates.Add(data);
				} catch (System.Exception e) {
					Debug.LogWarning(e);
				}
			});

			NotifyOnChange(redis);
		}

		protected virtual void ReleaseRedis() {
			NotifyOnChange(null);

			if (subsc != null) {
				subsc.UnsubscribeAll();
				subsc = null;
			}
			if (redis != null) {
				redis.Dispose();
				redis = null;
			}
		}
		#endregion

		#region definitions
		[System.Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct RouteData {
			public string path;
			public byte[] obj;

			#region interface
			public override string ToString() {
				var objLen = (obj != null) ? obj.Length : -1;
				return $"<{GetType().Name} : path={path}, data_length={objLen}>";
			}
			#endregion
		}
		[System.Serializable]
		public class RouteEvent : UnityEngine.Events.UnityEvent<RouteData> { }
		[System.Serializable]
		public class RedisEvent : UnityEngine.Events.UnityEvent<RedisConnection> { }

		[System.Serializable]
		public class Route {
			public string path;
			public RouteEvent listeners = new RouteEvent();
		}
		[System.Serializable]
		public class Events {
			public RedisEvent redisOnChange = new RedisEvent();
			public Route[] routers = new Route[0];
		}
		
		[System.Serializable]
		public class Tuner {
			public string server = "127.0.0.1";
		}
		#endregion
	}
}
