Shader "CanonPOW/District Néon"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" "RenderType" = "Opaque" "Queue" = "Geometry" }
        Pass
        {
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                half2 lightAndFog : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes vertex)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(vertex);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(vertex.positionOS.xyz);
                output.color = vertex.color;
                #ifndef UNITY_COLORSPACE_GAMMA
                    output.color.rgb = SRGBToLinear(output.color.rgb);
                #endif

                float3 normal = TransformObjectToWorldNormal(vertex.normalOS);
                float light = saturate(dot(normal, float3(-0.34, 0.82, -0.46)));
                output.lightAndFog = half2(lerp(0.64, 1.15, light), ComputeFogFactor(output.positionCS.z));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half illumination = lerp(input.lightAndFog.x, 1.9, input.color.a);
                return half4(MixFog(input.color.rgb * illumination, input.lightAndFog.y), 1);
            }
            ENDHLSL
        }
    }
}
