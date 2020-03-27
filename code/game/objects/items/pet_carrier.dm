#define pet_carrier_full(carrier) carrier.occupants.len >= carrier.max_occupants || carrier.occupant_weight >= carrier.max_occupant_weight

//Used to transport little animals without having to drag them across the station.
//Comes with a handy lock to prevent them from running off.
/obj/item/pet_carrier
	name = "pet carrier"
	desc = "A big white-and-blue pet carrier. Good for carrying <s>meat to the chef</s> cute animals around."
	icon = 'icons/obj/pet_carrier.dmi'
	icon_state = "pet_carrier_open"
	item_state = "pet_carrier"
	lefthand_file = 'icons/mob/inhands/items_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/items_righthand.dmi'
	force = 5
	attack_verb = list("bashed", "carried")
	w_class = WEIGHT_CLASS_BULKY
	throw_speed = 2
	throw_range = 3
	custom_materials = list(/datum/material/iron = 7500, /datum/material/glass = 100)
	var/open = TRUE
	var/locked = FALSE
	var/list/occupants = list()
	var/occupant_weight = 0
	var/max_occupants = 3 //Hard-cap so you can't have infinite mice or something in one carrier
	var/max_occupant_weight = MOB_SIZE_SMALL //This is calculated from the mob sizes of occupants

/obj/item/pet_carrier/Destroy()
	if(occupants.len)
		for(var/V in occupants)
			remove_occupant(V)
	return ..()

/obj/item/pet_carrier/Exited(atom/movable/occupant)
	if(occupant in occupants && isliving(occupant))
		var/mob/living/L = occupant
		occupants -= occupant
		occupant_weight -= L.mob_size

/obj/item/pet_carrier/handle_atom_del(atom/A)
	if(A in occupants && isliving(A))
		var/mob/living/L = A
		occupants -= L
		occupant_weight -= L.mob_size
	..()

/obj/item/pet_carrier/examine(mob/user)
	. = ..()
	if(occupants.len)
		for(var/V in occupants)
			var/mob/living/L = V
			. += "<span class='notice'>It has [L] inside.</span>"
	else
		. += "<span class='notice'>It has nothing inside.</span>"
	if(user.canUseTopic(src))
		. += "<span class='notice'>Activate it in your hand to [open ? "close" : "open"] its door.</span>"
		if(!open)
			. += "<span class='notice'>Alt-click to [locked ? "unlock" : "lock"] its door.</span>"

/obj/item/pet_carrier/attack_self(mob/living/user)
	if(open)
		to_chat(user, "<span class='notice'>You close [src]'s door.</span>")
		playsound(user, 'sound/effects/bin_close.ogg', 50, TRUE)
		open = FALSE
	else
		if(locked)
			to_chat(user, "<span class='warning'>[src] is locked!</span>")
			return
		to_chat(user, "<span class='notice'>You open [src]'s door.</span>")
		playsound(user, 'sound/effects/bin_open.ogg', 50, TRUE)
		open = TRUE
	update_icon()

/obj/item/pet_carrier/AltClick(mob/living/user)
	if(open || !user.canUseTopic(src, BE_CLOSE))
		return
	locked = !locked
	to_chat(user, "<span class='notice'>You flip the lock switch [locked ? "down" : "up"].</span>")
	if(locked)
		playsound(user, 'sound/machines/boltsdown.ogg', 30, TRUE)
	else
		playsound(user, 'sound/machines/boltsup.ogg', 30, TRUE)
	update_icon()

/obj/item/pet_carrier/attack(mob/living/target, mob/living/user)
	if(user.a_intent == INTENT_HARM)
		return ..()
	if(!open)
		to_chat(user, "<span class='warning'>You need to open [src]'s door!</span>")
		return
	if(target.mob_size > max_occupant_weight)
		if(ishuman(target))
			var/mob/living/carbon/human/H = target
			if(isfelinid(H))
				to_chat(user, "<span class='warning'>You'd need a lot of catnip and treats, plus maybe a laser pointer, for that to work.</span>")
			else
				to_chat(user, "<span class='warning'>Humans, generally, do not fit into pet carriers.</span>")
		else
			to_chat(user, "<span class='warning'>You get the feeling [target] isn't meant for a [name].</span>")
		return
	if(user == target)
		to_chat(user, "<span class='warning'>Why would you ever do that?</span>")
		return
	load_occupant(user, target)

