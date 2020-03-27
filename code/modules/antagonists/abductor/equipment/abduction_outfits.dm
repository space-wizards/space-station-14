/datum/outfit/abductor
	name = "Abductor Basic"
	uniform = /obj/item/clothing/under/abductor
	shoes = /obj/item/clothing/shoes/combat
	back = /obj/item/storage/backpack
	ears = /obj/item/radio/headset/abductor

/datum/outfit/abductor/proc/link_to_console(mob/living/carbon/human/H, team_number)
	var/datum/antagonist/abductor/A = H.mind.has_antag_datum(/datum/antagonist/abductor)
	if(!team_number && A)
		team_number = A.team.team_number
	if(!team_number)
		team_number = 1

	var/obj/machinery/abductor/console/console = get_abductor_console(team_number)
	if(console)
		var/obj/item/clothing/suit/armor/abductor/vest/V = locate() in H
		if(V)
			console.AddVest(V)
			ADD_TRAIT(V, TRAIT_NODROP, ABDUCTOR_VEST_TRAIT)

		var/obj/item/storage/backpack/B = locate() in H
		if(B)
			for(var/obj/item/abductor/gizmo/G in B.contents)
				console.AddGizmo(G)

/datum/outfit/abductor/post_equip(mob/living/carbon/human/H, visualsOnly = FALSE)
	..()
	if(!visualsOnly)
		link_to_console(H)


/datum/outfit/abductor/agent
	name = "Abductor Agent"
	head = /obj/item/clothing/head/helmet/abductor
	suit = /obj/item/clothing/suit/armor/abductor/vest
	suit_store = /obj/item/abductor/baton
	belt = /obj/item/storage/belt/military/abductor/full

	backpack_contents = list(
		/obj/item/gun/energy/alien = 1,
		/obj/item/abductor/silencer = 1
		)

/datum/outfit/abductor/scientist
	name = "Abductor Scientist"

	backpack_contents = list(
		/obj/item/abductor/gizmo = 1
		)

/datum/outfit/abductor/scientist/post_equip(mob/living/carbon/human/H, visualsOnly = FALSE)
	..()
	if(!visualsOnly)
		var/obj/item/implant/abductor/beamplant = new /obj/item/implant/abductor(H)
		beamplant.implant(H)

/datum/outfit/abductor/scientist/onemanteam
	name = "Abductor Scientist (w/ agent gear)"
	head = /obj/item/clothing/head/helmet/abductor
	suit = /obj/item/clothing/suit/armor/abductor/vest
	suit_store = /obj/item/abductor/baton
	belt = /obj/item/storage/belt/military/abductor/full

	backpack_contents = list(
	/obj/item/abductor/gizmo = 1,
	/obj/item/gun/energy/alien = 1,
	/obj/item/abductor/silencer = 1
	)
