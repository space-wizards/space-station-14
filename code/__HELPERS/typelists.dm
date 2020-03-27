GLOBAL_LIST_EMPTY(typelists)

#ifndef TESTING

/datum/proc/typelist(key, list/values = list())
	var/list/mytypelist = GLOB.typelists[type] || (GLOB.typelists[type] = list())
	return mytypelist[key] || (mytypelist[key] = values.Copy())

#else
// mostly the same code as above, just more verbose, slower and has tallying for saved lists
/datum/proc/typelist(key, list/values)
	if (!values)
		values = list()
	GLOB.typelistkeys |= key
	if (GLOB.typelists[type])
		if (GLOB.typelists[type][key])
			GLOB.typelists[type]["[key]-saved"]++
			return GLOB.typelists[type][key]
		else
			GLOB.typelists[type][key] = values.Copy()
	else
		GLOB.typelists[type] = list()
		GLOB.typelists[type][key] = values.Copy()
	return GLOB.typelists[type][key]

GLOBAL_LIST_EMPTY(typelistkeys)

/proc/tallytypelistsavings()
	var/savings = list()
	var/saveditems = list()
	for (var/key in GLOB.typelistkeys)
		savings[key] = 0
		saveditems[key] = 0

	for (var/type in GLOB.typelists)
		for (var/saving in savings)
			if (GLOB.typelists[type]["[saving]-saved"])
				savings[saving] += GLOB.typelists[type]["[saving]-saved"]
				saveditems[saving] += (GLOB.typelists[type]["[saving]-saved"] * length(GLOB.typelists[type][saving]))

	for (var/saving in savings)
		to_chat(world, "Savings for [saving]: [savings[saving]] lists, [saveditems[saving]] items")
#endif
