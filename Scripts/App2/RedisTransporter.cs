using CloudStructures;
using CloudStructures.Converters;
using CloudStructures.Structures;
using SphereOfInfluenceSys.App2.Structures;
using StackExchange.Redis;
using System.Threading.Tasks;
using UnityEngine;
using WeSyncSys.Extensions.TimeExt;

namespace SphereOfInfluenceSys.App2 {

	public class RedisTransporter : MonoBehaviour {

		public const string CH_SHARED_DATA_UPDATE = "SharedDataUpdate";
		public const string K_APP2_DATA = "App2Data";

		[SerializeField]
		protected Events events = new Events();
		[SerializeField]
		protected Settings settings = new Settings();

		protected RedisConnection redis;
		protected RedisString<SharedData> redisString;
		protected ISubscriber subsc;

		protected TaskScheduler mainScheduler;

		#region interface
		public void Listen(SharedData shared) {
			TaskSet(shared);
		}
		public void Notify(SharedData shared) {
			events.Changed?.Invoke(shared);
		}
		#endregion

		#region unity
		private void OnEnable() {
			mainScheduler = TaskScheduler.FromCurrentSynchronizationContext();

			redis = new RedisConnection(
				new RedisConfig("App2", settings.server), new MessagePackConverter());
			redisString = new RedisString<SharedData>(
				redis, K_APP2_DATA, System.TimeSpan.FromHours(1));
			subsc = redis.GetConnection().GetSubscriber();

			subsc.Subscribe(CH_SHARED_DATA_UPDATE, (ch, v) => {
				TaskGet();

			});

		}
		private void OnDisable() {
			if (subsc != null) {
				subsc.UnsubscribeAll();
				subsc = null;
			}
			if (redis != null) {
				redis.Dispose();
				redis = null;
			}
		}
		private void Update() {
		}
		#endregion

		#region member
		private void TaskSet(SharedData shared) {
			try {
				redisString
					.SetAsync(shared)
					.ContinueWith(t => {
						TaskPub();
					}, mainScheduler);
			} catch (System.Exception e) {
				Debug.LogWarning(e);
			}
		}
		private void TaskGet() {
			try {
				redisString
					.GetAsync()
					.ContinueWith(t => _TaskGetResult(t), mainScheduler);
			} catch (System.Exception e) {
				Debug.LogWarning(e);
			}
		}
		protected void TaskPub() {
			try {
				subsc.Publish(CH_SHARED_DATA_UPDATE, TimeExtension.CurrTick, CommandFlags.FireAndForget);
			} catch(System.Exception e) {
				Debug.LogWarning(e);
			}
		}
		private void _TaskGetResult(Task<RedisResult<SharedData>> t) {
			try {
				if (t.IsFaulted)
					Debug.LogWarning(t.Exception);
				else if (t.IsCompleted) {
					Notify(t.Result.Value);
				}
			} catch (System.Exception e) {
				Debug.LogWarning(e);
			}
		}
		#endregion

		#region definitions
		[System.Serializable]
		public class Events {
			public OccupyServer.Events.SharedDataEvent Changed = new OccupyServer.Events.SharedDataEvent();
		}
		[System.Serializable]
		public class Settings {
			public string server = "127.0.0.1";
		}
		#endregion
	}
}
