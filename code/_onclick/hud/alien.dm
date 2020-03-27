/obj/screen/alien
	icon = 'icons/mob/screen_alien.dmi'

/obj/screen/alien/leap
	name = "toggle leap"
	icon_state = "leap_off"

/obj/screen/alien/leap/Click()
	if(isalienhunter(usr))
		var/mob/living/carbon/alien/humanoid/hunter/AH = usr
		AH.toggle_leap()

/obj/screen/alien/plasma_display
	icon = 'icons/mob/screen_gen.dmi'
	icon_state = "power_display2"
	name = "plasma stored"
	screen_loc = ui_alienplasmadisplay


/obj/screen/alien/alien_queen_finder
	icon = 'icons/mob/screen_alien.dmi'
	icon_state = "queen_finder"
	name = "queen sense"
	desc = "Allows you to sense the general direction of your Queen."
	screen_loc = ui_alien_queen_finder

/datum/hud/alien
	ui_style = 'icons/mob/screen_alien.dmi'

/datum/hud/alien/New(mob/living/carbon/alien/humanoid/owner)
	..()

	var/obj/screen/using

//equippable shit

//hands
	build_hand_slots()

//begin buttons

	using = new /obj/screen/swap_hand()
	using.icon = ui_style
	using.icon_state = "swap_1"
	using.screen_loc = ui_swaphand_position(owner,1)
	using.hud = src
	static_inventory += using

	using = new /obj/screen/swap_hand()
	using.icon = ui_style
	using.icon_state = "swap_2"
	using.screen_loc = ui_swaphand_position(owner,2)
	using.hud = src
	static_inventory += using

	using = new /obj/screen/act_intent/alien()
	using.icon_state = mymob.a_intent
	using.hud = src
	static_inventory += using
	action_intent = using

	if(isalienhunter(mymob))
		var/mob/living/carbon/alien/humanoid/hunter/H = mymob
		H.leap_icon = new /obj/screen/alien/leap()
		H.leap_icon.screen_loc = ui_alien_storage_r
		static_inventory += H.leap_icon

	using = new/obj/screen/language_menu
	using.screen_loc = ui_alien_language_menu
	using.hud = src
	static_inventory += using

	using = new /obj/screen/drop()
	using.icon = ui_style
	using.screen_loc = ui_drop_throw
	using.hud = src
	static_inventory += using

	using = new /obj/screen/resist()
	using.icon = ui_style
	using.screen_loc = ui_above_movement
	using.hud = src
	hotkeybuttons += using

	throw_icon = new /obj/screen/throw_catch()
	throw_icon.icon = ui_style
	throw_icon.screen_loc = ui_drop_throw
	throw_icon.hud = src
	hotkeybuttons += throw_icon

	pull_icon = new /obj/screen/pull()
	pull_icon.icon = ui_style
	pull_icon.update_icon()
	pull_icon.screen_loc = ui_above_movement
	pull_icon.hud = src
	static_inventory += pull_icon

//begin indicators

	healths = new /obj/screen/healths/alien()
	healths.hud = src
	infodisplay += healths

	alien_plasma_display = new /obj/screen/alien/plasma_display()
	alien_plasma_display.hud = src
	infodisplay += alien_plasma_display

	if(!isalienqueen(mymob))
		alien_queen_finder = new /obj/screen/alien/alien_queen_finder
		alien_queen_finder.hud = src
		infodisplay += alien_queen_finder

	zone_select = new /obj/screen/zone_sel/alien()
	zone_select.hud = src
	zone_select.update_icon()
	static_inventory += zone_select

	for(var/obj/screen/inventory/inv in (static_inventory + toggleable_inventory))
		if(inv.slot_id)
			inv.hud = src
			inv_slots[TOBITSHIFT(inv.slot_id) + 1] = inv
			inv.update_icon()

/datum/hud/alien/persistent_inventory_update()
	if(!mymob)
		return
	var/mob/living/carbon/alien/humanoid/H = mymob
	if(hud_version != HUD_STYLE_NOHUD)
		for(var/obj/item/I in H.held_items)
			I.screen_loc = ui_hand_position(H.get_held_index_of_item(I))
			H.client.screen += I
	else
		for(var/obj/item/I in H.held_items)
			I.screen_loc = null
			H.client.screen -= I
