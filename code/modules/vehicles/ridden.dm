/obj/vehicle/ridden
	name = "ridden vehicle"
	can_buckle = TRUE
	max_buckled_mobs = 1
	buckle_lying = FALSE
	default_driver_move = FALSE
	var/legs_required = 2
	var/arms_required = 1	//why not?
	var/fall_off_if_missing_arms = FALSE //heh...
	var/message_cooldown = 0

/obj/vehicle/ridden/Initialize()
	. = ..()
	LoadComponent(/datum/component/riding)

/obj/vehicle/ridden/examine(mob/user)
	. = ..()
	if(key_type)
		if(!inserted_key)
			. += "<span class='notice'>Put a key inside it by clicking it with the key.</span>"
		else
			. += "<span class='notice'>Alt-click [src] to remove the key.</span>"

/obj/vehicle/ridden/generate_action_type(actiontype)
	var/datum/action/vehicle/ridden/A = ..()
	. = A
	if(istype(A))
		A.vehicle_ridden_target = src

/obj/vehicle/ridden/post_unbuckle_mob(mob/living/M)
	remove_occupant(M)
	return ..()

/obj/vehicle/ridden/post_buckle_mob(mob/living/M)
	add_occupant(M)
	return ..()

/obj/vehicle/ridden/attackby(obj/item/I, mob/user, params)
	if(key_type && !is_key(inserted_key) && is_key(I))
		if(user.transferItemToLoc(I, src))
			to_chat(user, "<span class='notice'>You insert \the [I] into \the [src].</span>")
			if(inserted_key)	//just in case there's an invalid key
				inserted_key.forceMove(drop_location())
			inserted_key = I
		else
			to_chat(user, "<span class='warning'>[I] seems to be stuck to your hand!</span>")
		return
	return ..()

/obj/vehicle/ridden/AltClick(mob/user)
	if(inserted_key && user.canUseTopic(src, BE_CLOSE, ismonkey(user)))
		if(!is_occupant(user))
			to_chat(user, "<span class='warning'>You must be riding the [src] to remove [src]'s key!</span>")
			return
		to_chat(user, "<span class='notice'>You remove \the [inserted_key] from \the [src].</span>")
		inserted_key.forceMove(drop_location())
		user.put_in_hands(inserted_key)
		inserted_key = null
	return ..()

/obj/vehicle/ridden/driver_move(mob/user, direction)
	if(key_type && !is_key(inserted_key))
		if(message_cooldown < world.time)
			to_chat(user, "<span class='warning'>[src] has no key inserted!</span>")
			message_cooldown = world.time + 5 SECONDS
		return FALSE
	if(legs_required)
		var/how_many_legs = user.get_num_legs()
		if(how_many_legs < legs_required)
			if(message_cooldown < world.time)
				to_chat(user, "<span class='warning'>You can't seem to manage that with[how_many_legs ? " your leg[how_many_legs > 1 ? "s" : null]" : "out legs"]...</span>")
				message_cooldown = world.time + 5 SECONDS
			return FALSE
	if(arms_required)
		var/how_many_arms = user.get_num_arms()
		if(how_many_arms < arms_required)
			if(fall_off_if_missing_arms)
				unbuckle_mob(user, TRUE)
				user.visible_message("<span class='danger'>[user] falls off \the [src].</span>",\
				"<span class='danger'>You fall off \the [src] while trying to operate it without [arms_required ? "both arms":"an arm"]!</span>")
				if(isliving(user))
					var/mob/living/L = user
					L.Stun(30)
				return FALSE

			if(message_cooldown < world.time)
				to_chat(user, "<span class='warning'>You can't seem to manage that with[how_many_arms ? " your arm[how_many_arms > 1 ? "s" : null]" : "out arms"]...</span>")
				message_cooldown = world.time + 5 SECONDS
			return FALSE
	var/datum/component/riding/R = GetComponent(/datum/component/riding)
	R.handle_ride(user, direction)
	return ..()

/obj/vehicle/ridden/user_buckle_mob(mob/living/M, mob/user, check_loc = TRUE)
	if(!in_range(user, src) || !in_range(M, src))
		return FALSE
	. = ..(M, user, FALSE)

/obj/vehicle/ridden/buckle_mob(mob/living/M, force = FALSE, check_loc = TRUE)
	if(!force && occupant_amount() >= max_occupants)
		return FALSE
	return ..()

/obj/vehicle/ridden/zap_act(zap_str, zap_flags, shocked_targets)
	zap_buckle_check(zap_str)
	. = ..()
