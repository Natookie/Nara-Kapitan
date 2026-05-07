Shader "Custom/Lightning"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Lightning Color", Color) = (1,1,1,1)
        _Intensity ("Intensity", Range(0,5)) = 1

        _Width ("Core Width", Range(0.001, 0.2)) = 0.02
        _GlowWidth ("Glow Width", Range(0.01, 0.5)) = 0.1

        _JitterStrength ("Jitter Strength", Float) = 0.05
        _JitterScale ("Jitter Scale", Float) = 20

        _Speed ("Animation Speed", Float) = 1
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            float4 _Color;
            float _Intensity;
            float _Width;
            float _GlowWidth;

            float _JitterStrength;
            float _JitterScale;
            float _Speed;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            float noise(float x)
            {
                return sin(x) * 0.5 + 0.5;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float time = _Time.y * _Speed;

                float2 uv = i.uv;

                float centerX = 0.5;
                float jitter = sin(uv.y * _JitterScale + time * 10) * _JitterStrength;
                jitter += sin(uv.y * (_JitterScale * 0.5) - time * 15) * (_JitterStrength * 0.5);

                float lightningX = centerX + jitter;

                float dist = abs(uv.x - lightningX);
                float core = smoothstep(_Width, 0.0, dist);
                float glow = smoothstep(_GlowWidth, 0.0, dist);

                float intensity = core * 2.0 + glow;

                float flicker = sin(time * 50) * 0.5 + 0.5;
                flicker = lerp(0.7, 1.0, flicker);

                intensity *= flicker;
                intensity *= _Intensity;

                float fade = pow(1.0 - uv.y, 1.5);
                intensity *= fade;

                float alpha = saturate(intensity);

                return float4(_Color.rgb * intensity, alpha);
            }
            ENDCG
        }
    }
}