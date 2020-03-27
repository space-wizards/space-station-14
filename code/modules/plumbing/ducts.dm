/*
All the important duct code:
/code/datums/components/plumbing/plumbing.dm
/code/datums/ductnet.dm
*/
/obj/machinery/duct
	name = "fluid duct"
	icon = 'icons/obj/plumbing/fluid_ducts.dmi'
	icon_state = "nduct"
	level = 1
	///bitfield with the directions we're connected in
	var/connects
	///set to TRUE to disable smart duct behaviour
	var/dumb = FALSE
	///wheter we allow our connects to be changed after initialization or not
	var/lock_connects = FALSE
	///our ductnet, wich tracks what we're connected to
	var/datum/ductnet/duct
	///amount we can transfer per process. note that the ductnet can carry as much as the lowest capacity duct
	var/capacity = 10

	///the color of our duct
	var/duct_color = null
	///TRUE to ignore colors, so yeah we also connect with other colors without issue
	var/ignore_colors = FALSE
	///1,2,4,8,16
	var/duct_layer = DUCT_LAYER_DEFAULT
	///whether we allow our layers to be altered
	var/lock_layers = FALSE
	///TRUE to let colors connect when forced with a wrench, false to just not do that at all
	var/color_to_color_support = TRUE
	///wheter to even bother with plumbing code or not
	var/active = TRUE
	///track ducts we're connected to. Mainly for ducts we connect to that we normally wouldn't, like different layers and colors, for when we regenerate the ducts
	var/list/neighbours = list()
	///wheter we just unanchored or drop whatever is in the variable. either is safe
	var/drop_on_wrench = /obj/item/stack/ducts

/obj/machinery/duct/Initialize(mapload, no_anchor, color_of_duct, layer_of_duct = DUCT_LAYER_DEFAULT, force_connects)
	. = ..()
	if(no_anchor)
		active = FALSE
		anchored = FALSE
	else if(!can_anchor())
		qdel(src)
		CRASH("Overlapping ducts detected")
	if(force_connects)
		connects = force_connects //skip change_connects() because we're still initializing and we need to set our connects at one point
	if(!lock_layers)
		duct_layer = layer_of_duct
	if(!ignore_colors)
		duct_color = color_of_duct
	if(duct_color)
		add_atom_colour(duct_color, FIXED_COLOUR_PRIORITY)
	handle_layer()
	for(var/obj/machinery/duct/D in loc)
		if(D == src)
			continue
		if(D.duct_layer & duct_layer)
			disconnect_duct()
	if(active)
		attempt_connect()
///start looking around us for stuff to connect to
/obj/machinery/duct/proc/attempt_connect()
	reset_connects() //All connects are gathered here again eitherway, we might aswell reset it so they properly update when reconnecting

	for(var/atom/movable/AM in loc)
		var/datum/component/plumbing/P = AM.GetComponent(/datum/component/plumbing)
		if(P?.active)
			disconnect_duct() //let's not built under plumbing machinery
			return
	for(var/D in GLOB.cardinals)
		if(dumb && !(D & connects))
			continue
		for(var/atom/movable/AM in get_step(src, D))
			if(connect_network(AM, D))
				add_connects(D)
	update_icon()
///see if whatever we found can be connected to
/obj/machinery/duct/proc/connect_network(atom/movable/AM, direction, ignore_color)
	if(istype(AM, /obj/machinery/duct))
		return connect_duct(AM, direction, ignore_color)

	var/plumber = AM.GetComponent(/datum/component/plumbing)
	if(!plumber)
		return
	return connect_plumber(plumber, direction)
