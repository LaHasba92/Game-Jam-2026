Shader "CanonPOW/District Néon ciel"
{
    SubShader
    {
        Tags { "RenderType" = "Background" "Queue" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 direction : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes vertex)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(vertex);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(vertex.positionOS.xyz);
                output.direction = vertex.positionOS.xyz;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                half altitude = max(0, direction.y);
                half haze = exp(-altitude * 9.0);
                half band = smoothstep(0.02, 0.10, altitude) * (1 - smoothstep(0.14, 0.34, altitude));
                half facing = 0.5 + 0.5 * direction.z;
                half3 horizon = lerp(half3(0.25, 0.16, 0.38), half3(0.17, 0.22, 0.41), facing);
                half3 sky = lerp(half3(0.035, 0.030, 0.085), horizon, haze) +
                    band * half3(0.064, 0.028, 0.093);
                #ifndef UNITY_COLORSPACE_GAMMA
                    sky = SRGBToLinear(sky);
                #endif
                return half4(sky, 1);
            }
            ENDHLSL
        }
    }
}
