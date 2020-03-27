/datum/computer/file/embedded_program/simple_vent_controller

	var/airpump_tag

/datum/computer/file/embedded_program/simple_vent_controller/receive_user_command(command)
	switch(command)
		if("vent_inactive")
			post_signal(new /datum/signal(list(
				"tag" = airpump_tag,
				"sigtype" = "command",
				"power" = 0
			)))

		if("vent_pump")
			post_signal(new /datum/signal(list(
				"tag" = airpump_tag,
				"sigtype" = "command",
				"stabilize" = 1,
				"power" = 1
			)))

		if("vent_clear")
			post_signal(new /datum/signal(list(
				"tag" = airpump_tag,
				"sigtype" = "command",
				"purge" = 1,
				"power" = 1
			)))

/datum/computer/file/embedded_program/simple_vent_controller/process()
	return 0


/obj/machinery/embedded_controller/radio/simple_vent_controller
	icon = 'icons/obj/airlock_machines.dmi'
	icon_state = "airlock_control_standby"

	name = "vent controller"
	density = FALSE

	frequency = FREQ_ATMOS_CONTROL
	power_channel = ENVIRON

	// Setup parameters only
	var/airpump_tag

/obj/machinery/embedded_controller/radio/simple_vent_controller/Initialize(mapload)
	. = ..()
	if(!mapload)
		return
	var/datum/computer/file/embedded_program/simple_vent_controller/new_prog = new

	new_prog.airpump_tag = airpump_tag
	new_prog.master = src
	program = new_prog

/obj/machinery/embedded_controller/radio/simple_vent_controller/update_icon_state()
	if(on && program)
		icon_state = "airlock_control_standby"
	else
		icon_state = "airlock_control_off"


/obj/machinery/embedded_controller/radio/simple_vent_controller/return_text()
	var/state_options = null
	state_options = {"<A href='?src=[REF(src)];command=vent_inactive'>Deactivate Vent</A><BR>
<A href='?src=[REF(src)];command=vent_pump'>Activate Vent / Pump</A><BR>
<A href='?src=[REF(src)];command=vent_clear'>Activate Vent / Clear</A><BR>"}
	var/output = {"<B>Vent Control Console</B><HR>
[state_options]<HR>"}

	return output
