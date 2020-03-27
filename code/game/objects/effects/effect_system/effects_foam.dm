// Foam
// Similar to smoke, but slower and mobs absorb its reagent through their exposed skin.
#define ALUMINUM_FOAM 1
#define IRON_FOAM 2
#define RESIN_FOAM 3


/obj/effect/particle_effect/foam
	name = "foam"
	icon_state = "foam"
	opacity = 0
	anchored = TRUE
	density = FALSE
	layer = EDGED_TURF_LAYER
	mouse_opacity = MOUSE_OPACITY_TRANSPARENT
	var/amount = 3
	animate_movement = NO_STEPS
	var/metal = 0
	var/lifetime = 40
	var/reagent_divisor = 7
	var/static/list/blacklisted_turfs = typecacheof(list(
	/turf/open/space/transit,
	/turf/open/chasm,
	/turf/open/lava))
	var/slippery_foam = TRUE

/obj/effect/particle_effect/foam/firefighting
	name = "firefighting foam"
	lifetime = 20 //doesn't last as long as normal foam
	amount = 0 //no spread
	slippery_foam = FALSE
	var/absorbed_plasma = 0

/obj/effect/particle_effect/foam/firefighting/process()
	..()

	var/turf/open/T = get_turf(src)
	var/obj/effect/hotspot/hotspot = (locate(/obj/effect/hotspot) in T)
	if(hotspot && istype(T) && T.air)
		qdel(hotspot)
		var/datum/gas_mixture/G = T.air
		var/plas_amt = min(30,G.gases[/datum/gas/plasma][MOLES]) //Absorb some plasma
		G.gases[/datum/gas/plasma][MOLES] -= plas_amt
		absorbed_plasma += plas_amt
		if(G.temperature > T20C)
			G.temperature = max(G.temperature/2,T20C)
		G.garbage_collect()
		T.air_update_turf()

/obj/effect/particle_effect/foam/firefighting/kill_foam()
	STOP_PROCESSING(SSfastprocess, src)

	if(absorbed_plasma)
		var/obj/effect/decal/cleanable/plasma/P = (locate(/obj/effect/decal/cleanable/plasma) in get_turf(src))
		if(!P)
			P = new(loc)
		P.reagents.add_reagent(/datum/reagent/stable_plasma, absorbed_plasma)

	flick("[icon_state]-disolve", src)
	QDEL_IN(src, 5)

/obj/effect/particle_effect/foam/firefighting/foam_mob(mob/living/L)
	if(!istype(L))
		return
	L.adjust_fire_stacks(-2)
	L.ExtinguishMob()

/obj/effect/particle_effect/foam/firefighting/temperature_expose(datum/gas_mixture/air, exposed_temperature, exposed_volume)
	return

/obj/effect/particle_effect/foam/metal
	name = "aluminium foam"
	metal = ALUMINUM_FOAM
	icon_state = "mfoam"
	slippery_foam = FALSE

/obj/effect/particle_effect/foam/metal/smart
	name = "smart foam"

/obj/effect/particle_effect/foam/metal/iron
	name = "iron foam"
	metal = IRON_FOAM

/obj/effect/particle_effect/foam/metal/resin
	name = "resin foam"
	metal = RESIN_FOAM

/obj/effect/particle_effect/foam/long_life
	lifetime = 150

/obj/effect/particle_effect/foam/Initialize()
	. = ..()
	create_reagents(1000) //limited by the size of the reagent holder anyway.
	START_PROCESSING(SSfastprocess, src)
	playsound(src, 'sound/effects/bubbles2.ogg', 80, TRUE, -3)

/obj/effect/particle_effect/foam/ComponentInitialize()
	. = ..()
	if(slippery_foam)
		AddComponent(/datum/component/slippery, 100)

/obj/effect/particle_effect/foam/Destroy()
	STOP_PROCESSING(SSfastprocess, src)
	return ..()


/obj/effect/particle_effect/foam/proc/kill_foam()
	STOP_PROCESSING(SSfastprocess, src)
	switch(metal)
		if(ALUMINUM_FOAM)
			new /obj/structure/foamedmetal(get_turf(src))
		if(IRON_FOAM)
			new /obj/structure/foamedmetal/iron(get_turf(src))
		if(RESIN_FOAM)
			new /obj/structure/foamedmetal/resin(get_turf(src))
	flick("[icon_state]-disolve", src)
	QDEL_IN(src, 5)

