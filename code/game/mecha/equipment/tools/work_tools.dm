
//Hydraulic clamp, Kill clamp, Extinguisher, RCD, Cable layer.


/obj/item/mecha_parts/mecha_equipment/hydraulic_clamp
	name = "hydraulic clamp"
	desc = "Equipment for engineering exosuits. Lifts objects and loads them into cargo."
	icon_state = "mecha_clamp"
	equip_cooldown = 15
	energy_drain = 10
	tool_behaviour = TOOL_RETRACTOR
	toolspeed = 0.8
	var/dam_force = 20
	var/obj/mecha/working/ripley/cargo_holder
	harmful = TRUE

/obj/item/mecha_parts/mecha_equipment/hydraulic_clamp/can_attach(obj/mecha/working/ripley/M as obj)
	if(..())
		if(istype(M))
			return 1
	return 0

/obj/item/mecha_parts/mecha_equipment/hydraulic_clamp/attach(obj/mecha/M as obj)
	..()
	cargo_holder = M
	return

/obj/item/mecha_parts/mecha_equipment/hydraulic_clamp/detach(atom/moveto = null)
	..()
	cargo_holder = null

/obj/item/mecha_parts/mecha_equipment/hydraulic_clamp/action(atom/target)
	if(!action_checks(target))
		return
	if(!cargo_holder)
		return
	if(ismecha(target))
		var/obj/mecha/M = target
		var/have_ammo
		for(var/obj/item/mecha_ammo/box in cargo_holder.cargo)
			if(istype(box, /obj/item/mecha_ammo) && box.rounds)
				have_ammo = TRUE
				if(M.ammo_resupply(box, chassis.occupant, TRUE))
					return
		if(have_ammo)
			to_chat(chassis.occupant, "No further supplies can be provided to [M].")
		else
			to_chat(chassis.occupant, "No providable supplies found in cargo hold")
		return
	if(isobj(target))
		var/obj/O = target
		if(istype(O, /obj/machinery/door/firedoor))
			var/obj/machinery/door/firedoor/D = O
			D.try_to_crowbar(src,chassis.occupant)
			return
		if(istype(O, /obj/machinery/door/airlock/))
			var/obj/machinery/door/airlock/D = O
			D.try_to_crowbar(src,chassis.occupant)
			return
		if(!O.anchored)
			if(cargo_holder.cargo.len < cargo_holder.cargo_capacity)
				chassis.visible_message("<span class='notice'>[chassis] lifts [target] and starts to load it into cargo compartment.</span>")
				O.anchored = TRUE
				if(do_after_cooldown(target))
					cargo_holder.cargo += O
					O.forceMove(chassis)
					O.anchored = FALSE
					occupant_message("<span class='notice'>[target] successfully loaded.</span>")
					log_message("Loaded [O]. Cargo compartment capacity: [cargo_holder.cargo_capacity - cargo_holder.cargo.len]", LOG_MECHA)
				else
					O.anchored = initial(O.anchored)
			else
				occupant_message("<span class='warning'>Not enough room in cargo compartment!</span>")
		else
			occupant_message("<span class='warning'>[target] is firmly secured!</span>")

	else if(isliving(target))
		var/mob/living/M = target
		if(M.stat == DEAD)
			return
		if(chassis.occupant.a_intent == INTENT_HARM)
			M.take_overall_damage(dam_force)
			if(!M)
				return
			M.adjustOxyLoss(round(dam_force/2))
			M.updatehealth()
			target.visible_message("<span class='danger'>[chassis] squeezes [target]!</span>", \
								"<span class='userdanger'>[chassis] squeezes you!</span>",\
								"<span class='hear'>You hear something crack.</span>")
			log_combat(chassis.occupant, M, "attacked", "[name]", "(INTENT: [uppertext(chassis.occupant.a_intent)]) (DAMTYPE: [uppertext(damtype)])")
		else
			step_away(M,chassis)
			occupant_message("<span class='notice'>You push [target] out of the way.</span>")
			chassis.visible_message("<span class='notice'>[chassis] pushes [target] out of the way.</span>")
		return 1



//This is pretty much just for the death-ripley
/obj/item/mecha_parts/mecha_equipment/hydraulic_clamp/kill
	name = "\improper KILL CLAMP"
	desc = "They won't know what clamped them!"
	energy_drain = 0
	dam_force = 0
	var/real_clamp = FALSE

/obj/item/mecha_parts/mecha_equipment/hydraulic_clamp/kill/real
	desc = "They won't know what clamped them! This time for real!"
	energy_drain = 10
	dam_force = 20
	real_clamp = TRUE

