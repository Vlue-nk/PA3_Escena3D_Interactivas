Shader "PA3/MysticFresnel"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.015, 0.08, 0.09, 1)
        _EdgeColor ("Fresnel HDR Color", Color) = (0.02, 0.65, 0.72, 1)
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 3
        _EmissionStrength ("Emission Strength", Range(0, 8)) = 2.4
        _PulseSpeed ("Pulse Speed", Range(0.1, 8)) = 1.4
        _PulseScale ("Pulse Scale", Range(0.1, 4)) = 1.3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "UniversalForward"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float3 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionHCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1; };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeColor;
                float _FresnelPower;
                float _EmissionStrength;
                float _PulseSpeed;
                float _PulseScale;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs pos = GetVertexPositionInputs(input.positionOS);
                output.positionHCS = pos.positionCS;
                output.positionWS = pos.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDir = normalize(GetWorldSpaceNormalizeViewDir(input.positionWS));
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), _FresnelPower);
                float pulse = saturate(0.5 + 0.5 * sin(_Time.y * _PulseSpeed + input.positionWS.y * _PulseScale));
                float3 color = lerp(_BaseColor.rgb, _EdgeColor.rgb, fresnel);
                float3 emission = _EdgeColor.rgb * fresnel * _EmissionStrength * (0.55 + pulse * 0.45);
                return half4(color + emission, saturate(0.72 + fresnel * 0.28));
            }
            ENDHLSL
        }
    }
}
