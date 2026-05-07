Shader "Custom/ImpactFlash"
{
    Properties
    {
        _Radius ("Radius", Range(0,1)) = 0.3
        _Softness ("Softness", Range(0.001,0.5)) = 0.1
        _Intensity ("Intensity", Range(0,5)) = 1
    }

    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }

        ZWrite Off
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            float _Radius;
            float _Softness;
            float _Intensity;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);

                float mask = smoothstep(_Radius, _Radius - _Softness, dist);

                float3 color = lerp(float3(0,0,0), float3(1,1,1) * _Intensity, mask);

                return float4(color, mask);
            }
            ENDCG
        }
    }
}