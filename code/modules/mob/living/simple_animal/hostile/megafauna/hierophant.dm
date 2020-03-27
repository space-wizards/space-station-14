/*

The Hierophant

The Hierophant spawns in its arena, which makes fighting it challenging but not impossible.

The text this boss speaks is ROT4, use ROT22 to decode

The Hierophant's attacks are as follows;
- These attacks happen at a random, increasing chance:
	If target is at least 2 tiles away; Blinks to the target after a very brief delay, damaging everything near the start and end points.
		As above, but does so multiple times if below half health.
	Rapidly creates cardinal and diagonal Cross Blasts under a target.
	If chasers are off cooldown, creates 4 chasers.

- IF TARGET IS OUTSIDE THE ARENA: Creates an arena around the target for 10 seconds, blinking to the target if not in the created arena.
	The arena has a 20 second cooldown, giving people a small window to get the fuck out.

- If no chasers exist, creates a chaser that will seek its target, leaving a trail of blasts.
	Is more likely to create a second, slower, chaser if hurt.
- If the target is at least 2 tiles away, may Blink to the target after a very brief delay, damaging everything near the start and end points.
- Creates a cardinal or diagonal blast(Cross Blast) under its target, exploding after a short time.
	If below half health, the created Cross Blast may fire in all directions.
- Creates an expanding AoE burst.

- IF ATTACKING IN MELEE: Creates an expanding AoE burst.

Cross Blasts and the AoE burst gain additional range as Hierophant loses health, while Chasers gain additional speed.

When Hierophant dies, it stops trying to murder you and shrinks into a small form, which, while much weaker, is still quite effective.
- The smaller club can place a teleport beacon, allowing the user to teleport themself and their allies to the beacon.

Difficulty: Hard

*/

/mob/living/simple_animal/hostile/megafauna/hierophant
	name = "hierophant"
	desc = "A massive metal club that hangs in the air as though waiting. It'll make you dance to its beat."
	health = 2500
	maxHealth = 2500
	attack_verb_continuous = "clubs"
	attack_verb_simple = "club"
	attack_sound = 'sound/weapons/sonic_jackhammer.ogg'
	icon_state = "hierophant"
	icon_living = "hierophant"
	friendly_verb_continuous = "stares down"
	friendly_verb_simple = "stare down"
	icon = 'icons/mob/lavaland/hierophant_new.dmi'
	faction = list("boss") //asteroid mobs? get that shit out of my beautiful square house
	speak_emote = list("preaches")
	armour_penetration = 50
	melee_damage_lower = 15
	melee_damage_upper = 15
	speed = 10
	move_to_delay = 10
	ranged = TRUE
	ranged_cooldown_time = 40
	aggro_vision_range = 21 //so it can see to one side of the arena to the other
	loot = list(/obj/item/hierophant_club)
	crusher_loot = list(/obj/item/hierophant_club, /obj/item/crusher_trophy/vortex_talisman)
	wander = FALSE
	gps_name = "Zealous Signal"
	achievement_type = /datum/award/achievement/boss/hierophant_kill
	crusher_achievement_type = /datum/award/achievement/boss/hierophant_crusher
	score_achievement_type = /datum/award/score/hierophant_score
	del_on_death = TRUE
	deathsound = 'sound/magic/repulse.ogg'
	attack_action_types = list(/datum/action/innate/megafauna_attack/blink,
							   /datum/action/innate/megafauna_attack/chaser_swarm,
							   /datum/action/innate/megafauna_attack/cross_blasts,
							   /datum/action/innate/megafauna_attack/blink_spam)

	var/burst_range = 3 //range on burst aoe
	var/beam_range = 5 //range on cross blast beams
	var/chaser_speed = 3 //how fast chasers are currently
	var/chaser_cooldown = 101 //base cooldown/cooldown var between spawning chasers
	var/major_attack_cooldown = 60 //base cooldown for major attacks
	var/arena_cooldown = 200 //base cooldown/cooldown var for creating an arena
	var/blinking = FALSE //if we're doing something that requires us to stand still and not attack
	var/obj/effect/hierophant/spawned_beacon //the beacon we teleport back to
	var/timeout_time = 15 //after this many Life() ticks with no target, we return to our beacon
	var/did_reset = TRUE //if we timed out, returned to our beacon, and healed some
	var/list/kill_phrases = list("Wsyvgi sj irivkc xettih. Vitemvmrk...", "Irivkc wsyvgi jsyrh. Vitemvmrk...", "Jyip jsyrh. Egxmzexmrk vitemv gcgpiw...", "Kix fiex. Liepmrk...")
	var/list/target_phrases = list("Xevkix psgexih.", "Iriqc jsyrh.", "Eguymvih xevkix.")
	var/list/stored_nearby = list() // stores people nearby the hierophant when it enters the death animation

