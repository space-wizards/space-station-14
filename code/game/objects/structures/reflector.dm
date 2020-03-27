/obj/structure/reflector
	name = "reflector base"
	icon = 'icons/obj/structures.dmi'
	icon_state = "reflector_map"
	desc = "A base for reflector assemblies."
	anchored = FALSE
	density = FALSE
	var/deflector_icon_state
	var/image/deflector_overlay
	var/finished = FALSE
	var/admin = FALSE //Can't be rotated or deconstructed
	var/can_rotate = TRUE
	var/framebuildstacktype = /obj/item/stack/sheet/metal
	var/framebuildstackamount = 5
	var/buildstacktype = /obj/item/stack/sheet/metal
	var/buildstackamount = 0
	var/list/allowed_projectile_typecache = list(/obj/projectile/beam)
	var/rotation_angle = -1

/obj/structure/reflector/Initialize()
	. = ..()
	icon_state = "reflector_base"
	allowed_projectile_typecache = typecacheof(allowed_projectile_typecache)
	if(deflector_icon_state)
		deflector_overlay = image(icon, deflector_icon_state)
		add_overlay(deflector_overlay)

	if(rotation_angle == -1)
		setAngle(dir2angle(dir))
	else
		setAngle(rotation_angle)

	if(admin)
		can_rotate = FALSE

/obj/structure/reflector/examine(mob/user)
	. = ..()
	if(finished)
		. += "It is set to [rotation_angle] degrees, and the rotation is [can_rotate ? "unlocked" : "locked"]."
		if(!admin)
			if(can_rotate)
				. += "<span class='notice'>Alt-click to adjust its direction.</span>"
			else
				. += "<span class='notice'>Use screwdriver to unlock the rotation.</span>"

/obj/structure/reflector/proc/setAngle(new_angle)
	if(can_rotate)
		rotation_angle = new_angle
		if(deflector_overlay)
			cut_overlay(deflector_overlay)
			deflector_overlay.transform = turn(matrix(), new_angle)
			add_overlay(deflector_overlay)


/obj/structure/reflector/setDir(new_dir)
	return ..(NORTH)

/obj/structure/reflector/proc/dir_map_to_angle(dir)
	return 0

/obj/structure/reflector/bullet_act(obj/projectile/P)
	var/pdir = P.dir
	var/pangle = P.Angle
	var/ploc = get_turf(P)
	if(!finished || !allowed_projectile_typecache[P.type] || !(P.dir in GLOB.cardinals))
		return ..()
	if(auto_reflect(P, pdir, ploc, pangle) != BULLET_ACT_FORCE_PIERCE)
		return ..()
	return BULLET_ACT_FORCE_PIERCE

/obj/structure/reflector/proc/auto_reflect(obj/projectile/P, pdir, turf/ploc, pangle)
	P.ignore_source_check = TRUE
	P.range = P.decayedRange
	P.decayedRange = max(P.decayedRange--, 0)
	return BULLET_ACT_FORCE_PIERCE

/obj/structure/reflector/attackby(obj/item/W, mob/user, params)
	if(admin)
		return

	if(W.tool_behaviour == TOOL_SCREWDRIVER)
		can_rotate = !can_rotate
		to_chat(user, "<span class='notice'>You [can_rotate ? "unlock" : "lock"] [src]'s rotation.</span>")
		W.play_tool_sound(src)
		return

	if(W.tool_behaviour == TOOL_WRENCH)
		if(anchored)
			to_chat(user, "<span class='warning'>Unweld [src] from the floor first!</span>")
			return
		user.visible_message("<span class='notice'>[user] starts to dismantle [src].</span>", "<span class='notice'>You start to dismantle [src]...</span>")
		if(W.use_tool(src, user, 80, volume=50))
			to_chat(user, "<span class='notice'>You dismantle [src].</span>")
			new framebuildstacktype(drop_location(), framebuildstackamount)
			if(buildstackamount)
				new buildstacktype(drop_location(), buildstackamount)
			qdel(src)
	else if(W.tool_behaviour == TOOL_WELDER)
		if(obj_integrity < max_integrity)
			if(!W.tool_start_check(user, amount=0))
				return

			user.visible_message("<span class='notice'>[user] starts to repair [src].</span>",
								"<span class='notice'>You begin repairing [src]...</span>",
								"<span class='hear'>You hear welding.</span>")
			if(W.use_tool(src, user, 40, volume=40))
				obj_integrity = max_integrity
				user.visible_message("<span class='notice'>[user] has repaired [src].</span>", \
									"<span class='notice'>You finish repairing [src].</span>")

		else if(!anchored)
			if(!W.tool_start_check(user, amount=0))
				return

			user.visible_message("<span class='notice'>[user] starts to weld [src] to the floor.</span>",
								"<span class='notice'>You start to weld [src] to the floor...</span>",
								"<span class='hear'>You hear welding.</span>")
			if (W.use_tool(src, user, 20, volume=50))
				setAnchored(TRUE)
				to_chat(user, "<span class='notice'>You weld [src] to the floor.</span>")
		else
			if(!W.tool_start_check(user, amount=0))
				return

			user.visible_message("<span class='notice'>[user] starts to cut [src] free from the floor.</span>",
								"<span class='notice'>You start to cut [src] free from the floor...</span>",
								"<span class='hear'>You hear welding.</span>")
			if (W.use_tool(src, user, 20, volume=50))
				setAnchored(FALSE)
				to_chat(user, "<span class='notice'>You cut [src] free from the floor.</span>")

	//Finishing the frame
	else if(istype(W, /obj/item/stack/sheet))
		if(finished)
			return
		var/obj/item/stack/sheet/S = W
		if(istype(S, /obj/item/stack/sheet/glass))
			if(S.use(5))
				new /obj/structure/reflector/single(drop_location())
				qdel(src)
			else
				to_chat(user, "<span class='warning'>You need five sheets of glass to create a reflector!</span>")
				return
		if(istype(S, /obj/item/stack/sheet/rglass))
			if(S.use(10))
				new /obj/structure/reflector/double(drop_location())
				qdel(src)
			else
				to_chat(user, "<span class='warning'>You need ten sheets of reinforced glass to create a double reflector!</span>")
				return
		if(istype(S, /obj/item/stack/sheet/mineral/diamond))
			if(S.use(1))
				new /obj/structure/reflector/box(drop_location())
				qdel(src)
	else
		return ..()

