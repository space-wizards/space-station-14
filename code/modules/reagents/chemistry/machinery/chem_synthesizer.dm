/obj/machinery/chem_dispenser/chem_synthesizer //formerly SCP-294 made by mrty, but now only for testing purposes
	name = "\improper debug chemical synthesizer"
	desc = "If you see this, yell at adminbus."
	icon = 'icons/obj/chemical.dmi'
	icon_state = "dispenser"
	amount = 10
	resistance_flags = INDESTRUCTIBLE | FIRE_PROOF | ACID_PROOF | LAVA_PROOF
	flags_1 = NODECONSTRUCT_1
	use_power = NO_POWER_USE
	ui_x = 390
	ui_y = 330

	var/static/list/shortcuts = list(
		"meth" = /datum/reagent/drug/methamphetamine
	)

/obj/machinery/chem_dispenser/chem_synthesizer/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, \
											datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "chem_synthesizer", name, ui_x, ui_y, master_ui, state)
		ui.open()

/obj/machinery/chem_dispenser/chem_synthesizer/ui_act(action, params)
	if(..())
		return
	switch(action)
		if("ejectBeaker")
			if(beaker)
				beaker.forceMove(drop_location())
				if(Adjacent(usr) && !issilicon(usr))
					usr.put_in_hands(beaker)
				beaker = null
				. = TRUE
		if("input")
			var/input_reagent = replacetext(lowertext(input("Enter the name of any reagent", "Input") as text|null), " ", "") //95% of the time, the reagent id is a lowercase/no spaces version of the name

			if (isnull(input_reagent))
				return

			if(shortcuts[input_reagent])
				input_reagent = shortcuts[input_reagent]
			else
				input_reagent = find_reagent(input_reagent)
			if(!input_reagent)
				say("REAGENT NOT FOUND")
				return
			else
				if(!beaker)
					return
				else if(!beaker.reagents && !QDELETED(beaker))
					beaker.create_reagents(beaker.volume)
				beaker.reagents.add_reagent(input_reagent, amount)
		if("makecup")
			if(beaker)
				return
			beaker = new /obj/item/reagent_containers/glass/beaker/bluespace(src)
			visible_message("<span class='notice'>[src] dispenses a bluespace beaker.</span>")
		if("amount")
			var/input = text2num(params["amount"])
			if(input)
				amount = input
	update_icon()

/obj/machinery/chem_dispenser/chem_synthesizer/proc/find_reagent(input)
	. = FALSE
	if(GLOB.chemical_reagents_list[input]) //prefer IDs!
		return input
	else
		return get_chem_id(input)
