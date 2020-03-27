/obj/vehicle/ridden/scooter
	name = "scooter"
	desc = "A fun way to get around."
	icon_state = "scooter"
	are_legs_exposed = TRUE
	fall_off_if_missing_arms = TRUE

/obj/vehicle/ridden/scooter/Initialize()
	. = ..()
	var/datum/component/riding/D = LoadComponent(/datum/component/riding)
	D.set_riding_offsets(RIDING_OFFSET_ALL, list(TEXT_NORTH = list(0), TEXT_SOUTH = list(-2), TEXT_EAST = list(0), TEXT_WEST = list( 2)))


/obj/vehicle/ridden/scooter/wrench_act(mob/living/user, obj/item/I)
	..()
	to_chat(user, "<span class='notice'>You begin to remove the handlebars...</span>")
	if(I.use_tool(src, user, 40, volume=50))
		var/obj/vehicle/ridden/scooter/skateboard/S = new(drop_location())
		new /obj/item/stack/rods(drop_location(), 2)
		to_chat(user, "<span class='notice'>You remove the handlebars from [src].</span>")
		if(has_buckled_mobs())
			var/mob/living/carbon/H = buckled_mobs[1]
			unbuckle_mob(H)
			S.buckle_mob(H)
		qdel(src)
	return TRUE

/obj/vehicle/ridden/scooter/Moved()
	. = ..()
	for(var/m in buckled_mobs)
		var/mob/living/buckled_mob = m
		if(buckled_mob.get_num_legs(FALSE) > 0)
			buckled_mob.pixel_y = 5
		else
			buckled_mob.pixel_y = -4

/obj/vehicle/ridden/scooter/buckle_mob(mob/living/M, force = FALSE, check_loc = TRUE)
	if(!istype(M))
		return FALSE
	if(M.get_num_legs() < legs_required && M.get_num_arms() < arms_required)
		to_chat(M, "<span class='warning'>You don't think it'd be a good idea trying to ride \the [src]...</span>")
		return FALSE
	return ..()

/obj/vehicle/ridden/scooter/skateboard
	name = "improvised skateboard"
	desc = "An unfinished scooter which can only barely be called a skateboard. It's still rideable, but probably unsafe. Looks like you'll need to add a few rods to make handlebars."
	icon_state = "skateboard"
	density = FALSE
	arms_required = 0
	fall_off_if_missing_arms = FALSE
	var/datum/effect_system/spark_spread/sparks
	///Whether the board is currently grinding
	var/grinding = FALSE
	///Stores the time of the last crash plus a short cooldown, affects availability and outcome of certain actions
	var/next_crash
	///Stores the default icon state
	var/board_icon = "skateboard"
	///The handheld item counterpart for the board
	var/board_item_type = /obj/item/melee/skateboard
	///Stamina drain multiplier
	var/instability = 10

/obj/vehicle/ridden/scooter/skateboard/Initialize()
	. = ..()
	var/datum/component/riding/D = LoadComponent(/datum/component/riding)
	D.vehicle_move_delay = 1.5
	D.set_vehicle_dir_layer(SOUTH, ABOVE_MOB_LAYER)
	D.set_vehicle_dir_layer(NORTH, OBJ_LAYER)
	D.set_vehicle_dir_layer(EAST, OBJ_LAYER)
	D.set_vehicle_dir_layer(WEST, OBJ_LAYER)
	sparks = new
	sparks.set_up(1, 0, src)
	sparks.attach(src)

/obj/vehicle/ridden/scooter/skateboard/Destroy()
	if(sparks)
		QDEL_NULL(sparks)
	. = ..()

/obj/vehicle/ridden/scooter/skateboard/relaymove()
	if (grinding || world.time < next_crash)
		return FALSE
	return ..()

/obj/vehicle/ridden/scooter/skateboard/generate_actions()
	. = ..()
	initialize_controller_action_type(/datum/action/vehicle/ridden/scooter/skateboard/ollie, VEHICLE_CONTROL_DRIVE)

/obj/vehicle/ridden/scooter/skateboard/post_buckle_mob(mob/living/M)//allows skateboards to be non-dense but still allows 2 skateboarders to collide with each other
	density = TRUE
	return ..()

/obj/vehicle/ridden/scooter/skateboard/post_unbuckle_mob(mob/living/M)
	if(!has_buckled_mobs())
		density = FALSE
	return ..()

