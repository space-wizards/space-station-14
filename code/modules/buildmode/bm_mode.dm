/datum/buildmode_mode
	var/key = "oops"

	var/datum/buildmode/BM

	// would corner selection work better as a component?
	var/use_corner_selection = FALSE
	var/list/preview
	var/turf/cornerA
	var/turf/cornerB

/datum/buildmode_mode/New(datum/buildmode/BM)
	src.BM = BM
	preview = list()
	return ..()

/datum/buildmode_mode/Destroy()
	cornerA = null
	cornerB = null
	QDEL_LIST(preview)
	preview = null
	return ..()

/datum/buildmode_mode/proc/enter_mode(datum/buildmode/BM)
	return

/datum/buildmode_mode/proc/exit_mode(datum/buildmode/BM)
	return

/datum/buildmode_mode/proc/get_button_iconstate()
	return "buildmode_[key]"

/datum/buildmode_mode/proc/show_help(client/c)
	CRASH("No help defined, yell at a coder")

/datum/buildmode_mode/proc/change_settings(client/c)
	to_chat(c, "<span class='warning'>There is no configuration available for this mode</span>")
	return

/datum/buildmode_mode/proc/Reset()
	deselect_region()

/datum/buildmode_mode/proc/select_tile(turf/T, corner_to_select)
	var/overlaystate
	BM.holder.images -= preview
	switch(corner_to_select)
		if(AREASELECT_CORNERA)
			overlaystate = "greenOverlay"
		if(AREASELECT_CORNERB)
			overlaystate = "blueOverlay"

	var/image/I = image('icons/turf/overlays.dmi', T, overlaystate)
	I.plane = ABOVE_LIGHTING_PLANE
	preview += I
	BM.holder.images += preview
	return T

/datum/buildmode_mode/proc/highlight_region(region)
	BM.holder.images -= preview
	for(var/t in region)
		var/image/I = image('icons/turf/overlays.dmi', t, "redOverlay")
		I.plane = ABOVE_LIGHTING_PLANE
		preview += I
	BM.holder.images += preview

/datum/buildmode_mode/proc/deselect_region()
	BM.holder.images -= preview
	preview.Cut()
	cornerA = null
	cornerB = null

/datum/buildmode_mode/proc/handle_click(client/c, params, object)
	var/list/pa = params2list(params)
	var/left_click = pa.Find("left")
	if(use_corner_selection)
		if(left_click)
			if(!cornerA)
				cornerA = select_tile(get_turf(object), AREASELECT_CORNERA)
				return
			if(cornerA && !cornerB)
				cornerB = select_tile(get_turf(object), AREASELECT_CORNERB)
				to_chat(c, "<span class='boldwarning'>Region selected, if you're happy with your selection left click again, otherwise right click.</span>")
				return
			handle_selected_area(c, params)
			deselect_region()
		else
			to_chat(c, "<span class='notice'>Region selection canceled!</span>")
			deselect_region()
	return

/datum/buildmode_mode/proc/handle_selected_area(client/c, params)
