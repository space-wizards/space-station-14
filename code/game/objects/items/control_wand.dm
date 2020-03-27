#define WAND_OPEN "Open Door"
#define WAND_BOLT "Toggle Bolts"
#define WAND_EMERGENCY "Toggle Emergency Access"

/obj/item/door_remote
	icon_state = "gangtool-white"
	item_state = "electronic"
	lefthand_file = 'icons/mob/inhands/misc/devices_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/misc/devices_righthand.dmi'
	icon = 'icons/obj/device.dmi'
	name = "control wand"
	desc = "Remotely controls airlocks."
	w_class = WEIGHT_CLASS_TINY
	var/mode = WAND_OPEN
	var/region_access = 1 //See access.dm
	var/list/access_list

/obj/item/door_remote/Initialize()
	. = ..()
	access_list = get_region_accesses(region_access)
	AddComponent(/datum/component/ntnet_interface)

/obj/item/door_remote/attack_self(mob/user)
	switch(mode)
		if(WAND_OPEN)
			mode = WAND_BOLT
		if(WAND_BOLT)
			mode = WAND_EMERGENCY
		if(WAND_EMERGENCY)
			mode = WAND_OPEN
	to_chat(user, "<span class='notice'>Now in mode: [mode].</span>")

// Airlock remote works by sending NTNet packets to whatever it's pointed at.
/obj/item/door_remote/afterattack(atom/A, mob/user)
	. = ..()
	var/datum/component/ntnet_interface/target_interface = A.GetComponent(/datum/component/ntnet_interface)

	if(!target_interface)
		return

	// Generate a control packet.
	var/datum/netdata/data = new
	data.recipient_ids = list(target_interface.hardware_id)

	switch(mode)
		if(WAND_OPEN)
			data.data["data"] = "open"
		if(WAND_BOLT)
			data.data["data"] = "bolt"
		if(WAND_EMERGENCY)
			data.data["data"] = "emergency"

	data.data["data_secondary"] = "toggle"
	data.passkey = access_list

	ntnet_send(data)


/obj/item/door_remote/omni
	name = "omni door remote"
	desc = "This control wand can access any door on the station."
	icon_state = "gangtool-yellow"
	region_access = 0

/obj/item/door_remote/captain
	name = "command door remote"
	icon_state = "gangtool-yellow"
	region_access = 7

/obj/item/door_remote/chief_engineer
	name = "engineering door remote"
	icon_state = "gangtool-orange"
	region_access = 5

/obj/item/door_remote/research_director
	name = "research door remote"
	icon_state = "gangtool-purple"
	region_access = 4

/obj/item/door_remote/head_of_security
	name = "security door remote"
	icon_state = "gangtool-red"
	region_access = 2

/obj/item/door_remote/quartermaster
	name = "supply door remote"
	desc = "Remotely controls airlocks. This remote has additional Vault access."
	icon_state = "gangtool-green"
	region_access = 6

/obj/item/door_remote/chief_medical_officer
	name = "medical door remote"
	icon_state = "gangtool-blue"
	region_access = 3

/obj/item/door_remote/civillian
	name = "civilian door remote"
	icon_state = "gangtool-white"
	region_access = 1

#undef WAND_OPEN
#undef WAND_BOLT
#undef WAND_EMERGENCY