///connect to a duct
/obj/machinery/duct/proc/connect_duct(obj/machinery/duct/D, direction, ignore_color)
	var/opposite_dir = turn(direction, 180)
	if(!active || !D.active)
		return

	if(!dumb && D.dumb && !(opposite_dir & D.connects))
		return
	if(dumb && D.dumb && !(connects & D.connects)) //we eliminated a few more scenarios in attempt connect
		return

	if((duct == D.duct) && duct)//check if we're not just comparing two null values
		add_neighbour(D, direction)

		D.add_connects(opposite_dir)
		D.update_icon()
		return TRUE //tell the current pipe to also update it's sprite
	if(!(D in neighbours)) //we cool
		if((duct_color != D.duct_color) && !(ignore_colors || D.ignore_colors))
			return
		if(!(duct_layer & D.duct_layer))
			return

	if(D.duct)
		if(duct)
			duct.assimilate(D.duct)
		else
			D.duct.add_duct(src)
	else
		if(duct)
			duct.add_duct(D)
		else
			create_duct()
			duct.add_duct(D)
	add_neighbour(D, direction)
	//tell our buddy its time to pass on the torch of connecting to pipes. This shouldn't ever infinitely loop since it only works on pipes that havent been inductrinated
	D.attempt_connect()

	return TRUE
///connect to a plumbing object
/obj/machinery/duct/proc/connect_plumber(datum/component/plumbing/P, direction)
	var/opposite_dir = turn(direction, 180)
	if(duct_layer != DUCT_LAYER_DEFAULT) //plumbing devices don't support multilayering. 3 is the default layer so we only use that. We can change this later
		return FALSE

	if(!P.active)
		return

	var/comp_directions = P.supply_connects + P.demand_connects //they should never, ever have supply and demand connects overlap or catastrophic failure
	if(opposite_dir & comp_directions)
		if(!duct)
			create_duct()
		if(duct.add_plumber(P, opposite_dir))
			neighbours[P.parent] = direction
			return TRUE
///we disconnect ourself from our neighbours. we also destroy our ductnet and tell our neighbours to make a new one
/obj/machinery/duct/proc/disconnect_duct()
	anchored = FALSE
	active = FALSE
	if(duct)
		duct.remove_duct(src)
	lose_neighbours()
	reset_connects(0)
	update_icon()
	if(ispath(drop_on_wrench) && !QDELING(src))
		new drop_on_wrench(drop_location())
		qdel(src)

///''''''''''''''''optimized''''''''''''''''' proc for quickly reconnecting after a duct net was destroyed
/obj/machinery/duct/proc/reconnect()
	if(neighbours.len && !duct)
		create_duct()
	for(var/atom/movable/AM in neighbours)
		if(istype(AM, /obj/machinery/duct))
			var/obj/machinery/duct/D = AM
			if(D.duct)
				if(D.duct == duct) //we're already connected
					continue
				else
					duct.assimilate(D.duct)
					continue
			else
				duct.add_duct(D)
				D.reconnect()
		else
			var/datum/component/plumbing/P = AM.GetComponent(/datum/component/plumbing)
			if(AM in get_step(src, neighbours[AM])) //did we move?
				if(P)
					connect_plumber(P, neighbours[AM])
			else
				neighbours -= AM //we moved

///Special proc to draw a new connect frame based on neighbours. not the norm so we can support multiple duct kinds
/obj/machinery/duct/proc/generate_connects()
	if(lock_connects)
		return
	connects = 0
	for(var/A in neighbours)
		connects |= neighbours[A]
	update_icon()

///create a new duct datum
/obj/machinery/duct/proc/create_duct()
	duct = new()
	duct.add_duct(src)
///add a duct as neighbour. this means we're connected and will connect again if we ever regenerate
/obj/machinery/duct/proc/add_neighbour(obj/machinery/duct/D, direction)
	if(!(D in neighbours))
		neighbours[D] = direction
	if(!(src in D.neighbours))
		D.neighbours[src] = turn(direction, 180)
///remove all our neighbours, and remove us from our neighbours aswell
/obj/machinery/duct/proc/lose_neighbours()
	for(var/obj/machinery/duct/D in neighbours)
		D.neighbours.Remove(src)
	neighbours = list()
