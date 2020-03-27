/**
  * If your mob is concious, drop the item in the active hand
  *
  * This is a hidden verb, likely for binding with winset for hotkeys
  */
/client/verb/drop_item()
	set hidden = 1
	if(!iscyborg(mob) && mob.stat == CONSCIOUS)
		mob.dropItemToGround(mob.get_active_held_item())
	return

/**
  * force move the control_object of your client mob
  *
  * Used in admin possession and called from the client Move proc
  * ensures the possessed object moves and not the admin mob
  *
  * Has no sanity other than checking density
  */
/client/proc/Move_object(direct)
	if(mob && mob.control_object)
		if(mob.control_object.density)
			step(mob.control_object,direct)
			if(!mob.control_object)
				return
			mob.control_object.setDir(direct)
		else
			mob.control_object.forceMove(get_step(mob.control_object,direct))

#define MOVEMENT_DELAY_BUFFER 0.75
#define MOVEMENT_DELAY_BUFFER_DELTA 1.25

/**
  * Move a client in a direction
  *
  * Huge proc, has a lot of functionality
  *
  * Mostly it will despatch to the mob that you are the owner of to actually move
  * in the physical realm
  *
  * Things that stop you moving as a mob:
  * * world time being less than your next move_delay
  * * not being in a mob, or that mob not having a loc
  * * missing the n and direction parameters
  * * being in remote control of an object (calls Moveobject instead)
  * * being dead (it ghosts you instead)
  *
  * Things that stop you moving as a mob living (why even have OO if you're just shoving it all
  * in the parent proc with istype checks right?):
  * * having incorporeal_move set (calls Process_Incorpmove() instead)
  * * being grabbed
  * * being buckled  (relaymove() is called to the buckled atom instead)
  * * having your loc be some other mob (relaymove() is called on that mob instead)
  * * Not having MOBILITY_MOVE
  * * Failing Process_Spacemove() call
  *
  * At this point, if the mob is is confused, then a random direction and target turf will be calculated for you to travel to instead
  *
  * Now the parent call is made (to the byond builtin move), which moves you
  *
  * Some final move delay calculations (doubling if you moved diagonally successfully)
  *
  * if mob throwing is set I believe it's unset at this point via a call to finalize
  *
  * Finally if you're pulling an object and it's dense, you are turned 180 after the move
  * (if you ask me, this should be at the top of the move so you don't dance around)
  *
  */
/client/Move(n, direct)
	if(world.time < move_delay) //do not move anything ahead of this check please
		return FALSE
	else
		next_move_dir_add = 0
		next_move_dir_sub = 0
	var/old_move_delay = move_delay
	move_delay = world.time + world.tick_lag //this is here because Move() can now be called mutiple times per tick
	if(!mob || !mob.loc)
		return FALSE
	if(!n || !direct)
		return FALSE
	if(mob.notransform)
		return FALSE	//This is sota the goto stop mobs from moving var
	if(mob.control_object)
		return Move_object(direct)
	if(!isliving(mob))
		return mob.Move(n, direct)
	if(mob.stat == DEAD)
		mob.ghostize()
		return FALSE
	if(mob.force_moving)
		return FALSE

	var/mob/living/L = mob  //Already checked for isliving earlier
	if(L.incorporeal_move)	//Move though walls
		Process_Incorpmove(direct)
		return FALSE

	if(mob.remote_control)					//we're controlling something, our movement is relayed to it
		return mob.remote_control.relaymove(mob, direct)

	if(isAI(mob))
		return AIMove(n,direct,mob)

	if(Process_Grab()) //are we restrained by someone's grip?
		return

	if(mob.buckled)							//if we're buckled to something, tell it we moved.
		return mob.buckled.relaymove(mob, direct)

	if(!(L.mobility_flags & MOBILITY_MOVE))
		return FALSE

	if(isobj(mob.loc) || ismob(mob.loc))	//Inside an object, tell it we moved
		var/atom/O = mob.loc
		return O.relaymove(mob, direct)

	if(!mob.Process_Spacemove(direct))
		return FALSE
	//We are now going to move
	var/add_delay = mob.cached_multiplicative_slowdown
	if(old_move_delay + (add_delay*MOVEMENT_DELAY_BUFFER_DELTA) + MOVEMENT_DELAY_BUFFER > world.time)
		move_delay = old_move_delay
	else
		move_delay = world.time

	if(L.confused)
		var/newdir = 0
		if(L.confused > 40)
			newdir = pick(GLOB.alldirs)
		else if(prob(L.confused * 1.5))
			newdir = angle2dir(dir2angle(direct) + pick(90, -90))
		else if(prob(L.confused * 3))
			newdir = angle2dir(dir2angle(direct) + pick(45, -45))
		if(newdir)
			direct = newdir
			n = get_step(L, direct)

	. = ..()

	if((direct & (direct - 1)) && mob.loc == n) //moved diagonally successfully
		add_delay *= 2
	move_delay += add_delay
	if(.) // If mob is null here, we deserve the runtime
		if(mob.throwing)
			mob.throwing.finalize(FALSE)

	var/atom/movable/P = mob.pulling
	if(P && !ismob(P) && P.density)
		mob.setDir(turn(mob.dir, 180))

