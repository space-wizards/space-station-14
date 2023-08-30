light_mode unshaded;

uniform sampler2D SCREEN_TEXTURE;
uniform highp float visibility; // number between -1 and 1
uniform mediump vec2 reference;

const mediump float time_scale = 0.25;
const mediump float distance_scale = 0.125;

void fragment() {

    highp vec4 sprite = zTexture(UV);

    if (sprite.a == 0.0) {
        discard;
    }

    // get distortion magnitude. hand crafted from a random jumble of trig functions
    highp vec2 coords = (FRAGCOORD.xy + reference) * distance_scale;
    highp float w = sin(TIME + (coords.x + coords.y + 2.0*sin(TIME*time_scale) * sin(TIME*time_scale + coords.x - coords.y)) );

    // visualize distortion via:
    // COLOR = vec4(w,w,w,1.0);

    w *= (3.0 + visibility * 2.0);

    highp vec4 background = zTextureSpec(SCREEN_TEXTURE, ( FRAGCOORD.xy + vec2(w) ) * SCREEN_PIXEL_SIZE );

    lowp float alpha;
    if (visibility>0.0)
      alpha = sprite.a * visibility;
    else
      alpha = 0.0;

    COLOR.xyz = mix(background.xyz, sprite.xyz, alpha);
    COLOR.a = 1.0;
}
