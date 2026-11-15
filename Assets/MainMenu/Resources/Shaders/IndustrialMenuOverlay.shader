Shader "UI/Industrial Menu Overlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _ChromaTint ("Glitch Accent", Color) = (0.28,0.92,0.76,1)
        _NoiseTint ("Noise Tint", Color) = (0.012,0.028,0.022,1)
        _FxIntensity ("FX Intensity", Range(0,1)) = 0.55
        _FxQuality ("FX Quality", Range(0,1)) = 0.65
        _GlitchAmount ("Glitch Amount", Range(0,1)) = 0
        _GlitchSeed ("Glitch Seed", Float) = 0
        _EffectTime ("Unscaled Effect Time", Float) = 0
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
            Name "IndustrialOverlay"

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _ChromaTint;
            fixed4 _NoiseTint;
            float4 _ClipRect;
            float _FxIntensity;
            float _FxQuality;
            float _GlitchAmount;
            float _GlitchSeed;
            float _EffectTime;

            v2f vert(appdata_t input)
            {
                v2f output;
                output.worldPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = input.texcoord;
                output.color = input.color * _Color;
                return output;
            }

            float Hash21(float2 value)
            {
                value = frac(value * float2(123.34, 456.21));
                value += dot(value, value + 45.32);
                return frac(value.x * value.y);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                float2 uv = input.uv;
                float strength = saturate(_FxIntensity) * saturate(_FxQuality);
                float glitch = saturate(_GlitchAmount) * step(0.001, strength);

                float time = _EffectTime;
                float scan = pow(0.5 + 0.5 * sin(uv.y * _ScreenParams.y * 3.14159265), 12.0);
                float grain = Hash21(floor(uv * _ScreenParams.xy * 0.6) + floor(time * 18.0));
                float sweepY = frac(time * 0.07);
                float sweep = 1.0 - smoothstep(0.0, 0.035, abs(uv.y - sweepY));
                float2 centered = uv * 2.0 - 1.0;
                float vignette = smoothstep(0.55, 1.55, dot(centered, centered));

                // Sparse horizontal sync tears, with separated chroma edges and short data blocks.
                float row = floor(uv.y * 92.0);
                float rowNoise = Hash21(float2(row, floor(_GlitchSeed * 7.0)));
                float tear = step(0.91, rowNoise);
                float segment = step(0.40, Hash21(float2(floor(uv.x * 18.0), row + _GlitchSeed)));
                float chromaEdge = step(0.65, frac(uv.y * 92.0));
                float block = step(0.982, Hash21(floor(uv * float2(48, 32)) + _GlitchSeed));
                float rollingLine = 1.0 - smoothstep(0.0, 0.003, abs(uv.y-frac(_GlitchSeed*0.13)));
                float distress = (tear * segment * 0.50 + block * 0.25 + rollingLine * 0.65) * glitch;
                float3 amber = float3(1.0, 0.54, 0.13);
                float3 finalColor = lerp(_NoiseTint.rgb, amber, sweep * 0.4);
                finalColor = lerp(finalColor, lerp(amber, _ChromaTint.rgb, chromaEdge), saturate(distress * 3.0));
                float alpha = strength * (scan * 0.018 + grain * 0.009 + sweep * 0.022 + vignette * 0.13);
                alpha += distress * lerp(0.35, 0.75, grain);

                fixed4 result = fixed4(finalColor, alpha) * input.color;
                result.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);
                #ifdef UNITY_UI_ALPHACLIP
                clip(result.a - 0.001);
                #endif
                return result;
            }
            ENDCG
        }
    }
}
