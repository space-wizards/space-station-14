// A vendor machine for modular computer portable devices - Laptops and Tablets

/obj/machinery/lapvend
	name = "computer vendor"
	desc = "A vending machine with microfabricator capable of dispensing various NT-branded computers."
	icon = 'icons/obj/vending.dmi'
	icon_state = "robotics"
	layer = 2.9
	density = TRUE

	// The actual laptop/tablet
	var/obj/item/modular_computer/laptop/fabricated_laptop = null
	var/obj/item/modular_computer/tablet/fabricated_tablet = null

	// Utility vars
	var/state = 0 							// 0: Select device type, 1: Select loadout, 2: Payment, 3: Thankyou screen
	var/devtype = 0 						// 0: None(unselected), 1: Laptop, 2: Tablet
	var/total_price = 0						// Price of currently vended device.
	var/credits = 0

	// Device loadout
	var/dev_cpu = 1							// 1: Default, 2: Upgraded
	var/dev_battery = 1						// 1: Default, 2: Upgraded, 3: Advanced
	var/dev_disk = 1						// 1: Default, 2: Upgraded, 3: Advanced
	var/dev_netcard = 0						// 0: None, 1: Basic, 2: Long-Range
	var/dev_apc_recharger = 0				// 0: None, 1: Standard (LAPTOP ONLY)
	var/dev_printer = 0						// 0: None, 1: Standard
	var/dev_card = 0						// 0: None, 1: Standard

	ui_x = 500
	ui_y = 400

// Removes all traces of old order and allows you to begin configuration from scratch.
/obj/machinery/lapvend/proc/reset_order()
	state = 0
	devtype = 0
	if(fabricated_laptop)
		qdel(fabricated_laptop)
		fabricated_laptop = null
	if(fabricated_tablet)
		qdel(fabricated_tablet)
		fabricated_tablet = null
	dev_cpu = 1
	dev_battery = 1
	dev_disk = 1
	dev_netcard = 0
	dev_apc_recharger = 0
	dev_printer = 0
	dev_card = 0

