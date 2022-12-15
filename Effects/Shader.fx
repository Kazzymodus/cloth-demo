sampler uImage0 : register(s0);
sampler uImage1 : register(s1);
sampler uImage2 : register(s2);
float3 uColor;
float3 uSecondaryColor;
float uOpacity;
float uSaturation;
float uRotation;
float uTime;
float4 uSourceRect;
float2 uWorldPosition;
float2 uImageSize0;
float2 uImageSize1;
float2 uImageSize2;
float4 uShaderSpecificData;

static const float PI = 3.1415926535f;

float4 CapeColor(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{ 
    return float4(uColor, 1) * uOpacity;
}

float4 CapeSparks(float4 sampleColor : COLOR0, float2 coords : TEXCOORD0) : COLOR0
{ 
    float2 pixel = coords * uImageSize0;
    float2 noiseCoords = pixel / uImageSize1;
    
    float noise = frac(tex2D(uImage1, noiseCoords.x).b * 4);
       
    float wave = frac(coords.y - uTime * 0.5f + noise);
    float tips = saturate(wave - 0.9f) * 10;
    float4 rays = float4(tips * uColor, tips);
    
    float4 background = float4(uSecondaryColor, 1) * uOpacity;
    
    float fade = (tips + (1 - coords.y));
    float tipsFullBright = ((1 - tips) * sampleColor + tips);
    
    return (background + rays) * fade * tipsFullBright;
}

technique Technique1
{	
    pass CapeColor
    {
        PixelShader = compile ps_2_0 CapeColor();
    }

    pass CapeSparks
    {
        PixelShader = compile ps_2_0 CapeSparks();
    }
}
