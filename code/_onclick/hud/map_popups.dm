/client
	var/list/screen_maps //assoc list with all the active maps - when a screen obj is added to a map, it's put in here as well. "mapname" = list(screen objs in map)

/obj/screen
	var/assigned_map
	var/list/screen_info = list()//x,x pix, y, y pix || x,y
	var/del_on_map_removal = TRUE//this could probably be changed to be a proc, for conditional removal. for now, this works.

/client/proc/clear_map(var/map_to_clear)//not really needed most of the time, as the client's screen list gets reset on relog. any of the buttons are going to get caught by garbage collection anyway. they're effectively qdel'd.
	if(!map_to_clear|| !(map_to_clear in screen_maps))
		return FALSE
	for(var/obj/screen/x in screen_maps[map_to_clear])
		screen_maps[map_to_clear] -= x
		if(x.del_on_map_removal)
			qdel(x)
	screen_maps -= map_to_clear

/client/proc/clear_all_maps()
	for(var/x in screen_maps)
		clear_map(x)

/client/proc/close_popup(var/popup)
	winshow(src,popup,0)
	handle_popup_close(popup)

/client/verb/handle_popup_close(window_id as text) //when the popup closes in any way (player or proc call) it calls this.
	set hidden = TRUE
	clear_map("[window_id]_map")

/client/proc/create_popup(var/name, var/ratiox = 100, var/ratioy=100) //ratio is how many pixels by how many pixels. keep it simple
	winclone(src,"popupwindow",name)
	var/list/winparams = new
	winparams["size"] = "[ratiox]x[ratioy]"
	winparams["on-close"] = "handle-popup-close [name]"
	winset(src,"[name]",list2params(winparams))
	winshow(src,"[name]",1)

	var/list/params = new
	params["parent"] = "[name]"
	params["type"] = "map"
	params["size"] = "[ratiox]x[ratioy]"
	params["anchor1"] = "0,0"
	params["anchor2"] = "[ratiox],[ratioy]"
	winset(src, "[name]_map", list2params(params))
	if(!screen_maps)
		screen_maps = list()
	screen_maps["[name]_map"] = list()//initialized on the popup level, if we did it in setup_popup, we'd need to add code for the few situations where a background isn't desired.

	return "[name]_map"

/client/proc/setup_popup(var/popup_name,var/width = 9,var/height = 9,var/tilesize = 2,var/bgicon) //create the popup, and get it ready for generic use by giving it a background. width/height are multiplied by 64 by degfault.
	if(!popup_name)
		return
	clear_map("[popup_name]_map")
	var/x_value = world.icon_size*tilesize*width
	var/y_value = world.icon_size*tilesize*height
	var/newmap = create_popup(popup_name,x_value,y_value)
	var/obj/screen/background = new
	background.name = "background"
	background.assigned_map = newmap
	background.screen_loc = "[newmap]:1,1 TO [width],[height]"
	background.icon = 'icons/mob/map_backgrounds.dmi'
	if(bgicon)
		background.icon_state = bgicon
	else
		background.icon_state = "clear"
	background.layer = -1
	background.plane = -1

	screen_maps["[popup_name]_map"] += background
	screen += background

	return newmap

/client/proc/add_objs_to_map(var/list/to_add)
	if(!screen_maps)
		screen_maps = list()
	if(!to_add.len) return
	for(var/obj/screen/adding in to_add)
		var/len = adding.screen_info.len
		var/list/data = adding.screen_info
		switch (len)
			if(4) //set up for x/y offsets.
				if(adding.assigned_map)
					adding.screen_loc = "[adding.assigned_map]:[data[1]]:[data[2]],[data[3]],[data[4]]"
				else
					adding.screen_loc = "[data[1]]:[data[2]],[data[3]],[data[4]]"
			if(2) //set up for simple.
				if(adding.assigned_map)
					adding.screen_loc = "[adding.assigned_map]:[data[1]],[data[2]]"
				else
					adding.screen_loc = "[data[1]],[data[2]]"
			if(0) //legacy - screen_loc is already set up. don't add the map here, assumed to be old HUD code, or some custom overwrite (eg, x TO y) so it'd probably break it.

			else
				stack_trace("[adding]'s screen_data has an invalid length. should be either 4,2,0 - it is [len]")
				continue
		if(!screen_maps[adding.assigned_map])
			screen_maps[adding.assigned_map] = list()
		screen_maps[adding.assigned_map] += adding
		screen += adding
