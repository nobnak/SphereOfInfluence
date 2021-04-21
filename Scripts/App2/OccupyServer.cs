using nobnak.Gist;
using nobnak.Gist.Extensions.ScreenExt;
using SphereOfInfluenceSys.Extensions.TimeExt;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using static SphereOfInfluenceSys.Core.Occupy;

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
					world = settings.world
				};
				Debug.Log($"{GetType().Name} : Notify. {sharing}");
				events.Changed.Invoke(sharing);
			};
		}
		private void OnValidate() {
			notifier.Invalidate();
		}
		private void Update() {
			var currTick = TimeExtension.CurrTick;
			var expiredTick = currTick - (10f + settings.world.duration).ToTicks();
			for (var i = 0; i < workingdata.regions.Count; ) {
				var r = workingdata.regions[i];
				if (r.birthTimeTick > expiredTick) {
					i++;
					continue;
				}

				notifier.Invalidate();
				workingdata.regions.RemoveAt(i);
			}

			var debug = true;
			if (debug) {
				if (Input.GetMouseButtonDown(0)) {
					var uv = Input.mousePosition.UV();
					var pos = Vector2.Scale(settings.world.worldSize, uv);
					var reg = new NetworkRegion(Time.frameCount, pos);
					workingdata.regions.Add(reg);
					Debug.Log($"{GetType().Name} : Add region. {reg}");
					notifier.Invalidate();
				}
			}

			notifier.Validate();
		}
		#endregion

		#region classes
		public class SharedData {
			public NetworkRegion[] regions;
			public WorldSetttings world;

			#region interface

			#region object
			public override string ToString() {
				var tmp = new StringBuilder();
				tmp.AppendLine($"{GetType().Name} :  ");
				tmp.AppendLine($"Regions, count={regions.Length}");
				for (var i = 0; i < regions.Length; i++)
					tmp.AppendLine($"\t{i}. {regions[i]}");
				tmp.AppendLine($"{world}");
				return tmp.ToString();
			}
			#endregion

			#endregion
		}

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
		public class WorldSetttings {
			public Vector2 worldSize = Vector2.one;
			public float duration = 15f;

		}
		[System.Serializable]
		public class ServerSettings {
			public WorldSetttings world = new WorldSetttings();
		}
		#endregion
	}
}