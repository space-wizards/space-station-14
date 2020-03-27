/obj/machinery/nuclearbomb/syndicate/bananium
	name = "bananium fission explosive"
	desc = "You probably shouldn't stick around to see if this is armed."
	icon = 'icons/obj/machines/nuke.dmi'
	icon_state = "bananiumbomb_base"

/obj/machinery/nuclearbomb/syndicate/bananium/update_icon_state()
	if(deconstruction_state != NUKESTATE_INTACT)
		icon_state = "bananiumbomb_base"
		return
	
	switch(get_nuke_state())
		if(NUKE_OFF_LOCKED, NUKE_OFF_UNLOCKED)
			icon_state = "bananiumbomb_base"
		if(NUKE_ON_TIMING)
			icon_state = "bananiumbomb_timing"
		if(NUKE_ON_EXPLODING)
			icon_state = "bananiumbomb_exploding"

/obj/machinery/nuclearbomb/syndicate/bananium/get_cinematic_type(off_station)
	switch(off_station)
		if(0)
			return CINEMATIC_NUKE_CLOWNOP
		if(1)
			return CINEMATIC_NUKE_MISS
		if(2)
			return CINEMATIC_NUKE_FAKE //it is farther away, so just a bikehorn instead of an airhorn
	return CINEMATIC_NUKE_FAKE

/obj/machinery/nuclearbomb/syndicate/bananium/really_actually_explode(off_station)
	Cinematic(get_cinematic_type(off_station), world)
	for(var/i in GLOB.human_list)
		var/mob/living/carbon/human/H = i
		var/turf/T = get_turf(H)
		if(!T || T.z != z)
			continue
		H.Stun(10)
		var/obj/item/clothing/C
		if(!H.w_uniform || H.dropItemToGround(H.w_uniform))
			C = new /obj/item/clothing/under/rank/civilian/clown(H)
			ADD_TRAIT(C, TRAIT_NODROP, CLOWN_NUKE_TRAIT)
			H.equip_to_slot_or_del(C, ITEM_SLOT_ICLOTHING)

		if(!H.shoes || H.dropItemToGround(H.shoes))
			C = new /obj/item/clothing/shoes/clown_shoes(H)
			ADD_TRAIT(C, TRAIT_NODROP, CLOWN_NUKE_TRAIT)
			H.equip_to_slot_or_del(C, ITEM_SLOT_FEET)

		if(!H.wear_mask || H.dropItemToGround(H.wear_mask))
			C = new /obj/item/clothing/mask/gas/clown_hat(H)
			ADD_TRAIT(C, TRAIT_NODROP, CLOWN_NUKE_TRAIT)
			H.equip_to_slot_or_del(C, ITEM_SLOT_MASK)

		H.dna.add_mutation(CLOWNMUT)
		H.gain_trauma(/datum/brain_trauma/mild/phobia/clowns, TRAUMA_RESILIENCE_LOBOTOMY) //MWA HA HA
