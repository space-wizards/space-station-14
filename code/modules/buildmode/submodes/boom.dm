/datum/buildmode_mode/boom
	key = "boom"

	var/devastation = -1
	var/heavy = -1
	var/light = -1
	var/flash = -1
	var/flames = -1

/datum/buildmode_mode/boom/show_help(client/c)
	to_chat(c, "<span class='notice'>***********************************************************</span>")
	to_chat(c, "<span class='notice'>Mouse Button on obj  = Kaboom</span>")
	to_chat(c, "<span class='notice'>NOTE: Using the \"Config/Launch Supplypod\" verb allows you to do this in an IC way (i.e., making a cruise missile come down from the sky and explode wherever you click!)</span>")
	to_chat(c, "<span class='notice'>***********************************************************</span>")

/datum/buildmode_mode/boom/change_settings(client/c)
	devastation = input(c, "Range of total devastation. -1 to none", text("Input")) as num|null
	if(devastation == null)
		devastation = -1
	heavy = input(c, "Range of heavy impact. -1 to none", text("Input")) as num|null
	if(heavy == null)
		heavy = -1
	light = input(c, "Range of light impact. -1 to none", text("Input")) as num|null
	if(light == null)
		light = -1
	flash = input(c, "Range of flash. -1 to none", text("Input")) as num|null
	if(flash == null)
		flash = -1
	flames = input(c, "Range of flames. -1 to none", text("Input")) as num|null
	if(flames == null)
		flames = -1

/datum/buildmode_mode/boom/handle_click(client/c, params, obj/object)
	var/list/pa = params2list(params)
	var/left_click = pa.Find("left")

	if(left_click)
		explosion(object, devastation, heavy, light, flash, FALSE, TRUE, flames)
		log_admin("Build Mode: [key_name(c)] caused an explosion(dev=[devastation], hvy=[heavy], lgt=[light], flash=[flash], flames=[flames]) at [AREACOORD(object)]")