/obj/vehicle/ridden/scooter/skateboard/Bump(atom/A)
	. = ..()
	if(A.density && has_buckled_mobs())
		var/mob/living/H = buckled_mobs[1]
		H.adjustStaminaLoss(instability*6)
		playsound(src, 'sound/effects/bang.ogg', 40, TRUE)
		if(!iscarbon(H) || H.getStaminaLoss() >= 100 || grinding || world.time < next_crash)
			var/atom/throw_target = get_edge_target_turf(H, pick(GLOB.cardinals))
			unbuckle_mob(H)
			H.throw_at(throw_target, 3, 2)
			var/head_slot = H.get_item_by_slot(ITEM_SLOT_HEAD)
			if(!head_slot || !(istype(head_slot,/obj/item/clothing/head/helmet) || istype(head_slot,/obj/item/clothing/head/hardhat)))
				H.adjustOrganLoss(ORGAN_SLOT_BRAIN, 5)
				H.updatehealth()
			visible_message("<span class='danger'>[src] crashes into [A], sending [H] flying!</span>")
			H.Paralyze(80)
		else
			var/backdir = turn(dir, 180)
			vehicle_move(backdir)
			H.spin(4, 1)
		next_crash = world.time + 10

///Moves the vehicle forward and if it lands on a table, repeats
/obj/vehicle/ridden/scooter/skateboard/proc/grind()
	vehicle_move(dir)
	if(has_buckled_mobs() && locate(/obj/structure/table) in loc.contents)
		var/mob/living/L = buckled_mobs[1]
		L.adjustStaminaLoss(instability*0.5)
		if (L.getStaminaLoss() >= 100)
			playsound(src, 'sound/effects/bang.ogg', 20, TRUE)
			unbuckle_mob(L)
			var/atom/throw_target = get_edge_target_turf(src, pick(GLOB.cardinals))
			L.throw_at(throw_target, 2, 2)
			visible_message("<span class='danger'>[L] loses [L.p_their()] footing and slams on the ground!</span>")
			L.Paralyze(40)
			grinding = FALSE
			icon_state = board_icon
			return
		else
			playsound(src, 'sound/vehicles/skateboard_roll.ogg', 50, TRUE)
			if(prob (25))
				var/turf/location = get_turf(loc)
				if(location)
					location.hotspot_expose(1000,1000)
				sparks.start() //the most radical way to start plasma fires
			addtimer(CALLBACK(src, .proc/grind), 2)
			return
	else
		grinding = FALSE
		icon_state = board_icon

/obj/vehicle/ridden/scooter/skateboard/MouseDrop(atom/over_object)
	. = ..()
	var/mob/living/carbon/M = usr
	if(!istype(M) || M.incapacitated() || !Adjacent(M))
		return
	if(has_buckled_mobs() && over_object == M)
		to_chat(M, "<span class='warning'>You can't lift this up when somebody's on it.</span>")
		return
	if(over_object == M)
		var/board = new board_item_type(get_turf(M))
		M.put_in_hands(board)
		qdel(src)

/obj/vehicle/ridden/scooter/skateboard/pro
	name = "skateboard"
	desc = "A RaDSTORMz brand professional skateboard. Looks a lot more stable than the average board."
	icon_state = "skateboard2"
	board_icon = "skateboard2"
	board_item_type = /obj/item/melee/skateboard/pro
	instability = 6

/obj/vehicle/ridden/scooter/skateboard/hoverboard/
	name = "hoverboard"
	desc = "A blast from the past, so retro!"
	board_item_type = /obj/item/melee/skateboard/hoverboard
	instability = 3
	icon_state = "hoverboard_red"
	board_icon = "hoverboard_red"

/obj/vehicle/ridden/scooter/skateboard/hoverboard/screwdriver_act(mob/living/user, obj/item/I)
	return FALSE

/obj/vehicle/ridden/scooter/skateboard/hoverboard/attackby(obj/item/I, mob/user, params)
	if(istype(I, /obj/item/stack/rods))
		return
	else
		return ..()

/obj/vehicle/ridden/scooter/skateboard/hoverboard/admin
	name = "\improper Board Of Directors"
	desc = "The engineering complexity of a spaceship concentrated inside of a board. Just as expensive, too."
	board_item_type = /obj/item/melee/skateboard/hoverboard/admin
	instability = 0
	icon_state = "hoverboard_nt"
	board_icon = "hoverboard_nt"

//CONSTRUCTION
/obj/item/scooter_frame
	name = "scooter frame"
	desc = "A metal frame for building a scooter. Looks like you'll need to add some metal to make wheels."
	icon = 'icons/obj/vehicles.dmi'
	icon_state = "scooter_frame"
	w_class = WEIGHT_CLASS_NORMAL

