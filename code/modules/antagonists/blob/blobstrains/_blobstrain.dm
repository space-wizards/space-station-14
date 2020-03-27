GLOBAL_LIST_INIT(valid_blobstrains, subtypesof(/datum/blobstrain) - list(/datum/blobstrain/reagent, /datum/blobstrain/multiplex))

/datum/blobstrain
	var/name
	var/description
	var/color = "#000000"
	var/complementary_color = "#000000" //a color that's complementary to the normal blob color
	var/shortdesc = null //just damage and on_mob effects, doesn't include special, blob-tile only effects
	var/effectdesc = null //any long, blob-tile specific effects
	var/analyzerdescdamage = "Unknown. Report this bug to a coder, or just adminhelp."
	var/analyzerdesceffect = "N/A"
	var/blobbernaut_message = "slams" //blobbernaut attack verb
	var/message = "The blob strikes you" //message sent to any mob hit by the blob
	var/message_living = null //extension to first mob sent to only living mobs i.e. silicons have no skin to be burnt
	var/core_regen = 2
	var/resource_delay = 0
	var/point_rate = 2
	var/mob/camera/blob/overmind

/datum/blobstrain/New(mob/camera/blob/new_overmind)
	if (!istype(new_overmind))
		stack_trace("blobstrain created without overmind")
	overmind = new_overmind

/datum/blobstrain/proc/on_gain()
	overmind.color = complementary_color
	for(var/BL in GLOB.blobs)
		var/obj/structure/blob/B = BL
		B.update_icon()
	for(var/BLO in overmind.blob_mobs)
		var/mob/living/simple_animal/hostile/blob/BM = BLO
		BM.update_icons() //If it's getting a new strain, tell it what it does!
		to_chat(BM, "Your overmind's blob strain is now: <b><font color=\"[color]\">[name]</b></font>!")
		to_chat(BM, "The <b><font color=\"[color]\">[name]</b></font> strain [shortdesc ? "[shortdesc]" : "[description]"]")

/datum/blobstrain/proc/on_lose()

/datum/blobstrain/proc/on_sporedeath(mob/living/spore)

/datum/blobstrain/proc/send_message(mob/living/M)
	var/totalmessage = message
	if(message_living && !issilicon(M))
		totalmessage += message_living
	totalmessage += "!"
	to_chat(M, "<span class='userdanger'>[totalmessage]</span>")

/datum/blobstrain/proc/core_process()
	if(resource_delay <= world.time)
		resource_delay = world.time + 10 // 1 second
		overmind.add_points(point_rate)
	overmind.blob_core.obj_integrity = min(overmind.blob_core.max_integrity, overmind.blob_core.obj_integrity+core_regen)

/datum/blobstrain/proc/attack_living(mob/living/L, list/nearby_blobs) // When the blob attacks people
	send_message(L)

/datum/blobstrain/proc/blobbernaut_attack(mob/living/L, blobbernaut) // When this blob's blobbernaut attacks people

/datum/blobstrain/proc/damage_reaction(obj/structure/blob/B, damage, damage_type, damage_flag, coefficient = 1) //when the blob takes damage, do this
	return coefficient*damage

/datum/blobstrain/proc/death_reaction(obj/structure/blob/B, damage_flag, coefficient = 1) //when a blob dies, do this
	return

/datum/blobstrain/proc/expand_reaction(obj/structure/blob/B, obj/structure/blob/newB, turf/T, mob/camera/blob/O, coefficient = 1) //when the blob expands, do this
	return

/datum/blobstrain/proc/tesla_reaction(obj/structure/blob/B, power, coefficient = 1) //when the blob is hit by a tesla bolt, do this
	return 1 //return 0 to ignore damage

/datum/blobstrain/proc/extinguish_reaction(obj/structure/blob/B, coefficient = 1) //when the blob is hit with water, do this
	return

/datum/blobstrain/proc/emp_reaction(obj/structure/blob/B, severity, coefficient = 1) //when the blob is hit with an emp, do this
	return

/datum/blobstrain/proc/examine(mob/user)
	return list("<b>Progress to Critical Mass:</b> <span class='notice'>[overmind.blobs_legit.len]/[overmind.blobwincount].</span>")
