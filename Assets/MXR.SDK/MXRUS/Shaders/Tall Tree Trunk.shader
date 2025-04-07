// A shader to animate the trunk of a tall, thin tree. This can be used for
// models of trees such as pine and conifer.

Shader "Universal Render Pipeline/MXR/Tall Tree Trunk" {
    Properties {
        [Space(10)]
        _BaseMap("Albedo", 2D) = "white" {}
        _BaseColor("Color", Color) = (1,1,1,1)

        [Space(10)]
        [Header(Wind)]
        [Space(10)]
        _WindFrequency("Frequency", Float) = 0.1
        _WindX("Strength X", Range(0, 1)) = 0.1
        _WindZ("Strength Z", Range(0, 1)) = 0.1
        _WindSync("Sync", Range(0.0, 1.0)) = 0.9
    }

    SubShader {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200
        Cull Back
        ZWrite On
        Blend One Zero

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

            // vertex function input
            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            // vertex function output, as well as the frag shader input
            struct Varyings {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float3 worldPosition : TEXCOORD2;
            };

            float4 _BaseColor;
            float _WindFrequency; 
            float _WindX;
            float _WindZ;
            float _WindSync;

            Varyings vert(Attributes input) {
                Varyings output;

                // ================================================
                // Trunk sway
                // ================================================
                float3 worldPos = TransformObjectToWorld(input.positionOS.xyz); // world space position of this vertex
                float3 worldNorm = TransformObjectToWorldNormal(input.normalOS); // world space normal of this vertex
                float3 transformWorldPos = unity_ObjectToWorld._m03_m13_m23; // world space position of the transform this material is on 
                float yDistance = distance(transformWorldPos.y, worldPos.y); // the Y distance (height) of this vertex from the Y component of the transform

                // Phase is a linear time multiple + an adjustable randomness based on tree position so all tree instances using the X and Z world position
                // of the transform, this is done so that trees with the same material don't animate identically. The sync property allows adjusting 
                // how identical the animations should be. Sync=1 makes the the animations identical. Sync=0 introduces max differences where if two tree transform
                // positions have 1 meter of difference in their position along the X and Z positions combined would be 1/4th sin cycle apart
                float windPhase = (_Time.y * _WindFrequency * 2 * 3.1416) + ((transformWorldPos.x + transformWorldPos.z) * (3.1416 / 2) * (1 - _WindSync));

                // Calculate the XZ movement that would be made for a vertex at 1m height. 
                // _WindX and _WindZ are divided by 5000 so that the 0-1 property range is more intuitive.
                float2 windBend = float2(_WindX / 5000, _WindZ / 5000) * sin(windPhase);

                // Scale the XZ movement exponentially based on high this vertex of the branch is. This is done so that the trunk bends to form a C shape
                // while bending instead of tilting. A value of 3 as the "stiffness" of the trunk provides good results.
                float2 planarWindOffset = windBend * pow(yDistance, 3);
                worldPos.xz += planarWindOffset;

                // A vertex that moves along the XZ plane as the trunk bends should also move down, otherwise the trunk will skew.
                // The equation here is emperical and not mathematically derived. 
                // If the stiffness above is changed to 2, the value here can change to 3. A stiffness+1 value here works well.
                float yWindOffset = planarWindOffset * cos(length(windBend));
                worldPos.y -= length(planarWindOffset) * abs(sin(windPhase) / 4);
                
                // Apply to output
                output.positionHCS = TransformWorldToHClip(worldPos);
                output.uv = input.uv;
                output.worldNormal = worldNorm;
                output.worldPosition = worldPos;

                return output;
            }

            half4 frag(Varyings input) : SV_Target {
                float4 albedo = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv) * _BaseColor;
                float3 normal = normalize(input.worldNormal);

                // Gets the ambient color for the normal of this vertex
                float3 ambient = SampleSH(normal);

                // Gets the main light of the Unity scene
                Light mainLight = GetMainLight();

                // Lambertian diffuse model where this value would be 1 if the light is shining exactly
                // perpendicularly to the normal. Reduces to 0 as it faces away and doesn't go below 0.
                // In short, NdotL represents the intensity of the light.
                float NdotL = max(0, dot(normal, normalize(mainLight.direction)));

                // Calculate the final lighting
                float3 lighting = ambient + NdotL * mainLight.color.rgb;

                // Multiply the texture color by lighting and return the result with alpha 1 as 
                // this tree trunk shader is opaque.
                float3 finalColor = albedo.rgb * lighting;
                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/Lit"
}
