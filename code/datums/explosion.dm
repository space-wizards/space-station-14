#define EXPLOSION_THROW_SPEED 4

GLOBAL_LIST_EMPTY(explosions)
//Against my better judgement, I will return the explosion datum
//If I see any GC errors for it I will find you
//and I will gib you
/proc/explosion(atom/epicenter, devastation_range, heavy_impact_range, light_impact_range, flash_range, adminlog = TRUE, ignorecap = FALSE, flame_range = 0, silent = FALSE, smoke = FALSE)
	return new /datum/explosion(epicenter, devastation_range, heavy_impact_range, light_impact_range, flash_range, adminlog, ignorecap, flame_range, silent, smoke)

//This datum creates 3 async tasks
//1 GatherSpiralTurfsProc runs spiral_range_turfs(tick_checked = TRUE) to populate the affected_turfs list
//2 CaculateExplosionBlock adds the blockings to the cached_exp_block list
//3 The main thread explodes the prepared turfs

/datum/explosion
	var/explosion_id
	var/atom/explosion_source
	var/started_at
	var/running = TRUE
	var/stopped = 0		//This is the number of threads stopped !DOESN'T COUNT THREAD 2!
	var/static/id_counter = 0

#define EX_PREPROCESS_EXIT_CHECK \
	if(!running) {\
		stopped = 2;\
		qdel(src);\
		return;\
	}

#define EX_PREPROCESS_CHECK_TICK \
	if(TICK_CHECK) {\
		stoplag();\
		EX_PREPROCESS_EXIT_CHECK\
	}

