GLOBAL_LIST_INIT(VVlocked, list("vars", "datum_flags", "client", "mob"))		//Requires DEBUG
GLOBAL_PROTECT(VVlocked)
GLOBAL_LIST_INIT(VVicon_edit_lock, list("icon", "icon_state", "overlays", "underlays"))		//Requires DEBUG or FUN
GLOBAL_PROTECT(VVicon_edit_lock)
GLOBAL_LIST_INIT(VVckey_edit, list("key", "ckey"))	//Requires DEBUG or SPAWN
GLOBAL_PROTECT(VVckey_edit)
GLOBAL_LIST_INIT(VVpixelmovement, list("bound_x", "bound_y", "step_x", "step_y", "step_size", "bound_height", "bound_width", "bounds"))		//No editing ever.
GLOBAL_PROTECT(VVpixelmovement)

/client/proc/vv_parse_text(O, new_var)
	if(O && findtext(new_var,"\["))
		var/process_vars = alert(usr,"\[] detected in string, process as variables?","Process Variables?","Yes","No")
		if(process_vars == "Yes")
			. = string2listofvars(new_var, O)

//do they want you to include subtypes?
//FALSE = no subtypes, strict exact type pathing (or the type doesn't have subtypes)
//TRUE = Yes subtypes
//NULL = User cancelled at the prompt or invalid type given
/client/proc/vv_subtype_prompt(var/type)
	if (!ispath(type))
		return
	var/list/subtypes = subtypesof(type)
	if (!subtypes || !subtypes.len)
		return FALSE
	if (subtypes && subtypes.len)
		switch(alert("Strict object type detection?", "Type detection", "Strictly this type","This type and subtypes", "Cancel"))
			if("Strictly this type")
				return FALSE
			if("This type and subtypes")
				return TRUE
			else
				return

/client/proc/vv_reference_list(type, subtypes)
	. = list()
	var/list/types = list(type)
	if (subtypes)
		types = typesof(type)

	var/list/fancytypes = make_types_fancy(types)

	for(var/fancytype in fancytypes) //swap the assoication
		types[fancytypes[fancytype]] = fancytype

	var/things = get_all_of_type(type, subtypes)

	var/i = 0
	for(var/thing in things)
		var/datum/D = thing
		i++
		//try one of 3 methods to shorten the type text:
		//	fancy type,
		//	fancy type with the base type removed from the begaining,
		//	the type with the base type removed from the begaining
		var/fancytype = types[D.type]
		if (findtext(fancytype, types[type]))
			fancytype = copytext(fancytype, length(types[type]) + 1)
		var/shorttype = copytext("[D.type]", length("[type]") + 1)
		if (length_char(shorttype) > length_char(fancytype))
			shorttype = fancytype
		if (!length(shorttype))
			shorttype = "/"

		.["[D]([shorttype])[REF(D)]#[i]"] = D

/client/proc/mod_list_add_ass(atom/O) //hehe

	var/list/L = vv_get_value(restricted_classes = list(VV_RESTORE_DEFAULT))
	var/class = L["class"]
	if (!class)
		return
	var/var_value = L["value"]

	if(class == VV_TEXT || class == VV_MESSAGE)
		var/list/varsvars = vv_parse_text(O, var_value)
		for(var/V in varsvars)
			var_value = replacetext(var_value,"\[[V]]","[O.vars[V]]")

	return var_value


/client/proc/mod_list_add(list/L, atom/O, original_name, objectvar)
	var/list/LL = vv_get_value(restricted_classes = list(VV_RESTORE_DEFAULT))
	var/class = LL["class"]
	if (!class)
		return
	var/var_value = LL["value"]

	if(class == VV_TEXT || class == VV_MESSAGE)
		var/list/varsvars = vv_parse_text(O, var_value)
		for(var/V in varsvars)
			var_value = replacetext(var_value,"\[[V]]","[O.vars[V]]")

	if (O)
		L = L.Copy()

	L += var_value

	switch(alert("Would you like to associate a value with the list entry?",,"Yes","No"))
		if("Yes")
			L[var_value] = mod_list_add_ass(O) //hehe
	if (O)
		if (O.vv_edit_var(objectvar, L) == FALSE)
			to_chat(src, "Your edit was rejected by the object.")
			return
	log_world("### ListVarEdit by [src]: [(O ? O.type : "/list")] [objectvar]: ADDED=[var_value]")
	log_admin("[key_name(src)] modified [original_name]'s [objectvar]: ADDED=[var_value]")
	message_admins("[key_name_admin(src)] modified [original_name]'s [objectvar]: ADDED=[var_value]")

