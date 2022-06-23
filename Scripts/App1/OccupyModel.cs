using nobnak.Gist;
using nobnak.Gist.Events;
using nobnak.Gist.GPUBuffer;
using nobnak.Gist.ObjectExt;
using SphereOfInfluenceSys.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Events;
using WeSyncSys.Extensions;

namespace SphereOfInfluenceSys.App1 {

	[ExecuteAlways]
	public class OccupyModel : MonoBehaviour, IEnumerable<Occupy.Region> {

		public static readonly Vector2Int DEFAULT_SCREEN_SIZE = new Vector2Int(4, 4);

		public OccupyModelEvent UpdateOnOccupy = new OccupyModelEvent();
		public TextureEvent VisualizeIds = new TextureEvent();

		[SerializeField]
		protected Settings settings = new Settings();
		[SerializeField]
		protected List<Occupy.Region> points = new List<Occupy.Region>();

		public Occupy Occupy { get; protected set; }
		public AsyncCPUTexture<Vector4> CpuTexIds { get; protected set; } = new AsyncCPUTexture<Vector4>();
		public Validator Validator { get; protected set; } = new Validator();

		protected Coroutine cworker;
		protected Vector2Int screenSize = DEFAULT_SCREEN_SIZE;
		protected RenderTexture colorTex;

		#region unity
		private void Awake() {
			Validator.Validation += () => {
			};
			CpuTexIds.OnComplete += (v, o, result) => {
				UpdateOnOccupy.Invoke(this);
			};
		}

		private void OnEnable() {
			Occupy = new Occupy();
			cworker = StartCoroutine(Worker());
		}
		private void OnDisable() {
			if (Occupy != null) {
				Occupy.Dispose();
				Occupy = null;
			}
			if (cworker != null) {
				StopCoroutine(cworker);
				cworker = null;
			}
			colorTex.DestroySelf();
		}
		private void OnValidate() {
			Validator.Invalidate();
		}
		#endregion

