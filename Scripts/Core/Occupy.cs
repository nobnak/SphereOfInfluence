using nobnak.Gist.Extensions.GPUExt;
using nobnak.Gist.GPUBuffer;
using nobnak.Gist.ObjectExt;
using nobnak.Gist.Scoped;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
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

		public static readonly int PROP_R_IDS = Shader.PropertyToID("_RIds");
		public static readonly int PROP_COLORS = Shader.PropertyToID("_Colors");
		public static readonly int PROP_COLOR_PARAMS = Shader.PropertyToID("_ColorParams");

		public static readonly int PROP_IDS = Shader.PropertyToID("_Ids");
		public static readonly int PROP_TEXEL_SIZE = Shader.PropertyToID("_TexelSize");
		public static readonly int PROP_METRICS = Shader.PropertyToID("_Metrics");

		public static readonly int PROP_LIFE_LIMIT = Shader.PropertyToID("_Life_Limit");
		public static readonly int PROP_LIFES = Shader.PropertyToID("_Lifes");

		public static readonly int PROP_POSITIONS_LENGTH = Shader.PropertyToID("_Positions_Length");
		public static readonly int PROP_POSITIONS = Shader.PropertyToID("_Positions");
		public static readonly int PROP_POSITION_IDS = Shader.PropertyToID("_PositionIDs");

		public readonly int ID_CalcOfSoI;
		public readonly int ID_ColorOfId;

		public int Width { get; protected set; }
		public int Height { get; protected set; }

		protected ComputeShader cs;
		protected GPUList<Vector2> positions = new GPUList<Vector2>();
		protected GPUList<float> lifes = new GPUList<float>();
		protected GPUList<int> positionIds = new GPUList<int>();

		protected float lifeLimit;
		protected Vector4 texelSize;
		protected Vector3Int dispatchSize;

		public Occupy(int width, int height) {
			SetSize(width, height);

			cs = Resources.Load<ComputeShader>(CS_FILE);

			ID_CalcOfSoI = cs.FindKernel(K_CalcOfSoI);
			ID_ColorOfId = cs.FindKernel(K_ColorOfId);
		}

		#region interface
		public RenderTexture Ids { get; protected set; }
		public float LifeLimit {
			get { return lifeLimit; }
			set {
				if (value > 0f)
					lifeLimit = value;
			}
		}
		public bool SetSize(int width, int height) {
			if (width <= 0 || height <= 0) {
				Debug.LogWarningFormat("Size must be larger than 0 : {0}x{1}", width, height);
				return false;
			}
			var modified = Width != width || Height != height;

			if (modified) {
				ReleaseDepentsOnSize();

				Width = width;
				Height = height;

				dispatchSize = new Vector3Int(
					width.DispatchSize(NUMTHREADS2D), height.DispatchSize(NUMTHREADS2D), 1);
				texelSize = new Vector4(1f / width, 1f / height, width, height);
				
				Ids = new RenderTexture(width, height, 0, FORMAT_TEX_IDS);
				Ids.wrapMode = TextureWrapMode.Clamp;
				Ids.filterMode = FilterMode.Point;
				Ids.enableRandomWrite = true;
				Ids.Create();

			}

			return modified;
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
		public void Update(Matrix4x4 metrics) {
			cs.SetVector(PROP_TEXEL_SIZE, texelSize);
			cs.SetMatrix(PROP_METRICS, metrics);

			cs.SetTexture(ID_CalcOfSoI, PROP_IDS, Ids);

			cs.SetInt(PROP_POSITIONS_LENGTH, positions.Count);
			cs.SetBuffer(ID_CalcOfSoI, PROP_POSITIONS, this.positions);
			cs.SetBuffer(ID_CalcOfSoI, PROP_POSITION_IDS, this.positionIds);

			var t = CurrentTime;
			cs.SetVector(PROP_LIFE_LIMIT, new Vector4(t - lifeLimit, t, lifeLimit, 1f / lifeLimit));
			cs.SetBuffer(ID_CalcOfSoI, PROP_LIFES, this.lifes);

			cs.Dispatch(ID_CalcOfSoI, dispatchSize.x, dispatchSize.y, dispatchSize.z);
		}
		public void Visualize(RenderTexture colors, int clusters = 10, float offset = 0.5f) {
			cs.SetVector(PROP_COLOR_PARAMS, new Vector4(1f / clusters, offset, 0, 0));

			cs.SetTexture(ID_ColorOfId, PROP_R_IDS, Ids);
			cs.SetTexture(ID_ColorOfId, PROP_COLORS, colors);

			cs.Dispatch(ID_ColorOfId, dispatchSize.x, dispatchSize.y, dispatchSize.z);
		}
		#endregion
		#region static
		public static float CurrentTime {
			get { return Time.timeSinceLevelLoad; }
		}
		#endregion

		#region IDisposable
		public void Dispose() {
			ReleaseDepentsOnSize();
			positions.Dispose();
			positionIds.Dispose();
			lifes.Dispose();
		}

		private void ReleaseDepentsOnSize() {
			Ids.DestroySelf();
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
		}
		#endregion
	}
}
