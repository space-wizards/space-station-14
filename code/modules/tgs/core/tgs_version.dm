/datum/tgs_version/New(raw_parameter)
	src.raw_parameter = raw_parameter
	deprefixed_parameter = replacetext(raw_parameter, "/tg/station 13 Server v", "")
	var/list/version_bits = splittext(deprefixed_parameter, ".")

	suite = text2num(version_bits[1])
	if(version_bits.len > 1)
		major = text2num(version_bits[2])
		if(version_bits.len > 2)
			minor = text2num(version_bits[3])
			if(version_bits.len == 4)
				patch = text2num(version_bits[4])

/datum/tgs_version/proc/Valid(allow_wildcards = FALSE)
	if(suite == null)
		return FALSE
	if(allow_wildcards)
		return TRUE
	return !Wildcard()

/datum/tgs_version/Wildcard()
	return major == null || minor == null || patch == null