/obj/item/mecha_parts/mecha_equipment/hydraulic_clamp/kill/action(atom/target)
	if(!action_checks(target))
		return
	if(!cargo_holder)
		return
	if(isobj(target))
		var/obj/O = target
		if(!O.anchored)
			if(cargo_holder.cargo.len < cargo_holder.cargo_capacity)
				chassis.visible_message("<span class='notice'>[chassis] lifts [target] and starts to load it into cargo compartment.</span>")
				O.anchored = TRUE
				if(do_after_cooldown(target))
					cargo_holder.cargo += O
					O.forceMove(chassis)
					O.anchored = FALSE
					occupant_message("<span class='notice'>[target] successfully loaded.</span>")
					log_message("Loaded [O]. Cargo compartment capacity: [cargo_holder.cargo_capacity - cargo_holder.cargo.len]", LOG_MECHA)
				else
					O.anchored = initial(O.anchored)
			else
				occupant_message("<span class='warning'>Not enough room in cargo compartment!</span>")
		else
			occupant_message("<span class='warning'>[target] is firmly secured!</span>")

	else if(isliving(target))
		var/mob/living/M = target
		if(M.stat == DEAD)
			return
		if(chassis.occupant.a_intent == INTENT_HARM)
			if(real_clamp)
				M.take_overall_damage(dam_force)
				if(!M)
					return
				M.adjustOxyLoss(round(dam_force/2))
				M.updatehealth()
				target.visible_message("<span class='danger'>[chassis] destroys [target] in an unholy fury!</span>", \
									"<span class='userdanger'>[chassis] destroys you in an unholy fury!</span>")
				log_combat(chassis.occupant, M, "attacked", "[name]", "(INTENT: [uppertext(chassis.occupant.a_intent)]) (DAMTYPE: [uppertext(damtype)])")
			else
				target.visible_message("<span class='danger'>[chassis] destroys [target] in an unholy fury!</span>", \
									"<span class='userdanger'>[chassis] destroys you in an unholy fury!</span>")
		else if(chassis.occupant.a_intent == INTENT_DISARM)
			if(real_clamp)
				var/mob/living/carbon/C = target
				var/play_sound = FALSE
				var/limbs_gone = ""
				var/obj/item/bodypart/affected = C.get_bodypart(BODY_ZONE_L_ARM)
				if(affected != null)
					affected.dismember(damtype)
					play_sound = TRUE
					limbs_gone = ", [affected]"
				affected = C.get_bodypart(BODY_ZONE_R_ARM)
				if(affected != null)
					affected.dismember(damtype)
					play_sound = TRUE
					limbs_gone = "[limbs_gone], [affected]"
				if(play_sound)
					playsound(src, get_dismember_sound(), 80, TRUE)
					target.visible_message("<span class='danger'>[chassis] rips [target]'s arms off!</span>", \
								   "<span class='userdanger'>[chassis] rips your arms off!</span>")
					log_combat(chassis.occupant, M, "dismembered of[limbs_gone],", "[name]", "(INTENT: [uppertext(chassis.occupant.a_intent)]) (DAMTYPE: [uppertext(damtype)])")
			else
				target.visible_message("<span class='danger'>[chassis] rips [target]'s arms off!</span>", \
								   "<span class='userdanger'>[chassis] rips your arms off!</span>")
		else
			step_away(M,chassis)
			target.visible_message("<span class='danger'>[chassis] tosses [target] like a piece of paper!</span>", \
								"<span class='userdanger'>[chassis] tosses you like a piece of paper!</span>")
		return 1



/obj/item/mecha_parts/mecha_equipment/extinguisher
	name = "exosuit extinguisher"
	desc = "Equipment for engineering exosuits. A rapid-firing high capacity fire extinguisher."
	icon_state = "mecha_exting"
	equip_cooldown = 5
	energy_drain = 0
	range = MECHA_MELEE|MECHA_RANGED

/obj/item/mecha_parts/mecha_equipment/extinguisher/Initialize()
	. = ..()
	create_reagents(1000)
	reagents.add_reagent(/datum/reagent/water, 1000)

