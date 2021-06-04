using nobnak.Gist;
using nobnak.Gist.Extensions.ScreenExt;
using SphereOfInfluenceSys.Core.Structures;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using WeSyncSys.Extensions.TimeExt;

namespace SphereOfInfluenceSys.App2 {

	public class OccupyServer : MonoBehaviour {

		[SerializeField]
		protected Events events = new Events();
		[SerializeField]
		protected ServerSettings settings = new ServerSettings();
		[SerializeField]
		protected WorkingData workingdata = new WorkingData();

		protected Validator	notifier = new Validator();

		#region unity
		private void OnEnable() {
			notifier.Reset();
			notifier.Validation += () => {
				var sharing = new SharedData() {
					regions = workingdata.regions.ToArray(),
					occupy = settings.occupy
				};
				Notify(sharing);
			};
		}
		private void OnValidate() {
			notifier.Invalidate();
		}
		private void Update() {
			var currTick = TimeExtension.CurrTick;
			var expiredTick = currTick - (1.2f * (10f + settings.occupy.lifeLimit)).ToTicks();
			for (var i = 0; i < workingdata.regions.Count; ) {
				var r = workingdata.regions[i];
				if (r.tick > expiredTick) {
					i++;
					continue;
				}

				notifier.Invalidate();
				workingdata.regions.RemoveAt(i);
			}

			if (settings.debug) {
				if (Input.GetMouseButtonDown(0)) {
					var uv = Input.mousePosition.UV();
					var pos = Vector2.Scale(Vector2.one, uv);
					var reg = new NetworkRegion(Time.frameCount, pos);
					workingdata.regions.Add(reg);
					Debug.Log($"{GetType().Name} : Add region. {reg}");
					notifier.Invalidate();
				}
			}

			notifier.Validate();
		}
		#endregion

		#region interface
		public void Notify(SharedData sharing) {
			events.Changed?.Invoke(sharing);
		}

		#endregion

		#region classes

		[System.Serializable]
		public class Events {
			[System.Serializable]
			public class SharedDataEvent : UnityEvent<SharedData> { }

			public SharedDataEvent Changed = new SharedDataEvent();
		}
		[System.Serializable]
		public class WorkingData {
			public List<NetworkRegion> regions = new List<NetworkRegion>();
		}


		[System.Serializable]
		public class ServerSettings {
			public bool debug;
			public OccupationSetttings occupy = new OccupationSetttings();
		}
		#endregion
	}
}