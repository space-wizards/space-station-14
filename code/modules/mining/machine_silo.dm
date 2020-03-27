GLOBAL_DATUM(ore_silo_default, /obj/machinery/ore_silo)
GLOBAL_LIST_EMPTY(silo_access_logs)

/obj/machinery/ore_silo
	name = "ore silo"
	desc = "An all-in-one bluespace storage and transmission system for the station's mineral distribution needs."
	icon = 'icons/obj/mining.dmi'
	icon_state = "silo"
	density = TRUE
	circuit = /obj/item/circuitboard/machine/ore_silo

	var/list/holds = list()
	var/list/datum/component/remote_materials/connected = list()
	var/log_page = 1

/obj/machinery/ore_silo/Initialize(mapload)
	. = ..()
	AddComponent(/datum/component/material_container,
		list(/datum/material/iron, /datum/material/glass, /datum/material/silver, /datum/material/gold, /datum/material/diamond, /datum/material/plasma, /datum/material/uranium, /datum/material/bananium, /datum/material/titanium, /datum/material/bluespace, /datum/material/plastic),
		INFINITY,
		FALSE,
		/obj/item/stack,
		null,
		null,
		TRUE)
	if (!GLOB.ore_silo_default && mapload && is_station_level(z))
		GLOB.ore_silo_default = src

/obj/machinery/ore_silo/Destroy()
	if (GLOB.ore_silo_default == src)
		GLOB.ore_silo_default = null

	for(var/C in connected)
		var/datum/component/remote_materials/mats = C
		mats.disconnect_from(src)

	connected = null

	var/datum/component/material_container/materials = GetComponent(/datum/component/material_container)
	materials.retrieve_all()

	return ..()

/obj/machinery/ore_silo/proc/remote_attackby(obj/machinery/M, mob/user, obj/item/stack/I)
	var/datum/component/material_container/materials = GetComponent(/datum/component/material_container)
	// stolen from /datum/component/material_container/proc/OnAttackBy
	if(user.a_intent != INTENT_HELP)
		return
	if(I.item_flags & ABSTRACT)
		return
	if(!istype(I) || (I.flags_1 & HOLOGRAM_1) || (I.item_flags & NO_MAT_REDEMPTION))
		to_chat(user, "<span class='warning'>[M] won't accept [I]!</span>")
		return
	var/item_mats = I.custom_materials & materials.materials
	if(!length(item_mats))
		to_chat(user, "<span class='warning'>[I] does not contain sufficient materials to be accepted by [M].</span>")
		return
	// assumes unlimited space...
	var/amount = I.amount
	materials.user_insert(I, user)
	silo_log(M, "deposited", amount, "sheets", item_mats)
	return TRUE

/obj/machinery/ore_silo/attackby(obj/item/W, mob/user, params)
	if (istype(W, /obj/item/stack))
		return remote_attackby(src, user, W)
	return ..()

/obj/machinery/ore_silo/ui_interact(mob/user)
	user.set_machine(src)
	var/datum/browser/popup = new(user, "ore_silo", null, 600, 550)
	popup.set_content(generate_ui())
	popup.open()

/obj/machinery/ore_silo/proc/generate_ui()
	var/datum/component/material_container/materials = GetComponent(/datum/component/material_container)
	var/list/ui = list("<head><title>Ore Silo</title></head><body><div class='statusDisplay'><h2>Stored Material:</h2>")
	var/any = FALSE
	for(var/M in materials.materials)
		var/datum/material/mat = M
		var/amount = materials.materials[M]
		var/sheets = round(amount) / MINERAL_MATERIAL_AMOUNT
		var/ref = REF(M)
		if (sheets)
			if (sheets >= 1)
				ui += "<a href='?src=[REF(src)];ejectsheet=[ref];eject_amt=1'>Eject</a>"
			else
				ui += "<span class='linkOff'>Eject</span>"
			if (sheets >= 20)
				ui += "<a href='?src=[REF(src)];ejectsheet=[ref];eject_amt=20'>20x</a>"
			else
				ui += "<span class='linkOff'>20x</span>"
			ui += "<b>[mat.name]</b>: [sheets] sheets<br>"
			any = TRUE
	if(!any)
		ui += "Nothing!"

	ui += "</div><div class='statusDisplay'><h2>Connected Machines:</h2>"
	for(var/C in connected)
		var/datum/component/remote_materials/mats = C
		var/atom/parent = mats.parent
		var/hold_key = "[get_area(parent)]/[mats.category]"
		ui += "<a href='?src=[REF(src)];remove=[REF(mats)]'>Remove</a>"
		ui += "<a href='?src=[REF(src)];hold[!holds[hold_key]]=[url_encode(hold_key)]'>[holds[hold_key] ? "Allow" : "Hold"]</a>"
		ui += " <b>[parent.name]</b> in [get_area_name(parent, TRUE)]<br>"
	if(!connected.len)
		ui += "Nothing!"

	ui += "</div><div class='statusDisplay'><h2>Access Logs:</h2>"
	var/list/logs = GLOB.silo_access_logs[REF(src)]
	var/len = LAZYLEN(logs)
	var/num_pages = 1 + round((len - 1) / 30)
	var/page = CLAMP(log_page, 1, num_pages)
	if(num_pages > 1)
		for(var/i in 1 to num_pages)
			if(i == page)
				ui += "<span class='linkOff'>[i]</span>"
			else
				ui += "<a href='?src=[REF(src)];page=[i]'>[i]</a>"

	ui += "<ol>"
	any = FALSE
	for(var/i in (page - 1) * 30 + 1 to min(page * 30, len))
		var/datum/ore_silo_log/entry = logs[i]
		ui += "<li value=[len + 1 - i]>[entry.formatted]</li>"
		any = TRUE
	if (!any)
		ui += "<li>Nothing!</li>"

	ui += "</ol></div>"
	return ui.Join()