/obj/item/mecha_parts/mecha_equipment/extinguisher/action(atom/target) //copypasted from extinguisher. TODO: Rewrite from scratch.
	if(!action_checks(target) || get_dist(chassis, target)>3)
		return

	if(istype(target, /obj/structure/reagent_dispensers/watertank) && get_dist(chassis,target) <= 1)
		var/obj/structure/reagent_dispensers/watertank/WT = target
		WT.reagents.trans_to(src, 1000)
		occupant_message("<span class='notice'>Extinguisher refilled.</span>")
		playsound(chassis, 'sound/effects/refill.ogg', 50, TRUE, -6)
	else
		if(reagents.total_volume > 0)
			playsound(chassis, 'sound/effects/extinguish.ogg', 75, TRUE, -3)
			var/direction = get_dir(chassis,target)
			var/turf/T = get_turf(target)
			var/turf/T1 = get_step(T,turn(direction, 90))
			var/turf/T2 = get_step(T,turn(direction, -90))

			var/list/the_targets = list(T,T1,T2)
			spawn(0)
				for(var/a=0, a<5, a++)
					var/obj/effect/particle_effect/water/W = new /obj/effect/particle_effect/water(get_turf(chassis))
					if(!W)
						return
					var/turf/my_target = pick(the_targets)
					var/datum/reagents/R = new/datum/reagents(5)
					W.reagents = R
					R.my_atom = W
					reagents.trans_to(W,1, transfered_by = chassis.occupant)
					for(var/b=0, b<4, b++)
						if(!W)
							return
						step_towards(W,my_target)
						if(!W)
							return
						var/turf/W_turf = get_turf(W)
						W.reagents.reaction(W_turf)
						for(var/atom/atm in W_turf)
							W.reagents.reaction(atm)
						if(W.loc == my_target)
							break
						sleep(2)
		return 1

/obj/item/mecha_parts/mecha_equipment/extinguisher/get_equip_info()
	return "[..()] \[[src.reagents.total_volume]\]"

/obj/item/mecha_parts/mecha_equipment/extinguisher/can_attach(obj/mecha/working/M as obj)
	if(..())
		if(istype(M))
			return 1
	return 0



/obj/item/mecha_parts/mecha_equipment/rcd
	name = "mounted RCD"
	desc = "An exosuit-mounted Rapid Construction Device."
	icon_state = "mecha_rcd"
	equip_cooldown = 10
	energy_drain = 250
	range = MECHA_MELEE|MECHA_RANGED
	item_flags = NO_MAT_REDEMPTION
	var/mode = 0 //0 - deconstruct, 1 - wall or floor, 2 - airlock.

/obj/item/mecha_parts/mecha_equipment/rcd/Initialize()
	. = ..()
	GLOB.rcd_list += src

/obj/item/mecha_parts/mecha_equipment/rcd/Destroy()
	GLOB.rcd_list -= src
	return ..()

/obj/item/mecha_parts/mecha_equipment/rcd/action(atom/target)
	if(istype(target, /turf/open/space/transit))//>implying these are ever made -Sieve
		return

	if(!isturf(target) && !istype(target, /obj/machinery/door/airlock))
		target = get_turf(target)
	if(!action_checks(target) || get_dist(chassis, target)>3)
		return
	playsound(chassis, 'sound/machines/click.ogg', 50, TRUE)

	switch(mode)
		if(0)
			if(iswallturf(target))
				var/turf/closed/wall/W = target
				occupant_message("<span class='notice'>Deconstructing [W]...</span>")
				if(do_after_cooldown(W))
					chassis.spark_system.start()
					W.ScrapeAway(flags = CHANGETURF_INHERIT_AIR)
					playsound(W, 'sound/items/deconstruct.ogg', 50, TRUE)
			else if(isfloorturf(target))
				var/turf/open/floor/F = target
				occupant_message("<span class='notice'>Deconstructing [F]...</span>")
				if(do_after_cooldown(target))
					chassis.spark_system.start()
					F.ScrapeAway(flags = CHANGETURF_INHERIT_AIR)
					playsound(F, 'sound/items/deconstruct.ogg', 50, TRUE)
			else if (istype(target, /obj/machinery/door/airlock))
				occupant_message("<span class='notice'>Deconstructing [target]...</span>")
				if(do_after_cooldown(target))
					chassis.spark_system.start()
					qdel(target)
					playsound(target, 'sound/items/deconstruct.ogg', 50, TRUE)
		if(1)
			if(isspaceturf(target))
				var/turf/open/space/S = target
				occupant_message("<span class='notice'>Building Floor...</span>")
				if(do_after_cooldown(S))
					S.PlaceOnTop(/turf/open/floor/plating, flags = CHANGETURF_INHERIT_AIR)
					playsound(S, 'sound/items/deconstruct.ogg', 50, TRUE)
					chassis.spark_system.start()
			else if(isfloorturf(target))
				var/turf/open/floor/F = target
				occupant_message("<span class='notice'>Building Wall...</span>")
				if(do_after_cooldown(F))
					F.PlaceOnTop(/turf/closed/wall)
					playsound(F, 'sound/items/deconstruct.ogg', 50, TRUE)
					chassis.spark_system.start()
		if(2)
			if(isfloorturf(target))
				occupant_message("<span class='notice'>Building Airlock...</span>")
				if(do_after_cooldown(target))
					chassis.spark_system.start()
					var/obj/machinery/door/airlock/T = new /obj/machinery/door/airlock(target)
					T.autoclose = TRUE
					playsound(target, 'sound/items/deconstruct.ogg', 50, TRUE)
					playsound(target, 'sound/effects/sparks2.ogg', 50, TRUE)



