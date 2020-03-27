/datum/computer_file/program/contract_uplink
	filename = "contractor uplink"
	filedesc = "Syndicate Contractor Uplink"
	program_icon_state = "assign"
	extended_desc = "A standard, Syndicate issued system for handling important contracts while on the field."
	size = 10
	requires_ntnet = 0
	available_on_ntnet = 0
	unsendable = 1
	undeletable = 1
	tgui_id = "synd_contract"
	ui_style = "syndicate"
	ui_x = 500
	ui_y = 600
	var/error = ""
	var/info_screen = TRUE
	var/assigned = FALSE
	var/first_load = TRUE

/datum/computer_file/program/contract_uplink/run_program(var/mob/living/user)
	. = ..(user)

/datum/computer_file/program/contract_uplink/ui_act(action, params)
	if(..())
		return TRUE

	var/mob/living/user = usr
	var/obj/item/computer_hardware/hard_drive/small/syndicate/hard_drive = computer.all_components[MC_HDD]

	switch(action)
		if("PRG_contract-accept")
			var/contract_id = text2num(params["contract_id"])

			// Set as the active contract
			hard_drive.traitor_data.contractor_hub.assigned_contracts[contract_id].status = CONTRACT_STATUS_ACTIVE
			hard_drive.traitor_data.contractor_hub.current_contract = hard_drive.traitor_data.contractor_hub.assigned_contracts[contract_id]

			program_icon_state = "single_contract"
			return TRUE
		if("PRG_login")
			var/datum/antagonist/traitor/traitor_data = user.mind.has_antag_datum(/datum/antagonist/traitor)

			// Bake their data right into the hard drive, or we don't allow non-antags gaining access to an unused
			// contract system.
			// We also create their contracts at this point.
			if (traitor_data)
				// Only play greet sound, and handle contractor hub when assigning for the first time.
				if (!traitor_data.contractor_hub)
					user.playsound_local(user, 'sound/effects/contractstartup.ogg', 100, FALSE)
					traitor_data.contractor_hub = new
					traitor_data.contractor_hub.create_hub_items()

				// Stops any topic exploits such as logging in multiple times on a single system.
				if (!assigned)
					traitor_data.contractor_hub.create_contracts(traitor_data.owner)

					hard_drive.traitor_data = traitor_data

					program_icon_state = "contracts"
					assigned = TRUE
			else
				error = "UNAUTHORIZED USER"
			return TRUE
		if("PRG_call_extraction")
			if (hard_drive.traitor_data.contractor_hub.current_contract.status != CONTRACT_STATUS_EXTRACTING)
				if (hard_drive.traitor_data.contractor_hub.current_contract.handle_extraction(user))
					user.playsound_local(user, 'sound/effects/confirmdropoff.ogg', 100, TRUE)
					hard_drive.traitor_data.contractor_hub.current_contract.status = CONTRACT_STATUS_EXTRACTING

					program_icon_state = "extracted"
				else
					user.playsound_local(user, 'sound/machines/uplinkerror.ogg', 50)
					error = "Either both you or your target aren't at the dropoff location, or the pod hasn't got a valid place to land. Clear space, or make sure you're both inside."
			else
				user.playsound_local(user, 'sound/machines/uplinkerror.ogg', 50)
				error = "Already extracting... Place the target into the pod. If the pod was destroyed, you will need to cancel this contract."

			return TRUE
		if("PRG_contract_abort")
			var/contract_id = hard_drive.traitor_data.contractor_hub.current_contract.id

			hard_drive.traitor_data.contractor_hub.current_contract = null
			hard_drive.traitor_data.contractor_hub.assigned_contracts[contract_id].status = CONTRACT_STATUS_ABORTED

			program_icon_state = "contracts"

			return TRUE
		if("PRG_redeem_TC")
			if (hard_drive.traitor_data.contractor_hub.contract_TC_to_redeem)
				var/obj/item/stack/telecrystal/crystals = new /obj/item/stack/telecrystal(get_turf(user),
															hard_drive.traitor_data.contractor_hub.contract_TC_to_redeem)
				if(ishuman(user))
					var/mob/living/carbon/human/H = user
					if(H.put_in_hands(crystals))
						to_chat(H, "<span class='notice'>Your payment materializes into your hands!</span>")
					else
						to_chat(user, "<span class='notice'>Your payment materializes onto the floor.</span>")

				hard_drive.traitor_data.contractor_hub.contract_TC_payed_out += hard_drive.traitor_data.contractor_hub.contract_TC_to_redeem
				hard_drive.traitor_data.contractor_hub.contract_TC_to_redeem = 0
				return TRUE
			else
				user.playsound_local(user, 'sound/machines/uplinkerror.ogg', 50)
			return TRUE
		if ("PRG_clear_error")
			error = ""
			return TRUE
		if("PRG_set_first_load_finished")
			first_load = FALSE
			return TRUE
		if("PRG_toggle_info")
			info_screen = !info_screen
			return TRUE
		if ("buy_hub")
			if (hard_drive.traitor_data.owner.current == user)
				var/item = params["item"]

				for (var/datum/contractor_item/hub_item in hard_drive.traitor_data.contractor_hub.hub_items)
					if (hub_item.name == item)
						hub_item.handle_purchase(hard_drive.traitor_data.contractor_hub, user)
			else
				error = "Invalid user... You weren't recognised as the user of this system."

