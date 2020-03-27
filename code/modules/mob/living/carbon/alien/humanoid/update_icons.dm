
/mob/living/carbon/alien/humanoid/update_icons()
	cut_overlays()
	for(var/I in overlays_standing)
		add_overlay(I)

	var/asleep = IsSleeping()
	if(stat == DEAD)
		//If we mostly took damage from fire
		if(getFireLoss() > 125)
			icon_state = "alien[caste]_husked"
		else
			icon_state = "alien[caste]_dead"

	else if((stat == UNCONSCIOUS && !asleep) || stat == SOFT_CRIT || IsParalyzed())
		icon_state = "alien[caste]_unconscious"
	else if(leap_on_click)
		icon_state = "alien[caste]_pounce"

	else if(!(mobility_flags & MOBILITY_STAND))
		icon_state = "alien[caste]_sleep"
	else if(mob_size == MOB_SIZE_LARGE)
		icon_state = "alien[caste]"
		if(drooling)
			add_overlay("alienspit_[caste]")
	else
		icon_state = "alien[caste]"
		if(drooling)
			add_overlay("alienspit")

	if(leaping)
		if(alt_icon == initial(alt_icon))
			var/old_icon = icon
			icon = alt_icon
			alt_icon = old_icon
		icon_state = "alien[caste]_leap"
		pixel_x = -32
		pixel_y = -32
	else
		if(alt_icon != initial(alt_icon))
			var/old_icon = icon
			icon = alt_icon
			alt_icon = old_icon
		pixel_x = get_standard_pixel_x_offset(mobility_flags & MOBILITY_STAND)
		pixel_y = get_standard_pixel_y_offset(mobility_flags & MOBILITY_STAND)
	update_inv_hands()
	update_inv_handcuffed()

/mob/living/carbon/alien/humanoid/regenerate_icons()
	if(!..())
	//	update_icons() //Handled in update_transform(), leaving this here as a reminder
		update_transform()

/mob/living/carbon/alien/humanoid/update_transform() //The old method of updating lying/standing was update_icons(). Aliens still expect that.
	if(lying)
		lying = 90 //Anything else looks retarded
	..()
	update_icons()

/mob/living/carbon/alien/humanoid/update_inv_handcuffed()
	remove_overlay(HANDCUFF_LAYER)
	var/cuff_icon = "aliencuff"
	var/dmi_file = 'icons/mob/alien.dmi'

	if(mob_size == MOB_SIZE_LARGE)
		cuff_icon = "aliencuff_[caste]"
		dmi_file = 'icons/mob/alienqueen.dmi'

	if(handcuffed)
		overlays_standing[HANDCUFF_LAYER] = mutable_appearance(dmi_file, cuff_icon, -HANDCUFF_LAYER)
		apply_overlay(HANDCUFF_LAYER)

//Royals have bigger sprites, so inhand things must be handled differently.
/mob/living/carbon/alien/humanoid/royal/update_inv_hands()
	..()
	remove_overlay(HANDS_LAYER)
	var/list/hands = list()

	var/obj/item/l_hand = get_item_for_held_index(1)
	if(l_hand)
		var/itm_state = l_hand.item_state
		if(!itm_state)
			itm_state = l_hand.icon_state
		hands += mutable_appearance(alt_inhands_file, "[itm_state][caste]_l", -HANDS_LAYER)

	var/obj/item/r_hand = get_item_for_held_index(2)
	if(r_hand)
		var/itm_state = r_hand.item_state
		if(!itm_state)
			itm_state = r_hand.icon_state
		hands += mutable_appearance(alt_inhands_file, "[itm_state][caste]_r", -HANDS_LAYER)

	overlays_standing[HANDS_LAYER] = hands
	apply_overlay(HANDS_LAYER)