/mob/living/simple_animal/hostile/megafauna/hierophant/Initialize()
	. = ..()
	spawned_beacon = new(loc)

/datum/action/innate/megafauna_attack/blink
	name = "Blink To Target"
	icon_icon = 'icons/mob/actions/actions_items.dmi'
	button_icon_state = "sniper_zoom"
	chosen_message = "<span class='colossus'>You are now blinking to your target.</span>"
	chosen_attack_num = 1

/datum/action/innate/megafauna_attack/chaser_swarm
	name = "Chaser Swarm"
	icon_icon = 'icons/effects/effects.dmi'
	button_icon_state = "hierophant_squares_indefinite"
	chosen_message = "<span class='colossus'>You are firing a chaser swarm at your target.</span>"
	chosen_attack_num = 2

/datum/action/innate/megafauna_attack/cross_blasts
	name = "Cross Blasts"
	icon_icon = 'icons/effects/effects.dmi'
	button_icon_state = "hierophant_blast_indefinite"
	chosen_message = "<span class='colossus'>You are now firing cross blasts at your target.</span>"
	chosen_attack_num = 3

/datum/action/innate/megafauna_attack/blink_spam
	name = "Blink Chase"
	icon_icon = 'icons/obj/lavaland/artefacts.dmi'
	button_icon_state = "hierophant_club_ready_beacon"
	chosen_message = "<span class='colossus'>You are now repeatedly blinking at your target.</span>"
	chosen_attack_num = 4

/mob/living/simple_animal/hostile/megafauna/hierophant/OpenFire()
	if(blinking)
		return

	calculate_rage()
	var/blink_counter = 1 + round(anger_modifier * 0.08)
	var/cross_counter = 1 + round(anger_modifier * 0.12)

	arena_trap(target)
	ranged_cooldown = world.time + max(5, ranged_cooldown_time - anger_modifier * 0.75) //scale cooldown lower with high anger.

	var/target_slowness = 0
	var/mob/living/L
	if(isliving(target))
		L = target
		target_slowness += L.cached_multiplicative_slowdown
	if(client)
		target_slowness += 1

	target_slowness = max(target_slowness, 1)
	chaser_speed = max(1, (3 - anger_modifier * 0.04) + ((target_slowness - 1) * 0.5))

	if(client)
		switch(chosen_attack)
			if(1)
				blink(target)
			if(2)
				chaser_swarm(blink_counter, target_slowness, cross_counter)
			if(3)
				cross_blast_spam(blink_counter, target_slowness, cross_counter)
			if(4)
				blink_spam(blink_counter, target_slowness, cross_counter)
		return

	if(L?.stat == DEAD && !blinking && get_dist(src, L) > 2)
		blink(L)
		return

	if(prob(anger_modifier * 0.75)) //major ranged attack
		var/list/possibilities = list()
		if(cross_counter > 1)
			possibilities += "cross_blast_spam"
		if(get_dist(src, target) > 2)
			possibilities += "blink_spam"
		if(chaser_cooldown < world.time)
			if(prob(anger_modifier * 2))
				possibilities = list("chaser_swarm")
			else
				possibilities += "chaser_swarm"
		if(possibilities.len)
			switch(pick(possibilities))
				if("blink_spam") //blink either once or multiple times.
					blink_spam(blink_counter, target_slowness, cross_counter)
				if("cross_blast_spam") //fire a lot of cross blasts at a target.
					cross_blast_spam(blink_counter, target_slowness, cross_counter)
				if("chaser_swarm") //fire four fucking chasers at a target and their friends.
					chaser_swarm(blink_counter, target_slowness, cross_counter)
			return

	if(chaser_cooldown < world.time) //if chasers are off cooldown, fire some!
		var/obj/effect/temp_visual/hierophant/chaser/C = new /obj/effect/temp_visual/hierophant/chaser(loc, src, target, chaser_speed, FALSE)
		chaser_cooldown = world.time + initial(chaser_cooldown)
		if((prob(anger_modifier) || target.Adjacent(src)) && target != src)
			var/obj/effect/temp_visual/hierophant/chaser/OC = new(loc, src, target, chaser_speed * 1.5, FALSE)
			OC.moving = 4
			OC.moving_dir = pick(GLOB.cardinals - C.moving_dir)

	else if(prob(10 + (anger_modifier * 0.5)) && get_dist(src, target) > 2)
		blink(target)

	else if(prob(70 - anger_modifier)) //a cross blast of some type
		if(prob(anger_modifier * (2 / target_slowness)) && health < maxHealth * 0.5) //we're super angry do it at all dirs
			INVOKE_ASYNC(src, .proc/blasts, target, GLOB.alldirs)
		else if(prob(60))
			INVOKE_ASYNC(src, .proc/blasts, target, GLOB.cardinals)
		else
			INVOKE_ASYNC(src, .proc/blasts, target, GLOB.diagonals)
	else //just release a burst of power
		INVOKE_ASYNC(src, .proc/burst, get_turf(src))

