#define MOOK_ATTACK_NEUTRAL 0
#define MOOK_ATTACK_WARMUP 1
#define MOOK_ATTACK_ACTIVE 2
#define MOOK_ATTACK_RECOVERY 3
#define ATTACK_INTERMISSION_TIME 5

//Fragile but highly aggressive wanderers that pose a large threat in numbers.
//They'll attempt to leap at their target from afar using their hatchets.
/mob/living/simple_animal/hostile/jungle/mook
	name = "wanderer"
	desc = "This unhealthy looking primitive is wielding a rudimentary hatchet, swinging it with wild abandon. One isn't much of a threat, but in numbers they can quickly overwhelm a superior opponent."
	icon = 'icons/mob/jungle/mook.dmi'
	icon_state = "mook"
	icon_living = "mook"
	icon_dead = "mook_dead"
	mob_biotypes = MOB_ORGANIC|MOB_HUMANOID
	pixel_x = -16
	maxHealth = 45
	health = 45
	melee_damage_lower = 30
	melee_damage_upper = 30
	pixel_y = -8
	ranged = TRUE
	ranged_cooldown_time = 10
	pass_flags = LETPASSTHROW
	robust_searching = TRUE
	stat_attack = UNCONSCIOUS
	attack_sound = 'sound/weapons/rapierhit.ogg'
	deathsound = 'sound/voice/mook_death.ogg'
	aggro_vision_range = 15 //A little more aggressive once in combat to balance out their really low HP
	var/attack_state = MOOK_ATTACK_NEUTRAL
	var/struck_target_leap = FALSE

	footstep_type = FOOTSTEP_MOB_BAREFOOT

/mob/living/simple_animal/hostile/jungle/mook/CanAllowThrough(atom/movable/O)
	. = ..()
	if(istype(O, /mob/living/simple_animal/hostile/jungle/mook))
		var/mob/living/simple_animal/hostile/jungle/mook/M = O
		if(M.attack_state == MOOK_ATTACK_ACTIVE && M.throwing)
			return TRUE

/mob/living/simple_animal/hostile/jungle/mook/death()
	desc = "A deceased primitive. Upon closer inspection, it was suffering from severe cellular degeneration and its garments are machine made..."//Can you guess the twist
	return ..()

/mob/living/simple_animal/hostile/jungle/mook/AttackingTarget()
	if(isliving(target))
		if(ranged_cooldown <= world.time && attack_state == MOOK_ATTACK_NEUTRAL)
			var/mob/living/L = target
			if(L.incapacitated())
				WarmupAttack(forced_slash_combo = TRUE)
				return
			WarmupAttack()
		return
	return ..()

/mob/living/simple_animal/hostile/jungle/mook/Goto()
	if(attack_state != MOOK_ATTACK_NEUTRAL)
		return
	return ..()

/mob/living/simple_animal/hostile/jungle/mook/Move()
	if(attack_state == MOOK_ATTACK_WARMUP || attack_state == MOOK_ATTACK_RECOVERY)
		return
	return ..()

/mob/living/simple_animal/hostile/jungle/mook/proc/WarmupAttack(forced_slash_combo = FALSE)
	if(attack_state == MOOK_ATTACK_NEUTRAL && target)
		attack_state = MOOK_ATTACK_WARMUP
		walk(src,0)
		update_icons()
		if(prob(50) && get_dist(src,target) <= 3 || forced_slash_combo)
			addtimer(CALLBACK(src, .proc/SlashCombo), ATTACK_INTERMISSION_TIME)
			return
		addtimer(CALLBACK(src, .proc/LeapAttack), ATTACK_INTERMISSION_TIME + rand(0,3))
		return
	attack_state = MOOK_ATTACK_RECOVERY
	ResetNeutral()

/mob/living/simple_animal/hostile/jungle/mook/proc/SlashCombo()
	if(attack_state == MOOK_ATTACK_WARMUP && !stat)
		attack_state = MOOK_ATTACK_ACTIVE
		update_icons()
		SlashAttack()
		addtimer(CALLBACK(src, .proc/SlashAttack), 3)
		addtimer(CALLBACK(src, .proc/SlashAttack), 6)
		addtimer(CALLBACK(src, .proc/AttackRecovery), 9)

/mob/living/simple_animal/hostile/jungle/mook/proc/SlashAttack()
	if(target && !stat && attack_state == MOOK_ATTACK_ACTIVE)
		melee_damage_lower = 15
		melee_damage_upper = 15
		var/mob_direction = get_dir(src,target)
		if(get_dist(src,target) > 1)
			step(src,mob_direction)
		if(targets_from && isturf(targets_from.loc) && target.Adjacent(targets_from) && isliving(target))
			var/mob/living/L = target
			L.attack_animal(src)
			return
		var/swing_turf = get_step(src,mob_direction)
		new /obj/effect/temp_visual/kinetic_blast(swing_turf)
		playsound(src, 'sound/weapons/slashmiss.ogg', 50, TRUE)

