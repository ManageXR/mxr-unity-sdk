Shader "Universal Render Pipeline/MXR/Tall Tree Branches" {
    Properties {
        [Header(Rendering)]
        [Space(10)]
        _BaseMap("Albedo", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)

        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5

        _OcclusionMap("Occlusion", 2D) = "white" {}
        _OcclusionStrength("Occlusion Strength", Range(0, 1)) = 1.0

        [Space(10)]
        [Header(Wind)]
        [Space(10)]
        _WindFrequency("Frequency", Float) = 0.1
        _WindX("Strength X", Range(0, 10)) = 0.1
        _WindZ("Strength Z", Range(0, 10)) = 0.1
        _WindSync("Sync", Range(0.0, 1.0)) = 0.9

        [Space(10)]
        [Header(Rustle)]
        [Space(10)]
        _RustleFrequency("Frequency", Float) = 0.25
        _RustleStrength("Strength", Range(0.0, 1.0)) = 0.5
    }

    SubShader {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }

        LOD 200
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPosition : TEXCOORD2;
            };

            float4 _BaseColor;
            float4 _AmbientLight;

            TEXTURE2D(_OcclusionMap);
            SAMPLER(sampler_OcclusionMap);
            float _OcclusionStrength;

            float _Cutoff;

            float _WindFrequency;
            float _WindSync;
            float _WindX;
            float _WindZ;

            float _RustleStrength;
            float _RustleFrequency;

            Varyings vert(Attributes input) {
                Varyings output;

                // ================================================
                // Trunk sway is identical to the Tree Trunk shader to ensure branches move in sync with the trunk
                // ================================================
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz);
                float3 worldNorm = TransformObjectToWorldNormal(input.normalOS);
                float3 transformWorldPos = unity_ObjectToWorld._m03_m13_m23;
                float yDistance = distance(transformWorldPos.y, worldPos.y);

                float windPhase = (_Time.y * _WindFrequency * 2 * 3.1416) + ((transformWorldPos.x + transformWorldPos.z) * (3.1416 / 2) * (1 - _WindSync));
                float2 windBend = float2(_WindX / 5000, _WindZ / 5000) * sin(windPhase);
                float2 planarWindOffset = windBend * pow(yDistance, 3);
                worldPos.xz += planarWindOffset;

                float yWindOffset = planarWindOffset * cos(length(windBend));
                worldPos.y -= length(planarWindOffset) * abs(sin(windPhase) / 4);
                
                // ================================================
                // Branch rustle - smaller, more local and faster movements
                // ================================================
                // Get the distance from the center of the trunk. This is used to linearly scale the amount of rustling
                // of the branch vertex as this distance increases. This is done so that the vertices close to where the branch
                // is attached to the trunk move less and the branch looks like they're fixed to the trunk
                float distFromTrunk = distance(worldPos.xz, transformWorldPos.xz);

                // Rustle strength multiplied by 10 so that the 0-1 property range is more intuitive
                float rustleFactor = distFromTrunk * _RustleStrength / 10; 

                // Much like windPhase above, this is the phase for the sin calculation and to make sure that all the branches don't 
                // rustle in sync, we add PI * yDistance so that the vertices of the branches rustle in different sin phases based on their height.
                float rustlePhase = (_Time.y * _RustleFrequency * 2 * 3.1416) + (yDistance * 3.1416);
                float branchRustle = sin(rustlePhase);

                worldPos += (worldNorm * branchRustle) * rustleFactor;

                // Apply to output
                output.positionHCS = TransformWorldToHClip(worldPos);
                output.uv = input.uv;
                output.worldNormal = worldNorm;
                output.worldPosition = worldPos;

                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                // Identical to the Tall Tree Trunk shader, except that this shader also performs
                // - alpha clipping as branches are not opaque
                // - occlusion map so that the branches can be made to appear darker where they are closer to the trunk
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normal = normalize(input.worldNormal);

                float3 ambient = SampleSH(normal);

                clip(albedo.a - _Cutoff);

                Light mainLight = GetMainLight();
                float NdotL = max(0, dot(normal, normalize(mainLight.direction)));

                float occlusion = SAMPLE_TEXTURE2D(_OcclusionMap, sampler_OcclusionMap, input.uv).r;
                occlusion = lerp(1.0, occlusion, _OcclusionStrength);

                float3 lighting = (NdotL * mainLight.color.rgb + ambient) * occlusion;

                float3 finalColor = albedo.rgb * lighting;
                return float4(finalColor, albedo.a);
            }

            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/Lit"
}
