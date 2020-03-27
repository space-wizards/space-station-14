/datum/fantasy_affix
	var/placement // A bitflag of "slots" this affix takes up, for example pre/suffix
	var/alignment
	var/weight = 10

// For those occasional affixes which only make sense in certain circumstances
/datum/fantasy_affix/proc/validate(datum/component/fantasy/comp)
	return TRUE

/datum/fantasy_affix/proc/apply(datum/component/fantasy/comp, newName)
	return newName

/datum/fantasy_affix/proc/remove(datum/component/fantasy/comp)
