Shader "UI/Industrial Menu Surface"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [PerRendererData] _TextureSampleAdd ("Texture Sample Add", Vector) = (0,0,0,0)
        _FxIntensity ("FX Intensity", Range(0,1)) = 0.55
        _FxQuality ("FX Quality", Range(0,1)) = 0.65
        _Hover ("Hover", Range(0,1)) = 0
        _Pressed ("Pressed", Range(0,1)) = 0
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
            Name "IndustrialSurface"

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
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float _FxIntensity;
            float _FxQuality;
            float _Hover;
            float _Pressed;

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
                value = frac(value * float2(127.1, 311.7));
                value += dot(value, value + 19.19);
                return frac(value.x * value.y);
            }

            fixed4 frag(v2f input) : SV_Target
            {
                fixed4 color = (tex2D(_MainTex, input.uv) + _TextureSampleAdd) * input.color;
                float strength = saturate(_FxIntensity) * saturate(_FxQuality);

                float sweepPhase = frac(input.uv.x * 0.72 + input.uv.y * 0.28 - _Time.y * 0.24);
                float sweep = (1.0 - smoothstep(0.0, 0.085, abs(sweepPhase - 0.5))) * saturate(_Hover);
                float scan = 0.5 + 0.5 * sin(input.uv.y * _ScreenParams.y * 2.35);
                float grain = Hash21(floor(input.uv * _ScreenParams.xy * 0.28));

                float2 edgeDistance = min(input.uv, 1.0 - input.uv);
                float rim = 1.0 - smoothstep(0.0, 0.075, min(edgeDistance.x, edgeDistance.y));
                float amber = strength * (sweep * 0.52 + _Hover * 0.16 + rim * _Hover * 0.08);

                color.rgb *= 1.0 + strength * (scan * 0.018 + (grain - 0.5) * 0.022);
                color.rgb += float3(1.0, 0.34, 0.025) * amber * color.a;
                color.rgb *= lerp(1.0, 0.88, saturate(_Pressed));
                color.a *= UnityGet2DClipping(input.worldPosition.xy, _ClipRect);

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