/mob/living/simple_animal/hostile/megafauna/hierophant/proc/blink_spam(blink_counter, target_slowness, cross_counter)
	ranged_cooldown = world.time + max(5, major_attack_cooldown - anger_modifier * 0.75)
	if(health < maxHealth * 0.5 && blink_counter > 1)
		visible_message("<span class='hierophant'>\"Mx ampp rsx iwgeti.\"</span>")
		var/oldcolor = color
		animate(src, color = "#660099", time = 6)
		SLEEP_CHECK_DEATH(6)
		while(!QDELETED(target) && blink_counter)
			if(loc == target.loc || loc == target) //we're on the same tile as them after about a second we can stop now
				break
			blink_counter--
			blinking = FALSE
			blink(target)
			blinking = TRUE
			SLEEP_CHECK_DEATH(4 + target_slowness)
		animate(src, color = oldcolor, time = 8)
		addtimer(CALLBACK(src, /atom/proc/update_atom_colour), 8)
		SLEEP_CHECK_DEATH(8)
		blinking = FALSE
	else
		blink(target)

/mob/living/simple_animal/hostile/megafauna/hierophant/proc/cross_blast_spam(blink_counter, target_slowness, cross_counter)
	ranged_cooldown = world.time + max(5, major_attack_cooldown - anger_modifier * 0.75)
	visible_message("<span class='hierophant'>\"Piezi mx rsalivi xs vyr.\"</span>")
	blinking = TRUE
	var/oldcolor = color
	animate(src, color = "#660099", time = 6)
	SLEEP_CHECK_DEATH(6)
	while(!QDELETED(target) && cross_counter)
		cross_counter--
		if(prob(60))
			INVOKE_ASYNC(src, .proc/blasts, target, GLOB.cardinals)
		else
			INVOKE_ASYNC(src, .proc/blasts, target, GLOB.diagonals)
		SLEEP_CHECK_DEATH(6 + target_slowness)
	animate(src, color = oldcolor, time = 8)
	addtimer(CALLBACK(src, /atom/proc/update_atom_colour), 8)
	SLEEP_CHECK_DEATH(8)
	blinking = FALSE


/mob/living/simple_animal/hostile/megafauna/hierophant/proc/chaser_swarm(blink_counter, target_slowness, cross_counter)
	ranged_cooldown = world.time + max(5, major_attack_cooldown - anger_modifier * 0.75)
	visible_message("<span class='hierophant'>\"Mx gerrsx lmhi.\"</span>")
	blinking = TRUE
	var/oldcolor = color
	animate(src, color = "#660099", time = 6)
	SLEEP_CHECK_DEATH(6)
	var/list/targets = ListTargets()
	var/list/cardinal_copy = GLOB.cardinals.Copy()
	while(targets.len && cardinal_copy.len)
		var/mob/living/pickedtarget = pick(targets)
		if(targets.len >= cardinal_copy.len)
			pickedtarget = pick_n_take(targets)
		if(!istype(pickedtarget) || pickedtarget.stat == DEAD)
			pickedtarget = target
			if(QDELETED(pickedtarget) || (istype(pickedtarget) && pickedtarget.stat == DEAD))
				break //main target is dead and we're out of living targets, cancel out
		var/obj/effect/temp_visual/hierophant/chaser/C = new(loc, src, pickedtarget, chaser_speed, FALSE)
		C.moving = 3
		C.moving_dir = pick_n_take(cardinal_copy)
		SLEEP_CHECK_DEATH(8 + target_slowness)
	chaser_cooldown = world.time + initial(chaser_cooldown)
	animate(src, color = oldcolor, time = 8)
	addtimer(CALLBACK(src, /atom/proc/update_atom_colour), 8)
	SLEEP_CHECK_DEATH(8)
	blinking = FALSE

