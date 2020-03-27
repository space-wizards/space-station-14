#define MAX_RANGE_FIND 32

/mob/living/carbon/monkey
	var/aggressive=0 // set to 1 using VV for an angry monkey
	var/frustration=0
	var/pickupTimer=0
	var/list/enemies = list()
	var/mob/living/target
	var/obj/item/pickupTarget
	var/mode = MONKEY_IDLE
	var/list/myPath = list()
	var/list/blacklistItems = list()
	var/maxStepsTick = 6
	var/best_force = 0
	var/martial_art = new/datum/martial_art
	var/resisting = FALSE
	var/pickpocketing = FALSE
	var/disposing_body = FALSE
	var/obj/machinery/disposal/bodyDisposal = null
	var/next_battle_screech = 0
	var/battle_screech_cooldown = 50

/mob/living/carbon/monkey/proc/IsStandingStill()
	return resisting || pickpocketing || disposing_body

// blocks
// taken from /mob/living/carbon/human/interactive/
/mob/living/carbon/monkey/proc/walk2derpless(target)
	if(!target || IsStandingStill())
		return 0

	if(myPath.len <= 0)
		myPath = get_path_to(src, get_turf(target), /turf/proc/Distance, MAX_RANGE_FIND + 1, 250,1)

	if(myPath)
		if(myPath.len > 0)
			for(var/i = 0; i < maxStepsTick; ++i)
				if(!IsDeadOrIncap())
					if(myPath.len >= 1)
						walk_to(src,myPath[1],0,5)
						myPath -= myPath[1]
			return 1

	// failed to path correctly so just try to head straight for a bit
	walk_to(src,get_turf(target),0,5)
	sleep(1)
	walk_to(src,0)

	return 0

// taken from /mob/living/carbon/human/interactive/
/mob/living/carbon/monkey/proc/IsDeadOrIncap(checkDead = TRUE)
	if(!(mobility_flags & MOBILITY_FLAGS_INTERACTION))
		return 1
	if(health <= 0 && checkDead)
		return 1
	if(IsUnconscious())
		return 1
	if(IsStun() || IsParalyzed())
		return 1
	if(stat)
		return 1
	return 0

/mob/living/carbon/monkey/proc/battle_screech()
	if(next_battle_screech < world.time)
		emote(pick("roar","screech"))
		for(var/mob/living/carbon/monkey/M in view(7,src))
			M.next_battle_screech = world.time + battle_screech_cooldown

/mob/living/carbon/monkey/proc/equip_item(obj/item/I)
	if(I.loc == src)
		return TRUE

	if(I.anchored)
		blacklistItems[I] ++
		return FALSE

	// WEAPONS
	if(istype(I, /obj/item))
		var/obj/item/W = I
		if(W.force >= best_force)
			put_in_hands(W)
			best_force = W.force
			return TRUE

	// CLOTHING
	else if(istype(I, /obj/item/clothing))
		var/obj/item/clothing/C = I
		monkeyDrop(C)
		addtimer(CALLBACK(src, .proc/pickup_and_wear, C), 5)
		return TRUE

	// EVERYTHING ELSE
	else
		if(!get_item_for_held_index(1) || !get_item_for_held_index(2))
			put_in_hands(I)
			return TRUE

	blacklistItems[I] ++
	return FALSE

/mob/living/carbon/monkey/proc/pickup_and_wear(var/obj/item/clothing/C)
	if(!equip_to_appropriate_slot(C))
		monkeyDrop(get_item_by_slot(C)) // remove the existing item if worn
		addtimer(CALLBACK(src, .proc/equip_to_appropriate_slot, C), 5)

/mob/living/carbon/monkey/resist_restraints()
	var/obj/item/I = null
	if(handcuffed)
		I = handcuffed
	else if(legcuffed)
		I = legcuffed
	if(I)
		changeNext_move(CLICK_CD_BREAKOUT)
		last_special = world.time + CLICK_CD_BREAKOUT
		cuff_resist(I)

