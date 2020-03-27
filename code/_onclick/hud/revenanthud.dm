/datum/hud/revenant
	ui_style = 'icons/mob/screen_gen.dmi'

/datum/hud/revenant/New(mob/owner)
	..()

	pull_icon = new /obj/screen/pull()
	pull_icon.icon = ui_style
	pull_icon.update_icon()
	pull_icon.screen_loc = ui_living_pull
	pull_icon.hud = src
	static_inventory += pull_icon

	healths = new /obj/screen/healths/revenant()
	healths.hud = src
	infodisplay += healths
