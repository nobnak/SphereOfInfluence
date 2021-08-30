using SphereOfInfluenceSys.Core.Interfaces;
using UnityEngine;

namespace SphereOfInfluenceSys.Core.Abstracts {

	public abstract class AbstractOccupyClient : MonoBehaviour, ISampler {

		public virtual bool IsActive => isActiveAndEnabled;

		public abstract SampleResultCode TrySample(Vector2 uv, out int regId);
	}

}