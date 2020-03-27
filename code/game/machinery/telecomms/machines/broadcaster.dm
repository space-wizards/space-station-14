/*
	The broadcaster sends processed messages to all radio devices in the game. They
	do not have to be headsets; intercoms and station-bounced radios suffice.

	They receive their message from a server after the message has been logged.
*/

GLOBAL_LIST_EMPTY(recentmessages) // global list of recent messages broadcasted : used to circumvent massive radio spam
GLOBAL_VAR_INIT(message_delay, 0) // To make sure restarting the recentmessages list is kept in sync

/obj/machinery/telecomms/broadcaster
	name = "subspace broadcaster"
	icon_state = "broadcaster"
	desc = "A dish-shaped machine used to broadcast processed subspace signals."
	density = TRUE
	use_power = IDLE_POWER_USE
	idle_power_usage = 25
	circuit = /obj/item/circuitboard/machine/telecomms/broadcaster

/obj/machinery/telecomms/broadcaster/receive_information(datum/signal/subspace/signal, obj/machinery/telecomms/machine_from)
	// Don't broadcast rejected signals
	if(!istype(signal))
		return
	if(signal.data["reject"])
		return
	if(!signal.data["message"])
		return

	// Prevents massive radio spam
	signal.mark_done()
	var/datum/signal/subspace/original = signal.original
	if(original && ("compression" in signal.data))
		original.data["compression"] = signal.data["compression"]

	var/turf/T = get_turf(src)
	if (T)
		signal.levels |= T.z

	var/signal_message = "[signal.frequency]:[signal.data["message"]]:[signal.data["name"]]"
	if(signal_message in GLOB.recentmessages)
		return
	GLOB.recentmessages.Add(signal_message)

	if(signal.data["slow"] > 0)
		sleep(signal.data["slow"]) // simulate the network lag if necessary

	signal.broadcast()

	if(!GLOB.message_delay)
		GLOB.message_delay = TRUE
		addtimer(CALLBACK(GLOBAL_PROC, .proc/end_message_delay), 1 SECONDS)

	/* --- Do a snazzy animation! --- */
	flick("broadcaster_send", src)

/proc/end_message_delay()
	GLOB.message_delay = FALSE
	GLOB.recentmessages = list()

/obj/machinery/telecomms/broadcaster/Destroy()
	// In case message_delay is left on 1, otherwise it won't reset the list and people can't say the same thing twice anymore.
	if(GLOB.message_delay)
		GLOB.message_delay = 0
	return ..()



//Preset Broadcasters

//--PRESET LEFT--//

/obj/machinery/telecomms/broadcaster/preset_left
	id = "Broadcaster A"
	network = "tcommsat"
	autolinkers = list("broadcasterA")

//--PRESET RIGHT--//

/obj/machinery/telecomms/broadcaster/preset_right
	id = "Broadcaster B"
	network = "tcommsat"
	autolinkers = list("broadcasterB")

/obj/machinery/telecomms/broadcaster/preset_left/birdstation
	name = "Broadcaster"
