/obj/vehicle/ridden/wheelchair/motorized
	name = "motorized wheelchair"
	desc = "A chair with big wheels. It seems to have a motor in it."
	max_integrity = 150
	var/speed = 2
	var/power_efficiency = 1
	var/power_usage = 100
	var/panel_open = FALSE
	var/list/required_parts = list(/obj/item/stock_parts/manipulator, 
							/obj/item/stock_parts/manipulator,
							/obj/item/stock_parts/capacitor)
	var/obj/item/stock_parts/cell/power_cell

/obj/vehicle/ridden/wheelchair/motorized/CheckParts(list/parts_list)
	..()
	refresh_parts()

/obj/vehicle/ridden/wheelchair/motorized/proc/refresh_parts()
	speed = 1 // Should never be under 1
	for(var/obj/item/stock_parts/manipulator/M in contents)
		speed += M.rating
	for(var/obj/item/stock_parts/capacitor/C in contents)
		power_efficiency = C.rating
	var/datum/component/riding/D = GetComponent(/datum/component/riding)
	D.vehicle_move_delay = round(CONFIG_GET(number/movedelay/run_delay) * delay_multiplier) / speed

/obj/vehicle/ridden/wheelchair/motorized/obj_destruction(damage_flag)
	var/turf/T = get_turf(src)
	for(var/atom/movable/A in contents)
		A.forceMove(T)
		if(isliving(A))
			var/mob/living/L = A
			L.update_mobility()
	..()

/obj/vehicle/ridden/wheelchair/motorized/driver_move(mob/living/user, direction)
	if(istype(user))
		if(!canmove)
			return FALSE
		if(!power_cell)
			to_chat(user, "<span class='warning'>There seems to be no cell installed in [src].</span>")
			canmove = FALSE
			addtimer(VARSET_CALLBACK(src, canmove, TRUE), 20)
			return FALSE
		if(power_cell.charge < power_usage / max(power_efficiency, 1))			
			to_chat(user, "<span class='warning'>The display on [src] blinks 'Out of Power'.</span>")
			canmove = FALSE
			addtimer(VARSET_CALLBACK(src, canmove, TRUE), 20)
			return FALSE
		if(user.get_num_arms() < arms_required)
			to_chat(user, "<span class='warning'>You don't have enough arms to operate the motor controller!</span>")
			canmove = FALSE
			addtimer(VARSET_CALLBACK(src, canmove, TRUE), 20)
			return FALSE
		power_cell.use(power_usage / max(power_efficiency, 1))
	return ..()

/obj/vehicle/ridden/wheelchair/motorized/set_move_delay(mob/living/user)
	return

/obj/vehicle/ridden/wheelchair/motorized/post_buckle_mob(mob/living/user)
	. = ..()
	density = TRUE

/obj/vehicle/ridden/wheelchair/motorized/post_unbuckle_mob()
	. = ..()
	density = FALSE

/obj/vehicle/ridden/wheelchair/motorized/attack_hand(mob/living/user)
	if(power_cell && panel_open)
		power_cell.update_icon()
		user.put_in_hands(power_cell)
		power_cell = null
		to_chat(user, "<span class='notice'>You remove the power cell from [src].</span>")
		return
	return ..()
	
/obj/vehicle/ridden/wheelchair/motorized/attackby(obj/item/I, mob/user, params)
	if(I.tool_behaviour == TOOL_SCREWDRIVER)
		I.play_tool_sound(src)
		panel_open = !panel_open
		user.visible_message("<span class='notice'>[user] [panel_open ? "opens" : "closes"] the maintenance panel on [src].</span>", "<span class='notice'>You [panel_open ? "open" : "close"] the maintenance panel.</span>")
		return
	if(panel_open)
		if(istype(I, /obj/item/stock_parts/cell))
			if(power_cell)
				to_chat(user, "<span class='warning'>There is a power cell already installed.</span>")
			else
				I.forceMove(src)
				power_cell = I
				to_chat(user, "<span class='notice'>You install the [I].</span>")
			refresh_parts()
			return
		if(istype(I, /obj/item/stock_parts))
			var/obj/item/stock_parts/B = I
			var/P
			for(var/obj/item/stock_parts/A in contents)
				for(var/D in required_parts)
					if(ispath(A.type, D))
						P = D
						break
				if(istype(B, P) && istype(A, P))
					if(B.get_part_rating() > A.get_part_rating())
						B.forceMove(src)
						user.put_in_hands(A)
						user.visible_message("<span class='notice'>[user] replaces [A] with [B] in [src].</span>", "<span class='notice'>You replace [A] with [B].</span>")
						break
			refresh_parts()
			return
	return ..()

/obj/vehicle/ridden/wheelchair/motorized/wrench_act(mob/living/user, obj/item/I)
	to_chat(user, "<span class='notice'>You begin to detach the wheels...</span>")
	if(I.use_tool(src, user, 40, volume=50))
		to_chat(user, "<span class='notice'>You detach the wheels and deconstruct the chair.</span>")
		new /obj/item/stack/rods(drop_location(), 8)
		new /obj/item/stack/sheet/metal(drop_location(), 10)
		var/turf/T = get_turf(src)
		for(var/atom/movable/A in contents)
			A.forceMove(T)
			if(isliving(A))
				var/mob/living/L = A
				L.update_mobility()
		qdel(src)
	return TRUE

/obj/vehicle/ridden/wheelchair/motorized/examine(mob/user)
	. = ..()
	if((obj_flags & EMAGGED) && panel_open)
		. += "There is a bomb under the maintenance panel."
	. += "There is a small screen on it, [(in_range(user, src) || isobserver(user)) ? "[power_cell ? "it reads:" : "but it is dark."]" : "but you can't see it from here."]"
	if(!power_cell || (!in_range(user, src) && !isobserver(user)))
		return
	. += "Speed: [speed]"
	. += "Energy efficiency: [power_efficiency]"
	. += "Power: [power_cell.charge] out of [power_cell.maxcharge]"

/obj/vehicle/ridden/wheelchair/motorized/Bump(atom/movable/M)
	. = ..()
	// Here is the shitty emag functionality.
	if(obj_flags & EMAGGED && (istype(M, /turf/closed) || isliving(M)))
		explosion(src, -1, 1, 3, 2, 0)
		visible_message("<span class='boldwarning'>[src] explodes!!</span>")
		return
	// If the speed is higher than delay_multiplier throw the person on the wheelchair away
	if(M.density && speed > delay_multiplier && has_buckled_mobs())
		var/mob/living/H = buckled_mobs[1]
		var/atom/throw_target = get_edge_target_turf(H, pick(GLOB.cardinals))
		unbuckle_mob(H)
		H.throw_at(throw_target, 2, 3)
		H.Knockdown(100)
		H.adjustStaminaLoss(40)
		if(isliving(M))
			var/mob/living/D = M
			throw_target = get_edge_target_turf(D, pick(GLOB.cardinals))
			D.throw_at(throw_target, 2, 3)
			D.Knockdown(80)
			D.adjustStaminaLoss(35)
			visible_message("<span class='danger'>[src] crashes into [M], sending [H] and [D] flying!</span>")
		else
			visible_message("<span class='danger'>[src] crashes into [M], sending [H] flying!</span>")
		playsound(src, 'sound/effects/bang.ogg', 50, 1)
		
/obj/vehicle/ridden/wheelchair/motorized/emag_act(mob/user)
	if((obj_flags & EMAGGED) || !panel_open)
		return
	to_chat(user, "<span class='warning'>A bomb appears in [src], what the fuck?</span>")
	obj_flags |= EMAGGED
