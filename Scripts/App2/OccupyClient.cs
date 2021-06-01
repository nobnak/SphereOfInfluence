using nobnak.Gist;
using nobnak.Gist.Cameras;
using nobnak.Gist.Extensions.ScreenExt;
using SphereOfInfluenceSys.App2.Structures;
using SphereOfInfluenceSys.Core;
using UnityEngine;
using WeSyncSys;
using nobnak.Gist.Extensions.Texture2DExt;
using nobnak.Gist.ObjectExt;
using UnityEngine.Rendering;

namespace SphereOfInfluenceSys.App2 {

	public class OccupyClient : MonoBehaviour {

		public const CameraEvent EVT_CAMCBUF = CameraEvent.AfterEverything;

		[SerializeField]
		protected Camera targetCam;
		[SerializeField]
		protected WeSyncExhibitor wesync;
		[SerializeField]
		protected Tuner tuner = new Tuner();

		protected CameraData cameraData = default;
		protected SharedData shared;

		protected Validator validator = new Validator();
		protected Occupy occupy;
		protected RenderTexture colorTex;
		protected CommandBuffer cbuf;

		#region unity
		private void OnEnable() {
			occupy = new Occupy();

			validator.Reset();
			validator.SetCheckers(() => !cameraData.Equals(targetCam));
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

				occupy.CurrTuner = tuner.occupy;
				occupy.Update(colorTex);

				targetCam.RemoveCommandBuffer(EVT_CAMCBUF, cbuf);
				cbuf.Clear();
				cbuf.SetViewport(new Rect(Vector2.zero, 0.2f * (Vector2)size));
				cbuf.Blit(colorTex, BuiltinRenderTextureType.CurrentActive);
				targetCam.AddCommandBuffer(EVT_CAMCBUF, cbuf);
			};

			cbuf = new CommandBuffer();

		}
		private void OnDisable() {
			if (occupy != null) {
				occupy.Dispose();
				occupy = null;
			}
			if (colorTex != null) {
				colorTex.DestroySelf();
				colorTex = null;
			}
			if (cbuf != null) {
				targetCam?.RemoveCommandBuffer(EVT_CAMCBUF, cbuf);
				cbuf.Destroy();
			}
		}
		private void OnValidate() {
			validator.Invalidate();
		}
		private void Update() {
			validator.Validate();
		}
		#endregion

		#region interface
		public void Listen(SharedData shared) {
			Debug.Log($"{GetType().Name} : Receive shared data. {shared}");
			this.shared = shared;
			occupy.Clear();
			foreach (var r in shared.regions)
				occupy.Add(r);
			validator.Invalidate();
		}
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
			public Occupy.Tuner occupy = new Occupy.Tuner();
		}
		#endregion
	}
}
