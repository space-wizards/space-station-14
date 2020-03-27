
/proc/overwrite_field_if_available(datum/data/record/base, datum/data/record/other, var/field_name)
	if(other.fields[field_name])
		base.fields[field_name] = other.fields[field_name]

		
