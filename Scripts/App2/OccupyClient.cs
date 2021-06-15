using nobnak.Gist;
using nobnak.Gist.Cameras;
using nobnak.Gist.Extensions.ScreenExt;
using nobnak.Gist.Extensions.Texture2DExt;
using nobnak.Gist.GPUBuffer;
using nobnak.Gist.ObjectExt;
using SphereOfInfluenceSys.Core;
using SphereOfInfluenceSys.Core.Structures;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using WeSyncSys;

namespace SphereOfInfluenceSys.App2 {

	public class OccupyClient : MonoBehaviour {

		public const CameraEvent EVT_CAMCBUF = CameraEvent.AfterEverything;

		[SerializeField]
		protected Camera targetCam;
		[SerializeField]
		protected WeSyncExhibitor wesync;
		[SerializeField]
		protected Tuner tuner = new Tuner();
		[SerializeField]
		protected WorkingMem mem = new WorkingMem();

		protected CameraData cameraData = default;
		protected SharedData shared;

		protected Validator validator = new Validator();
		protected Occupy occupy;
		protected RenderTexture colorTex;
		protected PIPTexture pip;

		protected AsyncCPUTexture<Vector4> idTexCpu = new AsyncCPUTexture<Vector4>();
		protected Coroutine idTexReadbackCo;

		#region unity
		private void OnEnable() {
			occupy = new Occupy();
			pip = new PIPTexture();

			validator.Reset();
			validator.SetCheckers(() => cameraData.Equals(targetCam));
			validator.Validation += () => {
				cameraData = targetCam;
				if (targetCam == null)
					return;

				var size = targetCam.Size();
				if (colorTex == null || colorTex.Size() != size) {
					colorTex.DestroySelf();
					colorTex = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32);
					colorTex.filterMode = FilterMode.Point;
					colorTex.wrapMode = TextureWrapMode.Clamp;
					colorTex.enableRandomWrite = true;
					colorTex.Create();
				}

				if (wesync != null)
					Debug.Log($"{wesync.CurrSubspace}");

				pip.TargetCam = targetCam;
				pip.CurrTuner = tuner.pip;
				pip.Clear();
				pip.Add(colorTex);
			};

			idTexReadbackCo = StartCoroutine(UpdateOccupation());
		}
		private void OnDisable() {
			if (idTexReadbackCo != null) {
				StopCoroutine(idTexReadbackCo);
				idTexReadbackCo = null;
			}
			if (occupy != null) {
				occupy.Dispose();
				occupy = null;
			}
			if (colorTex != null) {
				colorTex.DestroySelf();
				colorTex = null;
			}
			if (pip != null) {
				pip.Dispose();
				pip = null;
			}
		}
		private void OnValidate() {
			validator.Invalidate();
		}
		#endregion

		#region members
		private IEnumerator UpdateOccupation() {
			while (true) {
				yield return null;

				validator.Validate();
				pip.Validate();

				if (wesync != null) {
					var subspace = wesync.CurrSubspace;
					if (subspace != default) {
						occupy.CurrTuner = mem.occupy;
						occupy.Update(subspace, colorTex);
					}
				}

				idTexCpu.Source = occupy.IdTex;
				foreach (var __ in idTexCpu)
					yield return null;
			}
		}
		#endregion

		#region interface
		public Tuner CurrTuner {
			get {
				validator.Validate();
				return tuner;
			}
			set {
				tuner = value.DeepCopy();
				validator.Invalidate();
			}
		}
		public void Listen(SharedData shared) {
			Debug.Log($"{GetType().Name} : Receive shared data. {shared}");
			this.shared = shared;
			mem.occupy.occupy = shared.occupy.DeepCopy();
			occupy.Clear();
			foreach (var r in shared.regions)
				occupy.Add(r);
			validator.Invalidate();
		}
		public void ListenCamera(GameObject go) {
			Debug.Log($"Update camera");
			targetCam = go.GetComponent<Camera>();
			validator.Invalidate();
		}
		public void Listen(WeSyncExhibitor wesync) {
			Debug.Log($"Update WeSync");
			this.wesync = wesync;
			validator.Invalidate();
		}
		#endregion

		#region definition
		[System.Serializable]
		public class Tuner {
			public PIPTexture.Tuner pip = new PIPTexture.Tuner();
		}
		[System.Serializable]
		public class WorkingMem {
			public Occupy.Tuner occupy = new Occupy.Tuner();
		}
		#endregion
	}
}
