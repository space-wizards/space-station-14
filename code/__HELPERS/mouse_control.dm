/proc/mouse_angle_from_client(client/client)
	var/list/mouse_control = params2list(client.mouseParams)
	if(mouse_control["screen-loc"] && client)
		var/list/screen_loc_params = splittext(mouse_control["screen-loc"], ",")
		var/list/screen_loc_X = splittext(screen_loc_params[1],":")
		var/list/screen_loc_Y = splittext(screen_loc_params[2],":")
		var/x = (text2num(screen_loc_X[1]) * 32 + text2num(screen_loc_X[2]) - 32)
		var/y = (text2num(screen_loc_Y[1]) * 32 + text2num(screen_loc_Y[2]) - 32)
		var/list/screenview = getviewsize(client.view)
		var/screenviewX = screenview[1] * world.icon_size
		var/screenviewY = screenview[2] * world.icon_size
		var/ox = round(screenviewX/2) - client.pixel_x //"origin" x
		var/oy = round(screenviewY/2) - client.pixel_y //"origin" y
		var/angle = SIMPLIFY_DEGREES(ATAN2(y - oy, x - ox))
		return angle

//Wow, specific name!
/proc/mouse_absolute_datum_map_position_from_client(client/client)
	if(!isloc(client.mob.loc))
		return
	var/list/mouse_control = params2list(client.mouseParams)
	var/atom/A = client.eye
	var/turf/T = get_turf(A)
	var/cx = T.x
	var/cy = T.y
	var/cz = T.z
	if(mouse_control["screen-loc"])
		var/x = 0
		var/y = 0
		var/z = 0
		var/p_x = 0
		var/p_y = 0
		//Split screen-loc up into X+Pixel_X and Y+Pixel_Y
		var/list/screen_loc_params = splittext(mouse_control["screen-loc"], ",")
		//Split X+Pixel_X up into list(X, Pixel_X)
		var/list/screen_loc_X = splittext(screen_loc_params[1],":")
		//Split Y+Pixel_Y up into list(Y, Pixel_Y)
		var/list/screen_loc_Y = splittext(screen_loc_params[2],":")
		var/sx = text2num(screen_loc_X[1])
		var/sy = text2num(screen_loc_Y[1])
		//Get the resolution of the client's current screen size.
		var/list/screenview = getviewsize(client.view)
		var/svx = screenview[1]
		var/svy = screenview[2]
		var/cox = round((svx - 1) / 2)
		var/coy = round((svy - 1) / 2)
		x = cx + (sx - 1 - cox)
		y = cy + (sy - 1 - coy)
		z = cz
		p_x = text2num(screen_loc_X[2])
		p_y = text2num(screen_loc_Y[2])
		return new /datum/position(x, y, z, p_x, p_y)
