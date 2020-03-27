//Designed for things that need precision trajectories like projectiles.
//Don't use this for anything that you don't absolutely have to use this with (like projectiles!) because it isn't worth using a datum unless you need accuracy down to decimal places in pixels.

//You might see places where it does - 16 - 1. This is intentionally 17 instead of 16, because of how byond's tiles work and how not doing it will result in rounding errors like things getting put on the wrong turf.

#define RETURN_PRECISE_POSITION(A) new /datum/position(A)
#define RETURN_PRECISE_POINT(A) new /datum/point(A)

#define RETURN_POINT_VECTOR(ATOM, ANGLE, SPEED) (new /datum/point/vector(ATOM, null, null, null, null, ANGLE, SPEED))
#define RETURN_POINT_VECTOR_INCREMENT(ATOM, ANGLE, SPEED, AMT) (new /datum/point/vector(ATOM, null, null, null, null, ANGLE, SPEED, AMT))

/proc/point_midpoint_points(datum/point/a, datum/point/b)	//Obviously will not support multiZ calculations! Same for the two below.
	var/datum/point/P = new
	P.x = a.x + (b.x - a.x) / 2
	P.y = a.y + (b.y - a.y) / 2
	P.z = a.z
	return P

/proc/pixel_length_between_points(datum/point/a, datum/point/b)
	return sqrt(((b.x - a.x) ** 2) + ((b.y - a.y) ** 2))

/proc/angle_between_points(datum/point/a, datum/point/b)
	return ATAN2((b.y - a.y), (b.x - a.x))

/datum/position			//For positions with map x/y/z and pixel x/y so you don't have to return lists. Could use addition/subtraction in the future I guess.
	var/x = 0
	var/y = 0
	var/z = 0
	var/pixel_x = 0
	var/pixel_y = 0

/datum/position/proc/valid()
	return x && y && z && !isnull(pixel_x) && !isnull(pixel_y)

/datum/position/New(_x = 0, _y = 0, _z = 0, _pixel_x = 0, _pixel_y = 0)	//first argument can also be a /datum/point.
	if(istype(_x, /datum/point))
		var/datum/point/P = _x
		var/turf/T = P.return_turf()
		_x = T.x
		_y = T.y
		_z = T.z
		_pixel_x = P.return_px()
		_pixel_y = P.return_py()
	else if(isatom(_x))
		var/atom/A = _x
		_x = A.x
		_y = A.y
		_z = A.z
		_pixel_x = A.pixel_x
		_pixel_y = A.pixel_y
	x = _x
	y = _y
	z = _z
	pixel_x = _pixel_x
	pixel_y = _pixel_y

/datum/position/proc/return_turf()
	return locate(x, y, z)

/datum/position/proc/return_px()
	return pixel_x

/datum/position/proc/return_py()
	return pixel_y

/datum/position/proc/return_point()
	return new /datum/point(src)

/datum/point		//A precise point on the map in absolute pixel locations based on world.icon_size. Pixels are FROM THE EDGE OF THE MAP!
	var/x = 0
	var/y = 0
	var/z = 0

/datum/point/proc/valid()
	return x && y && z

/datum/point/proc/copy_to(datum/point/p = new)
	p.x = x
	p.y = y
	p.z = z
	return p

/datum/point/New(_x, _y, _z, _pixel_x = 0, _pixel_y = 0)	//first argument can also be a /datum/position or /atom.
	if(istype(_x, /datum/position))
		var/datum/position/P = _x
		_x = P.x
		_y = P.y
		_z = P.z
		_pixel_x = P.pixel_x
		_pixel_y = P.pixel_y
	else if(istype(_x, /atom))
		var/atom/A = _x
		_x = A.x
		_y = A.y
		_z = A.z
		_pixel_x = A.pixel_x
		_pixel_y = A.pixel_y
	initialize_location(_x, _y, _z, _pixel_x, _pixel_y)

/datum/point/proc/initialize_location(tile_x, tile_y, tile_z, p_x = 0, p_y = 0)
	if(!isnull(tile_x))
		x = ((tile_x - 1) * world.icon_size) + world.icon_size / 2 + p_x + 1
	if(!isnull(tile_y))
		y = ((tile_y - 1) * world.icon_size) + world.icon_size / 2 + p_y + 1
	if(!isnull(tile_z))
		z = tile_z