/mob/living/carbon/monkey/proc/should_target(var/mob/living/L)
	if(HAS_TRAIT(src, TRAIT_PACIFISM))
		return FALSE

	if(enemies[L])
		return TRUE

	// target non-monkey mobs when aggressive, with a small probability of monkey v monkey
	if(aggressive && (!istype(L, /mob/living/carbon/monkey/) || prob(MONKEY_AGGRESSIVE_MVM_PROB)))
		return TRUE

	return FALSE

/mob/living/carbon/monkey/proc/handle_combat()
	if(pickupTarget)
		if(IsDeadOrIncap() || restrained() || blacklistItems[pickupTarget] || HAS_TRAIT(pickupTarget, TRAIT_NODROP))
			pickupTarget = null
		else
			pickupTimer++
			if(pickupTimer >= 4)
				blacklistItems[pickupTarget] ++
				pickupTarget = null
				pickupTimer = 0
			else
				INVOKE_ASYNC(src, .proc/walk2derpless, pickupTarget.loc)
				if(Adjacent(pickupTarget) || Adjacent(pickupTarget.loc)) // next to target
					drop_all_held_items() // who cares about these items, i want that one!
					if(isturf(pickupTarget.loc)) // on floor
						equip_item(pickupTarget)
						pickupTarget = null
						pickupTimer = 0
					else if(ismob(pickupTarget.loc)) // in someones hand
						var/mob/M = pickupTarget.loc
						if(!pickpocketing)
							pickpocketing = TRUE
							M.visible_message("<span class='warning'>[src] starts trying to take [pickupTarget] from [M]!</span>", "<span class='danger'>[src] tries to take [pickupTarget]!</span>")
							INVOKE_ASYNC(src, .proc/pickpocket, M)
			return TRUE

	switch(mode)
		if(MONKEY_IDLE)		// idle
			if(enemies.len)
				var/list/around = view(src, MONKEY_ENEMY_VISION) // scan for enemies
				for(var/mob/living/L in around)
					if( should_target(L) )
						if(L.stat == CONSCIOUS)
							battle_screech()
							retaliate(L)
							return TRUE
						else
							bodyDisposal = locate(/obj/machinery/disposal/) in around
							if(bodyDisposal)
								target = L
								mode = MONKEY_DISPOSE
								return TRUE

			// pickup any nearby objects
			if(!pickupTarget)
				var/obj/item/I = locate(/obj/item/) in oview(2,src)
				if(I && !blacklistItems[I])
					pickupTarget = I
				else
					var/mob/living/carbon/human/H = locate(/mob/living/carbon/human/) in oview(2,src)
					if(H)
						pickupTarget = pick(H.held_items)

		if(MONKEY_HUNT)		// hunting for attacker
			if(health < MONKEY_FLEE_HEALTH)
				mode = MONKEY_FLEE
				return TRUE

			if(target != null)
				INVOKE_ASYNC(src, .proc/walk2derpless, target)

			// pickup any nearby weapon
			if(!pickupTarget && prob(MONKEY_WEAPON_PROB))
				var/obj/item/W = locate(/obj/item/) in oview(2,src)
				if(!locate(/obj/item) in held_items)
					best_force = 0
				if(W && !blacklistItems[W] && W.force > best_force)
					pickupTarget = W

			// recruit other monkies
			var/list/around = view(src, MONKEY_ENEMY_VISION)
			for(var/mob/living/carbon/monkey/M in around)
				if(M.mode == MONKEY_IDLE && prob(MONKEY_RECRUIT_PROB))
					M.battle_screech()
					M.target = target
					M.mode = MONKEY_HUNT

			// switch targets
			for(var/mob/living/L in around)
				if(L != target && should_target(L) && L.stat == CONSCIOUS && prob(MONKEY_SWITCH_TARGET_PROB))
					target = L
					return TRUE

			// if can't reach target for long enough, go idle
			if(frustration >= MONKEY_HUNT_FRUSTRATION_LIMIT)
				back_to_idle()
				return TRUE

			if(target && target.stat == CONSCIOUS)		// make sure target exists
				if(Adjacent(target) && isturf(target.loc) && !IsDeadOrIncap())	// if right next to perp

					// check if target has a weapon
					var/obj/item/W
					for(var/obj/item/I in target.held_items)
						if(!(I.item_flags & ABSTRACT))
							W = I
							break

					// if the target has a weapon, chance to disarm them
					if(W && prob(MONKEY_ATTACK_DISARM_PROB))
						pickupTarget = W
						a_intent = INTENT_DISARM
						monkey_attack(target)

					else
						a_intent = INTENT_HARM
						monkey_attack(target)

					return TRUE

				else								// not next to perp
					var/turf/olddist = get_dist(src, target)
					if((get_dist(src, target)) >= (olddist))
						frustration++
					else
						frustration = 0
			else
				back_to_idle()

		if(MONKEY_FLEE)
			var/list/around = view(src, MONKEY_FLEE_VISION)
			target = null

			// flee from anyone who attacked us and we didn't beat down
			for(var/mob/living/L in around)
				if( enemies[L] && L.stat == CONSCIOUS )
					target = L

			if(target != null)
				walk_away(src, target, MONKEY_ENEMY_VISION, 5)
			else
				back_to_idle()

			return TRUE

		if(MONKEY_DISPOSE)

			// if can't dispose of body go back to idle
			if(!target || !bodyDisposal || frustration >= MONKEY_DISPOSE_FRUSTRATION_LIMIT)
				back_to_idle()
				return TRUE

			if(target.pulledby != src && !istype(target.pulledby, /mob/living/carbon/monkey/))

				INVOKE_ASYNC(src, .proc/walk2derpless, target.loc)

				if(Adjacent(target) && isturf(target.loc))
					a_intent = INTENT_GRAB
					target.grabbedby(src)
				else
					var/turf/olddist = get_dist(src, target)
					if((get_dist(src, target)) >= (olddist))
						frustration++
					else
						frustration = 0

			else if(!disposing_body)
				INVOKE_ASYNC(src, .proc/walk2derpless, bodyDisposal.loc)

				if(Adjacent(bodyDisposal))
					disposing_body = TRUE
					addtimer(CALLBACK(src, .proc/stuff_mob_in), 5)

				else
					var/turf/olddist = get_dist(src, bodyDisposal)
					if((get_dist(src, bodyDisposal)) >= (olddist))
						frustration++
					else
						frustration = 0

			return TRUE

	return IsStandingStill()

