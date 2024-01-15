#version 140
#define HAS_MOD
#define HAS_DFDX
#define HAS_FLOAT_TEXTURES
#define HAS_SRGB
#define HAS_UNIFORM_BUFFERS
#define FRAGMENT_SHADER

// -- Utilities Start --

// It's literally just called the Z-Library for alphabetical ordering reasons.
//  - 20kdc

// -- varying/attribute/texture2D --

#ifndef HAS_VARYING_ATTRIBUTE
#define texture2D texture
#ifdef VERTEX_SHADER
#define varying out
#define attribute in
#else
#define varying in
#define attribute in
#define gl_FragColor colourOutput
out highp vec4 colourOutput;
#endif
#endif

#ifndef NO_ARRAY_PRECISION
#define ARRAY_LOWP lowp
#define ARRAY_MEDIUMP mediump
#define ARRAY_HIGHP highp
#else
#define ARRAY_LOWP lowp
#define ARRAY_MEDIUMP mediump
#define ARRAY_HIGHP highp
#endif

// -- shadow depth --

// If float textures are supported, puts the values in the R/G fields.
// This assumes RG32F format.
// If float textures are NOT supported.
// This assumes RGBA8 format.
// Operational range is "whatever works for FOV depth"
highp vec4 zClydeShadowDepthPack(highp vec2 val) {
#ifdef HAS_FLOAT_TEXTURES
    return vec4(val, 0.0, 1.0);
#else
    highp vec2 valH = floor(val);
    return vec4(valH / 255.0, val - valH);
#endif
}

// Inverts the previous function.
highp vec2 zClydeShadowDepthUnpack(highp vec4 val) {
#ifdef HAS_FLOAT_TEXTURES
    return val.xy;
#else
    return (val.xy * 255.0) + val.zw;
#endif
}

// -- srgb/linear conversion core --

highp vec4 zFromSrgb(highp vec4 sRGB)
{
    highp vec3 higher = pow((sRGB.rgb + 0.055) / 1.055, vec3(2.4));
    highp vec3 lower = sRGB.rgb / 12.92;
    highp vec3 s = max(vec3(0.0), sign(sRGB.rgb - 0.04045));
    return vec4(mix(lower, higher, s), sRGB.a);
}

highp vec4 zToSrgb(highp vec4 sRGB)
{
    highp vec3 higher = (pow(sRGB.rgb, vec3(0.41666666666667)) * 1.055) - 0.055;
    highp vec3 lower = sRGB.rgb * 12.92;
    highp vec3 s = max(vec3(0.0), sign(sRGB.rgb - 0.0031308));
    return vec4(mix(lower, higher, s), sRGB.a);
}

// -- uniforms --

#ifdef HAS_UNIFORM_BUFFERS
layout (std140) uniform projectionViewMatrices
{
    highp mat3 projectionMatrix;
    highp mat3 viewMatrix;
};

layout (std140) uniform uniformConstants
{
    highp vec2 SCREEN_PIXEL_SIZE;
    highp float TIME;
};
#else
uniform highp mat3 projectionMatrix;
uniform highp mat3 viewMatrix;
uniform highp vec2 SCREEN_PIXEL_SIZE;
uniform highp float TIME;
#endif

uniform sampler2D TEXTURE;
uniform highp vec2 TEXTURE_PIXEL_SIZE;

// -- srgb emulation --

#ifdef HAS_SRGB

highp vec4 zTextureSpec(sampler2D tex, highp vec2 uv)
{
    return texture2D(tex, uv);
}

highp vec4 zAdjustResult(highp vec4 col)
{
    return col;
}
#else
uniform lowp vec2 SRGB_EMU_CONFIG;

highp vec4 zTextureSpec(sampler2D tex, highp vec2 uv)
{
    highp vec4 col = texture2D(tex, uv);
    if (SRGB_EMU_CONFIG.x > 0.5)
    {
        return zFromSrgb(col);
    }
    return col;
}

highp vec4 zAdjustResult(highp vec4 col)
{
    if (SRGB_EMU_CONFIG.y > 0.5)
    {
        return zToSrgb(col);
    }
    return col;
}
#endif

highp vec4 zTexture(highp vec2 uv)
{
    return zTextureSpec(TEXTURE, uv);
}

// -- Utilities End --

varying highp vec2 UV;
varying highp vec2 Pos;
varying highp vec4 VtxModulate;

uniform sampler2D lightMap;

uniform ARRAY_HIGHP float Zoom;
uniform ARRAY_HIGHP float CircleRadius;
uniform ARRAY_HIGHP float CircleMinDist;
uniform ARRAY_HIGHP float CirclePow;
uniform ARRAY_HIGHP float CircleMax;
uniform ARRAY_HIGHP float CircleMult;




void main()
{
    highp vec4 FRAGCOORD = gl_FragCoord;

    lowp vec4 COLOR;

    lowp vec3 lightSample = texture2D(lightMap, Pos).rgb;

     highp vec2 aspect = vec2 ( 1.0 / SCREEN_PIXEL_SIZE . x , 1.0 / SCREEN_PIXEL_SIZE . y ) ;
 highp float actualZoom = Zoom ;
 highp float circle = zCircleGradient ( aspect , FRAGCOORD . xy , CircleMax , CircleRadius / actualZoom , CircleMinDist / actualZoom , CirclePow ) * CircleMult ;
 COLOR = mix ( vec4 ( 0.0 ) , vec4 ( vec3 ( 0.0 ) , 1.0 ) , clamp ( circle , 0.0 , 1.0 ) ) ;


    gl_FragColor = zAdjustResult(COLOR * VtxModulate * vec4(lightSample, 1.0));
}