/client/proc/mod_list(list/L, atom/O, original_name, objectvar, index, autodetect_class = FALSE)
	if(!check_rights(R_VAREDIT))
		return
	if(!istype(L, /list))
		to_chat(src, "Not a List.")
		return

	if(L.len > 1000)
		var/confirm = alert(src, "The list you're trying to edit is very long, continuing may crash the server.", "Warning", "Continue", "Abort")
		if(confirm != "Continue")
			return

	var/is_normal_list = IS_NORMAL_LIST(L)
	var/list/names = list()
	for (var/i in 1 to L.len)
		var/key = L[i]
		var/value
		if (is_normal_list && !isnum(key))
			value = L[key]
		if (value == null)
			value = "null"
		names["#[i] [key] = [value]"] = i
	if (!index)
		var/variable = input("Which var?","Var") as null|anything in names + "(ADD VAR)" + "(CLEAR NULLS)" + "(CLEAR DUPES)" + "(SHUFFLE)"

		if(variable == null)
			return

		if(variable == "(ADD VAR)")
			mod_list_add(L, O, original_name, objectvar)
			return

		if(variable == "(CLEAR NULLS)")
			L = L.Copy()
			listclearnulls(L)
			if (!O.vv_edit_var(objectvar, L))
				to_chat(src, "Your edit was rejected by the object.")
				return
			log_world("### ListVarEdit by [src]: [O.type] [objectvar]: CLEAR NULLS")
			log_admin("[key_name(src)] modified [original_name]'s [objectvar]: CLEAR NULLS")
			message_admins("[key_name_admin(src)] modified [original_name]'s list [objectvar]: CLEAR NULLS")
			return

		if(variable == "(CLEAR DUPES)")
			L = uniqueList(L)
			if (!O.vv_edit_var(objectvar, L))
				to_chat(src, "Your edit was rejected by the object.")
				return
			log_world("### ListVarEdit by [src]: [O.type] [objectvar]: CLEAR DUPES")
			log_admin("[key_name(src)] modified [original_name]'s [objectvar]: CLEAR DUPES")
			message_admins("[key_name_admin(src)] modified [original_name]'s list [objectvar]: CLEAR DUPES")
			return

		if(variable == "(SHUFFLE)")
			L = shuffle(L)
			if (!O.vv_edit_var(objectvar, L))
				to_chat(src, "Your edit was rejected by the object.")
				return
			log_world("### ListVarEdit by [src]: [O.type] [objectvar]: SHUFFLE")
			log_admin("[key_name(src)] modified [original_name]'s [objectvar]: SHUFFLE")
			message_admins("[key_name_admin(src)] modified [original_name]'s list [objectvar]: SHUFFLE")
			return

		index = names[variable]


	var/assoc_key
	if (index == null)
		return
	var/assoc = 0
	var/prompt = alert(src, "Do you want to edit the key or its assigned value?", "Associated List", "Key", "Assigned Value", "Cancel")
	if (prompt == "Cancel")
		return
	if (prompt == "Assigned Value")
		assoc = 1
		assoc_key = L[index]
	var/default
	var/variable
	var/old_assoc_value		//EXPERIMENTAL - Keep old associated value while modifying key, if any
	if(is_normal_list)
		if (assoc)
			variable = L[assoc_key]
		else
			variable = L[index]
			//EXPERIMENTAL - Keep old associated value while modifying key, if any
			if(IS_VALID_ASSOC_KEY(variable))
				var/found = L[variable]
				if(!isnull(found))
					old_assoc_value = found
			//

	default = vv_get_class(objectvar, variable)

	to_chat(src, "Variable appears to be <b>[uppertext(default)]</b>.")

	to_chat(src, "Variable contains: [variable]")

	if(default == VV_NUM)
		var/dir_text = ""
		var/tdir = variable
		if(tdir > 0 && tdir < 16)
			if(tdir & 1)
				dir_text += "NORTH"
			if(tdir & 2)
				dir_text += "SOUTH"
			if(tdir & 4)
				dir_text += "EAST"
			if(tdir & 8)
				dir_text += "WEST"

		if(dir_text)
			to_chat(usr, "If a direction, direction is: [dir_text]")

	var/original_var = variable

	if (O)
		L = L.Copy()
	var/class
	if(autodetect_class)
		if (default == VV_TEXT)
			default = VV_MESSAGE
		class = default
	var/list/LL = vv_get_value(default_class = default, current_value = original_var, restricted_classes = list(VV_RESTORE_DEFAULT), extra_classes = list(VV_LIST, "DELETE FROM LIST"))
	class = LL["class"]
	if (!class)
		return
	var/new_var = LL["value"]

	if(class == VV_MESSAGE)
		class = VV_TEXT

	switch(class) //Spits a runtime error if you try to modify an entry in the contents list. Dunno how to fix it, yet.
		if(VV_LIST)
			mod_list(variable, O, original_name, objectvar)

		if("DELETE FROM LIST")
			L.Cut(index, index+1)
			if (O)
				if (O.vv_edit_var(objectvar, L))
					to_chat(src, "Your edit was rejected by the object.")
					return
			log_world("### ListVarEdit by [src]: [O.type] [objectvar]: REMOVED=[html_encode("[original_var]")]")
			log_admin("[key_name(src)] modified [original_name]'s [objectvar]: REMOVED=[original_var]")
			message_admins("[key_name_admin(src)] modified [original_name]'s [objectvar]: REMOVED=[original_var]")
			return

		if(VV_TEXT)
			var/list/varsvars = vv_parse_text(O, new_var)
			for(var/V in varsvars)
				new_var = replacetext(new_var,"\[[V]]","[O.vars[V]]")


	if(is_normal_list)
		if(assoc)
			L[assoc_key] = new_var
		else
			L[index] = new_var
			if(!isnull(old_assoc_value) && IS_VALID_ASSOC_KEY(new_var))
				L[new_var] = old_assoc_value
	if (O)
		if (O.vv_edit_var(objectvar, L) == FALSE)
			to_chat(src, "Your edit was rejected by the object.")
			return
	log_world("### ListVarEdit by [src]: [(O ? O.type : "/list")] [objectvar]: [original_var]=[new_var]")
	log_admin("[key_name(src)] modified [original_name]'s [objectvar]: [original_var]=[new_var]")
	message_admins("[key_name_admin(src)] modified [original_name]'s varlist [objectvar]: [original_var]=[new_var]")

