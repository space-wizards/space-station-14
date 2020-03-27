/datum/wires/microwave
	holder_type = /obj/machinery/microwave
	proper_name = "Microwave"

/datum/wires/microwave/New(atom/holder)
	wires = list(
		WIRE_ACTIVATE
	)
	..()

/datum/wires/microwave/interactable(mob/user)
	. = FALSE
	var/obj/machinery/microwave/M = holder
	if(M.panel_open)
		. = TRUE

/datum/wires/microwave/on_pulse(wire)
	var/obj/machinery/microwave/M = holder
	switch(wire)
		if(WIRE_ACTIVATE)
			M.cook()

/datum/wires/microwave/on_cut(wire, mend)
	var/obj/machinery/microwave/M = holder
	switch(wire)
		if(WIRE_ACTIVATE)
			M.wire_disabled = !mend
