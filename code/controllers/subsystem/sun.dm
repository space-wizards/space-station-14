SUBSYSTEM_DEF(sun)
	name = "Sun"
	wait = 1 MINUTES
	flags = SS_NO_TICK_CHECK

	var/azimuth = 0 ///clockwise, top-down rotation from 0 (north) to 359
	var/azimuth_mod = 1 ///multiplier against base_rotation
	var/base_rotation = 6 ///base rotation in degrees per fire

/datum/controller/subsystem/sun/Initialize(start_timeofday)
	azimuth = rand(0, 359)
	azimuth_mod = round(rand(50, 200)/100, 0.01) // 50% - 200% of standard rotation
	if(prob(50))
		azimuth_mod *= -1
	return ..()

/datum/controller/subsystem/sun/fire(resumed = FALSE)
	azimuth += azimuth_mod * base_rotation
	azimuth = round(azimuth, 0.01)
	if(azimuth >= 360)
		azimuth -= 360
	if(azimuth < 0)
		azimuth += 360
	complete_movement()

/datum/controller/subsystem/sun/proc/complete_movement()
	SEND_SIGNAL(src, COMSIG_SUN_MOVED, azimuth)

/datum/controller/subsystem/sun/vv_edit_var(var_name, var_value)
	. = ..()
	if(var_name == NAMEOF(src, azimuth))
		complete_movement()
