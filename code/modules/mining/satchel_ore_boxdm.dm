
/**********************Ore box**************************/

/obj/structure/ore_box
	icon = 'icons/obj/mining.dmi'
	icon_state = "orebox"
	name = "ore box"
	desc = "A heavy wooden box, which can be filled with a lot of ores."
	density = TRUE
	pressure_resistance = 5*ONE_ATMOSPHERE

	var/ui_x = 335
	var/ui_y = 415

/obj/structure/ore_box/attackby(obj/item/W, mob/user, params)
	if (istype(W, /obj/item/stack/ore))
		user.transferItemToLoc(W, src)
	else if(SEND_SIGNAL(W, COMSIG_CONTAINS_STORAGE))
		SEND_SIGNAL(W, COMSIG_TRY_STORAGE_TAKE_TYPE, /obj/item/stack/ore, src)
		to_chat(user, "<span class='notice'>You empty the ore in [W] into \the [src].</span>")
	else
		return ..()

/obj/structure/ore_box/ComponentInitialize()
	. = ..()
	AddComponent(/datum/component/rad_insulation, 0.01) //please datum mats no more cancer

/obj/structure/ore_box/crowbar_act(mob/living/user, obj/item/I)
	if(I.use_tool(src, user, 50, volume=50))
		user.visible_message("<span class='notice'>[user] pries \the [src] apart.</span>",
			"<span class='notice'>You pry apart \the [src].</span>",
			"<span class='hear'>You hear splitting wood.</span>")
		deconstruct(TRUE, user)
	return TRUE

/obj/structure/ore_box/examine(mob/living/user)
	if(Adjacent(user) && istype(user))
		ui_interact(user)
	. = ..()

/obj/structure/ore_box/attack_hand(mob/user)
	. = ..()
	if(.)
		return
	if(Adjacent(user))
		ui_interact(user)

/obj/structure/ore_box/attack_robot(mob/user)
	if(Adjacent(user))
		ui_interact(user)

/obj/structure/ore_box/proc/dump_box_contents()
	var/drop = drop_location()
	for(var/obj/item/stack/ore/O in src)
		if(QDELETED(O))
			continue
		if(QDELETED(src))
			break
		O.forceMove(drop)
		if(TICK_CHECK)
			stoplag()
			drop = drop_location()

/obj/structure/ore_box/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, \
									datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "ore_box", name, ui_x, ui_y, master_ui, state)
		ui.open()

/obj/structure/ore_box/ui_data()
	var/contents = list()
	for(var/obj/item/stack/ore/O in src)
		contents[O.type] += O.amount

	var/data = list()
	data["materials"] = list()
	for(var/type in contents)
		var/obj/item/stack/ore/O = type
		var/name = initial(O.name)
		data["materials"] += list(list("name" = name, "amount" = contents[type], "id" = type))

	return data

/obj/structure/ore_box/ui_act(action, params)
	if(..())
		return
	if(!Adjacent(usr))
		return
	add_fingerprint(usr)
	usr.set_machine(src)
	switch(action)
		if("removeall")
			dump_box_contents()
			to_chat(usr, "<span class='notice'>You open the release hatch on the box..</span>")

/obj/structure/ore_box/deconstruct(disassembled = TRUE, mob/user)
	var/obj/item/stack/sheet/mineral/wood/WD = new (loc, 4)
	if(user)
		WD.add_fingerprint(user)
	dump_box_contents()
	qdel(src)

/obj/structure/ore_box/onTransitZ()
	return
