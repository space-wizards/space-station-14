/obj/machinery/biogenerator
	name = "biogenerator"
	desc = "Converts plants into biomass, which can be used to construct useful items."
	icon = 'icons/obj/machines/biogenerator.dmi'
	icon_state = "biogen-empty"
	density = TRUE
	use_power = IDLE_POWER_USE
	idle_power_usage = 40
	circuit = /obj/item/circuitboard/machine/biogenerator
	var/processing = FALSE
	var/obj/item/reagent_containers/glass/beaker = null
	var/points = 0
	var/menustat = "menu"
	var/efficiency = 0
	var/productivity = 0
	var/max_items = 40
	var/datum/techweb/stored_research
	var/list/show_categories = list("Food", "Botany Chemicals", "Organic Materials")
	var/list/timesFiveCategories = list("Food", "Botany Chemicals")

/obj/machinery/biogenerator/Initialize()
	. = ..()
	stored_research = new /datum/techweb/specialized/autounlocking/biogenerator
	create_reagents(1000)

/obj/machinery/biogenerator/Destroy()
	QDEL_NULL(beaker)
	return ..()

/obj/machinery/biogenerator/contents_explosion(severity, target)
	..()
	if(beaker)
		beaker.ex_act(severity, target)

/obj/machinery/biogenerator/handle_atom_del(atom/A)
	..()
	if(A == beaker)
		beaker = null
		update_icon()
		updateUsrDialog()

/obj/machinery/biogenerator/RefreshParts()
	var/E = 0
	var/P = 0
	var/max_storage = 40
	for(var/obj/item/stock_parts/matter_bin/B in component_parts)
		P += B.rating
		max_storage = 40 * B.rating
	for(var/obj/item/stock_parts/manipulator/M in component_parts)
		E += M.rating
	efficiency = E
	productivity = P
	max_items = max_storage

/obj/machinery/biogenerator/examine(mob/user)
	. = ..()
	if(in_range(user, src) || isobserver(user))
		. += "<span class='notice'>The status display reads: Productivity at <b>[productivity*100]%</b>.<br>Matter consumption reduced by <b>[(efficiency*25)-25]</b>%.<br>Machine can hold up to <b>[max_items]</b> pieces of produce.</span>"

/obj/machinery/biogenerator/on_reagent_change(changetype)			//When the reagents change, change the icon as well.
	update_icon()

/obj/machinery/biogenerator/update_icon_state()
	if(panel_open)
		icon_state = "biogen-empty-o"
	else if(!src.beaker)
		icon_state = "biogen-empty"
	else if(!src.processing)
		icon_state = "biogen-stand"
	else
		icon_state = "biogen-work"