/mob/living/simple_animal/hostile/megafauna/hierophant/proc/blasts(mob/victim, list/directions = GLOB.cardinals) //fires cross blasts with a delay
	var/turf/T = get_turf(victim)
	if(!T)
		return
	if(directions == GLOB.cardinals)
		new /obj/effect/temp_visual/hierophant/telegraph/cardinal(T, src)
	else if(directions == GLOB.diagonals)
		new /obj/effect/temp_visual/hierophant/telegraph/diagonal(T, src)
	else
		new /obj/effect/temp_visual/hierophant/telegraph(T, src)
	playsound(T,'sound/effects/bin_close.ogg', 200, TRUE)
	SLEEP_CHECK_DEATH(2)
	new /obj/effect/temp_visual/hierophant/blast(T, src, FALSE)
	for(var/d in directions)
		INVOKE_ASYNC(src, .proc/blast_wall, T, d)

/mob/living/simple_animal/hostile/megafauna/hierophant/proc/blast_wall(turf/T, set_dir) //make a wall of blasts beam_range tiles long
	var/range = beam_range
	var/turf/previousturf = T
	var/turf/J = get_step(previousturf, set_dir)
	for(var/i in 1 to range)
		new /obj/effect/temp_visual/hierophant/blast(J, src, FALSE)
		previousturf = J
		J = get_step(previousturf, set_dir)

/mob/living/simple_animal/hostile/megafauna/hierophant/proc/arena_trap(mob/victim) //trap a target in an arena
	var/turf/T = get_turf(victim)
	if(!istype(victim) || victim.stat == DEAD || !T || arena_cooldown > world.time)
		return
	if((istype(get_area(T), /area/ruin/unpowered/hierophant) || istype(get_area(src), /area/ruin/unpowered/hierophant)) && victim != src)
		return
	arena_cooldown = world.time + initial(arena_cooldown)
	for(var/d in GLOB.cardinals)
		INVOKE_ASYNC(src, .proc/arena_squares, T, d)
	for(var/t in RANGE_TURFS(11, T))
		if(t && get_dist(t, T) == 11)
			new /obj/effect/temp_visual/hierophant/wall(t, src)
			new /obj/effect/temp_visual/hierophant/blast(t, src, FALSE)
	if(get_dist(src, T) >= 11) //hey you're out of range I need to get closer to you!
		INVOKE_ASYNC(src, .proc/blink, T)

/mob/living/simple_animal/hostile/megafauna/hierophant/proc/arena_squares(turf/T, set_dir) //make a fancy effect extending from the arena target
	var/turf/previousturf = T
	var/turf/J = get_step(previousturf, set_dir)
	for(var/i in 1 to 10)
		var/obj/effect/temp_visual/hierophant/squares/HS = new(J)
		HS.setDir(set_dir)
		previousturf = J
		J = get_step(previousturf, set_dir)
		SLEEP_CHECK_DEATH(0.5)

/mob/living/simple_animal/hostile/megafauna/hierophant/proc/blink(mob/victim) //blink to a target
	if(blinking || !victim)
		return
	var/turf/T = get_turf(victim)
	var/turf/source = get_turf(src)
	new /obj/effect/temp_visual/hierophant/telegraph(T, src)
	new /obj/effect/temp_visual/hierophant/telegraph(source, src)
	playsound(T,'sound/magic/wand_teleport.ogg', 200, TRUE)
	playsound(source,'sound/machines/airlockopen.ogg', 200, TRUE)
	blinking = TRUE
	SLEEP_CHECK_DEATH(2) //short delay before we start...
	new /obj/effect/temp_visual/hierophant/telegraph/teleport(T, src)
	new /obj/effect/temp_visual/hierophant/telegraph/teleport(source, src)
	for(var/t in RANGE_TURFS(1, T))
		var/obj/effect/temp_visual/hierophant/blast/B = new(t, src, FALSE)
		B.damage = 30
	for(var/t in RANGE_TURFS(1, source))
		var/obj/effect/temp_visual/hierophant/blast/B = new(t, src, FALSE)
		B.damage = 30
	animate(src, alpha = 0, time = 2, easing = EASE_OUT) //fade out
	SLEEP_CHECK_DEATH(1)
	visible_message("<span class='hierophant_warning'>[src] fades out!</span>")
	density = FALSE
	SLEEP_CHECK_DEATH(2)
	forceMove(T)
	SLEEP_CHECK_DEATH(1)
	animate(src, alpha = 255, time = 2, easing = EASE_IN) //fade IN
	SLEEP_CHECK_DEATH(1)
	density = TRUE
	visible_message("<span class='hierophant_warning'>[src] fades in!</span>")
	SLEEP_CHECK_DEATH(1) //at this point the blasts we made detonate
	blinking = FALSE