// Recalculates the price and optionally even fabricates the device.
/obj/machinery/lapvend/proc/fabricate_and_recalc_price(fabricate = FALSE)
	total_price = 0
	if(devtype == 1) 		// Laptop, generally cheaper to make it accessible for most station roles
		var/obj/item/computer_hardware/battery/battery_module = null
		if(fabricate)
			fabricated_laptop = new /obj/item/modular_computer/laptop/buildable(src)
			fabricated_laptop.install_component(new /obj/item/computer_hardware/battery)
			battery_module = fabricated_laptop.all_components[MC_CELL]
		total_price = 99
		switch(dev_cpu)
			if(1)
				if(fabricate)
					fabricated_laptop.install_component(new /obj/item/computer_hardware/processor_unit/small)
			if(2)
				if(fabricate)
					fabricated_laptop.install_component(new /obj/item/computer_hardware/processor_unit)
				total_price += 299
		switch(dev_battery)
			if(1) // Basic(750C)
				if(fabricate)
					battery_module.try_insert(new /obj/item/stock_parts/cell/computer)
			if(2) // Upgraded(1100C)
				if(fabricate)
					battery_module.try_insert(new /obj/item/stock_parts/cell/computer/advanced)
				total_price += 199
			if(3) // Advanced(1500C)
				if(fabricate)
					battery_module.try_insert(new /obj/item/stock_parts/cell/computer/super)
				total_price += 499
		switch(dev_disk)
			if(1) // Basic(128GQ)
				if(fabricate)
					fabricated_laptop.install_component(new /obj/item/computer_hardware/hard_drive)
			if(2) // Upgraded(256GQ)
				if(fabricate)
					fabricated_laptop.install_component(new /obj/item/computer_hardware/hard_drive/advanced)
				total_price += 99
			if(3) // Advanced(512GQ)
				if(fabricate)
					fabricated_laptop.install_component(new /obj/item/computer_hardware/hard_drive/super)
				total_price += 299
		switch(dev_netcard)
			if(1) // Basic(Short-Range)
				if(fabricate)
					fabricated_laptop.install_component(new /obj/item/computer_hardware/network_card)
				total_price += 99
			if(2) // Advanced (Long Range)
				if(fabricate)
					fabricated_laptop.install_component(new /obj/item/computer_hardware/network_card/advanced)
				total_price += 299
		if(dev_apc_recharger)
			total_price += 399
			if(fabricate)
				fabricated_laptop.install_component(new /obj/item/computer_hardware/recharger/APC)
		if(dev_printer)
			total_price += 99
			if(fabricate)
				fabricated_laptop.install_component(new /obj/item/computer_hardware/printer/mini)
		if(dev_card)
			total_price += 199
			if(fabricate)
				fabricated_laptop.install_component(new /obj/item/computer_hardware/card_slot)

		return total_price
	else if(devtype == 2) 	// Tablet, more expensive, not everyone could probably afford this.
		var/obj/item/computer_hardware/battery/battery_module = null
		if(fabricate)
			fabricated_tablet = new(src)
			fabricated_tablet.install_component(new /obj/item/computer_hardware/battery)
			fabricated_tablet.install_component(new /obj/item/computer_hardware/processor_unit/small)
			battery_module = fabricated_tablet.all_components[MC_CELL]
		total_price = 199
		switch(dev_battery)
			if(1) // Basic(300C)
				if(fabricate)
					battery_module.try_insert(new /obj/item/stock_parts/cell/computer/nano)
			if(2) // Upgraded(500C)
				if(fabricate)
					battery_module.try_insert(new /obj/item/stock_parts/cell/computer/micro)
				total_price += 199
			if(3) // Advanced(750C)
				if(fabricate)
					battery_module.try_insert(new /obj/item/stock_parts/cell/computer)
				total_price += 499
		switch(dev_disk)
			if(1) // Basic(32GQ)
				if(fabricate)
					fabricated_tablet.install_component(new /obj/item/computer_hardware/hard_drive/micro)
			if(2) // Upgraded(64GQ)
				if(fabricate)
					fabricated_tablet.install_component(new /obj/item/computer_hardware/hard_drive/small)
				total_price += 99
			if(3) // Advanced(128GQ)
				if(fabricate)
					fabricated_tablet.install_component(new /obj/item/computer_hardware/hard_drive)
				total_price += 299
		switch(dev_netcard)
			if(1) // Basic(Short-Range)
				if(fabricate)
					fabricated_tablet.install_component(new/obj/item/computer_hardware/network_card)
				total_price += 99
			if(2) // Advanced (Long Range)
				if(fabricate)
					fabricated_tablet.install_component(new/obj/item/computer_hardware/network_card/advanced)
				total_price += 299
		if(dev_printer)
			total_price += 99
			if(fabricate)
				fabricated_tablet.install_component(new/obj/item/computer_hardware/printer)
		if(dev_card)
			total_price += 199
			if(fabricate)
				fabricated_tablet.install_component(new/obj/item/computer_hardware/card_slot)
		return total_price
	return FALSE