/mob/living/carbon/monkey/proc/pickpocket(var/mob/M)
	if(do_mob(src, M, MONKEY_ITEM_SNATCH_DELAY) && pickupTarget)
		for(var/obj/item/I in M.held_items)
			if(I == pickupTarget)
				M.visible_message("<span class='danger'>[src] snatches [pickupTarget] from [M].</span>", "<span class='userdanger'>[src] snatched [pickupTarget]!</span>")
				if(M.temporarilyRemoveItemFromInventory(pickupTarget))
					if(!QDELETED(pickupTarget) && !equip_item(pickupTarget))
						pickupTarget.forceMove(drop_location())
				else
					M.visible_message("<span class='danger'>[src] tried to snatch [pickupTarget] from [M], but failed!</span>", "<span class='userdanger'>[src] tried to grab [pickupTarget]!</span>")
	pickpocketing = FALSE
	pickupTarget = null
	pickupTimer = 0

/mob/living/carbon/monkey/proc/stuff_mob_in()
	if(bodyDisposal && target && Adjacent(bodyDisposal))
		bodyDisposal.stuff_mob_in(target, src)
	disposing_body = FALSE
	back_to_idle()

/mob/living/carbon/monkey/proc/back_to_idle()

	if(pulling)
		stop_pulling()

	mode = MONKEY_IDLE
	target = null
	a_intent = INTENT_HELP
	frustration = 0
	walk_to(src,0)

