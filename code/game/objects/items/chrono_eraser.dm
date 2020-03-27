#define CHRONO_BEAM_RANGE 3
#define CHRONO_FRAME_COUNT 22
/obj/item/chrono_eraser
	name = "Timestream Eradication Device"
	desc = "The result of outlawed time-bluespace research, this device is capable of wiping a being from the timestream. They never are, they never were, they never will be."
	icon = 'icons/obj/chronos.dmi'
	icon_state = "chronobackpack"
	item_state = "backpack"
	lefthand_file = 'icons/mob/inhands/equipment/backpack_lefthand.dmi'
	righthand_file = 'icons/mob/inhands/equipment/backpack_righthand.dmi'
	w_class = WEIGHT_CLASS_BULKY
	slot_flags = ITEM_SLOT_BACK
	slowdown = 1
	actions_types = list(/datum/action/item_action/equip_unequip_TED_Gun)
	var/obj/item/gun/energy/chrono_gun/PA = null
	var/list/erased_minds = list() //a collection of minds from the dead

/obj/item/chrono_eraser/proc/pass_mind(datum/mind/M)
	erased_minds += M

/obj/item/chrono_eraser/dropped()
	..()
	if(PA)
		qdel(PA)

/obj/item/chrono_eraser/Destroy()
	dropped()
	return ..()

/obj/item/chrono_eraser/ui_action_click(mob/user)
	if(iscarbon(user))
		var/mob/living/carbon/C = user
		if(C.back == src)
			if(PA)
				qdel(PA)
			else
				PA = new(src)
				user.put_in_hands(PA)

/obj/item/chrono_eraser/item_action_slot_check(slot, mob/user)
	if(slot == ITEM_SLOT_BACK)
		return 1

/obj/item/gun/energy/chrono_gun
	name = "T.E.D. Projection Apparatus"
	desc = "It's as if they never existed in the first place."
	icon = 'icons/obj/chronos.dmi'
	icon_state = "chronogun"
	item_state = "chronogun"
	w_class = WEIGHT_CLASS_NORMAL
	item_flags = DROPDEL
	ammo_type = list(/obj/item/ammo_casing/energy/chrono_beam)
	can_charge = FALSE
	fire_delay = 50
	var/obj/item/chrono_eraser/TED = null
	var/obj/structure/chrono_field/field = null
	var/turf/startpos = null

/obj/item/gun/energy/chrono_gun/Initialize()
	. = ..()
	ADD_TRAIT(src, TRAIT_NODROP, CHRONO_GUN_TRAIT)
	if(istype(loc, /obj/item/chrono_eraser))
		TED = loc
	else //admin must have spawned it
		TED = new(src.loc)
		return INITIALIZE_HINT_QDEL

/obj/item/gun/energy/chrono_gun/ComponentInitialize()
	. = ..()
	AddElement(/datum/element/update_icon_blocker)

/obj/item/gun/energy/chrono_gun/process_fire(atom/target, mob/living/user, message = TRUE, params = null, zone_override = "", bonus_spread = 0)
	if(field)
		field_disconnect(field)
	..()

/obj/item/gun/energy/chrono_gun/Destroy()
	if(TED)
		TED.PA = null
		TED = null
	if(field)
		field_disconnect(field)
	return ..()

/obj/item/gun/energy/chrono_gun/proc/field_connect(obj/structure/chrono_field/F)
	var/mob/living/user = loc
	if(F.gun)
		if(isliving(user) && F.captured)
			to_chat(user, "<span class='alert'><b>FAIL: <i>[F.captured]</i> already has an existing connection.</b></span>")
		field_disconnect(F)
	else
		startpos = get_turf(src)
		field = F
		F.gun = src
		if(isliving(user) && F.captured)
			to_chat(user, "<span class='notice'>Connection established with target: <b>[F.captured]</b></span>")


/obj/item/gun/energy/chrono_gun/proc/field_disconnect(obj/structure/chrono_field/F)
	if(F && field == F)
		var/mob/living/user = loc
		if(F.gun == src)
			F.gun = null
		if(isliving(user) && F.captured)
			to_chat(user, "<span class='alert'>Disconnected from target: <b>[F.captured]</b></span>")
	field = null
	startpos = null

/obj/item/gun/energy/chrono_gun/proc/field_check(obj/structure/chrono_field/F)
	if(F)
		if(field == F)
			var/turf/currentpos = get_turf(src)
			var/mob/living/user = loc
			if((currentpos == startpos) && (field in view(CHRONO_BEAM_RANGE, currentpos)) && (user.mobility_flags & MOBILITY_STAND) && (user.stat == CONSCIOUS))
				return 1
		field_disconnect(F)
		return 0

/obj/item/gun/energy/chrono_gun/proc/pass_mind(datum/mind/M)
	if(TED)
		TED.pass_mind(M)


/obj/projectile/energy/chrono_beam
	name = "eradication beam"
	icon_state = "chronobolt"
	range = CHRONO_BEAM_RANGE
	nodamage = TRUE
	var/obj/item/gun/energy/chrono_gun/gun = null

/obj/projectile/energy/chrono_beam/Initialize()
	. = ..()
	var/obj/item/ammo_casing/energy/chrono_beam/C = loc
	if(istype(C))
		gun = C.gun

