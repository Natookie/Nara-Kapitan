Shader "Custom/2DWaterShader"
{
    Properties
    {
        //Main water color properties
        [MainColor] _WaterTopColor("Water Top Color", Color) = (0.2, 0.6, 0.8, 1)
        [MainColor] _WaterBottomColor("Water Bottom Color", Color) = (0.1, 0.3, 0.5, 1)
        
        //Outline properties
        _OutlineColor("Outline Color", Color) = (0.05, 0.2, 0.4, 1)
        _OutlineWidth("Outline Width", Range(0, 0.1)) = 0.02
        
        //Noise properties
        _NoiseScale("Noise Scale", Range(0.1, 10)) = 2.0
        _NoiseIntensity("Noise Intensity", Range(0, 1)) = 0.5
        _ScrollSpeed("Scroll Speed", Range(0, 2)) = 0.5
        
        //Water noise scroll direction
        [KeywordEnum(RightToLeft, LeftToRight, Up, Down, BothHorizontal, BothVertical)] _WaterScrollDirection("Water Scroll Direction", Float) = 0
        
        //Circle/Bubble properties
        _CircleDensity("Circle Density", Range(0, 1)) = 0.3
        _CircleSize("Circle Size", Range(0.01, 0.2)) = 0.05
        _CircleColor("Circle Color", Color) = (0.8, 0.9, 1, 0.6)
        _CircleTopColor("Circle Top Color", Color) = (0.9, 1.0, 1.0, 0.8)
        _CircleBottomColor("Circle Bottom Color", Color) = (0.6, 0.7, 0.9, 0.4)
        _CircleScrollSpeed("Circle Scroll Speed", Range(0, 2)) = 0.8
        [Toggle] _CircleScrollWithNoise("Circles Scroll With Noise", Float) = 1
        _CircleGridSize("Circle Grid Size", Range(5, 50)) = 20
        
        //Circle scroll direction
        [KeywordEnum(RightToLeft, LeftToRight, Up, Down, BothHorizontal, BothVertical)] _CircleScrollDirection("Circle Scroll Direction", Float) = 0
        
        //Additional wave properties
        _WaveFrequency("Wave Frequency", Range(0.1, 5)) = 1.5
        _WaveAmplitude("Wave Amplitude", Range(0, 0.1)) = 0.02
        
        //Render order properties
        [Enum(UnityEngine.Rendering.CompareFunction)] _ZTest("Z Test", Float) = 4 //4 = LessEqual
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 0 //0 = Off
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Source Blend", Float) = 5 //5 = SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Destination Blend", Float) = 10 //10 = OneMinusSrcAlpha
    }
    
    SubShader
    {
        Tags { 
            "Queue"="Transparent+1" 
            "RenderType"="Transparent"
            "ForceNoShadowCasting"="True"
            "IgnoreProjector"="True"
        }
        
        Blend [_SrcBlend] [_DstBlend]
        Cull [_Cull]
        ZWrite Off
        ZTest [_ZTest]
        
        Pass
        {
            Name "Water Pass"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature _WATERSCROLLDIRECTION_RIGHTTOLEFT _WATERSCROLLDIRECTION_LEFTTORIGHT _WATERSCROLLDIRECTION_UP _WATERSCROLLDIRECTION_DOWN _WATERSCROLLDIRECTION_BOTH_HORIZONTAL _WATERSCROLLDIRECTION_BOTH_VERTICAL
            #pragma shader_feature _CIRCLESCROLLDIRECTION_RIGHTTOLEFT _CIRCLESCROLLDIRECTION_LEFTTORIGHT _CIRCLESCROLLDIRECTION_UP _CIRCLESCROLLDIRECTION_DOWN _CIRCLESCROLLDIRECTION_BOTH_HORIZONTAL _CIRCLESCROLLDIRECTION_BOTH_VERTICAL
            #include "UnityCG.cginc"
            #include "UnityShaderVariables.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float4 worldPos : TEXCOORD1;
            };

            //Properties
            float4 _WaterTopColor;
            float4 _WaterBottomColor;
            float4 _OutlineColor;
            float _OutlineWidth;
            float _NoiseScale;
            float _NoiseIntensity;
            float _ScrollSpeed;
            float _CircleDensity;
            float _CircleSize;
            float4 _CircleColor;
            float4 _CircleTopColor;
            float4 _CircleBottomColor;
            float _CircleScrollSpeed;
            float _CircleScrollWithNoise;
            float _CircleGridSize;
            float _WaveFrequency;
            float _WaveAmplitude;
            float _ZTest;
            float _Cull;
            float _SrcBlend;
            float _DstBlend;
            
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }
            
            float random2(float2 uv)
            {
                return frac(sin(dot(uv, float2(42.4532, 93.847))) * 54761.3847);
            }
            
            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                
                float a = random(i);
                float b = random(i + float2(1, 0));
                float c = random(i + float2(0, 1));
                float d = random(i + float2(1, 1));
                
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }
            
            float simpleNoise(float2 uv)
            {
                return sin(uv.x * _WaveFrequency + _Time.y * _ScrollSpeed) * 
                       cos(uv.y * _WaveFrequency * 0.5) * 0.5 + 0.5;
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                
                float2 waterScrollOffset = float2(0, 0);
                #if defined(_WATERSCROLLDIRECTION_RIGHTTOLEFT)
                    waterScrollOffset.x = -_Time.y * _ScrollSpeed;
                #elif defined(_WATERSCROLLDIRECTION_LEFTTORIGHT)
                    waterScrollOffset.x = _Time.y * _ScrollSpeed;
                #elif defined(_WATERSCROLLDIRECTION_UP)
                    waterScrollOffset.y = _Time.y * _ScrollSpeed;
                #elif defined(_WATERSCROLLDIRECTION_DOWN)
                    waterScrollOffset.y = -_Time.y * _ScrollSpeed;
                #elif defined(_WATERSCROLLDIRECTION_BOTH_HORIZONTAL)
                    waterScrollOffset.x = _Time.y * _ScrollSpeed;
                #elif defined(_WATERSCROLLDIRECTION_BOTH_VERTICAL)
                    waterScrollOffset.y = _Time.y * _ScrollSpeed;
                #endif
                
                float waveOffset = simpleNoise(v.uv * _NoiseScale + waterScrollOffset) * _WaveAmplitude;
                float4 displacedVertex = v.vertex;
                displacedVertex.x += waveOffset;
                
                o.vertex = UnityObjectToClipPos(displacedVertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, displacedVertex);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                
                //Water noise scroll direction
                float2 waterScrollOffset = float2(0, 0);
                #if defined(_WATERSCROLLDIRECTION_RIGHTTOLEFT)
                    waterScrollOffset.x = -_Time.y * _ScrollSpeed;
                #elif defined(_WATERSCROLLDIRECTION_LEFTTORIGHT)
                    waterScrollOffset.x = _Time.y * _ScrollSpeed;
                #elif defined(_WATERSCROLLDIRECTION_UP)
                    waterScrollOffset.y = _Time.y * _ScrollSpeed;
                #elif defined(_WATERSCROLLDIRECTION_DOWN)
                    waterScrollOffset.y = -_Time.y * _ScrollSpeed;
                #elif defined(_WATERSCROLLDIRECTION_BOTH_HORIZONTAL)
                    waterScrollOffset.x = _Time.y * _ScrollSpeed;
                #elif defined(_WATERSCROLLDIRECTION_BOTH_VERTICAL)
                    waterScrollOffset.y = _Time.y * _ScrollSpeed;
                #endif
                
                float2 scrollUV = uv * _NoiseScale + waterScrollOffset;
                float noiseValue = noise(scrollUV);
                
                float2 scrollUV2 = uv * (_NoiseScale * 2.5) - waterScrollOffset * 1.3;
                float noiseValue2 = noise(scrollUV2);
                noiseValue = (noiseValue + noiseValue2) * 0.5;
                
                float topBias = 1.0 - uv.y;
                float biasedNoise = noiseValue * (0.4 + topBias * 0.8);
                float finalNoise = biasedNoise * _NoiseIntensity;
                
                //Gradient (Top lighter, Bottom darker)
                float gradientFactor = uv.y;
                float4 gradientColor = lerp(_WaterBottomColor, _WaterTopColor, gradientFactor);
                
                float4 waterColor = gradientColor;
                waterColor.rgb += float3(finalNoise * 0.8, finalNoise * 0.6, finalNoise);
                waterColor.rgb = saturate(waterColor.rgb);
                
                //Outline
                float outline = 0;
                if (uv.x < _OutlineWidth || uv.x > 1.0 - _OutlineWidth || 
                    uv.y < _OutlineWidth || uv.y > 1.0 - _OutlineWidth)
                {
                    outline = 1;
                }
                
                //Circular texture with independent direction control
                float circles = 0;
                float4 circleColorResult = _CircleColor;
                
                float2 circleScrollOffset = float2(0, 0);
                if (_CircleScrollWithNoise > 0.5)
                {
                    #if defined(_CIRCLESCROLLDIRECTION_RIGHTTOLEFT)
                        circleScrollOffset.x = -_Time.y * _CircleScrollSpeed;
                    #elif defined(_CIRCLESCROLLDIRECTION_LEFTTORIGHT)
                        circleScrollOffset.x = _Time.y * _CircleScrollSpeed;
                    #elif defined(_CIRCLESCROLLDIRECTION_UP)
                        circleScrollOffset.y = _Time.y * _CircleScrollSpeed;
                    #elif defined(_CIRCLESCROLLDIRECTION_DOWN)
                        circleScrollOffset.y = -_Time.y * _CircleScrollSpeed;
                    #elif defined(_CIRCLESCROLLDIRECTION_BOTH_HORIZONTAL)
                        circleScrollOffset.x = _Time.y * _CircleScrollSpeed;
                    #elif defined(_CIRCLESCROLLDIRECTION_BOTH_VERTICAL)
                        circleScrollOffset.y = _Time.y * _CircleScrollSpeed;
                    #endif
                }
                
                //Generate circle grid with random offsets
                float2 circleUV = uv * _CircleGridSize + circleScrollOffset;
                float2 grid = floor(circleUV);
                float2 randomOffset = float2(random(grid), random2(grid + float2(0.5, 0.5)));
                randomOffset = (randomOffset - 0.5) * 0.8;
                float2 cellUV = frac(circleUV) - 0.5;
                cellUV += randomOffset * 0.5;
                
                float circleChance = random(grid);
                float randomSize = random(grid + 0.237) * 0.7 + 0.3;
                float randomSize2 = random2(grid + 0.742) * 0.5 + 0.5;
                float actualCircleSize = _CircleSize * randomSize * (0.7 + randomSize2 * 0.6);
                float yOffset = (random(grid + 0.123) - 0.5) * 0.3;
                
                if (circleChance < _CircleDensity)
                {
                    float dist = length(cellUV);
                    float circleValue = 1.0 - smoothstep(0, actualCircleSize, dist);
                    circles = max(circles, circleValue);
                    
                    float pulsePhase = random(grid + 0.345) * 6.28318;
                    circles *= (0.6 + sin(_Time.y * 2 + pulsePhase) * 0.4);
                    circles *= random(grid + 0.5) * 0.7 + 0.3;
                    
                    float circleGradientFactor = saturate(uv.y + yOffset);
                    circleColorResult = lerp(_CircleBottomColor, _CircleTopColor, circleGradientFactor);
                    
                    float3 colorVariation = float3(
                        random(grid + 0.1) * 0.2,
                        random(grid + 0.2) * 0.15,
                        random(grid + 0.3) * 0.1
                    );
                    circleColorResult.rgb += colorVariation;
                }
                
                //Second layer of smaller circles
                float2 circleUV2 = uv * (_CircleGridSize * 1.4) - circleScrollOffset * 0.8;
                float2 grid2 = floor(circleUV2);
                float2 randomOffset2 = float2(random2(grid2), random(grid2 + float2(0.3, 0.7)));
                randomOffset2 = (randomOffset2 - 0.5) * 0.7;
                float2 cellUV2 = frac(circleUV2) - 0.5;
                cellUV2 += randomOffset2 * 0.6;
                
                float circleChance2 = random2(grid2);
                
                if (circleChance2 < _CircleDensity * 0.35)
                {
                    float dist = length(cellUV2);
                    float circleValue = 1.0 - smoothstep(0, _CircleSize * 0.4, dist);
                    circles = max(circles, circleValue * 0.4);
                    
                    float circleGradientFactor2 = saturate(uv.y + (random(grid2 + 0.8) - 0.5) * 0.2);
                    float4 smallCircleColor = lerp(_CircleBottomColor, _CircleTopColor, circleGradientFactor2);
                    circleColorResult = lerp(circleColorResult, smallCircleColor, circleValue * 0.5);
                }
                
                //Third layer - occasional large bubbles
                float2 circleUV3 = uv * (_CircleGridSize * 0.7) + circleScrollOffset * 1.2;
                float2 grid3 = floor(circleUV3);
                float2 randomOffset3 = float2(random(grid3 + 0.9), random2(grid3 + 0.4));
                randomOffset3 = (randomOffset3 - 0.5) * 1.0;
                float2 cellUV3 = frac(circleUV3) - 0.5;
                cellUV3 += randomOffset3 * 0.8;
                
                float circleChance3 = random(grid3 + 0.55);
                
                if (circleChance3 < _CircleDensity * 0.15)
                {
                    float dist = length(cellUV3);
                    float circleValue = 1.0 - smoothstep(0, _CircleSize * 1.2, dist);
                    circles = max(circles, circleValue * 0.6);
                    
                    float circleGradientFactor3 = saturate(uv.y);
                    float4 largeCircleColor = lerp(_CircleBottomColor, _CircleTopColor, circleGradientFactor3);
                    largeCircleColor.rgb += float3(0.1, 0.05, 0);
                    circleColorResult = lerp(circleColorResult, largeCircleColor, circleValue * 0.7);
                }
                
                //Blend circles with water
                waterColor = lerp(waterColor, circleColorResult, circles * 0.7);
                
                //Add subtle sparkle at top surface
                float sparkle = pow(finalNoise, 3) * (1.0 - uv.y) * 0.3;
                waterColor.rgb += float3(sparkle, sparkle * 0.8, sparkle);
                
                //Apply outline
                waterColor = lerp(waterColor, _OutlineColor, outline);
                
                //Final alpha
                waterColor.a = 0.95 - (outline * 0.1);
                
                return waterColor;
            }
            ENDCG
        }
    }
    
    FallBack "Transparent/VertexLit"
}