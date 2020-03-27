/datum/buildmode_mode/varedit
	key = "edit"
	// Varedit mode
	var/varholder = null
	var/valueholder = null

/datum/buildmode_mode/varedit/Destroy()
	varholder = null
	valueholder = null
	return ..()

/datum/buildmode_mode/varedit/show_help(client/c)
	to_chat(c, "<span class='notice'>***********************************************************</span>")
	to_chat(c, "<span class='notice'>Right Mouse Button on buildmode button = Select var(type) & value</span>")
	to_chat(c, "<span class='notice'>Left Mouse Button on turf/obj/mob      = Set var(type) & value</span>")
	to_chat(c, "<span class='notice'>Right Mouse Button on turf/obj/mob     = Reset var's value</span>")
	to_chat(c, "<span class='notice'>***********************************************************</span>")

/datum/buildmode_mode/varedit/Reset()
	. = ..()
	varholder = null
	valueholder = null

/datum/buildmode_mode/varedit/change_settings(client/c)
	varholder = input(c, "Enter variable name:" ,"Name", "name")
	
	if(!vv_varname_lockcheck(varholder))
		return

	var/temp_value = c.vv_get_value()
	if(isnull(temp_value["class"]))
		Reset()
		to_chat(c, "<span class='notice'>Variable unset.</span>")
		return
	valueholder = temp_value["value"]

/datum/buildmode_mode/varedit/handle_click(client/c, params, obj/object)
	var/list/pa = params2list(params)
	var/left_click = pa.Find("left")
	var/right_click = pa.Find("right")

	if(isnull(varholder))
		to_chat(c, "<span class='warning'>Choose a variable to modify first.</span>")
		return
	if(left_click)
		if(object.vars.Find(varholder))
			if(object.vv_edit_var(varholder, valueholder) == FALSE)
				to_chat(c, "<span class='warning'>Your edit was rejected by the object.</span>")
				return
			log_admin("Build Mode: [key_name(c)] modified [object.name]'s [varholder] to [valueholder]")
		else
			to_chat(c, "<span class='warning'>[initial(object.name)] does not have a var called '[varholder]'</span>")
	if(right_click)
		if(object.vars.Find(varholder))
			var/reset_value = initial(object.vars[varholder])
			if(object.vv_edit_var(varholder, reset_value) == FALSE)
				to_chat(c, "<span class='warning'>Your edit was rejected by the object.</span>")
				return
			log_admin("Build Mode: [key_name(c)] modified [object.name]'s [varholder] to [reset_value]")
		else
			to_chat(c, "<span class='warning'>[initial(object.name)] does not have a var called '[varholder]'</span>")

