using MessagePack;
using SphereOfInfluenceSys.Extensions.TimeExt;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
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
	[MessagePackObject(keyAsPropertyName: true)]
	public class WorldSetttings {
		public float worldSize_x;
		public float worldSize_y;
		public float duration = 15f;

		[IgnoreMember]
		public Vector2 WorldSize {
			get => new Vector2(worldSize_x, worldSize_y);
			set {
				worldSize_x = value.x;
				worldSize_y = value.y;
			}
		}
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
		public readonly Vector2 position;

		[SerializationConstructor]
		public NetworkRegion(int id, float position_x, float position_y, long tick) {
			this.id = id;
			this.position = default;
			this.position_x = position_x;
			this.position_y = position_y;
			this.tick = tick;
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
				+ $"pos={position}";
		}
		#endregion

		#endregion

		#region static
		public static implicit operator Region(NetworkRegion hires) {
			return new Region(
				hires.id,
				hires.position,
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
