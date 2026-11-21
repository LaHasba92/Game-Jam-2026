Shader "CanonPOW/Landscape Sky"
{
    Properties
    {
        [HideInInspector] _Landscape("Landscape", Float) = 0
    }
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

            CBUFFER_START(UnityPerMaterial)
                float _Landscape;
            CBUFFER_END

            struct Attributes
            {
                float3 positionOS : POSITION;
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
                output.positionCS = TransformObjectToHClip(vertex.positionOS);
                output.direction = vertex.positionOS;
                return output;
            }

            float Hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 blend = frac(p);
                blend = blend * blend * (3 - 2 * blend);
                return lerp(lerp(Hash(cell), Hash(cell + float2(1, 0)), blend.x),
                    lerp(Hash(cell + float2(0, 1)), Hash(cell + 1), blend.x), blend.y);
            }

            half3 Daylight(float3 direction)
            {
                bool beach = _Landscape > 0.5;
                half3 zenith = beach ? half3(0.29, 0.66, 0.85) : half3(0.51, 0.69, 0.65);
                half3 horizon = beach ? half3(0.73, 0.88, 0.9) : half3(0.83, 0.84, 0.67);
                half3 color = lerp(zenith, horizon, exp(-max(0, direction.y) * 4.3));
                float2 plane = direction.xz / (max(0, direction.y) + 0.23);
                float cloud = Noise(plane * 2.5) * 0.66 + Noise(plane * 5.7) * 0.24 + Noise(plane * 13.3) * 0.10;
                float cover = smoothstep(beach ? 0.53 : 0.61, 0.78, cloud) * smoothstep(0.01, 0.20, direction.y);
                color = lerp(color, half3(0.97, 0.96, 0.89), cover * (beach ? 0.72 : 0.43));
                float sun = dot(direction, beach ? normalize(float3(0.94, 0.3, -0.32)) :
                    normalize(float3(0.86, 0.42, 0.29)));
                color = lerp(color, half3(1, 0.87, 0.62), pow(saturate(sun), beach ? 31 : 27) * 0.43);
                return lerp(color, half3(1, 0.97, 0.84), smoothstep(0.9988, 0.99925, sun));
            }

            half3 Space(float3 direction)
            {
                float skyHeight = saturate(direction.y);
                half3 color = lerp(half3(0.029, 0.042, 0.088), half3(0.007, 0.011, 0.038), saturate(skyHeight * 6.6));
                float2 sky = float2(atan2(direction.z, direction.x), asin(clamp(direction.y, -1, 1))) * 79;
                float2 cell = floor(sky);
                float star = Hash(cell);
                float2 spot = 0.23 + 0.54 * float2(Hash(cell + 3), Hash(cell + 8));
                float glimmer = 1 - smoothstep(0.014, 0.105, length(frac(sky) - spot));
                color += half3(0.61, 0.74, 0.88) * step(0.87, star) * glimmer * smoothstep(-0.04, 0.13, direction.y);

                const float earthRadius = 0.077;
                float3 earth = normalize(float3(0.96, 0.19, -0.21));
                float3 right = normalize(cross(earth, float3(0, 1, 0)));
                float3 up = cross(right, earth);
                float2 p = float2(dot(direction, right), dot(direction, up)) / earthRadius;
                float radius = length(p);
                float facing = dot(earth, direction);
                color += half3(0.13, 0.32, 0.63) * pow(saturate(facing), 132) * 0.22;
                if (radius < 1.08 && facing > 0)
                {
                    float3 normal = float3(p, sqrt(saturate(1 - dot(p, p))));
                    float landMass = max(1 - length((p - float2(-0.20, 0.40)) * float2(1.4, 2.1)),
                        1 - length((p - float2(0.26, 0.02)) * float2(2.4, 1.35)));
                    landMass += 0.78 * Noise(p * 8) + 0.23 * Noise(p * 25);
                    float land = smoothstep(0.86, 0.95, landMass);
                    float clouds = 0.62 * Noise(p * float2(4.5, 12)) + 0.38 * Noise(p * 27);
                    half3 planet = lerp(half3(0.16, 0.42, 0.79), half3(0.50, 0.69, 0.60), land);
                    planet = lerp(planet, half3(0.89, 0.95, 0.97), smoothstep(0.58, 0.78, clouds) * 0.8);
                    float light = smoothstep(-0.35, 0.82, dot(normal, float3(-0.51, 0.52, 0.61)));
                    planet *= 0.15 + light * 0.93;
                    float limb = smoothstep(0.86, 1, radius) * (0.18 + light * 0.45);
                    planet += half3(0.37, 0.61, 0.90) * limb;
                    color = lerp(color, planet, 1 - smoothstep(0.984, 1.014, radius));
                }
                return color;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.direction);
                half3 sky = _Landscape < 1.5 ? Daylight(direction) : Space(direction);
                #ifndef UNITY_COLORSPACE_GAMMA
                    sky = SRGBToLinear(sky);
                #endif
                return half4(sky, 1);
            }
            ENDHLSL
        }
    }
}
