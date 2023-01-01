uniform mediump vec4 color:
uniform mediump float size:
uniform mediump float width:
uniform mediump float speed;
uniform mediump float cycle;
uniform mediump float ratio;
uniform mediump float time_shift;
const mediump float PI = 3.14159265359;

mediump float rand(mediump float x){
	return fract(sin(x)*100000.0);
}

void fragment(){
	mediump float bolt = abs(mod(UV.y * cycle + (rand(TIME) + time_shift) * speed * -1., 0.5)-0.25)-0.125;
	bolt *= 4. * width;
	// why 4 ? Because we use mod 0.5, the value be devide by half
	// and we -0.25 (which is half again) for abs function. So it 25%!

	// scale down at start and end
	bolt *=  (0.5 - abs(UV.y-0.5)) * 2.;

	// turn to a line
	// center align line
	mediump float wave = abs(UV.x - 0.5 + bolt);
	// invert and ajust size
	wave = 1. - step(size*.5, wave);

	mediump float blink = step(rand(TIME)*ratio, .5);
	wave *= blink;

	mediump vec4 display = color * mediump vec4(wave);

	COLOR = display;
}