		#region interface
		#region IEnumerable
		public IEnumerator<Occupy.Region> GetEnumerator() {
			foreach (var p in points)
				yield return p;
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
		#endregion
		
		public Settings CurrentSettings {
			get { return settings.DeepCopy(); }
			set {
				if (!settings.Equals(value)) {
					settings = value.DeepCopy();
					Validator.Invalidate();
				}
			}
		}
		public float CurrentMetrics {
			get { return settings.metricsScale; }
			set {
				if (settings.metricsScale != value) {
					Validator.Invalidate();
					settings.metricsScale = value;
				}
			}
		}
		public void Sample(Vector2 pos, out int id) {
			var v = CpuTexIds[pos][0];
			id = (int)v;
		}
		public void Add(Occupy.Region pi) {
			Validator.Invalidate();
			points.Add(pi);
		}
		public void RemoveByID(int id) {
			if (points.RemoveAll(v => v.id == id) >= 0)
				Validator.Invalidate();
		}
		public Vector2Int SetScreenSize {
			set {
				if (screenSize != value) {
					screenSize = value;
					Validator.Invalidate();

					if (screenSize.x < DEFAULT_SCREEN_SIZE.x || screenSize.y < DEFAULT_SCREEN_SIZE.y)
						Debug.LogWarningFormat("{0} : Invalid screen size {1}", GetType().Name, value);
				}
			}
		}
		#endregion

		#region private
		IEnumerator Worker() {
			while (true) {
				ClearUnusedPoints();
				UpdateOccupy();
				Visualize();

				CpuTexIds.Source = Occupy.IdTex;
				yield return CpuTexIds.StartCoroutine();
			}
		}

		private void Visualize() {
			if (colorTex == null || colorTex.width != screenSize.x || colorTex.height != screenSize.y) {
				colorTex.DestroySelf();
				colorTex = new RenderTexture(screenSize.x, screenSize.y, 0,
					RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
				colorTex.enableRandomWrite = true;
				colorTex.wrapMode = TextureWrapMode.Clamp;
				colorTex.filterMode = FilterMode.Point;
				colorTex.hideFlags = HideFlags.DontSave;
				colorTex.Create();
			}
			Occupy.Visualize(colorTex, settings.clusters);
			VisualizeIds.Invoke(colorTex);
		}

		private void ClearUnusedPoints() {
			var oldlife = TimeExtension.CurrRelativeSeconds - settings.lifeLimit;
			for (var i = points.Count - 1; i >= 0; i--) {
				if (points[i].birthTime < oldlife)
					points.RemoveAt(i);
			}
		}
		private void UpdateOccupy() {
			var aspect = (float)screenSize.x / screenSize.y;
			var scale = settings.metricsScale;
			var fieldSize = new Vector2(aspect * scale, scale);

			var positions = points.Select(p => Vector2.Scale(p.position, fieldSize)).ToArray();
			var ids = points.Select(p => p.id).ToArray();
			var lifes = points.Select(p => p.birthTime).ToArray();

			Occupy.LifeLimit = settings.lifeLimit;
			Occupy.EdgeDuration = settings.edgeDuration;
			Occupy.Clear();
			for (var i = 0; i < positions.Length; i++)
				Occupy.Add(ids[i], positions[i], lifes[i]);
			Occupy.Update(screenSize);
		}
		#endregion

		#region classes
		[StructLayout(LayoutKind.Sequential)]
		public struct Vector4Int {
			public int x;
			public int y;
			public int z;
			public int w;

			public Vector4Int(int x, int y, int z, int w) {
				this.x = x;
				this.y = y;
				this.z = z;
				this.w = w;
			}

			public int this[int index] {
				get {
					switch (index) {
						case 0:
							return x;
						case 1:
							return y;
						case 2:
							return z;
						case 3:
							return w;
						default:
							throw new IndexOutOfRangeException($"Index={index} is out of range");
					}
				}
				set {
					switch (index) {
						case 0:
							x = value;
							break;
						case 1:
							y = value;
							break;
						case 2:
							z = value;
							break;
						case 3:
							w = value;
							break;
						default:
							throw new IndexOutOfRangeException($"Index={index} is out or range");
					}
				}
			}

			public override string ToString() {
				return $"Int4({x},{y},{z},{w})";
			}

			public static explicit operator Vector4Int(Vector4 v) {
				return new Vector4Int() {
					x = Mathf.RoundToInt(v.x),
					y = Mathf.RoundToInt(v.y),
					z = Mathf.RoundToInt(v.z),
					w = Mathf.RoundToInt(v.w)
				};
			}

			public static explicit operator Vector4Int(Color v) {
				return (Vector4Int)(Vector4)v;
			}
		}
		[System.Serializable]
		public class OccupyModelEvent : UnityEvent<OccupyModel> { }
		[System.Serializable]
		public struct CameraSettings {
			public readonly float orthographicSize;
			public readonly float aspect;
			public readonly Vector2Int screenSize;

			public CameraSettings(Camera cam) {
				this.orthographicSize = cam.orthographicSize;
				this.aspect = cam.aspect;
				this.screenSize = new Vector2Int(cam.pixelWidth, cam.pixelHeight);
			}

			#region static
			public static implicit operator CameraSettings(Camera cam) {
				return new CameraSettings(cam);
			}
			#endregion
			#region interface
			public override bool Equals(object obj) {
				if (!(obj is CameraSettings))
					return false;
				var b = (CameraSettings)obj;
				return orthographicSize == b.orthographicSize
					&& aspect == b.aspect
					&& screenSize == b.screenSize;
			}
			public override string ToString() {
				return string.Format("<{0}:ortho size={1}, aspect={2}, screen={3}",
					GetType().Name, orthographicSize, aspect, screenSize);
			}
			public override int GetHashCode() {
				var h = 58313;
				h = h + orthographicSize.GetHashCode() * 3467;
				h = h + aspect.GetHashCode() * 3467;
				h = h + screenSize.GetHashCode() * 3467;
				return h;
			}
			#endregion
		}
		[System.Serializable]
		public class Settings {
			public float lifeLimit = 10f;
			public Vector2 edgeDuration = new Vector2(0.5f, 0.1f);
			public float metricsScale = 0.01f;
			public int clusters = 10;
		}
		#endregion
	}
}