/obj/machinery/ore_silo/Topic(href, href_list)
	if(..())
		return
	add_fingerprint(usr)
	usr.set_machine(src)

	if(href_list["remove"])
		var/datum/component/remote_materials/mats = locate(href_list["remove"]) in connected
		if (mats)
			mats.disconnect_from(src)
			connected -= mats
			updateUsrDialog()
			return TRUE
	else if(href_list["hold1"])
		holds[href_list["hold1"]] = TRUE
		updateUsrDialog()
		return TRUE
	else if(href_list["hold0"])
		holds -= href_list["hold0"]
		updateUsrDialog()
		return TRUE
	else if(href_list["ejectsheet"])
		var/datum/material/eject_sheet = locate(href_list["ejectsheet"])
		var/datum/component/material_container/materials = GetComponent(/datum/component/material_container)
		var/count = materials.retrieve_sheets(text2num(href_list["eject_amt"]), eject_sheet, drop_location())
		var/list/matlist = list()
		matlist[eject_sheet] = MINERAL_MATERIAL_AMOUNT
		silo_log(src, "ejected", -count, "sheets", matlist)
		return TRUE
	else if(href_list["page"])
		log_page = text2num(href_list["page"]) || 1
		updateUsrDialog()
		return TRUE

/obj/machinery/ore_silo/multitool_act(mob/living/user, obj/item/multitool/I)
	. = ..()
	if (istype(I))
		to_chat(user, "<span class='notice'>You log [src] in the multitool's buffer.</span>")
		I.buffer = src
		return TRUE

/obj/machinery/ore_silo/proc/silo_log(obj/machinery/M, action, amount, noun, list/mats)
	if (!length(mats))
		return
	var/datum/ore_silo_log/entry = new(M, action, amount, noun, mats)

	var/list/datum/ore_silo_log/logs = GLOB.silo_access_logs[REF(src)]
	if(!LAZYLEN(logs))
		GLOB.silo_access_logs[REF(src)] = logs = list(entry)
	else if(!logs[1].merge(entry))
		logs.Insert(1, entry)

	updateUsrDialog()
	flick("silo_active", src)

/obj/machinery/ore_silo/examine(mob/user)
	. = ..()
	. += "<span class='notice'>[src] can be linked to techfabs, circuit printers and protolathes with a multitool.</span>"

/datum/ore_silo_log
	var/name  // for VV
	var/formatted  // for display

	var/timestamp
	var/machine_name
	var/area_name
	var/action
	var/noun
	var/amount
	var/list/materials

/datum/ore_silo_log/New(obj/machinery/M, _action, _amount, _noun, list/mats=list())
	timestamp = station_time_timestamp()
	machine_name = M.name
	area_name = get_area_name(M, TRUE)
	action = _action
	amount = _amount
	noun = _noun
	materials = mats.Copy()
	for(var/each in materials)
		materials[each] *= abs(_amount)
	format()

/datum/ore_silo_log/proc/merge(datum/ore_silo_log/other)
	if (other == src || action != other.action || noun != other.noun)
		return FALSE
	if (machine_name != other.machine_name || area_name != other.area_name)
		return FALSE

	timestamp = other.timestamp
	amount += other.amount
	for(var/each in other.materials)
		materials[each] += other.materials[each]
	format()
	return TRUE

/datum/ore_silo_log/proc/format()
	name = "[machine_name]: [action] [amount]x [noun]"

	var/list/msg = list("([timestamp]) <b>[machine_name]</b> in [area_name]<br>[action] [abs(amount)]x [noun]<br>")
	var/sep = ""
	for(var/key in materials)
		var/val = round(materials[key]) / MINERAL_MATERIAL_AMOUNT
		msg += sep
		sep = ", "
		msg += "[amount < 0 ? "-" : "+"][val] [copytext(key, length(key[1]) + 1)]"
	formatted = msg.Join()
