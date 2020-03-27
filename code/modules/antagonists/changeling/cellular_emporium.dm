// cellular emporium
// The place where changelings go to buy their biological weaponry.

/datum/cellular_emporium
	var/name = "cellular emporium"
	var/datum/antagonist/changeling/changeling

/datum/cellular_emporium/New(my_changeling)
	. = ..()
	changeling = my_changeling

/datum/cellular_emporium/Destroy()
	changeling = null
	. = ..()

/datum/cellular_emporium/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.always_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "cellular_emporium", name, 900, 480, master_ui, state)
		ui.open()

/datum/cellular_emporium/ui_data(mob/user)
	var/list/data = list()

	var/can_readapt = changeling.canrespec
	var/genetic_points_remaining = changeling.geneticpoints
	var/absorbed_dna_count = changeling.absorbedcount
	var/true_absorbs = changeling.trueabsorbs

	data["can_readapt"] = can_readapt
	data["genetic_points_remaining"] = genetic_points_remaining
	data["absorbed_dna_count"] = absorbed_dna_count

	var/list/abilities = list()

	for(var/path in changeling.all_powers)
		var/datum/action/changeling/ability = path

		var/dna_cost = initial(ability.dna_cost)
		if(dna_cost <= 0)
			continue

		var/list/AL = list()
		AL["name"] = initial(ability.name)
		AL["desc"] = initial(ability.desc)
		AL["helptext"] = initial(ability.helptext)
		AL["owned"] = changeling.has_sting(ability)
		var/req_dna = initial(ability.req_dna)
		var/req_absorbs = initial(ability.req_absorbs)
		AL["dna_cost"] = dna_cost
		AL["can_purchase"] = ((req_absorbs <= true_absorbs) && (req_dna <= absorbed_dna_count) && (dna_cost <= genetic_points_remaining))

		abilities += list(AL)

	data["abilities"] = abilities

	return data

/datum/cellular_emporium/ui_act(action, params)
	if(..())
		return

	switch(action)
		if("readapt")
			if(changeling.canrespec)
				changeling.readapt()
		if("evolve")
			var/sting_name = params["name"]
			changeling.purchase_power(sting_name)

/datum/action/innate/cellular_emporium
	name = "Cellular Emporium"
	icon_icon = 'icons/obj/drinks.dmi'
	button_icon_state = "changelingsting"
	background_icon_state = "bg_changeling"
	var/datum/cellular_emporium/cellular_emporium

/datum/action/innate/cellular_emporium/New(our_target)
	. = ..()
	button.name = name
	if(istype(our_target, /datum/cellular_emporium))
		cellular_emporium = our_target
	else
		CRASH("cellular_emporium action created with non emporium")

/datum/action/innate/cellular_emporium/Activate()
	cellular_emporium.ui_interact(owner)
