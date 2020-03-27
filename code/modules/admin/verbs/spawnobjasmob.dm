/datum/admins/proc/spawn_objasmob(object as text)
	set category = "Debug"
	set desc = "(obj path) Spawn object-mob"
	set name = "Spawn object-mob"

	if(!check_rights(R_SPAWN))
		return

	var/chosen = pick_closest_path(object, make_types_fancy(subtypesof(/obj)))

	if (!chosen)
		return

	var/mob/living/simple_animal/hostile/mimic/copy/basemob = /mob/living/simple_animal/hostile/mimic/copy

	var/obj/chosen_obj = text2path(chosen)

	var/list/settings = list(
    "mainsettings" = list(
      "name" = list("desc" = "Name", "type" = "string", "value" = "Bob"),
			"maxhealth" = list("desc" = "Max. health", "type" = "number", "value" = 100),
      "access" = list("desc" = "Access ID", "type" = "datum", "path" = "/obj/item/card/id", "value" = "Default"),
			"objtype" = list("desc" = "Base obj type", "type" = "datum", "path" = "/obj", "value" = "[chosen]"),
			"googlyeyes" = list("desc" = "Googly eyes", "type" = "boolean", "value" = "No"),
			"disableai" = list("desc" = "Disable AI", "type" = "boolean", "value" = "Yes"),
			"idledamage" = list("desc" = "Damaged while idle", "type" = "boolean", "value" = "No"),
			"dropitem" = list("desc" = "Drop obj on death", "type" = "boolean", "value" = "Yes"),
			"mobtype" = list("desc" = "Base mob type", "type" = "datum", "path" = "/mob/living/simple_animal/hostile/mimic/copy", "value" = "/mob/living/simple_animal/hostile/mimic/copy"),
			"ckey" = list("desc" = "ckey", "type" = "ckey", "value" = "none"),
    )
	)

	var/list/prefreturn = presentpreflikepicker(usr,"Customize mob", "Customize mob", Button1="Ok", width = 450, StealFocus = 1,Timeout = 0, settings=settings)
	if (prefreturn["button"] == 1)
		settings = prefreturn["settings"]
		var/mainsettings = settings["mainsettings"]
		chosen_obj = text2path(mainsettings["objtype"]["value"])

		basemob = text2path(mainsettings["mobtype"]["value"])
		if (!ispath(basemob, /mob/living/simple_animal/hostile/mimic/copy) || !ispath(chosen_obj, /obj))
			to_chat(usr, "Mob or object path invalid")

		basemob = new basemob(get_turf(usr), new chosen_obj(get_turf(usr)), usr, mainsettings["dropitem"]["value"] == "Yes" ? FALSE : TRUE, (mainsettings["googlyeyes"]["value"] == "Yes" ? FALSE : TRUE))

		if (mainsettings["disableai"]["value"] == "Yes")
			basemob.toggle_ai(AI_OFF)

		if (mainsettings["idledamage"]["value"] == "No")
			basemob.idledamage = FALSE

		if (mainsettings["access"])
			var/newaccess = text2path(mainsettings["access"]["value"])
			if (ispath(newaccess))
				basemob.access_card = new newaccess

		if (mainsettings["maxhealth"]["value"])
			if (!isnum(mainsettings["maxhealth"]["value"]))
				mainsettings["maxhealth"]["value"] = text2num(mainsettings["maxhealth"]["value"])
			if (mainsettings["maxhealth"]["value"] > 0)
				basemob.maxHealth = basemob.maxHealth =  mainsettings["maxhealth"]["value"]

		if (mainsettings["name"]["value"])
			basemob.name = basemob.real_name = html_decode(mainsettings["name"]["value"])

		if (mainsettings["ckey"]["value"] != "none")
			basemob.ckey = mainsettings["ckey"]["value"]


		log_admin("[key_name(usr)] spawned a sentient object-mob [basemob] from [chosen_obj] at [AREACOORD(usr)]")
		SSblackbox.record_feedback("tally", "admin_verb", 1, "Spawn object-mob") //If you are copy-pasting this, ensure the 2nd parameter is unique to the new proc!
