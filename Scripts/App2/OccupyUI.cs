using ModelDrivenGUISystem;
using ModelDrivenGUISystem.Factory;
using ModelDrivenGUISystem.ValueWrapper;
using ModelDrivenGUISystem.View;
using nobnak.Gist;
using nobnak.Gist.Exhibitor;
using UnityEngine;

namespace SphereOfInfluenceSys.App2 {

	[ExecuteAlways]
	public class OccupyUI : AbstractExhibitor {

		[SerializeField]
		protected Linker linker = new Linker();
		[SerializeField]
		protected Tuner tuner = new Tuner();

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
			validator.Validate();
		}
		#endregion

		#region interface

		#region AbstractExhibitor
		public override void ApplyViewModelToModel() {
			if (linker.server != null) {
				var sa = (tuner.provider & Tuner.ProviderFlags.Server) != 0;
				linker.server.gameObject.SetActive(sa);
				linker.server.CurrTuner = tuner.server;
			}
			if (linker.client != null) {
				var ca = (tuner.provider & Tuner.ProviderFlags.Client) != 0;
				linker.client.gameObject.SetActive(ca);
				linker.client.CurrTuner = tuner.client;
			}
			if (linker.redis != null)
				linker.redis.CurrTuner = tuner.redis;


		}
		public override void ResetViewModelFromModel() {
			if (linker.server != null)
				tuner.server = linker.server.CurrTuner;
			if (linker.client != null)
				tuner.client = linker.client.CurrTuner;
			if (linker.redis != null)
				tuner.redis = linker.redis.CurrTuner;
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
			public OccupyServer server;
			public OccupyClient client;
			public RedisTransporter redis;
		}
		[System.Serializable]
		public class Tuner {
			public enum ProviderFlags {
				None = 0,
				Client = 1 << 0,
				Server = 1 << 1,
				Host = Client | Server,
			}

			public ProviderFlags provider = ProviderFlags.Host;

			public OccupyServer.Tuner server = new OccupyServer.Tuner();
			public OccupyClient.Tuner client = new OccupyClient.Tuner();
			public RedisTransporter.Tuner redis = new RedisTransporter.Tuner();
		}
		#endregion
	}
}
