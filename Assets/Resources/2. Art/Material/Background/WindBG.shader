Shader "Custom/WindBG"
{
    Properties
    {
        [Header(Colors)]
        _ColorStart("Color Start", Color) = (0.9, 0.95, 1, 0.6)
        _ColorMid("Color Mid", Color) = (0.7, 0.85, 1, 0.4)
        _ColorEnd("Color End", Color) = (0.5, 0.6, 0.8, 0.1)
        
        [Header(Motion)]
        _Speed("Base Speed", Range(0.1, 8)) = 3
        _SpeedVariation("Speed Variation", Range(0, 3)) = 1.5
        _WaveStrength("Wave Strength", Range(0, 0.3)) = 0.1
        _WaveFrequency("Wave Frequency", Range(1, 8)) = 3
        
        [Header(Streaks)]
        _Density("Streak Density", Range(1, 40)) = 15
        _WidthMin("Min Width", Range(0.01, 0.15)) = 0.03
        _WidthMax("Max Width", Range(0.03, 0.25)) = 0.08
        _LengthMin("Min Length", Range(0.2, 1)) = 0.4
        _LengthMax("Max Length", Range(0.5, 2)) = 0.9
        
        [Header(Noise)]
        _NoiseScale("Noise Scale", Range(1, 10)) = 4
        _NoiseStrength("Noise Strength", Range(0, 0.5)) = 0.2
        
        [Header(Edge Fade)]
        _EdgeSoftness("Edge Softness", Range(0, 1)) = 0.15
    }
    
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
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
            
            //Colors
            float4 _ColorStart;
            float4 _ColorMid;
            float4 _ColorEnd;
            
            //Motion
            float _Speed;
            float _SpeedVariation;
            float _WaveStrength;
            float _WaveFrequency;
            
            //Streaks
            float _Density;
            float _WidthMin;
            float _WidthMax;
            float _LengthMin;
            float _LengthMax;
            
            //Noise
            float _NoiseScale;
            float _NoiseStrength;
            
            //Edge
            float _EdgeSoftness;
            
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            float randomRange(float2 uv, float minVal, float maxVal)
            {
                return minVal + random(uv) * (maxVal - minVal);
            }
            
            float smoothNoise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float2 u = f * f * (3.0 - 2.0 * f);
                
                float a = random(i);
                float b = random(i + float2(1, 0));
                float c = random(i + float2(0, 1));
                float d = random(i + float2(1, 1));
                
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                //Edge fade
                float edgeFade = 1;
                edgeFade *= 1 - smoothstep(0, _EdgeSoftness, uv.y);
                edgeFade *= 1 - smoothstep(1 - _EdgeSoftness, 1, uv.y);
                
                //Use world position for consistent streaks
                float2 worldUV = uv * _Density;
                float gridY = worldUV.y;
                float cellIndex = floor(gridY);
                float cellOffset = frac(gridY);
                
                //Wavy motion
                float waveX = sin(uv.y * _WaveFrequency * 3.14159 * 2 + _Time.y * 2) * _WaveStrength;
                waveX += cos((uv.y * 5 + _Time.y) * 1.57) * _WaveStrength * 0.5;
                
                float totalAlpha = 0;
                float3 totalColor = float3(0, 0, 0);
                
                //Generate streaks - ALL moving RIGHT TO LEFT
                for(int layer = 0; layer < 3; layer++)
                {
                    float2 seed = float2(cellIndex, layer);
                    
                    //Random properties - speed is ALWAYS positive (right to left)
                    float streakSpeed = _Speed + randomRange(seed, -_SpeedVariation * 0.5, _SpeedVariation);
                    float streakWidth = randomRange(seed, _WidthMin, _WidthMax);
                    float streakLength = randomRange(seed, _LengthMin, _LengthMax);
                    float streakYOffset = random(seed) * 0.8 - 0.4;
                    float streakPhase = random(seed + 0.5) * 6.28318;
                    
                    //RIGHT TO LEFT movement: subtract time from position
                    //This ensures ALL streaks move from right edge to left edge
                    float streakWorldX = _Time.y * streakSpeed;
                    float streakLocalX = uv.x - streakWorldX + waveX;
                    
                    //Wrap and clamp to create continuous streaks moving RIGHT TO LEFT
                    streakLocalX = frac(streakLocalX / streakLength) * streakLength;
                    
                    //Vertical shape
                    float distToCenter = abs(cellOffset - 0.5 - streakYOffset);
                    float verticalAlpha = 1 - smoothstep(0, streakWidth, distToCenter);
                    
                    //Horizontal trail - always moving right to left
                    float t = streakLocalX;
                    float trailAlpha = 0;
                    
                    if(t > 0 && t < streakLength)
                    {
                        //Trail fades from RIGHT (bright) to LEFT (faded)
                        //Higher alpha at start (t near 0 = right side)
                        trailAlpha = 1 - (t / streakLength);
                        trailAlpha = pow(trailAlpha, 1.5);
                        
                        float trailNoise = sin(t * 20 + _Time.y * 10) * 0.3 + 0.7;
                        trailAlpha *= trailNoise;
                    }
                    
                    float streakAlpha = verticalAlpha * trailAlpha;
                    
                    //Noise and pulse
                    float noise = smoothNoise(float2(uv.x * _NoiseScale, uv.y * _NoiseScale + _Time.y));
                    streakAlpha *= (0.6 + noise * _NoiseStrength);
                    
                    float pulse = 0.7 + sin(_Time.y * streakSpeed * 2 + streakPhase) * 0.3;
                    streakAlpha *= pulse;
                    
                    //Layer intensity
                    float layerIntensity = (layer == 0) ? 1.0 : ((layer == 1) ? 0.5 : 0.25);
                    streakAlpha *= layerIntensity;
                    
                    //Color gradient - bright at right (t small), faded at left (t large)
                    float3 streakColor;
                    if(t < streakLength * 0.3)
                    {
                        float gradientT = t / (streakLength * 0.3);
                        streakColor = lerp(_ColorStart.rgb, _ColorMid.rgb, gradientT);
                    }
                    else
                    {
                        float gradientT = (t - streakLength * 0.3) / (streakLength * 0.7);
                        streakColor = lerp(_ColorMid.rgb, _ColorEnd.rgb, gradientT);
                    }
                    
                    //Color variation
                    float rVar = random(seed);
                    float gVar = random(seed + 0.1);
                    float bVar = random(seed + 0.2);
                    streakColor += float3(rVar * 0.15, gVar * 0.1, bVar * 0.08);
                    
                    totalAlpha += streakAlpha;
                    totalColor += streakColor * streakAlpha;
                }
                
                //Ambient background
                float ambientStrength = 0.12;
                float ambientNoise = smoothNoise(float2(uv.x * 2, uv.y * 2 + _Time.y * 0.5));
                totalAlpha += ambientNoise * ambientStrength;
                totalColor += _ColorEnd.rgb * ambientNoise * ambientStrength;
                
                //Blend colors
                float3 finalColor = totalColor / max(totalAlpha, 0.02);
                
                //Particles (also moving right to left)
                float particleDensity = _Density * 1.5;
                float particleGridY = uv.y * particleDensity;
                float particleCell = floor(particleGridY);
                float particleOffset = frac(particleGridY);
                float2 particleSeed = float2(particleCell, 5);
                
                if(random(particleSeed) < 0.12 && particleOffset < 0.1)
                {
                    float particleSpeed = _Speed * (random(particleSeed + 0.5) * 0.8 + 0.6);
                    float particleX = uv.x - frac(_Time.y * particleSpeed);
                    
                    if(particleX > 0.95)
                    {
                        float particleAlpha = (1 - (particleX - 0.95) / 0.05) * 0.5;
                        particleAlpha = saturate(particleAlpha);
                        totalAlpha += particleAlpha;
                        finalColor += float3(0.5, 0.45, 0.4) * particleAlpha;
                    }
                }
                
                //Final alpha
                float finalAlpha = saturate(totalAlpha) * edgeFade;
                
                return fixed4(finalColor, finalAlpha * _ColorStart.a);
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}