/mob/living/simple_animal/hostile/wizard
	name = "Space Wizard"
	desc = "EI NATH?"
	icon = 'icons/mob/simple_human.dmi'
	icon_state = "wizard"
	icon_living = "wizard"
	icon_dead = "wizard_dead"
	mob_biotypes = MOB_ORGANIC|MOB_HUMANOID
	speak_chance = 0
	turns_per_move = 3
	speed = 0
	maxHealth = 100
	health = 100
	harm_intent_damage = 5
	melee_damage_lower = 5
	melee_damage_upper = 5
	attack_verb_continuous = "punches"
	attack_verb_simple = "punch"
	attack_sound = 'sound/weapons/punch1.ogg'
	a_intent = INTENT_HARM
	atmos_requirements = list("min_oxy" = 5, "max_oxy" = 0, "min_tox" = 0, "max_tox" = 1, "min_co2" = 0, "max_co2" = 5, "min_n2" = 0, "max_n2" = 0)
	unsuitable_atmos_damage = 15
	faction = list(ROLE_WIZARD)
	status_flags = CANPUSH

	retreat_distance = 3 //out of fireball range
	minimum_distance = 3
	del_on_death = 1
	loot = list(/obj/effect/mob_spawn/human/corpse/wizard,
				/obj/item/staff)

	var/obj/effect/proc_holder/spell/aimed/fireball/fireball = null
	var/obj/effect/proc_holder/spell/targeted/turf_teleport/blink/blink = null
	var/obj/effect/proc_holder/spell/targeted/projectile/magic_missile/mm = null

	var/next_cast = 0

	footstep_type = FOOTSTEP_MOB_SHOE

/mob/living/simple_animal/hostile/wizard/Initialize()
	. = ..()
	fireball = new /obj/effect/proc_holder/spell/aimed/fireball
	fireball.clothes_req = 0
	fireball.human_req = 0
	fireball.player_lock = 0
	AddSpell(fireball)
	implants += new /obj/item/implant/exile(src)

	mm = new /obj/effect/proc_holder/spell/targeted/projectile/magic_missile
	mm.clothes_req = 0
	mm.human_req = 0
	mm.player_lock = 0
	AddSpell(mm)

	blink = new /obj/effect/proc_holder/spell/targeted/turf_teleport/blink
	blink.clothes_req = 0
	blink.human_req = 0
	blink.player_lock = 0
	blink.outer_tele_radius = 3
	AddSpell(blink)

/mob/living/simple_animal/hostile/wizard/handle_automated_action()
	. = ..()
	if(target && next_cast < world.time)
		if((get_dir(src,target) in list(SOUTH,EAST,WEST,NORTH)) && fireball.cast_check(0,src)) //Lined up for fireball
			src.setDir(get_dir(src,target))
			fireball.perform(list(target), user = src)
			next_cast = world.time + 10 //One spell per second
			return .
		if(mm.cast_check(0,src))
			mm.choose_targets(src)
			next_cast = world.time + 10
			return .
		if(blink.cast_check(0,src)) //Spam Blink when you can
			blink.choose_targets(src)
			next_cast = world.time + 10
			return .
