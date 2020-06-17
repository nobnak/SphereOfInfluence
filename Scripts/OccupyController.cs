using Common;
using Hunting;
using nobnak.Gist;
using nobnak.Gist.Extensions.ComponentExt;
using nobnak.Gist.Layer2;
using SphereOfInfluenceSys.Core;
using System.Collections.Generic;
using UnityEngine;

namespace SphereOfInfluenceSys {

	[ExecuteInEditMode]
	public class OccupyController : BasicOccupyCtrl {

		[SerializeField]
		protected Controller graffitiController;

		public Validator Validator { get; protected set; } = new Validator();

		protected Dictionary<int, int> scannerToTextureID = new Dictionary<int, int>();

		#region unity
		protected override void OnEnable() {
			base.OnEnable();

			cam = cam ?? Camera.main;

			foreach (var cf in this.Children<CameraFollower>())
				cf.Source = cam;
		}
		protected override void Update() {
			base.Update();
		}
		#endregion

		#region interface
		public void ListenOnAddTexture(SketchTexture sketchTexture) {
			if (sketchTexture == null)
				return;

			var scannerId = sketchTexture.scanner_id;
			var spawn = graffitiController.SpawnableFieldAt(scannerId);
			if (spawn != null) {
				var uv = WorldToUvPos(spawn.transform.position);
				Replace(uv, sketchTexture);
			}
		}
		public Vector2 WorldToUvPos(Vector3 worldPos) {
			return cam.WorldToViewportPoint(worldPos);
		}
		public void Sample(Vector2 uvPos, out int id) {
			model.Sample(uvPos, out id);
		}
		public SketchTexture SketchTextureByScannerID(int id) {
			int texid;
			if (!TryGetTextureIDByScannerID(id, out texid)) {
				if (id >= 0)
					Debug.LogFormat("Sketch tex not found for scanner id={0}", id);
				return null;
			}

			SketchTexture tex;
			if (SketchTextureStorage.Instance.Find(texid, out tex)) { 
				return tex;
			} else {
				Debug.Log($"Id not found : id={texid}");
				Remove(id);
				return null;
			}
		}
		#endregion

		#region member

		#region PointInfo List
		private void Remove(int scannerID) {
			scannerToTextureID.Remove(scannerID);
			model.RemoveByID(scannerID);
		}
		private void Add(Vector2 uv, SketchTexture tex) {
			var pi = new Occupy.PointInfo(tex.scanner_id, uv);
			scannerToTextureID[tex.scanner_id] = tex.id;
			model.Add(pi);
			Debug.LogFormat("Add point in occupy : {0}", pi);
		}
		private bool TryGetTextureIDByScannerID(int scannerId, out int texid) {
			return scannerToTextureID.TryGetValue(scannerId, out texid);
		}
		private void Replace(Vector2 uv, SketchTexture tex) {
			Remove(tex.scanner_id);
			Add(uv, tex);
		}
		#endregion
		#endregion
	}
}