/**
  * Checks to see if you're being grabbed and if so attempts to break it
  *
  * Called by client/Move()
  */
/client/proc/Process_Grab()
	if(mob.pulledby)
		if((mob.pulledby == mob.pulling) && (mob.pulledby.grab_state == GRAB_PASSIVE))			//Don't autoresist passive grabs if we're grabbing them too.
			return
		if(mob.incapacitated(ignore_restraints = 1))
			move_delay = world.time + 10
			return TRUE
		else if(mob.restrained(ignore_grab = 1))
			move_delay = world.time + 10
			to_chat(src, "<span class='warning'>You're restrained! You can't move!</span>")
			return TRUE
		else
			return mob.resist_grab(1)

/**
  * Allows mobs to ignore density and phase through objects
  *
  * Called by client/Move()
  *
  * The behaviour depends on the incorporeal_move value of the mob
  *
  * * INCORPOREAL_MOVE_BASIC - forceMoved to the next tile with no stop
  * * INCORPOREAL_MOVE_SHADOW  - the same but leaves a cool effect path
  * * INCORPOREAL_MOVE_JAUNT - the same but blocked by holy tiles
  *
  * You'll note this is another mob living level proc living at the client level
  */
/client/proc/Process_Incorpmove(direct)
	var/turf/mobloc = get_turf(mob)
	if(!isliving(mob))
		return
	var/mob/living/L = mob
	switch(L.incorporeal_move)
		if(INCORPOREAL_MOVE_BASIC)
			var/T = get_step(L,direct)
			if(T)
				L.forceMove(T)
			L.setDir(direct)
		if(INCORPOREAL_MOVE_SHADOW)
			if(prob(50))
				var/locx
				var/locy
				switch(direct)
					if(NORTH)
						locx = mobloc.x
						locy = (mobloc.y+2)
						if(locy>world.maxy)
							return
					if(SOUTH)
						locx = mobloc.x
						locy = (mobloc.y-2)
						if(locy<1)
							return
					if(EAST)
						locy = mobloc.y
						locx = (mobloc.x+2)
						if(locx>world.maxx)
							return
					if(WEST)
						locy = mobloc.y
						locx = (mobloc.x-2)
						if(locx<1)
							return
					else
						return
				var/target = locate(locx,locy,mobloc.z)
				if(target)
					L.loc = target
					var/limit = 2//For only two trailing shadows.
					for(var/turf/T in getline(mobloc, L.loc))
						new /obj/effect/temp_visual/dir_setting/ninja/shadow(T, L.dir)
						limit--
						if(limit<=0)
							break
			else
				new /obj/effect/temp_visual/dir_setting/ninja/shadow(mobloc, L.dir)
				var/T = get_step(L,direct)
				if(T)
					L.forceMove(T)
			L.setDir(direct)
		if(INCORPOREAL_MOVE_JAUNT) //Incorporeal move, but blocked by holy-watered tiles and salt piles.
			var/turf/open/floor/stepTurf = get_step(L, direct)
			if(stepTurf)
				for(var/obj/effect/decal/cleanable/food/salt/S in stepTurf)
					to_chat(L, "<span class='warning'>[S] bars your passage!</span>")
					if(isrevenant(L))
						var/mob/living/simple_animal/revenant/R = L
						R.reveal(20)
						R.stun(20)
					return
				if(stepTurf.flags_1 & NOJAUNT_1)
					to_chat(L, "<span class='warning'>Some strange aura is blocking the way.</span>")
					return
				if (locate(/obj/effect/blessing, stepTurf))
					to_chat(L, "<span class='warning'>Holy energies block your path!</span>")
					return

				L.forceMove(stepTurf)
			L.setDir(direct)
	return TRUE


