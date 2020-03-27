#define DEBRIS_DENSITY (length(core.contents) / length(overmind.blobs_legit)) // items per blob

// Accumulates junk liberally
/datum/blobstrain/debris_devourer
	name = "Debris Devourer"
	description = "will launch accumulated debris into targets."
	analyzerdescdamage = "Does medium brute damage and may grab onto melee weapons."
	analyzerdesceffect = "Devours loose items left on the station, and releases them when attacking or attacked."
	color = "#8B1000"
	complementary_color = "#00558B"
	blobbernaut_message = "blasts"
	message = "The blob blasts you"


/datum/blobstrain/debris_devourer/attack_living(mob/living/L, list/nearby_blobs)
	send_message(L)
	for (var/obj/structure/blob/blob in nearby_blobs)
		debris_attack(L, blob)

/datum/blobstrain/debris_devourer/on_sporedeath(mob/living/spore)
	for(var/i in 1 to 3)
		var/obj/item/I = locate() in overmind.blob_core
		if (I && !QDELETED(I))
			I.forceMove(get_turf(spore))
			I.throw_at(get_edge_target_turf(spore,pick(GLOB.alldirs)), 3, 5)

/datum/blobstrain/debris_devourer/expand_reaction(obj/structure/blob/B, obj/structure/blob/newB, turf/T, mob/camera/blob/O, coefficient = 1) //when the blob expands, do this
	for (var/obj/item/I in T)
		I.forceMove(overmind.blob_core)

/datum/blobstrain/debris_devourer/proc/debris_attack(mob/living/L, source)
	var/obj/structure/blob/core/core = overmind.blob_core
	if (prob(20 * DEBRIS_DENSITY)) // Pretend the items are spread through the blob and its mobs and not in the core.
		var/obj/item/I = locate() in core
		if (I && !QDELETED(I))
			I.forceMove(get_turf(source))
			I.throw_at(L, 2, 5)

/datum/blobstrain/debris_devourer/blobbernaut_attack(mob/living/L, mob/living/blobbernaut) // When this blob's blobbernaut attacks people
	debris_attack(L,blobbernaut)

/datum/blobstrain/debris_devourer/damage_reaction(obj/structure/blob/B, damage, damage_type, damage_flag, coefficient = 1) //when the blob takes damage, do this
	var/obj/structure/blob/core/core = overmind.blob_core
	return round(max((coefficient*damage)-min(coefficient*DEBRIS_DENSITY, 10), 0)) // reduce damage taken by items per blob, up to 10

/datum/blobstrain/debris_devourer/examine(mob/user)
	. = ..()
	var/obj/structure/blob/core/core = overmind.blob_core
	if (isobserver(user))
		. += "<span class='notice'>Absorbed debris is currently reducing incoming damage by [round(max(min(DEBRIS_DENSITY, 10),0))]</span>"
	else
		switch (round(max(min(DEBRIS_DENSITY, 10),0)))
			if (0)
				. += "<span class='notice'>There is not currently enough absorbed debris to reduce damage.</span>"
			if (1 to 3)
				. += "<span class='notice'>Absorbed debris is currently reducing incoming damage by a very low amount.</span>" // these roughly correspond with force description strings
			if (4 to 7)
				. += "<span class='notice'>Absorbed debris is currently reducing incoming damage by a low amount.</span>"
			if (8 to 10)
				. += "<span class='notice'>Absorbed debris is currently reducing incoming damage by a medium amount.</span>"


#undef DEBRIS_DENSITY