/mob/living/simple_animal/hostile/megafauna/hierophant/proc/melee_blast(mob/victim) //make a 3x3 blast around a target
	if(!victim)
		return
	var/turf/T = get_turf(victim)
	if(!T)
		return
	new /obj/effect/temp_visual/hierophant/telegraph(T, src)
	playsound(T,'sound/effects/bin_close.ogg', 200, TRUE)
	SLEEP_CHECK_DEATH(2)
	for(var/t in RANGE_TURFS(1, T))
		new /obj/effect/temp_visual/hierophant/blast(t, src, FALSE)

//expanding square
/proc/hierophant_burst(mob/caster, turf/original, burst_range, spread_speed = 0.5)
	playsound(original,'sound/machines/airlockopen.ogg', 200, TRUE)
	var/last_dist = 0
	for(var/t in spiral_range_turfs(burst_range, original))
		var/turf/T = t
		if(!T)
			continue
		var/dist = get_dist(original, T)
		if(dist > last_dist)
			last_dist = dist
			sleep(1 + min(burst_range - last_dist, 12) * spread_speed) //gets faster as it gets further out
		new /obj/effect/temp_visual/hierophant/blast(T, caster, FALSE)

/mob/living/simple_animal/hostile/megafauna/hierophant/proc/burst(turf/original, spread_speed)
	hierophant_burst(src, original, burst_range, spread_speed)

/mob/living/simple_animal/hostile/megafauna/hierophant/Life()
	. = ..()
	if(. && spawned_beacon && !QDELETED(spawned_beacon) && !client)
		if(target || loc == spawned_beacon.loc)
			timeout_time = initial(timeout_time)
		else
			timeout_time--
		if(timeout_time <= 0 && !did_reset)
			did_reset = TRUE
			visible_message("<span class='hierophant_warning'>\"Vixyvrmrk xs fewi...\"</span>")
			blink(spawned_beacon)
			adjustHealth(min((health - maxHealth) * 0.5, -250)) //heal for 50% of our missing health, minimum 10% of maximum health
			wander = FALSE
			if(health > maxHealth * 0.9)
				visible_message("<span class='hierophant'>\"Vitemvw gsqtpixi. Stivexmrk ex qebmqyq ijjmgmirgc.\"</span>")
			else
				visible_message("<span class='hierophant'>\"Vitemvw gsqtpixi. Stivexmsrep ijjmgmirgc gsqtvsqmwih.\"</span>")

/mob/living/simple_animal/hostile/megafauna/hierophant/death()
	if(health > 0 || stat == DEAD)
		return
	else
		set_stat(DEAD)
		blinking = TRUE //we do a fancy animation, release a huge burst(), and leave our staff.
		visible_message("<span class='hierophant'>\"Mrmxmexmrk wipj-hiwxvygx wiuyirgi...\"</span>")
		visible_message("<span class='hierophant_warning'>[src] shrinks, releasing a massive burst of energy!</span>")
		for(var/mob/living/L in view(7,src))
			stored_nearby += L // store the people to grant the achievements to once we die
		hierophant_burst(null, get_turf(src), 10)
		set_stat(CONSCIOUS) // deathgasp wont run if dead, stupid
		..(force_grant = stored_nearby)

/mob/living/simple_animal/hostile/megafauna/hierophant/Destroy()
	qdel(spawned_beacon)
	. = ..()

/mob/living/simple_animal/hostile/megafauna/hierophant/devour(mob/living/L)
	for(var/obj/item/W in L)
		if(!L.dropItemToGround(W))
			qdel(W)
	visible_message("<span class='hierophant_warning'>\"[pick(kill_phrases)]\"</span>")
	visible_message("<span class='hierophant_warning'>[src] annihilates [L]!</span>","<span class='userdanger'>You annihilate [L], restoring your health!</span>")
	adjustHealth(-L.maxHealth*0.5)
	L.dust()

/mob/living/simple_animal/hostile/megafauna/hierophant/CanAttack(atom/the_target)
	. = ..()
	if(istype(the_target, /mob/living/simple_animal/hostile/asteroid/hivelordbrood)) //ignore temporary targets in favor of more permanent targets
		return FALSE

