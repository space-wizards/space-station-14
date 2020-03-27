//Used in storage.
/datum/numbered_display
	var/obj/item/sample_object
	var/number

/datum/numbered_display/New(obj/item/sample, _number = 1)
	if(!istype(sample))
		qdel(src)
	sample_object = sample
	number = _number