///add a connect direction
/obj/machinery/duct/proc/add_connects(new_connects) //make this a define to cut proc calls?
	if(!lock_connects)
		connects |= new_connects
///remove our connects
/obj/machinery/duct/proc/reset_connects()
	if(!lock_connects)
		connects = 0
///get a list of the ducts we can connect to if we are dumb
/obj/machinery/duct/proc/get_adjacent_ducts()
	var/list/adjacents = list()
	for(var/A in GLOB.cardinals)
		if(A & connects)
			for(var/obj/machinery/duct/D in get_step(src, A))
				if((turn(A, 180) & D.connects) && D.active)
					adjacents += D
	return adjacents

/obj/machinery/duct/update_icon_state()
	var/temp_icon = initial(icon_state)
	for(var/D in GLOB.cardinals)
		if(D & connects)
			if(D == NORTH)
				temp_icon += "_n"
			if(D == SOUTH)
				temp_icon += "_s"
			if(D == EAST)
				temp_icon += "_e"
			if(D == WEST)
				temp_icon += "_w"
	icon_state = temp_icon

///update the layer we are on
/obj/machinery/duct/proc/handle_layer()
	var/offset
	switch(duct_layer)//it's a bitfield, but it's fine because it only works when there's one layer, and multiple layers should be handled differently
		if(FIRST_DUCT_LAYER)
			offset = -10
		if(SECOND_DUCT_LAYER)
			offset = -5
		if(THIRD_DUCT_LAYER)
			offset = 0
		if(FOURTH_DUCT_LAYER)
			offset = 5
		if(FIFTH_DUCT_LAYER)
			offset = 10
	pixel_x = offset
	pixel_y = offset


/obj/machinery/duct/wrench_act(mob/living/user, obj/item/I) //I can also be the RPD
	..()
	add_fingerprint(user)
	I.play_tool_sound(src)
	if(anchored)
		user.visible_message( \
		"[user] unfastens \the [src].", \
		"<span class='notice'>You unfasten \the [src].</span>", \
		"<span class='hear'>You hear ratcheting.</span>")
		disconnect_duct()
	else if(can_anchor())
		anchored = TRUE
		active = TRUE
		user.visible_message( \
		"[user] fastens \the [src].", \
		"<span class='notice'>You fasten \the [src].</span>", \
		"<span class='hear'>You hear ratcheting.</span>")
		attempt_connect()
	return TRUE
///collection of all the sanity checks to prevent us from stacking ducts that shouldnt be stacked
/obj/machinery/duct/proc/can_anchor(turf/T)
	if(!T)
		T = get_turf(src)
	for(var/obj/machinery/duct/D in T)
		if(!anchored)
			continue
		for(var/A in GLOB.cardinals)
			if(A & connects && A & D.connects)
				return FALSE
	return TRUE

/obj/machinery/duct/doMove(destination)
	. = ..()
	disconnect_duct()
	anchored = FALSE

/obj/machinery/duct/Destroy()
	disconnect_duct()
	return ..()

/obj/machinery/duct/MouseDrop_T(atom/A, mob/living/user)
	if(!istype(A, /obj/machinery/duct))
		return
	var/obj/machinery/duct/D = A
	var/obj/item/I = user.get_active_held_item()
	if(I?.tool_behaviour != TOOL_WRENCH)
		to_chat(user, "<span class='warning'>You need to be holding a wrench in your active hand to do that!</span>")
		return
	if(get_dist(src, D) != 1)
		return
	var/direction = get_dir(src, D)
	if(!(direction in GLOB.cardinals))
		return
	if(duct_layer != D.duct_layer)
		return

	add_connects(direction) //the connect of the other duct is handled in connect_network, but do this here for the parent duct because it's not necessary in normal cases
	add_neighbour(D, direction)
	connect_network(D, direction, TRUE)
	update_icon()