/mob/living/simple_animal/hostile/megafauna/hierophant/GiveTarget(new_target)
	var/targets_the_same = (new_target == target)
	. = ..()
	if(. && target && !targets_the_same)
		visible_message("<span class='hierophant_warning'>\"[pick(target_phrases)]\"</span>")
		if(spawned_beacon && loc == spawned_beacon.loc && did_reset)
			arena_trap(src)

/mob/living/simple_animal/hostile/megafauna/hierophant/adjustHealth(amount, updating_health = TRUE, forced = FALSE)
	. = ..()
	if(src && . && !blinking)
		wander = TRUE
		did_reset = FALSE

/mob/living/simple_animal/hostile/megafauna/hierophant/AttackingTarget()
	if(!blinking)
		if(target && isliving(target))
			var/mob/living/L = target
			if(L.stat != DEAD)
				if(ranged_cooldown <= world.time)
					calculate_rage()
					ranged_cooldown = world.time + max(5, ranged_cooldown_time - anger_modifier * 0.75)
					INVOKE_ASYNC(src, .proc/burst, get_turf(src))
				else
					burst_range = 3
					INVOKE_ASYNC(src, .proc/burst, get_turf(src), 0.25) //melee attacks on living mobs cause it to release a fast burst if on cooldown
			else
				devour(L)
		else
			return ..()

/mob/living/simple_animal/hostile/megafauna/hierophant/DestroySurroundings()
	if(!blinking)
		..()

/mob/living/simple_animal/hostile/megafauna/hierophant/Move()
	if(!blinking)
		. = ..()

/mob/living/simple_animal/hostile/megafauna/hierophant/Moved(oldLoc, movement_dir)
	. = ..()
	if(!stat && .)
		var/obj/effect/temp_visual/hierophant/squares/HS = new(oldLoc)
		HS.setDir(movement_dir)
		playsound(src, 'sound/mecha/mechmove04.ogg', 150, TRUE, -4)
		if(target)
			arena_trap(target)

/mob/living/simple_animal/hostile/megafauna/hierophant/Goto(target, delay, minimum_distance)
	wander = TRUE
	if(!blinking)
		..()

/mob/living/simple_animal/hostile/megafauna/hierophant/proc/calculate_rage() //how angry we are overall
	did_reset = FALSE //oh hey we're doing SOMETHING, clearly we might need to heal if we recall
	anger_modifier = CLAMP(((maxHealth - health) / 42),0,50)
	burst_range = initial(burst_range) + round(anger_modifier * 0.08)
	beam_range = initial(beam_range) + round(anger_modifier * 0.12)

//Hierophant overlays
/obj/effect/temp_visual/hierophant
	name = "vortex energy"
	layer = BELOW_MOB_LAYER
	var/mob/living/caster //who made this, anyway

/obj/effect/temp_visual/hierophant/Initialize(mapload, new_caster)
	. = ..()
	if(new_caster)
		caster = new_caster

/obj/effect/temp_visual/hierophant/squares
	icon_state = "hierophant_squares"
	duration = 3
	light_range = MINIMUM_USEFUL_LIGHT_RANGE
	randomdir = FALSE

/obj/effect/temp_visual/hierophant/squares/Initialize(mapload, new_caster)
	. = ..()
	if(ismineralturf(loc))
		var/turf/closed/mineral/M = loc
		M.gets_drilled(caster)

/obj/effect/temp_visual/hierophant/wall //smoothing and pooling were not friends, but pooling is dead.
	name = "vortex wall"
	icon = 'icons/turf/walls/hierophant_wall_temp.dmi'
	icon_state = "wall"
	light_range = MINIMUM_USEFUL_LIGHT_RANGE
	duration = 100
	smooth = SMOOTH_TRUE

/obj/effect/temp_visual/hierophant/wall/Initialize(mapload, new_caster)
	. = ..()
	queue_smooth_neighbors(src)
	queue_smooth(src)

/obj/effect/temp_visual/hierophant/wall/Destroy()
	queue_smooth_neighbors(src)
	return ..()

/obj/effect/temp_visual/hierophant/wall/CanAllowThrough(atom/movable/mover, turf/target)
	. = ..()
	if(QDELETED(caster))
		return FALSE
	if(mover == caster.pulledby)
		return
	if(istype(mover, /obj/projectile))
		var/obj/projectile/P = mover
		if(P.firer == caster)
			return
	if(mover != caster)
		return FALSE