/obj/item/pet_carrier/relaymove(mob/living/user, direction)
	if(open)
		loc.visible_message("<span class='notice'>[user] climbs out of [src]!</span>", \
		"<span class='warning'>[user] jumps out of [src]!</span>")
		remove_occupant(user)
		return
	else if(!locked)
		loc.visible_message("<span class='notice'>[user] pushes open the door to [src]!</span>", \
		"<span class='warning'>[user] pushes open the door of [src]!</span>")
		open = TRUE
		update_icon()
		return
	else if(user.client)
		container_resist(user)

/obj/item/pet_carrier/container_resist(mob/living/user)
	user.changeNext_move(CLICK_CD_BREAKOUT)
	user.last_special = world.time + CLICK_CD_BREAKOUT
	if(user.mob_size <= MOB_SIZE_SMALL)
		to_chat(user, "<span class='notice'>You poke a limb through [src]'s bars and start fumbling for the lock switch... (This will take some time.)</span>")
		to_chat(loc, "<span class='warning'>You see [user] reach through the bars and fumble for the lock switch!</span>")
		if(!do_after(user, rand(300, 400), target = user) || open || !locked || !(user in occupants))
			return
		loc.visible_message("<span class='warning'>[user] flips the lock switch on [src] by reaching through!</span>", null, null, null, user)
		to_chat(user, "<span class='boldannounce'>Bingo! The lock pops open!</span>")
		locked = FALSE
		playsound(src, 'sound/machines/boltsup.ogg', 30, TRUE)
		update_icon()
	else
		loc.visible_message("<span class='warning'>[src] starts rattling as something pushes against the door!</span>", null, null, null, user)
		to_chat(user, "<span class='notice'>You start pushing out of [src]... (This will take about 20 seconds.)</span>")
		if(!do_after(user, 200, target = user) || open || !locked || !(user in occupants))
			return
		loc.visible_message("<span class='warning'>[user] shoves out of	[src]!</span>", null, null, null, user)
		to_chat(user, "<span class='notice'>You shove open [src]'s door against the lock's resistance and fall out!</span>")
		locked = FALSE
		open = TRUE
		update_icon()
		remove_occupant(user)

/obj/item/pet_carrier/update_icon_state()
	if(open)
		icon_state = initial(icon_state)
	else
		icon_state = "pet_carrier_[!occupants.len ? "closed" : "occupied"]"

/obj/item/pet_carrier/update_overlays()
	. = ..()
	if(!open)
		. += "[locked ? "" : "un"]locked"

/obj/item/pet_carrier/MouseDrop(atom/over_atom)
	. = ..()
	if(isopenturf(over_atom) && usr.canUseTopic(src, BE_CLOSE, ismonkey(usr)) && usr.Adjacent(over_atom) && open && occupants.len)
		usr.visible_message("<span class='notice'>[usr] unloads [src].</span>", \
		"<span class='notice'>You unload [src] onto [over_atom].</span>")
		for(var/V in occupants)
			remove_occupant(V, over_atom)

/obj/item/pet_carrier/proc/load_occupant(mob/living/user, mob/living/target)
	if(pet_carrier_full(src))
		to_chat(user, "<span class='warning'>[src] is already carrying too much!</span>")
		return
	user.visible_message("<span class='notice'>[user] starts loading [target] into [src].</span>", \
	"<span class='notice'>You start loading [target] into [src]...</span>", null, null, target)
	to_chat(target, "<span class='userdanger'>[user] starts loading you into [user.p_their()] [name]!</span>")
	if(!do_mob(user, target, 30))
		return
	if(target in occupants)
		return
	if(pet_carrier_full(src)) //Run the checks again, just in case
		to_chat(user, "<span class='warning'>[src] is already carrying too much!</span>")
		return
	user.visible_message("<span class='notice'>[user] loads [target] into [src]!</span>", \
	"<span class='notice'>You load [target] into [src].</span>", null, null, target)
	to_chat(target, "<span class='userdanger'>[user] loads you into [user.p_their()] [name]!</span>")
	add_occupant(target)

/obj/item/pet_carrier/proc/add_occupant(mob/living/occupant)
	if(occupant in occupants || !istype(occupant))
		return
	occupant.forceMove(src)
	occupants += occupant
	occupant_weight += occupant.mob_size

/obj/item/pet_carrier/proc/remove_occupant(mob/living/occupant, turf/new_turf)
	if(!(occupant in occupants) || !istype(occupant))
		return
	occupant.forceMove(new_turf ? new_turf : drop_location())
	occupants -= occupant
	occupant_weight -= occupant.mob_size
	occupant.setDir(SOUTH)

#undef pet_carrier_full