/obj/item/scooter_frame/attackby(obj/item/I, mob/user, params)
	if(istype(I, /obj/item/stack/sheet/metal))
		if(!I.tool_start_check(user, amount=5))
			return
		to_chat(user, "<span class='notice'>You begin to add wheels to [src].</span>")
		if(I.use_tool(src, user, 80, volume=50, amount=5))
			to_chat(user, "<span class='notice'>You finish making wheels for [src].</span>")
			new /obj/vehicle/ridden/scooter/skateboard(user.loc)
			qdel(src)
	else
		return ..()

/obj/item/scooter_frame/wrench_act(mob/living/user, obj/item/I)
	..()
	to_chat(user, "<span class='notice'>You deconstruct [src].</span>")
	new /obj/item/stack/rods(drop_location(), 10)
	I.play_tool_sound(src)
	qdel(src)
	return TRUE

/obj/vehicle/ridden/scooter/skateboard/attackby(obj/item/I, mob/user, params)
	if(istype(I, /obj/item/stack/rods))
		if(!I.tool_start_check(user, amount=2))
			return
		to_chat(user, "<span class='notice'>You begin making handlebars for [src].</span>")
		if(I.use_tool(src, user, 25, volume=50, amount=2))
			to_chat(user, "<span class='notice'>You add the rods to [src], creating handlebars.</span>")
			var/obj/vehicle/ridden/scooter/S = new(loc)
			if(has_buckled_mobs())
				var/mob/living/carbon/H = buckled_mobs[1]
				unbuckle_mob(H)
				S.buckle_mob(H)
			qdel(src)
	else
		return ..()

/obj/vehicle/ridden/scooter/skateboard/screwdriver_act(mob/living/user, obj/item/I)
	if(..())
		return TRUE
	to_chat(user, "<span class='notice'>You begin to deconstruct and remove the wheels on [src]...</span>")
	if(I.use_tool(src, user, 20, volume=50))
		to_chat(user, "<span class='notice'>You deconstruct the wheels on [src].</span>")
		new /obj/item/stack/sheet/metal(drop_location(), 5)
		new /obj/item/scooter_frame(drop_location())
		if(has_buckled_mobs())
			var/mob/living/carbon/H = buckled_mobs[1]
			unbuckle_mob(H)
		qdel(src)
	return TRUE

/obj/vehicle/ridden/scooter/skateboard/wrench_act(mob/living/user, obj/item/I)
	return

//Wheelys
/obj/vehicle/ridden/scooter/wheelys
	name = "Wheely-Heels"
	desc = "Uses patented retractable wheel technology. Never sacrifice speed for style - not that this provides much of either."
	icon = null
	density = FALSE

/obj/vehicle/ridden/scooter/wheelys/Initialize()
	. = ..()
	var/datum/component/riding/D = LoadComponent(/datum/component/riding)
	D.vehicle_move_delay = 0
	D.set_vehicle_dir_layer(SOUTH, ABOVE_MOB_LAYER)
	D.set_vehicle_dir_layer(NORTH, OBJ_LAYER)
	D.set_vehicle_dir_layer(EAST, OBJ_LAYER)
	D.set_vehicle_dir_layer(WEST, OBJ_LAYER)

/obj/vehicle/ridden/scooter/wheelys/post_unbuckle_mob(mob/living/M)
	if(!has_buckled_mobs())
		to_chat(M, "<span class='notice'>You pop the Wheely-Heels' wheels back into place.</span>")
		moveToNullspace()
	return ..()

/obj/vehicle/ridden/scooter/wheelys/post_buckle_mob(mob/living/M)
	to_chat(M, "<span class='notice'>You pop out the Wheely-Heels' wheels.</span>")
	return ..()

/obj/vehicle/ridden/scooter/wheelys/Bump(atom/A)
	. = ..()
	if(A.density && has_buckled_mobs())
		var/mob/living/H = buckled_mobs[1]
		var/atom/throw_target = get_edge_target_turf(H, pick(GLOB.cardinals))
		unbuckle_mob(H)
		H.throw_at(throw_target, 4, 3)
		H.Paralyze(30)
		H.adjustStaminaLoss(10)
		var/head_slot = H.get_item_by_slot(ITEM_SLOT_HEAD)
		if(!head_slot || !(istype(head_slot,/obj/item/clothing/head/helmet) || istype(head_slot,/obj/item/clothing/head/hardhat)))
			H.adjustOrganLoss(ORGAN_SLOT_BRAIN, 1)
			H.updatehealth()
		visible_message("<span class='danger'>[src] crashes into [A], sending [H] flying!</span>")
		playsound(src, 'sound/effects/bang.ogg', 50, TRUE)
