/obj/item/grenade/c4
	name = "C-4 charge"
	desc = "Used to put holes in specific areas without too much extra hole. A saboteur's favorite."
	icon_state = "plastic-explosive0"
	item_state = "plastic-explosive"
	lefthand_file = 'icons/mob/inhands/weapons/bombs_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/weapons/bombs_righthand.dmi'
	item_flags = NOBLUDGEON
	flags_1 = NONE
	det_time = 10
	display_timer = FALSE
	w_class = WEIGHT_CLASS_SMALL
	gender = PLURAL
	var/atom/target = null
	var/mutable_appearance/plastic_overlay
	var/directional = FALSE
	var/aim_dir = NORTH
	var/boom_sizes = list(0, 0, 3)
	var/full_damage_on_mobs = FALSE

/obj/item/grenade/c4/Initialize()
	. = ..()
	plastic_overlay = mutable_appearance(icon, "[item_state]2", HIGH_OBJ_LAYER)
	wires = new /datum/wires/explosive/c4(src)

/obj/item/grenade/c4/ComponentInitialize()
	. = ..()
	AddComponent(/datum/component/empprotection, EMP_PROTECT_WIRES)

/obj/item/grenade/c4/Destroy()
	qdel(wires)
	wires = null
	target = null
	..()

/obj/item/grenade/c4/attackby(obj/item/I, mob/user, params)
	if(I.tool_behaviour == TOOL_SCREWDRIVER)
		to_chat(user, "<span class='notice'>The wire panel can be accessed without a screwdriver.</span>")
	else if(is_wire_tool(I))
		wires.interact(user)
	else
		return ..()

/obj/item/grenade/c4/prime()
	if(QDELETED(src))
		return
	var/turf/location
	if(target)
		if(!QDELETED(target))
			location = get_turf(target)
			target.cut_overlay(plastic_overlay, TRUE)
			if(!ismob(target) || full_damage_on_mobs)
				target.ex_act(EXPLODE_HEAVY, target)
	else
		location = get_turf(src)
	if(location)
		if(directional && target && target.density)
			var/turf/T = get_step(location, aim_dir)
			explosion(get_step(T, aim_dir), boom_sizes[1], boom_sizes[2], boom_sizes[3])
		else
			explosion(location, boom_sizes[1], boom_sizes[2], boom_sizes[3])
	qdel(src)

//assembly stuff
/obj/item/grenade/c4/receive_signal()
	prime()

/obj/item/grenade/c4/attack_self(mob/user)
	var/newtime = input(usr, "Please set the timer.", "Timer", 10) as num|null

	if (isnull(newtime))
		return

	if(user.get_active_held_item() == src)
		newtime = CLAMP(newtime, 10, 60000)
		det_time = newtime
		to_chat(user, "Timer set for [det_time] seconds.")

/obj/item/grenade/c4/afterattack(atom/movable/AM, mob/user, flag)
	. = ..()
	aim_dir = get_dir(user,AM)
	if(!flag)
		return

	to_chat(user, "<span class='notice'>You start planting [src]. The timer is set to [det_time]...</span>")

	if(do_after(user, 30, target = AM))
		if(!user.temporarilyRemoveItemFromInventory(src))
			return
		target = AM

		message_admins("[ADMIN_LOOKUPFLW(user)] planted [name] on [target.name] at [ADMIN_VERBOSEJMP(target)] with [det_time] second fuse")
		log_game("[key_name(user)] planted [name] on [target.name] at [AREACOORD(user)] with a [det_time] second fuse")

		notify_ghosts("[user] has planted \a [src] on [target] with a [det_time] second fuse!", source = target, action = NOTIFY_ORBIT, flashwindow = FALSE, header = "Explosive Planted")

		moveToNullspace()	//Yep

		if(istype(AM, /obj/item)) //your crappy throwing star can't fly so good with a giant brick of c4 on it.
			var/obj/item/I = AM
			I.throw_speed = max(1, (I.throw_speed - 3))
			I.throw_range = max(1, (I.throw_range - 3))
			I.embedding = I.embedding.setRating(embed_chance = 0)
		else if(istype(AM, /mob/living))
			plastic_overlay.layer = FLOAT_LAYER

		target.add_overlay(plastic_overlay)
		to_chat(user, "<span class='notice'>You plant the bomb. Timer counting down from [det_time].</span>")
		addtimer(CALLBACK(src, .proc/prime), det_time*10)

/obj/item/grenade/c4/proc/shout_syndicate_crap(mob/M)
	if(!M)
		return
	var/message_say = "FOR NO RAISIN!"
	if(M.mind)
		var/datum/mind/UM = M.mind
		if(UM.has_antag_datum(/datum/antagonist/nukeop) || UM.has_antag_datum(/datum/antagonist/traitor))
			message_say = "FOR THE SYNDICATE!"
		else if(UM.has_antag_datum(/datum/antagonist/changeling))
			message_say = "FOR THE HIVE!"
		else if(UM.has_antag_datum(/datum/antagonist/cult))
			message_say = "FOR NAR'SIE!"
		else if(UM.has_antag_datum(/datum/antagonist/rev))
			message_say = "VIVA LA REVOLUTION!"
		else if(UM.has_antag_datum(/datum/antagonist/brother))
			message_say = "FOR MY BROTHER!"
		else if(UM.has_antag_datum(/datum/antagonist/ninja))
			message_say = "FOR THE SPIDER CLAN!"
		else if(UM.has_antag_datum(/datum/antagonist/fugitive))
			message_say = "FOR FREEDOM!"
		else if(UM.has_antag_datum(/datum/antagonist/ashwalker))
			message_say = "I HAVE NO IDEA WHAT THIS THING DOES!"
		else if(UM.has_antag_datum(/datum/antagonist/ert))
			message_say = "FOR NANOTRASEN!"
		else if(UM.has_antag_datum(/datum/antagonist/pirate))
			message_say = "FOR ME MATEYS!"
		else if(UM.has_antag_datum(/datum/antagonist/wizard))
			message_say = "FOR THE FEDERATION!"
	M.say(message_say, forced="C4 suicide")

/obj/item/grenade/c4/suicide_act(mob/user)
	message_admins("[ADMIN_LOOKUPFLW(user)] suicided with [src] at [ADMIN_VERBOSEJMP(user)]")
	log_game("[key_name(user)] suicided with [src] at [AREACOORD(user)]")
	user.visible_message("<span class='suicide'>[user] activates [src] and holds it above [user.p_their()] head! It looks like [user.p_theyre()] going out with a bang!</span>")
	shout_syndicate_crap(user)
	explosion(user,0,2,0) //Cheap explosion imitation because putting prime() here causes runtimes
	user.gib(1, 1)
	qdel(src)

// X4 is an upgraded directional variant of c4 which is relatively safe to be standing next to. And much less safe to be standing on the other side of.
// C4 is intended to be used for infiltration, and destroying tech. X4 is intended to be used for heavy breaching and tight spaces.
// Intended to replace C4 for nukeops, and to be a randomdrop in surplus/random traitor purchases.

/obj/item/grenade/c4/x4
	name = "X-4 charge"
	desc = "A shaped high-explosive breaching charge. Designed to ensure user safety and wall nonsafety."
	icon_state = "plasticx40"
	item_state = "plasticx4"
	directional = TRUE
	boom_sizes = list(0, 2, 5)