/obj/projectile/energy/chrono_beam/on_hit(atom/target)
	if(target && gun && isliving(target))
		var/obj/structure/chrono_field/F = new(target.loc, target, gun)
		gun.field_connect(F)


/obj/item/ammo_casing/energy/chrono_beam
	name = "eradication beam"
	projectile_type = /obj/projectile/energy/chrono_beam
	icon_state = "chronobolt"
	e_cost = 0
	var/obj/item/gun/energy/chrono_gun/gun

/obj/item/ammo_casing/energy/chrono_beam/Initialize()
	if(istype(loc))
		gun = loc
	. = ..()





/obj/structure/chrono_field
	name = "eradication field"
	desc = "An aura of time-bluespace energy."
	icon = 'icons/effects/effects.dmi'
	icon_state = "chronofield"
	density = FALSE
	anchored = TRUE
	resistance_flags = INDESTRUCTIBLE | LAVA_PROOF | FIRE_PROOF | UNACIDABLE | ACID_PROOF | FREEZE_PROOF
	move_resist = INFINITY
	interaction_flags_atom = NONE
	var/mob/living/captured = null
	var/obj/item/gun/energy/chrono_gun/gun = null
	var/tickstokill = 15
	var/mutable_appearance/mob_underlay
	var/preloaded = 0
	var/RPpos = null

/obj/structure/chrono_field/Initialize(mapload, mob/living/target, obj/item/gun/energy/chrono_gun/G)
	if(target && isliving(target) && G)
		target.forceMove(src)
		captured = target
		var/icon/mob_snapshot = getFlatIcon(target)
		var/icon/cached_icon = new()

		for(var/i=1, i<=CHRONO_FRAME_COUNT, i++)
			var/icon/removing_frame = icon('icons/obj/chronos.dmi', "erasing", SOUTH, i)
			var/icon/mob_icon = icon(mob_snapshot)
			mob_icon.Blend(removing_frame, ICON_MULTIPLY)
			cached_icon.Insert(mob_icon, "frame[i]")

		mob_underlay = mutable_appearance(cached_icon, "frame1")
		update_icon()

		desc = initial(desc) + "<br><span class='info'>It appears to contain [target.name].</span>"
	START_PROCESSING(SSobj, src)
	return ..()

/obj/structure/chrono_field/Destroy()
	if(gun && gun.field_check(src))
		gun.field_disconnect(src)
	return ..()

/obj/structure/chrono_field/update_icon()
	var/ttk_frame = 1 - (tickstokill / initial(tickstokill))
	ttk_frame = CLAMP(CEILING(ttk_frame * CHRONO_FRAME_COUNT, 1), 1, CHRONO_FRAME_COUNT)
	if(ttk_frame != RPpos)
		RPpos = ttk_frame
		mob_underlay.icon_state = "frame[RPpos]"
		underlays = list() //hack: BYOND refuses to update the underlay to match the icon_state otherwise
		underlays += mob_underlay

/obj/structure/chrono_field/process()
	if(captured)
		if(tickstokill > initial(tickstokill))
			for(var/atom/movable/AM in contents)
				AM.forceMove(drop_location())
			qdel(src)
		else if(tickstokill <= 0)
			to_chat(captured, "<span class='boldnotice'>As the last essence of your being is erased from time, you are taken back to your most enjoyable memory. You feel happy...</span>")
			var/mob/dead/observer/ghost = captured.ghostize(1)
			if(captured.mind)
				if(ghost)
					ghost.mind = null
				if(gun)
					gun.pass_mind(captured.mind)
			qdel(captured)
			qdel(src)
		else
			captured.Unconscious(80)
			if(captured.loc != src)
				captured.forceMove(src)
			update_icon()
			if(gun)
				if(gun.field_check(src))
					tickstokill--
				else
					gun = null
					return .()
			else
				tickstokill++
	else
		qdel(src)

/obj/structure/chrono_field/bullet_act(obj/projectile/P)
	if(istype(P, /obj/projectile/energy/chrono_beam))
		var/obj/projectile/energy/chrono_beam/beam = P
		var/obj/item/gun/energy/chrono_gun/Pgun = beam.gun
		if(Pgun && istype(Pgun))
			Pgun.field_connect(src)
	else
		return BULLET_ACT_HIT

/obj/structure/chrono_field/assume_air()
	return 0

/obj/structure/chrono_field/return_air() //we always have nominal air and temperature
	var/datum/gas_mixture/GM = new
	GM.add_gases(/datum/gas/oxygen, /datum/gas/nitrogen)
	GM.gases[/datum/gas/oxygen][MOLES] = MOLES_O2STANDARD
	GM.gases[/datum/gas/nitrogen][MOLES] = MOLES_N2STANDARD
	GM.temperature = T20C
	return GM

/obj/structure/chrono_field/singularity_act()
	return

/obj/structure/chrono_field/singularity_pull()
	return

/obj/structure/chrono_field/ex_act()
	return

/obj/structure/chrono_field/blob_act(obj/structure/blob/B)
	return


#undef CHRONO_BEAM_RANGE
#undef CHRONO_FRAME_COUNT