/**
  * Handles mob/living movement in space (or no gravity)
  *
  * Called by /client/Move()
  *
  * return TRUE for movement or FALSE for none
  *
  * You can move in space if you have a spacewalk ability
  */
/mob/Process_Spacemove(movement_dir = 0)
	if(spacewalk || ..())
		return TRUE
	var/atom/movable/backup = get_spacemove_backup()
	if(backup)
		if(istype(backup) && movement_dir && !backup.anchored)
			if(backup.newtonian_move(turn(movement_dir, 180))) //You're pushing off something movable, so it moves
				to_chat(src, "<span class='info'>You push off of [backup] to propel yourself.</span>")
		return TRUE
	return FALSE

/**
  * Find movable atoms? near a mob that are viable for pushing off when moving
  */
/mob/get_spacemove_backup()
	for(var/A in orange(1, get_turf(src)))
		if(isarea(A))
			continue
		else if(isturf(A))
			var/turf/turf = A
			if(isspaceturf(turf))
				continue
			if(!turf.density && !mob_negates_gravity())
				continue
			return A
		else
			var/atom/movable/AM = A
			if(AM == buckled)
				continue
			if(ismob(AM))
				var/mob/M = AM
				if(M.buckled)
					continue
			if(!AM.CanPass(src) || AM.density)
				if(AM.anchored)
					return AM
				if(pulling == AM)
					continue
				. = AM

/**
  * Returns true if a mob has gravity
  *
  * I hate that this exists
  */
/mob/proc/mob_has_gravity()
	return has_gravity()

/**
  * Does this mob ignore gravity
  */
/mob/proc/mob_negates_gravity()
	return FALSE

/// Called when this mob slips over, override as needed
/mob/proc/slip(knockdown_amount, obj/O, lube, paralyze, force_drop)
	return

/// Update the gravity status of this mob
/mob/proc/update_gravity(has_gravity, override=FALSE)
	var/speed_change = max(0, has_gravity - STANDARD_GRAVITY)
	if(!speed_change)
		remove_movespeed_modifier(MOVESPEED_ID_MOB_GRAVITY, update=TRUE)
	else
		add_movespeed_modifier(MOVESPEED_ID_MOB_GRAVITY, update=TRUE, priority=100, override=TRUE, multiplicative_slowdown=speed_change, blacklisted_movetypes=FLOATING)

//bodypart selection verbs - Cyberboss
//8:repeated presses toggles through head - eyes - mouth
//4: r-arm 5: chest 6: l-arm
//1: r-leg 2: groin 3: l-leg

///Validate the client's mob has a valid zone selected
/client/proc/check_has_body_select()
	return mob && mob.hud_used && mob.hud_used.zone_select && istype(mob.hud_used.zone_select, /obj/screen/zone_sel)

/**
  * Hidden verb to set the target zone of a mob to the head
  *
  * (bound to 8) - repeated presses toggles through head - eyes - mouth
  */
/client/verb/body_toggle_head()
	set name = "body-toggle-head"
	set hidden = 1

	if(!check_has_body_select())
		return

	var/next_in_line
	switch(mob.zone_selected)
		if(BODY_ZONE_HEAD)
			next_in_line = BODY_ZONE_PRECISE_EYES
		if(BODY_ZONE_PRECISE_EYES)
			next_in_line = BODY_ZONE_PRECISE_MOUTH
		else
			next_in_line = BODY_ZONE_HEAD

	var/obj/screen/zone_sel/selector = mob.hud_used.zone_select
	selector.set_selected_zone(next_in_line, mob)

