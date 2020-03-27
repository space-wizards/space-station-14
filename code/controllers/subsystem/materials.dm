/*! How material datums work
Materials are now instanced datums, with an associative list of them being kept in SSmaterials. We only instance the materials once and then re-use these instances for everything.

These materials call on_applied() on whatever item they are applied to, common effects are adding components, changing color and changing description. This allows us to differentiate items based on the material they are made out of.area

*/

SUBSYSTEM_DEF(materials)
	name = "Materials"
	flags = SS_NO_FIRE
	init_order = INIT_ORDER_MATERIALS
	///Dictionary of material.type || material ref
	var/list/materials = list() 
	///Dictionary of category || list of material refs
	var/list/materials_by_category = list() 
	///List of stackcrafting recipes for materials using rigid materials
	var/list/rigid_stack_recipes = list(
		new /datum/stack_recipe("chair", /obj/structure/chair/greyscale, one_per_turf = TRUE, on_floor = TRUE, applies_mats = TRUE),
		new /datum/stack_recipe("toilet", /obj/structure/toilet/greyscale, one_per_turf = TRUE, on_floor = TRUE, applies_mats = TRUE),
		new /datum/stack_recipe("sink", /obj/structure/sink/greyscale, one_per_turf = TRUE, on_floor = TRUE, applies_mats = TRUE),
	)

/datum/controller/subsystem/materials/Initialize(timeofday)
	InitializeMaterials()
	return ..()

///Ran on initialize, populated the materials and materials_by_category dictionaries with their appropiate vars (See these variables for more info)
/datum/controller/subsystem/materials/proc/InitializeMaterials(timeofday)
	for(var/type in subtypesof(/datum/material))
		var/datum/material/ref = new type
		materials[type] = ref
		for(var/c in ref.categories)
			materials_by_category[c] += list(ref)
