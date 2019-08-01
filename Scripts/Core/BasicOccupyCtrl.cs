using nobnak.Gist;
using nobnak.Gist.ObjectExt;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SphereOfInfluenceSys.Core {

	public class BasicOccupyCtrl : MonoBehaviour {

		[SerializeField]
		protected OccupyModel model;
		[SerializeField]
		protected Camera cam;
		[SerializeField]
		protected Settings settings = new Settings();

		protected int currid = 0;
		protected Validator validator = new Validator();
		protected OccupyModel.CameraSettings camsettings = default;

		#region unity
		protected virtual void OnEnable() {
			validator.Reset();
			validator.SetCheckers(() => camsettings.Equals((OccupyModel.CameraSettings)cam));
			validator.Validation += () => {
				camsettings = cam;
				model.SetScreenSize = Lod(camsettings.screenSize, settings.lod);
				model.CurrentSettings = settings.modelSettings;
			};
		}
		protected virtual void OnValidate() {
			validator.Invalidate();
		}
		protected virtual void Update() {
			validator.Validate();

			if (settings.debugInput) {
				if (Input.GetMouseButtonDown(0)) {
					var uv = cam.ScreenToViewportPoint(Input.mousePosition);
					currid = (++currid) % settings.modelSettings.clusters;
					model.Add(new Occupy.PointInfo(currid, uv));
				}
				if (Input.GetMouseButtonDown(1)) {
					var uv = cam.ScreenToViewportPoint(Input.mousePosition);
					int iselected;
					model.Sample(uv, out iselected);
					if (iselected >= 0)
						Debug.LogFormat("Scanner ID={0} at position", iselected);
					else
						Debug.LogFormat("No scanner ID applied");
				}
			}
		}
		#endregion

		#region interface
		public virtual Settings CurrentSettings {
			get {
				return settings.DeepCopy();
			}
			set {
				if (!settings.Equals(value)) {
					settings = value.DeepCopy();
					validator.Invalidate();
				}
			}
		}
		#endregion

		#region member
		protected virtual Vector2Int Lod(Vector2Int size, int lod) {
			lod = (lod >= 0 ? lod : 0);
			return new Vector2Int(size.x >> lod, size.y >> lod);
		}
		#endregion

		#region classes
		[System.Serializable]
		public class Settings {
			public OccupyModel.Settings modelSettings;
			[Range(0, 4)]
			public int lod = 2;
			public bool debugInput = false;
		}
		#endregion
	}
}