/obj/effect/temp_visual/hierophant/chaser //a hierophant's chaser. follows target around, moving and producing a blast every speed deciseconds.
	duration = 98
	var/mob/living/target //what it's following
	var/turf/targetturf //what turf the target is actually on
	var/moving_dir //what dir it's moving in
	var/previous_moving_dir //what dir it was moving in before that
	var/more_previouser_moving_dir //what dir it was moving in before THAT
	var/moving = 0 //how many steps to move before recalculating
	var/standard_moving_before_recalc = 4 //how many times we step before recalculating normally
	var/tiles_per_step = 1 //how many tiles we move each step
	var/speed = 3 //how many deciseconds between each step
	var/currently_seeking = FALSE
	var/friendly_fire_check = FALSE //if blasts produced apply friendly fire
	var/monster_damage_boost = TRUE
	var/damage = 10

/obj/effect/temp_visual/hierophant/chaser/Initialize(mapload, new_caster, new_target, new_speed, is_friendly_fire)
	. = ..()
	target = new_target
	friendly_fire_check = is_friendly_fire
	if(new_speed)
		speed = new_speed
	addtimer(CALLBACK(src, .proc/seek_target), 1)

/obj/effect/temp_visual/hierophant/chaser/proc/get_target_dir()
	. = get_cardinal_dir(src, targetturf)
	if((. != previous_moving_dir && . == more_previouser_moving_dir) || . == 0) //we're alternating, recalculate
		var/list/cardinal_copy = GLOB.cardinals.Copy()
		cardinal_copy -= more_previouser_moving_dir
		. = pick(cardinal_copy)

/obj/effect/temp_visual/hierophant/chaser/proc/seek_target()
	if(!currently_seeking)
		currently_seeking = TRUE
		targetturf = get_turf(target)
		while(target && src && !QDELETED(src) && currently_seeking && x && y && targetturf) //can this target actually be sook out
			if(!moving) //we're out of tiles to move, find more and where the target is!
				more_previouser_moving_dir = previous_moving_dir
				previous_moving_dir = moving_dir
				moving_dir = get_target_dir()
				var/standard_target_dir = get_cardinal_dir(src, targetturf)
				if((standard_target_dir != previous_moving_dir && standard_target_dir == more_previouser_moving_dir) || standard_target_dir == 0)
					moving = 1 //we would be repeating, only move a tile before checking
				else
					moving = standard_moving_before_recalc
			if(moving) //move in the dir we're moving in right now
				var/turf/T = get_turf(src)
				for(var/i in 1 to tiles_per_step)
					var/maybe_new_turf = get_step(T, moving_dir)
					if(maybe_new_turf)
						T = maybe_new_turf
					else
						break
				forceMove(T)
				make_blast() //make a blast, too
				moving--
				sleep(speed)
			targetturf = get_turf(target)
/obj/effect/temp_visual/hierophant/chaser/proc/make_blast()
	var/obj/effect/temp_visual/hierophant/blast/B = new(loc, caster, friendly_fire_check)
	B.damage = damage
	B.monster_damage_boost = monster_damage_boost

/obj/effect/temp_visual/hierophant/telegraph
	icon = 'icons/effects/96x96.dmi'
	icon_state = "hierophant_telegraph"
	pixel_x = -32
	pixel_y = -32
	duration = 3

/obj/effect/temp_visual/hierophant/telegraph/diagonal
	icon_state = "hierophant_telegraph_diagonal"

/obj/effect/temp_visual/hierophant/telegraph/cardinal
	icon_state = "hierophant_telegraph_cardinal"

/obj/effect/temp_visual/hierophant/telegraph/teleport
	icon_state = "hierophant_telegraph_teleport"
	duration = 9

/obj/effect/temp_visual/hierophant/telegraph/edge
	icon_state = "hierophant_telegraph_edge"
	duration = 40

/obj/effect/temp_visual/hierophant/blast
	icon_state = "hierophant_blast"
	name = "vortex blast"
	light_range = 2
	light_power = 2
	desc = "Get out of the way!"
	duration = 9
	var/damage = 10 //how much damage do we do?
	var/monster_damage_boost = TRUE //do we deal extra damage to monsters? Used by the boss
	var/list/hit_things = list() //we hit these already, ignore them
	var/friendly_fire_check = FALSE
	var/bursting = FALSE //if we're bursting and need to hit anyone crossing us

/obj/effect/temp_visual/hierophant/blast/Initialize(mapload, new_caster, friendly_fire)
	. = ..()
	friendly_fire_check = friendly_fire
	if(new_caster)
		hit_things += new_caster
	if(ismineralturf(loc)) //drill mineral turfs
		var/turf/closed/mineral/M = loc
		M.gets_drilled(caster)
	INVOKE_ASYNC(src, .proc/blast)

