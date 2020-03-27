/mob/living/simple_animal/hostile/stickman
	name = "Angry Stick Man"
	desc = "A being from a realm with only 2 dimensions. At least it's trying to stay faced towards you."
	icon_state = "stickman"
	icon_living = "stickman"
	icon_dead = "stickman_dead"
	icon_gib = "syndicate_gib"
	mob_biotypes = MOB_HUMANOID
	gender = MALE
	speak_chance = 0
	turns_per_move = 5
	speed = 0
	stat_attack = UNCONSCIOUS
	robust_searching = 1
	environment_smash = ENVIRONMENT_SMASH_NONE
	maxHealth = 100
	health = 100
	harm_intent_damage = 5
	obj_damage = 0
	melee_damage_lower = 10
	melee_damage_upper = 10
	attack_verb_continuous = "punches"
	attack_verb_simple = "punch"
	attack_sound = 'sound/weapons/punch1.ogg'
	a_intent = INTENT_HARM
	atmos_requirements = list("min_oxy" = 5, "max_oxy" = 0, "min_tox" = 0, "max_tox" = 1, "min_co2" = 0, "max_co2" = 5, "min_n2" = 0, "max_n2" = 0)
	unsuitable_atmos_damage = 15
	faction = list("hostile","stickman")
	check_friendly_fire = 1
	status_flags = CANPUSH
	var/datum/action/boss/wizard_summon_minions/changesummons = /datum/action/boss/wizard_summon_minions
	var/summoned_by_wizard = 0

/mob/living/simple_animal/hostile/stickman/ranged
	ranged = 1
	retreat_distance = 5
	minimum_distance = 5
	icon_state = "stickmanranged"
	icon_living = "stickmanranged"
	casingtype = /obj/item/ammo_casing/c45
	projectilesound = 'sound/misc/bang.ogg'
	loot = list(/obj/item/gun/ballistic/automatic/pistol/stickman)


/mob/living/simple_animal/hostile/stickman/dog
	name = "Angry Stick Dog"
	desc = "Stickman's best friend, if he could see him at least."
	icon_state = "stickdog"
	icon_living = "stickdog"
	icon_dead = "stickdog_dead"
	mob_biotypes = MOB_BEAST

/mob/living/simple_animal/hostile/stickman/Initialize(mapload, wizard_summoned)
	. = ..()
	new /obj/effect/temp_visual/paper_scatter(src)
	summoned_by_wizard = wizard_summoned

/mob/living/simple_animal/hostile/stickman/death()
	..()
	if(summoned_by_wizard == 1)
		changesummons.summoned_minions --