/obj/effect/particle_effect/foam/smart/kill_foam() //Smart foam adheres to area borders for walls
	STOP_PROCESSING(SSfastprocess, src)
	if(metal)
		var/turf/T = get_turf(src)
		if(isspaceturf(T)) //Block up any exposed space
			T.PlaceOnTop(/turf/open/floor/plating/foam, flags = CHANGETURF_INHERIT_AIR)
		for(var/direction in GLOB.cardinals)
			var/turf/cardinal_turf = get_step(T, direction)
			if(get_area(cardinal_turf) != get_area(T)) //We're at an area boundary, so let's block off this turf!
				new/obj/structure/foamedmetal(T)
				break
	flick("[icon_state]-disolve", src)
	QDEL_IN(src, 5)

/obj/effect/particle_effect/foam/process()
	lifetime--
	if(lifetime < 1)
		kill_foam()
		return

	var/fraction = 1/initial(reagent_divisor)
	for(var/obj/O in range(0,src))
		if(O.type == src.type)
			continue
		if(isturf(O.loc))
			var/turf/T = O.loc
			if(T.intact && O.level == 1) //hidden under the floor
				continue
		if(lifetime % reagent_divisor)
			reagents.reaction(O, VAPOR, fraction)
	var/hit = 0
	for(var/mob/living/L in range(0,src))
		hit += foam_mob(L)
	if(hit)
		lifetime++ //this is so the decrease from mobs hit and the natural decrease don't cumulate.
	var/T = get_turf(src)
	if(lifetime % reagent_divisor)
		reagents.reaction(T, VAPOR, fraction)

	if(--amount < 0)
		return
	spread_foam()

/obj/effect/particle_effect/foam/proc/foam_mob(mob/living/L)
	if(lifetime<1)
		return 0
	if(!istype(L))
		return 0
	var/fraction = 1/initial(reagent_divisor)
	if(lifetime % reagent_divisor)
		reagents.reaction(L, VAPOR, fraction)
	lifetime--
	return 1

/obj/effect/particle_effect/foam/proc/spread_foam()
	var/turf/t_loc = get_turf(src)
	for(var/turf/T in t_loc.reachableAdjacentTurfs())
		var/obj/effect/particle_effect/foam/foundfoam = locate() in T //Don't spread foam where there's already foam!
		if(foundfoam)
			continue

		if(is_type_in_typecache(T, blacklisted_turfs))
			continue

		for(var/mob/living/L in T)
			foam_mob(L)
		var/obj/effect/particle_effect/foam/F = new src.type(T)
		F.amount = amount
		reagents.copy_to(F, (reagents.total_volume))
		F.add_atom_colour(color, FIXED_COLOUR_PRIORITY)
		F.metal = metal


/obj/effect/particle_effect/foam/temperature_expose(datum/gas_mixture/air, exposed_temperature, exposed_volume)
	if(prob(max(0, exposed_temperature - 475))) //foam dissolves when heated
		kill_foam()


/obj/effect/particle_effect/foam/metal/temperature_expose(datum/gas_mixture/air, exposed_temperature, exposed_volume)
	return


///////////////////////////////////////////////
//FOAM EFFECT DATUM
/datum/effect_system/foam_spread
	var/amount = 10		// the size of the foam spread.
	var/obj/chemholder
	effect_type = /obj/effect/particle_effect/foam
	var/metal = 0


/datum/effect_system/foam_spread/metal
	effect_type = /obj/effect/particle_effect/foam/metal


/datum/effect_system/foam_spread/metal/smart
	effect_type = /obj/effect/particle_effect/foam/smart


/datum/effect_system/foam_spread/long
	effect_type = /obj/effect/particle_effect/foam/long_life

/datum/effect_system/foam_spread/New()
	..()
	chemholder = new /obj()
	var/datum/reagents/R = new/datum/reagents(1000)
	chemholder.reagents = R
	R.my_atom = chemholder

/datum/effect_system/foam_spread/Destroy()
	qdel(chemholder)
	chemholder = null
	return ..()

