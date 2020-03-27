/datum/nanite_extra_setting/number
	setting_type = NESTYPE_NUMBER
	var/min
	var/max
	var/unit = ""

/datum/nanite_extra_setting/number/New(initial, min, max, unit)
	value = initial
	src.min = min
	src.max = max
	if(unit)
		src.unit = unit

/datum/nanite_extra_setting/number/set_value(value)
	if(istext(value))
		value = text2num(value)
	if(!value || !isnum(value))
		return
	src.value = CLAMP(value, min, max)

/datum/nanite_extra_setting/number/get_copy()
	return new /datum/nanite_extra_setting/number(value, min, max, unit)

/datum/nanite_extra_setting/number/get_frontend_list(name)
	return list(list(
		"name" = name,
		"type" = setting_type,
		"value" = value,
		"min" = min,
		"max" = max,
		"unit" = unit
	))
