// PharmaSynth liquid-fill shader (URP 17, Quest-friendly).
// Fulfills the property contract driven by LiquidPhysics.cs:
//   _Fill, _LiquidColour, _SceneColourAmount, _UpVector, _LocalYMin, _LocalYMax, _WobbleX, _WobbleZ
// Notes:
// - Fill plane is computed in OBJECT space along _UpVector (world-up transformed to local by C#).
// - Backfaces above the clip reveal the liquid "surface" (lightened) — classic cull-off liquid trick.
// - _SceneColourAmount is approximated as transparency instead of sampling _CameraOpaqueTexture,
//   which would cost an opaque-texture resolve on Quest.
Shader "PharmaSynth/Liquid"
{
    Properties
    {
        _LiquidColour ("Liquid Colour", Color) = (0.2, 0.5, 1.0, 1.0)
        _TopColour ("Surface Lighten", Range(0,1)) = 0.25
        _SceneColourAmount ("Scene Colour Amount", Range(0,1)) = 0.2
        _Fill ("Fill", Range(0,1)) = 0.5
        _WobbleX ("Wobble X", Float) = 0
        _WobbleZ ("Wobble Z", Float) = 0
        _WobbleScale ("Wobble Scale", Float) = 6
        _UpVector ("Local Up Vector", Vector) = (0,1,0,0)
        _LocalYMin ("Local Y Min", Float) = -0.5
        _LocalYMax ("Local Y Max", Float) = 0.5
        _RimPower ("Rim Power", Range(0.5,8)) = 3
        _RimStrength ("Rim Strength", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _LiquidColour;
                float _TopColour;
                float _SceneColourAmount;
                float _Fill;
                float _WobbleX;
                float _WobbleZ;
                float _WobbleScale;
                float4 _UpVector;
                float _LocalYMin;
                float _LocalYMax;
                float _RimPower;
                float _RimStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
                float3 normalWS   : TEXCOORD1;
                float3 viewDirWS  : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs pos = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = pos.positionCS;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.normalWS   = TransformObjectToWorldNormal(IN.normalOS);
                OUT.viewDirWS  = GetWorldSpaceViewDir(pos.positionWS);
                return OUT;
            }

            half4 frag (Varyings IN, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 up = normalize(_UpVector.xyz);
                float h = dot(IN.positionOS, up);
                float wobble = IN.positionOS.x * _WobbleX * _WobbleScale
                             + IN.positionOS.z * _WobbleZ * _WobbleScale;
                float fillLine = lerp(_LocalYMin, _LocalYMax, saturate(_Fill));
                clip(fillLine - (h + wobble));

                half4 col = _LiquidColour;
                if (IS_FRONT_VFACE(face, false, true))
                {
                    // Backface = visible liquid surface plane
                    col.rgb = saturate(col.rgb + _TopColour);
                }
                else
                {
                    float fres = pow(1.0 - saturate(dot(normalize(IN.normalWS), normalize(IN.viewDirWS))), _RimPower);
                    col.rgb = saturate(col.rgb + fres * _RimStrength);
                }
                col.a = saturate(col.a * (1.0 - _SceneColourAmount * 0.8));
                return col;
            }
            ENDHLSL
        }
    }
    FallBack Off
}