///has a total of 5 layers and doesnt give a shit about color. its also dumb so doesnt autoconnect.
/obj/machinery/duct/multilayered
	name = "duct layer-manifold"
	icon = 'icons/obj/2x2.dmi'
	icon_state = "multiduct"
	pixel_x = -15
	pixel_y = -15

	color_to_color_support = FALSE
	duct_layer = FIRST_DUCT_LAYER | SECOND_DUCT_LAYER | THIRD_DUCT_LAYER | FOURTH_DUCT_LAYER | FIFTH_DUCT_LAYER
	drop_on_wrench = null

	lock_connects = TRUE
	lock_layers = TRUE
	ignore_colors = TRUE
	dumb = TRUE

	active = FALSE
	anchored = FALSE

/obj/machinery/duct/multilayered/Initialize(mapload, no_anchor, color_of_duct, layer_of_duct = DUCT_LAYER_DEFAULT, force_connects)
	. = ..()
	update_connects()

/obj/machinery/duct/multilayered/ComponentInitialize()
	. = ..()
	AddElement(/datum/element/update_icon_blocker)

/obj/machinery/duct/multilayered/wrench_act(mob/living/user, obj/item/I)
	. = ..()
	update_connects()

/obj/machinery/duct/multilayered/proc/update_connects()
	if(dir & NORTH || dir & SOUTH)
		connects = NORTH | SOUTH
	else
		connects = EAST | WEST

///don't connect to other multilayered stuff because honestly it shouldnt be done and I dont wanna deal with it
/obj/machinery/duct/multilayered/connect_duct(obj/machinery/duct/D, direction, ignore_color)
	if(istype(D, /obj/machinery/duct/multilayered))
		return
	return ..()

/obj/machinery/duct/multilayered/handle_layer()
	return

/obj/item/stack/ducts
	name = "stack of duct"
	desc = "A stack of fluid ducts."
	singular_name = "duct"
	icon = 'icons/obj/plumbing/fluid_ducts.dmi'
	icon_state = "ducts"
	custom_materials = list(/datum/material/iron=500)
	w_class = WEIGHT_CLASS_TINY
	novariants = FALSE
	max_amount = 50
	item_flags = NOBLUDGEON
	merge_type = /obj/item/stack/ducts
	///Color of our duct
	var/duct_color = "grey"
	///Default layer of our duct
	var/duct_layer = "Default Layer"
	///Assoc index with all the available layers. yes five might be a bit much. Colors uses a global by the way
	var/list/layers = list("First Layer" = FIRST_DUCT_LAYER, "Second Layer" = SECOND_DUCT_LAYER, "Default Layer" = DUCT_LAYER_DEFAULT,
		"Fourth Layer" = FOURTH_DUCT_LAYER, "Fifth Layer" = FIFTH_DUCT_LAYER)

/obj/item/stack/ducts/examine(mob/user)
	. = ..()
	. += "<span class='notice'>It's current color and layer are [duct_color] and [duct_layer]. Use in-hand to change.</span>"

/obj/item/stack/ducts/attack_self(mob/user)
	var/new_layer = input("Select a layer", "Layer") as null|anything in layers
	if(new_layer)
		duct_layer = new_layer
	var/new_color = input("Select a color", "Color") as null|anything in GLOB.pipe_paint_colors
	if(new_color)
		duct_color = new_color
		add_atom_colour(GLOB.pipe_paint_colors[new_color], FIXED_COLOUR_PRIORITY)

/obj/item/stack/ducts/afterattack(atom/A, user, proximity)
	. = ..()
	if(!proximity)
		return
	if(istype(A, /obj/machinery/duct))
		var/obj/machinery/duct/D = A
		if(!D.anchored)
			add(1)
			qdel(D)
	if(istype(A, /turf/open) && use(1))
		var/turf/open/OT = A
		new /obj/machinery/duct(OT, FALSE, GLOB.pipe_paint_colors[duct_color], layers[duct_layer])
		playsound(get_turf(src), 'sound/machines/click.ogg', 50, TRUE)

/obj/item/stack/ducts/fifty
	amount = 50
