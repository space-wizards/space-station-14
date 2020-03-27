/datum/hud/constructs
	ui_style = 'icons/mob/screen_construct.dmi'

/datum/hud/constructs/New(mob/owner)
	..()
	pull_icon = new /obj/screen/pull()
	pull_icon.icon = ui_style
	pull_icon.update_icon()
	pull_icon.screen_loc = ui_construct_pull
	pull_icon.hud = src
	static_inventory += pull_icon

	healths = new /obj/screen/healths/construct()
	healths.hud = src
	infodisplay += healths
