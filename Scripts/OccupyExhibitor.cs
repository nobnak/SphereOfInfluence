using FlowerArrangementSystem;
using ModelDrivenGUISystem;
using ModelDrivenGUISystem.Factory;
using ModelDrivenGUISystem.ValueWrapper;
using ModelDrivenGUISystem.View;
using nobnak.Gist;
using nobnak.Gist.Exhibitor;
using nobnak.Gist.IMGUI.Scope;
using nobnak.Gist.Scoped;
using SphereOfInfluenceSys.Core;
using UnityEngine;

namespace SphereOfInfluenceSys {

	public class OccupyExhibitor : AbstractExhibitor {

		[SerializeField]
		protected Refs refs;
		[SerializeField]
		protected Data data = new Data();

		protected Validator validator = new Validator();
		protected BaseView view;

		#region unity
		private void Awake() {
			validator.Reset();
			validator.Validation += () => {
				refs.toggle.Activity = data.visualizeOccupyField;
				refs.cont.CurrentSettings = data.controllerSettings;
				refs.recom.upperLimitScale = Mathf.Clamp(data.recomUpperLimitScale, 1f, 2f);
			};
		}
		private void Update() {
			validator.Validate();
		}
		private void OnValidate() {
			validator.Invalidate();
		}
		#endregion
		#region member
		protected BaseView CurrentView {
			get {
				if (view == null) {
					var viewFactory = new SimpleViewFactory();
					view = ClassConfigurator.GenerateClassView(new BaseValue<object>(data), viewFactory);
				}
				return view;
			}
		}
		protected void ClearView() {
			if (view != null) {
				view.Dispose();
				view = null;
			}
		}
		#endregion
		#region AbstractExhibitorGUI
		public override void DeserializeFromJson(string json) {
			JsonUtility.FromJsonOverwrite(json, data);
			ClearView();
			validator.Validate(true);
		}
		public override void Draw() {
			using (new GUIChangedScope(() => Invalidate())) {
				CurrentView.Draw();
			}
		}
		public override void Invalidate() {
			validator.Invalidate();
		}
		public override object RawData() {
			return data;
		}
		public override string SerializeToJson() {
			validator.Validate();
			return JsonUtility.ToJson(data, true);
		}
		#endregion

		#region classes
		[System.Serializable]
		public class Refs {
			public OccupyModel model;
			public OccupyController cont;
			public OccupyFlowerRecom recom;
			public ObjectModal toggle;
		}
		[System.Serializable]
		public class Data {
			[Header("View")]
			public bool visualizeOccupyField;
			[Header("Controller")]
			public BasicOccupyCtrl.Settings controllerSettings = new BasicOccupyCtrl.Settings();
			[Header("Recom")]
			[Range(1f, 2f)]
			public float recomUpperLimitScale = 2f;
		}
		#endregion
	}
}
