// A three-way junction that sorts objects based on check_sorting(H) proc
// This is a base type, use subtypes on the map.
/obj/structure/disposalpipe/sorting
	name = "sorting disposal pipe"
	desc = "An underfloor disposal pipe with a sorting mechanism."
	icon_state = "pipe-j1s"
	initialize_dirs = DISP_DIR_RIGHT | DISP_DIR_FLIP

/obj/structure/disposalpipe/sorting/nextdir(obj/structure/disposalholder/H)
	var/sortdir = dpdir & ~(dir | turn(dir, 180))
	if(H.dir != sortdir)		// probably came from the negdir
		if(check_sorting(H))	// if destination matches filtered type...
			return sortdir		// exit through sortdirection

	// go with the flow to positive direction
	return dir

// Sorting check, to be overridden in subtypes
/obj/structure/disposalpipe/sorting/proc/check_sorting(obj/structure/disposalholder/H)
	return FALSE



// Mail sorting junction, uses package tags to sort objects.
/obj/structure/disposalpipe/sorting/mail
	flip_type = /obj/structure/disposalpipe/sorting/mail/flip
	var/sortType = 0
	// sortType is to be set in map editor.
	// Supports both singular numbers and strings of numbers similar to access level strings.
	// Look at the list called TAGGERLOCATIONS in /_globalvars/lists/flavor_misc.dm
	var/list/sortTypes = list()

/obj/structure/disposalpipe/sorting/mail/flip
	flip_type = /obj/structure/disposalpipe/sorting/mail
	icon_state = "pipe-j2s"
	initialize_dirs = DISP_DIR_LEFT | DISP_DIR_FLIP

/obj/structure/disposalpipe/sorting/mail/Initialize()
	. = ..()
	// Generate a list of soring tags.
	if(sortType)
		if(isnum(sortType))
			sortTypes |= sortType
		else if(istext(sortType))
			var/list/sorts = splittext(sortType,";")
			for(var/x in sorts)
				var/n = text2num(x)
				if(n)
					sortTypes |= n

/obj/structure/disposalpipe/sorting/mail/examine(mob/user)
	. = ..()
	if(sortTypes.len)
		. += "It is tagged with the following tags:"
		for(var/t in sortTypes)
			. += "\t[GLOB.TAGGERLOCATIONS[t]]."
	else
		. += "It has no sorting tags set."

/obj/structure/disposalpipe/sorting/mail/attackby(obj/item/I, mob/user, params)
	if(istype(I, /obj/item/destTagger))
		var/obj/item/destTagger/O = I

		if(O.currTag)// Tagger has a tag set
			if(O.currTag in sortTypes)
				sortTypes -= O.currTag
				to_chat(user, "<span class='notice'>Removed \"[GLOB.TAGGERLOCATIONS[O.currTag]]\" filter.</span>")
			else
				sortTypes |= O.currTag
				to_chat(user, "<span class='notice'>Added \"[GLOB.TAGGERLOCATIONS[O.currTag]]\" filter.</span>")
			playsound(src, 'sound/machines/twobeep_high.ogg', 100, TRUE)
	else
		return ..()

/obj/structure/disposalpipe/sorting/mail/check_sorting(obj/structure/disposalholder/H)
	return (H.destinationTag in sortTypes)




// Wrap sorting junction, sorts objects destined for the mail office mail table (tomail = 1)
/obj/structure/disposalpipe/sorting/wrap
	desc = "An underfloor disposal pipe which sorts wrapped and unwrapped objects."
	flip_type = /obj/structure/disposalpipe/sorting/wrap/flip
	initialize_dirs = DISP_DIR_RIGHT | DISP_DIR_FLIP

/obj/structure/disposalpipe/sorting/wrap/check_sorting(obj/structure/disposalholder/H)
	return H.tomail

/obj/structure/disposalpipe/sorting/wrap/flip
	icon_state = "pipe-j2s"
	flip_type = /obj/structure/disposalpipe/sorting/wrap
	initialize_dirs = DISP_DIR_LEFT | DISP_DIR_FLIP
