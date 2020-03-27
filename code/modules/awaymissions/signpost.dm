/*An alternative to exit gateways, signposts send you back to somewhere safe onstation with their semiotic magic.*/
/obj/structure/signpost
	icon = 'icons/obj/stationobjs.dmi'
	icon_state = "signpost"
	anchored = TRUE
	density = TRUE
	var/question = "Travel back?"
	var/list/zlevels

/obj/structure/signpost/Initialize()
	. = ..()
	set_light(2)
	zlevels = SSmapping.levels_by_trait(ZTRAIT_STATION)

/obj/structure/signpost/interact(mob/user)
	. = ..()
	if(.)
		return
	if(alert(question,name,"Yes","No") == "Yes" && Adjacent(user))
		var/turf/T = find_safe_turf(zlevels=zlevels)

		if(T)
			var/atom/movable/AM = user.pulling
			if(AM)
				AM.forceMove(T)
			user.forceMove(T)
			if(AM)
				user.start_pulling(AM)
			to_chat(user, "<span class='notice'>You blink and find yourself in [get_area_name(T)].</span>")
		else
			to_chat(user, "Nothing happens. You feel that this is a bad sign.")

/obj/structure/signpost/attackby(obj/item/W, mob/user, params)
	return interact(user)

/obj/structure/signpost/attack_paw(mob/user)
	return interact(user)

/obj/structure/signpost/attack_hulk(mob/user)
	return

/obj/structure/signpost/attack_larva(mob/user)
	return interact(user)

/obj/structure/signpost/attack_robot(mob/user)
	if (Adjacent(user))
		return interact(user)

/obj/structure/signpost/attack_slime(mob/user)
	return interact(user)

/obj/structure/signpost/attack_animal(mob/user)
	return interact(user)

/obj/structure/signpost/salvation
	name = "\proper salvation"
	desc = "In the darkest times, we will find our way home."
	resistance_flags = INDESTRUCTIBLE

/obj/structure/signpost/exit
	name = "exit"
	desc = "Make sure to bring all your belongings with you when you \
		exit the area."
	question = "Leave? You might never come back."

/obj/structure/signpost/exit/Initialize()
	. = ..()
	zlevels = list()
	for(var/i in 1 to world.maxz)
		zlevels += i
	zlevels -= SSmapping.levels_by_trait(ZTRAIT_CENTCOM) // no easy victory, even with meme signposts
	// also, could you think of the horror if they ended up in a holodeck
	// template or something
