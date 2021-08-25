using CloudStructures;
using CloudStructures.Structures;
using nobnak.Gist;
using nobnak.Gist.Extensions.ScreenExt;
using nobnak.Gist.ObjectExt;
using SphereOfInfluenceSys.Core.Structures;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;
using WeSyncSys.Extensions.TimeExt;

namespace SphereOfInfluenceSys.App2 {

	public class RemoteOccupyBase : MonoBehaviour {

		public const string K_SharedData = "SharedData";
		public const string PATH = "/occupyshared_data";

		protected Validator connectionValidator = new Validator();

		protected RedisConnection redis;
		protected TaskScheduler mainScheduler;
		protected RedisString<SharedData> redisString;

		#region interface
		public virtual bool IsRedisInitialized { get => redis != null; }
		public virtual void ThrowIfRedisIsNotInitialized() {
			if (!IsRedisInitialized)
				throw new System.Exception("Redis is not working");
		}
		public virtual void Listen(RedisConnection redis) {
			connectionValidator.Invalidate();
			this.redis = redis;

			if (IsRedisInitialized) {
				redisString = new RedisString<SharedData>(
					redis, K_SharedData, System.TimeSpan.FromHours(1));
			}
		}
		#endregion

		#region unity
		protected virtual void OnEnable() {
			mainScheduler = TaskScheduler.FromCurrentSynchronizationContext();

			connectionValidator.Reset();
		}
		protected virtual void Update() {
			connectionValidator.Validate();
		}
		#endregion
	}
}