/*
/datum/verbs/menu/Example/verb/Example()
	set name = "" //if this starts with @ the verb is not created and name becomes the command to invoke.
	set desc = "" //desc is the text given to this entry in the menu
	//You can not use src in these verbs. It will be the menu at compile time, but the client at runtime.
*/

GLOBAL_LIST_EMPTY(menulist)

/datum/verbs/menu
	var/checkbox = CHECKBOX_NONE //checkbox type.
	var/default //default checked type.
	//Set to true to append our children to our parent,
	//Rather then add us as a node (used for having more then one checkgroups in the same menu)

/datum/verbs/menu/GetList()
	return GLOB.menulist

/datum/verbs/menu/HandleVerb(list/entry, verbpath, client/C)
	var/datum/verbs/menu/verb_true_parent = GLOB.menulist[verblist[verbpath]]
	var/true_checkbox = verb_true_parent.checkbox
	if (true_checkbox != CHECKBOX_NONE)
		var/checkedverb = verb_true_parent.Get_checked(C)
		if (true_checkbox == CHECKBOX_GROUP)
			if (verbpath == checkedverb)
				entry["is-checked"] = TRUE
			else
				entry["is-checked"] = FALSE
		else if (true_checkbox == CHECKBOX_TOGGLE)
			entry["is-checked"] = checkedverb

		entry["command"] = ".updatemenuchecked \"[verb_true_parent.type]\" \"[verbpath]\"\n[entry["command"]]"
		entry["can-check"] = TRUE
		entry["group"] = "[verb_true_parent.type]"
	return list2params(entry)

/datum/verbs/menu/proc/Get_checked(client/C)
	return C.prefs.menuoptions[type] || default || FALSE

/datum/verbs/menu/proc/Load_checked(client/C) //Loads the checked menu item into a new client. Used by icon menus to invoke the checked item.
	return

/datum/verbs/menu/proc/Set_checked(client/C, verbpath)
	if (checkbox == CHECKBOX_GROUP)
		C.prefs.menuoptions[type] = verbpath
		C.prefs.save_preferences()
	else if (checkbox == CHECKBOX_TOGGLE)
		var/checked = Get_checked(C)
		C.prefs.menuoptions[type] = !checked
		C.prefs.save_preferences()
		winset(C, "[verbpath]", "is-checked = [!checked]")

/client/verb/updatemenuchecked(menutype as text, verbpath as text)
	set name = ".updatemenuchecked"
	menutype = text2path(menutype)
	verbpath = text2path(verbpath)
	if (!menutype || !verbpath)
		return
	var/datum/verbs/menu/M = GLOB.menulist[menutype]
	if (!M)
		return
	if (!(verbpath in typesof("[menutype]/verb")))
		return
	M.Set_checked(src, verbpath)


/datum/verbs/menu/Icon/Load_checked(client/C) //So we can be lazy, we invoke the "checked" menu item on menu load.
	var/procpath/verbpath = Get_checked(C)
	if (!verbpath || !(verbpath in typesof("[type]/verb")))
		return

	if(verbpath.name[1] == "@")
		winset(C, null, list2params(list("command" = copytext(verbpath.name, length(verbpath.name[1]) + 1))))
	else
		winset(C, null, list2params(list("command" = replacetext(verbpath.name, " ", "-"))))

/datum/verbs/menu/Icon/Size
	checkbox = CHECKBOX_GROUP
	default = /datum/verbs/menu/Icon/Size/verb/iconstretchtofit

/datum/verbs/menu/Icon/Size/verb/iconstretchtofit()
	set name = "@.winset \"mapwindow.map.icon-size=0\""
	set desc = "&Auto (stretch-to-fit)"

/datum/verbs/menu/Icon/Size/verb/icon96()
	set name = "@.winset \"mapwindow.map.icon-size=96\""
	set desc = "&96x96 (3x)"

/datum/verbs/menu/Icon/Size/verb/icon64()
	set name = "@.winset \"mapwindow.map.icon-size=64\""
	set desc = "&64x64 (2x)"

/datum/verbs/menu/Icon/Size/verb/icon48()
	set name = "@.winset \"mapwindow.map.icon-size=48\""
	set desc = "&48x48 (1.5x)"

/datum/verbs/menu/Icon/Size/verb/icon32()
	set name = "@.winset \"mapwindow.map.icon-size=32\""
	set desc = "&32x32 (1x)"


/datum/verbs/menu/Icon/Scaling
	checkbox = CHECKBOX_GROUP
	name = "Scaling Mode"
	default = /datum/verbs/menu/Icon/Scaling/verb/NN

/datum/verbs/menu/Icon/Scaling/verb/NN()
	set name = "@.winset \"mapwindow.map.zoom-mode=distort\""
	set desc = "Nearest Neighbor"

/datum/verbs/menu/Icon/Scaling/verb/PS()
	set name = "@.winset \"mapwindow.map.zoom-mode=normal\""
	set desc = "Point Sampling"

/datum/verbs/menu/Icon/Scaling/verb/BL()
	set name = "@.winset \"mapwindow.map.zoom-mode=blur\""
	set desc = "Bilinear"

