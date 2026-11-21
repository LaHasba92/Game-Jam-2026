Shader "CanonPOW/Map Compatibility"
{
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            CGPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "UnityCG.cginc"

            struct Attributes
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                half4 color : COLOR;
            };

            struct Varyings
            {
                float4 pos : SV_POSITION;
                half3 tint : COLOR;
                UNITY_FOG_COORDS(0)
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.pos = UnityObjectToClipPos(input.vertex);
                half light = saturate(dot(UnityObjectToWorldNormal(input.normal), half3(-0.35, 0.83, 0.43)));
                half shade = lerp(0.72, 1.12, light);
                shade = lerp(shade, 1.2, step(0.75, input.color.a));
                output.tint = input.color.rgb * shade;
                #ifndef UNITY_COLORSPACE_GAMMA
                    output.tint = GammaToLinearSpace(output.tint);
                #endif
                UNITY_TRANSFER_FOG(output, output.pos);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = half4(input.tint, 1);
                UNITY_APPLY_FOG(input.fogCoord, color);
                return color;
            }
            ENDCG
        }
    }
}
