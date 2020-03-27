/obj/machinery/abductor/gland_dispenser
	name = "replacement organ storage"
	desc = "A tank filled with replacement organs."
	icon = 'icons/obj/abductor.dmi'
	icon_state = "dispenser"
	density = TRUE
	var/list/gland_types
	var/list/gland_colors
	var/list/amounts

/obj/machinery/abductor/gland_dispenser/proc/random_color()
	//TODO : replace with presets or spectrum
	return rgb(rand(0,255),rand(0,255),rand(0,255))

/obj/machinery/abductor/gland_dispenser/Initialize()
	. = ..()
	gland_types = subtypesof(/obj/item/organ/heart/gland)
	gland_types = shuffle(gland_types)
	gland_colors = new/list(gland_types.len)
	amounts = new/list(gland_types.len)
	for(var/i=1,i<=gland_types.len,i++)
		gland_colors[i] = random_color()
		amounts[i] = rand(1,5)

/obj/machinery/abductor/gland_dispenser/attack_hand(mob/user)
	. = ..()
	if(.)
		return
	if(!isabductor(user))
		return
	user.set_machine(src)
	var/box_css = {"
	<style>
	a.box.gland {
		float: left;
		width: 20px;
		height: 20px;
		margin: 5px;
		border-width: 1px;
		border-style: solid;
		border-color: rgba(0,0,0,.2);
		text-align: center;
		}
	</style>"}
	var/dat = ""
	var/item_count = 0
	for(var/i=1,i<=gland_colors.len,i++)
		item_count++
		var/g_color = gland_colors[i]
		var/amount = amounts[i]
		dat += "<a class='box gland' style='background-color:[g_color]' href='?src=[REF(src)];dispense=[i]'>[amount]</a>"
		if(item_count == 4) // Four boxes per line
			dat +="</br></br>"
			item_count = 0
	var/datum/browser/popup = new(user, "glands", "Gland Dispenser", 200, 200)
	popup.add_head_content(box_css)
	popup.set_content(dat)
	popup.open()
	return

/obj/machinery/abductor/gland_dispenser/attackby(obj/item/W, mob/user, params)
	if(istype(W, /obj/item/organ/heart/gland))
		if(!user.transferItemToLoc(W, src))
			return
		for(var/i=1,i<=gland_colors.len,i++)
			if(gland_types[i] == W.type)
				amounts[i]++
	else
		return ..()

/obj/machinery/abductor/gland_dispenser/Topic(href, href_list)
	if(..())
		return
	usr.set_machine(src)

	if(href_list["dispense"])
		Dispense(text2num(href_list["dispense"]))
	updateUsrDialog()

/obj/machinery/abductor/gland_dispenser/proc/Dispense(count)
	if(amounts[count]>0)
		amounts[count]--
		var/T = gland_types[count]
		new T(get_turf(src))
