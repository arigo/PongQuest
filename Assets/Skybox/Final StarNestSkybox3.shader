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


Shader "Skybox/FinalWallStarNest3" {
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
		
		//Fractal repeating rate
		//Low numbers are busy and give lots of repititio
		//High numbers are very sparce
		_Tile ("Tile", Float) = 0.700

        _Color ("Color", Color) = (1, 1, 1, 1)    // this is only for Cell.cs
		
        _Parameters ("Atten/-/FormuParam", Vector) = (1, 0, 0.35295, 0)
	}

	SubShader {
		Tags { "RenderType"="Opaque" }
        LOD 200
        
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
			
			float _Tile;

            float3 _Parameters;

			struct appdata_t {
				float4 vertex : POSITION;
                float3 normal : NORMAL;
			};
			struct v2f {
				float4 pos : SV_POSITION;
				float4 rayDir : TEXCOORD0;	// Vector for incoming ray; z = attenuation
            };

            #define S  0.2
			
			v2f vert(appdata_t v) {
				v2f OUT;
				OUT.pos = UnityObjectToClipPos(v.vertex);
                float attenuation = dot(v.normal, normalize(ObjSpaceLightDir(v.vertex)));
                attenuation = attenuation * 0.5 + 0.5;
                attenuation *= _Parameters.x;

				// Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
                float3 vv = v.vertex.xyz;
                vv.y *= 2.5;
				float3 eyeRay = normalize(vv);

				OUT.rayDir = float4(eyeRay * S * .5, attenuation);
				
				
				return OUT;
			}
			
            half4 frag(v2f IN) : SV_Target{
                half3 dir = IN.rayDir.xyz;

                float time = _Time.x;

                float3 from = _Center.xyz;

                //scroll over time
                from += _Scroll.xyz * time;
                //scroll from camera position
                //from += _WorldSpaceCameraPos * _CamScroll;


                //volumetric rendering
                float fade = 0.01;
                float3 v = float3(0, 0, 0);

                float3 p = from + dir;

                p = _Tile - fmod(abs(p), _Tile * 2);
                float pa = 0;
                float a = 0;
                int iterations = 8;
                for (int i = 0; i < iterations; i++) {
                    // unroll one
                    p = abs(p) / dot(p, p) - _Parameters.z;
                    a += abs(length(p) - pa);
                    pa = length(p);
                    // unroll two
                    p = abs(p) / dot(p, p) - _Parameters.z;
                    a += abs(length(p) - pa);
                    pa = length(p);
                    // unroll three
                    p = abs(p) / dot(p, p) - _Parameters.z;
                    a += abs(length(p) - pa);
                    pa = length(p);
                }

                a = frac(a * 0.06) * 3;

                float3 col = float3(
                    clamp(1 - a, 0, 1) + clamp(a - 2, 0, 1),
                    1,
                    clamp(1 - abs(a - 1), 0, 1));

                return float4(col * IN.rayDir.w, 1);
            }


            ENDCG
		}
		
		
	}

	Fallback Off
}