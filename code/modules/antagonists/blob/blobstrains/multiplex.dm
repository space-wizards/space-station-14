/datum/blobstrain/multiplex
	var/list/blobstrains
	var/typeshare

/datum/blobstrain/multiplex/New(mob/camera/blob/new_overmind, list/blobstrains)
	. = ..()
	for (var/bt in blobstrains)
		if (ispath(bt, /datum/blobstrain))
			src.blobstrains += new bt(overmind)
		else if (istype(bt, /datum/blobstrain))
			var/datum/blobstrain/bts = bt
			bts.overmind = overmind
			src.blobstrains += bt
	 typeshare = (0.8 * length(src.blobstrains)) - (length(src.blobstrains)-1) // 1 is 80%, 2 are 60% etc

/datum/blobstrain/multiplex/damage_reaction(obj/structure/blob/B, damage, damage_type, damage_flag, coefficient = 1) //when the blob takes damage, do this
	for (var/datum/blobstrain/bt in blobstrains)
		. += bt.damage_reaction(B, damage, damage_type, damage_flag, coefficient*typeshare)

/datum/blobstrain/multiplex/death_reaction(obj/structure/blob/B, damage_flag, coefficient = 1) //when a blob dies, do this
	for (var/datum/blobstrain/bt in blobstrains)
		. += bt.death_reaction(B, damage_flag, coefficient*typeshare)

/datum/blobstrain/multiplex/expand_reaction(obj/structure/blob/B, obj/structure/blob/newB, turf/T, mob/camera/blob/O, coefficient = 1) //when the blob expands, do this
	for (var/datum/blobstrain/bt in blobstrains)
		. += bt.expand_reaction(B, newB, T, O, coefficient*typeshare)

/datum/blobstrain/multiplex/tesla_reaction(obj/structure/blob/B, power, coefficient = 1) //when the blob is hit by a tesla bolt, do this
	for (var/datum/blobstrain/bt in blobstrains)
		. += bt.tesla_reaction(B, power, coefficient*typeshare)
	if (prob(. / length(blobstrains) * 100))
		return 1

/datum/blobstrain/multiplex/extinguish_reaction(obj/structure/blob/B, coefficient = 1) //when the blob is hit with water, do this
	for (var/datum/blobstrain/bt in blobstrains)
		. += bt.extinguish_reaction(B, coefficient*typeshare)

/datum/blobstrain/multiplex/emp_reaction(obj/structure/blob/B, severity, coefficient = 1) //when the blob is hit with an emp, do this
	for (var/datum/blobstrain/bt in blobstrains)
		. += bt.emp_reaction(B, severity, coefficient*typeshare)
