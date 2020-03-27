/mob/living/simple_animal/hostile/retaliate/poison
    var/poison_per_bite = 0
    var/poison_type = /datum/reagent/toxin

/mob/living/simple_animal/hostile/retaliate/poison/AttackingTarget()
    . = ..()
    if(. && isliving(target))
        var/mob/living/L = target
        if(L.reagents && !poison_per_bite == 0)
            L.reagents.add_reagent(poison_type, poison_per_bite)
        return .

/mob/living/simple_animal/hostile/retaliate/poison/snake
        name = "snake"
        desc = "A slithery snake. These legless reptiles are the bane of mice and adventurers alike."
        icon_state = "snake"
        icon_living = "snake"
        icon_dead = "snake_dead"
        speak_emote = list("hisses")
        health = 20
        maxHealth = 20
        attack_verb_continuous = "bites"
        attack_verb_simple = "bite"
        melee_damage_lower = 5
        melee_damage_upper = 6
        response_help_continuous = "pets"
        response_help_simple = "pet"
        response_disarm_continuous = "shoos"
        response_disarm_simple = "shoo"
        response_harm_continuous = "steps on"
        response_harm_simple = "step on"
        faction = list("hostile")
        ventcrawler = VENTCRAWLER_ALWAYS
        density = FALSE
        pass_flags = PASSTABLE | PASSMOB
        mob_size = MOB_SIZE_SMALL
        mob_biotypes = MOB_ORGANIC|MOB_BEAST|MOB_REPTILE
        gold_core_spawnable = FRIENDLY_SPAWN
        obj_damage = 0
        environment_smash = ENVIRONMENT_SMASH_NONE


/mob/living/simple_animal/hostile/retaliate/poison/snake/ListTargets(atom/the_target)
	. = oview(vision_range, targets_from) //get list of things in vision range
	var/list/living_mobs = list()
	var/list/mice = list()
	for (var/HM in .)
		//Yum a tasty mouse
		if(istype(HM, /mob/living/simple_animal/mouse))
			mice += HM
		if(isliving(HM))
			living_mobs += HM

	// if no tasty mice to chase, lets chase any living mob enemies in our vision range
	if(length(mice) == 0)
		//Filter living mobs (in range mobs) by those we consider enemies (retaliate behaviour)
		return  living_mobs & enemies
	return mice

/mob/living/simple_animal/hostile/retaliate/poison/snake/AttackingTarget()
        if(istype(target, /mob/living/simple_animal/mouse))
                visible_message("<span class='notice'>[name] consumes [target] in a single gulp!</span>", "<span class='notice'>You consume [target] in a single gulp!</span>")
                QDEL_NULL(target)
                adjustBruteLoss(-2)
        else
                return ..()
