/datum/disease/gastrolosis
	name = "Invasive Gastrolosis"
	max_stages = 4
	spread_text = "Degenerative Virus"
	spread_flags = DISEASE_SPREAD_SPECIAL
	cure_text = "Salt and mutadone"
	agent = "Agent S and DNA restructuring"
	viable_mobtypes = list(/mob/living/carbon/human)
	stage_prob = 1
	disease_flags = CURABLE
	cures = list(/datum/reagent/consumable/sodiumchloride,  /datum/reagent/medicine/mutadone)

/datum/disease/gastrolosis/stage_act()
	..()
	if(is_species(affected_mob, /datum/species/snail))
		cure()
	switch(stage)
		if(2)
			if(prob(2))
				affected_mob.emote("gag")
			if(prob(1))
				var/turf/open/OT = get_turf(affected_mob)
				if(isopenturf(OT))
					OT.MakeSlippery(TURF_WET_LUBE, 40)
		if(3)
			if(prob(5))
				affected_mob.emote("gag")
			if(prob(5))
				var/turf/open/OT = get_turf(affected_mob)
				if(isopenturf(OT))
					OT.MakeSlippery(TURF_WET_LUBE, 100)
		if(4)
			var/obj/item/organ/eyes/eyes = locate(/obj/item/organ/eyes/snail) in affected_mob.internal_organs
			if(!eyes && prob(5))
				var/obj/item/organ/eyes/snail/new_eyes = new()
				new_eyes.Insert(affected_mob, drop_if_replaced = TRUE)
				affected_mob.visible_message("<span class='warning'>[affected_mob]'s eyes fall out, with snail eyes taking its place!</span>", \
				"<span class='userdanger'>You scream in pain as your eyes are pushed out by your new snail eyes!</span>")
				affected_mob.emote("scream")
				return
			var/obj/item/shell = affected_mob.get_item_by_slot(ITEM_SLOT_BACK)
			if(!istype(shell, /obj/item/storage/backpack/snail))
				shell = null
			if(!shell && prob(5))
				if(affected_mob.dropItemToGround(affected_mob.get_item_by_slot(ITEM_SLOT_BACK)))
					affected_mob.equip_to_slot_or_del(new /obj/item/storage/backpack/snail(affected_mob), ITEM_SLOT_BACK)
					affected_mob.visible_message("<span class='warning'>[affected_mob] grows a grotesque shell on their back!</span>", \
					"<span class='userdanger'>You scream in pain as a shell pushes itself out from under your skin!</span>")
					affected_mob.emote("scream")
					return
			var/obj/item/organ/tongue/tongue = locate(/obj/item/organ/tongue/snail) in affected_mob.internal_organs
			if(!tongue && prob(5))
				var/obj/item/organ/tongue/snail/new_tongue = new()
				new_tongue.Insert(affected_mob)
				to_chat(affected_mob, "<span class='userdanger'>You feel your speech slow down...</span>")
				return
			if(shell && eyes && tongue && prob(5))
				affected_mob.set_species(/datum/species/snail)
				affected_mob.visible_message("<span class='warning'>[affected_mob] turns into a snail!</span>", \
				"<span class='boldnotice'>You turned into a snail person! You feel an urge to cccrrraaawwwlll...</span>")
				cure()
			if(prob(10))
				affected_mob.emote("gag")
			if(prob(10))
				var/turf/open/OT = get_turf(affected_mob)
				if(isopenturf(OT))
					OT.MakeSlippery(TURF_WET_LUBE, 100)

/datum/disease/gastrolosis/cure()
	. = ..()
	if(!is_species(affected_mob, /datum/species/snail)) //undo all the snail fuckening
		var/mob/living/carbon/human/H = affected_mob
		var/obj/item/organ/tongue/tongue = locate(/obj/item/organ/tongue/snail) in H.internal_organs
		if(tongue)
			var/obj/item/organ/tongue/new_tongue = new H.dna.species.mutanttongue ()
			new_tongue.Insert(H)
		var/obj/item/organ/eyes/eyes = locate(/obj/item/organ/eyes/snail) in H.internal_organs
		if(eyes)
			var/obj/item/organ/eyes/new_eyes = new H.dna.species.mutanteyes ()
			new_eyes.Insert(H)
		var/obj/item/storage/backpack/bag = H.get_item_by_slot(ITEM_SLOT_BACK)
		if(istype(bag, /obj/item/storage/backpack/snail))
			bag.emptyStorage()
			H.doUnEquip(bag, TRUE, no_move = TRUE)
			qdel(bag)
