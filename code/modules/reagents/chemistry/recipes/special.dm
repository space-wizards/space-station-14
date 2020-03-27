GLOBAL_LIST_INIT(food_reagents, build_reagents_to_food()) //reagentid = related food types

/proc/build_reagents_to_food()
	. = list()
	for (var/type in subtypesof(/obj/item/reagent_containers/food))
		var/obj/item/reagent_containers/food/item = new type()
		for(var/r in item.list_reagents)
			if (!.[r])
				.[r] = list()
			.[r] += type
		qdel(item)
	//dang plant snowflake
	for (var/type in subtypesof(/obj/item/seeds))
		var/obj/item/seeds/item = new type()
		for(var/r in item.reagents_add)
			if (!.[r])
				.[r] = list()
			.[r] += type
		qdel(item)


#define RNGCHEM_INPUT "input"
#define RNGCHEM_CATALYSTS "catalysts"
#define RNGCHEM_OUTPUT "output"

/datum/chemical_reaction/randomized
	name = "semi randomized reaction"

	var/persistent = FALSE
	var/persistence_period = 7 //Will reset every x days
	var/created //creation timestamp

	var/randomize_container = FALSE
	var/list/possible_containers = list()

	var/randomize_req_temperature = TRUE
	var/min_temp = 1
	var/max_temp = 600

	var/randomize_inputs = TRUE
	var/min_input_reagent_amount = 1
	var/max_input_reagent_amount = 10
	var/min_input_reagents = 2
	var/max_input_reagents = 5
	var/list/possible_reagents = list()
	var/min_catalysts = 0
	var/max_catalysts = 2
	var/list/possible_catalysts = list()

	var/randomize_results = FALSE
	var/min_output_reagent_amount = 1
	var/max_output_reagent_amount = 5
	var/min_result_reagents = 1
	var/max_result_reagents = 1
	var/list/possible_results = list()

/datum/chemical_reaction/randomized/proc/GenerateRecipe()
	created = world.time
	if(randomize_container)
		required_container = pick(possible_containers)
	if(randomize_req_temperature)
		required_temp = rand(min_temp,max_temp)
		is_cold_recipe = pick(TRUE,FALSE)

	if(randomize_results)
		results = list()
		var/list/remaining_possible_results = GetPossibleReagents(RNGCHEM_OUTPUT)
		var/out_reagent_count = min(rand(min_result_reagents,max_result_reagents),remaining_possible_results.len)
		for(var/i in 1 to out_reagent_count)
			var/r_id = pick_n_take(remaining_possible_results)
			results[r_id] = rand(min_output_reagent_amount,max_output_reagent_amount)

	if(randomize_inputs)
		var/list/remaining_possible_reagents = GetPossibleReagents(RNGCHEM_INPUT)
		var/list/remaining_possible_catalysts = GetPossibleReagents(RNGCHEM_CATALYSTS)
		//We're going to assume we're not doing any weird partial reactions for now.
		for(var/reagent_type in results)
			remaining_possible_catalysts -= reagent_type
			remaining_possible_reagents -= reagent_type

		var/in_reagent_count = min(rand(min_input_reagents,max_input_reagents),remaining_possible_reagents.len)
		if(in_reagent_count <= 0)
			return FALSE

		required_reagents = list()
		for(var/i in 1 to in_reagent_count)
			var/r_id = pick_n_take(remaining_possible_reagents)
			required_reagents[r_id] = rand(min_input_reagent_amount,max_input_reagent_amount)
			remaining_possible_catalysts -= r_id //Can't have same reagents both as catalyst and reagent. Or can we ?

		required_catalysts = list()
		var/in_catalyst_count = min(rand(min_catalysts,max_catalysts),remaining_possible_catalysts.len)
		for(var/i in 1 to in_catalyst_count)
			var/r_id = pick_n_take(remaining_possible_catalysts)
			required_catalysts[r_id] = rand(min_input_reagent_amount,max_input_reagent_amount)

	return TRUE

