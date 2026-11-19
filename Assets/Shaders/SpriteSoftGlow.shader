Shader "UI/SpriteSoftGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _GlowColor ("Glow Color", Color) = (1,1,1,1)
        _GlowSize ("Glow Size", Range(0, 15)) = 6.0
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2.2

        // Standard Unity UI Stencil properties
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex        : SV_POSITION;
                fixed4 color         : COLOR;
                float2 texcoord      : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;

            fixed4 _GlowColor;
            float _GlowSize;
            float _GlowIntensity;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            float SampleAlphaSafe(float2 uv)
            {
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                    return 0.0;
                return tex2D(_MainTex, uv).a;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                half4 texColor = (tex2D(_MainTex, IN.texcoord) + _TextureSampleAdd) * IN.color;

                // Soft radial blur of the sprite's alpha in 3 concentric rings (12 directions each)
                float radius = _GlowSize * 0.01;
                float acc = 0.0;
                float totalWeight = 0.0;

                if (radius > 0.0001 && _GlowColor.a > 0.001)
                {
                    const int DIRECTIONS = 12;
                    const float TWO_PI = 6.2831853;

                    for (int i = 0; i < DIRECTIONS; i++)
                    {
                        float angle = (TWO_PI * i) / DIRECTIONS;
                        float2 dir = float2(cos(angle), sin(angle));

                        // Ring 1 (inner, strongest)
                        acc += SampleAlphaSafe(IN.texcoord + dir * (radius * 0.33)) * 1.0;
                        // Ring 2 (middle)
                        acc += SampleAlphaSafe(IN.texcoord + dir * (radius * 0.66)) * 0.6;
                        // Ring 3 (outer, softest)
                        acc += SampleAlphaSafe(IN.texcoord + dir * radius) * 0.25;

                        totalWeight += 1.85;
                    }
                }

                float glowAlpha = (totalWeight > 0.0)
                    ? saturate((acc / totalWeight) * _GlowIntensity) * _GlowColor.a
                    : 0.0;

                // Composite original sprite OVER the soft glow
                half outAlpha = texColor.a + glowAlpha * (1.0 - texColor.a);
                half3 outRGB = (outAlpha > 0.001)
                    ? (texColor.rgb * texColor.a + _GlowColor.rgb * glowAlpha * (1.0 - texColor.a)) / outAlpha
                    : half3(0.0, 0.0, 0.0);

                half4 finalColor = half4(outRGB, outAlpha);

                #ifdef UNITY_UI_CLIP_RECT
                finalColor.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip(finalColor.a - 0.001);
                #endif

                return finalColor;
            }
            ENDCG
        }
    }
}