/obj/machinery/lapvend/ui_act(action, params)
	if(..())
		return TRUE

	switch(action)
		if("pick_device")
			if(state) // We've already picked a device type
				return FALSE
			devtype = text2num(params["pick"])
			state = 1
			fabricate_and_recalc_price(FALSE)
			return TRUE
		if("clean_order")
			reset_order()
			return TRUE
		if("purchase")
			try_purchase()
			return TRUE
	if((state != 1) && devtype) // Following IFs should only be usable when in the Select Loadout mode
		return FALSE
	switch(action)
		if("confirm_order")
			state = 2 // Wait for ID swipe for payment processing
			fabricate_and_recalc_price(FALSE)
			return TRUE
		if("hw_cpu")
			dev_cpu = text2num(params["cpu"])
			fabricate_and_recalc_price(FALSE)
			return TRUE
		if("hw_battery")
			dev_battery = text2num(params["battery"])
			fabricate_and_recalc_price(FALSE)
			return TRUE
		if("hw_disk")
			dev_disk = text2num(params["disk"])
			fabricate_and_recalc_price(FALSE)
			return TRUE
		if("hw_netcard")
			dev_netcard = text2num(params["netcard"])
			fabricate_and_recalc_price(FALSE)
			return TRUE
		if("hw_tesla")
			dev_apc_recharger = text2num(params["tesla"])
			fabricate_and_recalc_price(FALSE)
			return TRUE
		if("hw_nanoprint")
			dev_printer = text2num(params["print"])
			fabricate_and_recalc_price(FALSE)
			return TRUE
		if("hw_card")
			dev_card = text2num(params["card"])
			fabricate_and_recalc_price(FALSE)
			return TRUE
	return FALSE

/obj/machinery/lapvend/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	if(stat & (BROKEN | NOPOWER | MAINT))
		if(ui)
			ui.close()
		return FALSE

	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if (!ui)
		ui = new(user, src, ui_key, "computer_fabricator", "Personal Computer Vendor", ui_x, ui_y, state = state)
		ui.open()

/obj/machinery/lapvend/attackby(obj/item/I, mob/user)
	if(istype(I, /obj/item/stack/spacecash))
		var/obj/item/stack/spacecash/c = I
		if(!user.temporarilyRemoveItemFromInventory(c))
			return
		credits += c.value
		visible_message("<span class='info'><span class='name'>[user]</span> inserts [c.value] cr into [src].</span>")
		qdel(c)
		return
	else if(istype(I, /obj/item/holochip))
		var/obj/item/holochip/HC = I
		credits += HC.credits
		visible_message("<span class='info'>[user] inserts a [HC.credits] cr holocredit chip into [src].</span>")
		qdel(HC)
		return
	else if(istype(I, /obj/item/card/id))
		if(state != 2)
			return
		var/obj/item/card/id/ID = I
		var/datum/bank_account/account = ID.registered_account
		var/target_credits = total_price - credits
		if(!account.adjust_money(-target_credits))
			say("Insufficient credits on card to purchase!")
			return
		credits += target_credits
		say("[target_credits] cr has been desposited from your account.")
		return
	return ..()

// Simplified payment processing, returns 1 on success.
/obj/machinery/lapvend/proc/process_payment()
	if(total_price > credits)
		say("Insufficient credits.")
		return FALSE
	else
		return TRUE

/obj/machinery/lapvend/ui_data(mob/user)

	var/list/data = list()
	data["state"] = state
	if(state == 1)
		data["devtype"] = devtype
		data["hw_battery"] = dev_battery
		data["hw_disk"] = dev_disk
		data["hw_netcard"] = dev_netcard
		data["hw_tesla"] = dev_apc_recharger
		data["hw_nanoprint"] = dev_printer
		data["hw_card"] = dev_card
		data["hw_cpu"] = dev_cpu
	if(state == 1 || state == 2)
		data["totalprice"] = total_price
		data["credits"] = credits

	return data


/obj/machinery/lapvend/proc/try_purchase()
	// Awaiting payment state
	if(state == 2)
		if(process_payment())
			fabricate_and_recalc_price(1)
			if((devtype == 1) && fabricated_laptop)
				fabricated_laptop.forceMove(src.loc)
				fabricated_laptop = null
			else if((devtype == 2) && fabricated_tablet)
				fabricated_tablet.forceMove(src.loc)
				fabricated_tablet = null
			credits -= total_price
			say("Enjoy your new product!")
			state = 3
			addtimer(CALLBACK(src, .proc/reset_order), 100)
			return TRUE
		return FALSE
