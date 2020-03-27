///////////	thederelict items

/obj/item/paper/fluff/ruins/thederelict/equipment
	info = "If the equipment breaks there should be enough spare parts in our engineering storage near the north east solar array."
	name = "Equipment Inventory"

/obj/item/paper/fluff/ruins/thederelict/syndie_mission
	name = "Mission Objectives"
	info = "The Syndicate have cunningly disguised a Syndicate Uplink as your PDA. Simply enter the code \"678 Bravo\" into the ringtone select to unlock its hidden features. <br><br><b>Objective #1</b>. Kill the God damn AI in a fire blast that it rocks the station. <b>Success!</b>  <br><b>Objective #2</b>. Escape alive. <b>Failed.</b>"

/obj/item/paper/fluff/ruins/thederelict/nukie_objectives
	name = "Objectives of a Nuclear Operative"
	info = "<b>Objective #1</b>: Destroy the station with a nuclear device."

/obj/item/paper/crumpled/bloody/ruins/thederelict/unfinished
	name = "unfinished paper scrap"
	desc = "Looks like someone started shakily writing a will in space common, but were interrupted by something bloody..."
	info = "I, Victor Belyakov, do hereby leave my _- "
/obj/item/paper/fluff/ruins/thederelict/vaultraider
	name = "Vault Raider Objectives"
	info = "<b>Objectives #1</b>: Find out what is hidden in Kosmicheskaya Stantsiya 13s Vault"


/// Vault controller for use on the derelict/KS13.
/obj/machinery/computer/vaultcontroller
	name = "vault controller"
	desc = "It seems to be powering and controlling the vault locks."
	icon_screen = "power"
	icon_keyboard = "power_key"
	light_color = LIGHT_COLOR_YELLOW
	use_power = NO_POWER_USE
	resistance_flags = INDESTRUCTIBLE | LAVA_PROOF | FIRE_PROOF | UNACIDABLE | ACID_PROOF

	var/obj/structure/cable/attached_cable
	var/obj/machinery/door/airlock/vault/derelict/door1
	var/obj/machinery/door/airlock/vault/derelict/door2
	var/locked = TRUE
	var/siphoned_power = 0
	var/siphon_max = 1e7

	ui_x = 300
	ui_y = 120


/obj/machinery/computer/monitor/examine(mob/user)
	. = ..()
	. += "<span class='notice'>It appears to be powered via a cable connector.</span>"


//Checks for cable connection, charges if possible.
/obj/machinery/computer/vaultcontroller/process()
	if(siphoned_power >= siphon_max)
		return
	update_cable()
	if(attached_cable)
		attempt_siphon()


///Looks for a cable connection beneath the machine.
/obj/machinery/computer/vaultcontroller/proc/update_cable()
	var/turf/T = get_turf(src)
	attached_cable = locate(/obj/structure/cable) in T


///Initializes airlock links.
/obj/machinery/computer/vaultcontroller/proc/find_airlocks()
	for(var/obj/machinery/door/airlock/A in GLOB.airlocks)
		if(A.id_tag == "derelictvault")
			if(!door1)
				door1 = A
				continue
			if(door1 && !door2)
				door2 = A
				break


///Tries to charge from powernet excess, no upper limit except max charge.
/obj/machinery/computer/vaultcontroller/proc/attempt_siphon()
	var/surpluspower = CLAMP(attached_cable.surplus(), 0, (siphon_max - siphoned_power))
	if(surpluspower)
		attached_cable.add_load(surpluspower)
		siphoned_power += surpluspower


///Handles the doors closing
/obj/machinery/computer/vaultcontroller/proc/cycle_close(obj/machinery/door/airlock/A)
	A.safe = FALSE //Make sure its forced closed, always
	A.unbolt()
	A.close()
	A.bolt()


///Handles the doors opening
/obj/machinery/computer/vaultcontroller/proc/cycle_open(obj/machinery/door/airlock/A)
	A.unbolt()
	A.open()
	A.bolt()


///Attempts to lock the vault doors
/obj/machinery/computer/vaultcontroller/proc/lock_vault()
	if(door1 && !door1.density)
		cycle_close(door1)
	if(door2 && !door2.density)
		cycle_close(door2)
	if(door1.density && door1.locked && door2.density && door2.locked)
		locked = TRUE