/obj/machinery/biogenerator/attackby(obj/item/O, mob/user, params)
	if(user.a_intent == INTENT_HARM)
		return ..()

	if(processing)
		to_chat(user, "<span class='warning'>The biogenerator is currently processing.</span>")
		return

	if(default_deconstruction_screwdriver(user, "biogen-empty-o", "biogen-empty", O))
		if(beaker)
			var/obj/item/reagent_containers/glass/B = beaker
			B.forceMove(drop_location())
			beaker = null
		update_icon()
		return

	if(default_deconstruction_crowbar(O))
		return

	if(istype(O, /obj/item/reagent_containers/glass))
		. = 1 //no afterattack
		if(!panel_open)
			if(beaker)
				to_chat(user, "<span class='warning'>A container is already loaded into the machine.</span>")
			else
				if(!user.transferItemToLoc(O, src))
					return
				beaker = O
				to_chat(user, "<span class='notice'>You add the container to the machine.</span>")
				update_icon()
				updateUsrDialog()
		else
			to_chat(user, "<span class='warning'>Close the maintenance panel first.</span>")
		return

	else if(istype(O, /obj/item/storage/bag/plants))
		var/obj/item/storage/bag/plants/PB = O
		var/i = 0
		for(var/obj/item/reagent_containers/food/snacks/grown/G in contents)
			i++
		if(i >= max_items)
			to_chat(user, "<span class='warning'>The biogenerator is already full! Activate it.</span>")
		else
			for(var/obj/item/reagent_containers/food/snacks/grown/G in PB.contents)
				if(i >= max_items)
					break
				if(SEND_SIGNAL(PB, COMSIG_TRY_STORAGE_TAKE, G, src))
					i++
			if(i<max_items)
				to_chat(user, "<span class='info'>You empty the plant bag into the biogenerator.</span>")
			else if(PB.contents.len == 0)
				to_chat(user, "<span class='info'>You empty the plant bag into the biogenerator, filling it to its capacity.</span>")
			else
				to_chat(user, "<span class='info'>You fill the biogenerator to its capacity.</span>")
		return TRUE //no afterattack

	else if(istype(O, /obj/item/reagent_containers/food/snacks/grown))
		var/i = 0
		for(var/obj/item/reagent_containers/food/snacks/grown/G in contents)
			i++
		if(i >= max_items)
			to_chat(user, "<span class='warning'>The biogenerator is full! Activate it.</span>")
		else
			if(user.transferItemToLoc(O, src))
				to_chat(user, "<span class='info'>You put [O.name] in [src.name]</span>")
		return TRUE //no afterattack
	else if (istype(O, /obj/item/disk/design_disk))
		user.visible_message("<span class='notice'>[user] begins to load \the [O] in \the [src]...</span>",
			"<span class='notice'>You begin to load a design from \the [O]...</span>",
			"<span class='hear'>You hear the chatter of a floppy drive.</span>")
		processing = TRUE
		var/obj/item/disk/design_disk/D = O
		if(do_after(user, 10, target = src))
			for(var/B in D.blueprints)
				if(B)
					stored_research.add_design(B)
		processing = FALSE
		return TRUE
	else
		to_chat(user, "<span class='warning'>You cannot put this in [src.name]!</span>")

/obj/machinery/biogenerator/ui_interact(mob/user)
	if(stat & BROKEN || panel_open)
		return
	. = ..()
	var/dat
	if(processing)
		dat += "<div class='statusDisplay'>Biogenerator is processing! Please wait...</div><BR>"
	else
		switch(menustat)
			if("nopoints")
				dat += "<div class='statusDisplay'>You do not have enough biomass to create products.<BR>Please, put growns into reactor and activate it.</div>"
				menustat = "menu"
			if("complete")
				dat += "<div class='statusDisplay'>Operation complete.</div>"
				menustat = "menu"
			if("void")
				dat += "<div class='statusDisplay'>Error: No growns inside.<BR>Please, put growns into reactor.</div>"
				menustat = "menu"
			if("nobeakerspace")
				dat += "<div class='statusDisplay'>Not enough space left in container. Unable to create product.</div>"
				menustat = "menu"
		if(beaker)
			var/categories = show_categories.Copy()
			for(var/V in categories)
				categories[V] = list()
			for(var/V in stored_research.researched_designs)
				var/datum/design/D = SSresearch.techweb_design_by_id(V)
				for(var/C in categories)
					if(C in D.category)
						categories[C] += D

			dat += "<div class='statusDisplay'>Biomass: [points] units.</div><BR>"
			dat += "<A href='?src=[REF(src)];activate=1'>Activate</A><A href='?src=[REF(src)];detach=1'>Detach Container</A>"
			for(var/cat in categories)
				dat += "<h3>[cat]:</h3>"
				dat += "<div class='statusDisplay'>"
				for(var/V in categories[cat])
					var/datum/design/D = V
					dat += "[D.name]: <A href='?src=[REF(src)];create=[D.id];amount=1'>Make</A>"
					if(cat in timesFiveCategories)
						dat += "<A href='?src=[REF(src)];create=[D.id];amount=5'>x5</A>"
					if(ispath(D.build_path, /obj/item/stack))
						dat += "<A href='?src=[REF(src)];create=[D.id];amount=10'>x10</A>"
					dat += "([D.materials[getmaterialref(/datum/material/biomass)]/efficiency])<br>"
				dat += "</div>"
		else
			dat += "<div class='statusDisplay'>No container inside, please insert container.</div>"

	var/datum/browser/popup = new(user, "biogen", name, 350, 520)
	popup.set_content(dat)
	popup.open()

/obj/machinery/biogenerator/AltClick(mob/living/user)
	. = ..()
	if(istype(user) && user.canUseTopic(src, BE_CLOSE, FALSE, NO_TK))
		detach(user)

