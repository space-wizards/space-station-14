/obj/screen/buildmode
	icon = 'icons/misc/buildmode.dmi'
	var/datum/buildmode/bd
	// If we don't do this, we get occluded by item action buttons
	layer = ABOVE_HUD_LAYER

/obj/screen/buildmode/New(bld)
	bd = bld
	return ..()

/obj/screen/buildmode/Destroy()
	bd = null
	return ..()

/obj/screen/buildmode/mode
	name = "Toggle Mode"
	icon_state = "buildmode_basic"
	screen_loc = "NORTH,WEST"

/obj/screen/buildmode/mode/Click(location, control, params)
	var/list/pa = params2list(params)

	if(pa.Find("left"))
		bd.toggle_modeswitch()
	else if(pa.Find("right"))
		bd.mode.change_settings(usr.client)
	update_icon()
	return 1

/obj/screen/buildmode/mode/update_icon_state()
	icon_state = bd.mode.get_button_iconstate()

/obj/screen/buildmode/help
	icon_state = "buildhelp"
	screen_loc = "NORTH,WEST+1"
	name = "Buildmode Help"

/obj/screen/buildmode/help/Click(location, control, params)
	bd.mode.show_help(usr.client)
	return 1

/obj/screen/buildmode/bdir
	icon_state = "build"
	screen_loc = "NORTH,WEST+2"
	name = "Change Dir"

/obj/screen/buildmode/bdir/update_icon_state()
	dir = bd.build_dir

/obj/screen/buildmode/bdir/Click()
	bd.toggle_dirswitch()
	update_icon()
	return 1

// used to switch between modes
/obj/screen/buildmode/modeswitch
	var/datum/buildmode_mode/modetype

/obj/screen/buildmode/modeswitch/New(bld, mt)
	modetype = mt
	icon_state = "buildmode_[initial(modetype.key)]"
	name = initial(modetype.key)
	return ..(bld)

/obj/screen/buildmode/modeswitch/Click()
	bd.change_mode(modetype)
	return 1

// used to switch between dirs
/obj/screen/buildmode/dirswitch
	icon_state = "build"

/obj/screen/buildmode/dirswitch/New(bld, dir)
	src.dir = dir
	name = dir2text(dir)
	return ..(bld)

/obj/screen/buildmode/dirswitch/Click()
	bd.change_dir(dir)
	return 1

/obj/screen/buildmode/quit
	icon_state = "buildquit"
	screen_loc = "NORTH,WEST+3"
	name = "Quit Buildmode"

/obj/screen/buildmode/quit/Click()
	bd.quit()
	return 1
