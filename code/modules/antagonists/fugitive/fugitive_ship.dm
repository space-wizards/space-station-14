//works similar to the experiment machine (experiment.dm) except it just holds more and more prisoners

/obj/machinery/fugitive_capture
	name = "bluespace capture machine"
	desc = "Much, MUCH bigger on the inside to transport prisoners safely."
	icon = 'icons/obj/machines/research.dmi'
	icon_state = "bluespace-prison"
	density = TRUE
	resistance_flags = INDESTRUCTIBLE | LAVA_PROOF | FIRE_PROOF | ACID_PROOF //ha ha no getting out!!

/obj/machinery/fugitive_capture/examine(mob/user)
	. = ..()
	. += "<span class='notice'>Add a prisoner by dragging them into the machine.</span>"

/obj/machinery/fugitive_capture/MouseDrop_T(mob/target, mob/user)
	var/mob/living/fugitive_hunter = user
	if(!isliving(fugitive_hunter))
		return
	if(fugitive_hunter.stat || (!(fugitive_hunter.mobility_flags & MOBILITY_STAND) || !(fugitive_hunter.mobility_flags & MOBILITY_UI)) || !Adjacent(fugitive_hunter) || !target.Adjacent(fugitive_hunter) || !ishuman(target))
		return
	var/mob/living/carbon/human/fugitive = target
	var/datum/antagonist/fugitive/fug_antag = fugitive.mind.has_antag_datum(/datum/antagonist/fugitive)
	if(!fug_antag)
		to_chat(fugitive_hunter, "<span class='warning'>This is not a wanted fugitive!</span>")
		return
	if(do_after(fugitive_hunter, 50, target = fugitive))
		add_prisoner(fugitive, fug_antag)

/obj/machinery/fugitive_capture/proc/add_prisoner(mob/living/carbon/human/fugitive, datum/antagonist/fugitive/antag)
	fugitive.forceMove(src)
	antag.is_captured = TRUE
	to_chat(fugitive, "<span class='userdanger'>You are thrown into a vast void of bluespace, and as you fall further into oblivion the comparatively small entrance to reality gets smaller and smaller until you cannot see it anymore. You have failed to avoid capture.</span>")
	fugitive.ghostize(TRUE) //so they cannot suicide, round end stuff.

/obj/machinery/computer/shuttle/hunter
	name = "shuttle console"
	shuttleId = "huntership"
	possible_destinations = "huntership_home;huntership_custom;whiteship_home;syndicate_nw"

/obj/machinery/computer/camera_advanced/shuttle_docker/syndicate/hunter
	name = "shuttle navigation computer"
	desc = "Used to designate a precise transit location to travel to."
	shuttleId = "huntership"
	lock_override = CAMERA_LOCK_STATION
	shuttlePortId = "huntership_custom"
	see_hidden = FALSE
	jumpto_ports = list("huntership_home" = 1, "whiteship_home" = 1, "syndicate_nw" = 1)
	view_range = 12

/obj/structure/closet/crate/eva
	name = "EVA crate"

/obj/structure/closet/crate/eva/PopulateContents()
	..()
	for(var/i in 1 to 3)
		new /obj/item/clothing/suit/space/eva(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/head/helmet/space/eva(src)
	for(var/i in 1 to 3)
		new /obj/item/clothing/mask/breath(src)
	for(var/i in 1 to 3)
		new /obj/item/tank/internals/oxygen(src)
