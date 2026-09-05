// Tutorial Mode guidance overlay (2026-08-07).
//
// Exists because URP's stock Unlit shader does NOT declare a _ZTest property, so
// material.SetInt("_ZTest", ...) against it is a silent no-op — the beacon and the
// x-ray silhouette both compiled, both looked configured, and both would have
// rendered with ordinary depth testing (i.e. through nothing at all).
//
// Two uses, one shader, set by _ZTest:
//   Greater (5) -> the x-ray ghost: visible ONLY where the object is occluded, so an
//                  unobstructed bottle looks completely normal.
//   Always  (8) -> the waypoint beacon: always on top, readable through a cabinet door.
//
// ⛔ STEREO INSTANCING IS MANDATORY (2026-09-05, found in the headset). The project runs
// Single Pass Instanced, where BOTH eyes are rendered into one texture array and the
// shader picks its slice from the instance id. A shader that omits these macros silently
// draws to eye index 0 only, so the arrows and the beacon appeared in the LEFT EYE ONLY
// and the user had to close one eye to play. Nothing in the editor shows this — the Game
// view renders a single eye and looks perfect. Copy this block into any new shader.
Shader "PharmaSynth/GuideOverlay"
{
    Properties
    {
        _BaseColor ("Base Colour", Color) = (1, 0.72, 0.2, 0.4)
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest ("ZTest", Float) = 5
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "GuideOverlay"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest [_ZTest]
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                return half4(_BaseColor.rgb, _BaseColor.a);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Unlit"
}
