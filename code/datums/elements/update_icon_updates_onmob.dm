//update_icon() may change the onmob icons
//Very good name, I know

/datum/element/update_icon_updates_onmob/Attach(datum/target)
	. = ..()
	if(!istype(target, /obj/item))
		return ELEMENT_INCOMPATIBLE
	RegisterSignal(target, COMSIG_ATOM_UPDATED_ICON, .proc/update_onmob)

/datum/element/update_icon_updates_onmob/proc/update_onmob(obj/item/target)
	if(ismob(target.loc))
		var/mob/M = target.loc
		if(M.is_holding(target))
			M.update_inv_hands()
		else
			M.regenerate_icons() //yeah this is shit, but we don't know which update_foo() proc to call instead so we'll call them all
