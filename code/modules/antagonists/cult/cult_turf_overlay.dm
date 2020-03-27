//an "overlay" used by clockwork walls and floors to appear normal to mesons.
/obj/effect/cult_turf/overlay
	mouse_opacity = MOUSE_OPACITY_TRANSPARENT
	var/atom/linked

/obj/effect/cult_turf/overlay/examine(mob/user)
	if(linked)
		linked.examine(user)

/obj/effect/cult_turf/overlay/ex_act()
	return FALSE

/obj/effect/cult_turf/overlay/singularity_act()
	return
/obj/effect/cult_turf/overlay/singularity_pull()
	return

/obj/effect/cult_turf/overlay/singularity_pull(S, current_size)
	return

/obj/effect/cult_turf/overlay/Destroy()
	if(linked)
		linked = null
	. = ..()

/obj/effect/cult_turf/overlay/floor
	icon = 'icons/turf/floors.dmi'
	icon_state = "clockwork_floor"
	layer = TURF_LAYER

/obj/effect/cult_turf/overlay/floor/bloodcult
	icon_state = "cult"
