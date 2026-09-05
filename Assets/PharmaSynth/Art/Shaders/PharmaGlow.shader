// Tutorial Mode guidance GLOW (2026-08-07).
//
// Tinting an object's base colour just makes it look repainted — "orange", not lit.
// This draws an additive shell over the target instead, in two passes, so a guided
// item reads as glowing whether or not anything is in the way:
//
//   Pass "Rim"      — where the object IS visible: an additive fresnel rim, brightest
//                     at the silhouette edge and transparent through the middle, so the
//                     object still looks like itself but wears a halo.
//   Pass "Occluded" — where the object is HIDDEN: a flat ghost, so it can still be
//                     found through a cabinet door.
//
// The pulse is computed from _Time in the shader rather than driven per-frame from C#:
// it costs nothing, never stutters with framerate, and needs no Update loop.
//
// NOTE the sibling PharmaGuide.shader is a different job (single flat pass with a
// settable _ZTest, used by the waypoint beacon) — do not merge them.
Shader "PharmaSynth/GuideGlow"
{
    Properties
    {
        _BaseColor  ("Glow Colour", Color) = (1, 0.72, 0.2, 1)
        _Intensity  ("Rim Intensity", Range(0, 6)) = 2.2
        _RimPower   ("Rim Tightness", Range(0.5, 8)) = 2.2
        _PulseSpeed ("Pulse Speed", Float) = 3.0
        _PulseMin   ("Pulse Floor", Range(0, 1)) = 0.35
        _Occluded   ("Through-Wall Strength", Range(0, 1)) = 0.35
        _Swell      ("Pulse Swell (metres)", Range(0, 0.05)) = 0.0
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+10" "RenderPipeline" = "UniversalPipeline" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor;
            float  _Intensity;
            float  _RimPower;
            float  _PulseSpeed;
            float  _PulseMin;
            float  _Occluded;
            float  _Swell;
        CBUFFER_END

        // GLOBAL, so it must live OUTSIDE UnityPerMaterial (a global inside the
        // per-material buffer breaks SRP batching and reads garbage).
        //
        // 0 = pulse, 1 = hold steady. Phrased as "steady" and NOT as "flash" because an
        // unset shader global reads as ZERO — so the default has to be the normal
        // pulsing behaviour, or every scene that never touched the setting would come
        // up frozen. Written by ComfortApplier from the "reduce flashing" setting.
        float _GuideSteady;

        // ⛔ STEREO INSTANCING IS MANDATORY (2026-09-05, found in the headset). The project
        // runs Single Pass Instanced: both eyes render into one texture array and the shader
        // picks its slice from the instance id. Omit these macros and the shader silently
        // draws to eye index 0 only — the guidance glow appeared in the LEFT EYE ONLY and the
        // user had to close one eye to play. The Game view renders a single eye, so nothing
        // in the editor reveals it. Both passes need the pragma; the frags need the setup.
        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS   : NORMAL;
            UNITY_VERTEX_INPUT_INSTANCE_ID
        };

        struct Varyings
        {
            float4 positionHCS : SV_POSITION;
            float3 normalWS    : TEXCOORD0;
            float3 viewWS      : TEXCOORD1;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            UNITY_VERTEX_OUTPUT_STEREO
        };

        // 0..1, never fully dark: a glow that blinks out entirely reads as a fault.
        //
        // With _GuideSteady at 1 the wave is forced to its PEAK rather than its mean —
        // accessibility must not cost the player brightness, or "reduce flashing" would
        // quietly become "harder to find things".
        float Pulse()
        {
            float wave = 0.5 + 0.5 * sin(_Time.y * _PulseSpeed);
            wave = lerp(wave, 1.0, saturate(_GuideSteady));
            return lerp(_PulseMin, 1.0, wave);
        }

        Varyings Vert (Attributes IN)
        {
            Varyings OUT;
            UNITY_SETUP_INSTANCE_ID(IN);
            UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
            UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
            float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
            float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
            // Breathe OUTWARD along the normal as well as brightening, so the pulse is
            // a change in size and not only in intensity. Offset is in METRES (world),
            // so a test tube and a Florence flask swell by the same visible amount
            // rather than by a fraction of their own wildly different scales.
            positionWS += normalWS * (_Swell * Pulse());
            OUT.positionHCS = TransformWorldToHClip(positionWS);
            OUT.normalWS = normalWS;
            OUT.viewWS = GetWorldSpaceViewDir(positionWS);
            return OUT;
        }
        ENDHLSL

        Pass
        {
            Name "Rim"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One One            // additive: adds light, never repaints
            ZWrite Off
            ZTest LEqual
            Offset -1, -1            // lift off the source surface, no z-fighting
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragRim
            #pragma multi_compile_instancing

            half4 FragRim (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                float3 N = normalize(IN.normalWS);
                float3 V = normalize(IN.viewWS);
                float rim = pow(saturate(1.0 - saturate(dot(N, V))), _RimPower);
                return half4(_BaseColor.rgb * rim * _Intensity * Pulse(), 1.0);
            }
            ENDHLSL
        }

        // ⛔ LightMode MUST differ from the Rim pass. URP draws ONE pass per matching
        // shader tag, so with both tagged SRPDefaultUnlit only the first ever drew —
        // the rim appeared and the through-wall ghost silently never did. The forward
        // renderer matches SRPDefaultUnlit, UniversalForward and UniversalForwardOnly,
        // so giving each pass its own tag gets both drawn.
        Pass
        {
            Name "Occluded"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha One
            ZWrite Off
            ZTest Greater
            Cull Back

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragOccluded
            #pragma multi_compile_instancing

            half4 FragOccluded (Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);
                return half4(_BaseColor.rgb, _BaseColor.a * _Occluded * Pulse());
            }
            ENDHLSL
        }
    }

    FallBack Off
}
