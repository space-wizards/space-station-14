/* * * * * * * * * * * * * * * * * * * * * * * * * *
 * /datum/recipe by rastaf0            13 apr 2011 *
 * * * * * * * * * * * * * * * * * * * * * * * * * *
 * This is powerful and flexible recipe system.
 * It exists not only for food.
 * supports both reagents and objects as prerequisites.
 * In order to use this system you have to define a deriative from /datum/recipe
 * * reagents are reagents. Acid, milc, booze, etc.
 * * items are objects. Fruits, tools, circuit boards.
 * * result is type to create as new object
 * * time is optional parameter, you shall use in in your machine,
     default /datum/recipe/ procs does not rely on this parameter.
 *
 *  Functions you need:
 *  /datum/recipe/proc/make(var/obj/container as obj)
 *    Creates result inside container,
 *    deletes prerequisite reagents,
 *    transfers reagents from prerequisite objects,
 *    deletes all prerequisite objects (even not needed for recipe at the moment).
 *
 *  /proc/select_recipe(list/datum/recipe/avaiable_recipes, obj/obj as obj, exact = 1)
 *    Wonderful function that select suitable recipe for you.
 *    obj is a machine (or magik hat) with prerequisites,
 *    exact = 0 forces algorithm to ignore superfluous stuff.
 *
 *
 *  Functions you do not need to call directly but could:
 *  /datum/recipe/proc/check_reagents(var/datum/reagents/avail_reagents)
 *    //1=precisely,  0=insufficiently, -1=superfluous
 *
 *  /datum/recipe/proc/check_items(var/obj/container as obj)
 *    //1=precisely, 0=insufficiently, -1=superfluous
 *
 * */

/datum/recipe
	var/list/reagents_list // example:  = list(/datum/reagent/consumable/berryjuice = 5) // do not list same reagent twice
	var/list/items // example: =list(/obj/item/crowbar, /obj/item/welder) // place /foo/bar before /foo
	var/result //example: = /obj/item/reagent_containers/food/snacks/donut
	var/time = 100 // 1/10 part of second


/datum/recipe/proc/check_reagents(datum/reagents/avail_reagents) //1=precisely, 0=insufficiently, -1=superfluous
	. = 1
	for (var/r_r in reagents_list)
		var/aval_r_amnt = avail_reagents.get_reagent_amount(r_r)
		if (!(abs(aval_r_amnt - reagents_list[r_r])<0.5)) //if NOT equals
			if (aval_r_amnt>reagents_list[r_r])
				. = -1
			else
				return 0
	if ((reagents_list?(reagents_list.len):(0)) < avail_reagents.reagent_list.len)
		return -1
	return .

/datum/recipe/proc/check_items(obj/container) //1=precisely, 0=insufficiently, -1=superfluous
	if (!items)
		if (locate(/obj/) in container)
			return -1
		else
			return 1
	. = 1
	var/list/checklist = items.Copy()
	for (var/obj/O in container)
		var/found = 0
		for (var/type in checklist)
			if (istype(O,type))
				checklist-=type
				found = 1
				break
		if (!found)
			. = -1
	if (checklist.len)
		return 0
	return .

//general version
/datum/recipe/proc/make(obj/container)
	var/obj/result_obj = new result(container)
	for (var/obj/O in (container.contents-result_obj))
		O.reagents.trans_to(result_obj, O.reagents.total_volume)
		qdel(O)
	container.reagents.clear_reagents()
	return result_obj

// food-related
/datum/recipe/proc/make_food(obj/container)
	var/obj/result_obj = new result(container)
	for (var/obj/O in (container.contents-result_obj))
		if (O.reagents)
			O.reagents.del_reagent(/datum/reagent/consumable/nutriment)
			O.reagents.update_total()
			O.reagents.trans_to(result_obj, O.reagents.total_volume)
		qdel(O)
	container.reagents.clear_reagents()
	return result_obj

/proc/select_recipe(list/datum/recipe/avaiable_recipes, obj/obj, exact = 1 as num)
	if (!exact)
		exact = -1
	var/list/datum/recipe/possible_recipes = new
	for (var/datum/recipe/recipe in avaiable_recipes)
		if (recipe.check_reagents(obj.reagents)==exact && recipe.check_items(obj)==exact)
			possible_recipes+=recipe
	if (possible_recipes.len==0)
		return null
	else if (possible_recipes.len==1)
		return possible_recipes[1]
	else //okay, let's select the most complicated recipe
		var/r_count = 0
		var/i_count = 0
		. = possible_recipes[1]
		for (var/datum/recipe/recipe in possible_recipes)
			var/N_i = (recipe.items)?(recipe.items.len):0
			var/N_r = (recipe.reagents_list)?(recipe.reagents_list.len):0
			if (N_i > i_count || (N_i== i_count && N_r > r_count ))
				r_count = N_r
				i_count = N_i
				. = recipe
		return .
