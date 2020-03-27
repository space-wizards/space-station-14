
/mob/living/carbon/alien/larva/regenerate_icons()
	cut_overlays()
	update_icons()

/mob/living/carbon/alien/larva/update_icons()
	var/state = 0
	if(amount_grown > 80)
		state = 2
	else if(amount_grown > 50)
		state = 1

	if(stat == DEAD)
		icon_state = "larva[state]_dead"
	else if(handcuffed || legcuffed) //This should be an overlay. Who made this an icon_state?
		icon_state = "larva[state]_cuff"
	else if(!(mobility_flags & MOBILITY_STAND))
		icon_state = "larva[state]_sleep"
	else if(IsStun())
		icon_state = "larva[state]_stun"
	else
		icon_state = "larva[state]"

/mob/living/carbon/alien/larva/update_transform() //All this is handled in update_icons()
	..()
	return update_icons()

/mob/living/carbon/alien/larva/update_inv_handcuffed()
	update_icons() //larva icon_state changes if cuffed/uncuffed.