/obj/machinery/biogenerator/proc/activate()
	if (usr.stat != CONSCIOUS)
		return
	if (src.stat != NONE) //NOPOWER etc
		return
	if(processing)
		to_chat(usr, "<span class='warning'>The biogenerator is in the process of working.</span>")
		return
	var/S = 0
	for(var/obj/item/reagent_containers/food/snacks/grown/I in contents)
		S += 5
		if(I.reagents.get_reagent_amount(/datum/reagent/consumable/nutriment) < 0.1)
			points += 1*productivity
		else points += I.reagents.get_reagent_amount(/datum/reagent/consumable/nutriment)*10*productivity
		qdel(I)
	if(S)
		processing = TRUE
		update_icon()
		updateUsrDialog()
		playsound(src.loc, 'sound/machines/blender.ogg', 50, TRUE)
		use_power(S*30)
		sleep(S+15/productivity)
		processing = FALSE
		update_icon()
	else
		menustat = "void"

/obj/machinery/biogenerator/proc/check_cost(list/materials, multiplier = 1, remove_points = TRUE)
	if(materials.len != 1 || materials[1] != getmaterialref(/datum/material/biomass))
		return FALSE
	if (materials[getmaterialref(/datum/material/biomass)]*multiplier/efficiency > points)
		menustat = "nopoints"
		return FALSE
	else
		if(remove_points)
			points -= materials[getmaterialref(/datum/material/biomass)]*multiplier/efficiency
		update_icon()
		updateUsrDialog()
		return TRUE

/obj/machinery/biogenerator/proc/check_container_volume(list/reagents, multiplier = 1)
	var/sum_reagents = 0
	for(var/R in reagents)
		sum_reagents += reagents[R]
	sum_reagents *= multiplier

	if(beaker.reagents.total_volume + sum_reagents > beaker.reagents.maximum_volume)
		menustat = "nobeakerspace"
		return FALSE

	return TRUE

/obj/machinery/biogenerator/proc/create_product(datum/design/D, amount)
	if(!beaker || !loc)
		return FALSE

	if(ispath(D.build_path, /obj/item/stack))
		if(!check_container_volume(D.make_reagents, amount))
			return FALSE
		if(!check_cost(D.materials, amount))
			return FALSE

		new D.build_path(drop_location(), amount)
		for(var/R in D.make_reagents)
			beaker.reagents.add_reagent(R, D.make_reagents[R]*amount)
	else
		var/i = amount
		while(i > 0)
			if(!check_container_volume(D.make_reagents))
				return .
			if(!check_cost(D.materials))
				return .
			if(D.build_path)
				new D.build_path(loc)
			for(var/R in D.make_reagents)
				beaker.reagents.add_reagent(R, D.make_reagents[R])
			. = 1
			--i

	menustat = "complete"
	update_icon()
	return .

/obj/machinery/biogenerator/proc/detach(mob/living/user)
	if(beaker)
		user.put_in_hands(beaker)
		beaker = null
		update_icon()

/obj/machinery/biogenerator/Topic(href, href_list)
	if(..() || panel_open)
		return

	usr.set_machine(src)

	if(href_list["activate"])
		activate()
		updateUsrDialog()

	else if(href_list["detach"])
		detach(usr)
		updateUsrDialog()

	else if(href_list["create"])
		var/amount = (text2num(href_list["amount"]))
		//Can't be outside these (if you change this keep a sane limit)
		amount = CLAMP(amount, 1, 10)
		var/id = href_list["create"]
		if(!stored_research.researched_designs.Find(id))
			//naughty naughty
			stack_trace("ID did not map to a researched datum [id]")
			return

		//Get design by id (or may return error design)
		var/datum/design/D = SSresearch.techweb_design_by_id(id)
		//Valid design datum, amount and the datum is not the error design, lets proceed
		if(D && amount && !istype(D, /datum/design/error_design))
			create_product(D, amount)
		//This shouldnt happen normally but href forgery is real
		else
			stack_trace("ID could not be turned into a valid techweb design datum [id]")
		updateUsrDialog()

	else if(href_list["menu"])
		menustat = "menu"
		updateUsrDialog()
