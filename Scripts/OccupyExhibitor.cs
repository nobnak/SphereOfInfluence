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
		protected Data vm = new Data();

		protected Validator validator = new Validator();
		protected BaseView view;

		#region unity
		private void Awake() {
			validator.Reset();
			validator.Validation += ()=>ReflectChangeOf(MVVMComponent.Model);
		}
		private void Update() {
			validator.Validate();
		}
		private void OnValidate() {
			ReflectChangeOf(MVVMComponent.ViewModel);
		}
		#endregion
		

		#region interface

		#region AbstractExhibitor
		public override void Draw() {
			validator.Validate();
			using (new GUIChangedScope(() => validator.Invalidate())) {
				CurrentView.Draw();
			}
		}
		public override string SerializeToJson() {
			validator.Validate();
			return JsonUtility.ToJson(vm, true);
		}
		public override void DeserializeFromJson(string json) {
			JsonUtility.FromJsonOverwrite(json, vm);
			ReflectChangeOf(MVVMComponent.ViewModel);
		}
		public override object RawData() {
			return vm;
		}
		public override void ApplyViewModelToModel() {
			refs.toggle.Activity = vm.visualizeOccupyField;
			refs.cont.CurrentSettings = vm.controllerSettings;
			refs.recom.upperLimitScale = Mathf.Clamp(vm.recomUpperLimitScale, 1f, 2f);
		}
		public override void ResetViewModelFromModel() {
			vm.visualizeOccupyField = refs.toggle.Activity;
			vm.controllerSettings = refs.cont.CurrentSettings;
			vm.recomUpperLimitScale = refs.recom.upperLimitScale;
		}
		public override void ResetView() {
			if (view != null) {
				view.Dispose();
				view = null;
			}
		}
		#endregion

		#endregion

		#region member
		protected BaseView CurrentView {
			get {
				if (view == null) {
					var viewFactory = new SimpleViewFactory();
					view = ClassConfigurator.GenerateClassView(new BaseValue<object>(vm), viewFactory);
				}
				return view;
			}
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
