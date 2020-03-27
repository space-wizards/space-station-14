/obj/machinery/door/airlock/alarmlock
	name = "glass alarm airlock"
	icon = 'icons/obj/doors/airlocks/station2/glass.dmi'
	overlays_file = 'icons/obj/doors/airlocks/station2/overlays.dmi'
	opacity = 0
	assemblytype = /obj/structure/door_assembly/door_assembly_public
	glass = TRUE

	var/datum/radio_frequency/air_connection
	var/air_frequency = FREQ_ATMOS_ALARMS
	autoclose = FALSE

/obj/machinery/door/airlock/alarmlock/Initialize()
	. = ..()
	air_connection = new

/obj/machinery/door/airlock/alarmlock/Destroy()
	SSradio.remove_object(src,air_frequency)
	air_connection = null
	return ..()

/obj/machinery/door/airlock/alarmlock/Initialize()
	. = ..()
	SSradio.remove_object(src, air_frequency)
	air_connection = SSradio.add_object(src, air_frequency, RADIO_TO_AIRALARM)
	open()

/obj/machinery/door/airlock/alarmlock/receive_signal(datum/signal/signal)
	..()
	if(stat & (NOPOWER|BROKEN))
		return

	var/alarm_area = signal.data["zone"]
	var/alert = signal.data["alert"]

	if(alarm_area == get_area_name(src))
		switch(alert)
			if("severe")
				autoclose = TRUE
				close()
			if("minor", "clear")
				autoclose = FALSE
				open()
