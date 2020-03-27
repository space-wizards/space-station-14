/mob/living/simple_animal/pet/gondola/gondolapod
	name = "gondola"
	real_name = "gondola"
	desc = "The silent walker. This one seems to be part of a delivery agency."
	response_help_continuous = "pets"
	response_help_simple = "pet"
	response_disarm_continuous = "bops"
	response_disarm_simple = "bop"
	response_harm_continuous = "kicks"
	response_harm_simple = "kick"
	faction = list("gondola")
	turns_per_move = 10
	icon = 'icons/mob/gondolapod.dmi'
	icon_state = "gondolapod"
	icon_living = "gondolapod"
	pixel_x = -16//2x2 sprite
	pixel_y = -5
	layer = TABLE_LAYER//so that deliveries dont appear underneath it
	loot = list(/obj/effect/decal/cleanable/blood/gibs, /obj/item/stack/sheet/animalhide/gondola = 2, /obj/item/reagent_containers/food/snacks/meat/slab/gondola = 2)
	//Gondolas aren't affected by cold.
	atmos_requirements = list("min_oxy" = 0, "max_oxy" = 0, "min_tox" = 0, "max_tox" = 0, "min_co2" = 0, "max_co2" = 0, "min_n2" = 0, "max_n2" = 0)
	minbodytemp = 0
	maxbodytemp = 1500
	maxHealth = 200
	health = 200
	del_on_death = TRUE
	var/opened = FALSE
	var/obj/structure/closet/supplypod/centcompod/linked_pod

/mob/living/simple_animal/pet/gondola/gondolapod/Initialize(mapload, pod)
	linked_pod = pod
	name = linked_pod.name
	. = ..()

/mob/living/simple_animal/pet/gondola/gondolapod/update_icon_state()
	if(opened)
		icon_state = "gondolapod_open"
	else
		icon_state = "gondolapod"

/mob/living/simple_animal/pet/gondola/gondolapod/verb/deliver()
	set name = "Release Contents"
	set category = "Gondola"
	set desc = "Release any contents stored within your vast belly."
	linked_pod.open(src, forced = TRUE)

/mob/living/simple_animal/pet/gondola/gondolapod/examine(mob/user)
	. = ..()
	if (contents.len)
		. += "<span class='notice'>It looks like it hasn't made its delivery yet.</b></span>"
	else
		. += "<span class='notice'>It looks like it has already made its delivery.</b></span>"

/mob/living/simple_animal/pet/gondola/gondolapod/verb/check()
	set name = "Count Contents"
	set category = "Gondola"
	set desc = "Take a deep look inside youself, and count up what's inside"
	var/total = contents.len
	if (total)
		to_chat(src, "<span class='notice'>You detect [total] object\s within your incredibly vast belly.</span>")
	else
		to_chat(src, "<span class='notice'>A closer look inside yourself reveals... nothing.</span>")

/mob/living/simple_animal/pet/gondola/gondolapod/proc/setOpened()
	opened = TRUE
	update_icon()
	addtimer(CALLBACK(src, .proc/setClosed), 50)

/mob/living/simple_animal/pet/gondola/gondolapod/proc/setClosed()
	opened = FALSE
	update_icon()

/mob/living/simple_animal/pet/gondola/gondolapod/death()
	qdel(linked_pod) //Will cause the open() proc for the linked supplypod to be called with the "broken" parameter set to true, meaning that it will dump its contents on death
	qdel(src)
	..()
