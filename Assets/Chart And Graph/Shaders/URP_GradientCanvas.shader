Shader "Chart/Canvas/URP_Gradient"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Angle("Angle", Float) = 0
        _Combine("Combine", Color) = (1,1,1,0)
        _ColorFrom("From", Color) = (1,1,1,1)
        _ColorTo("To", Color) = (1,1,1,1)
        _ChartTiling("Tiling", Float) = 1

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
            "RenderPipeline"="UniversalPipeline"
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
            Name "Default"
            HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #pragma target 2.0

                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

                #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
                #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

                half _Angle;
                half _ChartTiling;
                half4 _Combine;
                half4 _ColorFrom;
                half4 _ColorTo;
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);
                float4 _MainTex_ST;
                float4 _ClipRect;

                // URP用の2Dクリッピング関数
                half UnityGet2DClipping(in float2 position, in float4 clipRect)
                {
                    half2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
                    return inside.x * inside.y;
                }

                struct appdata_t
                {
                    float4 vertex   : POSITION;
                    float2 texcoord : TEXCOORD0;
                    UNITY_VERTEX_INPUT_INSTANCE_ID
                };

                struct v2f
                {
                    float2 uv : TEXCOORD0;
                    float4 worldPosition : TEXCOORD1;
                    float4 pos : SV_POSITION;
                    half4 color : COLOR;
                    UNITY_VERTEX_OUTPUT_STEREO
                };

                v2f vert(appdata_t v)
                {
                    v2f res;
                    UNITY_SETUP_INSTANCE_ID(v);
                    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(res);
                    res.worldPosition = v.vertex;
                    res.pos = TransformObjectToHClip(v.vertex.xyz);
                    
                    half angle = _Angle * 3.14159 * 2 / 360.0;
                    half lerpValue = (v.texcoord.x / _ChartTiling - 0.5) * sin(angle) + (v.texcoord.y - 0.5) * cos(angle);
                    lerpValue = lerpValue + 0.5;
                    res.color = lerp(_ColorFrom, _ColorTo, lerpValue);
                    half combineAlpha = _Combine.a;
                    half alpha = res.color.a;
                    res.color = lerp(res.color, _Combine, combineAlpha);
                    res.color.a = alpha;
                    res.uv = TRANSFORM_TEX(v.texcoord, _MainTex);
                    return res;
                }

                half4 frag(v2f v) : SV_Target
                {
                    half4 texData = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, v.uv) * v.color;
                    
                    #ifdef UNITY_UI_CLIP_RECT
                    texData.a *= UnityGet2DClipping(v.worldPosition.xy, _ClipRect);
                    #endif
                    
                    #ifdef UNITY_UI_ALPHACLIP
                    clip (texData.a - 0.001);
                    #endif
                    
                    return texData;
                }
            ENDHLSL
        }
    }
}