/obj/effect/temp_visual/hierophant/blast/proc/blast()
	var/turf/T = get_turf(src)
	if(!T)
		return
	playsound(T,'sound/magic/blind.ogg', 125, TRUE, -5) //make a sound
	sleep(6) //wait a little
	bursting = TRUE
	do_damage(T) //do damage and mark us as bursting
	sleep(1.3) //slightly forgiving; the burst animation is 1.5 deciseconds
	bursting = FALSE //we no longer damage crossers

/obj/effect/temp_visual/hierophant/blast/Crossed(atom/movable/AM)
	..()
	if(bursting)
		do_damage(get_turf(src))

/obj/effect/temp_visual/hierophant/blast/proc/do_damage(turf/T)
	if(!damage)
		return
	for(var/mob/living/L in T.contents - hit_things) //find and damage mobs...
		hit_things += L
		if((friendly_fire_check && caster && caster.faction_check_mob(L)) || L.stat == DEAD)
			continue
		if(L.client)
			flash_color(L.client, "#660099", 1)
		playsound(L,'sound/weapons/sear.ogg', 50, TRUE, -4)
		to_chat(L, "<span class='userdanger'>You're struck by a [name]!</span>")
		var/limb_to_hit = L.get_bodypart(pick(BODY_ZONE_HEAD, BODY_ZONE_CHEST, BODY_ZONE_R_ARM, BODY_ZONE_L_ARM, BODY_ZONE_R_LEG, BODY_ZONE_L_LEG))
		var/armor = L.run_armor_check(limb_to_hit, "melee", "Your armor absorbs [src]!", "Your armor blocks part of [src]!", 50, "Your armor was penetrated by [src]!")
		L.apply_damage(damage, BURN, limb_to_hit, armor)
		if(ishostile(L))
			var/mob/living/simple_animal/hostile/H = L //mobs find and damage you...
			if(H.stat == CONSCIOUS && !H.target && H.AIStatus != AI_OFF && !H.client)
				if(!QDELETED(caster))
					if(get_dist(H, caster) <= H.aggro_vision_range)
						H.FindTarget(list(caster), 1)
					else
						H.Goto(get_turf(caster), H.move_to_delay, 3)
		if(monster_damage_boost && (ismegafauna(L) || istype(L, /mob/living/simple_animal/hostile/asteroid)))
			L.adjustBruteLoss(damage)
		if(caster)
			log_combat(caster, L, "struck with a [name]")
	for(var/obj/mecha/M in T.contents - hit_things) //also damage mechs.
		hit_things += M
		if(M.occupant)
			if(friendly_fire_check && caster && caster.faction_check_mob(M.occupant))
				continue
			to_chat(M.occupant, "<span class='userdanger'>Your [M.name] is struck by a [name]!</span>")
		playsound(M,'sound/weapons/sear.ogg', 50, TRUE, -4)
		M.take_damage(damage, BURN, 0, 0)

/obj/effect/hierophant
	name = "hierophant beacon"
	desc = "A strange beacon, allowing mass teleportation for those able to use it."
	icon = 'icons/obj/lavaland/artefacts.dmi'
	icon_state = "hierophant_tele_off"
	light_range = 2
	layer = LOW_OBJ_LAYER
	anchored = TRUE

/obj/effect/hierophant/ex_act()
	return

/obj/effect/hierophant/attackby(obj/item/I, mob/user, params)
	if(istype(I, /obj/item/hierophant_club))
		var/obj/item/hierophant_club/H = I
		if(H.timer > world.time)
			return
		if(H.beacon == src)
			to_chat(user, "<span class='notice'>You start removing your hierophant beacon...</span>")
			H.timer = world.time + 51
			INVOKE_ASYNC(H, /obj/item/hierophant_club.proc/prepare_icon_update)
			if(do_after(user, 50, target = src))
				playsound(src,'sound/magic/blind.ogg', 200, TRUE, -4)
				new /obj/effect/temp_visual/hierophant/telegraph/teleport(get_turf(src), user)
				to_chat(user, "<span class='hierophant_warning'>You collect [src], reattaching it to the club!</span>")
				H.beacon = null
				user.update_action_buttons_icon()
				qdel(src)
			else
				H.timer = world.time
				INVOKE_ASYNC(H, /obj/item/hierophant_club.proc/prepare_icon_update)
		else
			to_chat(user, "<span class='hierophant_warning'>You touch the beacon with the club, but nothing happens.</span>")
	else
		return ..()
