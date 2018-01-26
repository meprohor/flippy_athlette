Shader "FlippyAthlette/Platform"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_BumpOffset ("MaxOffset", Range(.0, 1.0)) = .25
	}
	
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf Standard fullforwardshadows
		#pragma target 3.0

		struct Input
		{
			float3 worldPos;
		};

		fixed4 _Color;
		half _BumpOffset;
		
		half randomValue(float3 pos)
		{
			return frac(sin(dot(pos, float3(12.9898f, 78.233f, 45.5432f))) * 43758.5453f);
		}

		void surf (Input IN, inout SurfaceOutputStandard o)
		{
			o.Albedo = _Color.rgb * (randomValue(IN.worldPos) * _BumpOffset + (1.0f - _BumpOffset));
		}
		ENDCG
	}
	FallBack "Diffuse"
}
