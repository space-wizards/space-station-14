#define VV_HTML_ENCODE(thing) ( sanitize ? html_encode(thing) : thing )
/proc/debug_variable(name, value, level, datum/D, sanitize = TRUE)			//if D is a list, name will be index, and value will be assoc value.
	var/header
	if(D)
		if(islist(D))
			var/index = name
			if (value)
				name = D[name] //name is really the index until this line
			else
				value = D[name]
			header = "<li style='backgroundColor:white'>([VV_HREF_TARGET_1V(D, VV_HK_LIST_EDIT, "E", index)]) ([VV_HREF_TARGET_1V(D, VV_HK_LIST_CHANGE, "C", index)]) ([VV_HREF_TARGET_1V(D, VV_HK_LIST_REMOVE, "-", index)]) "
		else
			header = "<li style='backgroundColor:white'>([VV_HREF_TARGET_1V(D, VV_HK_BASIC_EDIT, "E", name)]) ([VV_HREF_TARGET_1V(D, VV_HK_BASIC_CHANGE, "C", name)]) ([VV_HREF_TARGET_1V(D, VV_HK_BASIC_MASSEDIT, "M", name)]) "
	else
		header = "<li>"

	var/item
	if (isnull(value))
		item = "[VV_HTML_ENCODE(name)] = <span class='value'>null</span>"

	else if (istext(value))
		item = "[VV_HTML_ENCODE(name)] = <span class='value'>\"[VV_HTML_ENCODE(value)]\"</span>"

	else if (isicon(value))
		#ifdef VARSICON
		var/icon/I = icon(value)
		var/rnd = rand(1,10000)
		var/rname = "tmp[REF(I)][rnd].png"
		usr << browse_rsc(I, rname)
		item = "[VV_HTML_ENCODE(name)] = (<span class='value'>[value]</span>) <img class=icon src=\"[rname]\">"
		#else
		item = "[VV_HTML_ENCODE(name)] = /icon (<span class='value'>[value]</span>)"
		#endif

	else if (isfile(value))
		item = "[VV_HTML_ENCODE(name)] = <span class='value'>'[value]'</span>"

	else if (istype(value, /datum))
		var/datum/DV = value
		if ("[DV]" != "[DV.type]") //if the thing as a name var, lets use it.
			item = "<a href='?_src_=vars;[HrefToken()];Vars=[REF(value)]'>[VV_HTML_ENCODE(name)] [REF(value)]</a> = [DV] [DV.type]"
		else
			item = "<a href='?_src_=vars;[HrefToken()];Vars=[REF(value)]'>[VV_HTML_ENCODE(name)] [REF(value)]</a> = [DV.type]"

	else if (islist(value))
		var/list/L = value
		var/list/items = list()

		if (L.len > 0 && !(name == "underlays" || name == "overlays" || L.len > (IS_NORMAL_LIST(L) ? VV_NORMAL_LIST_NO_EXPAND_THRESHOLD : VV_SPECIAL_LIST_NO_EXPAND_THRESHOLD)))
			for (var/i in 1 to L.len)
				var/key = L[i]
				var/val
				if (IS_NORMAL_LIST(L) && !isnum(key))
					val = L[key]
				if (isnull(val))	// we still want to display non-null false values, such as 0 or ""
					val = key
					key = i

				items += debug_variable(key, val, level + 1, sanitize = sanitize)

			item = "<a href='?_src_=vars;[HrefToken()];Vars=[REF(value)]'>[VV_HTML_ENCODE(name)] = /list ([L.len])</a><ul>[items.Join()]</ul>"
		else
			item = "<a href='?_src_=vars;[HrefToken()];Vars=[REF(value)]'>[VV_HTML_ENCODE(name)] = /list ([L.len])</a>"

	else if (name in GLOB.bitfields)
		var/list/flags = list()
		for (var/i in GLOB.bitfields[name])
			if (value & GLOB.bitfields[name][i])
				flags += i
			item = "[VV_HTML_ENCODE(name)] = [VV_HTML_ENCODE(jointext(flags, ", "))]"
	else
		item = "[VV_HTML_ENCODE(name)] = <span class='value'>[VV_HTML_ENCODE(value)]</span>"

	return "[header][item]</li>"

#undef VV_HTML_ENCODE
