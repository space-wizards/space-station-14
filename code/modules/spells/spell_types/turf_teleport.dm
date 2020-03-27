/obj/effect/proc_holder/spell/targeted/turf_teleport
	name = "Turf Teleport"
	desc = "This spell teleports the target to the turf in range."
	nonabstract_req = TRUE

	var/inner_tele_radius = 1
	var/outer_tele_radius = 2

	var/include_space = FALSE //whether it includes space tiles in possible teleport locations
	var/include_dense = FALSE //whether it includes dense tiles in possible teleport locations
	var/sound1 = 'sound/weapons/zapbang.ogg'
	var/sound2 = 'sound/weapons/zapbang.ogg'

/obj/effect/proc_holder/spell/targeted/turf_teleport/cast(list/targets,mob/user = usr)
	playsound(get_turf(user), sound1, 50,TRUE)
	for(var/mob/living/target in targets)
		var/list/turfs = new/list()
		for(var/turf/T in range(target,outer_tele_radius))
			if(T in range(target,inner_tele_radius))
				continue
			if(isspaceturf(T) && !include_space)
				continue
			if(T.density && !include_dense)
				continue
			if(T.x>world.maxx-outer_tele_radius || T.x<outer_tele_radius)
				continue	//putting them at the edge is dumb
			if(T.y>world.maxy-outer_tele_radius || T.y<outer_tele_radius)
				continue
			turfs += T

		if(!turfs.len)
			var/list/turfs_to_pick_from = list()
			for(var/turf/T in orange(target,outer_tele_radius))
				if(!(T in orange(target,inner_tele_radius)))
					turfs_to_pick_from += T
			turfs += pick(/turf in turfs_to_pick_from)

		var/turf/picked = pick(turfs)

		if(!picked || !isturf(picked))
			return

		if(do_teleport(user, picked, forceMove = TRUE, channel = TELEPORT_CHANNEL_MAGIC))
			playsound(get_turf(user), sound1, 50,TRUE)
