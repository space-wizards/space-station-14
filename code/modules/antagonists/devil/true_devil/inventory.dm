/mob/living/carbon/true_devil/doUnEquip(obj/item/I, force, newloc, no_move, invdrop = TRUE, silent = FALSE)
	if(..())
		update_inv_hands()
		return 1
	return 0

/mob/living/carbon/true_devil/update_inv_hands()
	//TODO LORDPIDEY:  Figure out how to make the hands line up properly.  the l/r_hand_overlay should use the down sprite when facing down, left, or right, and the up sprite when facing up.
	remove_overlay(DEVIL_HANDS_LAYER)
	var/list/hands_overlays = list()
	var/obj/item/l_hand = get_item_for_held_index(1) //hardcoded 2-hands only, for now.
	var/obj/item/r_hand = get_item_for_held_index(2)

	if(r_hand)
		var/mutable_appearance/r_hand_overlay = r_hand.build_worn_icon(default_layer = DEVIL_HANDS_LAYER, default_icon_file = r_hand.righthand_file, isinhands = TRUE)

		hands_overlays += r_hand_overlay

		if(client && hud_used && hud_used.hud_version != HUD_STYLE_NOHUD)
			r_hand.layer = ABOVE_HUD_LAYER
			r_hand.plane = ABOVE_HUD_PLANE
			r_hand.screen_loc = ui_hand_position(get_held_index_of_item(r_hand))
			client.screen |= r_hand

	if(l_hand)
		var/mutable_appearance/l_hand_overlay = l_hand.build_worn_icon(default_layer = DEVIL_HANDS_LAYER, default_icon_file = l_hand.lefthand_file, isinhands = TRUE)

		hands_overlays += l_hand_overlay

		if(client && hud_used && hud_used.hud_version != HUD_STYLE_NOHUD)
			l_hand.layer = ABOVE_HUD_LAYER
			l_hand.plane = ABOVE_HUD_PLANE
			l_hand.screen_loc = ui_hand_position(get_held_index_of_item(l_hand))
			client.screen |= l_hand
	if(hands_overlays.len)
		devil_overlays[DEVIL_HANDS_LAYER] = hands_overlays
	apply_overlay(DEVIL_HANDS_LAYER)

/mob/living/carbon/true_devil/remove_overlay(cache_index)
	var/I = devil_overlays[cache_index]
	if(I)
		cut_overlay(I)
		devil_overlays[cache_index] = null


/mob/living/carbon/true_devil/apply_overlay(cache_index)
	if((. = devil_overlays[cache_index]))
		add_overlay(.)
