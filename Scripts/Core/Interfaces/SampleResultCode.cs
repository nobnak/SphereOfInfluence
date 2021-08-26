using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SphereOfInfluenceSys.Core.Interfaces {

	public enum SampleResultCode {
		Error_Unknown = -1,
		OK_RegionFound = 0,
		Error_InitialRegion,
		Error_CannnotConvertID,
	}
}
