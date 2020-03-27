/mob/living/simple_animal/hostile/gorilla/proc/apply_overlay(cache_index)
	. = gorilla_overlays[cache_index]
	if(.)
		add_overlay(.)

/mob/living/simple_animal/hostile/gorilla/proc/remove_overlay(cache_index)
	var/I = gorilla_overlays[cache_index]
	if(I)
		cut_overlay(I)
		gorilla_overlays[cache_index] = null

/mob/living/simple_animal/hostile/gorilla/update_inv_hands()
	cut_overlays("standing_overlay")
	remove_overlay(GORILLA_HANDS_LAYER)

	var/standing = FALSE
	for(var/I in held_items)
		if(I)
			standing = TRUE
			break
	if(!standing)
		if(stat != DEAD)
			icon_state = "crawling"
			speed = 1
		return ..()
	if(stat != DEAD)
		icon_state = "standing"
		speed = 3 // Gorillas are slow when standing up.

	var/list/hands_overlays = list()

	var/obj/item/l_hand = get_item_for_held_index(1)
	var/obj/item/r_hand = get_item_for_held_index(2)

	if(r_hand)
		var/mutable_appearance/r_hand_overlay = r_hand.build_worn_icon(default_layer = GORILLA_HANDS_LAYER, default_icon_file = r_hand.righthand_file, isinhands = TRUE)
		r_hand_overlay.pixel_y -= 1
		hands_overlays += r_hand_overlay

	if(l_hand)
		var/mutable_appearance/l_hand_overlay = l_hand.build_worn_icon(default_layer = GORILLA_HANDS_LAYER, default_icon_file = l_hand.lefthand_file, isinhands = TRUE)
		l_hand_overlay.pixel_y -= 1
		hands_overlays += l_hand_overlay

	if(hands_overlays.len)
		gorilla_overlays[GORILLA_HANDS_LAYER] = hands_overlays
	apply_overlay(GORILLA_HANDS_LAYER)
	add_overlay("standing_overlay")
	return ..()

/mob/living/simple_animal/hostile/gorilla/regenerate_icons()
	update_inv_hands()

