/datum/icon_snapshot
	var/name
	var/icon
	var/icon_state
	var/list/overlays

/datum/icon_snapshot/proc/makeImg()
	if(!icon || !icon_state)
		return
	var/obj/temp = new
	temp.icon = icon
	temp.icon_state = icon_state
	temp.overlays = overlays.Copy()
	var/icon/tempicon = getFlatIcon(temp) // TODO Actually write something less heavy-handed for this
	return tempicon
