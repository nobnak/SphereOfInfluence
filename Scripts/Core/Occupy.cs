using nobnak.Gist.Extensions.GPUExt;
using nobnak.Gist.Extensions.Texture2DExt;
using nobnak.Gist.GPUBuffer;
using nobnak.Gist.ObjectExt;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using WeSyncSys.Extensions.TimeExt;

namespace SphereOfInfluenceSys.Core {

	public class Occupy : System.IDisposable {
		public const int NUMTHREADS2D = 8;
		public const string CS_FILE = "Occupy";
		public const string K_CalcOfSoI = "CalcSoI";
		public const string K_ColorOfId = "ColorOfID";

		public const GraphicsFormat FORMAT_TEX_IDS = GraphicsFormat.R32G32B32A32_SFloat;

		public static readonly int P_IdTex = Shader.PropertyToID("_IdTex");
		public static readonly int P_IdTexR = Shader.PropertyToID("_IdTexR");
		public static readonly int P_ColorTex = Shader.PropertyToID("_ColorTex");

		public static readonly int PROP_COLOR_PARAMS = Shader.PropertyToID("_ColorParams");
		public static readonly int P_ScreenTexelSize = Shader.PropertyToID("_ScreenTexelSize");
		public static readonly int P_UV2FieldPos = Shader.PropertyToID("_UV2FieldPos");

		public static readonly int PROP_LIFE_LIMIT = Shader.PropertyToID("_Life_Limit");

		public static readonly int P_Regions_Length = Shader.PropertyToID("_Regions_Length");
		public static readonly int P_Regions = Shader.PropertyToID("_Regions");

		public readonly int ID_CalcOfSoI;
		public readonly int ID_ColorOfId;

		public Vector2Int ScreenSize { get; protected set; }

		protected ComputeShader cs;
		protected GPUList<Region> regions = new GPUList<Region>();

		protected Tuner tuner = new Tuner();
		protected Matrix4x4 uv2FieldPos;

		public Occupy() {
			cs = Resources.Load<ComputeShader>(CS_FILE);

			ID_CalcOfSoI = cs.FindKernel(K_CalcOfSoI);
			ID_ColorOfId = cs.FindKernel(K_ColorOfId);
		}

		#region interface

		#region IDisposable
		public void Dispose() {
			IdTex.DestroySelf();
			regions.Dispose();
		}
		#endregion

		public RenderTexture IdTex { get; protected set; }

		public Tuner CurrTuner {
			get {
				return tuner.DeepCopy();
			}
			set {
				if (value.Valid())
					tuner = value.DeepCopy();
			}
		}
		[System.Obsolete]
		public float LifeLimit {
			get { return tuner.lifeLimit; }
			set {
				if (value > 0f)
					tuner.lifeLimit = value;
			}
		}
		[System.Obsolete]
		public Vector2 EdgeDuration {
			get { return tuner.edgeDuration; }
			set {
				tuner.edgeDuration.x = Mathf.Clamp01(value.x);
				tuner.edgeDuration.y = Mathf.Clamp01(1f - value.y);
			}
		}
		[System.Obsolete]
		public void SetFieldSize(Vector2 size) {
			SetFieldSize(size, Vector2.zero, size);
		}
		[System.Obsolete]
		public void SetFieldSize(Vector2 viewportSize, Vector2 viewportOffset, Vector2 global) {
			var local = new Rect(viewportOffset, viewportSize);
			SetFieldSize(local, global);
		}
		[System.Obsolete]
		public void SetFieldSize(Rect local, Vector2 global) {
			tuner.localField = local;
			tuner.globalField = global;
		}

		public int Add(int id, Vector2 normPos, float life = -1f) {
			var count = regions.Count;
			life = (life >= 0f ? life : TimeExtension.CurrRelativeSeconds);
			return Add(new Region(id, normPos, life));
		}

