/datum/nanite_extra_setting/type
	setting_type = NESTYPE_TYPE
	var/list/types

/datum/nanite_extra_setting/type/New(initial, types)
	value = initial
	src.types = types

/datum/nanite_extra_setting/type/get_copy()
	return new /datum/nanite_extra_setting/type(value, types)

/datum/nanite_extra_setting/type/get_frontend_list(name)
	return list(list(
		"name" = name,
		"type" = setting_type,
		"value" = value,
		"types" = types
	))
