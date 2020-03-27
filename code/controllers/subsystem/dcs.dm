PROCESSING_SUBSYSTEM_DEF(dcs)
	name = "Datum Component System"
	flags = SS_NO_INIT

	var/list/elements_by_type = list()

/datum/controller/subsystem/processing/dcs/Recover()
	comp_lookup = SSdcs.comp_lookup

/datum/controller/subsystem/processing/dcs/proc/GetElement(datum/element/eletype, ...)
	var/element_id = eletype
	
	if(initial(eletype.element_flags) & ELEMENT_BESPOKE)
		var/list/fullid = list("[eletype]")
		for(var/i in initial(eletype.id_arg_index) to length(args))
			var/argument = args[i]
			if(istext(argument) || isnum(argument))
				fullid += "[argument]"
			else
				fullid += "[REF(argument)]"
		element_id = fullid.Join("&")
			
	. = elements_by_type[element_id]
	if(.)
		return
	if(!ispath(eletype, /datum/element))
		CRASH("Attempted to instantiate [eletype] as a /datum/element")
	. = elements_by_type[element_id] = new eletype
