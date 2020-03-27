#define MINER_DASH_RANGE 4

/*

BLOOD-DRUNK MINER

Effectively a highly aggressive miner, the blood-drunk miner has very few attacks but compensates by being highly aggressive.

The blood-drunk miner's attacks are as follows
- If not in KA range, it will rapidly dash at its target
- If in KA range, it will fire its kinetic accelerator
- If in melee range, will rapidly attack, akin to an actual player
- After any of these attacks, may transform its cleaving saw:
	Untransformed, it attacks very rapidly for smaller amounts of damage
	Transformed, it attacks at normal speed for higher damage and cleaves enemies hit

When the blood-drunk miner dies, it leaves behind the cleaving saw it was using and its kinetic accelerator.

Difficulty: Medium

*/

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner
	name = "blood-drunk miner"
	desc = "A miner destined to wander forever, engaged in an endless hunt."
	health = 900
	maxHealth = 900
	icon_state = "miner"
	icon_living = "miner"
	icon = 'icons/mob/broadMobs.dmi'
	mob_biotypes = MOB_ORGANIC|MOB_HUMANOID
	light_color = "#E4C7C5"
	movement_type = GROUND
	speak_emote = list("roars")
	speed = 3
	move_to_delay = 3
	projectiletype = /obj/projectile/kinetic/miner
	projectilesound = 'sound/weapons/kenetic_accel.ogg'
	ranged = TRUE
	ranged_cooldown_time = 16
	pixel_x = -16
	crusher_loot = list(/obj/item/melee/transforming/cleaving_saw, /obj/item/gun/energy/kinetic_accelerator, /obj/item/crusher_trophy/miner_eye)
	loot = list(/obj/item/melee/transforming/cleaving_saw, /obj/item/gun/energy/kinetic_accelerator)
	wander = FALSE
	del_on_death = TRUE
	blood_volume = BLOOD_VOLUME_NORMAL
	gps_name = "Resonant Signal"
	achievement_type = /datum/award/achievement/boss/blood_miner_kill
	crusher_achievement_type = /datum/award/achievement/boss/blood_miner_crusher
	score_achievement_type = /datum/award/score/blood_miner_score
	var/obj/item/melee/transforming/cleaving_saw/miner/miner_saw
	var/time_until_next_transform = 0
	var/dashing = FALSE
	var/dash_cooldown = 15
	var/guidance = FALSE
	var/transform_stop_attack = FALSE // stops the blood drunk miner from attacking after transforming his weapon until the next attack chain
	deathmessage = "falls to the ground, decaying into glowing particles."
	deathsound = "bodyfall"
	footstep_type = FOOTSTEP_MOB_HEAVY
	attack_action_types = list(/datum/action/innate/megafauna_attack/dash,
							   /datum/action/innate/megafauna_attack/kinetic_accelerator,
							   /datum/action/innate/megafauna_attack/transform_weapon)

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/Initialize()
	. = ..()
	miner_saw = new(src)

/datum/action/innate/megafauna_attack/dash
	name = "Dash To Target"
	icon_icon = 'icons/mob/actions/actions_items.dmi'
	button_icon_state = "sniper_zoom"
	chosen_message = "<span class='colossus'>You are now dashing to your target.</span>"
	chosen_attack_num = 1

/datum/action/innate/megafauna_attack/kinetic_accelerator
	name = "Fire Kinetic Accelerator"
	icon_icon = 'icons/obj/guns/energy.dmi'
	button_icon_state = "kineticgun"
	chosen_message = "<span class='colossus'>You are now shooting your kinetic accelerator.</span>"
	chosen_attack_num = 2

/datum/action/innate/megafauna_attack/transform_weapon
	name = "Transform Weapon"
	icon_icon = 'icons/obj/lavaland/artefacts.dmi'
	button_icon_state = "cleaving_saw"
	chosen_message = "<span class='colossus'>You are now transforming your weapon.</span>"
	chosen_attack_num = 3

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/OpenFire()
	if(client)
		switch(chosen_attack)
			if(1)
				dash(target)
			if(2)
				shoot_ka()
			if(3)
				transform_weapon()
		return

	Goto(target, move_to_delay, minimum_distance)
	if(get_dist(src, target) > MINER_DASH_RANGE && dash_cooldown <= world.time)
		dash_attack()
	else
		shoot_ka()
	transform_weapon()

