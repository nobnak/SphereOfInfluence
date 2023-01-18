using ModelDrivenGUISystem;
using ModelDrivenGUISystem.Factory;
using ModelDrivenGUISystem.ValueWrapper;
using ModelDrivenGUISystem.View;
using nobnak.Gist;
using nobnak.Gist.Exhibitor;
using nobnak.Gist.Extensions.Array;
using nobnak.Gist.Extensions.ScreenExt;
using SphereOfInfluenceSys.Core;
using SphereOfInfluenceSys.Core.Interfaces;
using System.Collections.Generic;
using UnityEngine;
using WeSyncSys;

namespace SphereOfInfluenceSys.App2 {

	[ExecuteAlways]
	public class LocalOccupyUI : AbstractExhibitor {

		[SerializeField]
		protected Linker linker = new Linker();
		[SerializeField]
		protected Tuner tuner = new Tuner();
		[SerializeField]
		protected DebugData data = new DebugData();

		protected BaseView view;
		protected Validator validator = new Validator();

		#region unity
		private void OnEnable() {
			validator.Reset();
			validator.Validation += () => {
				ReflectChangeOf(MVVMComponent.ViewModel);
			};
		}
		private void OnValidate() {
			validator.Invalidate();
		}
		private void Update() {
			var tlimit = Occupy.Region.Now - tuner.occupyTuner.occupy.occupy.lifeLimit - 1f;
			for (var i = 0; i < data.regions.Count;) {
				var r = data.regions[i];
				if (r.birthTime < tlimit) {
					data.regions.RemoveAt(i);
					validator.Invalidate();
				} else {
					i++;
				}
			}

			if (tuner.debugTuner.enabled) {
				if (Input.GetMouseButtonDown(0)) {
					var uv = Input.mousePosition.UV();
					var pos = linker.wesync.Space.uv2wpos(uv);
					var r = new Occupy.Region(Time.frameCount, pos);
					data.regions.Add(r);
					validator.Invalidate();
					Debug.Log($"Add region : {r}");
				}
				if (Input.GetMouseButtonDown(1)) {
					var uv = Input.mousePosition.UV();
					var occ = linker.occupy;
					if (occ != null) {
						var res = occ.TrySample(uv, out var regId);
						Debug.Log($"Sample : id={regId}, uv={uv}, res={res}");
					}
				}
			}

			validator.Validate();
		}
		#endregion

		#region interface

		#region AbstractExhibitor
		public override void ApplyViewModelToModel() {
			if (linker.occupy != null) {
				linker.occupy.CurrTuner = tuner.occupyTuner;

				if (tuner.debugTuner.enabled) {
					var rs = linker.occupy.Regions;
					rs.Clear();
					rs.AddRange(data.regions);
				}
			}
		}
		public override void ResetViewModelFromModel() {
			if (linker.occupy != null) {
				tuner.occupyTuner = linker.occupy.CurrTuner;
			}
		}
		public override void ResetView() {
			if (view != null) {
				view.Dispose();
				view = null;
			}
		}

		public override void Draw() {
			if (view == null) {
				var f = new SimpleViewFactory();
				view = ClassConfigurator.GenerateClassView(new BaseValue<object>(tuner), f);
			}
			view.Draw();
		}

		public override void DeserializeFromJson(string json) {
			JsonUtility.FromJsonOverwrite(json, tuner);
			validator.Invalidate();
		}

		public override object RawData() {
			return tuner;
		}

		public override string SerializeToJson() {
			return JsonUtility.ToJson(tuner);
		}
		#endregion

		#endregion

		#region definition
		[System.Serializable]
		public class Linker {
			public LocalOccupyClient occupy;
			public WeSyncExhibitor wesync;
		}

		[System.Serializable]
		public class DebugData {
			public List<Occupy.Region> regions = new List<Occupy.Region>();
		}
		[System.Serializable]
		public class DebugTuner {
			public bool enabled;
		}

		[System.Serializable]
		public class Tuner {
			public DebugTuner debugTuner = new DebugTuner();
			public LocalOccupyClient.Tuner occupyTuner = new LocalOccupyClient.Tuner();
		}
		#endregion
	}
}
