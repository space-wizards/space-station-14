/obj/docking_port/mobile/elevator
	name = "elevator"
	id = "elevator"
	dwidth = 3
	width = 7
	height = 7
	movement_force = list("KNOCKDOWN" = 0, "THROW" = 0)

/obj/docking_port/mobile/elevator/request(obj/docking_port/stationary/S) //No transit, no ignition, just a simple up/down platform
	initiate_docking(S, force=TRUE)
