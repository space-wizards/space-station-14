/mob/living/simple_animal/pet
	icon = 'icons/mob/pets.dmi'
	mob_size = MOB_SIZE_SMALL
	mob_biotypes = MOB_ORGANIC|MOB_BEAST
	blood_volume = BLOOD_VOLUME_NORMAL
	var/unique_pet = FALSE // if the mob can be renamed
	var/obj/item/clothing/neck/petcollar/pcollar
	var/collar_type //if the mob has collar sprites, define them.

/mob/living/simple_animal/pet/handle_atom_del(atom/A)
	if(A == pcollar)
		pcollar = null
	return ..()

/mob/living/simple_animal/pet/proc/add_collar(obj/item/clothing/neck/petcollar/P, mob/user)
	if(QDELETED(P) || pcollar)
		return
	if(!user.transferItemToLoc(P, src))
		return
	pcollar = P
	regenerate_icons()
	to_chat(user, "<span class='notice'>You put the [P] around [src]'s neck.</span>")
	if(P.tagname && !unique_pet)
		fully_replace_character_name(null, "\proper [P.tagname]")

/mob/living/simple_animal/pet/attackby(obj/item/O, mob/user, params)
	if(istype(O, /obj/item/clothing/neck/petcollar) && !pcollar)
		add_collar(O, user)
		return

	if(istype(O, /obj/item/newspaper))
		if(!stat)
			user.visible_message("<span class='notice'>[user] baps [name] on the nose with the rolled up [O].</span>")
			dance_rotate(src)
	else
		..()

/mob/living/simple_animal/pet/Initialize()
	. = ..()
	if(pcollar)
		pcollar = new(src)
		regenerate_icons()

/mob/living/simple_animal/pet/Destroy()
	QDEL_NULL(pcollar)
	return ..()

/mob/living/simple_animal/pet/revive(full_heal = FALSE, admin_revive = FALSE)
	. = ..()
	if(.)
		if(collar_type)
			collar_type = "[initial(collar_type)]"
		regenerate_icons()

/mob/living/simple_animal/pet/death(gibbed)
	..(gibbed)
	if(collar_type)
		collar_type = "[initial(collar_type)]_dead"
	regenerate_icons()

/mob/living/simple_animal/pet/gib()
	if(pcollar)
		pcollar.forceMove(drop_location())
		pcollar = null
	..()

/mob/living/simple_animal/pet/regenerate_icons()
	cut_overlays()
	if(pcollar && collar_type)
		add_overlay("[collar_type]collar")
		add_overlay("[collar_type]tag")
