/*
 *    Copied from AvatarSurfaceShaderPBS.shader from the Oculus Integration package,
 *    and tweaked to support changing the color of individual buttons.
 */

Shader "Custom/DefaultOculusTouchShader" {
	Properties{
		// Global parameters
		_Albedo("Albedo (G) and component mask (RB)", 2D) = "" {}
		_Surface("Metallic (R) Occlusion (G) and Smoothness (A)", 2D) = "" {}
        _Components("Component colors", 2D) = "black" {}
	}
	SubShader{
		Tags {
			"Queue" = "Geometry+400"
			"RenderType" = "Opaque"
		}

		LOD 200

			CGPROGRAM

// Physically based Standard lighting model, and enable shadows on all light types
#pragma surface surf Standard vertex:vert fullforwardshadows

// Use shader model 3.0 target, to get nicer looking lighting
#pragma target 3.0

sampler2D _Albedo;
float4 _Albedo_ST;
sampler2D _Surface;
float4 _Surface_ST;
sampler2D _Components;

struct Input {
	float2 texcoord;
};

void vert(inout appdata_full v, out Input o) {
	UNITY_INITIALIZE_OUTPUT(Input, o);
	o.texcoord = v.texcoord.xy;
}

void surf (Input IN, inout SurfaceOutputStandard o) {
    float4 albedo_and_mask = tex2D(_Albedo, TRANSFORM_TEX(IN.texcoord, _Albedo));
    float3 albedo = albedo_and_mask.ggg;
    float3 component_color = tex2D(_Components, float2(albedo_and_mask.r, albedo_and_mask.b)).rgb;
    o.Albedo = albedo + component_color;
	float4 surfaceParams = tex2D(_Surface, TRANSFORM_TEX(IN.texcoord, _Surface));
	o.Metallic = surfaceParams.r;
	o.Occlusion = surfaceParams.g;
	o.Smoothness = surfaceParams.a;
}

#pragma only_renderers d3d11 gles3 gles

ENDCG
	}
	FallBack "Diffuse"
}