/obj/structure/reflector/proc/rotate(mob/user)
	if (!can_rotate || admin)
		to_chat(user, "<span class='warning'>The rotation is locked!</span>")
		return FALSE
	var/new_angle = input(user, "Input a new angle for primary reflection face.", "Reflector Angle", rotation_angle) as null|num
	if(!user.canUseTopic(src, BE_CLOSE, ismonkey(user)))
		return
	if(!isnull(new_angle))
		setAngle(SIMPLIFY_DEGREES(new_angle))
	return TRUE

/obj/structure/reflector/AltClick(mob/user)
	if(!user.canUseTopic(src, BE_CLOSE, ismonkey(user)))
		return
	else if(finished)
		rotate(user)


//TYPES OF REFLECTORS, SINGLE, DOUBLE, BOX

//SINGLE

/obj/structure/reflector/single
	name = "reflector"
	deflector_icon_state = "reflector"
	desc = "An angled mirror for reflecting laser beams."
	density = TRUE
	finished = TRUE
	buildstacktype = /obj/item/stack/sheet/glass
	buildstackamount = 5

/obj/structure/reflector/single/anchored
	anchored = TRUE

/obj/structure/reflector/single/mapping
	admin = TRUE
	anchored = TRUE

/obj/structure/reflector/single/auto_reflect(obj/projectile/P, pdir, turf/ploc, pangle)
	var/incidence = GET_ANGLE_OF_INCIDENCE(rotation_angle, (P.Angle + 180))
	if(abs(incidence) > 90 && abs(incidence) < 270)
		return FALSE
	var/new_angle = SIMPLIFY_DEGREES(rotation_angle + incidence)
	P.setAngle(new_angle)
	return ..()

//DOUBLE

/obj/structure/reflector/double
	name = "double sided reflector"
	deflector_icon_state = "reflector_double"
	desc = "A double sided angled mirror for reflecting laser beams."
	density = TRUE
	finished = TRUE
	buildstacktype = /obj/item/stack/sheet/rglass
	buildstackamount = 10

/obj/structure/reflector/double/anchored
	anchored = TRUE

/obj/structure/reflector/double/mapping
	admin = TRUE
	anchored = TRUE

/obj/structure/reflector/double/auto_reflect(obj/projectile/P, pdir, turf/ploc, pangle)
	var/incidence = GET_ANGLE_OF_INCIDENCE(rotation_angle, (P.Angle + 180))
	var/new_angle = SIMPLIFY_DEGREES(rotation_angle + incidence)
	P.setAngle(new_angle)
	return ..()

//BOX

/obj/structure/reflector/box
	name = "reflector box"
	deflector_icon_state = "reflector_box"
	desc = "A box with an internal set of mirrors that reflects all laser beams in a single direction."
	density = TRUE
	finished = TRUE
	buildstacktype = /obj/item/stack/sheet/mineral/diamond
	buildstackamount = 1

/obj/structure/reflector/box/anchored
	anchored = TRUE

/obj/structure/reflector/box/mapping
	admin = TRUE
	anchored = TRUE

/obj/structure/reflector/box/auto_reflect(obj/projectile/P)
	P.setAngle(rotation_angle)
	return ..()

/obj/structure/reflector/ex_act()
	if(admin)
		return
	else
		return ..()

/obj/structure/reflector/dir_map_to_angle(dir)
	return dir2angle(dir)

/obj/structure/reflector/singularity_act()
	if(admin)
		return
	else
		return ..()