/datum/effect_system/foam_spread/set_up(amt=5, loca, datum/reagents/carry = null, metaltype = 0)
	if(isturf(loca))
		location = loca
	else
		location = get_turf(loca)

	amount = round(sqrt(amt / 2), 1)
	carry.copy_to(chemholder, carry.total_volume)
	if(metaltype)
		metal = metaltype

/datum/effect_system/foam_spread/start()
	var/obj/effect/particle_effect/foam/F = new effect_type(location)
	var/foamcolor = mix_color_from_reagents(chemholder.reagents.reagent_list)
	chemholder.reagents.copy_to(F, chemholder.reagents.total_volume/amount)
	F.add_atom_colour(foamcolor, FIXED_COLOUR_PRIORITY)
	F.amount = amount
	F.metal = metal


//////////////////////////////////////////////////////////
// FOAM STRUCTURE. Formed by metal foams. Dense and opaque, but easy to break
/obj/structure/foamedmetal
	icon = 'icons/effects/effects.dmi'
	icon_state = "metalfoam"
	density = TRUE
	opacity = 1 	// changed in New()
	anchored = TRUE
	layer = EDGED_TURF_LAYER
	resistance_flags = FIRE_PROOF | ACID_PROOF
	name = "foamed metal"
	desc = "A lightweight foamed metal wall."
	gender = PLURAL
	max_integrity = 20
	CanAtmosPass = ATMOS_PASS_DENSITY

/obj/structure/foamedmetal/Initialize()
	. = ..()
	air_update_turf(1)

/obj/structure/foamedmetal/Move()
	var/turf/T = loc
	. = ..()
	move_update_air(T)

/obj/structure/foamedmetal/attack_paw(mob/user)
	return attack_hand(user)

/obj/structure/foamedmetal/play_attack_sound(damage_amount, damage_type = BRUTE, damage_flag = 0)
	playsound(src.loc, 'sound/weapons/tap.ogg', 100, TRUE)

/obj/structure/foamedmetal/attack_hand(mob/user)
	. = ..()
	if(.)
		return
	user.changeNext_move(CLICK_CD_MELEE)
	user.do_attack_animation(src, ATTACK_EFFECT_PUNCH)
	to_chat(user, "<span class='warning'>You hit [src] but bounce off it!</span>")
	playsound(src.loc, 'sound/weapons/tap.ogg', 100, TRUE)

/obj/structure/foamedmetal/iron
	max_integrity = 50
	icon_state = "ironfoam"

//Atmos Backpack Resin, transparent, prevents atmos and filters the air
/obj/structure/foamedmetal/resin
	name = "\improper ATMOS Resin"
	desc = "A lightweight, transparent resin used to suffocate fires, scrub the air of toxins, and restore the air to a safe temperature."
	opacity = FALSE
	icon_state = "atmos_resin"
	alpha = 120
	max_integrity = 10

/obj/structure/foamedmetal/resin/Initialize()
	. = ..()
	if(isopenturf(loc))
		var/turf/open/O = loc
		O.ClearWet()
		if(O.air)
			var/datum/gas_mixture/G = O.air
			G.temperature = 293.15
			for(var/obj/effect/hotspot/H in O)
				qdel(H)
			var/list/G_gases = G.gases
			for(var/I in G_gases)
				if(I == /datum/gas/oxygen || I == /datum/gas/nitrogen)
					continue
				G_gases[I][MOLES] = 0
			G.garbage_collect()
			O.air_update_turf()
		for(var/obj/machinery/atmospherics/components/unary/U in O)
			if(!U.welded)
				U.welded = TRUE
				U.update_icon()
				U.visible_message("<span class='danger'>[U] sealed shut!</span>")
		for(var/mob/living/L in O)
			L.ExtinguishMob()
		for(var/obj/item/Item in O)
			Item.extinguish()

/obj/structure/foamedmetal/resin/CanAllowThrough(atom/movable/mover, turf/target)
	. = ..()
	if(istype(mover) && (mover.pass_flags & PASSGLASS))
		return TRUE

#undef ALUMINUM_FOAM
#undef IRON_FOAM
#undef RESIN_FOAM
