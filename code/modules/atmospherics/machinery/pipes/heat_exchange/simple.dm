/obj/machinery/atmospherics/pipe/heat_exchanging/simple
	icon = 'icons/obj/atmospherics/pipes/he-simple.dmi'
	icon_state = "pipe11-2"

	name = "pipe"
	desc = "A one meter section of heat-exchanging pipe."

	dir = SOUTH
	initialize_directions = SOUTH|NORTH
	pipe_flags = PIPING_CARDINAL_AUTONORMALIZE

	device_type = BINARY

	construction_type = /obj/item/pipe/binary/bendable
	pipe_state = "he"

/obj/machinery/atmospherics/pipe/heat_exchanging/simple/SetInitDirections()
	if(dir in GLOB.diagonals)
		initialize_directions = dir
		return
	switch(dir)
		if(NORTH, SOUTH)
			initialize_directions = SOUTH|NORTH
		if(EAST, WEST)
			initialize_directions = EAST|WEST

/obj/machinery/atmospherics/pipe/heat_exchanging/simple/update_icon()
	icon_state = "pipe[nodes[1] ? "1" : "0"][nodes[2] ? "1" : "0"]-[piping_layer]"
	update_layer()
	update_alpha()


/obj/machinery/atmospherics/pipe/heat_exchanging/simple/layer1
	piping_layer = 1
	icon_state = "pipe11-1"

/obj/machinery/atmospherics/pipe/heat_exchanging/simple/layer3
	piping_layer = 3
	icon_state = "pipe11-3"
