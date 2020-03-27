#define GONDOLA_HEIGHT pick("gondola_body_long", "gondola_body_medium", "gondola_body_short")
#define GONDOLA_COLOR pick("A87855", "915E48", "683E2C")
#define GONDOLA_MOUSTACHE pick("gondola_moustache_large", "gondola_moustache_small")
#define GONDOLA_EYES pick("gondola_eyes_close", "gondola_eyes_far")

//Gondolas

/mob/living/simple_animal/pet/gondola
	name = "gondola"
	real_name = "gondola"
	desc = "Gondola is the silent walker. Having no hands he embodies the Taoist principle of wu-wei (non-action) while his smiling facial expression shows his utter and complete acceptance of the world as it is. Its hide is extremely valuable."
	response_help_continuous = "pets"
	response_help_simple = "pet"
	response_disarm_continuous = "bops"
	response_disarm_simple = "bop"
	response_harm_continuous = "kicks"
	response_harm_simple = "kick"
	faction = list("gondola")
	turns_per_move = 10
	icon = 'icons/mob/gondolas.dmi'
	icon_state = "gondola"
	icon_living = "gondola"
	loot = list(/obj/effect/decal/cleanable/blood/gibs, /obj/item/stack/sheet/animalhide/gondola = 1, /obj/item/reagent_containers/food/snacks/meat/slab/gondola = 1)
	//Gondolas aren't affected by cold.
	atmos_requirements = list("min_oxy" = 0, "max_oxy" = 0, "min_tox" = 0, "max_tox" = 0, "min_co2" = 0, "max_co2" = 0, "min_n2" = 0, "max_n2" = 0)
	minbodytemp = 0
	maxbodytemp = 1500
	maxHealth = 200
	health = 200
	del_on_death = TRUE

	//Gondolas don't make footstep sounds

/mob/living/simple_animal/pet/gondola/Initialize()
	. = ..()
	if (!(istype(src, /mob/living/simple_animal/pet/gondola/gondolapod)))
		CreateGondola()

/mob/living/simple_animal/pet/gondola/proc/CreateGondola()
	icon_state = null
	icon_living = null
	var/height = GONDOLA_HEIGHT
	var/mutable_appearance/body_overlay = mutable_appearance(icon, height)
	var/mutable_appearance/eyes_overlay = mutable_appearance(icon, GONDOLA_EYES)
	var/mutable_appearance/moustache_overlay = mutable_appearance(icon, GONDOLA_MOUSTACHE)
	body_overlay.color = ("#[GONDOLA_COLOR]")

	//Offset the face to match the Gondola's height.
	switch(height)
		if("gondola_body_medium")
			eyes_overlay.pixel_y = -4
			moustache_overlay.pixel_y = -4
		if("gondola_body_short")
			eyes_overlay.pixel_y = -8
			moustache_overlay.pixel_y = -8

	cut_overlays(TRUE)
	add_overlay(body_overlay)
	add_overlay(eyes_overlay)
	add_overlay(moustache_overlay)

/mob/living/simple_animal/pet/gondola/IsVocal() //Gondolas are the silent walker.
	return FALSE

#undef GONDOLA_HEIGHT
#undef GONDOLA_COLOR
#undef GONDOLA_MOUSTACHE
#undef GONDOLA_EYES
