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

	public class RemoteOccupyServer : RemoteOccupyBase {

		[SerializeField]
		protected Events events = new Events();
		[SerializeField]
		protected Tuner settings = new Tuner();
		[SerializeField]
		protected WorkingData workingdata = new WorkingData();

		protected Validator	workingDataValidator = new Validator();

		#region unity
		protected override void OnEnable() {
			base.OnEnable();

			connectionValidator.Validated += () => workingDataValidator.Invalidate();

			workingDataValidator.Reset();
			workingDataValidator.Validation += () => {
				if (!IsRedisInitialized)
					return;

				var sharing = new SharedData() {
					regions = workingdata.regions.ToArray(),
					occupy = settings.occupy
				};
				Send(sharing);
			};
		}
		protected void OnValidate() {
			workingDataValidator.Invalidate();
		}
		protected override void Update() {
			base.Update();

			var currTick = TimeExtension.CurrTick;
			var expiredTick = currTick - (1.2f * (10f + settings.occupy.lifeLimit)).ToTicks();
			for (var i = 0; i < workingdata.regions.Count; ) {
				var r = workingdata.regions[i];
				if (r.tick > expiredTick) {
					i++;
					continue;
				}

				workingDataValidator.Invalidate();
				workingdata.regions.RemoveAt(i);
			}

			if (settings.debug) {
				if (Input.GetMouseButtonDown(0)) {
					var uv = Input.mousePosition.UV();
					var pos = Vector2.Scale(Vector2.one, uv);
					var reg = new NetworkRegion(Time.frameCount, pos);
					workingdata.regions.Add(reg);
					Debug.Log($"{GetType().Name} : Add region. {reg}");
					workingDataValidator.Invalidate();
				}
			}

			workingDataValidator.Validate();
		}
		#endregion

		#region interface
		public void Notify(RedisTransporter.RouteData route) {
			events.routeOnSend.Invoke(route);
		}
		public Tuner CurrTuner { 
			get => settings.DeepCopy();
			set {
				settings = value.DeepCopy();
				workingDataValidator.Invalidate();
			}
		}
		#endregion

		#region member
		protected virtual void Send(SharedData shared) {
			try {
				ThrowIfRedisIsNotInitialized();
				redisString
					.SetAsync(shared)
					.ContinueWith(t => {
						var data = new RedisTransporter.RouteData() {
							path = PATH,
						};
						Notify(data);
					}, mainScheduler);
			} catch (System.Exception e) {
				Debug.LogWarning(e);
			}
		}
		#endregion

		#region classes

		[System.Serializable]
		public class Events {
			[System.Serializable]
			public class SharedDataEvent : UnityEvent<SharedData> { }

			public RedisTransporter.RouteEvent routeOnSend = new RedisTransporter.RouteEvent();
		}
		[System.Serializable]
		public class WorkingData {
			public List<NetworkRegion> regions = new List<NetworkRegion>();
		}
		[System.Serializable]
		public class Tuner {
			public bool debug;
			public OccupyTuner occupy = new OccupyTuner();
		}
		#endregion
	}
}