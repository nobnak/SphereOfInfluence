using MessagePack;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using WeSyncSys.Extensions;
using static SphereOfInfluenceSys.Core.Occupy;

namespace SphereOfInfluenceSys.Core.Structures {

	[MessagePackObject(keyAsPropertyName: true)]
	public class SharedData {
		public NetworkRegion[] regions;
		public OccupyTuner occupy;

		#region interface

		#region object
		public override string ToString() {
			var tmp = new StringBuilder();
			tmp.AppendLine($"{GetType().Name} :  ");
			tmp.AppendLine($"Regions, count={regions.Length}");
			for (var i = 0; i < regions.Length; i++)
				tmp.AppendLine($"\t{i}. {regions[i]}");
			tmp.AppendLine($"{occupy}");
			return tmp.ToString();
		}
		#endregion

		#endregion
	}

	[System.Serializable]
	[StructLayout(LayoutKind.Explicit)]
	[MessagePackObject(keyAsPropertyName: true)]
	public class OccupyTuner {
		[FieldOffset(0)]
		[Tooltip("領域の寿命(sec)")]
		public float lifeLimit = 10f;
		[FieldOffset(4)]
		[Tooltip("拡大時間の割合 [0.0-1.0]")]
		public float edgeDuration_x = 0.5f;
		[FieldOffset(8)]
		[Tooltip("縮小開始時間 [0.0-1.0]")]
		public float edgeDuration_y = 0.9f;

		[IgnoreMember]
		[HideInInspector]
		[FieldOffset(4)]
		public Vector2 EdgeDuration;

		#region interface
		public bool Valid() =>
			edgeDuration_x >= 0f
			&& edgeDuration_y > edgeDuration_x
			&& edgeDuration_y <= 1f;
		public Vector4 TemporalSetting =>
			new Vector4(edgeDuration_x, edgeDuration_y, TimeExtension.CurrRelativeSeconds, 1f / lifeLimit);
		#endregion
	}

	[System.Serializable]
	[StructLayout(LayoutKind.Explicit)]
	[MessagePackObject(keyAsPropertyName:true)]
	public struct NetworkRegion {
		[FieldOffset(0)]
		public readonly int id;
		[FieldOffset(4)]
		public readonly long tick;
		[FieldOffset(12)]
		public readonly float position_x;
		[FieldOffset(16)]
		public readonly float position_y;

		[FieldOffset(12)]
		[IgnoreMember]
		public readonly Vector2 Position;

		[SerializationConstructor]
		public NetworkRegion(int id, float position_x, float position_y, long tick) {
			this.id = id;
			this.tick = tick;

			this.Position = default;
			this.position_x = position_x;
			this.position_y = position_y;
		}
		public NetworkRegion(int id, Vector2 position, long tick)
			: this(id, position.x, position.y, tick) { }
		public NetworkRegion(int id, Vector2 position)
			: this(id, position, TimeExtension.CurrTick) { }

		#region interface

		#region object
		public override string ToString() {
			return $"{GetType().Name} : "
				+ $"id={id}, "
				+ $"time={tick.RelativeSeconds()} ({tick.ToDateTime()}), "
				+ $"pos={Position}";
		}
		#endregion

		#endregion

		#region static
		public static implicit operator Region(NetworkRegion hires) {
			return new Region(
				hires.id,
				hires.Position,
				hires.tick.RelativeSeconds());
		}
		public static explicit operator NetworkRegion(Region r) {
			return new NetworkRegion(
				r.id,
				r.position,
				r.birthTime.TickFromRelativeSeconds());
		}
		#endregion
	}
}