// attack using a held weapon otherwise bite the enemy, then if we are angry there is a chance we might calm down a little
/mob/living/carbon/monkey/proc/monkey_attack(mob/living/L)
	var/obj/item/Weapon = locate(/obj/item) in held_items

	// attack with weapon if we have one
	if(Weapon)
		L.attackby(Weapon, src)
	else
		L.attack_paw(src)

	// no de-aggro
	if(aggressive)
		return

	// if we arn't enemies, we were likely recruited to attack this target, jobs done if we calm down so go back to idle
	if(!enemies[L])
		if( target == L && prob(MONKEY_HATRED_REDUCTION_PROB) )
			back_to_idle()
		return // already de-aggroed

	if(prob(MONKEY_HATRED_REDUCTION_PROB))
		enemies[L] --

	// if we are not angry at our target, go back to idle
	if(enemies[L] <= 0)
		enemies.Remove(L)
		if( target == L )
			back_to_idle()

// get angry are a mob
/mob/living/carbon/monkey/proc/retaliate(mob/living/L)
	mode = MONKEY_HUNT
	target = L
	if(L != src)
		enemies[L] += MONKEY_HATRED_AMOUNT

	if(a_intent != INTENT_HARM)
		battle_screech()
		a_intent = INTENT_HARM

/mob/living/carbon/monkey/attack_hand(mob/living/L)
	if(L.a_intent == INTENT_HARM && prob(MONKEY_RETALIATE_HARM_PROB))
		retaliate(L)
	else if(L.a_intent == INTENT_DISARM && prob(MONKEY_RETALIATE_DISARM_PROB))
		retaliate(L)
	return ..()

/mob/living/carbon/monkey/attack_paw(mob/living/L)
	if(L.a_intent == INTENT_HARM && prob(MONKEY_RETALIATE_HARM_PROB))
		retaliate(L)
	else if(L.a_intent == INTENT_DISARM && prob(MONKEY_RETALIATE_DISARM_PROB))
		retaliate(L)
	return ..()

/mob/living/carbon/monkey/attackby(obj/item/W, mob/user, params)
	..()
	if((W.force) && (!target) && (W.damtype != STAMINA) )
		retaliate(user)

/mob/living/carbon/monkey/bullet_act(obj/projectile/Proj)
	if(istype(Proj , /obj/projectile/beam)||istype(Proj, /obj/projectile/bullet))
		if((Proj.damage_type == BURN) || (Proj.damage_type == BRUTE))
			if(!Proj.nodamage && Proj.damage < src.health && isliving(Proj.firer))
				retaliate(Proj.firer)
	. = ..()

/mob/living/carbon/monkey/hitby(atom/movable/AM, skipcatch = FALSE, hitpush = TRUE, blocked = FALSE, datum/thrownthing/throwingdatum)
	if(istype(AM, /obj/item))
		var/obj/item/I = AM
		if(I.throwforce < src.health && I.thrownby && ishuman(I.thrownby))
			var/mob/living/carbon/human/H = I.thrownby
			retaliate(H)
	..()

/mob/living/carbon/monkey/Crossed(atom/movable/AM)
	if(!IsDeadOrIncap() && ismob(AM) && target)
		var/mob/living/carbon/monkey/M = AM
		if(!istype(M) || !M)
			return
		knockOver(M)
		return
	..()

/mob/living/carbon/monkey/proc/monkeyDrop(var/obj/item/A)
	if(A)
		dropItemToGround(A, TRUE)
		update_icons()

/mob/living/carbon/monkey/grabbedby(mob/living/carbon/user)
	. = ..()
	if(!IsDeadOrIncap() && pulledby && (mode != MONKEY_IDLE || prob(MONKEY_PULL_AGGRO_PROB))) // nuh uh you don't pull me!
		if(Adjacent(pulledby))
			a_intent = INTENT_DISARM
			monkey_attack(pulledby)
			retaliate(pulledby)
			return TRUE

#undef MAX_RANGE_FIND