///Hidden verb to target the right arm, bound to 4
/client/verb/body_r_arm()
	set name = "body-r-arm"
	set hidden = 1

	if(!check_has_body_select())
		return

	var/obj/screen/zone_sel/selector = mob.hud_used.zone_select
	selector.set_selected_zone(BODY_ZONE_R_ARM, mob)

///Hidden verb to target the chest, bound to 5
/client/verb/body_chest()
	set name = "body-chest"
	set hidden = 1

	if(!check_has_body_select())
		return

	var/obj/screen/zone_sel/selector = mob.hud_used.zone_select
	selector.set_selected_zone(BODY_ZONE_CHEST, mob)

///Hidden verb to target the left arm, bound to 6
/client/verb/body_l_arm()
	set name = "body-l-arm"
	set hidden = 1

	if(!check_has_body_select())
		return

	var/obj/screen/zone_sel/selector = mob.hud_used.zone_select
	selector.set_selected_zone(BODY_ZONE_L_ARM, mob)

///Hidden verb to target the right leg, bound to 1
/client/verb/body_r_leg()
	set name = "body-r-leg"
	set hidden = 1

	if(!check_has_body_select())
		return

	var/obj/screen/zone_sel/selector = mob.hud_used.zone_select
	selector.set_selected_zone(BODY_ZONE_R_LEG, mob)

///Hidden verb to target the groin, bound to 2
/client/verb/body_groin()
	set name = "body-groin"
	set hidden = 1

	if(!check_has_body_select())
		return

	var/obj/screen/zone_sel/selector = mob.hud_used.zone_select
	selector.set_selected_zone(BODY_ZONE_PRECISE_GROIN, mob)

///Hidden verb to target the left leg, bound to 3
/client/verb/body_l_leg()
	set name = "body-l-leg"
	set hidden = 1

	if(!check_has_body_select())
		return

	var/obj/screen/zone_sel/selector = mob.hud_used.zone_select
	selector.set_selected_zone(BODY_ZONE_L_LEG, mob)

///Verb to toggle the walk or run status
/client/verb/toggle_walk_run()
	set name = "toggle-walk-run"
	set hidden = TRUE
	set instant = TRUE
	if(mob)
		mob.toggle_move_intent(usr)

/**
  * Toggle the move intent of the mob
  *
  * triggers an update the move intent hud as well
  */
/mob/proc/toggle_move_intent(mob/user)
	if(m_intent == MOVE_INTENT_RUN)
		m_intent = MOVE_INTENT_WALK
	else
		m_intent = MOVE_INTENT_RUN
	if(hud_used && hud_used.static_inventory)
		for(var/obj/screen/mov_intent/selector in hud_used.static_inventory)
			selector.update_icon()

///Moves a mob upwards in z level
/mob/verb/up()
	set name = "Move Upwards"
	set category = "IC"

	if(zMove(UP, TRUE))
		to_chat(src, "<span class='notice'>You move upwards.</span>")

///Moves a mob down a z level
/mob/verb/down()
	set name = "Move Down"
	set category = "IC"

	if(zMove(DOWN, TRUE))
		to_chat(src, "<span class='notice'>You move down.</span>")

///Move a mob between z levels, if it's valid to move z's on this turf
/mob/proc/zMove(dir, feedback = FALSE)
	if(dir != UP && dir != DOWN)
		return FALSE
	var/turf/target = get_step_multiz(src, dir)
	if(!target)
		if(feedback)
			to_chat(src, "<span class='warning'>There's nothing in that direction!</span>")
		return FALSE
	if(!canZMove(dir, target))
		if(feedback)
			to_chat(src, "<span class='warning'>You couldn't move there!</span>")
		return FALSE
	forceMove(target)
	return TRUE

/// Can this mob move between z levels
/mob/proc/canZMove(direction, turf/target)
	return FALSE