///Attempts to unlock the vault doors
/obj/machinery/computer/vaultcontroller/proc/unlock_vault()
	if(door1 && door1.density)
		cycle_open(door1)
	if(door2 && door2.density)
		cycle_open(door2)
	if(!door1.density && door1.locked && !door2.density && door2.locked)
		locked = FALSE


///Attempts to lock/unlock vault doors, if machine is charged.
/obj/machinery/computer/vaultcontroller/proc/activate_lock()
	if(siphoned_power < siphon_max)
		return
	if(!door1 || !door2)
		find_airlocks()
	if(locked)
		unlock_vault()
	else
		lock_vault()


/obj/machinery/computer/vaultcontroller/ui_interact(mob/user, ui_key = "main", datum/tgui/ui = null, force_open = FALSE, \
											datum/tgui/master_ui = null, datum/ui_state/state = GLOB.default_state)
	ui = SStgui.try_update_ui(user, src, ui_key, ui, force_open)
	if(!ui)
		ui = new(user, src, ui_key, "vault_controller", name, ui_x, ui_y, master_ui, state)
		ui.open()


/obj/machinery/computer/vaultcontroller/ui_act(action, params)
	if(..())
		return
	switch(action)
		if("togglelock")
			activate_lock()


/obj/machinery/computer/vaultcontroller/ui_data()
	var/list/data = list()
	data["stored"] = siphoned_power
	data["max"] = siphon_max
	data["doorstatus"] = locked
	return data


///Airlock that can't be deconstructed, broken or hacked.
/obj/machinery/door/airlock/vault/derelict
	locked = TRUE
	move_resist = INFINITY
	use_power = NO_POWER_USE
	resistance_flags = INDESTRUCTIBLE | LAVA_PROOF | FIRE_PROOF | UNACIDABLE | ACID_PROOF
	id_tag = "derelictvault"


///Overrides screwdriver attack to prevent all deconstruction and hacking.
/obj/machinery/door/airlock/vault/derelict/attackby(obj/item/C, mob/user, params)
	if(C.tool_behaviour == TOOL_SCREWDRIVER)
		return
	..()


// So drones can teach borgs and AI dronespeak. For best effect, combine with mother drone lawset.
/obj/item/dronespeak_manual
	name = "dronespeak manual"
	desc = "The book's cover reads: \"Understanding Dronespeak - An exercise in futility.\""
	icon = 'icons/obj/library.dmi'
	icon_state = "book2"

/obj/item/dronespeak_manual/attack_self(mob/living/user)
	..()
	if(isdrone(user) || issilicon(user))
		if(user.has_language(/datum/language/drone))
			to_chat(user, "<span class='boldannounce'>You start skimming through [src], but you already know dronespeak.</span>")
		else
			to_chat(user, "<span class='boldannounce'>You start skimming through [src], and suddenly the drone chittering makes sense.</span>")
			user.grant_language(/datum/language/drone, TRUE, TRUE, LANGUAGE_MIND)
		return

	if(user.has_language(/datum/language/drone))
		to_chat(user, "<span class='boldannounce'>You start skimming through [src], but you already know dronespeak.</span>")
	else
		to_chat(user, "<span class='boldannounce'>You start skimming through [src], but you can't make any sense of the contents.</span>")

/obj/item/dronespeak_manual/attack(mob/living/M, mob/living/user)
	if(!istype(M) || !istype(user))
		return
	if(M == user)
		attack_self(user)
		return

	playsound(loc, "punch", 25, TRUE, -1)
	if(isdrone(M) || issilicon(M))
		if(M.has_language(/datum/language/drone))
			M.visible_message("<span class='danger'>[user] beats [M] over the head with [src]!</span>", "<span class='userdanger'>[user] beats you over the head with [src]!</span>", "<span class='hear'>You hear smacking.</span>")
		else
			M.visible_message("<span class='notice'>[user] teaches [M] by beating [M.p_them()] over the head with [src]!</span>", "<span class='boldnotice'>As [user] hits you with [src], chitters resonate in your mind.</span>", "<span class='hear'>You hear smacking.</span>")
			M.grant_language(/datum/language/drone, TRUE, TRUE, LANGUAGE_MIND)
		return

/obj/structure/fluff/oldturret
	name = "broken turret"
	desc = "An obsolete model of turret, long non-functional."
	icon = 'icons/obj/turrets.dmi'
	icon_state = "turretCover"
	density = TRUE
