//Dextrous
/mob/living/simple_animal/hostile/guardian/dextrous
	melee_damage_lower = 10
	melee_damage_upper = 10
	damage_coeff = list(BRUTE = 0.75, BURN = 0.75, TOX = 0.75, CLONE = 0.75, STAMINA = 0, OXY = 0.75)
	playstyle_string = "<span class='holoparasite'>As a <b>dextrous</b> type you can hold items, store an item within yourself, and have medium damage resistance, but do low damage on attacks. Recalling and leashing will force you to drop unstored items!</span>"
	magic_fluff_string = "<span class='holoparasite'>..And draw the Drone, a dextrous master of construction and repair.</span>"
	tech_fluff_string = "<span class='holoparasite'>Boot sequence complete. Dextrous combat modules loaded. Holoparasite swarm online.</span>"
	carp_fluff_string = "<span class='holoparasite'>CARP CARP CARP! You caught one! It can hold stuff in its fins, sort of.</span>"
	dextrous = TRUE
	held_items = list(null, null)
	var/obj/item/internal_storage //what we're storing within ourself

/mob/living/simple_animal/hostile/guardian/dextrous/death(gibbed)
	..()
	if(internal_storage)
		dropItemToGround(internal_storage)

/mob/living/simple_animal/hostile/guardian/dextrous/examine(mob/user)
	if(dextrous)
		. = list("<span class='info'>*---------*\nThis is [icon2html(src)] \a <b>[src]</b>!\n[desc]")
		for(var/obj/item/I in held_items)
			if(!(I.item_flags & ABSTRACT))
				. += "It has [I.get_examine_string(user)] in its [get_held_index_name(get_held_index_of_item(I))]."
		if(internal_storage && !(internal_storage.item_flags & ABSTRACT))
			. += "It is holding [internal_storage.get_examine_string(user)] in its internal storage."
		. += "*---------*</span>"
	else
		return ..()

/mob/living/simple_animal/hostile/guardian/dextrous/Recall(forced)
	if(!summoner || loc == summoner || (cooldown > world.time && !forced))
		return FALSE
	drop_all_held_items()
	return ..() //lose items, then return

/mob/living/simple_animal/hostile/guardian/dextrous/snapback()
	if(summoner && !(get_dist(get_turf(summoner),get_turf(src)) <= range))
		drop_all_held_items()
		..() //lose items, then return

//SLOT HANDLING BULLSHIT FOR INTERNAL STORAGE
/mob/living/simple_animal/hostile/guardian/dextrous/doUnEquip(obj/item/I, force, newloc, no_move, invdrop = TRUE, silent = FALSE)
	if(..())
		update_inv_hands()
		if(I == internal_storage)
			internal_storage = null
			update_inv_internal_storage()
		return TRUE
	return FALSE

/mob/living/simple_animal/hostile/guardian/dextrous/can_equip(obj/item/I, slot, disable_warning = FALSE, bypass_equip_delay_self = FALSE)
	switch(slot)
		if(ITEM_SLOT_DEX_STORAGE)
			if(internal_storage)
				return FALSE
			return TRUE
	..()

/mob/living/simple_animal/hostile/guardian/dextrous/equip_to_slot(obj/item/I, slot)
	if(!..())
		return

	switch(slot)
		if(ITEM_SLOT_DEX_STORAGE)
			internal_storage = I
			update_inv_internal_storage()
		else
			to_chat(src, "<span class='danger'>You are trying to equip this item to an unsupported inventory slot. Report this to a coder!</span>")

/mob/living/simple_animal/hostile/guardian/dextrous/getBackSlot()
	return ITEM_SLOT_DEX_STORAGE

/mob/living/simple_animal/hostile/guardian/dextrous/getBeltSlot()
	return ITEM_SLOT_DEX_STORAGE

/mob/living/simple_animal/hostile/guardian/dextrous/proc/update_inv_internal_storage()
	if(internal_storage && client && hud_used && hud_used.hud_shown)
		internal_storage.screen_loc = ui_id
		client.screen += internal_storage

/mob/living/simple_animal/hostile/guardian/dextrous/regenerate_icons()
	..()
	update_inv_internal_storage()
