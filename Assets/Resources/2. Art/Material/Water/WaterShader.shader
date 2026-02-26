Shader "Custom/StormyWater"
{
    Properties
    {
        _SurfaceColor ("Surface Color", Color) = (0.05, 0.1, 0.2, 0.9)
        _DeepColor ("Deep Color", Color) = (0.02, 0.05, 0.1, 0.9)
        _FoamColor ("Foam Color", Color) = (0.9, 0.9, 1.0, 0.8)
        _StormIntensity ("Storm Intensity", Range(0, 1)) = 0.3
        _FoamIntensity ("Foam Intensity", Range(0, 2)) = 1.0
        _FoamSpread ("Foam Spread", Range(0, 1)) = 0.3
        _WaveDarkening ("Wave Darkening", Range(0, 1)) = 0.5
        _MaxDepth ("Max Depth", Float) = 15.0
        _CustomTime ("Time", Float) = 0.0
        _WaveScale ("Wave Scale", Float) = 0.3
        _WaveSpeed ("Wave Speed", Float) = 1.5
        _DepthGradientPower ("Depth Gradient Power", Range(0.5, 3)) = 1.5
        _FoamSoftness ("Foam Softness", Range(0, 1)) = 0.3
        _SurfaceLevel ("Surface Level", Float) = 0.0 // Added: World Y position of water surface
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Transparent+100" 
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off
        
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
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float depth : TEXCOORD1; // Vertical depth (0=surface, 1=max depth)
                float3 worldPos : TEXCOORD2;
                float waveHeight : TEXCOORD3;
            };
            
            // Properties
            float4 _SurfaceColor;
            float4 _DeepColor;
            float4 _FoamColor;
            float _StormIntensity;
            float _FoamIntensity;
            float _FoamSpread;
            float _WaveDarkening;
            float _MaxDepth;
            float _CustomTime;
            float _WaveScale;
            float _WaveSpeed;
            float _DepthGradientPower;
            float _FoamSoftness;
            float _SurfaceLevel;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                
                // CORRECT DEPTH CALCULATION:
                // Calculate depth based on world Y position relative to surface level
                // surfaceLevel is the world Y coordinate of the water surface
                float depthFromSurface = max(0.0, _SurfaceLevel - o.worldPos.y);
                
                // Convert to 0-1 range (0 at surface, 1 at max depth)
                o.depth = saturate(depthFromSurface / _MaxDepth);
                
                // Calculate wave pattern for foam placement
                // Use world X position for consistent waves across the ocean
                float wavePattern = sin(o.worldPos.x * _WaveScale + _CustomTime * _WaveSpeed) * 
                                   cos(o.worldPos.x * _WaveScale * 0.7 + _CustomTime * _WaveSpeed * 0.8);
                
                // Storm makes waves higher and more chaotic
                float stormEffect = 1.0 + _StormIntensity * 1.5;
                wavePattern *= stormEffect;
                
                // Add some secondary waves for complexity
                float secondaryWaves = sin(o.worldPos.x * _WaveScale * 1.3 + _CustomTime * _WaveSpeed * 1.2) * 0.3;
                wavePattern += secondaryWaves;
                
                // Store normalized wave height (0-1 range)
                o.waveHeight = wavePattern * 0.5 + 0.5;
                
                return o;
            }
            
            // Smooth foam function
            float smoothFoam(float height, float threshold, float spread)
            {
                return smoothstep(threshold - spread, threshold + spread, height);
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // 1. DEPTH-BASED COLOR GRADIENT (top to bottom)
                // This is the main gradient from surface to deep water
                float depthFactor = pow(i.depth, _DepthGradientPower);
                fixed4 waterColor = lerp(_SurfaceColor, _DeepColor, depthFactor);
                
                // 2. WAVE PATTERN DARKENING (troughs are darker)
                // Only apply to upper portion of water
                float waveDarkeningFactor = 1.0;
                if (i.depth < 0.3) // Only near surface
                {
                    // Troughs (low waveHeight) get darker during storms
                    waveDarkeningFactor = 1.0 - (_WaveDarkening * _StormIntensity * (1.0 - i.waveHeight));
                    waterColor.rgb *= waveDarkeningFactor;
                }
                
                // 3. FOAM CALCULATION
                float foam = 0.0;
                
                // Only add foam near the surface
                if (i.depth < 0.15) // Very top of water
                {
                    // A. Base foam at wave peaks
                    // Higher waveHeight = more foam
                    float peakFoam = smoothstep(0.6, 0.9, i.waveHeight);
                    
                    // B. Storm foam (increases with storm intensity)
                    float stormFoam = _StormIntensity * smoothstep(0.4, 0.8, i.waveHeight);
                    
                    // C. Animated foam lines
                    float timeOffset = _CustomTime * 2.0;
                    float foamLines = sin(i.worldPos.x * 0.8 + timeOffset) * 0.5 + 0.5;
                    foamLines = smoothstep(0.4, 0.6, foamLines);
                    
                    // D. Combine all foam sources
                    foam = peakFoam * (1.0 + _StormIntensity);
                    foam = max(foam, stormFoam);
                    foam = max(foam, foamLines * 0.3);
                    
                    // E. Apply spread for smoother edges
                    foam = smoothstep(0.2 - _FoamSpread, 0.2 + _FoamSpread, foam);
                    
                    // F. Fade out foam as we go deeper
                    foam *= (1.0 - smoothstep(0.0, 0.15, i.depth));
                }
                
                // Apply foam intensity
                foam = saturate(foam * _FoamIntensity);
                
                // 4. FINAL COLOR COMPOSITION
                fixed4 finalColor = waterColor;
                float finalAlpha = waterColor.a;
                
                // Apply foam with proper alpha blending
                if (foam > 0.01)
                {
                    // Use foam's alpha for blending
                    float foamAlpha = smoothstep(0.01, 0.5, foam) * _FoamColor.a;
                    
                    // Foam softens the edges
                    float foamEdgeSoftness = _FoamSoftness;
                    foamAlpha = smoothstep(foamEdgeSoftness, 1.0 - foamEdgeSoftness, foamAlpha);
                    
                    // Blend colors using foam alpha
                    // Foam adds its color on top of water
                    finalColor.rgb = lerp(waterColor.rgb, _FoamColor.rgb, foamAlpha);
                    
                    // Foam makes water more opaque
                    finalAlpha = lerp(waterColor.a, 1.0, foamAlpha * 0.5);
                    
                    // Add a subtle highlight to foam peaks
                    float highlight = smoothstep(0.5, 0.9, foam);
                    finalColor.rgb += _FoamColor.rgb * highlight * 0.1;
                }
                
                // 5. DEEP WATER DARKENING (smooth transition)
                // This is separate from the gradient - additional darkening in very deep water
                float deepDarkening = smoothstep(0.5, 0.9, i.depth);
                finalColor.rgb *= lerp(1.0, 0.7, deepDarkening);
                
                // 6. STORM OVERALL DARKENING (subtle)
                finalColor.rgb *= lerp(1.0, 0.9, _StormIntensity * 0.3);
                
                finalColor.a = finalAlpha;
                
                return finalColor;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}