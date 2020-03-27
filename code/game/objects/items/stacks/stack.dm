/* Stack type objects!
 * Contains:
 * 		Stacks
 * 		Recipe datum
 * 		Recipe list datum
 */

/*
 * Stacks
 */


/obj/item/stack
	icon = 'icons/obj/stack_objects.dmi'
	gender = PLURAL
	material_modifier = 0.01
	var/list/datum/stack_recipe/recipes
	var/singular_name
	var/amount = 1
	var/max_amount = 50 //also see stack recipes initialisation, param "max_res_amount" must be equal to this max_amount
	var/is_cyborg = 0 // It's 1 if module is used by a cyborg, and uses its storage
	var/datum/robot_energy_storage/source
	var/cost = 1 // How much energy from storage it costs
	var/merge_type = null // This path and its children should merge with this stack, defaults to src.type
	var/full_w_class = WEIGHT_CLASS_NORMAL //The weight class the stack should have at amount > 2/3rds max_amount
	var/novariants = TRUE //Determines whether the item should update it's sprites based on amount.
	var/list/mats_per_unit //list that tells you how much is in a single unit.
	///Datum material type that this stack is made of
	var/material_type
	//NOTE: When adding grind_results, the amounts should be for an INDIVIDUAL ITEM - these amounts will be multiplied by the stack size in on_grind()
	var/obj/structure/table/tableVariant // we tables now (stores table variant to be built from this stack)

/obj/item/stack/on_grind()
	for(var/i in 1 to grind_results.len) //This should only call if it's ground, so no need to check if grind_results exists
		grind_results[grind_results[i]] *= get_amount() //Gets the key at position i, then the reagent amount of that key, then multiplies it by stack size

/obj/item/stack/grind_requirements()
	if(is_cyborg)
		to_chat(usr, "<span class='warning'>[src] is electronically synthesized in your chassis and can't be ground up!</span>")
		return
	return TRUE

/obj/item/stack/Initialize(mapload, new_amount, merge = TRUE)
	if(new_amount != null)
		amount = new_amount
	while(amount > max_amount)
		amount -= max_amount
		new type(loc, max_amount, FALSE)
	if(!merge_type)
		merge_type = type
	if(custom_materials && custom_materials.len)
		mats_per_unit = list()
		var/in_process_mat_list = custom_materials.Copy()
		for(var/i in custom_materials)
			mats_per_unit[getmaterialref(i)] = in_process_mat_list[i]
			custom_materials[i] *= amount
	. = ..()
	if(merge)
		for(var/obj/item/stack/S in loc)
			if(S.merge_type == merge_type)
				merge(S)
	var/list/temp_recipes = get_main_recipes()
	recipes = temp_recipes.Copy()
	if(material_type)
		var/datum/material/M = getmaterialref(material_type) //First/main material
		for(var/i in M.categories)
			switch(i)
				if(MAT_CATEGORY_RIGID)
					var/list/temp = SSmaterials.rigid_stack_recipes.Copy()
					recipes += temp
	update_weight()
	update_icon()

/obj/item/stack/proc/get_main_recipes()
	SHOULD_CALL_PARENT(1)
	return list()//empty list

/obj/item/stack/proc/update_weight()
	if(amount <= (max_amount * (1/3)))
		w_class = CLAMP(full_w_class-2, WEIGHT_CLASS_TINY, full_w_class)
	else if (amount <= (max_amount * (2/3)))
		w_class = CLAMP(full_w_class-1, WEIGHT_CLASS_TINY, full_w_class)
	else
		w_class = full_w_class


/obj/item/stack/update_icon_state()
	if(novariants)
		return
	if(amount <= (max_amount * (1/3)))
		icon_state = initial(icon_state)
	else if (amount <= (max_amount * (2/3)))
		icon_state = "[initial(icon_state)]_2"
	else
		icon_state = "[initial(icon_state)]_3"


/obj/item/stack/Destroy()
	if (usr && usr.machine==src)
		usr << browse(null, "window=stack")
	. = ..()

/obj/item/stack/examine(mob/user)
	. = ..()
	if (is_cyborg)
		if(singular_name)
			. += "There is enough energy for [get_amount()] [singular_name]\s."
		else
			. += "There is enough energy for [get_amount()]."
		return
	if(singular_name)
		if(get_amount()>1)
			. += "There are [get_amount()] [singular_name]\s in the stack."
		else
			. += "There is [get_amount()] [singular_name] in the stack."
	else if(get_amount()>1)
		. += "There are [get_amount()] in the stack."
	else
		. += "There is [get_amount()] in the stack."
	. += "<span class='notice'>Alt-click to take a custom amount.</span>"

/obj/item/stack/proc/get_amount()
	if(is_cyborg)
		. = round(source.energy / cost)
	else
		. = (amount)

