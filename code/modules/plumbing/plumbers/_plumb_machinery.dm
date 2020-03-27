/**Basic plumbing object.
* It doesn't really hold anything special, YET.
* Objects that are plumbing but not a subtype are as of writing liquid pumps and the reagent_dispenser tank
* Also please note that the plumbing component is toggled on and off by the component using a signal from default_unfasten_wrench, so dont worry about it
*/
/obj/machinery/plumbing
	name = "pipe thing"
	icon = 'icons/obj/plumbing/plumbers.dmi'
	icon_state = "pump"
	density = TRUE
	active_power_usage = 30
	use_power = ACTIVE_POWER_USE
	resistance_flags = FIRE_PROOF | UNACIDABLE | ACID_PROOF
	///Plumbing machinery is always gonna need reagents, so we might aswell put it here
	var/buffer = 50
	///Flags for reagents, like INJECTABLE, TRANSPARENT bla bla everything thats in DEFINES/reagents.dm
	var/reagent_flags = TRANSPARENT
	///wheter we partake in rcd construction or not
	var/rcd_constructable = TRUE
	///cost of the plumbing rcd construction
	var/rcd_cost = 15
	///delay of constructing it throught the plumbing rcd
	var/rcd_delay = 10

/obj/machinery/plumbing/Initialize(mapload, bolt = TRUE)
	. = ..()
	anchored = bolt
	create_reagents(buffer, reagent_flags)
	AddComponent(/datum/component/simple_rotation, ROTATION_ALTCLICK | ROTATION_CLOCKWISE | ROTATION_COUNTERCLOCKWISE | ROTATION_VERBS, null, CALLBACK(src, .proc/can_be_rotated))

/obj/machinery/plumbing/proc/can_be_rotated(mob/user,rotation_type)
	return TRUE

/obj/machinery/plumbing/examine(mob/user)
	. = ..()
	. += "<span class='notice'>The maximum volume display reads: <b>[reagents.maximum_volume] units</b>.</span>"

/obj/machinery/plumbing/wrench_act(mob/living/user, obj/item/I)
	..()
	default_unfasten_wrench(user, I)
	return TRUE

/obj/machinery/plumbing/plunger_act(obj/item/plunger/P, mob/living/user, reinforced)
	to_chat(user, "<span class='notice'>You start furiously plunging [name].")
	if(do_after(user, 30, target = src))
		to_chat(user, "<span class='notice'>You finish plunging the [name].")
		reagents.reaction(get_turf(src), TOUCH) //splash on the floor
		reagents.clear_reagents()

/obj/machinery/plumbing/welder_act(mob/living/user, obj/item/I)
	. = ..()
	if(anchored)
		to_chat(user, "<span class='warning'>The [name] needs to be unbolted to do that!</span")
	if(I.tool_start_check(user, amount=0))
		to_chat(user, "<span class='notice'>You start slicing the [name] apart.</span")
		if(I.use_tool(src, user, rcd_delay * 2, volume=50))
			deconstruct(TRUE)
			to_chat(user, "<span class='notice'>You slice the [name] apart.</span")
			return TRUE

///We can empty beakers in here and everything
/obj/machinery/plumbing/input
	name = "input gate"
	desc = "Can be manually filled with reagents from containers."
	icon_state = "pipe_input"
	reagent_flags = TRANSPARENT | REFILLABLE
	rcd_cost = 5
	rcd_delay = 5

/obj/machinery/plumbing/input/Initialize(mapload, bolt)
	. = ..()
	AddComponent(/datum/component/plumbing/simple_supply, bolt)

///We can fill beakers in here and everything. we dont inheret from input because it has nothing that we need
/obj/machinery/plumbing/output
	name = "output gate"
	desc = "A manual output for plumbing systems, for taking reagents directly into containers."
	icon_state = "pipe_output"
	reagent_flags = TRANSPARENT | DRAINABLE
	rcd_cost = 5
	rcd_delay = 5

/obj/machinery/plumbing/output/Initialize(mapload, bolt)
	. = ..()
	AddComponent(/datum/component/plumbing/simple_demand, bolt)

/obj/machinery/plumbing/tank
	name = "chemical tank"
	desc = "A massive chemical holding tank."
	icon_state = "tank"
	buffer = 400
	rcd_cost = 25
	rcd_delay = 20

/obj/machinery/plumbing/tank/Initialize(mapload, bolt)
	. = ..()
	AddComponent(/datum/component/plumbing/tank, bolt)
