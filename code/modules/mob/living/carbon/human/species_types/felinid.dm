//Subtype of human
/datum/species/human/felinid
	name = "Felinid"
	id = "felinid"
	limbs_id = "human"

	mutant_bodyparts = list("ears", "tail_human")
	default_features = list("mcolor" = "FFF", "tail_human" = "Cat", "ears" = "Cat", "wings" = "None")

	mutantears = /obj/item/organ/ears/cat
	mutanttail = /obj/item/organ/tail/cat
	changesource_flags = MIRROR_BADMIN | WABBAJACK | MIRROR_PRIDE | MIRROR_MAGIC | RACE_SWAP | ERT_SPAWN | SLIME_EXTRACT
	var/original_felinid = TRUE //set to false for felinids created by mass-purrbation

/datum/species/human/felinid/qualifies_for_rank(rank, list/features)
	return TRUE

//Curiosity killed the cat's wagging tail.
/datum/species/human/felinid/spec_death(gibbed, mob/living/carbon/human/H)
	if(H)
		stop_wagging_tail(H)

/datum/species/human/felinid/spec_stun(mob/living/carbon/human/H,amount)
	if(H)
		stop_wagging_tail(H)
	. = ..()

/datum/species/human/felinid/can_wag_tail(mob/living/carbon/human/H)
	return ("tail_human" in mutant_bodyparts) || ("waggingtail_human" in mutant_bodyparts)

/datum/species/human/felinid/is_wagging_tail(mob/living/carbon/human/H)
	return ("waggingtail_human" in mutant_bodyparts)

/datum/species/human/felinid/start_wagging_tail(mob/living/carbon/human/H)
	if("tail_human" in mutant_bodyparts)
		mutant_bodyparts -= "tail_human"
		mutant_bodyparts |= "waggingtail_human"
	H.update_body()

/datum/species/human/felinid/stop_wagging_tail(mob/living/carbon/human/H)
	if("waggingtail_human" in mutant_bodyparts)
		mutant_bodyparts -= "waggingtail_human"
		mutant_bodyparts |= "tail_human"
	H.update_body()

/datum/species/human/felinid/on_species_gain(mob/living/carbon/C, datum/species/old_species, pref_load)
	if(ishuman(C))
		var/mob/living/carbon/human/H = C
		if(!pref_load)			//Hah! They got forcefully purrbation'd. Force default felinid parts on them if they have no mutant parts in those areas!
			if(H.dna.features["tail_human"] == "None")
				H.dna.features["tail_human"] = "Cat"
			if(H.dna.features["ears"] == "None")
				H.dna.features["ears"] = "Cat"
		if(H.dna.features["ears"] == "Cat")
			var/obj/item/organ/ears/cat/ears = new
			ears.Insert(H, drop_if_replaced = FALSE)
		else
			mutantears = /obj/item/organ/ears
		if(H.dna.features["tail_human"] == "Cat")
			var/obj/item/organ/tail/cat/tail = new
			tail.Insert(H, drop_if_replaced = FALSE)
		else
			mutanttail = null
	return ..()

/datum/species/human/felinid/on_species_loss(mob/living/carbon/H, datum/species/new_species, pref_load)
	var/obj/item/organ/ears/cat/ears = H.getorgan(/obj/item/organ/ears/cat)
	var/obj/item/organ/tail/cat/tail = H.getorgan(/obj/item/organ/tail/cat)

	if(ears)
		var/obj/item/organ/ears/NE
		if(new_species && new_species.mutantears)
			// Roundstart cat ears override new_species.mutantears, reset it here.
			new_species.mutantears = initial(new_species.mutantears)
			if(new_species.mutantears)
				NE = new new_species.mutantears
		if(!NE)
			// Go with default ears
			NE = new /obj/item/organ/ears
		NE.Insert(H, drop_if_replaced = FALSE)

	if(tail)
		var/obj/item/organ/tail/NT
		if(new_species && new_species.mutanttail)
			// Roundstart cat tail overrides new_species.mutanttail, reset it here.
			new_species.mutanttail = initial(new_species.mutanttail)
			if(new_species.mutanttail)
				NT = new new_species.mutanttail
		if(NT)
			NT.Insert(H, drop_if_replaced = FALSE)
		else
			tail.Remove(H)

/proc/mass_purrbation()
	for(var/M in GLOB.mob_list)
		if(ishuman(M))
			purrbation_apply(M)
		CHECK_TICK

/proc/mass_remove_purrbation()
	for(var/M in GLOB.mob_list)
		if(ishuman(M))
			purrbation_remove(M)
		CHECK_TICK

/proc/purrbation_toggle(mob/living/carbon/human/H, silent = FALSE)
	if(!ishumanbasic(H))
		return
	if(!isfelinid(H))
		purrbation_apply(H, silent)
		. = TRUE
	else
		purrbation_remove(H, silent)
		. = FALSE

/proc/purrbation_apply(mob/living/carbon/human/H, silent = FALSE)
	if(!ishuman(H) || isfelinid(H))
		return
	if(ishumanbasic(H))
		H.set_species(/datum/species/human/felinid)
		var/datum/species/human/felinid/cat_species = H.dna.species
		cat_species.original_felinid = FALSE
	else
		var/obj/item/organ/ears/cat/kitty_ears = new
		var/obj/item/organ/tail/cat/kitty_tail = new
		kitty_ears.Insert(H, TRUE, FALSE) //Gives nonhumans cat tail and ears
		kitty_tail.Insert(H, TRUE, FALSE)
	if(!silent)
		to_chat(H, "<span class='boldnotice'>Something is nya~t right.</span>")
		playsound(get_turf(H), 'sound/effects/meow1.ogg', 50, TRUE, -1)

/proc/purrbation_remove(mob/living/carbon/human/H, silent = FALSE)
	if(isfelinid(H))
		var/datum/species/human/felinid/cat_species = H.dna.species
		if(!cat_species.original_felinid)
			H.set_species(/datum/species/human)
	else if(ishuman(H) && !ishumanbasic(H))
		var/datum/species/target_species = H.dna.species
		var/organs = H.internal_organs
		for(var/obj/item/organ/current_organ in organs)
			if(istype(current_organ, /obj/item/organ/tail/cat))
				current_organ.Remove(H, TRUE)
				if(target_species.mutanttail)
					var/obj/item/organ/new_tail = new target_species.mutanttail
					new_tail.Insert(H, TRUE, FALSE)
			if(istype(current_organ, /obj/item/organ/ears/cat))
				var/obj/item/organ/new_ears = new target_species.mutantears
				new_ears.Insert(H, TRUE, FALSE)
	if(!silent)
		to_chat(H, "<span class='boldnotice'>You are no longer a cat.</span>")