/datum/explosion/New(atom/epicenter, devastation_range, heavy_impact_range, light_impact_range, flash_range, adminlog, ignorecap, flame_range, silent, smoke)
	set waitfor = FALSE

	var/id = ++id_counter
	explosion_id = id
	explosion_source = epicenter

	epicenter = get_turf(epicenter)
	if(!epicenter)
		return

	GLOB.explosions += src
	if(isnull(flame_range))
		flame_range = light_impact_range
	if(isnull(flash_range))
		flash_range = devastation_range

	// Archive the uncapped explosion for the doppler array
	var/orig_dev_range = devastation_range
	var/orig_heavy_range = heavy_impact_range
	var/orig_light_range = light_impact_range

	var/orig_max_distance = max(devastation_range, heavy_impact_range, light_impact_range, flash_range, flame_range)

	//Zlevel specific bomb cap multiplier
	var/cap_multiplier = SSmapping.level_trait(epicenter.z, ZTRAIT_BOMBCAP_MULTIPLIER)
	if (isnull(cap_multiplier))
		cap_multiplier = 1

	if(!ignorecap)
		devastation_range = min(GLOB.MAX_EX_DEVESTATION_RANGE * cap_multiplier, devastation_range)
		heavy_impact_range = min(GLOB.MAX_EX_HEAVY_RANGE * cap_multiplier, heavy_impact_range)
		light_impact_range = min(GLOB.MAX_EX_LIGHT_RANGE * cap_multiplier, light_impact_range)
		flash_range = min(GLOB.MAX_EX_FLASH_RANGE * cap_multiplier, flash_range)
		flame_range = min(GLOB.MAX_EX_FLAME_RANGE * cap_multiplier, flame_range)

	//DO NOT REMOVE THIS STOPLAG, IT BREAKS THINGS
	//not sleeping causes us to ex_act() the thing that triggered the explosion
	//doing that might cause it to trigger another explosion
	//this is bad
	//I would make this not ex_act the thing that triggered the explosion,
	//but everything that explodes gives us their loc or a get_turf()
	//and somethings expect us to ex_act them so they can qdel()
	stoplag() //tldr, let the calling proc call qdel(src) before we explode

	EX_PREPROCESS_EXIT_CHECK

	started_at = REALTIMEOFDAY

	var/max_range = max(devastation_range, heavy_impact_range, light_impact_range, flame_range)

	if(adminlog)
		message_admins("Explosion with size ([devastation_range], [heavy_impact_range], [light_impact_range], [flame_range]) in [ADMIN_VERBOSEJMP(epicenter)]")
		log_game("Explosion with size ([devastation_range], [heavy_impact_range], [light_impact_range], [flame_range]) in [loc_name(epicenter)]")

	var/x0 = epicenter.x
	var/y0 = epicenter.y
	var/z0 = epicenter.z
	var/area/areatype = get_area(epicenter)
	SSblackbox.record_feedback("associative", "explosion", 1, list("dev" = devastation_range, "heavy" = heavy_impact_range, "light" = light_impact_range, "flash" = flash_range, "flame" = flame_range, "orig_dev" = orig_dev_range, "orig_heavy" = orig_heavy_range, "orig_light" = orig_light_range, "x" = x0, "y" = y0, "z" = z0, "area" = areatype.type, "time" = time_stamp("YYYY-MM-DD hh:mm:ss", 1)))

	// Play sounds; we want sounds to be different depending on distance so we will manually do it ourselves.
	// Stereo users will also hear the direction of the explosion!

	// Calculate far explosion sound range. Only allow the sound effect for heavy/devastating explosions.
	// 3/7/14 will calculate to 80 + 35

	var/far_dist = 0
	far_dist += heavy_impact_range * 5
	far_dist += devastation_range * 20

	if(!silent)
		var/frequency = get_rand_frequency()
		var/sound/explosion_sound = sound(get_sfx("explosion"))
		var/sound/far_explosion_sound = sound('sound/effects/explosionfar.ogg')

		for(var/mob/M in GLOB.player_list)
			// Double check for client
			var/turf/M_turf = get_turf(M)
			if(M_turf && M_turf.z == z0)
				var/dist = get_dist(M_turf, epicenter)
				var/baseshakeamount
				if(orig_max_distance - dist > 0)
					baseshakeamount = sqrt((orig_max_distance - dist)*0.1)
				// If inside the blast radius + world.view - 2
				if(dist <= round(max_range + world.view - 2, 1))
					M.playsound_local(epicenter, null, 100, 1, frequency, falloff = 5, S = explosion_sound)
					if(baseshakeamount > 0)
						shake_camera(M, 25, CLAMP(baseshakeamount, 0, 10))
				// You hear a far explosion if you're outside the blast radius. Small bombs shouldn't be heard all over the station.
				else if(dist <= far_dist)
					var/far_volume = CLAMP(far_dist, 30, 50) // Volume is based on explosion size and dist
					far_volume += (dist <= far_dist * 0.5 ? 50 : 0) // add 50 volume if the mob is pretty close to the explosion
					M.playsound_local(epicenter, null, far_volume, 1, frequency, falloff = 5, S = far_explosion_sound)
					if(baseshakeamount > 0)
						shake_camera(M, 10, CLAMP(baseshakeamount*0.25, 0, 2.5))
			EX_PREPROCESS_CHECK_TICK

	//postpone processing for a bit
	var/postponeCycles = max(round(devastation_range/8),1)
	SSlighting.postpone(postponeCycles)
	SSmachines.postpone(postponeCycles)

	if(heavy_impact_range > 1)
		var/datum/effect_system/explosion/E
		if(smoke)
			E = new /datum/effect_system/explosion/smoke
		else
			E = new
		E.set_up(epicenter)
		E.start()

	EX_PREPROCESS_CHECK_TICK

	//flash mobs
	if(flash_range)
		for(var/mob/living/L in viewers(flash_range, epicenter))
			L.flash_act()

	EX_PREPROCESS_CHECK_TICK

	var/list/exploded_this_tick = list()	//open turfs that need to be blocked off while we sleep
	var/list/affected_turfs = GatherSpiralTurfs(max_range, epicenter)

	var/reactionary = CONFIG_GET(flag/reactionary_explosions)
	var/list/cached_exp_block

	if(reactionary)
		cached_exp_block = CaculateExplosionBlock(affected_turfs)

	//lists are guaranteed to contain at least 1 turf at this point

	var/iteration = 0
	var/affTurfLen = affected_turfs.len
	var/expBlockLen = cached_exp_block.len
	for(var/TI in affected_turfs)
		var/turf/T = TI
		++iteration
		var/init_dist = cheap_hypotenuse(T.x, T.y, x0, y0)
		var/dist = init_dist

		if(reactionary)
			var/turf/Trajectory = T
			while(Trajectory != epicenter)
				Trajectory = get_step_towards(Trajectory, epicenter)
				dist += cached_exp_block[Trajectory]

		var/flame_dist = dist < flame_range
		var/throw_dist = dist

		if(dist < devastation_range)
			dist = EXPLODE_DEVASTATE
		else if(dist < heavy_impact_range)
			dist = EXPLODE_HEAVY
		else if(dist < light_impact_range)
			dist = EXPLODE_LIGHT
		else
			dist = EXPLODE_NONE

		//------- EX_ACT AND TURF FIRES -------

		if(T == epicenter) // Ensures explosives detonating from bags trigger other explosives in that bag
			var/list/items = list()
			for(var/I in T)
				var/atom/A = I
				if (!(A.flags_1 & PREVENT_CONTENTS_EXPLOSION_1)) //The atom/contents_explosion() proc returns null if the contents ex_acting has been handled by the atom, and TRUE if it hasn't.
					items += A.GetAllContents()
			for(var/O in items)
				var/atom/A = O
				if(!QDELETED(A))
					A.ex_act(dist)

		if(flame_dist && prob(40) && !isspaceturf(T) && !T.density)
			new /obj/effect/hotspot(T) //Mostly for ambience!

		if(dist > EXPLODE_NONE)
			T.explosion_level = max(T.explosion_level, dist)	//let the bigger one have it
			T.explosion_id = id
			T.ex_act(dist)
			exploded_this_tick += T

		//--- THROW ITEMS AROUND ---

		var/throw_dir = get_dir(epicenter,T)
		for(var/obj/item/I in T)
			if(!I.anchored)
				var/throw_range = rand(throw_dist, max_range)
				var/turf/throw_at = get_ranged_target_turf(I, throw_dir, throw_range)
				I.throw_at(throw_at, throw_range, EXPLOSION_THROW_SPEED)

		//wait for the lists to repop
		var/break_condition
		if(reactionary)
			//If we've caught up to the density checker thread and there are no more turfs to process
			break_condition = iteration == expBlockLen && iteration < affTurfLen
		else
			//If we've caught up to the turf gathering thread and it's still running
			break_condition = iteration == affTurfLen && !stopped

		if(break_condition || TICK_CHECK)
			stoplag()

			if(!running)
				break

			//update the trackers
			affTurfLen = affected_turfs.len
			expBlockLen = cached_exp_block.len

			if(break_condition)
				if(reactionary)
					//until there are more block checked turfs than what we are currently at
					//or the explosion has stopped
					UNTIL(iteration < affTurfLen || !running)
				else
					//until there are more gathered turfs than what we are currently at
					//or there are no more turfs to gather/the explosion has stopped
					UNTIL(iteration < expBlockLen || stopped)

				if(!running)
					break

				//update the trackers
				affTurfLen = affected_turfs.len
				expBlockLen = cached_exp_block.len

			var/circumference = (PI * (init_dist + 4) * 2) //+4 to radius to prevent shit gaps
			if(exploded_this_tick.len > circumference)	//only do this every revolution
				for(var/Unexplode in exploded_this_tick)
					var/turf/UnexplodeT = Unexplode
					UnexplodeT.explosion_level = 0
				exploded_this_tick.Cut()

	//unfuck the shit
	for(var/Unexplode in exploded_this_tick)
		var/turf/UnexplodeT = Unexplode
		UnexplodeT.explosion_level = 0
	exploded_this_tick.Cut()

	var/took = (REALTIMEOFDAY - started_at) / 10

	//You need to press the DebugGame verb to see these now....they were getting annoying and we've collected a fair bit of data. Just -test- changes to explosion code using this please so we can compare
	if(GLOB.Debug2)
		log_world("## DEBUG: Explosion([x0],[y0],[z0])(d[devastation_range],h[heavy_impact_range],l[light_impact_range]): Took [took] seconds.")

	if(running)	//if we aren't in a hurry
		SEND_GLOBAL_SIGNAL(COMSIG_GLOB_EXPLOSION, epicenter, devastation_range, heavy_impact_range, light_impact_range, took, orig_dev_range, orig_heavy_range, orig_light_range)

	++stopped
	qdel(src)

