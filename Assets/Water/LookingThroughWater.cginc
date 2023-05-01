#if !defined(LOOKING_THROUGH_WATER_INCLUDED)
#define LOOKING_THROUGH_WATER_INCLUDED

sampler2D _CameraDepthTexture, _WaterBackground;

float3 _WaterFogColor;
float _WaterFogDensity;

float3 ColorBelowWater (float4 screenPos) {
	float2 uv = screenPos.xy / screenPos.w;
	float backgroundDepth =
		LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
	float surfaceDepth = UNITY_Z_0_FAR_FROM_CLIPSPACE(screenPos.z);
	float depthDifference = backgroundDepth - surfaceDepth;

	float3 backgroundColor = tex2D(_WaterBackground, uv).rgb;
	float fogFactor = exp2(-_WaterFogDensity * depthDifference);

	return lerp(_WaterFogColor, backgroundColor, fogFactor);
}

#endif