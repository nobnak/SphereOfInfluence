using SphereOfInfluenceSys.App2.Structures;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SphereOfInfluenceSys.App2 {

	public class OccupyClient : MonoBehaviour {

		#region interface
		public void Listen(SharedData shared) {
			Debug.Log($"{GetType().Name} : Receive shared data. {shared}");
		}
		#endregion
	}
}
