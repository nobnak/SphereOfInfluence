using nobnak.Gist;
using nobnak.Gist.Cameras;
using nobnak.Gist.Collection;
using nobnak.Gist.Extensions.ScreenExt;
using nobnak.Gist.Extensions.Texture2DExt;
using nobnak.Gist.GPUBuffer;
using nobnak.Gist.ObjectExt;
using SphereOfInfluenceSys.Core;
using SphereOfInfluenceSys.Core.Abstracts;
using SphereOfInfluenceSys.Core.Interfaces;
using SphereOfInfluenceSys.Core.Structures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using WeSyncSys;

namespace SphereOfInfluenceSys.App2 {

	[ExecuteAlways]
	public class LocalOccupyClient : AbstractOccupyClient {

		public const CameraEvent EVT_CAMCBUF = CameraEvent.AfterEverything;

		[SerializeField]
		protected Camera targetCam;
		[SerializeField]
		protected WeSyncExhibitor wesync;
		[SerializeField]
		protected Tuner tuner = new Tuner();

		protected CameraData cameraData;
		protected SharedData shared;

		protected Validator validator = new Validator();
		protected Occupy occupy;
		protected RenderTexture colorTex;
		protected PIPTexture pip;

		protected AsyncCPUTexture<Vector4> idTexCpu = new AsyncCPUTexture<Vector4>();
		protected Coroutine idTexReadbackCo;

		protected ReusableIndexStorage occIdStorage;
		protected HashSet<int> tmpRegIdSet;
		protected Dictionary<int, int> occToRegIdMap;
		protected Dictionary<int, ReusableIndexStorage.Token> regToOccIdMap;
		protected List<Occupy.Region> regions = new List<Occupy.Region>();

		#region unity
		protected virtual void OnEnable() {
			occupy = new Occupy();
			pip = new PIPTexture();

			tmpRegIdSet = new HashSet<int>();
			occIdStorage = new ReusableIndexStorage();
			occToRegIdMap = new Dictionary<int, int>();
			regToOccIdMap = new Dictionary<int, ReusableIndexStorage.Token>();

			regions.Clear();

			cameraData = default;
			validator.Reset();
			validator.SetCheckers(() => cameraData.Equals(targetCam));
			validator.Validation += () => {
				cameraData = targetCam;
				if (targetCam == null) return;

				CurrTuner = tuner;
				if (!isActiveAndEnabled) return;

				var size = occupy.SetScreenSize(targetCam.Size());
				if (colorTex == null || colorTex.Size() != size) {
					colorTex.DestroySelf();
					colorTex = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32);
					colorTex.filterMode = FilterMode.Point;
					colorTex.wrapMode = TextureWrapMode.Clamp;
					colorTex.enableRandomWrite = true;
					colorTex.hideFlags = HideFlags.DontSave;
					colorTex.Create();
					Debug.Log($"Create color tex : size={colorTex.Size()}");
				}

				pip.TargetCam = targetCam;
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
			while (isActiveAndEnabled) {
				yield return null;

				validator.Validate();
				if (!isActiveAndEnabled) yield break;

				pip.Validate();

				UpdateOccupyRegions();

				if (wesync != null) {
					var subspace = wesync.CurrSubspace;
					if (subspace != default) {
						occupy.Update(subspace);
						occupy.Visualize(colorTex);
					}
				}

				idTexCpu.Source = occupy.IdTex;
				foreach (var __ in idTexCpu)
					yield return null;

				UpdateRegisterIdMap();
			}
		}

		private void UpdateOccupyRegions() {
			foreach (var r in regions) {
				tmpRegIdSet.Remove(r.id);
			}
			foreach (var regId in tmpRegIdSet) {
				if (regToOccIdMap.TryGetValue(regId, out var occId)) {
					occId.Dispose();
					regToOccIdMap.Remove(regId);
				}
			}
			tmpRegIdSet.Clear();

			foreach (var r in regions) {
				tmpRegIdSet.Add(r.id);
				if (!regToOccIdMap.ContainsKey(r.id)) {
					var occId = occIdStorage.GetToken();
					regToOccIdMap[r.id] = occId;
				}
			}

			occupy.Clear();
			foreach (var r in regions)
				occupy.Add(regToOccIdMap[r.id], r.position, r.birthTime);
		}
		private void UpdateRegisterIdMap() {
			occToRegIdMap.Clear();
			foreach (var outerId in regToOccIdMap.Keys)
				occToRegIdMap[regToOccIdMap[outerId]] = outerId;
		}
		#endregion

		#region interface
		public Tuner CurrTuner {
			get {
				validator.Validate();
				if (occupy != null) tuner.occupy = occupy.CurrTuner;
				if (pip != null) tuner.pip = pip.CurrTuner;
				return tuner.DeepCopy();
			}
			set {
				tuner = value;
				if (occupy != null) occupy.CurrTuner = tuner.occupy;
				if (pip != null) pip.CurrTuner = tuner.pip;
				validator.Invalidate();
			}
		}
		public IList<Occupy.Region> Regions {
			get => regions;
		}

		#region ISampler
		public override SampleResultCode TrySample(Vector2 uv, out int regId) {
			regId = default;

			if (!IsActive) return SampleResultCode.E_SystemNotActive;

			var occId = Mathf.RoundToInt(idTexCpu[uv].x);
			if (occId == -1) return SampleResultCode.E_InitialRegion;

			var res = occToRegIdMap.TryGetValue(occId, out regId);
			if (!res) return SampleResultCode.E_CannnotConvertID;
			
			return SampleResultCode.S_RegionFound;
		}
		#endregion

		public void ListenCamera(GameObject go) {
			targetCam = go.GetComponent<Camera>();
			validator.Invalidate();
		}
		public void Listen(WeSyncExhibitor wesync) {
			this.wesync = wesync;
			validator.Invalidate();
		}
		#endregion

		#region definition
		[System.Serializable]
		public class Tuner {
			public PIPTexture.Tuner pip = new PIPTexture.Tuner();
			public Occupy.Tuner occupy = new Occupy.Tuner();
		}
		#endregion
	}
}
