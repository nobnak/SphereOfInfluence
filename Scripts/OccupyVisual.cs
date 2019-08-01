using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OccupySystem {

	[ExecuteInEditMode]
	[RequireComponent(typeof(Camera))]
	public class OccupyVisual : MonoBehaviour {
		public static readonly int ID_OCCUPY_TEX = Shader.PropertyToID("_OccupyTex");

		[SerializeField]
		protected Material mat;

		protected OccupyModel occ;

		#region interface
		public void ListenOnUpdate(OccupyModel o) {
			occ = o;
		}
		#endregion

		#region unity
		public void OnRenderImage(RenderTexture source, RenderTexture destination) {
			if (occ == null) {
				Graphics.Blit(source, destination);
				return;
			}

			mat.SetTexture(ID_OCCUPY_TEX, occ.Occupy.Ids);
			Graphics.Blit(source, destination, mat);
		}
		#endregion
	}
}