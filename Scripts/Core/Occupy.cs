using nobnak.Gist.Extensions.GPUExt;
using nobnak.Gist.GPUBuffer;
using nobnak.Gist.ObjectExt;
using nobnak.Gist.Scoped;
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
		public static readonly int P_BirthTime = Shader.PropertyToID("_BirthTime");

		public static readonly int PROP_POSITIONS_LENGTH = Shader.PropertyToID("_Positions_Length");
		public static readonly int PROP_POSITIONS = Shader.PropertyToID("_Positions");
		public static readonly int PROP_POSITION_IDS = Shader.PropertyToID("_PositionIDs");

		public readonly int ID_CalcOfSoI;
		public readonly int ID_ColorOfId;

		public Vector2Int ScreenSize { get; protected set; }

		protected ComputeShader cs;
		protected GPUList<Vector2> positions = new GPUList<Vector2>();
		protected GPUList<float> lifes = new GPUList<float>();
		protected GPUList<int> positionIds = new GPUList<int>();

		protected float lifeLimit;
		protected Vector2 edgeDuration = new Vector2(0.5f, 0.1f);

		protected Rect localFieldRect;
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
			positions.Dispose();
			positionIds.Dispose();
			lifes.Dispose();
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
		public void SetFieldSize(Vector2 localSize, Vector2 localOffset, Vector2 worldSize) {
			this.localFieldRect = new Rect(localOffset, localSize);
			this.worldFieldSize = worldSize;
			uv2FieldPos = Matrix4x4.zero;
			uv2FieldPos[0] = localFieldRect.width;	uv2FieldPos[12] = localFieldRect.x;
			uv2FieldPos[5] = localFieldRect.height;	uv2FieldPos[13] = localFieldRect.y;
			uv2FieldPos[2] = worldSize.x;
			uv2FieldPos[7] = worldSize.y;
		}
		public int Add(int id, Vector2 normPos, float life = -1f) {
			var count = positions.Count;
			positionIds.Add(id);
			positions.Add(normPos);
			lifes.Add(life >= 0f ? life : CurrentTime);
			return count;
		}

		public int Add(PointInfo pi) {
			return Add(pi.id, pi.position, pi.life);
		}
		public void Clear() {
			positionIds.Clear();
			positions.Clear();
			lifes.Clear();
		}
		public void Update(Vector2Int screenSize) {
			if (screenSize.x < 4 || screenSize.y < 4) {
				Debug.LogWarning($"Size too small : {screenSize}");
			}
			ScreenSize = screenSize;

			SetCommonParams();

			CheckIdTex(screenSize);
			cs.SetTexture(ID_CalcOfSoI, P_IdTex, IdTex);

			cs.SetInt(PROP_POSITIONS_LENGTH, positions.Count);
			cs.SetBuffer(ID_CalcOfSoI, PROP_POSITIONS, this.positions);
			cs.SetBuffer(ID_CalcOfSoI, PROP_POSITION_IDS, this.positionIds);

			var t = CurrentTime;
			cs.SetVector(PROP_LIFE_LIMIT, 
				new Vector4(edgeDuration.x, edgeDuration.y, t, 1f / lifeLimit));
			cs.SetBuffer(ID_CalcOfSoI, P_BirthTime, this.lifes);

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
		public static float CurrentTime {
			get { return Time.timeSinceLevelLoad; }
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
		[StructLayout(LayoutKind.Sequential)]
		public class PointInfo {
			public readonly int id;
			public readonly Vector2 position;
			public readonly float life;

			public PointInfo(int id, Vector2 pos, float life) {
				this.id = id;
				this.position = pos;
				this.life = life;
			}
			public PointInfo(int id, Vector2 pos) : this(id, pos, CurrentTime) { }

			#region interface

			#region object
			public override string ToString() {
				return $"{GetType().Name} : id={id}, position={position}, life={life}";
			}
			#endregion

			#endregion
		}
		#endregion
	}
}