#undef EX_PREPROCESS_EXIT_CHECK
#undef EX_PREPROCESS_CHECK_TICK

//asyncly populate the affected_turfs list
/datum/explosion/proc/GatherSpiralTurfs(range, turf/epicenter)
	set waitfor = FALSE
	. = list()
	spiral_range_turfs(range, epicenter, outlist = ., tick_checked = TRUE)
	++stopped

/datum/explosion/proc/CaculateExplosionBlock(list/affected_turfs)
	set waitfor = FALSE

	. = list()
	var/processed = 0
	while(running)
		var/I
		for(I in (processed + 1) to affected_turfs.len) // we cache the explosion block rating of every turf in the explosion area
			var/turf/T = affected_turfs[I]
			var/current_exp_block = T.density ? T.explosion_block : 0

			for(var/obj/O in T)
				var/the_block = O.explosion_block
				current_exp_block += the_block == EXPLOSION_BLOCK_PROC ? O.GetExplosionBlock() : the_block

			.[T] = current_exp_block

			if(TICK_CHECK)
				break

		processed = I
		stoplag()

/datum/explosion/Destroy()
	running = FALSE
	if(stopped < 2)	//wait for main thread and spiral_range thread
		return QDEL_HINT_IWILLGC
	GLOB.explosions -= src
	explosion_source = null
	return ..()

