using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SphereOfInfluenceSys.Core.Interfaces {

	public interface ISampler {

		bool IsActive { get; }
		SampleResultCode TrySample(Vector2 uv, out int regId);
	}
}