/obj/item/stack/attack_self(mob/user)
	interact(user)

/obj/item/stack/interact(mob/user, sublist)
	ui_interact(user, sublist)

/obj/item/stack/ui_interact(mob/user, recipes_sublist)
	. = ..()
	if (!recipes)
		return
	if (!src || get_amount() <= 0)
		user << browse(null, "window=stack")
	user.set_machine(src) //for correct work of onclose
	var/list/recipe_list = recipes
	if (recipes_sublist && recipe_list[recipes_sublist] && istype(recipe_list[recipes_sublist], /datum/stack_recipe_list))
		var/datum/stack_recipe_list/srl = recipe_list[recipes_sublist]
		recipe_list = srl.recipes
	var/t1 = "Amount Left: [get_amount()]<br>"
	for(var/i in 1 to length(recipe_list))
		var/E = recipe_list[i]
		if (isnull(E))
			t1 += "<hr>"
			continue
		if (i>1 && !isnull(recipe_list[i-1]))
			t1+="<br>"

		if (istype(E, /datum/stack_recipe_list))
			var/datum/stack_recipe_list/srl = E
			t1 += "<a href='?src=[REF(src)];sublist=[i]'>[srl.title]</a>"

		if (istype(E, /datum/stack_recipe))
			var/datum/stack_recipe/R = E
			var/max_multiplier = round(get_amount() / R.req_amount)
			var/title
			var/can_build = 1
			can_build = can_build && (max_multiplier>0)

			if (R.res_amount>1)
				title+= "[R.res_amount]x [R.title]\s"
			else
				title+= "[R.title]"
			title+= " ([R.req_amount] [singular_name]\s)"
			if (can_build)
				t1 += text("<A href='?src=[REF(src)];sublist=[recipes_sublist];make=[i];multiplier=1'>[title]</A>  ")
			else
				t1 += text("[]", title)
				continue
			if (R.max_res_amount>1 && max_multiplier>1)
				max_multiplier = min(max_multiplier, round(R.max_res_amount/R.res_amount))
				t1 += " |"
				var/list/multipliers = list(5,10,25)
				for (var/n in multipliers)
					if (max_multiplier>=n)
						t1 += " <A href='?src=[REF(src)];make=[i];multiplier=[n]'>[n*R.res_amount]x</A>"
				if (!(max_multiplier in multipliers))
					t1 += " <A href='?src=[REF(src)];make=[i];multiplier=[max_multiplier]'>[max_multiplier*R.res_amount]x</A>"

	var/datum/browser/popup = new(user, "stack", name, 400, 400)
	popup.set_content(t1)
	popup.open(FALSE)
	onclose(user, "stack")

/obj/item/stack/Topic(href, href_list)
	..()
	if (usr.restrained() || usr.stat || usr.get_active_held_item() != src)
		return
	if (href_list["sublist"] && !href_list["make"])
		interact(usr, text2num(href_list["sublist"]))
	if (href_list["make"])
		if (get_amount() < 1 && !is_cyborg)
			qdel(src)

		var/list/recipes_list = recipes
		if (href_list["sublist"])
			var/datum/stack_recipe_list/srl = recipes_list[text2num(href_list["sublist"])]
			recipes_list = srl.recipes
		var/datum/stack_recipe/R = recipes_list[text2num(href_list["make"])]
		var/multiplier = text2num(href_list["multiplier"])
		if (!multiplier ||(multiplier <= 0)) //href protection
			return
		if(!building_checks(R, multiplier))
			return
		if (R.time)
			var/adjusted_time = 0
			usr.visible_message("<span class='notice'>[usr] starts building \a [R.title].</span>", "<span class='notice'>You start building \a [R.title]...</span>")
			if(HAS_TRAIT(usr, R.trait_booster))
				adjusted_time = (R.time * R.trait_modifier)
			else
				adjusted_time = R.time
			if (!do_after(usr, adjusted_time, target = usr))
				return
			if(!building_checks(R, multiplier))
				return

		var/obj/O
		if(R.max_res_amount > 1) //Is it a stack?
			O = new R.result_type(usr.drop_location(), R.res_amount * multiplier)
		else if(ispath(R.result_type, /turf))
			var/turf/T = usr.drop_location()
			if(!isturf(T))
				return
			T.PlaceOnTop(R.result_type, flags = CHANGETURF_INHERIT_AIR)
		else
			O = new R.result_type(usr.drop_location())
		if(O)
			O.setDir(usr.dir)
		use(R.req_amount * multiplier)

		if(R.applies_mats && custom_materials && custom_materials.len)
			var/list/used_materials = list()
			for(var/i in custom_materials)
				used_materials[getmaterialref(i)] = R.req_amount / R.res_amount * (MINERAL_MATERIAL_AMOUNT / custom_materials.len)
			O.set_custom_materials(used_materials)

		//START: oh fuck i'm so sorry
		if(istype(O, /obj/structure/windoor_assembly))
			var/obj/structure/windoor_assembly/W = O
			W.ini_dir = W.dir
		else if(istype(O, /obj/structure/window))
			var/obj/structure/window/W = O
			W.ini_dir = W.dir
		//END: oh fuck i'm so sorry

		else if(istype(O, /obj/item/restraints/handcuffs/cable))
			var/obj/item/cuffs = O
			cuffs.color = color

		if (QDELETED(O))
			return //It's a stack and has already been merged

		if (isitem(O))
			usr.put_in_hands(O)
		O.add_fingerprint(usr)

		//BubbleWrap - so newly formed boxes are empty
		if ( istype(O, /obj/item/storage) )
			for (var/obj/item/I in O)
				qdel(I)
		//BubbleWrap END