/datum/point/proc/debug_out()
	var/turf/T = return_turf()
	return "\ref[src] aX [x] aY [y] aZ [z] pX [return_px()] pY [return_py()] mX [T.x] mY [T.y] mZ [T.z]"

/datum/point/proc/move_atom_to_src(atom/movable/AM)
	AM.forceMove(return_turf())
	AM.pixel_x = return_px()
	AM.pixel_y = return_py()

/datum/point/proc/return_turf()
	return locate(CEILING(x / world.icon_size, 1), CEILING(y / world.icon_size, 1), z)

/datum/point/proc/return_coordinates()		//[turf_x, turf_y, z]
	return list(CEILING(x / world.icon_size, 1), CEILING(y / world.icon_size, 1), z)

/datum/point/proc/return_position()
	return new /datum/position(src)

/datum/point/proc/return_px()
	return MODULUS(x, world.icon_size) - 16 - 1

/datum/point/proc/return_py()
	return MODULUS(y, world.icon_size) - 16 - 1

/datum/point/vector
	var/speed = 32				//pixels per iteration
	var/iteration = 0
	var/angle = 0
	var/mpx = 0					//calculated x/y movement amounts to prevent having to do trig every step.
	var/mpy = 0
	var/starting_x = 0			//just like before, pixels from EDGE of map! This is set in initialize_location().
	var/starting_y = 0
	var/starting_z = 0

/datum/point/vector/New(_x, _y, _z, _pixel_x = 0, _pixel_y = 0, _angle, _speed, initial_increment = 0)
	..()
	initialize_trajectory(_speed, _angle)
	if(initial_increment)
		increment(initial_increment)

/datum/point/vector/initialize_location(tile_x, tile_y, tile_z, p_x = 0, p_y = 0)
	. = ..()
	starting_x = x
	starting_y = y
	starting_z = z

/datum/point/vector/copy_to(datum/point/vector/v = new)
	..(v)
	v.speed = speed
	v.iteration = iteration
	v.angle = angle
	v.mpx = mpx
	v.mpy = mpy
	v.starting_x = starting_x
	v.starting_y = starting_y
	v.starting_z = starting_z
	return v

/datum/point/vector/proc/initialize_trajectory(pixel_speed, new_angle)
	if(!isnull(pixel_speed))
		speed = pixel_speed
	set_angle(new_angle)

/datum/point/vector/proc/set_angle(new_angle)		//calculations use "byond angle" where north is 0 instead of 90, and south is 180 instead of 270.
	if(isnull(angle))
		return
	angle = new_angle
	update_offsets()

/datum/point/vector/proc/update_offsets()
	mpx = sin(angle) * speed
	mpy = cos(angle) * speed

/datum/point/vector/proc/set_speed(new_speed)
	if(isnull(new_speed) || speed == new_speed)
		return
	speed = new_speed
	update_offsets()

/datum/point/vector/proc/increment(multiplier = 1)
	iteration++
	x += mpx * (multiplier)
	y += mpy * (multiplier)

/datum/point/vector/proc/return_vector_after_increments(amount = 7, multiplier = 1, force_simulate = FALSE)
	var/datum/point/vector/v = copy_to()
	if(force_simulate)
		for(var/i in 1 to amount)
			v.increment(multiplier)
	else
		v.increment(multiplier * amount)
	return v

/datum/point/vector/proc/on_z_change()
	return

/datum/point/vector/processed		//pixel_speed is per decisecond.
	var/last_process = 0
	var/last_move = 0
	var/paused = FALSE

/datum/point/vector/processed/Destroy()
	STOP_PROCESSING(SSprojectiles, src)
	return ..()

/datum/point/vector/processed/proc/start()
	last_process = world.time
	last_move = world.time
	START_PROCESSING(SSprojectiles, src)

/datum/point/vector/processed/process()
	if(paused)
		last_move += world.time - last_process
		last_process = world.time
		return
	var/needed_time = world.time - last_move
	last_process = world.time
	last_move = world.time
	increment(needed_time / SSprojectiles.wait)
