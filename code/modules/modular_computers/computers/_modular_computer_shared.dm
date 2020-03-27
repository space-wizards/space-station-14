
/obj/proc/is_modular_computer()
	return

/obj/proc/get_modular_computer_part(part_type)
	return null

/obj/item/modular_computer/is_modular_computer()
	return TRUE

/obj/item/modular_computer/get_modular_computer_part(part_type)
	if(!part_type)
		stack_trace("get_modular_computer_part() called without a valid part_type")
		return null
	return all_components[part_type]


/obj/machinery/modular_computer/is_modular_computer()
	return TRUE

/obj/machinery/modular_computer/get_modular_computer_part(part_type)
	if(!part_type)
		stack_trace("get_modular_computer_part() called without a valid part_type")
		return null
	return cpu?.all_components[part_type]


/obj/proc/get_modular_computer_parts_examine(mob/user)
	. = list()
	if(!is_modular_computer())
		return

	var/user_is_adjacent = Adjacent(user) //don't reveal full details unless they're close enough to see it on the screen anyway.

	var/obj/item/computer_hardware/ai_slot/ai_slot = get_modular_computer_part(MC_AI)
	if(ai_slot)
		if(ai_slot.stored_card)
			if(user_is_adjacent)
				. += "It has a slot installed for an intelliCard which contains: [ai_slot.stored_card.name]"
			else
				. += "It has a slot installed for an intelliCard, which appears to be occupied."
			. += "<span class='info'>Alt-click to eject the intelliCard.</span>"
		else
			. += "It has a slot installed for an intelliCard."

	var/obj/item/computer_hardware/card_slot/card_slot = get_modular_computer_part(MC_CARD)
	if(card_slot)
		if(card_slot.stored_card || card_slot.stored_card2)
			var/obj/item/card/id/first_ID = card_slot.stored_card
			var/obj/item/card/id/second_ID = card_slot.stored_card2
			var/multiple_cards = istype(first_ID) && istype(second_ID)
			if(user_is_adjacent)
				. += "It has two slots for identification cards installed[multiple_cards ? " which contain [first_ID] and [second_ID]" : ", one of which contains [first_ID ? first_ID : second_ID]"]."
			else
				. += "It has two slots for identification cards installed, [multiple_cards ? "both of which appear" : "and one of them appears"] to be occupied."
			. += "<span class='info'>Alt-click [src] to eject the identification card[multiple_cards ? "s":""].</span>"
		else
			. += "It has two slots installed for identification cards."

	var/obj/item/computer_hardware/printer/printer_slot = get_modular_computer_part(MC_PRINT)
	if(printer_slot)
		. += "It has a printer installed."
		if(user_is_adjacent)
			. += "The printer's paper levels are at: [printer_slot.stored_paper]/[printer_slot.max_paper].</span>]"
