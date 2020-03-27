
/************************
* PORTABLE TURRET COVER *
************************/

/obj/machinery/porta_turret_cover
	name = "turret"
	icon = 'icons/obj/turrets.dmi'
	icon_state = "turretCover"
	layer = HIGH_OBJ_LAYER
	density = FALSE
	max_integrity = 80
	var/obj/machinery/porta_turret/parent_turret = null

/obj/machinery/porta_turret_cover/Destroy()
	if(parent_turret)
		parent_turret.cover = null
		parent_turret.invisibility = 0
		parent_turret = null
	return ..()

//The below code is pretty much just recoded from the initial turret object. It's necessary but uncommented because it's exactly the same!
//>necessary
//I'm not fixing it because i'm fucking bored of this code already, but someone should just reroute these to the parent turret's procs.

/obj/machinery/porta_turret_cover/attack_ai(mob/user)
	. = ..()
	if(.)
		return
	return parent_turret.attack_ai(user)


/obj/machinery/porta_turret_cover/attack_hand(mob/user)
	. = ..()
	if(.)
		return
	return parent_turret.attack_hand(user)


/obj/machinery/porta_turret_cover/attackby(obj/item/I, mob/user, params)
	if(I.tool_behaviour == TOOL_WRENCH && !parent_turret.on)
		if(parent_turret.raised)
			return

		if(!parent_turret.anchored)
			parent_turret.setAnchored(TRUE)
			to_chat(user, "<span class='notice'>You secure the exterior bolts on the turret.</span>")
			parent_turret.invisibility = 0
			parent_turret.update_icon()
		else
			parent_turret.setAnchored(FALSE)
			to_chat(user, "<span class='notice'>You unsecure the exterior bolts on the turret.</span>")
			parent_turret.invisibility = INVISIBILITY_MAXIMUM
			parent_turret.update_icon()
			qdel(src)

	else if(I.GetID())
		if(parent_turret.allowed(user))
			parent_turret.locked = !parent_turret.locked
			to_chat(user, "<span class='notice'>Controls are now [parent_turret.locked ? "locked" : "unlocked"].</span>")
			updateUsrDialog()
		else
			to_chat(user, "<span class='notice'>Access denied.</span>")
	else if(I.tool_behaviour == TOOL_MULTITOOL && !parent_turret.locked)
		if(!multitool_check_buffer(user, I))
			return
		var/obj/item/multitool/M = I
		M.buffer = parent_turret
		to_chat(user, "<span class='notice'>You add [parent_turret] to multitool buffer.</span>")
	else
		return ..()

/obj/machinery/porta_turret_cover/attacked_by(obj/item/I, mob/user)
	parent_turret.attacked_by(I, user)

/obj/machinery/porta_turret_cover/attack_alien(mob/living/carbon/alien/humanoid/user)
	parent_turret.attack_alien(user)

/obj/machinery/porta_turret_cover/attack_animal(mob/living/simple_animal/user)
	parent_turret.attack_animal(user)

/obj/machinery/porta_turret_cover/attack_hulk(mob/living/carbon/human/user)
	return parent_turret.attack_hulk(user)

/obj/machinery/porta_turret_cover/can_be_overridden()
	. = 0

/obj/machinery/porta_turret_cover/emag_act(mob/user)
	if(!(parent_turret.obj_flags & EMAGGED))
		to_chat(user, "<span class='notice'>You short out [parent_turret]'s threat assessment circuits.</span>")
		visible_message("<span class='hear'>[parent_turret] hums oddly...</span>")
		parent_turret.obj_flags |= EMAGGED
		parent_turret.on = FALSE
		addtimer(VARSET_CALLBACK(parent_turret, on, TRUE), 4 SECONDS)
