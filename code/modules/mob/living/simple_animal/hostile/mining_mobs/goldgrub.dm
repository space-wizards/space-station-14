//An ore-devouring but easily scared creature
/mob/living/simple_animal/hostile/asteroid/goldgrub
	name = "goldgrub"
	desc = "A worm that grows fat from eating everything in its sight. Seems to enjoy precious metals and other shiny things, hence the name."
	icon = 'icons/mob/lavaland/lavaland_monsters.dmi'
	icon_state = "Goldgrub"
	icon_living = "Goldgrub"
	icon_aggro = "Goldgrub_alert"
	icon_dead = "Goldgrub_dead"
	icon_gib = "syndicate_gib"
	mob_biotypes = MOB_ORGANIC|MOB_BEAST
	vision_range = 2
	aggro_vision_range = 9
	move_to_delay = 5
	friendly_verb_continuous = "harmlessly rolls into"
	friendly_verb_simple = "harmlessly roll into"
	maxHealth = 45
	health = 45
	harm_intent_damage = 5
	melee_damage_lower = 0
	melee_damage_upper = 0
	attack_verb_continuous = "barrels into"
	attack_verb_simple = "barrel into"
	attack_sound = 'sound/weapons/punch1.ogg'
	a_intent = INTENT_HELP
	speak_emote = list("screeches")
	throw_message = "sinks in slowly, before being pushed out of "
	deathmessage = "stops moving as green liquid oozes from the carcass!"
	status_flags = CANPUSH
	gold_core_spawnable = HOSTILE_SPAWN
	search_objects = 1
	wanted_objects = list(/obj/item/stack/ore/diamond, /obj/item/stack/ore/gold, /obj/item/stack/ore/silver,
						  /obj/item/stack/ore/uranium)

	var/chase_time = 100
	var/will_burrow = TRUE
	var/datum/action/innate/goldgrub/spitore/spit
	var/datum/action/innate/goldgrub/burrow/burrow
	var/is_burrowed = FALSE

/mob/living/simple_animal/hostile/asteroid/goldgrub/Initialize()
	. = ..()
	var/i = rand(1,3)
	while(i)
		loot += pick(/obj/item/stack/ore/silver, /obj/item/stack/ore/gold, /obj/item/stack/ore/uranium, /obj/item/stack/ore/diamond)
		i--
	spit = new
	burrow = new
	spit.Grant(src)
	burrow.Grant(src)
	
/datum/action/innate/goldgrub
	background_icon_state = "bg_default"
	
/datum/action/innate/goldgrub/spitore
	name = "Spit Ore"
	desc = "Vomit out all of your consumed ores."
	
/datum/action/innate/goldgrub/spitore/Activate()
	var/mob/living/simple_animal/hostile/asteroid/goldgrub/G = owner
	if(G.stat == DEAD || G.is_burrowed)
		return
	G.barf_contents()
	
/datum/action/innate/goldgrub/burrow
	name = "Burrow"
	desc = "Burrow under soft ground, evading predators and increasing your speed."
	
/obj/effect/dummy/phased_mob/goldgrub
	name = "water"
	icon = 'icons/effects/effects.dmi'
	icon_state = "nothing"
	density = FALSE
	anchored = TRUE
	invisibility = 60
	resistance_flags = LAVA_PROOF | FIRE_PROOF | UNACIDABLE | ACID_PROOF
	var/canmove = TRUE
	
/obj/effect/dummy/phased_mob/goldgrub/relaymove(mob/user, direction)
	forceMove(get_step(src,direction))

/obj/effect/dummy/phased_mob/goldgrub/ex_act()
	return

/obj/effect/dummy/phased_mob/goldgrub/bullet_act()
	return BULLET_ACT_FORCE_PIERCE

/obj/effect/dummy/phased_mob/goldgrub/singularity_act()
	return
	
/datum/action/innate/goldgrub/burrow/Activate()
	var/mob/living/simple_animal/hostile/asteroid/goldgrub/G = owner
	var/obj/effect/dummy/phased_mob/goldgrub/holder = null
	if(G.stat == DEAD)
		return
	var/turf/T = get_turf(G)
	if (!istype(T, /turf/open/floor/plating/asteroid) || !do_after(G, 30, target = T))
		to_chat(G, "<span class='warning'>You can only burrow in and out of mining turfs and must stay still!</span>")
		return
	if (get_dist(G, T) != 0)
		to_chat(G, "<span class='warning'>Action cancelled, as you moved while reappearing.</span>")
		return
	if(G.is_burrowed)
		holder = G.loc
		G.forceMove(T)
		QDEL_NULL(holder)
		G.is_burrowed = FALSE
		G.visible_message("<span class='danger'>[G] emerges from the ground!</span>")
		playsound(get_turf(G), 'sound/effects/break_stone.ogg', 50, TRUE, -1)
	else
		G.visible_message("<span class='danger'>[G] buries into the ground, vanishing from sight!</span>")
		playsound(get_turf(G), 'sound/effects/break_stone.ogg', 50, TRUE, -1)
		holder = new /obj/effect/dummy/phased_mob/goldgrub(T)
		G.forceMove(holder)
		G.is_burrowed = TRUE
	
/mob/living/simple_animal/hostile/asteroid/goldgrub/GiveTarget(new_target)
	target = new_target
	if(target != null)
		if(istype(target, /obj/item/stack/ore))
			visible_message("<span class='notice'>The [name] looks at [target.name] with hungry eyes.</span>")
		else if(isliving(target))
			Aggro()
			visible_message("<span class='danger'>The [name] tries to flee from [target.name]!</span>")
			retreat_distance = 10
			minimum_distance = 10
			if(will_burrow)
				addtimer(CALLBACK(src, .proc/Burrow), chase_time)

/mob/living/simple_animal/hostile/asteroid/goldgrub/AttackingTarget()
	if(istype(target, /obj/item/stack/ore))
		EatOre(target)
		return
	return ..()
	
/mob/living/simple_animal/hostile/asteroid/goldgrub/proc/EatOre(atom/movable/targeted_ore)
	if(targeted_ore && targeted_ore.loc != src)
		targeted_ore.forceMove(src)
		return TRUE
	return FALSE

/mob/living/simple_animal/hostile/asteroid/goldgrub/death(gibbed)
	barf_contents()
	return ..()
	
/mob/living/simple_animal/hostile/asteroid/goldgrub/proc/barf_contents()
	visible_message("<span class='danger'>[src] spits out its consumed ores!</span>")
	playsound(src, 'sound/effects/splat.ogg', 50, TRUE)
	for(var/atom/movable/AM in src)
		AM.forceMove(loc)
	
/mob/living/simple_animal/hostile/asteroid/goldgrub/proc/Burrow()//Begin the chase to kill the goldgrub in time
	if(!stat)
		visible_message("<span class='danger'>The [name] buries into the ground, vanishing from sight!</span>")
		qdel(src)

/mob/living/simple_animal/hostile/asteroid/goldgrub/bullet_act(obj/projectile/P)
	visible_message("<span class='danger'>The [P.name] was repelled by [name]'s girth!</span>")
	return BULLET_ACT_BLOCK

/mob/living/simple_animal/hostile/asteroid/goldgrub/adjustHealth(amount, updating_health = TRUE, forced = FALSE)
	vision_range = 9
	. = ..()
