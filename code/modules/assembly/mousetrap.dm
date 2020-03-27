/obj/item/assembly/mousetrap
	name = "mousetrap"
	desc = "A handy little spring-loaded trap for catching pesty rodents."
	icon_state = "mousetrap"
	item_state = "mousetrap"
	custom_materials = list(/datum/material/iron=100)
	attachable = TRUE
	var/armed = FALSE
	drop_sound = 'sound/items/handling/component_drop.ogg'
	pickup_sound =  'sound/items/handling/component_pickup.ogg'


/obj/item/assembly/mousetrap/examine(mob/user)
	. = ..()
	. += "<span class='notice'>The pressure plate is [armed?"primed":"safe"].</span>"

/obj/item/assembly/mousetrap/activate()
	if(..())
		armed = !armed
		if(!armed)
			if(ishuman(usr))
				var/mob/living/carbon/human/user = usr
				if((HAS_TRAIT(user, TRAIT_DUMB) || HAS_TRAIT(user, TRAIT_CLUMSY)) && prob(50))
					to_chat(user, "<span class='warning'>Your hand slips, setting off the trigger!</span>")
					pulse(FALSE)
		update_icon()
		playsound(src, 'sound/weapons/handcuffs.ogg', 30, TRUE, -3)

/obj/item/assembly/mousetrap/update_icon()
	if(armed)
		icon_state = "mousetraparmed"
	else
		icon_state = "mousetrap"
	if(holder)
		holder.update_icon()

/obj/item/assembly/mousetrap/proc/triggered(mob/target, type = "feet")
	if(!armed)
		return
	var/obj/item/bodypart/affecting = null
	if(ishuman(target))
		var/mob/living/carbon/human/H = target
		if(HAS_TRAIT(H, TRAIT_PIERCEIMMUNE))
			playsound(src, 'sound/effects/snap.ogg', 50, TRUE)
			armed = FALSE
			update_icon()
			pulse(FALSE)
			return FALSE
		switch(type)
			if("feet")
				if(!H.shoes)
					affecting = H.get_bodypart(pick(BODY_ZONE_L_LEG, BODY_ZONE_R_LEG))
					H.Paralyze(60)
			if(BODY_ZONE_PRECISE_L_HAND, BODY_ZONE_PRECISE_R_HAND)
				if(!H.gloves)
					affecting = H.get_bodypart(type)
					H.Stun(60)
		if(affecting)
			if(affecting.receive_damage(1, 0))
				H.update_damage_overlays()
	else if(ismouse(target))
		var/mob/living/simple_animal/mouse/M = target
		visible_message("<span class='boldannounce'>SPLAT!</span>")
		M.splat()
	playsound(src, 'sound/effects/snap.ogg', 50, TRUE)
	armed = FALSE
	update_icon()
	pulse(FALSE)


/obj/item/assembly/mousetrap/attack_self(mob/living/carbon/human/user)
	if(!armed)
		to_chat(user, "<span class='notice'>You arm [src].</span>")
	else
		if((HAS_TRAIT(user, TRAIT_DUMB) || HAS_TRAIT(user, TRAIT_CLUMSY)) && prob(50))
			var/which_hand = BODY_ZONE_PRECISE_L_HAND
			if(!(user.active_hand_index % 2))
				which_hand = BODY_ZONE_PRECISE_R_HAND
			triggered(user, which_hand)
			user.visible_message("<span class='warning'>[user] accidentally sets off [src], breaking their fingers.</span>", \
								 "<span class='warning'>You accidentally trigger [src]!</span>")
			return
		to_chat(user, "<span class='notice'>You disarm [src].</span>")
	armed = !armed
	update_icon()
	playsound(src, 'sound/weapons/handcuffs.ogg', 30, TRUE, -3)


//ATTACK HAND IGNORING PARENT RETURN VALUE
/obj/item/assembly/mousetrap/attack_hand(mob/living/carbon/human/user)
	if(armed)
		if((HAS_TRAIT(user, TRAIT_DUMB) || HAS_TRAIT(user, TRAIT_CLUMSY)) && prob(50))
			var/which_hand = BODY_ZONE_PRECISE_L_HAND
			if(!(user.active_hand_index % 2))
				which_hand = BODY_ZONE_PRECISE_R_HAND
			triggered(user, which_hand)
			user.visible_message("<span class='warning'>[user] accidentally sets off [src], breaking their fingers.</span>", \
								 "<span class='warning'>You accidentally trigger [src]!</span>")
			return
	return ..()


/obj/item/assembly/mousetrap/Crossed(atom/movable/AM as mob|obj)
	if(armed)
		if(ismob(AM))
			var/mob/MM = AM
			if(!(MM.movement_type & FLYING))
				if(ishuman(AM))
					var/mob/living/carbon/H = AM
					if(H.m_intent == MOVE_INTENT_RUN)
						triggered(H)
						H.visible_message("<span class='warning'>[H] accidentally steps on [src].</span>", \
										  "<span class='warning'>You accidentally step on [src]</span>")
				else if(ismouse(MM))
					triggered(MM)
		else if(AM.density) // For mousetrap grenades, set off by anything heavy
			triggered(AM)
	..()


/obj/item/assembly/mousetrap/on_found(mob/finder)
	if(armed)
		if(finder)
			finder.visible_message("<span class='warning'>[finder] accidentally sets off [src], breaking their fingers.</span>", \
							   "<span class='warning'>You accidentally trigger [src]!</span>")
			triggered(finder, (finder.active_hand_index % 2 == 0) ? BODY_ZONE_PRECISE_R_HAND : BODY_ZONE_PRECISE_L_HAND)
			return TRUE	//end the search!
		else
			visible_message("<span class='warning'>[src] snaps shut!</span>")
			triggered(loc)
			return FALSE
	return FALSE


/obj/item/assembly/mousetrap/hitby(atom/movable/AM, skipcatch, hitpush, blocked, datum/thrownthing/throwingdatum)
	if(!armed)
		return ..()
	visible_message("<span class='warning'>[src] is triggered by [AM].</span>")
	triggered(null)


/obj/item/assembly/mousetrap/armed
	icon_state = "mousetraparmed"
	armed = TRUE
