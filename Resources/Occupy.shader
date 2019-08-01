Shader "Unlit/Occupy" {
	Properties {
		_Weights("Weights", 2D) = "white" {}
		_Ids("IDs", 2D) = "black" {}
		_HueOffset ("Hue Offset", Range(0, 1)) = 0.1097
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 100

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "Occupy.cginc"
			#include "Assets/Packages/Gist/CGIncludes/ColorSpace.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _Weights;
			float4 _Weights_ST;
			float4 _Weights_TexelSize;

			sampler2D _Ids;

			float _HueOffset;

			float4 ColorOfID(int id, float s = 1, float v = 1, float alpha = 1) {
				float h = id * _HueOffset;
				h -= floor(h);
				return float4(HSV2RGB(float3(h, s, v)), alpha);
			}

			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _Weights);
				return o;
			}
			
			fixed4 frag (v2f IN) : SV_Target {
				float4 w = tex2D(_Weights, IN.uv);
				int4 id = tex2D(_Ids, IN.uv);

				float4x4 colors = { ColorOfID(id.x), ColorOfID(id.y), ColorOfID(id.z), ColorOfID(id.w) };
				return mul(w, colors);
			}
			ENDCG
		}
	}
}
