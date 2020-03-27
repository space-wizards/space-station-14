/datum/action/changeling/biodegrade
	name = "Biodegrade"
	desc = "Dissolves restraints or other objects preventing free movement. Costs 30 chemicals."
	helptext = "This is obvious to nearby people, and can destroy standard restraints and closets."
	button_icon_state = "biodegrade"
	chemical_cost = 30 //High cost to prevent spam
	dna_cost = 2
	req_human = 1

/datum/action/changeling/biodegrade/sting_action(mob/living/carbon/human/user)
	var/used = FALSE // only one form of shackles removed per use
	if(!user.restrained() && !user.legcuffed && isopenturf(user.loc))
		to_chat(user, "<span class='warning'>We are already free!</span>")
		return FALSE

	if(user.handcuffed)
		var/obj/O = user.get_item_by_slot(ITEM_SLOT_HANDCUFFED)
		if(!istype(O))
			return FALSE
		user.visible_message("<span class='warning'>[user] vomits a glob of acid on [user.p_their()] [O]!</span>", \
			"<span class='warning'>We vomit acidic ooze onto our restraints!</span>")

		addtimer(CALLBACK(src, .proc/dissolve_handcuffs, user, O), 30)
		used = TRUE

	if(user.legcuffed)
		var/obj/O = user.get_item_by_slot(ITEM_SLOT_LEGCUFFED)
		if(!istype(O))
			return FALSE
		user.visible_message("<span class='warning'>[user] vomits a glob of acid on [user.p_their()] [O]!</span>", \
			"<span class='warning'>We vomit acidic ooze onto our restraints!</span>")

		addtimer(CALLBACK(src, .proc/dissolve_legcuffs, user, O), 30)
		used = TRUE

	if(user.wear_suit && user.wear_suit.breakouttime && !used)
		var/obj/item/clothing/suit/S = user.get_item_by_slot(ITEM_SLOT_OCLOTHING)
		if(!istype(S))
			return FALSE
		user.visible_message("<span class='warning'>[user] vomits a glob of acid across the front of [user.p_their()] [S]!</span>", \
			"<span class='warning'>We vomit acidic ooze onto our straight jacket!</span>")
		addtimer(CALLBACK(src, .proc/dissolve_straightjacket, user, S), 30)
		used = TRUE


	if(istype(user.loc, /obj/structure/closet) && !used)
		var/obj/structure/closet/C = user.loc
		if(!istype(C))
			return FALSE
		C.visible_message("<span class='warning'>[C]'s hinges suddenly begin to melt and run!</span>")
		to_chat(user, "<span class='warning'>We vomit acidic goop onto the interior of [C]!</span>")
		addtimer(CALLBACK(src, .proc/open_closet, user, C), 70)
		used = TRUE

	if(istype(user.loc, /obj/structure/spider/cocoon) && !used)
		var/obj/structure/spider/cocoon/C = user.loc
		if(!istype(C))
			return FALSE
		C.visible_message("<span class='warning'>[src] shifts and starts to fall apart!</span>")
		to_chat(user, "<span class='warning'>We secrete acidic enzymes from our skin and begin melting our cocoon...</span>")
		addtimer(CALLBACK(src, .proc/dissolve_cocoon, user, C), 25) //Very short because it's just webs
		used = TRUE
	..()
	return used

/datum/action/changeling/biodegrade/proc/dissolve_handcuffs(mob/living/carbon/human/user, obj/O)
	if(O && user.handcuffed == O)
		user.visible_message("<span class='warning'>[O] dissolve[O.gender==PLURAL?"":"s"] into a puddle of sizzling goop.</span>")
		new /obj/effect/decal/cleanable/greenglow(O.drop_location())
		qdel(O)

/datum/action/changeling/biodegrade/proc/dissolve_legcuffs(mob/living/carbon/human/user, obj/O)
	if(O && user.legcuffed == O)
		user.visible_message("<span class='warning'>[O] dissolve[O.gender==PLURAL?"":"s"] into a puddle of sizzling goop.</span>")
		new /obj/effect/decal/cleanable/greenglow(O.drop_location())
		qdel(O)

/datum/action/changeling/biodegrade/proc/dissolve_straightjacket(mob/living/carbon/human/user, obj/S)
	if(S && user.wear_suit == S)
		user.visible_message("<span class='warning'>[S] dissolves into a puddle of sizzling goop.</span>")
		new /obj/effect/decal/cleanable/greenglow(S.drop_location())
		qdel(S)

/datum/action/changeling/biodegrade/proc/open_closet(mob/living/carbon/human/user, obj/structure/closet/C)
	if(C && user.loc == C)
		C.visible_message("<span class='warning'>[C]'s door breaks and opens!</span>")
		new /obj/effect/decal/cleanable/greenglow(C.drop_location())
		C.welded = FALSE
		C.locked = FALSE
		C.broken = TRUE
		C.open()
		to_chat(user, "<span class='warning'>We open the container restraining us!</span>")

/datum/action/changeling/biodegrade/proc/dissolve_cocoon(mob/living/carbon/human/user, obj/structure/spider/cocoon/C)
	if(C && user.loc == C)
		new /obj/effect/decal/cleanable/greenglow(C.drop_location())
		qdel(C) //The cocoon's destroy will move the changeling outside of it without interference
		to_chat(user, "<span class='warning'>We dissolve the cocoon!</span>")
