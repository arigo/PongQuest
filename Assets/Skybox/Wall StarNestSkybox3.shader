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


Shader "Skybox/WallStarNest3" {
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
		
		//Brightness scale.
		_Brightness ("Brightness", Float) = .0005

		//How much color is present?
		_Saturation ("Saturation", Float) = 0.77
		
        _ColorSky ("Color Sky", Color) = (0, 0, 0, 0)
        _Parameters ("Alpha/Timedir/FormuParam", Vector) = (0.4, 1, 0.35295, 0)
	}

	SubShader {
		Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200

        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
		
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

			float _Brightness;
			float _Saturation;

            float4 _ColorSky;
            float3 _Parameters;

			struct appdata_t {
				float4 vertex : POSITION;
			};
			struct v2f {
				float4 pos : SV_POSITION;
				half3 rayDir : TEXCOORD0;	// Vector for incoming ray
			};

            #define S  0.2
			
			v2f vert(appdata_t v) {
				v2f OUT;
				OUT.pos = UnityObjectToClipPos(v.vertex);
			
				// Get the ray from the camera to the vertex and its length (which is the far point of the ray passing through the atmosphere)
				float3 eyeRay = normalize(v.vertex.xyz);

				OUT.rayDir = half3(eyeRay) * S * .5;
				
				
				return OUT;
			}
			
            half4 frag(v2f IN) : SV_Target{
                half3 col = half3(0, 0, 0);
                half3 dir = IN.rayDir;

                float time = _Time.x * _Parameters.y;

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
                int iterations = 10;
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

                a = max(a, 42);

                // coloring based on distance
                float3 v1 = _ColorSky;
                v += v1 * a*a * _Brightness * fade;

                float len = length(v);
                //Quick saturate
                v = lerp(float3(len, len, len), v, _Saturation);

                return float4(v, len * _Parameters.x);
            }
			
			
			
			
			ENDCG
		}
		
		
	}

	Fallback Off
}