		public int Add(Region pi) {
			var count = regions.Count;
			regions.Add(pi);
			return count;
		}
		public void Clear() {
			regions.Clear();
		}
		public Occupy Update(RenderTexture colorTex, int clusters = 10, float offset = 0.5f) {
			var screenSize = colorTex.Size();
			var t = TimeExtension.CurrRelativeSeconds;

			if (screenSize.x < 4 || screenSize.y < 4) {
				Debug.LogWarning($"Size too small : {screenSize}");
			}
			ScreenSize = screenSize;

			SetCommonParams();
			CheckIdTex(screenSize);

			var dispatchSize = GetDispatchSize(screenSize);
			cs.SetTexture(ID_CalcOfSoI, P_IdTex, IdTex);
			cs.SetInt(P_Regions_Length, regions.Count);
			cs.SetBuffer(ID_CalcOfSoI, P_Regions, regions);
			cs.SetVector(PROP_LIFE_LIMIT,
				new Vector4(tuner.edgeDuration.x, tuner.edgeDuration.y, t, 1f / tuner.lifeLimit));
			cs.Dispatch(ID_CalcOfSoI, dispatchSize.x, dispatchSize.y, dispatchSize.z);

			cs.SetVector(PROP_COLOR_PARAMS, new Vector4(1f / clusters, offset, 0, 0));
			cs.SetTexture(ID_ColorOfId, P_IdTexR, IdTex);
			cs.SetTexture(ID_ColorOfId, P_ColorTex, colorTex);
			cs.Dispatch(ID_ColorOfId, dispatchSize.x, dispatchSize.y, dispatchSize.z);

			return this;
		}
		[System.Obsolete]
		public void Update(Vector2Int screenSize) {
			if (screenSize.x < 4 || screenSize.y < 4) {
				Debug.LogWarning($"Size too small : {screenSize}");
			}
			ScreenSize = screenSize;

			SetCommonParams();

			CheckIdTex(screenSize);
			cs.SetTexture(ID_CalcOfSoI, P_IdTex, IdTex);

			cs.SetInt(P_Regions_Length, regions.Count);
			cs.SetBuffer(ID_CalcOfSoI, P_Regions, regions);

			var t = TimeExtension.CurrRelativeSeconds;
			cs.SetVector(PROP_LIFE_LIMIT, 
				new Vector4(tuner.edgeDuration.x, tuner.edgeDuration.y, t, 1f / tuner.lifeLimit));

			var dispatchSize = GetDispatchSize(screenSize);
			cs.Dispatch(ID_CalcOfSoI, dispatchSize.x, dispatchSize.y, dispatchSize.z);
		}
		[System.Obsolete]
		public void Visualize(RenderTexture colorTex, int clusters = 10, float offset = 0.5f) {
			SetCommonParams();

			cs.SetVector(PROP_COLOR_PARAMS, new Vector4(1f / clusters, offset, 0, 0));

			cs.SetTexture(ID_ColorOfId, P_IdTexR, IdTex);
			cs.SetTexture(ID_ColorOfId, P_ColorTex, colorTex);

			var dispatchSize = GetDispatchSize(ScreenSize);
			cs.Dispatch(ID_ColorOfId, dispatchSize.x, dispatchSize.y, dispatchSize.z);
		}
		#endregion

		#region member
		protected void SetCommonParams() {
			uv2FieldPos = GenerateFieldMatrix(tuner.localField, tuner.globalField);
			cs.SetMatrix(P_UV2FieldPos, uv2FieldPos);

			var screenTexelSize = GetScreenTexelSize(ScreenSize);
			cs.SetVector(P_ScreenTexelSize, screenTexelSize);
		}
		private void CheckIdTex(Vector2Int screenSize) {
			if (IdTex == null || IdTex.width != screenSize.x || IdTex.height != screenSize.y) {
				IdTex.DestroySelf();
				IdTex = new RenderTexture(screenSize.x, screenSize.y, 0, FORMAT_TEX_IDS);
				IdTex.wrapMode = TextureWrapMode.Clamp;
				IdTex.filterMode = FilterMode.Point;
				IdTex.enableRandomWrite = true;
				IdTex.Create();
			}
		}
		#endregion

		#region static
		public static Matrix4x4 GenerateFieldMatrix(Rect local, Vector2 global) {
			var uv2FieldPos = Matrix4x4.zero;
			uv2FieldPos[0] = local.width; uv2FieldPos[12] = local.x;
			uv2FieldPos[5] = local.height; uv2FieldPos[13] = local.y;
			uv2FieldPos[2] = global.x;
			uv2FieldPos[7] = global.y;
			return uv2FieldPos;
		}
		public static Vector3Int GetDispatchSize(Vector2Int screenSize) {
			return new Vector3Int(
				screenSize.x.DispatchSize(NUMTHREADS2D), screenSize.y.DispatchSize(NUMTHREADS2D), 1);
		}
		public static Vector4 GetScreenTexelSize(Vector2Int screenSize) {
			return new Vector4(
				1f / screenSize.x,
				1f / screenSize.y,
				screenSize.x,
				screenSize.y);
		}
		#endregion


		#region classes
		[System.Serializable]
		[StructLayout(LayoutKind.Sequential)]
		public struct Region {
			public readonly int id;
			public readonly float birthTime;
			public readonly Vector2 position;

			public Region(int id, Vector2 pos, float birthTime) {
				this.id = id;
				this.position = pos;
				this.birthTime = birthTime;
			}
			public Region(int id, Vector2 pos) 
				: this(id, pos, TimeExtension.CurrRelativeSeconds) { }

			#region interface

			#region object
			public override string ToString() {
				return $"{GetType().Name} : "
					+ $"id={id}, "
					+ $"position={position}, "
					+ $"birth_time={birthTime}";
			}
			#endregion

			#endregion
		}
		[System.Serializable]
		public class Tuner {
			public Rect localField = new Rect(0f, 0f, 1f, 1f);
			public Vector2 globalField = new Vector2(1f, 1f);

			public float lifeLimit = 10f;
			public Vector2 edgeDuration = new Vector2(0.5f, 0.9f);

			public bool Valid() {
				return edgeDuration.x >= 0f
					&& edgeDuration.y > edgeDuration.x
					&& edgeDuration.y <= 1f
					&& lifeLimit > 0f;
			}
		}
		#endregion
	}
}
