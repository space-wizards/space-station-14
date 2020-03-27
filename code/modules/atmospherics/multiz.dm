/obj/machinery/atmospherics/pipe/simple/multiz ///This is an atmospherics pipe which can relay air up a deck (Z+1). It currently only supports being on pipe layer 1
	name = "multi deck pipe adapter"
	desc = "An adapter which allows pipes to connect to other pipenets on different decks."
	icon_state = "multiz_pipe"
	icon = 'icons/obj/atmos.dmi'

/obj/machinery/atmospherics/pipe/simple/multiz/update_icon()
	. = ..()
	cut_overlays() //This adds the overlay showing it's a multiz pipe. This should go above turfs and such
	var/image/multiz_overlay_node = new(src) //If we have a firing state, light em up!
	multiz_overlay_node.icon = 'icons/obj/atmos.dmi'
	multiz_overlay_node.icon_state = "multiz_pipe"
	multiz_overlay_node.layer = HIGH_OBJ_LAYER
	add_overlay(multiz_overlay_node)

///Attempts to locate a multiz pipe that's above us, if it finds one it merges us into its pipenet
/obj/machinery/atmospherics/pipe/simple/multiz/pipeline_expansion()
	icon = 'icons/obj/atmos.dmi' //Just to refresh.
	var/turf/T = get_turf(src)
	var/obj/machinery/atmospherics/pipe/simple/multiz/above = locate(/obj/machinery/atmospherics/pipe/simple/multiz) in(SSmapping.get_turf_above(T))
	var/obj/machinery/atmospherics/pipe/simple/multiz/below = locate(/obj/machinery/atmospherics/pipe/simple/multiz) in(SSmapping.get_turf_below(T))
	if(below)
		below.pipeline_expansion() //If we've got one below us, force it to add us on facebook
	if(above)
		nodes += above
		above.nodes += src //Two way travel :)
		return ..()
	else
		return ..()