/obj/item/stack/proc/building_checks(datum/stack_recipe/R, multiplier)
	if (get_amount() < R.req_amount*multiplier)
		if (R.req_amount*multiplier>1)
			to_chat(usr, "<span class='warning'>You haven't got enough [src] to build \the [R.req_amount*multiplier] [R.title]\s!</span>")
		else
			to_chat(usr, "<span class='warning'>You haven't got enough [src] to build \the [R.title]!</span>")
		return FALSE
	var/turf/T = get_turf(usr)

	var/obj/D = R.result_type
	if(R.window_checks && !valid_window_location(T, initial(D.dir) == FULLTILE_WINDOW_DIR ? FULLTILE_WINDOW_DIR : usr.dir))
		to_chat(usr, "<span class='warning'>The [R.title] won't fit here!</span>")
		return FALSE
	if(R.one_per_turf && (locate(R.result_type) in T))
		to_chat(usr, "<span class='warning'>There is another [R.title] here!</span>")
		return FALSE
	if(R.on_floor)
		if(!isfloorturf(T))
			to_chat(usr, "<span class='warning'>\The [R.title] must be constructed on the floor!</span>")
			return FALSE
		for(var/obj/AM in T)
			if(istype(AM,/obj/structure/grille))
				continue
			if(istype(AM,/obj/structure/table))
				continue
			if(istype(AM,/obj/structure/window))
				var/obj/structure/window/W = AM
				if(!W.fulltile)
					continue
			if(AM.density)
				to_chat(usr, "<span class='warning'>Theres a [AM.name] here. You cant make a [R.title] here!</span>")
				return FALSE
	if(R.placement_checks)
		switch(R.placement_checks)
			if(STACK_CHECK_CARDINALS)
				var/turf/step
				for(var/direction in GLOB.cardinals)
					step = get_step(T, direction)
					if(locate(R.result_type) in step)
						to_chat(usr, "<span class='warning'>\The [R.title] must not be built directly adjacent to another!</span>")
						return FALSE
			if(STACK_CHECK_ADJACENT)
				if(locate(R.result_type) in range(1, T))
					to_chat(usr, "<span class='warning'>\The [R.title] must be constructed at least one tile away from others of its type!</span>")
					return FALSE
	return TRUE

/obj/item/stack/use(used, transfer = FALSE, check = TRUE) // return 0 = borked; return 1 = had enough
	if(check && zero_amount())
		return FALSE
	if (is_cyborg)
		return source.use_charge(used * cost)
	if (amount < used)
		return FALSE
	amount -= used
	if(check)
		zero_amount()
	for(var/i in mats_per_unit)
		custom_materials[i] = amount * mats_per_unit[i]
	update_icon()
	update_weight()
	return TRUE

/obj/item/stack/tool_use_check(mob/living/user, amount)
	if(get_amount() < amount)
		if(singular_name)
			if(amount > 1)
				to_chat(user, "<span class='warning'>You need at least [amount] [singular_name]\s to do this!</span>")
			else
				to_chat(user, "<span class='warning'>You need at least [amount] [singular_name] to do this!</span>")
		else
			to_chat(user, "<span class='warning'>You need at least [amount] to do this!</span>")

		return FALSE

	return TRUE

/obj/item/stack/proc/zero_amount()
	if(is_cyborg)
		return source.energy < cost
	if(amount < 1)
		qdel(src)
		return 1
	return 0

/obj/item/stack/proc/add(amount)
	if (is_cyborg)
		source.add_charge(amount * cost)
	else
		src.amount += amount
	if(mats_per_unit && mats_per_unit.len)
		for(var/i in mats_per_unit)
			custom_materials[i] = mats_per_unit[i] * src.amount
		set_custom_materials() //Refresh
	update_icon()
	update_weight()