/obj/item/melee/transforming/cleaving_saw/miner //nerfed saw because it is very murdery
	force = 6
	force_on = 10

/obj/item/melee/transforming/cleaving_saw/miner/attack(mob/living/target, mob/living/carbon/human/user)
	target.add_stun_absorption("miner", 10, INFINITY)
	..()
	target.stun_absorption -= "miner"

/obj/projectile/kinetic/miner
	damage = 20
	speed = 0.9
	icon_state = "ka_tracer"
	range = MINER_DASH_RANGE

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/adjustHealth(amount, updating_health = TRUE, forced = FALSE)
	var/adjustment_amount = amount * 0.1
	if(world.time + adjustment_amount > next_move)
		changeNext_move(adjustment_amount) //attacking it interrupts it attacking, but only briefly
	. = ..()

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/death()
	. = ..()
	if(.)
		new /obj/effect/temp_visual/dir_setting/miner_death(loc, dir)

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/Move(atom/newloc)
	if(dashing || (newloc && newloc.z == z && (islava(newloc) || ischasm(newloc)))) //we're not stupid!
		return FALSE
	return ..()

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/ex_act(severity, target)
	if(dash())
		return
	return ..()

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/MeleeAction(patience = TRUE)
	transform_stop_attack = FALSE
	return ..()

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/AttackingTarget()
	if(client)
		transform_stop_attack = FALSE
	if(QDELETED(target) || transform_stop_attack)
		return
	face_atom(target)
	if(isliving(target))
		var/mob/living/L = target
		if(L.stat == DEAD)
			visible_message("<span class='danger'>[src] butchers [L]!</span>",
			"<span class='userdanger'>You butcher [L], restoring your health!</span>")
			if(!is_station_level(z) || client) //NPC monsters won't heal while on station
				if(guidance)
					adjustHealth(-L.maxHealth)
				else
					adjustHealth(-(L.maxHealth * 0.5))
			L.gib()
			return TRUE
	changeNext_move(CLICK_CD_MELEE)
	miner_saw.melee_attack_chain(src, target)
	if(guidance)
		adjustHealth(-2)
	return TRUE

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/do_attack_animation(atom/A, visual_effect_icon, obj/item/used_item, no_effect)
	if(!used_item && !isturf(A))
		used_item = miner_saw
	..()

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/GiveTarget(new_target)
	var/targets_the_same = (new_target == target)
	. = ..()
	if(. && target && !targets_the_same)
		wander = TRUE

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/proc/dash_attack()
	INVOKE_ASYNC(src, .proc/dash, target)
	shoot_ka()

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/proc/shoot_ka()
	if(ranged_cooldown <= world.time && get_dist(src, target) <= MINER_DASH_RANGE && !Adjacent(target))
		ranged_cooldown = world.time + ranged_cooldown_time
		visible_message("<span class='danger'>[src] fires the proto-kinetic accelerator!</span>")
		face_atom(target)
		new /obj/effect/temp_visual/dir_setting/firing_effect(loc, dir)
		Shoot(target)
		changeNext_move(CLICK_CD_RANGE)

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/proc/dash(atom/dash_target)
	if(world.time < dash_cooldown)
		return
	var/list/accessable_turfs = list()
	var/self_dist_to_target = 0
	var/turf/own_turf = get_turf(src)
	if(!QDELETED(dash_target))
		self_dist_to_target += get_dist(dash_target, own_turf)
	for(var/turf/open/O in RANGE_TURFS(MINER_DASH_RANGE, own_turf))
		var/turf_dist_to_target = 0
		if(!QDELETED(dash_target))
			turf_dist_to_target += get_dist(dash_target, O)
		if(get_dist(src, O) >= MINER_DASH_RANGE && turf_dist_to_target <= self_dist_to_target && !islava(O) && !ischasm(O))
			var/valid = TRUE
			for(var/turf/T in getline(own_turf, O))
				if(is_blocked_turf(T, TRUE))
					valid = FALSE
					continue
			if(valid)
				accessable_turfs[O] = turf_dist_to_target
	var/turf/target_turf
	if(!QDELETED(dash_target))
		var/closest_dist = MINER_DASH_RANGE
		for(var/t in accessable_turfs)
			if(accessable_turfs[t] < closest_dist)
				closest_dist = accessable_turfs[t]
		for(var/t in accessable_turfs)
			if(accessable_turfs[t] != closest_dist)
				accessable_turfs -= t
	if(!LAZYLEN(accessable_turfs))
		return
	dash_cooldown = world.time + initial(dash_cooldown)
	target_turf = pick(accessable_turfs)
	var/turf/step_back_turf = get_step(target_turf, get_cardinal_dir(target_turf, own_turf))
	var/turf/step_forward_turf = get_step(own_turf, get_cardinal_dir(own_turf, target_turf))
	new /obj/effect/temp_visual/small_smoke/halfsecond(step_back_turf)
	new /obj/effect/temp_visual/small_smoke/halfsecond(step_forward_turf)
	var/obj/effect/temp_visual/decoy/fading/halfsecond/D = new (own_turf, src)
	forceMove(step_back_turf)
	playsound(own_turf, 'sound/weapons/punchmiss.ogg', 40, TRUE, -1)
	dashing = TRUE
	alpha = 0
	animate(src, alpha = 255, time = 5)
	SLEEP_CHECK_DEATH(2)
	D.forceMove(step_forward_turf)
	forceMove(target_turf)
	playsound(target_turf, 'sound/weapons/punchmiss.ogg', 40, TRUE, -1)
	SLEEP_CHECK_DEATH(1)
	dashing = FALSE
	return TRUE

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/proc/transform_weapon()
	if(time_until_next_transform <= world.time)
		miner_saw.transform_cooldown = 0
		miner_saw.transform_weapon(src, TRUE)
		if(!miner_saw.active)
			rapid_melee = 5 // 4 deci cooldown before changes, npcpool subsystem wait is 20, 20/4 = 5
		else
			rapid_melee = 3 // same thing but halved (slightly rounded up)
		transform_stop_attack = TRUE
		icon_state = "miner[miner_saw.active ? "_transformed":""]"
		icon_living = "miner[miner_saw.active ? "_transformed":""]"
		time_until_next_transform = world.time + rand(50, 100)

