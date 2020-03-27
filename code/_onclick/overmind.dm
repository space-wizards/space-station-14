// Blob Overmind Controls


/mob/camera/blob/ClickOn(atom/A, params) //Expand blob
	var/list/modifiers = params2list(params)
	if(modifiers["middle"])
		MiddleClickOn(A)
		return
	if(modifiers["shift"])
		ShiftClickOn(A)
		return
	if(modifiers["alt"])
		AltClickOn(A)
		return
	if(modifiers["ctrl"])
		CtrlClickOn(A)
		return
	var/turf/T = get_turf(A)
	if(T)
		expand_blob(T)

/mob/camera/blob/MiddleClickOn(atom/A) //Rally spores
	. = ..()
	var/turf/T = get_turf(A)
	if(T)
		rally_spores(T)

/mob/camera/blob/CtrlClickOn(atom/A) //Create a shield
	var/turf/T = get_turf(A)
	if(T)
		create_shield(T)

/mob/camera/blob/AltClickOn(atom/A) //Remove a blob
	var/turf/T = get_turf(A)
	if(T)
		remove_blob(T)
