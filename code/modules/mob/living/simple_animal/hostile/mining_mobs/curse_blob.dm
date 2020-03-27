/mob/living/simple_animal/hostile/asteroid/curseblob
	name = "curse mass"
	desc = "A mass of purple... smoke?"
	icon = 'icons/mob/lavaland/lavaland_monsters.dmi'
	icon_state = "curseblob"
	icon_living = "curseblob"
	icon_aggro = "curseblob"
	mob_biotypes = MOB_SPIRIT
	movement_type = FLYING
	move_to_delay = 5
	vision_range = 20
	aggro_vision_range = 20
	maxHealth = 40 //easy to kill, but oh, will you be seeing a lot of them.
	health = 40
	melee_damage_lower = 10
	melee_damage_upper = 10
	melee_damage_type = BURN
	attack_verb_continuous = "slashes"
	attack_verb_simple = "slash"
	attack_sound = 'sound/effects/curseattack.ogg'
	throw_message = "passes through the smokey body of"
	obj_damage = 0
	environment_smash = ENVIRONMENT_SMASH_NONE
	sentience_type = SENTIENCE_BOSS
	layer = LARGE_MOB_LAYER
	var/doing_move_loop = FALSE
	var/mob/living/set_target
	var/timerid

/mob/living/simple_animal/hostile/asteroid/curseblob/Initialize(mapload)
	. = ..()
	timerid = QDEL_IN(src, 600)
	playsound(src, 'sound/effects/curse1.ogg', 100, TRUE, -1)

/mob/living/simple_animal/hostile/asteroid/curseblob/Destroy()
	new /obj/effect/temp_visual/dir_setting/curse/blob(loc, dir)
	doing_move_loop = FALSE
	return ..()

/mob/living/simple_animal/hostile/asteroid/curseblob/Goto(move_target, delay, minimum_distance)
	move_loop(target, delay)

/mob/living/simple_animal/hostile/asteroid/curseblob/proc/move_loop(move_target, delay)
	set waitfor = FALSE
	if(doing_move_loop)
		return
	doing_move_loop = TRUE
	if(check_for_target())
		return
	while(!QDELETED(src) && doing_move_loop && isturf(loc) && !check_for_target())
		var/step_turf = get_step(src, get_dir(src, set_target))
		if(step_turf != get_turf(set_target))
			forceMove(step_turf)
		sleep(delay)
	doing_move_loop = FALSE

/mob/living/simple_animal/hostile/asteroid/curseblob/proc/check_for_target()
	if(QDELETED(set_target) || set_target.stat != CONSCIOUS || z != set_target.z)
		qdel(src)
		return TRUE

/mob/living/simple_animal/hostile/asteroid/curseblob/GiveTarget(new_target)
	if(check_for_target())
		return
	new_target = set_target
	. = ..()
	Goto(target, move_to_delay)

/mob/living/simple_animal/hostile/asteroid/curseblob/LoseTarget() //we can't lose our target!
	if(check_for_target())
		return

//if it's not our target, we ignore it
/mob/living/simple_animal/hostile/asteroid/curseblob/CanAllowThrough(atom/movable/mover, turf/target)
	. = ..()
	if(mover == set_target)
		return FALSE
	if(istype(mover, /obj/projectile))
		var/obj/projectile/P = mover
		if(P.firer == set_target)
			return FALSE

#define IGNORE_PROC_IF_NOT_TARGET(X) /mob/living/simple_animal/hostile/asteroid/curseblob/##X(AM) { if (AM == set_target) return ..(); }

IGNORE_PROC_IF_NOT_TARGET(attack_hand)

IGNORE_PROC_IF_NOT_TARGET(attack_hulk)

IGNORE_PROC_IF_NOT_TARGET(attack_paw)

IGNORE_PROC_IF_NOT_TARGET(attack_alien)

IGNORE_PROC_IF_NOT_TARGET(attack_larva)

IGNORE_PROC_IF_NOT_TARGET(attack_animal)

IGNORE_PROC_IF_NOT_TARGET(attack_slime)

/mob/living/simple_animal/hostile/asteroid/curseblob/bullet_act(obj/projectile/Proj)
	if(Proj.firer != set_target)
		return
	return ..()

/mob/living/simple_animal/hostile/asteroid/curseblob/attacked_by(obj/item/I, mob/living/L)
	if(L != set_target)
		return
	return ..()

#undef IGNORE_PROC_IF_NOT_TARGET
