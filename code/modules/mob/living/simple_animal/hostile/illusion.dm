/mob/living/simple_animal/hostile/illusion
	name = "illusion"
	desc = "It's a fake!"
	icon = 'icons/effects/effects.dmi'
	icon_state = "static"
	icon_living = "static"
	icon_dead = "null"
	gender = NEUTER
	mob_biotypes = NONE
	melee_damage_lower = 5
	melee_damage_upper = 5
	a_intent = INTENT_HARM
	attack_verb_continuous = "gores"
	attack_verb_simple = "gore"
	maxHealth = 100
	health = 100
	speed = 0
	faction = list("illusion")
	var/life_span = INFINITY //how long until they despawn
	var/mob/living/parent_mob
	var/multiply_chance = 0 //if we multiply on hit
	del_on_death = 1
	deathmessage = "vanishes into thin air! It was a fake!"


/mob/living/simple_animal/hostile/illusion/Life()
	..()
	if(world.time > life_span)
		death()


/mob/living/simple_animal/hostile/illusion/proc/Copy_Parent(mob/living/original, life = 50, hp = 100, damage = 0, replicate = 0 )
	appearance = original.appearance
	parent_mob = original
	setDir(original.dir)
	life_span = world.time+life
	health = hp
	melee_damage_lower = damage
	melee_damage_upper = damage
	multiply_chance = replicate
	faction -= "neutral"
	transform = initial(transform)
	pixel_y = initial(pixel_y)
	pixel_x = initial(pixel_x)

/mob/living/simple_animal/hostile/illusion/examine(mob/user)
	if(parent_mob)
		return parent_mob.examine(user)
	else
		return ..()


/mob/living/simple_animal/hostile/illusion/AttackingTarget()
	. = ..()
	if(. && isliving(target) && prob(multiply_chance))
		var/mob/living/L = target
		if(L.stat == DEAD)
			return
		var/mob/living/simple_animal/hostile/illusion/M = new(loc)
		M.faction = faction.Copy()
		M.Copy_Parent(parent_mob, 80, health/2, melee_damage_upper, multiply_chance/2)
		M.GiveTarget(L)

///////Actual Types/////////

/mob/living/simple_animal/hostile/illusion/escape
	retreat_distance = 10
	minimum_distance = 10
	melee_damage_lower = 0
	melee_damage_upper = 0
	speed = -1
	obj_damage = 0
	environment_smash = ENVIRONMENT_SMASH_NONE


/mob/living/simple_animal/hostile/illusion/escape/AttackingTarget()
	return FALSE
