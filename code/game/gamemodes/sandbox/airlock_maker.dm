/*
	This is for the sandbox for now,
	maybe useful later for an actual thing?
	-Sayu
*/

/obj/structure/door_assembly
	var/datum/airlock_maker/maker = null

/obj/structure/door_assembly/attack_hand()
	. = ..()
	if(.)
		return
	if(maker)
		maker.interact()

/datum/airlock_maker
	var/obj/structure/door_assembly/linked = null

	var/list/access_used = null
	var/require_all = 1

	var/paintjob = "none"
	var/glassdoor = 0

	var/doorname = "airlock"

/datum/airlock_maker/New(var/atom/target_loc)
	linked = new(target_loc)
	linked.maker = src
	linked.anchored = FALSE
	access_used = list()

	interact()

/datum/airlock_maker/proc/linkpretty(href,desc,active)
	if(!desc)
		var/static/list/defaults = list("No","Yes")
		desc = defaults[active+1]
	if(active)
		return "<a href='?src=[REF(src)];[href]'><b>[desc]</b></a>"
	return "<a href='?src=[REF(src)];[href]'><i>[desc]</i></a>"

/datum/airlock_maker/proc/interact()
	var/list/leftcolumn = list()
	var/list/rightcolumn = list()
	leftcolumn += "<u><b>Required Access</b></u>"
	for(var/access in get_all_accesses())
		leftcolumn += linkpretty("access=[access]",get_access_desc(access),access in access_used)
	leftcolumn += "Require all listed accesses: [linkpretty("reqall",null,require_all)]"

	rightcolumn += "<u><b>Paintjob</b></u>"
	for(var/option in list("none","engineering","atmos","security","command","medical","research","mining","maintenance","external","highsecurity"))
		rightcolumn += linkpretty("paint=[option]",option,option == paintjob)
	rightcolumn += "Glass door: " + linkpretty("glass",null,glassdoor) + "<br><br>"
	var/length = max(leftcolumn.len,rightcolumn.len)

	var/dat = "You may move the model airlock around.  A new airlock will be built in its space when you click done, below.<hr><br>"
	dat += "<a href='?src=[REF(src)];rename'>Door name</a>: \"[doorname]\""
	dat += "<table>"
	for(var/i=1; i<=length; i++)
		dat += "<tr><td>"
		if(i<=leftcolumn.len)
			dat += leftcolumn[i]
		dat += "</td><td>"
		if(i<=rightcolumn.len)
			dat += rightcolumn[i]
		dat += "</td></tr>"

	dat += "</table><hr><a href='?src=[REF(src)];done'>Finalize Airlock Construction</a> | <a href='?src=[REF(src)];cancel'>Cancel and Destroy Airlock</a>"
	usr << browse(dat,"window=airlockmaker")

/datum/airlock_maker/Topic(var/href,var/list/href_list)
	if(!usr)
		return
	if(!src || !linked || !linked.loc)
		usr << browse(null,"window=airlockmaker")
		return

	if("rename" in href_list)
		var/newname = stripped_input(usr,"New airlock name:","Name the airlock",doorname)
		if(newname)
			doorname = newname
	if("access" in href_list)
		var/value = text2num(href_list["access"])
		access_used ^= value
	if("reqall" in href_list)
		require_all = !require_all
	if("paint" in href_list)
		paintjob = href_list["paint"]
	if("glass" in href_list)
		glassdoor = !glassdoor

	if("cancel" in href_list)
		usr << browse(null,"window=airlockmaker")
		qdel(linked)
		qdel(src)
		return

	if("done" in href_list)
		usr << browse(null,"window=airlockmaker")
		var/turf/t_loc = linked.loc
		qdel(linked)
		if(!istype(t_loc))
			return

		var/target_type = "/obj/machinery/door/airlock"
		if(glassdoor)
			if(paintjob != "none")
				if(paintjob in list("external","highsecurity","maintenance")) // no glass version
					target_type += "/[paintjob]"
				else
					target_type += "/glass_[paintjob]"
			else
				target_type += "/glass"
		else if(paintjob != "none")
			target_type += "/[paintjob]"
		var/final = target_type
		target_type = text2path(final)
		if(!target_type)
			to_chat(usr, "Didn't work, contact Sayu with this: [final]")
			usr << browse(null,"window=airlockmaker")
			return

		var/obj/machinery/door/D = new target_type(t_loc)

		D.name = doorname

		if(access_used.len == 0)
			D.req_access = null
			D.req_one_access = null
		else if(require_all)
			D.req_access = access_used.Copy()
			D.req_one_access = null
		else
			D.req_access = null
			D.req_one_access = access_used.Copy()

		return

	interact()
