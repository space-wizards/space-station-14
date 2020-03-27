/datum/nanite_extra_setting/text
	setting_type = NESTYPE_TEXT

/datum/nanite_extra_setting/text/New(initial)
	value = initial

/datum/nanite_extra_setting/text/set_value(value)
	src.value = trim(value)

/datum/nanite_extra_setting/text/get_copy()
	return new /datum/nanite_extra_setting/text(value)

/datum/nanite_extra_setting/text/get_frontend_list(name)
	return list(list(
		"name" = name,
		"type" = setting_type,
		"value" = value
	))
