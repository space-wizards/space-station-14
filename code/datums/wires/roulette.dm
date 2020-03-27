/datum/wires/roulette
	holder_type = /obj/machinery/roulette
	proper_name = "Roulette Table"
	randomize = TRUE

/datum/wires/roulette/New(atom/holder)
	wires = list(
		WIRE_RESETOWNER,
		WIRE_PRIZEVEND,
		WIRE_SHOCK,
		WIRE_BOLTS
	)
	..()

/datum/wires/roulette/interactable(mob/user)
	. = FALSE
	var/obj/machinery/roulette/R = holder
	if(R.stat & MAINT)
		. = TRUE

/datum/wires/roulette/get_status()
	var/obj/machinery/roulette/R = holder
	var/list/status = list()
	status += "The machines bolts [R.anchored ? "have fallen!" : "look up."]"
	status += "The main circuit is [R.on ? "on" : "off"]."
	status += "The main system lock appears to be [R.locked ? "on" : "off"]."
	status += "The account balance system appears to be [R.my_card ? "connected to [R.my_card.registered_account.account_holder]" : "disconnected"]."
	return status

/datum/wires/roulette/on_pulse(wire)
	var/obj/machinery/roulette/R = holder
	switch(wire)
		if(WIRE_SHOCK)
			if(isliving(usr))
				R.shock(usr, 50)
		if(WIRE_BOLTS) // Pulse to toggle bolts (but only raise if power is on).
			if(!R.on)
				return
			R.anchored = !R.anchored
		if(WIRE_RESETOWNER)
			R.my_card = null
			R.audible_message("<span class='warning'>Owner reset!</span>")
			R.locked = FALSE
		if(WIRE_PRIZEVEND)
			if(isliving(usr))
				R.shock(usr, 70)
			if(R.locked)
				return
			R.audible_message("<span class='warning'>Unauthorized prize vend detected! Locking down machine!</span>")
			R.prize_theft(0.20)

/datum/wires/roulette/on_cut(wire, mend)
	var/obj/machinery/roulette/R = holder
	switch(wire)
		if(WIRE_SHOCK)
			if(isliving(usr))
				R.shock(usr, 60)
			if(mend)
				R.on = TRUE
			else
				R.on = FALSE
		if(WIRE_BOLTS) // Always drop
			if(!R.on)
				return
			R.anchored = TRUE
		if(WIRE_RESETOWNER)
			if(isliving(usr))
				R.shock(usr, 70)
		if(WIRE_PRIZEVEND)
			if(isliving(usr))
				R.shock(usr, 75)
			if(R.locked)
				return
			R.audible_message("<span class='warning'>Unauthorized prize vend detected! Locking down machine!</span>")
			R.prize_theft(0.10)