/obj/effect/temp_visual/dir_setting/miner_death
	icon_state = "miner_death"
	duration = 15

/obj/effect/temp_visual/dir_setting/miner_death/Initialize(mapload, set_dir)
	. = ..()
	INVOKE_ASYNC(src, .proc/fade_out)

/obj/effect/temp_visual/dir_setting/miner_death/proc/fade_out()
	var/matrix/M = new
	M.Turn(pick(90, 270))
	var/final_dir = dir
	if(dir & (EAST|WEST)) //Facing east or west
		final_dir = pick(NORTH, SOUTH) //So you fall on your side rather than your face or ass

	animate(src, transform = M, pixel_y = -6, dir = final_dir, time = 2, easing = EASE_IN|EASE_OUT)
	sleep(5)
	animate(src, color = list("#A7A19E", "#A7A19E", "#A7A19E", list(0, 0, 0)), time = 10, easing = EASE_IN, flags = ANIMATION_PARALLEL)
	sleep(4)
	animate(src, alpha = 0, time = 6, easing = EASE_OUT, flags = ANIMATION_PARALLEL)

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/guidance
	guidance = TRUE

/mob/living/simple_animal/hostile/megafauna/blood_drunk_miner/hunter/AttackingTarget()
	. = ..()
	if(. && prob(12))
		INVOKE_ASYNC(src, .proc/dash)

#undef MINER_DASH_RANGE
