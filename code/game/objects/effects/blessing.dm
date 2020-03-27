/obj/effect/blessing
	name = "holy blessing"
	desc = "Holy energies interfere with ethereal travel at this location."
	icon = 'icons/effects/effects.dmi'
	icon_state = null
	anchored = TRUE
	density = FALSE
	mouse_opacity = MOUSE_OPACITY_TRANSPARENT

/obj/effect/blessing/Initialize(mapload)
	. = ..()
	for(var/obj/effect/blessing/B in loc)
		if(B != src)
			return INITIALIZE_HINT_QDEL
		var/image/I = image(icon = 'icons/effects/effects.dmi', icon_state = "blessed", layer = ABOVE_OPEN_TURF_LAYER, loc = src)
		I.alpha = 64
		I.appearance_flags = RESET_ALPHA
		add_alt_appearance(/datum/atom_hud/alternate_appearance/basic/blessedAware, "blessing", I)
	RegisterSignal(loc, COMSIG_ATOM_INTERCEPT_TELEPORT, .proc/block_cult_teleport)

/obj/effect/blessing/Destroy()	
	UnregisterSignal(loc, COMSIG_ATOM_INTERCEPT_TELEPORT)
	return ..()
	
/obj/effect/blessing/proc/block_cult_teleport(datum/source, channel, turf/origin, turf/destination)
	if(channel == TELEPORT_CHANNEL_CULT)
		return COMPONENT_BLOCK_TELEPORT
