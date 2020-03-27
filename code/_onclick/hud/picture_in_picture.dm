/obj/screen/movable/pic_in_pic
	name = "Picture-in-picture"
	screen_loc = "CENTER"
	plane = FLOOR_PLANE
	var/atom/center
	var/width = 0
	var/height = 0
	var/list/shown_to = list()
	var/list/viewing_turfs = list()
	var/obj/screen/component_button/button_x
	var/obj/screen/component_button/button_expand
	var/obj/screen/component_button/button_shrink

	var/mutable_appearance/standard_background
	var/const/max_dimensions = 10

/obj/screen/movable/pic_in_pic/Initialize()
	. = ..()
	make_backgrounds()

/obj/screen/movable/pic_in_pic/Destroy()
	for(var/C in shown_to)
		unshow_to(C)
	QDEL_NULL(button_x)
	QDEL_NULL(button_shrink)
	QDEL_NULL(button_expand)
	return ..()

/obj/screen/movable/pic_in_pic/component_click(obj/screen/component_button/component, params)
	if(component == button_x)
		qdel(src)
	else if(component == button_expand)
		set_view_size(width+1, height+1)
	else if(component == button_shrink)
		set_view_size(width-1, height-1)

/obj/screen/movable/pic_in_pic/proc/make_backgrounds()
	standard_background = new /mutable_appearance()
	standard_background.icon = 'icons/misc/pic_in_pic.dmi'
	standard_background.icon_state = "background"
	standard_background.layer = SPACE_LAYER

/obj/screen/movable/pic_in_pic/proc/add_buttons()
	var/static/mutable_appearance/move_tab
	if(!move_tab)
		move_tab = new /mutable_appearance()
		//all these properties are always the same, and since adding something to the overlay
		//list makes a copy, there is no reason to make a new one each call
		move_tab.icon = 'icons/misc/pic_in_pic.dmi'
		move_tab.icon_state = "move"
		move_tab.plane = HUD_PLANE
	var/matrix/M = matrix()
	M.Translate(0, (height + 0.25) * world.icon_size)
	move_tab.transform = M
	add_overlay(move_tab)

	if(!button_x)
		button_x = new /obj/screen/component_button(null, src)
		var/mutable_appearance/MA = new /mutable_appearance()
		MA.name = "close"
		MA.icon = 'icons/misc/pic_in_pic.dmi'
		MA.icon_state = "x"
		MA.plane = HUD_PLANE
		button_x.appearance = MA
	M = matrix()
	M.Translate((max(4, width) - 0.75) * world.icon_size, (height + 0.25) * world.icon_size)
	button_x.transform = M
	vis_contents += button_x

	if(!button_expand)
		button_expand = new /obj/screen/component_button(null, src)
		var/mutable_appearance/MA = new /mutable_appearance()
		MA.name = "expand"
		MA.icon = 'icons/misc/pic_in_pic.dmi'
		MA.icon_state = "expand"
		MA.plane = HUD_PLANE
		button_expand.appearance = MA
	M = matrix()
	M.Translate(world.icon_size, (height + 0.25) * world.icon_size)
	button_expand.transform = M
	vis_contents += button_expand

	if(!button_shrink)
		button_shrink = new /obj/screen/component_button(null, src)
		var/mutable_appearance/MA = new /mutable_appearance()
		MA.name = "shrink"
		MA.icon = 'icons/misc/pic_in_pic.dmi'
		MA.icon_state = "shrink"
		MA.plane = HUD_PLANE
		button_shrink.appearance = MA
	M = matrix()
	M.Translate(2 * world.icon_size, (height + 0.25) * world.icon_size)
	button_shrink.transform = M
	vis_contents += button_shrink

/obj/screen/movable/pic_in_pic/proc/add_background()
	if((width > 0) && (height > 0))
		var/matrix/M = matrix()
		M.Scale(width + 0.5, height + 0.5)
		M.Translate((width-1)/2 * world.icon_size, (height-1)/2 * world.icon_size)
		standard_background.transform = M
		add_overlay(standard_background)

/obj/screen/movable/pic_in_pic/proc/set_view_size(width, height, do_refresh = TRUE)
	width = CLAMP(width, 0, max_dimensions)
	height = CLAMP(height, 0, max_dimensions)
	src.width = width
	src.height = height

	y_off = -height * world.icon_size - 16

	cut_overlays()
	add_background()
	add_buttons()
	if(do_refresh)
		refresh_view()

/obj/screen/movable/pic_in_pic/proc/set_view_center(atom/target, do_refresh = TRUE)
	center = target
	if(do_refresh)
		refresh_view()

/obj/screen/movable/pic_in_pic/proc/refresh_view()
	vis_contents -= viewing_turfs
	if(!width || !height)
		return
	viewing_turfs = get_visible_turfs()
	vis_contents += viewing_turfs

/obj/screen/movable/pic_in_pic/proc/get_visible_turfs()
	var/turf/T = get_turf(center)
	if(!T)
		return list()
	var/turf/lowerleft = locate(max(1, T.x - round(width/2)), max(1, T.y - round(height/2)), T.z)
	var/turf/upperright = locate(min(world.maxx, lowerleft.x + width - 1), min(world.maxy, lowerleft.y + height - 1), lowerleft.z)
	return block(lowerleft, upperright)

/obj/screen/movable/pic_in_pic/proc/show_to(client/C)
	if(C)
		shown_to[C] = 1
		C.screen += src

/obj/screen/movable/pic_in_pic/proc/unshow_to(client/C)
	if(C)
		shown_to -= C
		C.screen -= src