/datum/chemical_reaction/randomized/proc/GetPossibleReagents(kind)
	switch(kind)
		if(RNGCHEM_INPUT)
			return possible_reagents.Copy()
		if(RNGCHEM_CATALYSTS)
			return possible_catalysts.Copy()
		if(RNGCHEM_OUTPUT)
			return possible_results.Copy()

/datum/chemical_reaction/randomized/proc/HasConflicts()
	for(var/x in required_reagents)
		for(var/datum/chemical_reaction/R in GLOB.chemical_reactions_list[x])
			if(chem_recipes_do_conflict(R,src))
				return TRUE
	return FALSE

/datum/chemical_reaction/randomized/proc/unwrap_reagent_list(list/textreagents)
	. = list()
	for(var/R in textreagents)
		var/pathR = text2path(R)
		if(!pathR)
			return null
		.[pathR] = textreagents[R]

/datum/chemical_reaction/randomized/proc/LoadOldRecipe(recipe_data)
	created = text2num(recipe_data["timestamp"])

	var/req_reag = unwrap_reagent_list(recipe_data["required_reagents"])
	if(!req_reag)
		return FALSE
	required_reagents = req_reag

	var/req_catalysts = unwrap_reagent_list(recipe_data["required_catalysts"])
	if(!req_catalysts)
		return FALSE
	required_catalysts = req_catalysts

	required_temp = recipe_data["required_temp"]
	is_cold_recipe = recipe_data["is_cold_recipe"]

	var/temp_results = unwrap_reagent_list(recipe_data["results"])
	if(!temp_results)
		return FALSE
	results = temp_results
	var/containerpath = text2path(recipe_data["required_container"])
	if(!containerpath)
		return FALSE
	required_container =  containerpath
	return TRUE

/datum/chemical_reaction/randomized/secret_sauce
	name = "secret sauce creation"
	id = "secretsauce"
	persistent = TRUE
	persistence_period = 7 //Reset every week
	randomize_container = TRUE
	possible_containers = list(/obj/item/reagent_containers/glass/bucket) //easy way to ensure no common conflicts
	randomize_req_temperature = TRUE
	results = list(/datum/reagent/consumable/secretsauce=1)

/datum/chemical_reaction/randomized/secret_sauce/GetPossibleReagents(kind)
	switch(kind)
		if(RNGCHEM_INPUT,RNGCHEM_CATALYSTS)
			var/food_reagent_ids = list()
			for(var/key in GLOB.food_reagents)
				food_reagent_ids += key
			return food_reagent_ids
	return ..()


/obj/item/paper/secretrecipe
	name = "old recipe"
	var/recipe_id = "secretsauce"

/obj/item/paper/secretrecipe/examine(mob/user) //Extra secret
	if(isobserver(user))
		return list()
	. = ..()

/obj/item/paper/secretrecipe/Initialize()
	. = ..()
	if(SSpersistence.initialized)
		UpdateInfo()
	else
		SSticker.OnRoundstart(CALLBACK(src,.proc/UpdateInfo))

/obj/item/paper/secretrecipe/proc/UpdateInfo()
	var/datum/chemical_reaction/recipe = get_chemical_reaction(recipe_id)
	if(!recipe)
		info = "This recipe is illegible."
	var/list/dat = list("<ul>")
	for(var/rid in recipe.required_reagents)
		var/datum/reagent/R = GLOB.chemical_reagents_list[rid]
		dat += "<li>[recipe.required_reagents[rid]]u of [R.name]</li>"
	dat += "</ul>"
	if(recipe.required_catalysts.len)
		dat += "With following present: <ul>"
		for(var/rid in recipe.required_catalysts)
			var/datum/reagent/R = GLOB.chemical_reagents_list[rid]
			dat += "<li>[recipe.required_catalysts[rid]]u of [R.name]</li>"
		dat += "</ul>"
	dat += "Mix slowly"
	if(recipe.required_container)
		var/obj/item/I = recipe.required_container
		dat += " in [initial(I.name)]"
	if(recipe.required_temp != 0)
		if(recipe.is_cold_recipe)
			dat += " below [recipe.required_temp] degrees"
		else
			dat += " above [recipe.required_temp] degrees"
	dat += "."
	info = dat.Join("")
	update_icon()
