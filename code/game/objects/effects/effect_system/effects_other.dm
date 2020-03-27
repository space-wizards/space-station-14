
/////////////////////////////////////////////
//////// Attach a trail to any object, that spawns when it moves (like for the jetpack)
/// just pass in the object to attach it to in set_up
/// Then do start() to start it and stop() to stop it, obviously
/// and don't call start() in a loop that will be repeated otherwise it'll get spammed!
/////////////////////////////////////////////

/datum/effect_system/trail_follow
	var/turf/oldposition
	var/active = FALSE
	var/allow_overlap = FALSE
	var/auto_process = TRUE
	var/qdel_in_time = 10
	var/fadetype = "ion_fade"
	var/fade = TRUE
	var/nograv_required = FALSE

/datum/effect_system/trail_follow/set_up(atom/atom)
	attach(atom)
	oldposition = get_turf(atom)

/datum/effect_system/trail_follow/Destroy()
	oldposition = null
	stop()
	return ..()

/datum/effect_system/trail_follow/proc/stop()
	oldposition = null
	STOP_PROCESSING(SSfastprocess, src)
	active = FALSE
	return TRUE

/datum/effect_system/trail_follow/start()
	oldposition = get_turf(holder)
	if(!check_conditions())
		return FALSE
	if(auto_process)
		START_PROCESSING(SSfastprocess, src)
	active = TRUE
	return TRUE

/datum/effect_system/trail_follow/process()
	generate_effect()

/datum/effect_system/trail_follow/generate_effect()
	if(!check_conditions())
		return stop()
	if(oldposition && !(oldposition == get_turf(holder)))
		if(!oldposition.has_gravity() || !nograv_required)
			var/obj/effect/E = new effect_type(oldposition)
			set_dir(E)
			if(fade)
				flick(fadetype, E)
				E.icon_state = ""
			if(qdel_in_time)
				QDEL_IN(E, qdel_in_time)
	oldposition = get_turf(holder)

/datum/effect_system/trail_follow/proc/check_conditions()
	if(!get_turf(holder))
		return FALSE
	return TRUE

/datum/effect_system/trail_follow/steam
	effect_type = /obj/effect/particle_effect/steam

/obj/effect/particle_effect/ion_trails
	name = "ion trails"
	icon_state = "ion_trails"
	anchored = TRUE

/obj/effect/particle_effect/ion_trails/flight
	icon_state = "ion_trails_flight"

/datum/effect_system/trail_follow/ion
	effect_type = /obj/effect/particle_effect/ion_trails
	nograv_required = TRUE
	qdel_in_time = 20

/datum/effect_system/trail_follow/proc/set_dir(obj/effect/particle_effect/ion_trails/I)
	I.setDir(holder.dir)

//Reagent-based explosion effect

/datum/effect_system/reagents_explosion
	var/amount 						// TNT equivalent
	var/flashing = 0			// does explosion creates flash effect?
	var/flashing_factor = 0		// factor of how powerful the flash effect relatively to the explosion
	var/explosion_message = 1				//whether we show a message to mobs.

/datum/effect_system/reagents_explosion/set_up(amt, loca, flash = 0, flash_fact = 0, message = 1)
	amount = amt
	explosion_message = message
	if(isturf(loca))
		location = loca
	else
		location = get_turf(loca)

	flashing = flash
	flashing_factor = flash_fact

/datum/effect_system/reagents_explosion/start()
	if(explosion_message)
		location.visible_message("<span class='danger'>The solution violently explodes!</span>", \
								"<span class='hear'>You hear an explosion!</span>")

	dyn_explosion(location, amount, flashing_factor)