/datum/computer_file/program/contract_uplink/ui_data(mob/user)
	var/list/data = list()
	var/obj/item/computer_hardware/hard_drive/small/syndicate/hard_drive = computer.all_components[MC_HDD]
	var/screen_to_be = null

	data["first_load"] = first_load

	if (hard_drive && hard_drive.traitor_data != null)
		var/datum/antagonist/traitor/traitor_data = hard_drive.traitor_data
		data += get_header_data()

		if (traitor_data.contractor_hub.current_contract)
			data["ongoing_contract"] = TRUE
			screen_to_be = "single_contract"
			if (traitor_data.contractor_hub.current_contract.status == CONTRACT_STATUS_EXTRACTING)
				data["extraction_enroute"] = TRUE
				screen_to_be = "extracted"
			else
				data["extraction_enroute"] = FALSE
		else
			data["ongoing_contract"] = FALSE
			data["extraction_enroute"] = FALSE

		data["logged_in"] = TRUE
		data["station_name"] = GLOB.station_name
		data["redeemable_tc"] = traitor_data.contractor_hub.contract_TC_to_redeem
		data["earned_tc"] = traitor_data.contractor_hub.contract_TC_payed_out
		data["contracts_completed"] = traitor_data.contractor_hub.contracts_completed
		data["contract_rep"] = traitor_data.contractor_hub.contract_rep

		data["info_screen"] = info_screen

		data["error"] = error

		for (var/datum/contractor_item/hub_item in traitor_data.contractor_hub.hub_items)
			data["contractor_hub_items"] += list(list(
				"name" = hub_item.name,
				"desc" = hub_item.desc,
				"cost" = hub_item.cost,
				"limited" = hub_item.limited,
				"item_icon" = hub_item.item_icon
			))

		for (var/datum/syndicate_contract/contract in traitor_data.contractor_hub.assigned_contracts)
			data["contracts"] += list(list(
				"target" = contract.contract.target,
				"target_rank" = contract.target_rank,
				"payout" = contract.contract.payout,
				"payout_bonus" = contract.contract.payout_bonus,
				"dropoff" = contract.contract.dropoff,
				"id" = contract.id,
				"status" = contract.status,
				"message" = contract.wanted_message
			))

		var/direction
		if (traitor_data.contractor_hub.current_contract)
			var/turf/curr = get_turf(user)
			var/turf/dropoff_turf
			data["current_location"] = "[get_area_name(curr, TRUE)]"

			for (var/turf/content in traitor_data.contractor_hub.current_contract.contract.dropoff.contents)
				if (isturf(content))
					dropoff_turf = content
					break

			if(curr.z == dropoff_turf.z) //Direction calculations for same z-level only
				direction = uppertext(dir2text(get_dir(curr, dropoff_turf))) //Direction text (East, etc). Not as precise, but still helpful.
				if(get_area(user) == traitor_data.contractor_hub.current_contract.contract.dropoff)
					direction = "LOCATION CONFIRMED"
			else
				direction = "???"

			data["dropoff_direction"] = direction

	else
		data["logged_in"] = FALSE

	program_icon_state = screen_to_be
	update_computer_icon()
	return data