/mob/living/simple_animal/hostile/jungle/mook/proc/LeapAttack()
	if(target && !stat && attack_state == MOOK_ATTACK_WARMUP)
		attack_state = MOOK_ATTACK_ACTIVE
		density = FALSE
		melee_damage_lower = 30
		melee_damage_upper = 30
		update_icons()
		new /obj/effect/temp_visual/mook_dust(get_turf(src))
		playsound(src, 'sound/weapons/thudswoosh.ogg', 25, TRUE)
		playsound(src, 'sound/voice/mook_leap_yell.ogg', 100, TRUE)
		var/target_turf = get_turf(target)
		throw_at(target_turf, 7, 1, src, FALSE, callback = CALLBACK(src, .proc/AttackRecovery))
		return
	attack_state = MOOK_ATTACK_RECOVERY
	ResetNeutral()

/mob/living/simple_animal/hostile/jungle/mook/proc/AttackRecovery()
	if(attack_state == MOOK_ATTACK_ACTIVE && !stat)
		attack_state = MOOK_ATTACK_RECOVERY
		density = TRUE
		face_atom(target)
		if(!struck_target_leap)
			update_icons()
		struck_target_leap = FALSE
		if(prob(40))
			attack_state = MOOK_ATTACK_NEUTRAL
			if(target)
				if(isliving(target))
					var/mob/living/L = target
					if(L.incapacitated() && L.stat != DEAD)
						addtimer(CALLBACK(src, .proc/WarmupAttack, TRUE), ATTACK_INTERMISSION_TIME)
						return
			addtimer(CALLBACK(src, .proc/WarmupAttack), ATTACK_INTERMISSION_TIME)
			return
		addtimer(CALLBACK(src, .proc/ResetNeutral), ATTACK_INTERMISSION_TIME)

/mob/living/simple_animal/hostile/jungle/mook/proc/ResetNeutral()
	if(attack_state == MOOK_ATTACK_RECOVERY)
		attack_state = MOOK_ATTACK_NEUTRAL
		ranged_cooldown = world.time + ranged_cooldown_time
		update_icons()
		if(target && !stat)
			update_icons()
			Goto(target, move_to_delay, minimum_distance)

/mob/living/simple_animal/hostile/jungle/mook/throw_impact(atom/hit_atom, datum/thrownthing/throwingdatum)
	. = ..()
	if(isliving(hit_atom) && attack_state == MOOK_ATTACK_ACTIVE)
		var/mob/living/L = hit_atom
		if(CanAttack(L))
			L.attack_animal(src)
			struck_target_leap = TRUE
			density = TRUE
			update_icons()
	var/mook_under_us = FALSE
	for(var/A in get_turf(src))
		if(struck_target_leap && mook_under_us)
			break
		if(A == src)
			continue
		if(isliving(A))
			var/mob/living/ML = A
			if(!struck_target_leap && CanAttack(ML))//Check if some joker is attempting to use rest to evade us
				struck_target_leap = TRUE
				ML.attack_animal(src)
				density = TRUE
				struck_target_leap = TRUE
				update_icons()
				continue
			if(istype(ML, /mob/living/simple_animal/hostile/jungle/mook) && !mook_under_us)//If we land on the same tile as another mook, spread out so we don't stack our sprite on the same tile
				var/mob/living/simple_animal/hostile/jungle/mook/M = ML
				if(!M.stat)
					mook_under_us = TRUE
					var/anydir = pick(GLOB.cardinals)
					Move(get_step(src, anydir), anydir)
					continue

/mob/living/simple_animal/hostile/jungle/mook/handle_automated_action()
	if(attack_state)
		return
	return ..()

/mob/living/simple_animal/hostile/jungle/mook/OpenFire()
	if(isliving(target))
		var/mob/living/L = target
		if(L.incapacitated())
			return
	WarmupAttack()

/mob/living/simple_animal/hostile/jungle/mook/update_icons()
	. = ..()
	if(!stat)
		switch(attack_state)
			if(MOOK_ATTACK_NEUTRAL)
				icon_state = "mook"
			if(MOOK_ATTACK_WARMUP)
				icon_state = "mook_warmup"
			if(MOOK_ATTACK_ACTIVE)
				if(!density)
					icon_state = "mook_leap"
					return
				if(struck_target_leap)
					icon_state = "mook_strike"
					return
				icon_state = "mook_slash_combo"
			if(MOOK_ATTACK_RECOVERY)
				icon_state = "mook"

/obj/effect/temp_visual/mook_dust
	name = "dust"
	desc = "It's just a dust cloud!"
	icon = 'icons/mob/jungle/mook.dmi'
	icon_state = "mook_leap_cloud"
	layer = BELOW_MOB_LAYER
	pixel_x = -16
	pixel_y = -16
	duration = 10

#undef MOOK_ATTACK_NEUTRAL
#undef MOOK_ATTACK_WARMUP
#undef MOOK_ATTACK_ACTIVE
#undef MOOK_ATTACK_RECOVERY
#undef ATTACK_INTERMISSION_TIME
