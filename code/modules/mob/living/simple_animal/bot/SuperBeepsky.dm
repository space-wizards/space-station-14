/mob/living/simple_animal/bot/secbot/grievous //This bot is powerful. If you managed to get 4 eswords somehow, you deserve this horror. Emag him for best results.
	name = "General Beepsky"
	desc = "Is that a secbot with four eswords in its arms...?"
	icon = 'icons/mob/aibots.dmi'
	icon_state = "grievous"
	health = 150
	maxHealth = 150
	baton_type = /obj/item/melee/transforming/energy/sword/saber
	base_speed = 4 //he's a fast fucker
	var/block_chance = 50
	weapon_force = 30


/mob/living/simple_animal/bot/secbot/grievous/toy //A toy version of general beepsky!
	name = "Genewul Bweepskee"
	desc = "An adorable looking secbot with four toy swords taped to its arms"
	health = 50
	maxHealth = 50
	baton_type = /obj/item/toy/sword
	weapon_force = 0

/mob/living/simple_animal/bot/secbot/grievous/bullet_act(obj/projectile/P)
	visible_message("<span class='warning'>[src] deflects [P] with its energy swords!</span>")
	playsound(src, 'sound/weapons/blade1.ogg', 50, TRUE)
	return BULLET_ACT_BLOCK

/mob/living/simple_animal/bot/secbot/grievous/Crossed(atom/movable/AM)
	..()
	if(ismob(AM) && AM == target)
		visible_message("<span class='warning'>[src] flails his swords and cuts [AM]!</span>")
		playsound(src,'sound/effects/beepskyspinsabre.ogg',100,TRUE,-1)
		stun_attack(AM)

/mob/living/simple_animal/bot/secbot/grievous/Initialize()
	. = ..()
	weapon.attack_self(src)

/mob/living/simple_animal/bot/secbot/grievous/Destroy()
	QDEL_NULL(weapon)
	return ..()

/mob/living/simple_animal/bot/secbot/grievous/special_retaliate_after_attack(mob/user)
	if(mode != BOT_HUNT)
		return
	if(prob(block_chance))
		visible_message("<span class='warning'>[src] deflects [user]'s attack with his energy swords!</span>")
		playsound(src, 'sound/weapons/blade1.ogg', 50, TRUE, -1)
		return TRUE

/mob/living/simple_animal/bot/secbot/grievous/stun_attack(mob/living/carbon/C) //Criminals don't deserve to live
	weapon.attack(C, src)
	playsound(src, 'sound/weapons/blade1.ogg', 50, TRUE, -1)
	if(C.stat == DEAD)
		addtimer(CALLBACK(src, /atom/.proc/update_icon), 2)
		back_to_idle()


/mob/living/simple_animal/bot/secbot/grievous/handle_automated_action()
	if(!on)
		return
	switch(mode)
		if(BOT_IDLE)		// idle
			update_icon()
			walk_to(src,0)
			look_for_perp()	// see if any criminals are in range
			if(!mode && auto_patrol)	// still idle, and set to patrol
				mode = BOT_START_PATROL	// switch to patrol mode
		if(BOT_HUNT)		// hunting for perp
			update_icon()
			playsound(src,'sound/effects/beepskyspinsabre.ogg',100,TRUE,-1)
			// general beepsky doesn't give up so easily, jedi scum
			if(frustration >= 20)
				walk_to(src,0)
				back_to_idle()
				return
			if(target)		// make sure target exists
				if(Adjacent(target) && isturf(target.loc))	// if right next to perp
					target_lastloc = target.loc //stun_attack() can clear the target if they're dead, so this needs to be set first
					stun_attack(target)
					anchored = TRUE
					return
				else								// not next to perp
					var/turf/olddist = get_dist(src, target)
					walk_to(src, target,1,4)
					if((get_dist(src, target)) >= (olddist))
						frustration++
					else
						frustration = 0
			else
				back_to_idle()

		if(BOT_START_PATROL)
			look_for_perp()
			start_patrol()

		if(BOT_PATROL)
			look_for_perp()
			bot_patrol()

/mob/living/simple_animal/bot/secbot/grievous/look_for_perp()
	anchored = FALSE
	var/judgement_criteria = judgement_criteria()
	for (var/mob/living/carbon/C in view(7,src)) //Let's find us a criminal
		if((C.stat) || (C.handcuffed))
			continue

		if((C.name == oldtarget_name) && (world.time < last_found + 100))
			continue

		threatlevel = C.assess_threat(judgement_criteria, weaponcheck=CALLBACK(src, .proc/check_for_weapons))

		if(!threatlevel)
			continue

		else if(threatlevel >= 4)
			target = C
			oldtarget_name = C.name
			speak("Level [threatlevel] infraction alert!")
			playsound(src, pick('sound/voice/beepsky/criminal.ogg', 'sound/voice/beepsky/justice.ogg', 'sound/voice/beepsky/freeze.ogg'), 50, FALSE)
			playsound(src,'sound/weapons/saberon.ogg',50,TRUE,-1)
			visible_message("<span class='warning'>[src] ignites his energy swords!</span>")
			icon_state = "grievous-c"
			visible_message("<b>[src]</b> points at [C.name]!")
			mode = BOT_HUNT
			INVOKE_ASYNC(src, .proc/handle_automated_action)
			break
		else
			continue


/mob/living/simple_animal/bot/secbot/grievous/explode()

	walk_to(src,0)
	visible_message("<span class='boldannounce'>[src] lets out a huge cough as it blows apart!</span>")
	var/atom/Tsec = drop_location()

	var/obj/item/bot_assembly/secbot/Sa = new (Tsec)
	Sa.build_step = 1
	Sa.add_overlay("hs_hole")
	Sa.created_name = name
	new /obj/item/assembly/prox_sensor(Tsec)

	if(prob(50))
		drop_part(robot_arm, Tsec)

	do_sparks(3, TRUE, src)
	for(var/IS = 0 to 4)
		drop_part(baton_type, Tsec)
	new /obj/effect/decal/cleanable/oil(Tsec)
	qdel(src)
