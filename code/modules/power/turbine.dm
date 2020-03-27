// TURBINE v2 AKA rev4407 Engine reborn!

// How to use it? - Mappers
//
// This is a very good power generating mechanism. All you need is a blast furnace with soaring flames and output.
// Not everything is included yet so the turbine can run out of fuel quiet quickly. The best thing about the turbine is that even
// though something is on fire that passes through it, it won't be on fire as it passes out of it. So the exhaust fumes can still
// containt unreacted fuel - plasma and oxygen that needs to be filtered out and re-routed back. This of course requires smart piping
// For a computer to work with the turbine the compressor requires a comp_id matching with the turbine computer's id. This will be
// subjected to a change in the near future mind you. Right now this method of generating power is a good backup but don't expect it
// become a main power source unless some work is done. Have fun. At 50k RPM it generates 60k power. So more than one turbine is needed!
//
// - Numbers
//
// Example setup	 S - sparker
//					 B - Blast doors into space for venting
// *BBB****BBB*		 C - Compressor
// S    CT    *		 T - Turbine
// * ^ *  * V *		 D - Doors with firedoor
// **|***D**|**      ^ - Fuel feed (Not vent, but a gas outlet)
//   |      |        V - Suction vent (Like the ones in atmos
//


/obj/machinery/power/compressor
	name = "compressor"
	desc = "The compressor stage of a gas turbine generator."
	icon = 'icons/obj/atmospherics/pipes/simple.dmi'
	icon_state = "compressor"
	density = TRUE
	resistance_flags = FIRE_PROOF
	CanAtmosPass = ATMOS_PASS_DENSITY
	circuit = /obj/item/circuitboard/machine/power_compressor
	ui_x = 350
	ui_y = 280

	var/obj/machinery/power/turbine/turbine
	var/datum/gas_mixture/gas_contained
	var/turf/inturf
	var/starter = 0
	var/rpm = 0
	var/rpmtarget = 0
	var/capacity = 1e6
	var/comp_id = 0
	var/efficiency

/obj/machinery/power/compressor/Destroy()
	if (turbine && turbine.compressor == src)
		turbine.compressor = null
	turbine = null
	return ..()

/obj/machinery/power/turbine
	name = "gas turbine generator"
	desc = "A gas turbine used for backup power generation."
	icon = 'icons/obj/atmospherics/pipes/simple.dmi'
	icon_state = "turbine"
	density = TRUE
	resistance_flags = FIRE_PROOF
	CanAtmosPass = ATMOS_PASS_DENSITY
	circuit = /obj/item/circuitboard/machine/power_turbine
	var/opened = 0
	var/obj/machinery/power/compressor/compressor
	var/turf/outturf
	var/lastgen
	var/productivity = 1

/obj/machinery/power/turbine/Destroy()
	if (compressor && compressor.turbine == src)
		compressor.turbine = null
	compressor = null
	return ..()

// the inlet stage of the gas turbine electricity generator

/obj/machinery/power/compressor/Initialize()
	. = ..()
	// The inlet of the compressor is the direction it faces
	gas_contained = new
	inturf = get_step(src, dir)
	locate_machinery()
	if(!turbine)
		obj_break()


#define COMPFRICTION 5e5


/obj/machinery/power/compressor/locate_machinery()
	if(turbine)
		return
	turbine = locate() in get_step(src, get_dir(inturf, src))
	if(turbine)
		turbine.locate_machinery()

/obj/machinery/power/compressor/RefreshParts()
	var/E = 0
	for(var/obj/item/stock_parts/manipulator/M in component_parts)
		E += M.rating
	efficiency = E / 6

/obj/machinery/power/compressor/examine(mob/user)
	. = ..()
	if(in_range(user, src) || isobserver(user))
		. += "<span class='notice'>The status display reads: Efficiency at <b>[efficiency*100]%</b>.</span>"

/obj/machinery/power/compressor/attackby(obj/item/I, mob/user, params)
	if(default_deconstruction_screwdriver(user, initial(icon_state), initial(icon_state), I))
		return

	if(default_change_direction_wrench(user, I))
		turbine = null
		inturf = get_step(src, dir)
		locate_machinery()
		if(turbine)
			to_chat(user, "<span class='notice'>Turbine connected.</span>")
			stat &= ~BROKEN
		else
			to_chat(user, "<span class='alert'>Turbine not connected.</span>")
			obj_break()
		return

	default_deconstruction_crowbar(I)

