
///////////
// EASEL //
///////////

/obj/structure/easel
	name = "easel"
	desc = "Only for the finest of art!"
	icon = 'icons/obj/artstuff.dmi'
	icon_state = "easel"
	density = TRUE
	resistance_flags = FLAMMABLE
	max_integrity = 60
	var/obj/item/canvas/painting = null

//Adding canvases
/obj/structure/easel/attackby(obj/item/I, mob/user, params)
	if(istype(I, /obj/item/canvas))
		var/obj/item/canvas/C = I
		user.dropItemToGround(C)
		painting = C
		C.forceMove(get_turf(src))
		C.layer = layer+0.1
		user.visible_message("<span class='notice'>[user] puts \the [C] on \the [src].</span>","<span class='notice'>You place \the [C] on \the [src].</span>")
	else
		return ..()


//Stick to the easel like glue
/obj/structure/easel/Move()
	var/turf/T = get_turf(src)
	. = ..()
	if(painting && painting.loc == T) //Only move if it's near us.
		painting.forceMove(get_turf(src))
	else
		painting = null

/obj/item/canvas
	name = "canvas"
	desc = "Draw out your soul on this canvas!"
	icon = 'icons/obj/artstuff.dmi'
	icon_state = "square"
	resistance_flags = FLAMMABLE
	var/width = 11
	var/height = 11
	var/list/grid
	var/list/top_colors
	var/canvas_color = "#ffffff" //empty canvas color
	var/ui_x = 400
	var/ui_y = 400
	var/painting_name //Painting name, this is set after framing.
	var/framed = FALSE //Blocks edits, set on framing

/obj/item/canvas/Initialize()
	. = ..()
	top_colors = list()
	reset_grid()

/obj/item/canvas/proc/reset_grid()
	grid = new/list(width,height)
	for(var/x in 1 to width)
		for(var/y in 1 to height)
			grid[x][y] = canvas_color

/obj/item/canvas/attack_self(mob/user)
	. = ..()
	ui_interact(user)

/obj/item/canvas/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, \
										datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)

	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "canvas", name, ui_x, ui_y, master_ui, state)
		ui.set_autoupdate(FALSE)
		ui.open()

/obj/item/canvas/attackby(obj/item/I, mob/living/user, params)
	if(user.a_intent == INTENT_HELP)
		ui_interact(user)
	else
		return ..()

/obj/item/canvas/ui_data(mob/user)
	. = ..()
	.["grid"] = grid
	.["name"] = painting_name

/obj/item/canvas/examine(mob/user)
	. = ..()
	ui_interact(user)

/obj/item/canvas/ui_act(action, params)
	. = ..()
	if(. || framed)
		return
	var/mob/user = usr
	switch(action)
		if("paint")
			var/obj/item/I = user.get_active_held_item()
			var/color = get_paint_tool_color(I)
			if(!color)
				return FALSE
			var/x = text2num(params["x"])
			var/y = text2num(params["y"])
			grid[x][y] = color
			top_colors = get_most_common_colors(3)
			update_icon()
			. = TRUE

/obj/item/canvas/update_overlays()
	. = ..()
	for(var/i in 2 to top_colors.len) //first is used as base color
		var/mutable_appearance/detail = mutable_appearance(icon, "[icon_state]_detail_[i-1]")
		detail.appearance_flags |= RESET_COLOR
		detail.color = top_colors[i]
		. += detail

/obj/item/canvas/update_icon_state()
	. = ..()
	if(top_colors.len)
		color = top_colors[1]

/obj/item/canvas/proc/get_most_common_colors(count)
	var/list/tally = list()
	for(var/x in 1 to width)
		for(var/y in 1 to height)
			tally[grid[x][y]] += 1
	sortTim(tally,/proc/cmp_numeric_dsc,associative=TRUE)
	. = list()
	for(var/result in tally)
		. += result
		if(length(.) >= count)
			break

//Todo make this element ?
/obj/item/canvas/proc/get_paint_tool_color(obj/item/I)
	if(!I)
		return
	if(istype(I, /obj/item/toy/crayon))
		var/obj/item/toy/crayon/C = I
		return C.paint_color
	else if(istype(I, /obj/item/pen))
		var/obj/item/pen/P = I
		switch(P.colour)
			if("black")
				return "#000000"
			if("blue")
				return "#0000ff"
			if("red")
				return "#ff0000"
		return P.colour
	else if(istype(I, /obj/item/soap) || istype(I, /obj/item/reagent_containers/glass/rag))
		return canvas_color

/obj/item/canvas/nineteenXnineteen
	icon_state = "square"
	width = 19
	height = 19
	ui_x = 600
	ui_y = 600

/obj/item/canvas/twentythreeXnineteen
	icon_state = "wide"
	width = 23
	height = 19
	ui_x = 800
	ui_y = 600

/obj/item/canvas/twentythreeXtwentythree
	icon_state = "square"
	width = 23
	height = 23
	ui_x = 800
	ui_y = 800


/obj/item/wallframe/painting
	name = "painting frame"
	desc = "The perfect showcase for your favorite deathtrap memories."
	icon = 'icons/obj/decals.dmi'
	custom_materials = null
	flags_1 = 0
	icon_state = "frame-empty"
	result_path = /obj/structure/sign/painting

/obj/structure/sign/painting
	name = "Painting"
	desc = "Art or \"Art\"? You decide."
	icon = 'icons/obj/decals.dmi'
	icon_state = "frame-empty"
	buildable_sign = FALSE
	var/obj/item/canvas/C

/obj/structure/sign/painting/Initialize(mapload, dir, building)
	. = ..()
	AddComponent(/datum/component/art, 20)
	if(dir)
		setDir(dir)
	if(building)
		pixel_x = (dir & 3)? 0 : (dir == 4 ? -30 : 30)
		pixel_y = (dir & 3)? (dir ==1 ? -30 : 30) : 0

/obj/structure/sign/painting/attackby(obj/item/I, mob/user, params)
	if(!C && istype(I, /obj/item/canvas))
		frame_canvas(user,I)
	else if(C && !C.painting_name && istype(I,/obj/item/pen))
		try_rename(user)
	else
		return ..()

/obj/structure/sign/painting/examine(mob/user)
	. = ..()
	if(C)
		C.ui_interact(user,state = GLOB.physical_obscured_state)

/obj/structure/sign/painting/wirecutter_act(mob/living/user, obj/item/I)
	. = ..()
	if(C)
		C.framed = FALSE
		C.forceMove(drop_location())
		C = null
		to_chat(user, "<span class='notice'>You remove the painting from the frame.</span>")
		update_icon()
		return TRUE

/obj/structure/sign/painting/proc/frame_canvas(mob/user,obj/item/canvas/new_canvas)
	if(user.transferItemToLoc(new_canvas,src))
		C = new_canvas
		C.framed = TRUE
		to_chat(user,"<span class='notice'>You frame [C].</span>")
	update_icon()

/obj/structure/sign/painting/proc/try_rename(mob/user)
	var/new_name = stripped_input(user,"What do you want to name the painting?")
	if(C && !C.painting_name && new_name && user.canUseTopic(src,BE_CLOSE))
		C.painting_name = new_name
		SStgui.update_uis(C)

/obj/structure/sign/painting/update_overlays()
	. = ..()
	if(C && C.top_colors.len)
		var/mutable_appearance/MA = mutable_appearance(icon, "frame-content-overlay")
		MA.appearance_flags |= RESET_COLOR
		MA.color = C.top_colors[1]
		. += MA
