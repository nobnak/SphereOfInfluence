Shader "Hidden/OccupyVisual" {
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_OccupyTex ("Occupy tex", 2D) = "red" {}
		_HueStep ("Hue Step", Float) = 0.1097
		_HueOffset ("Hue Offset", Range(0, 1)) = 0.1097	
	}
	SubShader {
		Cull Off ZWrite Off ZTest Always

		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "Assets/Packages/Gist/CGIncludes/ColorSpace.cginc"

			struct appdata {
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};
			struct v2f {
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;

			sampler2D _OccupyTex;
			float _HueStep;
			float _HueOffset;

			float4 ColorOfID(int id, float s = 1, float v = 1, float alpha = 1) {
				float h = (id + 1) * _HueStep + _HueOffset;
				h -= floor(h);
				return float4(HSV2RGB(float3(h, s, v)), alpha);
			}

			v2f vert (appdata v) {
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			float4 frag (v2f i) : SV_Target {
				int4 id = tex2D(_OccupyTex, i.uv);
				float4 c = ColorOfID(id.x);
				return c;
			}
			ENDCG
		}
	}
}
