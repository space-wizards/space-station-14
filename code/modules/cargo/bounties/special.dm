/datum/bounty/item/alien_organs
	name = "Alien Organs"
	description = "Nanotrasen is interested in studying Xenomorph biology. Ship a set of organs to be thoroughly compensated."
	reward = 25000
	required_count = 3
	wanted_types = list(/obj/item/organ/brain/alien, /obj/item/organ/alien, /obj/item/organ/body_egg/alien_embryo, /obj/item/organ/liver/alien, /obj/item/organ/tongue/alien, /obj/item/organ/eyes/night_vision/alien)

/datum/bounty/item/syndicate_documents
	name = "Syndicate Documents"
	description = "Intel regarding the syndicate is highly prized at CentCom. If you find syndicate documents, ship them. You could save lives."
	reward = 15000
	wanted_types = list(/obj/item/documents/syndicate, /obj/item/documents/photocopy)

/datum/bounty/item/syndicate_documents/applies_to(obj/O)
	if(!..())
		return FALSE
	if(istype(O, /obj/item/documents/photocopy))
		var/obj/item/documents/photocopy/Copy = O
		return (Copy.copy_type && ispath(Copy.copy_type, /obj/item/documents/syndicate))
	return TRUE

/datum/bounty/item/adamantine
	name = "Adamantine"
	description = "Nanotrasen's anomalous materials division is in desparate need for Adamantine. Send them a large shipment and we'll make it worth your while."
	reward = 35000
	required_count = 10
	wanted_types = list(/obj/item/stack/sheet/mineral/adamantine)

/datum/bounty/item/trash
	name = "Trash"
	description = "Recently a group of janitors have run out of trash to clean up, without any trash Centcom wants to fire them to cut costs. Send a shipment of trash to keep them employed, and they'll give you a small compensation."
	reward = 1000
	required_count = 10
	wanted_types = list(/obj/item/trash)

/datum/bounty/more_bounties
	name = "More Bounties"
	description = "Complete enough bounties and CentCom will issue new ones!"
	reward = 5 // number of bounties
	var/required_bounties = 5

/datum/bounty/more_bounties/can_claim()
	return ..() && completed_bounty_count() >= required_bounties

/datum/bounty/more_bounties/completion_string()
	return "[min(required_bounties, completed_bounty_count())]/[required_bounties] Bounties"

/datum/bounty/more_bounties/reward_string()
	return "Up to [reward] new bounties"

/datum/bounty/more_bounties/claim()
	if(can_claim())
		claimed = TRUE
		for(var/i = 0; i < reward; ++i)
			try_add_bounty(random_bounty())
