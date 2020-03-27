/obj/machinery/transformer
	name = "\improper Automatic Robotic Factory 5000"
	desc = "A large metallic machine with an entrance and an exit. A sign on \
		the side reads, 'human go in, robot come out'. The human must be \
		lying down and alive. Has to cooldown between each use."
	icon = 'icons/obj/recycling.dmi'
	icon_state = "separator-AO1"
	layer = ABOVE_ALL_MOB_LAYER // Overhead
	density = FALSE
	var/transform_dead = 0
	var/transform_standing = 0
	var/cooldown_duration = 600 // 1 minute
	var/cooldown = 0
	var/cooldown_timer
	var/robot_cell_charge = 5000
	var/obj/effect/countdown/transformer/countdown
	var/mob/living/silicon/ai/masterAI

/obj/machinery/transformer/Initialize()
	// On us
	. = ..()
	new /obj/machinery/conveyor/auto(locate(x - 1, y, z), WEST)
	new /obj/machinery/conveyor/auto(loc, WEST)
	new /obj/machinery/conveyor/auto(locate(x + 1, y, z), WEST)
	countdown = new(src)
	countdown.start()

/obj/machinery/transformer/examine(mob/user)
	. = ..()
	if(cooldown && (issilicon(user) || isobserver(user)))
		. += "It will be ready in [DisplayTimeText(cooldown_timer - world.time)]."

/obj/machinery/transformer/Destroy()
	QDEL_NULL(countdown)
	. = ..()

/obj/machinery/transformer/update_icon_state()
	if(stat & (BROKEN|NOPOWER) || cooldown == 1)
		icon_state = "separator-AO0"
	else
		icon_state = initial(icon_state)

/obj/machinery/transformer/Bumped(atom/movable/AM)
	if(cooldown == 1)
		return

	// Crossed didn't like people lying down.
	if(ishuman(AM))
		// Only humans can enter from the west side, while lying down.
		var/move_dir = get_dir(loc, AM.loc)
		var/mob/living/carbon/human/H = AM
		if((transform_standing || !(H.mobility_flags & MOBILITY_STAND)) && move_dir == EAST)// || move_dir == WEST)
			AM.forceMove(drop_location())
			do_transform(AM)

/obj/machinery/transformer/CanAllowThrough(atom/movable/mover, turf/target)
	. = ..()
	// Allows items to go through,
	// to stop them from blocking the conveyor belt.
	if(!ishuman(mover))
		if(get_dir(src, mover) == EAST)
			return
	return FALSE

/obj/machinery/transformer/process()
	if(cooldown && (cooldown_timer <= world.time))
		cooldown = FALSE
		update_icon()

/obj/machinery/transformer/proc/do_transform(mob/living/carbon/human/H)
	if(stat & (BROKEN|NOPOWER))
		return
	if(cooldown == 1)
		return

	if(!transform_dead && H.stat == DEAD)
		playsound(src.loc, 'sound/machines/buzz-sigh.ogg', 50, FALSE)
		return

	// Activate the cooldown
	cooldown = 1
	cooldown_timer = world.time + cooldown_duration
	update_icon()

	playsound(src.loc, 'sound/items/welder.ogg', 50, TRUE)
	H.emote("scream") // It is painful
	H.adjustBruteLoss(max(0, 80 - H.getBruteLoss())) // Hurt the human, don't try to kill them though.

	// Sleep for a couple of ticks to allow the human to see the pain
	sleep(5)

	use_power(5000) // Use a lot of power.
	var/mob/living/silicon/robot/R = H.Robotize()
	R.cell = new /obj/item/stock_parts/cell/upgraded/plus(R, robot_cell_charge)

 	// So he can't jump out the gate right away.
	R.SetLockdown()
	if(masterAI)
		R.connected_ai = masterAI
		R.lawsync()
		R.lawupdate = 1
	addtimer(CALLBACK(src, .proc/unlock_new_robot, R), 50)

/obj/machinery/transformer/proc/unlock_new_robot(mob/living/silicon/robot/R)
	playsound(src.loc, 'sound/machines/ping.ogg', 50, FALSE)
	sleep(30)
	if(R)
		R.SetLockdown(0)
		R.notify_ai(NEW_BORG)
