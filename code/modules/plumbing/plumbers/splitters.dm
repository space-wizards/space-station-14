///it splits the reagents however you want. So you can "every 60 units, 45 goes left and 15 goes straight". The side direction is EAST, you can change this in the component
/obj/machinery/plumbing/splitter
	name = "Chemical Splitter"
	desc = "A chemical splitter for smart chemical factorization. Waits till a set of conditions is met and then stops all input and splits the buffer evenly or other in two ducts."
	icon_state = "splitter"
	buffer = 100
	density = FALSE

	///constantly switches between TRUE and FALSE. TRUE means the batch tick goes straight, FALSE means the next batch goes in the side duct.
	var/turn_straight = TRUE
	///how much we must transfer straight. note input can be as high as 10 reagents per process, usually
	var/transfer_straight = 5
	///how much we must transfer to the side
	var/transfer_side = 5
	//the maximum you can set the transfer to
	var/max_transfer = 9
	
	ui_x = 220
	ui_y = 105

/obj/machinery/plumbing/splitter/Initialize(mapload, bolt)
	. = ..()
	AddComponent(/datum/component/plumbing/splitter, bolt)

/obj/machinery/plumbing/splitter/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "chem_splitter", name, ui_x, ui_y, master_ui, state)
		ui.open()

/obj/machinery/plumbing/splitter/ui_data(mob/user)
	var/list/data = list()
	data["straight"] = transfer_straight
	data["side"] = transfer_side
	data["max_transfer"] = max_transfer
	return data

/obj/machinery/plumbing/splitter/ui_act(action, params)
	if(..())
		return
	. = TRUE
	switch(action)
		if("set_amount")
			var/direction = params["target"]
			var/value = CLAMP(text2num(params["amount"]), 1, max_transfer)
			switch(direction)
				if("straight")
					transfer_straight = value
				if("side")
					transfer_side = value
				else
					return FALSE
