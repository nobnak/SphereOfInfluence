using nobnak.Gist;
using nobnak.Gist.Layer2;
using nobnak.Gist.Layer2.Extensions;
using nobnak.Gist.Wrapper;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SphereOfInfluenceSys.Core {

	[ExecuteInEditMode]
	public class OccupyView : MonoBehaviour {

		[SerializeField]
		protected bool visible = true;
		[SerializeField]
		protected Renderer rend;
		[SerializeField]
		protected Layer layerPoints;

		protected GLFigure fig;
		protected OccupyModel model;
		protected Block block;

		#region unity
		private void OnEnable() {
			fig = new GLFigure();

			rend = rend ?? GetComponent<Renderer>();
			rend.enabled = visible;

			block = new Block(rend);
		}
		private void OnDisable() {
			if (rend != null)
				rend.enabled = false;
			fig.Dispose();
		}
		private void Update() {
			rend.enabled = visible;
		}
		private void OnRenderObject() {
			if (model == null)
				return;

			var rot = layerPoints.transform.rotation;
			var size = 0.1f * Vector2.one;
			var color = Color.magenta;
			foreach (var p in model) {
				var uv = (Vector2)p.position;
				var worldPos = layerPoints.LocalToWorld.TransformPoint(
					layerPoints.UvToLocalPos(uv, 0f));
				fig.FillQuad(worldPos, rot, size, color);
			}
		}
		#endregion

		#region interface
		public bool CurrentVisible { get { return visible; } set { visible = value; } }
		public void UpdateOn(OccupyModel occupyRend) {
			this.model = occupyRend;
			UpdateOn(occupyRend.Occupy);
		}

		public void UpdateOn(Occupy occupy) {
			block.SetTexture(OccupyVisual.ID_OCCUPY_TEX, occupy.Ids).Apply();
		}
		#endregion
	}
}
