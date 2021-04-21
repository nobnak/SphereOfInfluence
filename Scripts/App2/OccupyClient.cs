using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SphereOfInfluenceSys.App2 {

	public class OccupyClient : MonoBehaviour {

		#region interface
		public void Listen(OccupyServer.SharedData shared) {
			Debug.Log($"{GetType().Name} : Receive shared data. {shared}");
		}
		#endregion
	}
}
