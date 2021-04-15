using nobnak.Gist.Extensions.GPUExt;
using nobnak.Gist.GPUBuffer;
using nobnak.Gist.ObjectExt;
using nobnak.Gist.Scoped;
using SphereOfInfluenceSys.Extensions.TimeExt;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace SphereOfInfluenceSys.Core {

	public class Occupy : System.IDisposable {
		public const int NUMTHREADS2D = 8;
		public const string CS_FILE = "Occupy";
		public const string K_CalcOfSoI = "CalcSoI";
		public const string K_ColorOfId = "ColorOfID";

		public const GraphicsFormat FORMAT_TEX_WEIGHTS = GraphicsFormat.R32G32B32A32_SFloat;
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

		protected float lifeLimit;
		protected Vector2 edgeDuration = new Vector2(0.5f, 0.1f);

		protected Rect viewportRect;
		protected Vector2 worldFieldSize;
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
		public float LifeLimit {
			get { return lifeLimit; }
			set {
				if (value > 0f)
					lifeLimit = value;
			}
		}
		public Vector2 EdgeDuration {
			get { return edgeDuration; }
			set {
				edgeDuration.x = Mathf.Clamp01(value.x);
				edgeDuration.y = Mathf.Clamp01(1f - value.y);
			}
		}
		public void SetFieldSize(Vector2 size) {
			SetFieldSize(size, Vector2.zero, size);
		}
		public void SetFieldSize(Vector2 viewportSize, Vector2 viewportOffset, Vector2 worldSize) {
			this.viewportRect = new Rect(viewportOffset, viewportSize);
			this.worldFieldSize = worldSize;
			uv2FieldPos = Matrix4x4.zero;
			uv2FieldPos[0] = viewportRect.width;	uv2FieldPos[12] = viewportRect.x;
			uv2FieldPos[5] = viewportRect.height;	uv2FieldPos[13] = viewportRect.y;
			uv2FieldPos[2] = worldSize.x;
			uv2FieldPos[7] = worldSize.y;
		}
		public int Add(int id, Vector2 normPos, float life = -1f) {
			var count = regions.Count;
			life = (life >= 0f ? life : TimeExtension.RelativeSeconds);
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

			var t = TimeExtension.RelativeSeconds;
			cs.SetVector(PROP_LIFE_LIMIT, 
				new Vector4(edgeDuration.x, edgeDuration.y, t, 1f / lifeLimit));

			var dispatchSize = GetDispatchSize(screenSize);
			cs.Dispatch(ID_CalcOfSoI, dispatchSize.x, dispatchSize.y, dispatchSize.z);
		}
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
			public Region(int id, Vector2 pos) : this(id, pos, TimeExtension.RelativeSeconds) { }

			#region interface

			#region object
			public override string ToString() {
				return $"{GetType().Name} : id={id}, position={position}, birth_time={birthTime}";
			}
			#endregion

			#endregion
		}
		#endregion
	}
}
