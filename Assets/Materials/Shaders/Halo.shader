Shader "Custom/Halo"
{
    Properties
    {
        _Color("Color", Color) = (1, 0, 0, 1)
        _Size("Size", Vector) = (0.2, 0.1, 0, 0)
    }
    
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+7" }
        LOD 100

        Pass
        {
            Cull off
            ZWrite off
            Blend SrcAlpha OneMinusSrcAlpha

            CGPROGRAM
            //#pragma require compute
            //#include "Assets/Scripts/ShaderDebugger/debugger.cginc"

            #pragma vertex vert
            #pragma fragment frag

            struct incoming
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            fixed4 _Color;
            float2 _Size;

            v2f vert(incoming v)
            {
                v2f o;

                //uint root = DebugVertexO4(v.vertex);
                //DbgValue2(root, v.uv);

                float3 w_pos = mul(unity_ObjectToWorld, v.vertex);
                float3 w_view = w_pos - _WorldSpaceCameraPos;

                float3 w_depth = normalize(w_view);
                float3 w_orthogonal1 = normalize(float3(w_view.z, 0, -w_view.x));
                float3 w_orthogonal2 = float3(0, 1, 0);

                w_pos += (v.uv.x * w_orthogonal1 + v.uv.y * w_orthogonal2) * _Size.x;
                w_pos += w_depth * _Size.y;
                o.pos = mul(UNITY_MATRIX_VP, float4(w_pos, 1.0));
                o.uv = v.uv;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                float2 border2 = i.uv * i.uv;
                border2 *= border2;
                float border = border2.x + border2.y;

                //uint root = DebugFragment(i.pos);
                //DbgValue2(root, i.color_interpolate.xy);

                float alpha = 1 - border;
                fixed4 col = _Color;
                col.a *= alpha;
                return col;
            }
            ENDCG
        }
    }
}
