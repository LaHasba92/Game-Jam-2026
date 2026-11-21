Shader "CanonPOW/Landscape"
{
    Properties
    {
        [HideInInspector] _Landscape("Landscape", Float) = 0
    }
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

            CBUFFER_START(UnityPerMaterial)
                float _Landscape;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                half4 color : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half4 color : COLOR;
                half2 lightAndFog : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes vertex)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(vertex);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionWS = TransformObjectToWorld(vertex.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.color = vertex.color;
                half3 lightDir = _Landscape < 0.5 ? half3(0.68, 0.60, 0.42) :
                    _Landscape < 1.5 ? half3(-0.42, 0.86, -0.28) : half3(-0.56, 0.72, 0.41);
                half lambert = saturate(dot(TransformObjectToWorldNormal(vertex.normalOS), lightDir));
                output.lightAndFog = half2(lerp(0.71, 1.21, lambert), ComputeFogFactor(output.positionCS.z));
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 color = input.color.rgb;
                float2 p = input.positionWS.xz;
                half illumination = input.lightAndFog.x;

                if (input.color.a > 0.75)
                {
                    illumination = 1.13;
                }
                else if (input.color.a > 0.38)
                {
                    float wave = sin(p.y * 1.72 + sin(p.x * 0.24) * 0.74 + _Time.y * 1.06);
                    float glint = sin(p.x * 0.95 + p.y * 0.53 - _Time.y * 0.66);
                    float foam = smoothstep(0.71, 0.96, wave) * smoothstep(0.1, 0.85, glint);
                    float shallows = saturate((input.color.r - 0.28) * 1.6);
                    color = lerp(color * (0.96 + wave * 0.037), half3(0.87, 0.96, 0.92), foam * shallows * 0.56);
                    illumination = 1.02;
                }
                else if (input.color.a > 0.12)
                {
                    float texture = sin(p.x * 2.68 + sin(p.y * 1.8)) * sin(p.y * 4.1 + p.x * 0.39);
                    illumination *= 1 + texture * 0.035;
                    if (_Landscape < 0.5)
                    {
                        float canopy = sin(p.x * 0.67 + sin(p.y * 0.62)) * sin(p.y * 0.78 + p.x * 0.19);
                        float patches = smoothstep(-0.56, 0.15, canopy);
                        color *= lerp(half3(0.90, 0.92, 0.97), half3(1.075, 1.054, 1.00), patches);
                    }
                }

                color *= illumination;
                #ifndef UNITY_COLORSPACE_GAMMA
                    color = SRGBToLinear(color);
                #endif
                return half4(MixFog(color, input.lightAndFog.y), 1);
            }
            ENDHLSL
        }
    }
}
