//////////////////The Monster

/mob/living/simple_animal/imp
	name = "imp"
	real_name = "imp"
	unique_name = TRUE
	desc = "A large, menacing creature covered in armored black scales."
	speak_emote = list("cackles")
	emote_hear = list("cackles","screeches")
	response_help_continuous = "thinks better of touching"
	response_help_simple = "think better of touching"
	response_disarm_continuous = "flails at"
	response_disarm_simple = "flail at"
	response_harm_continuous = "punches"
	response_harm_simple = "punch"
	icon = 'icons/mob/mob.dmi'
	icon_state = "imp"
	icon_living = "imp"
	mob_biotypes = MOB_ORGANIC|MOB_HUMANOID
	speed = 1
	a_intent = INTENT_HARM
	stop_automated_movement = 1
	status_flags = CANPUSH
	attack_sound = 'sound/magic/demon_attack1.ogg'
	atmos_requirements = list("min_oxy" = 0, "max_oxy" = 0, "min_tox" = 0, "max_tox" = 0, "min_co2" = 0, "max_co2" = 0, "min_n2" = 0, "max_n2" = 0)
	minbodytemp = 250 //Weak to cold
	maxbodytemp = INFINITY
	faction = list("hell")
	attack_verb_continuous = "wildly tears into"
	attack_verb_simple = "wildly tear into"
	maxHealth = 200
	health = 200
	healable = 0
	environment_smash = ENVIRONMENT_SMASH_STRUCTURES
	obj_damage = 40
	melee_damage_lower = 10
	melee_damage_upper = 15
	see_in_dark = 8
	lighting_alpha = LIGHTING_PLANE_ALPHA_MOSTLY_INVISIBLE
	del_on_death = TRUE
	deathmessage = "screams in agony as it sublimates into a sulfurous smoke."
	deathsound = 'sound/magic/demon_dies.ogg'
	var/boost = 0
	bloodcrawl = BLOODCRAWL_EAT
	var/list/consumed_mobs = list()
	var/playstyle_string = "<span class='big bold'>You are an imp,</span><B> a mischievous creature from hell. You are the lowest rank on the hellish totem pole \
							Though you are not obligated to help, perhaps by aiding a higher ranking devil, you might just get a promotion. However, you are incapable	\
							of intentionally harming a fellow devil.</B>"

/mob/living/simple_animal/imp/Initialize()
	. = ..()
	set_varspeed(1)
	addtimer(CALLBACK(src, /mob/living/simple_animal/proc/set_varspeed, 0), 30)

/datum/antagonist/imp
	name = "Imp"
	antagpanel_category = "Devil"
	show_in_roundend = FALSE

/datum/antagonist/imp/on_gain()
	. = ..()
	give_objectives()

/datum/antagonist/imp/proc/give_objectives()
	var/datum/objective/newobjective = new
	newobjective.explanation_text = "Try to get a promotion to a higher devilic rank."
	newobjective.owner = owner
	objectives += newobjective
