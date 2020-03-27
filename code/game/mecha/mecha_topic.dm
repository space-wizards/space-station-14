
////////////////////////////////////
///// Rendering stats window ///////
////////////////////////////////////

/obj/mecha/proc/get_stats_html()
	. = {"<html>
			<head><title>[name] data</title>
				<style>
					body {color: #00ff00; background: #000000; font-family:"Lucida Console",monospace; font-size: 12px;}
					hr {border: 1px solid #0f0; color: #0f0; background-color: #0f0;}
					a {padding:2px 5px;;color:#0f0;}
					.wr {margin-bottom: 5px;}
					.header {cursor:pointer;}
					.open, .closed {background: #32CD32; color:#000; padding:1px 2px;}
					.links a {margin-bottom: 2px;padding-top:3px;}
					.visible {display: block;}
					.hidden {display: none;}
				</style>
				<script language='javascript' type='text/javascript'>
					[js_byjax]
					[js_dropdowns]
					function SSticker() {
						setInterval(function(){
							window.location='byond://?src=[REF(src)]&update_content=1';
						}, 1000);
					}

					window.onload = function() {
						dropdowns();
						SSticker();
					}
				</script>
			</head>
			<body>
				<div id='content'>
					[get_stats_part()]
				</div></div>
				<div id='eq_list'>
					[get_equipment_list()]
				</div>
				<hr>
				<div id='commands'>
					[get_commands()]
				</div>
				<div id='equipment_menu'>
					[get_equipment_menu()]
				</div>
			</body>
		</html>"}

///Returns the status of the mech.
/obj/mecha/proc/get_stats_part()
	var/integrity = obj_integrity/max_integrity*100
	var/cell_charge = get_charge()
	var/datum/gas_mixture/int_tank_air = 0
	var/tank_pressure = 0
	var/tank_temperature = 0
	var/cabin_pressure = 0
	if (internal_tank)
		int_tank_air = internal_tank.return_air()
		tank_pressure = internal_tank ? round(int_tank_air.return_pressure(),0.01) : "None"
		tank_temperature = internal_tank ? int_tank_air.temperature : "Unknown"
		cabin_pressure = round(return_pressure(),0.01)
	. =	{"[report_internal_damage()]
		[integrity<30?"<span class='userdanger'>DAMAGE LEVEL CRITICAL</span><br>":null]
		<b>Integrity: </b> [integrity]%<br>
		<b>Powercell charge: </b>[isnull(cell_charge)?"No powercell installed":"[cell.percent()]%"]<br>
		<b>Air source: </b>[internal_tank?"[use_internal_tank?"Internal Airtank":"Environment"]":"Environment"]<br>
		<b>Airtank pressure: </b>[internal_tank?"[tank_pressure]kPa":"N/A"]<br>
		<b>Airtank temperature: </b>[internal_tank?"[tank_temperature]&deg;K|[tank_temperature - T0C]&deg;C":"N/A"]<br>
		<b>Cabin pressure: </b>[internal_tank?"[cabin_pressure>WARNING_HIGH_PRESSURE ? "<span class='danger'>[cabin_pressure]</span>": cabin_pressure]kPa":"N/A"]<br>
		<b>Cabin temperature: </b> [internal_tank?"[return_temperature()]&deg;K|[return_temperature() - T0C]&deg;C":"N/A"]<br>
		[dna_lock?"<b>DNA-locked:</b><br> <span style='font-size:10px;letter-spacing:-1px;'>[dna_lock]</span> \[<a href='?src=[REF(src)];reset_dna=1'>Reset</a>\]<br>":""]<br>"}
	. += "[get_actions()]<br>"

///Returns HTML for mech actions. Ideally, this proc would be empty for the base mecha. Segmented for easy refactoring.
/obj/mecha/proc/get_actions()
	. = ""
	. += "[defense_action.owner ? "<b>Defense Mode: </b> [defense_mode ? "Enabled" : "Disabled"]<br>" : ""]"
	. += "[overload_action.owner ? "<b>Leg Actuators Overload: </b> [leg_overload_mode ? "Enabled" : "Disabled"]<br>" : ""]"
	. += "[smoke_action.owner ? "<b>Smoke: </b> [smoke]<br>" : ""]"
	. += "[zoom_action.owner ? "<b>Zoom: </b> [zoom_mode ? "Enabled" : "Disabled"]<br>" : ""]"
	. += "[switch_damtype_action.owner ? "<b>Damtype: </b> [damtype]<br>" : ""]"
	. += "[phasing_action.owner ? "<b>Phase Modulator: </b> [phasing ? "Enabled" : "Disabled"]<br>" : ""]"

///HTML for internal damage.
/obj/mecha/proc/report_internal_damage()
	. = ""
	var/list/dam_reports = list(
		"[MECHA_INT_FIRE]" = "<span class='userdanger'>INTERNAL FIRE</span>",
		"[MECHA_INT_TEMP_CONTROL]" = "<span class='userdanger'>LIFE SUPPORT SYSTEM MALFUNCTION</span>",
		"[MECHA_INT_TANK_BREACH]" = "<span class='userdanger'>GAS TANK BREACH</span>",
		"[MECHA_INT_CONTROL_LOST]" = "<span class='userdanger'>COORDINATION SYSTEM CALIBRATION FAILURE</span> - <a href='?src=[REF(src)];repair_int_control_lost=1'>Recalibrate</a>",
		"[MECHA_INT_SHORT_CIRCUIT]" = "<span class='userdanger'>SHORT CIRCUIT</span>"
								)
	for(var/tflag in dam_reports)
		var/intdamflag = text2num(tflag)
		if(internal_damage & intdamflag)
			. += dam_reports[tflag]
			. += "<br />"
	if(return_pressure() > WARNING_HIGH_PRESSURE)
		. += "<span class='userdanger'>DANGEROUSLY HIGH CABIN PRESSURE</span><br />"

///HTML for list of equipment.
/obj/mecha/proc/get_equipment_list() //outputs mecha equipment list in html
	if(!equipment.len)
		return
	. = "<b>Equipment:</b><div style=\"margin-left: 15px;\">"
	for(var/obj/item/mecha_parts/mecha_equipment/MT in equipment)
		. += "<div id='[REF(MT)]'>[MT.get_equip_info()]</div>"
	. += "</div>"

///HTML for commands.
/obj/mecha/proc/get_commands()
	. = {"
	<div class='wr'>
		<div class='header'>Electronics</div>
		<div class='links'>
			<b>Radio settings:</b><br>
			Microphone:
			[radio? "<a href='?src=[REF(src)];rmictoggle=1'>\
			<span id=\"rmicstate\">[radio.broadcasting?"Engaged":"Disengaged"]</span></a>":"Error"]<br>
			Speaker:
			[radio? "<a href='?src=[REF(src)];rspktoggle=1'><span id=\"rspkstate\">\
			[radio.listening?"Engaged":"Disengaged"]</span></a>":"Error"]<br>
			Frequency:
			[radio? "<a href='?src=[REF(src)];rfreq=-10'>-</a>":"-"]
			[radio? "<a href='?src=[REF(src)];rfreq=-2'>-</a>":"-"]
			<span id=\"rfreq\">[radio?"[format_frequency(radio.frequency)]":"Error"]</span>
			[radio? "<a href='?src=[REF(src)];rfreq=2'>+</a>":"+"]
			[radio? "<a href='?src=[REF(src)];rfreq=10'>+</a>":"+"]<br>
		</div>
	</div>
	<div class='wr'>
		<div class='header'>Permissions & Logging</div>
		<div class='links'>
			<a href='?src=[REF(src)];toggle_id_upload=1'><span id='t_id_upload'>[add_req_access?"L":"Unl"]ock ID upload panel</span></a><br>
			<a href='?src=[REF(src)];toggle_maint_access=1'><span id='t_maint_access'>[maint_access?"Forbid":"Permit"] maintenance protocols</span></a><br>
			[internal_tank?"<a href='?src=[REF(src)];toggle_port_connection=1'><span id='t_port_connection'>[internal_tank.connected_port?"Disconnect from":"Connect to"] gas port</span></a><br>":""]
			<a href='?src=[REF(src)];dna_lock=1'>DNA-lock</a><br>
			<a href='?src=[REF(src)];change_name=1'>Change exosuit name</a>
		</div>
	</div>"}


/obj/mecha/proc/get_equipment_menu() //outputs mecha html equipment menu
	. = {"
	<div class='wr'>
	<div class='header'>Equipment</div>
	<div class='links'>"}
	if(equipment.len)
		for(var/X in equipment)
			var/obj/item/mecha_parts/mecha_equipment/W = X
			. += "[W.name] [W.detachable?"<a href='?src=[REF(W)];detach=1'>Detach</a><br>":"\[Non-removable\]<br>"]"
	. += {"<b>Available equipment slots:</b> [max_equip-equipment.len]
	</div>
	</div>"}

/obj/mecha/proc/output_access_dialog(obj/item/card/id/id_card, mob/user)
	if(!id_card || !user)
		return
	. = {"<html>
			<head>
				<style>
					h1 {font-size:15px;margin-bottom:4px;}
					body {color: #00ff00; background: #000000; font-family:"Courier New", Courier, monospace; font-size: 12px;}
					a {color:#0f0;}
				</style>
			</head>
			<body>
				<h1>Following keycodes are present in this system:</h1>"}
	for(var/a in operation_req_access)
		. += "[get_access_desc(a)] - <a href='?src=[REF(src)];del_req_access=[a];user=[REF(user)];id_card=[REF(id_card)]'>Delete</a><br>"
	. += "<hr><h1>Following keycodes were detected on portable device:</h1>"
	for(var/a in id_card.access)
		if(a in operation_req_access)
			continue
		var/a_name = get_access_desc(a)
		if(!a_name)
			continue //there's some strange access without a name
		. += "[a_name] - <a href='?src=[REF(src)];add_req_access=[a];user=[REF(user)];id_card=[REF(id_card)]'>Add</a><br>"
	. +={"<hr><a href='?src=[REF(src)];finish_req_access=1;user=[REF(user)]'>Lock ID panel</a><br>
		<span class='danger'>(Warning! The ID upload panel can be unlocked only through Exosuit Interface.)</span>
		</body>
		</html>"}
	user << browse(., "window=exosuit_add_access")
	onclose(user, "exosuit_add_access")


/obj/mecha/proc/output_maintenance_dialog(obj/item/card/id/id_card,mob/user)
	if(!id_card || !user)
		return
	. = {"<html>
			<head>
				<style>
					body {color: #00ff00; background: #000000; font-family:"Courier New", Courier, monospace; font-size: 12px;}
					a {padding:2px 5px; background:#32CD32;color:#000;display:block;margin:2px;text-align:center;text-decoration:none;}
				</style>
			</head>
			<body>
				[add_req_access?"<a href='?src=[REF(src)];req_access=1;id_card=[REF(id_card)];user=[REF(user)]'>Edit operation keycodes</a>":null]
				[maint_access?"<a href='?src=[REF(src)];maint_access=1;id_card=[REF(id_card)];user=[REF(user)]'>[(construction_state > MECHA_LOCKED) ? "Terminate" : "Initiate"] maintenance protocol</a>":null]
				[(construction_state == MECHA_OPEN_HATCH) ?"--------------------</br>":null]
				[(construction_state == MECHA_OPEN_HATCH) ?"[cell?"<a href='?src=[REF(src)];drop_cell=1;id_card=[REF(id_card)];user=[REF(user)]'>Drop power cell</a>":"No cell installed</br>"]":null]
				[(construction_state == MECHA_OPEN_HATCH) ?"[scanmod?"<a href='?src=[REF(src)];drop_scanmod=1;id_card=[REF(id_card)];user=[REF(user)]'>Drop scanning module</a>":"No scanning module installed</br>"]":null]
				[(construction_state == MECHA_OPEN_HATCH) ?"[capacitor?"<a href='?src=[REF(src)];drop_cap=1;id_card=[REF(id_card)];user=[REF(user)]'>Drop capacitor</a>":"No capacitor installed</br>"]":null]
				[(construction_state == MECHA_OPEN_HATCH) ?"--------------------</br>":null]
				[(construction_state > MECHA_LOCKED) ?"<a href='?src=[REF(src)];set_internal_tank_valve=1;user=[REF(user)]'>Set Cabin Air Pressure</a>":null]
			</body>
		</html>"}
	user << browse(., "window=exosuit_maint_console")
	onclose(user, "exosuit_maint_console")




/////////////////
///// Topic /////
/////////////////

/obj/mecha/Topic(href, href_list)
	..()

	if(!usr)
		return

	if(href_list["close"])
		return

	if(usr.incapacitated())
		return

	if(in_range(src, usr))
		//Start of ID requirements.
		if(href_list["id_card"])
			var/obj/item/card/id/id_card
			id_card = locate(href_list["id_card"])
			if(!istype(id_card))
				return

			if(href_list["req_access"])
				if(!add_req_access)
					return
				output_access_dialog(id_card,usr)
				return

			if(href_list["maint_access"])
				if(!maint_access)
					return
				if(construction_state == MECHA_LOCKED)
					construction_state = MECHA_SECURE_BOLTS
					to_chat(usr, "<span class='notice'>The securing bolts are now exposed.</span>")
				else if(construction_state == MECHA_SECURE_BOLTS)
					construction_state = MECHA_LOCKED
					to_chat(usr, "<span class='notice'>The securing bolts are now hidden.</span>")
				output_maintenance_dialog(id_card,usr)
				return
			if(href_list["drop_cell"])
				if(construction_state == MECHA_OPEN_HATCH)
					cell.forceMove(get_turf(src))
					cell = null
				output_maintenance_dialog(id_card,usr)
				return
			if(href_list["drop_scanmod"])
				if(construction_state == MECHA_OPEN_HATCH)
					scanmod.forceMove(get_turf(src))
					scanmod = null
				output_maintenance_dialog(id_card,usr)
				return
			if(href_list["drop_cap"])
				if(construction_state == MECHA_OPEN_HATCH)
					capacitor.forceMove(get_turf(src))
					capacitor = null
				output_maintenance_dialog(id_card,usr)
				return

			if(href_list["add_req_access"])
				if(!add_req_access)
					return
				operation_req_access += text2num(href_list["add_req_access"])
				output_access_dialog(id_card,usr)
				return

			if(href_list["del_req_access"])
				if(!add_req_access)
					return
				operation_req_access -= text2num(href_list["del_req_access"])
				output_access_dialog(id_card, usr)
				return
			return //Here end everything requiring an ID.

		//Here ID access stuff goes to die.
		if(href_list["finish_req_access"])
			add_req_access = 0
			usr << browse(null,"window=exosuit_add_access")
			return

		//Set pressure.
		if(href_list["set_internal_tank_valve"] && construction_state)
			var/new_pressure = input(usr,"Input new output pressure","Pressure setting",internal_tank_valve) as num|null
			if(isnull(new_pressure) || usr.incapacitated() || !construction_state)
				return
			internal_tank_valve = new_pressure
			to_chat(usr, "<span class='notice'>The internal pressure valve has been set to [internal_tank_valve]kPa.</span>")
			return

	//Start of all internal topic stuff.
	if(usr != occupant)
		return

	if(href_list["update_content"])
		send_byjax(usr,"exosuit.browser","content", get_stats_part())
		return

	//Selects the mech equipment/weapon.
	if(href_list["select_equip"])
		var/obj/item/mecha_parts/mecha_equipment/equip = locate(href_list["select_equip"]) in src
		if(!equip || !equip.selectable)
			return
		selected = equip
		occupant_message("<span class='notice'>You switch to [equip].</span>")
		visible_message("<span class='notice'>[src] raises [equip].</span>")
		send_byjax(usr, "exosuit.browser", "eq_list", get_equipment_list())
		return

	//Toggles radio broadcasting
	if(href_list["rmictoggle"])
		radio.broadcasting = !radio.broadcasting
		send_byjax(usr,"exosuit.browser","rmicstate",(radio.broadcasting?"Engaged":"Disengaged"))
		return

	//Toggles radio listening
	if(href_list["rspktoggle"])
		radio.listening = !radio.listening
		send_byjax(usr,"exosuit.browser","rspkstate",(radio.listening?"Engaged":"Disengaged"))
		return

	//Changes radio freqency.
	if(href_list["rfreq"])
		var/new_frequency = radio.frequency + text2num(href_list["rfreq"])
		radio.set_frequency(sanitize_frequency(new_frequency, radio.freerange))
		send_byjax(usr,"exosuit.browser","rfreq","[format_frequency(radio.frequency)]")
		return

	//Changes the exosuit name.
	if(href_list["change_name"])
		var/userinput = stripped_input(usr, "Choose a new exosuit name.", "Rename exosuit", "", MAX_NAME_LEN)
		if(!userinput || usr != occupant || usr.incapacitated())
			return
		name = userinput
		return

	//Toggles ID upload.
	if (href_list["toggle_id_upload"])
		add_req_access = !add_req_access
		send_byjax(usr,"exosuit.browser","t_id_upload","[add_req_access?"L":"Unl"]ock ID upload panel")
		return

	//Toggles main access.
	if(href_list["toggle_maint_access"])
		if(construction_state)
			occupant_message("<span class='danger'>Maintenance protocols in effect</span>")
			return
		maint_access = !maint_access
		send_byjax(usr,"exosuit.browser","t_maint_access","[maint_access?"Forbid":"Permit"] maintenance protocols")
		return

	//Toggles connection port.
	if (href_list["toggle_port_connection"])
		if(internal_tank.connected_port)
			if(internal_tank.disconnect())
				occupant_message("<span class='notice'>Disconnected from the air system port.</span>")
				log_message("Disconnected from gas port.", LOG_MECHA)
			else
				occupant_message("<span class='warning'>Unable to disconnect from the air system port!</span>")
				return
		else
			var/obj/machinery/atmospherics/components/unary/portables_connector/possible_port = locate() in loc
			if(internal_tank.connect(possible_port))
				occupant_message("<span class='notice'>Connected to the air system port.</span>")
				log_message("Connected to gas port.", LOG_MECHA)
			else
				occupant_message("<span class='warning'>Unable to connect with air system port!</span>")
				return
		send_byjax(occupant,"exosuit.browser","t_port_connection","[internal_tank.connected_port?"Disconnect from":"Connect to"] gas port")
		return

	//Turns on the DNA lock
	if(href_list["dna_lock"])
		if(!iscarbon(occupant) || !occupant.dna)
			occupant_message("<span class='notice'>You feel a prick as the needle takes your DNA sample.</span>")
			return
		dna_lock = occupant.dna.unique_enzymes
		occupant_message("<span class='notice'>You feel a prick as the needle takes your DNA sample.</span>")
		return

	//Resets the DNA lock
	if(href_list["reset_dna"])
		dna_lock = null
		return

	//Repairs internal damage
	if(href_list["repair_int_control_lost"])
		occupant_message("<span class='notice'>Recalibrating coordination system...</span>")
		log_message("Recalibration of coordination system started.", LOG_MECHA)
		addtimer(CALLBACK(src, .proc/stationary_repair, loc), 100, TIMER_UNIQUE)

///Repairs internal damage if the mech hasn't moved.
/obj/mecha/proc/stationary_repair(location)
	if(location == loc)
		clearInternalDamage(MECHA_INT_CONTROL_LOST)
		occupant_message("<span class='notice'>Recalibration successful.</span>")
		log_message("Recalibration of coordination system finished with 0 errors.", LOG_MECHA)
	else
		occupant_message("<span class='warning'>Recalibration failed!</span>")
		log_message("Recalibration of coordination system failed with 1 error.", LOG_MECHA, color="red")
