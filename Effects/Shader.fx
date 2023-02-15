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

float4x4 MatrixTransform;

static const float PI = 3.1415926535f;

void CapeShader(inout float4 position : POSITION, inout float4 color : COLOR0, inout float3 coords : TEXCOORD0)
{
    position = mul(position, MatrixTransform);
}

float4 CapeColor(in float4 sampleColor : COLOR0) : COLOR0
{
    return float4(uColor, 1) * uOpacity * sampleColor;
}

float4 CapeSparks(in float4 sampleColor : COLOR0, in float3 coords : TEXCOORD0) : COLOR0
{
    float2 corrected = coords.xy / (1 / coords.z);
    float4 sampleCoords = float4(coords.x * uImageSize0.x / uImageSize1.x, 0, 0, coords.z);
    float noise = frac(tex2D(uImage1, corrected.x * uImageSize0.x / uImageSize1.x).b * 4);
    float wave = frac(corrected.y - uTime * 0.5f + noise);
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
        VertexShader = compile vs_2_0 CapeShader();
        PixelShader = compile ps_2_0 CapeColor();
    }

    pass CapeSparks
    {
        VertexShader = compile vs_2_0 CapeShader();
        PixelShader = compile ps_2_0 CapeSparks();
    }
}