/obj/item/mecha_parts/mecha_equipment/rcd/do_after_cooldown(var/atom/target)
	. = ..()

/obj/item/mecha_parts/mecha_equipment/rcd/Topic(href,href_list)
	..()
	if(href_list["mode"])
		mode = text2num(href_list["mode"])
		switch(mode)
			if(0)
				occupant_message("<span class='notice'>Switched RCD to Deconstruct.</span>")
				energy_drain = initial(energy_drain)
			if(1)
				occupant_message("<span class='notice'>Switched RCD to Construct.</span>")
				energy_drain = 2*initial(energy_drain)
			if(2)
				occupant_message("<span class='notice'>Switched RCD to Construct Airlock.</span>")
				energy_drain = 2*initial(energy_drain)
	return

/obj/item/mecha_parts/mecha_equipment/rcd/get_equip_info()
	return "[..()] \[<a href='?src=[REF(src)];mode=0'>D</a>|<a href='?src=[REF(src)];mode=1'>C</a>|<a href='?src=[REF(src)];mode=2'>A</a>\]"

//Dunno where else to put this so shrug
/obj/item/mecha_parts/mecha_equipment/ripleyupgrade
	name = "Ripley MK-II Conversion Kit"
	desc = "A pressurized canopy attachment kit for an Autonomous Power Loader Unit \"Ripley\" MK-I mecha, to convert it to the slower, but space-worthy MK-II design. This kit cannot be removed, once applied."
	icon_state = "ripleyupgrade"

/obj/item/mecha_parts/mecha_equipment/ripleyupgrade/can_attach(obj/mecha/working/ripley/M)
	if(M.type != /obj/mecha/working/ripley)
		to_chat(loc, "<span class='warning'>This conversion kit can only be applied to APLU MK-I models.</span>")
		return FALSE
	if(M.cargo.len)
		to_chat(loc, "<span class='warning'>[M]'s cargo hold must be empty before this conversion kit can be applied.</span>")
		return FALSE
	if(!M.maint_access) //non-removable upgrade, so lets make sure the pilot or owner has their say.
		to_chat(loc, "<span class='warning'>[M] must have maintenance protocols active in order to allow this conversion kit.</span>")
		return FALSE
	if(M.occupant) //We're actualy making a new mech and swapping things over, it might get weird if players are involved
		to_chat(loc, "<span class='warning'>[M] must be unoccupied before this conversion kit can be applied.</span>")
		return FALSE
	if(!M.cell) //Turns out things break if the cell is missing
		to_chat(loc, "<span class='warning'>The conversion process requires a cell installed.</span>")
		return FALSE
	return TRUE

/obj/item/mecha_parts/mecha_equipment/ripleyupgrade/attach(obj/mecha/M)
	var/obj/mecha/working/ripley/mkii/N = new /obj/mecha/working/ripley/mkii(get_turf(M),1)
	if(!N)
		return
	QDEL_NULL(N.cell)
	if (M.cell)
		N.cell = M.cell
		M.cell.forceMove(N)
		M.cell = null
	QDEL_NULL(N.scanmod)
	if (M.scanmod)
		N.scanmod = M.scanmod
		M.scanmod.forceMove(N)
		M.scanmod = null
	QDEL_NULL(N.capacitor)
	if (M.capacitor)
		N.capacitor = M.capacitor
		M.capacitor.forceMove(N)
		M.capacitor = null
	N.update_part_values()
	for(var/obj/item/mecha_parts/E in M.contents)
		if(istype(E, /obj/item/mecha_parts/concealed_weapon_bay)) //why is the bay not just a variable change who did this
			E.forceMove(N)
	for(var/obj/item/mecha_parts/mecha_equipment/E in M.equipment) //Move the equipment over...
		E.detach()
		E.attach(N)
		M.equipment -= E
	N.dna_lock = M.dna_lock
	N.maint_access = M.maint_access
	N.strafe = M.strafe
	N.obj_integrity = M.obj_integrity //This is not a repair tool
	if (M.name != "\improper APLU MK-I \"Ripley\"")
		N.name = M.name
	M.wreckage = 0
	qdel(M)
	playsound(get_turf(N),'sound/items/ratchet.ogg',50,TRUE)
	return
