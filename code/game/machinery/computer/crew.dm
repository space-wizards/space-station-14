#define SENSORS_UPDATE_PERIOD 100 //How often the sensor data updates.

/obj/machinery/computer/crew
	name = "crew monitoring console"
	desc = "Used to monitor active health sensors built into most of the crew's uniforms."
	icon_screen = "crew"
	icon_keyboard = "med_key"
	use_power = IDLE_POWER_USE
	idle_power_usage = 250
	active_power_usage = 500
	circuit = /obj/item/circuitboard/computer/crew

	light_color = LIGHT_COLOR_BLUE

/obj/machinery/computer/crew/syndie
	icon_keyboard = "syndie_key"

/obj/machinery/computer/crew/interact(mob/user)
	GLOB.crewmonitor.show(user,src)

GLOBAL_DATUM_INIT(crewmonitor, /datum/crewmonitor, new)

/datum/crewmonitor
	var/list/ui_sources = list() //List of user -> ui source
	var/list/jobs
	var/list/data_by_z = list()
	var/list/last_update = list()

/datum/crewmonitor/New()
	. = ..()

	var/list/jobs = new/list()
	jobs["Captain"] = 00
	jobs["Head of Personnel"] = 50
	jobs["Head of Security"] = 10
	jobs["Warden"] = 11
	jobs["Security Officer"] = 12
	jobs["Detective"] = 13
	jobs["Chief Medical Officer"] = 20
	jobs["Chemist"] = 21
	jobs["Geneticist"] = 22
	jobs["Virologist"] = 23
	jobs["Medical Doctor"] = 24
	jobs["Paramedic"] = 25
	jobs["Research Director"] = 30
	jobs["Scientist"] = 31
	jobs["Roboticist"] = 32
	jobs["Chief Engineer"] = 40
	jobs["Station Engineer"] = 41
	jobs["Atmospheric Technician"] = 42
	jobs["Quartermaster"] = 51
	jobs["Shaft Miner"] = 52
	jobs["Cargo Technician"] = 53
	jobs["Bartender"] = 61
	jobs["Cook"] = 62
	jobs["Botanist"] = 63
	jobs["Curator"] = 64
	jobs["Chaplain"] = 65
	jobs["Clown"] = 66
	jobs["Mime"] = 67
	jobs["Janitor"] = 68
	jobs["Lawyer"] = 69
	jobs["Admiral"] = 200
	jobs["CentCom Commander"] = 210
	jobs["Custodian"] = 211
	jobs["Medical Officer"] = 212
	jobs["Research Officer"] = 213
	jobs["Emergency Response Team Commander"] = 220
	jobs["Security Response Officer"] = 221
	jobs["Engineer Response Officer"] = 222
	jobs["Medical Response Officer"] = 223
	jobs["Assistant"] = 999 //Unknowns/custom jobs should appear after civilians, and before assistants

	src.jobs = jobs

/datum/crewmonitor/Destroy()
	return ..()

/datum/crewmonitor/ui_interact(mob/user, ui_key = "crew", datum/tgui/ui = null, force_open = FALSE, \
							datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if (!ui)
		ui = new(user, src, ui_key, "crew", "crew monitor", 800, 600 , master_ui, state)
		ui.open()

/datum/crewmonitor/proc/show(mob/M, source)
	ui_sources[M] = source
	ui_interact(M)

/datum/crewmonitor/ui_host(mob/user)
	return ui_sources[user]

/datum/crewmonitor/ui_data(mob/user)
	var/z = user.z
	if(!z)
		var/turf/T = get_turf(user)
		z = T.z
	var/list/zdata = update_data(z)
	. = list()
	.["sensors"] = zdata
	.["link_allowed"] = isAI(user)

/datum/crewmonitor/proc/update_data(z)
	if(data_by_z["[z]"] && last_update["[z]"] && world.time <= last_update["[z]"] + SENSORS_UPDATE_PERIOD)
		return data_by_z["[z]"]

	var/list/results = list()
	var/obj/item/clothing/under/U
	var/obj/item/card/id/I
	var/turf/pos
	var/ijob
	var/name
	var/assignment
	var/oxydam
	var/toxdam
	var/burndam
	var/brutedam
	var/area
	var/pos_x
	var/pos_y
	var/life_status

	for(var/i in GLOB.human_list)
		var/mob/living/carbon/human/H = i
		var/nanite_sensors = FALSE
		if(H in SSnanites.nanite_monitored_mobs)
			nanite_sensors = TRUE
		// Check if their z-level is correct and if they are wearing a uniform.
		// Accept H.z==0 as well in case the mob is inside an object.
		if ((H.z == 0 || H.z == z) && (istype(H.w_uniform, /obj/item/clothing/under) || nanite_sensors))
			U = H.w_uniform

			// Are the suit sensors on?
			if (nanite_sensors || ((U.has_sensor > 0) && U.sensor_mode))
				pos = H.z == 0 || (nanite_sensors || U.sensor_mode == SENSOR_COORDS) ? get_turf(H) : null

				// Special case: If the mob is inside an object confirm the z-level on turf level.
				if (H.z == 0 && (!pos || pos.z != z))
					continue

				I = H.wear_id ? H.wear_id.GetID() : null

				if (I)
					name = I.registered_name
					assignment = I.assignment
					ijob = jobs[I.assignment]
				else
					name = "Unknown"
					assignment = ""
					ijob = 80

				if (nanite_sensors || U.sensor_mode >= SENSOR_LIVING)
					life_status = (!H.stat ? TRUE : FALSE)
				else
					life_status = null

				if (nanite_sensors || U.sensor_mode >= SENSOR_VITALS)
					oxydam = round(H.getOxyLoss(),1)
					toxdam = round(H.getToxLoss(),1)
					burndam = round(H.getFireLoss(),1)
					brutedam = round(H.getBruteLoss(),1)
				else
					oxydam = null
					toxdam = null
					burndam = null
					brutedam = null

				if (nanite_sensors || U.sensor_mode >= SENSOR_COORDS)
					if (!pos)
						pos = get_turf(H)
					area = get_area_name(H, TRUE)
					pos_x = pos.x
					pos_y = pos.y
				else
					area = null
					pos_x = null
					pos_y = null

				results[++results.len] = list("name" = name, "assignment" = assignment, "ijob" = ijob, "life_status" = life_status, "oxydam" = oxydam, "toxdam" = toxdam, "burndam" = burndam, "brutedam" = brutedam, "area" = area, "pos_x" = pos_x, "pos_y" = pos_y, "can_track" = H.can_track(null))

	data_by_z["[z]"] = sortTim(results,/proc/sensor_compare)
	last_update["[z]"] = world.time

	return results

/proc/sensor_compare(list/a,list/b)
	return a["ijob"] - b["ijob"]

/datum/crewmonitor/ui_act(action,params)
	var/mob/living/silicon/ai/AI = usr
	if(!istype(AI))
		return
	switch (action)
		if ("select_person")
			AI.ai_camera_track(params["name"])

#undef SENSORS_UPDATE_PERIOD
