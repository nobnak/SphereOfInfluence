using MessagePack;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using WeSyncSys.Extensions.TimeExt;
using static SphereOfInfluenceSys.Core.Occupy;

namespace SphereOfInfluenceSys.App2.Structures {

	[MessagePackObject(keyAsPropertyName: true)]
	public class SharedData {
		public NetworkRegion[] regions;
		public WorldSetttings world;

		#region interface

		#region object
		public override string ToString() {
			var tmp = new StringBuilder();
			tmp.AppendLine($"{GetType().Name} :  ");
			tmp.AppendLine($"Regions, count={regions.Length}");
			for (var i = 0; i < regions.Length; i++)
				tmp.AppendLine($"\t{i}. {regions[i]}");
			tmp.AppendLine($"{world}");
			return tmp.ToString();
		}
		#endregion

		#endregion
	}

	[System.Serializable]
	[StructLayout(LayoutKind.Explicit)]
	[MessagePackObject(keyAsPropertyName: true)]
	public class WorldSetttings {
		[FieldOffset(0)]
		public float worldSize_x = 1f;
		[FieldOffset(4)]
		public float worldSize_y = 1f;
		[FieldOffset(8)]
		public float duration = 15f;

		[FieldOffset(0)]
		[IgnoreMember]
		public Vector2 WorldSize;
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
