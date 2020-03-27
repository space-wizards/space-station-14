/client/proc/vv_get_class(var_name, var_value)
	if(isnull(var_value))
		. = VV_NULL

	else if(isnum(var_value))
		if(var_name in GLOB.bitfields)
			. = VV_BITFIELD
		else
			. = VV_NUM

	else if(istext(var_value))
		if(findtext(var_value, "\n"))
			. = VV_MESSAGE
		else
			. = VV_TEXT

	else if(isicon(var_value))
		. = VV_ICON

	else if(ismob(var_value))
		. = VV_MOB_REFERENCE

	else if(isloc(var_value))
		. = VV_ATOM_REFERENCE

	else if(istype(var_value, /client))
		. = VV_CLIENT

	else if(istype(var_value, /datum))
		. = VV_DATUM_REFERENCE

	else if(ispath(var_value))
		if(ispath(var_value, /atom))
			. = VV_ATOM_TYPE
		else if(ispath(var_value, /datum))
			. = VV_DATUM_TYPE
		else
			. = VV_TYPE

	else if(islist(var_value))
		. = VV_LIST

	else if(isfile(var_value))
		. = VV_FILE
	else
		. = VV_NULL

/client/proc/vv_get_value(class, default_class, current_value, list/restricted_classes, list/extra_classes, list/classes, var_name)
	. = list("class" = class, "value" = null)
	if(!class)
		if(!classes)
			classes = list (
				VV_NUM,
				VV_TEXT,
				VV_MESSAGE,
				VV_ICON,
				VV_ATOM_REFERENCE,
				VV_DATUM_REFERENCE,
				VV_MOB_REFERENCE,
				VV_CLIENT,
				VV_ATOM_TYPE,
				VV_DATUM_TYPE,
				VV_TYPE,
				VV_FILE,
				VV_NEW_ATOM,
				VV_NEW_DATUM,
				VV_NEW_TYPE,
				VV_NEW_LIST,
				VV_NULL,
				VV_RESTORE_DEFAULT,
				VV_TEXT_LOCATE,
				VV_PROCCALL_RETVAL,
				)

		var/markstring
		if(!(VV_MARKED_DATUM in restricted_classes))
			markstring = "[VV_MARKED_DATUM] (CURRENT: [(istype(holder) && istype(holder.marked_datum))? holder.marked_datum.type : "NULL"])"
			classes += markstring

		if(restricted_classes)
			classes -= restricted_classes

		if(extra_classes)
			classes += extra_classes

		.["class"] = input(src, "What kind of data?", "Variable Type", default_class) as null|anything in classes
		if(holder && holder.marked_datum && .["class"] == markstring)
			.["class"] = VV_MARKED_DATUM

	switch(.["class"])
		if(VV_TEXT)
			.["value"] = input("Enter new text:", "Text", current_value) as null|text
			if(.["value"] == null)
				.["class"] = null
				return
		if(VV_MESSAGE)
			.["value"] = input("Enter new text:", "Text", current_value) as null|message
			if(.["value"] == null)
				.["class"] = null
				return


		if(VV_NUM)
			.["value"] = input("Enter new number:", "Num", current_value) as null|num
			if(.["value"] == null)
				.["class"] = null
				return

		if(VV_BITFIELD)
			.["value"] = input_bitfield(usr, "Editing bitfield: [var_name]", var_name, current_value)
			if(.["value"] == null)
				.["class"] = null
				return

		if(VV_ATOM_TYPE)
			.["value"] = pick_closest_path(FALSE)
			if(.["value"] == null)
				.["class"] = null
				return

		if(VV_DATUM_TYPE)
			.["value"] = pick_closest_path(FALSE, get_fancy_list_of_datum_types())
			if(.["value"] == null)
				.["class"] = null
				return

		if(VV_TYPE)
			var/type = current_value
			var/error = ""
			do
				type = input("Enter type:[error]", "Type", type) as null|text
				if(!type)
					break
				type = text2path(type)
				error = "\nType not found, Please try again"
			while(!type)
			if(!type)
				.["class"] = null
				return
			.["value"] = type

		if(VV_ATOM_REFERENCE)
			var/type = pick_closest_path(FALSE)
			var/subtypes = vv_subtype_prompt(type)
			if(subtypes == null)
				.["class"] = null
				return
			var/list/things = vv_reference_list(type, subtypes)
			var/value = input("Select reference:", "Reference", current_value) as null|anything in things
			if(!value)
				.["class"] = null
				return
			.["value"] = things[value]

		if(VV_DATUM_REFERENCE)
			var/type = pick_closest_path(FALSE, get_fancy_list_of_datum_types())
			var/subtypes = vv_subtype_prompt(type)
			if(subtypes == null)
				.["class"] = null
				return
			var/list/things = vv_reference_list(type, subtypes)
			var/value = input("Select reference:", "Reference", current_value) as null|anything in things
			if(!value)
				.["class"] = null
				return
			.["value"] = things[value]

		if(VV_MOB_REFERENCE)
			var/type = pick_closest_path(FALSE, make_types_fancy(typesof(/mob)))
			var/subtypes = vv_subtype_prompt(type)
			if(subtypes == null)
				.["class"] = null
				return
			var/list/things = vv_reference_list(type, subtypes)
			var/value = input("Select reference:", "Reference", current_value) as null|anything in things
			if(!value)
				.["class"] = null
				return
			.["value"] = things[value]

		if(VV_CLIENT)
			.["value"] = input("Select reference:", "Reference", current_value) as null|anything in GLOB.clients
			if(.["value"] == null)
				.["class"] = null
				return

		if(VV_FILE)
			.["value"] = input("Pick file:", "File") as null|file
			if(.["value"] == null)
				.["class"] = null
				return

		if(VV_ICON)
			.["value"] = input("Pick icon:", "Icon") as null|icon
			if(.["value"] == null)
				.["class"] = null
				return

		if(VV_MARKED_DATUM)
			.["value"] = holder.marked_datum
			if(.["value"] == null)
				.["class"] = null
				return

		if(VV_PROCCALL_RETVAL)
			var/list/get_retval = list()
			callproc_blocking(get_retval)
			.["value"] = get_retval[1]		//should have been set in proccall!
			if(.["value"] == null)
				.["class"] = null
				return

		if(VV_NEW_ATOM)
			var/type = pick_closest_path(FALSE)
			if(!type)
				.["class"] = null
				return
			.["type"] = type
			var/atom/newguy = new type()
			newguy.datum_flags |= DF_VAR_EDITED
			.["value"] = newguy

		if(VV_NEW_DATUM)
			var/type = pick_closest_path(FALSE, get_fancy_list_of_datum_types())
			if(!type)
				.["class"] = null
				return
			.["type"] = type
			var/datum/newguy = new type()
			newguy.datum_flags |= DF_VAR_EDITED
			.["value"] = newguy

		if(VV_NEW_TYPE)
			var/type = current_value
			var/error = ""
			do
				type = input("Enter type:[error]", "Type", type) as null|text
				if(!type)
					break
				type = text2path(type)
				error = "\nType not found, Please try again"
			while(!type)
			if(!type)
				.["class"] = null
				return
			.["type"] = type
			var/datum/newguy = new type()
			if(istype(newguy))
				newguy.datum_flags |= DF_VAR_EDITED
			.["value"] = newguy

		if(VV_NEW_LIST)
			.["value"] = list()
			.["type"] = /list

		if(VV_TEXT_LOCATE)
			var/datum/D
			do
				var/ref = input("Enter reference:", "Reference") as null|text
				if(!ref)
					break
				D = locate(ref)
				if(!D)
					alert("Invalid ref!")
					continue
				if(!D.can_vv_mark())
					alert("Datum can not be marked!")
					continue
			while(!D)
			.["type"] = D.type
			.["value"] = D