/obj/machinery/power/compressor/process()
	if(!starter)
		return
	if(!turbine || (turbine.stat & BROKEN))
		starter = FALSE
	if(stat & BROKEN || panel_open)
		starter = FALSE
		return
	cut_overlays()

	rpm = 0.9* rpm + 0.1 * rpmtarget
	var/datum/gas_mixture/environment = inturf.return_air()

	// It's a simplified version taking only 1/10 of the moles from the turf nearby. It should be later changed into a better version

	var/transfer_moles = environment.total_moles()/10
	var/datum/gas_mixture/removed = inturf.remove_air(transfer_moles)
	gas_contained.merge(removed)

// RPM function to include compression friction - be advised that too low/high of a compfriction value can make things screwy

	rpm = min(rpm, (COMPFRICTION*efficiency)/2)
	rpm = max(0, rpm - (rpm*rpm)/(COMPFRICTION*efficiency))


	if(starter && !(stat & NOPOWER))
		use_power(2800)
		if(rpm<1000)
			rpmtarget = 1000
	else
		if(rpm<1000)
			rpmtarget = 0



	if(rpm>50000)
		add_overlay(mutable_appearance(icon, "comp-o4", FLY_LAYER))
	else if(rpm>10000)
		add_overlay(mutable_appearance(icon, "comp-o3", FLY_LAYER))
	else if(rpm>2000)
		add_overlay(mutable_appearance(icon, "comp-o2", FLY_LAYER))
	else if(rpm>500)
		add_overlay(mutable_appearance(icon, "comp-o1", FLY_LAYER))
	 //TODO: DEFERRED

// These are crucial to working of a turbine - the stats modify the power output. TurbGenQ modifies how much raw energy can you get from
// rpms, TurbGenG modifies the shape of the curve - the lower the value the less straight the curve is.

#define TURBGENQ 100000
#define TURBGENG 0.5

/obj/machinery/power/turbine/Initialize()
	. = ..()
// The outlet is pointed at the direction of the turbine component
	outturf = get_step(src, dir)
	locate_machinery()
	if(!compressor)
		obj_break()
	connect_to_network()

/obj/machinery/power/turbine/RefreshParts()
	var/P = 0
	for(var/obj/item/stock_parts/capacitor/C in component_parts)
		P += C.rating
	productivity = P / 6

/obj/machinery/power/turbine/examine(mob/user)
	. = ..()
	if(in_range(user, src) || isobserver(user))
		. += "<span class='notice'>The status display reads: Productivity at <b>[productivity*100]%</b>.</span>"

/obj/machinery/power/turbine/locate_machinery()
	if(compressor)
		return
	compressor = locate() in get_step(src, get_dir(outturf, src))
	if(compressor)
		compressor.locate_machinery()

/obj/machinery/power/turbine/process()

	if(!compressor)
		stat = BROKEN

	if((stat & BROKEN) || panel_open)
		return
	if(!compressor.starter)
		return
	cut_overlays()

	// This is the power generation function. If anything is needed it's good to plot it in EXCEL before modifying
	// the TURBGENQ and TURBGENG values

	lastgen = ((compressor.rpm / TURBGENQ)**TURBGENG) * TURBGENQ * productivity

	add_avail(lastgen)

	// Weird function but it works. Should be something else...

	var/newrpm = ((compressor.gas_contained.temperature) * compressor.gas_contained.total_moles())/4

	newrpm = max(0, newrpm)

	if(!compressor.starter || newrpm > 1000)
		compressor.rpmtarget = newrpm

	if(compressor.gas_contained.total_moles()>0)
		var/oamount = min(compressor.gas_contained.total_moles(), (compressor.rpm+100)/35000*compressor.capacity)
		var/datum/gas_mixture/removed = compressor.gas_contained.remove(oamount)
		outturf.assume_air(removed)

// If it works, put an overlay that it works!

	if(lastgen > 100)
		add_overlay(mutable_appearance(icon, "turb-o", FLY_LAYER))

	updateDialog()

