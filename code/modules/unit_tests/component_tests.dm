/datum/unit_test/component_duping/Run()
	var/list/bad_dms = list()
	var/list/bad_dts = list()
	for(var/t in typesof(/datum/component))
		var/datum/component/comp = t
		if(!isnum(initial(comp.dupe_mode)))
			bad_dms += t
		var/dupe_type = initial(comp.dupe_type)
		if(dupe_type && !ispath(dupe_type))
			bad_dts += t
	if(length(bad_dms) || length(bad_dts))
		Fail("Components with invalid dupe modes: ([bad_dms.Join(",")]) ||| Components with invalid dupe types: ([bad_dts.Join(",")])")
