//Standard
/mob/living/simple_animal/hostile/guardian/punch
	melee_damage_lower = 20
	melee_damage_upper = 20
	obj_damage = 80
	next_move_modifier = 0.8 //attacks 20% faster
	environment_smash = ENVIRONMENT_SMASH_WALLS
	playstyle_string = "<span class='holoparasite'>As a <b>standard</b> type you have no special abilities, but have a high damage resistance and a powerful attack capable of smashing through walls.</span>"
	magic_fluff_string = "<span class='holoparasite'>..And draw the Assistant, faceless and generic, but never to be underestimated.</span>"
	tech_fluff_string = "<span class='holoparasite'>Boot sequence complete. Standard combat modules loaded. Holoparasite swarm online.</span>"
	carp_fluff_string = "<span class='holoparasite'>CARP CARP CARP! You caught one! It's really boring and standard. Better punch some walls to ease the tension.</span>"
	var/battlecry = "AT"

/mob/living/simple_animal/hostile/guardian/punch/verb/Battlecry()
	set name = "Set Battlecry"
	set category = "Guardian"
	set desc = "Choose what you shout as you punch people."
	var/input = stripped_input(src,"What do you want your battlecry to be? Max length of 6 characters.", ,"", 7)
	if(input)
		battlecry = input



/mob/living/simple_animal/hostile/guardian/punch/AttackingTarget()
	. = ..()
	if(isliving(target))
		say("[battlecry][battlecry][battlecry][battlecry][battlecry][battlecry][battlecry][battlecry][battlecry][battlecry]!!", ignore_spam = TRUE)
		playsound(loc, src.attack_sound, 50, TRUE, TRUE)
		playsound(loc, src.attack_sound, 50, TRUE, TRUE)
		playsound(loc, src.attack_sound, 50, TRUE, TRUE)
		playsound(loc, src.attack_sound, 50, TRUE, TRUE)
