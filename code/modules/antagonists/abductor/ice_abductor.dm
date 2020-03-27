/obj/structure/fluff/iced_abductor ///Unless more non-machine ayy structures made, it will stay in fluff.
	name = "Mysterious Block of Ice"
	desc = "A shadowy figure lies in this sturdy-looking block of ice. Who knows where it came from?"
	icon = 'icons/effects/freeze.dmi'
	icon_state =  "ice_ayy"
	density = TRUE
	deconstructible = FALSE

/obj/structure/fluff/iced_abductor/Destroy()
	var/turf/T = get_turf(src)
	new /obj/effect/mob_spawn/human/abductor(T)
	. = ..()