/obj/machinery/power/turbine/attackby(obj/item/I, mob/user, params)
	if(default_deconstruction_screwdriver(user, initial(icon_state), initial(icon_state), I))
		return

	if(default_change_direction_wrench(user, I))
		compressor = null
		outturf = get_step(src, dir)
		locate_machinery()
		if(compressor)
			to_chat(user, "<span class='notice'>Compressor connected.</span>")
			stat &= ~BROKEN
		else
			to_chat(user, "<span class='alert'>Compressor not connected.</span>")
			obj_break()
		return

	default_deconstruction_crowbar(I)

/obj/machinery/power/turbine/ui_interact(mob/user)

	if(!Adjacent(user)  || (stat & (NOPOWER|BROKEN)) && !issilicon(user))
		user.unset_machine(src)
		user << browse(null, "window=turbine")
		return

	var/t = "<TT><B>Gas Turbine Generator</B><HR><PRE>"

	t += "Generated power : [DisplayPower(lastgen)]<BR><BR>"

	t += "Turbine: [round(compressor.rpm)] RPM<BR>"

	t += "Starter: [ compressor.starter ? "<A href='?src=[REF(src)];str=1'>Off</A> <B>On</B>" : "<B>Off</B> <A href='?src=[REF(src)];str=1'>On</A>"]"

	t += "</PRE><HR><A href='?src=[REF(src)];close=1'>Close</A>"

	t += "</TT>"
	var/datum/browser/popup = new(user, "turbine", name)
	popup.set_content(t)
	popup.open()

	return

/obj/machinery/power/turbine/Topic(href, href_list)
	if(..())
		return

	if( href_list["close"] )
		usr << browse(null, "window=turbine")
		usr.unset_machine(src)
		return

	else if( href_list["str"] )
		if(compressor)
			compressor.starter = !compressor.starter

	updateDialog()





/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
/////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

// COMPUTER NEEDS A SERIOUS REWRITE.

/obj/machinery/computer/turbine_computer
	name = "gas turbine control computer"
	desc = "A computer to remotely control a gas turbine."
	icon_screen = "turbinecomp"
	icon_keyboard = "tech_key"
	circuit = /obj/item/circuitboard/computer/turbine_computer
	var/obj/machinery/power/compressor/compressor
	var/id = 0

	ui_x = 300
	ui_y = 200

/obj/machinery/computer/turbine_computer/Initialize()
	. = ..()
	return INITIALIZE_HINT_LATELOAD

/obj/machinery/computer/turbine_computer/LateInitialize()
	locate_machinery()

/obj/machinery/computer/turbine_computer/locate_machinery()
	if(id)
		for(var/obj/machinery/power/compressor/C in GLOB.machines)
			if(C.comp_id == id)
				compressor = C
				return
	else
		compressor = locate(/obj/machinery/power/compressor) in range(7, src)

/obj/machinery/computer/turbine_computer/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, \
									datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "turbine_computer", name, ui_x, ui_y, master_ui, state)
		ui.open()

/obj/machinery/computer/turbine_computer/ui_data(mob/user)
	var/list/data = list()

	data["compressor"] = compressor ? TRUE : FALSE
	data["compressor_broke"] = (!compressor || (compressor.stat & BROKEN)) ? TRUE : FALSE
	data["turbine"] = compressor?.turbine ? TRUE : FALSE
	data["turbine_broke"] = (!compressor || !compressor.turbine || (compressor.turbine.stat & BROKEN)) ? TRUE : FALSE
	data["online"] = compressor?.starter

	data["power"] = DisplayPower(compressor?.turbine?.lastgen)
	data["rpm"] = compressor?.rpm
	data["temp"] = compressor?.gas_contained.temperature

	return data

/obj/machinery/computer/turbine_computer/ui_act(action, params)
	if(..())
		return
	switch(action)
		if("toggle_power")
			if(compressor && compressor.turbine)
				compressor.starter = !compressor.starter
				. = TRUE
		if("reconnect")
			locate_machinery()
			. = TRUE

#undef COMPFRICTION
#undef TURBGENQ
#undef TURBGENG
