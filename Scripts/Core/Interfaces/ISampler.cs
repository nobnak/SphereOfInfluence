using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SphereOfInfluenceSys.Core.Interfaces {

	public interface ISampler {

		SampleResultCode TrySample(Vector2 uv, out int regId);
	}
}
