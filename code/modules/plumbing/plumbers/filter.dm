///chemical plumbing filter. If it's not filtered by left and right, it goes straight.
/obj/machinery/plumbing/filter
	name = "chemical filter"
	desc = "A chemical filter for filtering chemicals. The left and right outputs appear to be from the perspective of the input port."
	icon_state = "filter"
	density = FALSE
	///whitelist of chems id's that go to the left side. Empty to disable port
	var/list/left = list()
	///whitelist of chem id's that go to the right side. Empty to disable port
	var/list/right = list()
	///whitelist of chems but their name instead of path
	var/list/english_left = list()
	///whitelist of chems but their name instead of path
	var/list/english_right = list()

	ui_x = 500
	ui_y = 300

/obj/machinery/plumbing/filter/Initialize(mapload, bolt)
	. = ..()
	AddComponent(/datum/component/plumbing/filter, bolt)

/obj/machinery/plumbing/filter/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "chemical_filter", name, ui_x, ui_y, master_ui, state)
		ui.open()

/obj/machinery/plumbing/filter/ui_data(mob/user)
	var/list/data = list()
	data["left"] = english_left
	data["right"] = english_right
	return data

/obj/machinery/plumbing/filter/ui_act(action, params)
	if(..())
		return
	. = TRUE
	switch(action)
		if("add")
			var/new_chem_name = params["name"]
			var/chem_id = get_chem_id(new_chem_name)
			if(chem_id)
				switch(params["which"])
					if("left")
						if(!left.Find(chem_id))
							english_left += new_chem_name
							left += chem_id
					if("right")
						if(!right.Find(chem_id))
							english_right += new_chem_name
							right += chem_id
			else
				to_chat(usr, "<span class='warning'>No such known reagent exists!</span>")

		if("remove")
			var/chem_name = params["reagent"]
			var/chem_id = get_chem_id(chem_name)
			switch(params["which"])
				if("left")
					if(english_left.Find(chem_name))
						english_left -= chem_name
						left -= chem_id
				if("right")
					if(english_right.Find(chem_name))
						english_right -= chem_name
						right -= chem_id