/client/proc/check_bomb_impacts()
	set name = "Check Bomb Impact"
	set category = "Debug"

	var/newmode = alert("Use reactionary explosions?","Check Bomb Impact", "Yes", "No")
	var/turf/epicenter = get_turf(mob)
	if(!epicenter)
		return

	var/dev = 0
	var/heavy = 0
	var/light = 0
	var/list/choices = list("Small Bomb","Medium Bomb","Big Bomb","Custom Bomb")
	var/choice = input("Bomb Size?") in choices
	switch(choice)
		if(null)
			return 0
		if("Small Bomb")
			dev = 1
			heavy = 2
			light = 3
		if("Medium Bomb")
			dev = 2
			heavy = 3
			light = 4
		if("Big Bomb")
			dev = 3
			heavy = 5
			light = 7
		if("Custom Bomb")
			dev = input("Devastation range (Tiles):") as num
			heavy = input("Heavy impact range (Tiles):") as num
			light = input("Light impact range (Tiles):") as num

	var/max_range = max(dev, heavy, light)
	var/x0 = epicenter.x
	var/y0 = epicenter.y
	var/list/wipe_colours = list()
	for(var/turf/T in spiral_range_turfs(max_range, epicenter))
		wipe_colours += T
		var/dist = cheap_hypotenuse(T.x, T.y, x0, y0)

		if(newmode == "Yes")
			var/turf/TT = T
			while(TT != epicenter)
				TT = get_step_towards(TT,epicenter)
				if(TT.density)
					dist += TT.explosion_block

				for(var/obj/O in T)
					var/the_block = O.explosion_block
					dist += the_block == EXPLOSION_BLOCK_PROC ? O.GetExplosionBlock() : the_block

		if(dist < dev)
			T.color = "red"
			T.maptext = "Dev"
		else if (dist < heavy)
			T.color = "yellow"
			T.maptext = "Heavy"
		else if (dist < light)
			T.color = "blue"
			T.maptext = "Light"
		else
			continue

	addtimer(CALLBACK(GLOBAL_PROC, .proc/wipe_color_and_text, wipe_colours), 100)

/proc/wipe_color_and_text(list/atom/wiping)
	for(var/i in wiping)
		var/atom/A = i
		A.color = null
		A.maptext = ""

/proc/dyn_explosion(turf/epicenter, power, flash_range, adminlog = TRUE, ignorecap = TRUE, flame_range = 0, silent = FALSE, smoke = TRUE)
	if(!power)
		return
	var/range = 0
	range = round((2 * power)**GLOB.DYN_EX_SCALE)
	explosion(epicenter, round(range * 0.25), round(range * 0.5), round(range), flash_range*range, adminlog, ignorecap, flame_range*range, silent, smoke)

// Using default dyn_ex scale:
// 100 explosion power is a (5, 10, 20) explosion.
// 75 explosion power is a (4, 8, 17) explosion.
// 50 explosion power is a (3, 7, 14) explosion.
// 25 explosion power is a (2, 5, 10) explosion.
// 10 explosion power is a (1, 3, 6) explosion.
// 5 explosion power is a (0, 1, 3) explosion.
// 1 explosion power is a (0, 0, 1) explosion.
