// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

//Star Nest algorithm by Pablo Román Andrioli
//Unity 5.x shader by Jonathan Cohen
//This content is under the MIT License.
//
//Original Shader:
//https://www.shadertoy.com/view/XlfGRj
//
//This shader uses the same algorithm in 3d space to render a skybox.

// **Further simplified for Quest by Armin Rigo**


Shader "Skybox/StarNest3" {
	Properties {
		//Scrolls in this direction over time.
		_Scroll ("Scrolling direction (x,y,z) * time", Vector) = (0, 0, 0)
		
		//Center position in space and time.
		_Center ("Center Position (x, y, z)", Vector) = (1, .3, .5, 0)
		
		//How much does camera position cause the effect to scroll?
		//_CamScroll ("Camera Scroll", Float) = 0
		
		//Iterations of inner loop. 
		//The higher this is, the more distant objects get rendered.
		//_Iterations ("Iterations", Range(1, 30)) = 15
		
		//Volumetric rendering steps. Each 'step' renders more objects at all distances.
		//This has a higher performance hit than iterations.
		//_Volsteps ("Volumetric Steps", Range(1,20)) = 8
		
		//Magic number. Best values are around 0.400-0.600.
		_Formuparam ("Formuparam", Float) = 0.6
		
		//Fractal repeating rate
		//Low numbers are busy and give lots of repititio
		//High numbers are very sparce
		_Tile ("Tile", Float) = 0.700
		
		//Brightness scale.
		_Brightness ("Brightness", Float) = .0005

		//How much color is present?
		_Saturation ("Saturation", Float) = 0.77
		
        _ColorGround ("Color Ground", Color) = (0, 0, 0, 0)
        _ColorSky ("Color Sky", Color) = (0, 0, 0, 0)
	}

	SubShader {
		Tags { "Queue"="Background" "RenderType"="Background" "PreviewType"="Skybox" }
		Cull Off ZWrite Off
		
		Pass {
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			
			//int _Volsteps;
			//int _Iterations;
			
			float4 _Scroll;
			float4 _Center;
			//float _CamScroll;
			
			float _Formuparam;

			float _Tile;

			float _Brightness;
			float _Saturation;

            float4 _ColorGround;
            float4 _ColorSky;

			struct appdata_t {
				float4 vertex : POSITION;
			};
			struct v2f {
				float4 pos : SV_POSITION;
				half3 rayDir : TEXCOORD0;	// Vector for incoming ray, normalized ( == -eyeRay )
			}; 
			
			v2f vert(appdata_t v) {
				v2f OUT;
				OUT.pos = UnityObjectToClipPos(v.vertex);
			
				// Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
				float3 eyeRay = normalize(mul((float3x3)unity_ObjectToWorld, v.vertex.xyz));

				OUT.rayDir = half3(eyeRay);
				
				
				return OUT;
			}
			
            half4 frag(v2f IN) : SV_Target{
                half3 col = half3(0, 0, 0);
                half3 dir = IN.rayDir;

                float time = _Time.x;

                float3 from = _Center.xyz;

                //scroll over time
                from += _Scroll.xyz * time;
                //scroll from camera position
                //from += _WorldSpaceCameraPos * _CamScroll;


                //volumetric rendering
                float s = 0.2, fade = 0.01;
                float3 v = float3(0, 0, 0);

                float3 p = from + s * dir * .5;

                p = _Tile - fmod(abs(p), _Tile * 2);
                float pa = 0;
                float a = 0;
                int iterations = dir.y >= 0 ? 11 : 6;
                for (int i = 0; i < iterations; i++) {
                    // unroll one
                    p = abs(p) / dot(p, p) - _Formuparam;
                    a += abs(length(p) - pa);
                    pa = length(p);
                    // unroll two
                    p = abs(p) / dot(p, p) - _Formuparam;
                    a += abs(length(p) - pa);
                    pa = length(p);
                    // unroll three
                    p = abs(p) / dot(p, p) - _Formuparam;
                    a += abs(length(p) - pa);
                    pa = length(p);
                }

                a = max(a, 42);

                // coloring based on distance
                float3 v1 = dir.y >= 0 ? _ColorSky : _ColorGround;
                v += v1 * a*a * _Brightness * fade;

                float len = length(v);
                //Quick saturate
                v = lerp(float3(len, len, len), v, _Saturation);

                return float4(v, 1.0);
            }
			
			
			
			
			ENDCG
		}
		
		
	}

	Fallback Off
}