/obj/machinery/computer/bank_machine
	name = "bank machine"
	desc = "A machine used to deposit and withdraw station funds."
	icon = 'goon/icons/obj/goon_terminals.dmi'
	idle_power_usage = 100
	ui_x = 320
	ui_y = 165
	var/siphoning = FALSE
	var/next_warning = 0
	var/obj/item/radio/radio
	var/radio_channel = RADIO_CHANNEL_COMMON
	var/minimum_time_between_warnings = 400
	var/syphoning_credits = 0

/obj/machinery/computer/bank_machine/Initialize()
	. = ..()
	radio = new(src)
	radio.subspace_transmission = TRUE
	radio.canhear_range = 0
	radio.recalculateChannels()

/obj/machinery/computer/bank_machine/Destroy()
	QDEL_NULL(radio)
	. = ..()

/obj/machinery/computer/bank_machine/attackby(obj/item/I, mob/user)
	var/value = 0
	if(istype(I, /obj/item/stack/spacecash))
		var/obj/item/stack/spacecash/C = I
		value = C.value * C.amount
	else if(istype(I, /obj/item/holochip))
		var/obj/item/holochip/H = I
		value = H.credits
	if(value)
		var/datum/bank_account/D = SSeconomy.get_dep_account(ACCOUNT_CAR)
		if(D)
			D.adjust_money(value)
			to_chat(user, "<span class='notice'>You deposit [I]. The Cargo Budget is now [D.account_balance] cr.</span>")
		qdel(I)
		return
	return ..()

/obj/machinery/computer/bank_machine/process()
	..()
	if(siphoning)
		if (stat & (BROKEN|NOPOWER))
			say("Insufficient power. Halting siphon.")
			end_syphon()
		var/datum/bank_account/D = SSeconomy.get_dep_account(ACCOUNT_CAR)
		if(!D.has_money(200))
			say("Cargo budget depleted. Halting siphon.")
			end_syphon()
			return

		playsound(src, 'sound/items/poster_being_created.ogg', 100, TRUE)
		syphoning_credits += 200
		D.adjust_money(-200)
		if(next_warning < world.time && prob(15))
			var/area/A = get_area(loc)
			var/message = "Unauthorized credit withdrawal underway in [A.map_name]!!"
			radio.talk_into(src, message, radio_channel)
			next_warning = world.time + minimum_time_between_warnings

/obj/machinery/computer/bank_machine/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, \
									datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "bank_machine", name, ui_x, ui_y, master_ui, state)
		ui.open()

/obj/machinery/computer/bank_machine/ui_data(mob/user)
	var/list/data = list()
	var/datum/bank_account/D = SSeconomy.get_dep_account(ACCOUNT_CAR)

	if(D)
		data["current_balance"] = D.account_balance
	else
		data["current_balance"] = 0
	data["siphoning"] = siphoning
	data["station_name"] = station_name()

	return data

/obj/machinery/computer/bank_machine/ui_act(action, params)
	if(..())
		return

	switch(action)
		if("siphon")
			say("Siphon of station credits has begun!")
			siphoning = TRUE
			. = TRUE
		if("halt")
			say("Station credit withdrawal halted.")
			end_syphon()
			. = TRUE

/obj/machinery/computer/bank_machine/proc/end_syphon()
	siphoning = FALSE
	new /obj/item/holochip(drop_location(), syphoning_credits) //get the loot
	syphoning_credits = 0