/proc/vv_varname_lockcheck(param_var_name)
	if(param_var_name in GLOB.VVlocked)
		if(!check_rights(R_DEBUG))
			return FALSE
	if(param_var_name in GLOB.VVckey_edit)
		if(!check_rights(R_SPAWN|R_DEBUG))
			return FALSE
	if(param_var_name in GLOB.VVicon_edit_lock)
		if(!check_rights(R_FUN|R_DEBUG))
			return FALSE
	return TRUE

/client/proc/modify_variables(atom/O, param_var_name = null, autodetect_class = 0)
	if(!check_rights(R_VAREDIT))
		return

	var/class
	var/variable
	var/var_value

	if(param_var_name)
		if(!(param_var_name in O.vars))
			to_chat(src, "A variable with this name ([param_var_name]) doesn't exist in this datum ([O])")
			return
		variable = param_var_name

	else
		var/list/names = list()
		for (var/V in O.vars)
			names += V

		names = sortList(names)

		variable = input("Which var?","Var") as null|anything in names
		if(!variable)
			return

	if(!O.can_vv_get(variable))
		return

	var_value = O.vars[variable]
	if(!vv_varname_lockcheck(variable))
		return

	var/default = vv_get_class(variable, var_value)

	if(isnull(default))
		to_chat(src, "Unable to determine variable type.")
	else
		to_chat(src, "Variable appears to be <b>[uppertext(default)]</b>.")

	to_chat(src, "Variable contains: [var_value]")

	if(default == VV_NUM)
		var/dir_text = ""
		if(var_value > 0 && var_value < 16)
			if(var_value & 1)
				dir_text += "NORTH"
			if(var_value & 2)
				dir_text += "SOUTH"
			if(var_value & 4)
				dir_text += "EAST"
			if(var_value & 8)
				dir_text += "WEST"

		if(dir_text)
			to_chat(src, "If a direction, direction is: [dir_text]")

	if(autodetect_class && default != VV_NULL)
		if (default == VV_TEXT)
			default = VV_MESSAGE
		class = default

	var/list/value = vv_get_value(class, default, var_value, extra_classes = list(VV_LIST), var_name = variable)
	class = value["class"]

	if (!class)
		return
	var/var_new = value["value"]

	if(class == VV_MESSAGE)
		class = VV_TEXT

	var/original_name = "[O]"

	switch(class)
		if(VV_LIST)
			if(!islist(var_value))
				mod_list(list(), O, original_name, variable)

			mod_list(var_value, O, original_name, variable)
			return

		if(VV_RESTORE_DEFAULT)
			var_new = initial(O.vars[variable])

		if(VV_TEXT)
			var/list/varsvars = vv_parse_text(O, var_new)
			for(var/V in varsvars)
				var_new = replacetext(var_new,"\[[V]]","[O.vars[V]]")


	if (O.vv_edit_var(variable, var_new) == FALSE)
		to_chat(src, "Your edit was rejected by the object.")
		return
	vv_update_display(O, "varedited", VV_MSG_EDITED)
	log_world("### VarEdit by [key_name(src)]: [O.type] [variable]=[var_value] => [var_new]")
	log_admin("[key_name(src)] modified [original_name]'s [variable] from [html_encode("[var_value]")] to [html_encode("[var_new]")]")
	var/msg = "[key_name_admin(src)] modified [original_name]'s [variable] from [var_value] to [var_new]"
	message_admins(msg)
	admin_ticket_log(O, msg)
	return TRUE
