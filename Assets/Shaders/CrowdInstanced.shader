Shader "UltimatePenaltyCup/CrowdInstanced"
{
    Properties
    {
        _Smoothness ("Smoothness", Range(0, 1)) = 0.15
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }
        LOD 150
        Cull Back

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 color      : COLOR; // R=armMask, G=skinMask, B=pantsMask, A=flagMask
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half3  lighting   : TEXCOORD0;
                float4 vertColor  : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float _CrowdTime;
            float _CrowdExcitement;
            float _CrowdMode; // 0=Idle, 1=Tension, 2=GoalJump, 3=SaveGasp, 4=MissSlump

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _ShirtColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _PantsColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SkinColor)
                UNITY_DEFINE_INSTANCED_PROP(float4, _AnimParams) // x=phase, y=speed, z=style(0..2), w=heightScale
            UNITY_INSTANCING_BUFFER_END(Props)

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                float4 anim = UNITY_ACCESS_INSTANCED_PROP(Props, _AnimParams);
                float phase = anim.x;
                float spd = anim.y;
                float style = anim.z;

                float3 pos = IN.positionOS.xyz;
                float t = _CrowdTime * spd + phase;

                float armMask  = IN.color.r;
                float skinMask = IN.color.g;
                float flagMask = IN.color.a;

                // Idle gentle sway + rhythmic clap
                float idleSway = sin(t * 1.4) * 0.025;
                pos.x += idleSway * smoothstep(0.3, 1.3, pos.y);

                // Arm elevation & applause wave
                float armRaise = 0.0;
                if (_CrowdMode < 0.5)
                {
                    // Gentle applause / wave variation per spectator style
                    armRaise = (style > 1.2) ? (0.12 + abs(sin(t * 2.8)) * 0.14) : (0.04 + sin(t * 1.6) * 0.05);
                }
                else if (_CrowdMode < 1.5)
                {
                    // Tension lean forward & anticipatory clap
                    pos.z += 0.06 * smoothstep(0.6, 1.4, pos.y);
                    armRaise = 0.14 + abs(sin(t * 4.5)) * 0.16 * _CrowdExcitement;
                }
                else if (_CrowdMode < 2.5)
                {
                    // Goal celebration jump + raised cheering arms
                    float jump = abs(sin(t * 5.2)) * 0.24 * _CrowdExcitement;
                    pos.y += jump;
                    armRaise = (0.34 + sin(t * 8.0) * 0.08) * _CrowdExcitement;
                }
                else if (_CrowdMode < 3.5)
                {
                    // Save gasp / hands up or leaning
                    float hop = abs(sin(t * 3.8)) * 0.10 * _CrowdExcitement;
                    pos.y += hop;
                    armRaise = (0.26 + sin(t * 5.0) * 0.06) * _CrowdExcitement;
                }
                else
                {
                    // Miss / slumped arms
                    pos.y -= 0.05 * _CrowdExcitement * smoothstep(0.5, 1.4, pos.y);
                    armRaise = -0.04 + sin(t * 2.0) * 0.04;
                }

                pos.y += armMask * armRaise;
                pos.z += armMask * sin(t * 3.5) * 0.05;

                // Flag / Scarf flutter animation
                if (flagMask > 0.1)
                {
                    float wave = sin(_CrowdTime * 6.5 + phase + pos.y * 5.0) * 0.12 * (0.5 + 0.8 * _CrowdExcitement);
                    pos.x += wave * flagMask;
                    pos.z += cos(_CrowdTime * 5.2 + phase) * 0.08 * flagMask;
                }

                OUT.positionCS = TransformObjectToHClip(pos);
                float3 normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                Light mainLight = GetMainLight();
                float ndotl = saturate(dot(normalWS, mainLight.direction) * 0.65 + 0.35);
                OUT.lighting = half3(mainLight.color * ndotl + SampleSH(normalWS) * 0.65);
                OUT.vertColor = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                float4 shirt = UNITY_ACCESS_INSTANCED_PROP(Props, _ShirtColor);
                float4 pants = UNITY_ACCESS_INSTANCED_PROP(Props, _PantsColor);
                float4 skin  = UNITY_ACCESS_INSTANCED_PROP(Props, _SkinColor);

                // Select base color by anatomical vertex channel (G=skin/head/hands, B=pants, A=flag gold/team)
                float3 albedo = shirt.rgb;
                if (IN.vertColor.g > 0.5) albedo = skin.rgb;
                else if (IN.vertColor.b > 0.5) albedo = pants.rgb;
                else if (IN.vertColor.a > 0.5) albedo = lerp(shirt.rgb, float3(1.0, 0.82, 0.18), 0.55);

                return half4(albedo * IN.lighting, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
