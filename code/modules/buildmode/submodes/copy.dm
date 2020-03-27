/datum/buildmode_mode/copy
	key = "copy"
	var/atom/movable/stored = null

/datum/buildmode_mode/copy/Destroy()
	stored = null
	return ..()

/datum/buildmode_mode/copy/show_help(client/c)
	to_chat(c, "<span class='notice'>***********************************************************</span>")
	to_chat(c, "<span class='notice'>Left Mouse Button on obj/turf/mob   = Spawn a Copy of selected target</span>")
	to_chat(c, "<span class='notice'>Right Mouse Button on obj/mob = Select target to copy</span>")
	to_chat(c, "<span class='notice'>***********************************************************</span>")

/datum/buildmode_mode/copy/handle_click(client/c, params, obj/object)
	var/list/pa = params2list(params)
	var/left_click = pa.Find("left")
	var/right_click = pa.Find("right")

	if(left_click)
		var/turf/T = get_turf(object)
		if(stored)
			DuplicateObject(stored, perfectcopy=1, sameloc=0,newloc=T)
			log_admin("Build Mode: [key_name(c)] copied [stored] to [AREACOORD(object)]")
	else if(right_click)
		if(ismovableatom(object)) // No copying turfs for now.
			to_chat(c, "<span class='notice'>[object] set as template.</span>")
			stored = object