/obj/item/stack/proc/merge(obj/item/stack/S) //Merge src into S, as much as possible
	if(QDELETED(S) || QDELETED(src) || S == src) //amusingly this can cause a stack to consume itself, let's not allow that.
		return
	var/transfer = get_amount()
	if(S.is_cyborg)
		transfer = min(transfer, round((S.source.max_energy - S.source.energy) / S.cost))
	else
		transfer = min(transfer, S.max_amount - S.amount)
	if(pulledby)
		pulledby.start_pulling(S)
	S.copy_evidences(src)
	use(transfer, TRUE)
	S.add(transfer)
	return transfer

/obj/item/stack/Crossed(obj/o)
	if(istype(o, merge_type) && !o.throwing)
		merge(o)
	. = ..()

/obj/item/stack/hitby(atom/movable/AM, skipcatch, hitpush, blocked, datum/thrownthing/throwingdatum)
	if(istype(AM, merge_type))
		merge(AM)
	. = ..()

//ATTACK HAND IGNORING PARENT RETURN VALUE
/obj/item/stack/attack_hand(mob/user)
	if(user.get_inactive_held_item() == src)
		if(zero_amount())
			return
		return change_stack(user,1)
	else
		. = ..()

/obj/item/stack/AltClick(mob/living/user)
	. = ..()
	if(isturf(loc)) // to prevent people that are alt clicking a tile to see its content from getting undesidered pop ups
		return
	if(!istype(user) || !user.canUseTopic(src, BE_CLOSE, ismonkey(user)))
		return
	if(is_cyborg)
		return
	else
		if(zero_amount())
			return
		//get amount from user
		var/max = get_amount()
		var/stackmaterial = round(input(user,"How many sheets do you wish to take out of this stack? (Maximum  [max])") as null|num)
		max = get_amount()
		stackmaterial = min(max, stackmaterial)
		if(stackmaterial == null || stackmaterial <= 0 || !user.canUseTopic(src, BE_CLOSE, ismonkey(user)))
			return
		else
			change_stack(user, stackmaterial)
			to_chat(user, "<span class='notice'>You take [stackmaterial] sheets out of the stack.</span>")

/obj/item/stack/proc/change_stack(mob/user, amount)
	if(!use(amount, TRUE, FALSE))
		return FALSE
	var/obj/item/stack/F = new type(user? user : drop_location(), amount, FALSE)
	. = F
	F.copy_evidences(src)
	if(user)
		if(!user.put_in_hands(F, merge_stacks = FALSE))
			F.forceMove(user.drop_location())
		add_fingerprint(user)
		F.add_fingerprint(user)
	zero_amount()

/obj/item/stack/attackby(obj/item/W, mob/user, params)
	if(istype(W, merge_type))
		var/obj/item/stack/S = W
		if(merge(S))
			to_chat(user, "<span class='notice'>Your [S.name] stack now contains [S.get_amount()] [S.singular_name]\s.</span>")
	else
		. = ..()

/obj/item/stack/proc/copy_evidences(obj/item/stack/from)
	add_blood_DNA(from.return_blood_DNA())
	add_fingerprint_list(from.return_fingerprints())
	add_hiddenprint_list(from.return_hiddenprints())
	fingerprintslast  = from.fingerprintslast
	//TODO bloody overlay

/obj/item/stack/microwave_act(obj/machinery/microwave/M)
	if(istype(M) && M.dirty < 100)
		M.dirty += amount

/*
 * Recipe datum
 */
/datum/stack_recipe
	var/title = "ERROR"
	var/result_type
	var/req_amount = 1
	var/res_amount = 1
	var/max_res_amount = 1
	var/time = 0
	var/one_per_turf = FALSE
	var/on_floor = FALSE
	var/window_checks = FALSE
	var/placement_checks = FALSE
	var/applies_mats = FALSE
	var/trait_booster = null
	var/trait_modifier = 1

/datum/stack_recipe/New(title, result_type, req_amount = 1, res_amount = 1, max_res_amount = 1,time = 0, one_per_turf = FALSE, on_floor = FALSE, window_checks = FALSE, placement_checks = FALSE, applies_mats = FALSE, trait_booster = null, trait_modifier = 1)


	src.title = title
	src.result_type = result_type
	src.req_amount = req_amount
	src.res_amount = res_amount
	src.max_res_amount = max_res_amount
	src.time = time
	src.one_per_turf = one_per_turf
	src.on_floor = on_floor
	src.window_checks = window_checks
	src.placement_checks = placement_checks
	src.applies_mats = applies_mats
	src.trait_booster = trait_booster
	src.trait_modifier = trait_modifier
/*
 * Recipe list datum
 */
/datum/stack_recipe_list
	var/title = "ERROR"
	var/list/recipes

/datum/stack_recipe_list/New(title, recipes)
	src.title = title
	src.recipes = recipes
