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

// -- color --

// Grayscale function for the ITU's Rec BT-709. Primarily intended for HDTVs, but standard sRGB monitors are coincidentally extremely close.
highp float zGrayscale_BT709(highp vec3 col) {
    return dot(col, vec3(0.2126, 0.7152, 0.0722));
}

// Grayscale function for the ITU's Rec BT-601, primarily intended for SDTV, but amazing for a handful of niche use-cases.
highp float zGrayscale_BT601(highp vec3 col) {
    return dot(col, vec3(0.299, 0.587, 0.114));
}

// If you don't have any reason to be specifically using the above grayscale functions, then you should default to this.
highp float zGrayscale(highp vec3 col) {
    return zGrayscale_BT709(col);
}

// -- noise --

//zRandom, zNoise, and zFBM are derived from https://godotshaders.com/snippet/2d-noise/ and https://godotshaders.com/snippet/fractal-brownian-motion-fbm/
highp vec2 zRandom(highp vec2 uv){
    uv = vec2( dot(uv, vec2(127.1,311.7) ),
               dot(uv, vec2(269.5,183.3) ) );
    return -1.0 + 2.0 * fract(sin(uv) * 43758.5453123);
}

highp float zNoise(highp vec2 uv) {
    highp vec2 uv_index = floor(uv);
    highp vec2 uv_fract = fract(uv);

    highp vec2 blur = smoothstep(0.0, 1.0, uv_fract);

    return mix( mix( dot( zRandom(uv_index + vec2(0.0,0.0) ), uv_fract - vec2(0.0,0.0) ),
                     dot( zRandom(uv_index + vec2(1.0,0.0) ), uv_fract - vec2(1.0,0.0) ), blur.x),
                mix( dot( zRandom(uv_index + vec2(0.0,1.0) ), uv_fract - vec2(0.0,1.0) ),
                     dot( zRandom(uv_index + vec2(1.0,1.0) ), uv_fract - vec2(1.0,1.0) ), blur.x), blur.y) * 0.5 + 0.5;
}

highp float zFBM(highp vec2 uv) {
    const int octaves = 6;
    highp float amplitude = 0.5;
    highp float frequency = 3.0;
    highp float value = 0.0;

    for(int i = 0; i < octaves; i++) {
        value += amplitude * zNoise(frequency * uv);
        amplitude *= 0.5;
        frequency *= 2.0;
    }
    return value;
}


// -- generative --

// Function that creates a circular gradient. Screenspace shader bread n butter.
highp float zCircleGradient(highp vec2 ps, highp vec2 coord, highp float maxi, highp float radius, highp float dist, highp float power) {
    highp float rad = (radius * ps.y) * 0.001;
    highp float aspectratio = ps.x / ps.y;
    highp vec2 totaldistance = ((ps * 0.5) - coord) / (rad * ps);
    totaldistance.x *= aspectratio;
    highp float length = (length(totaldistance) * ps.y) - dist;
    return pow(clamp(length, 0.0, maxi), power);
}

// -- Utilities End --

// UV coordinates in texture-space. I.e., (0,0) is the corner of the texture currently being used to draw.
// When drawing a sprite from a texture atlas, (0,0) is the corner of the atlas, not the specific sprite being drawn.
varying highp vec2 UV;

// UV coordinates in quad-space. I.e., when drawing a sprite from a texture atlas (0,0) is the corner of the sprite
// currently being drawn.
varying highp vec2 UV2;

// TBH I'm not sure what this is for. I think it is scree  UV coordiantes, i.e., FRAGCOORD.xy * SCREEN_PIXEL_SIZE ?
// TODO CLYDE Is this still needed?
varying highp vec2 Pos;

// Vertex colour modulation. Note that negative values imply that the LIGHTMAP should be ignored. This is used to avoid
// having to set the texture to a white/blank texture for sprites that have no light shading applied.
varying highp vec4 VtxModulate;

// The current light map. Unless disabled, this is automatically sampled to create the LIGHT vector, which is then used
// to modulate the output colour.
// TODO CLYDE consistent shader variable naming
uniform sampler2D lightMap;

uniform ARRAY_HIGHP vec2 centerPx;
uniform ARRAY_HIGHP float angleDeg;
uniform ARRAY_HIGHP float rotation;
uniform ARRAY_HIGHP float outsideOpacity;
uniform ARRAY_HIGHP float edgeFeatherPixels;
uniform ARRAY_HIGHP float centerClearRadiusPixels;
uniform ARRAY_HIGHP float centerFeatherPixels;




void main()
{
    highp vec4 FRAGCOORD = gl_FragCoord;

    // The output colour. This should get set by the shader code block.
    // This will get modified by the LIGHT and MODULATE vectors.
    lowp vec4 COLOR;

    // The light colour, usually sampled from the LIGHTMAP
    lowp vec4 LIGHT;

    // Colour modulation vector.
    highp vec4 MODULATE;

    // Sample the texture outside of the branch / with uniform control flow.
    LIGHT = texture2D(lightMap, Pos);

    if (VtxModulate.x < 0.0)
    {
        // Negative VtxModulate implies unshaded/no lighting.
        MODULATE = -1.0 - VtxModulate;
        LIGHT = vec4(1.0);
    }
    else
    {
        MODULATE = VtxModulate;
    }

    // TODO CLYDE consistent shader variable naming
    // Requires breaking changes.
    lowp vec3 lightSample = LIGHT.xyz;

     highp vec2 v = FRAGCOORD . xy - centerPx ;
 highp float len = length ( v ) ;
 if ( len <= 1 e - 5 ) {
 COLOR = vec4 ( 0.0 , 0.0 , 0.0 , 0.0 ) ;
 return ;
 }
 highp vec2 dir = v / len ;
 highp vec2 fwd = vec2 ( cos ( rotation ) , sin ( rotation ) ) ;
 highp float halfAngle = angleDeg * 0.017453292519943295 * 0.5 ;
 highp float featherAng = edgeFeatherPixels / max ( len , 1 e - 5 ) ;
 highp float m = dot ( dir , fwd ) ;
 highp float cLow = cos ( halfAngle + featherAng ) ;
 highp float cHigh = cos ( halfAngle - featherAng ) ;
 highp float smoothEdge = 1.0 - smoothstep ( cLow , cHigh , m ) ;
 highp float smoothCenter = smoothstep ( centerClearRadiusPixels - centerFeatherPixels , centerClearRadiusPixels + centerFeatherPixels , len ) ;
 highp float alpha = outsideOpacity * smoothEdge * smoothCenter ;
 COLOR = vec4 ( 0.0 , 0.0 , 0.0 , alpha ) ;


    LIGHT.xyz = lightSample;

    gl_FragColor = zAdjustResult(COLOR * MODULATE * LIGHT);
}
