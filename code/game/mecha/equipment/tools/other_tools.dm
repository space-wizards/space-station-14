
// Teleporter, Wormhole generator, Gravitational catapult, Armor booster modules,
// Repair droid, Tesla Energy relay, Generators

////////////////////////////////////////////// TELEPORTER ///////////////////////////////////////////////

/obj/item/mecha_parts/mecha_equipment/teleporter
	name = "mounted teleporter"
	desc = "An exosuit module that allows exosuits to teleport to any position in view."
	icon_state = "mecha_teleport"
	equip_cooldown = 150
	energy_drain = 1000
	range = MECHA_RANGED

/obj/item/mecha_parts/mecha_equipment/teleporter/action(atom/target)
	if(!action_checks(target) || is_centcom_level(loc.z))
		return
	var/turf/T = get_turf(target)
	if(T)
		do_teleport(chassis, T, 4, channel = TELEPORT_CHANNEL_BLUESPACE)
		return 1



////////////////////////////////////////////// WORMHOLE GENERATOR //////////////////////////////////////////

/obj/item/mecha_parts/mecha_equipment/wormhole_generator
	name = "mounted wormhole generator"
	desc = "An exosuit module that allows generating of small quasi-stable wormholes."
	icon_state = "mecha_wholegen"
	equip_cooldown = 50
	energy_drain = 300
	range = MECHA_RANGED


/obj/item/mecha_parts/mecha_equipment/wormhole_generator/action(atom/target)
	if(!action_checks(target) || is_centcom_level(loc.z))
		return
	var/list/theareas = get_areas_in_range(100, chassis)
	if(!theareas.len)
		return
	var/area/thearea = pick(theareas)
	var/list/L = list()
	var/turf/pos = get_turf(src)
	for(var/turf/T in get_area_turfs(thearea.type))
		if(!T.density && pos.z == T.z)
			var/clear = 1
			for(var/obj/O in T)
				if(O.density)
					clear = 0
					break
			if(clear)
				L+=T
	if(!L.len)
		return
	var/turf/target_turf = pick(L)
	if(!target_turf)
		return
	var/list/obj/effect/portal/created = create_portal_pair(get_turf(src), target_turf, 300, 1, /obj/effect/portal/anom)
	var/turf/T = get_turf(target)
	message_admins("[ADMIN_LOOKUPFLW(chassis.occupant)] used a Wormhole Generator in [ADMIN_VERBOSEJMP(T)]")
	log_game("[key_name(chassis.occupant)] used a Wormhole Generator in [AREACOORD(T)]")
	src = null
	QDEL_LIST_IN(created, rand(150,300))
	return 1


/////////////////////////////////////// GRAVITATIONAL CATAPULT ///////////////////////////////////////////

/obj/item/mecha_parts/mecha_equipment/gravcatapult
	name = "mounted gravitational catapult"
	desc = "An exosuit mounted Gravitational Catapult."
	icon_state = "mecha_teleport"
	equip_cooldown = 10
	energy_drain = 100
	range = MECHA_MELEE|MECHA_RANGED
	var/atom/movable/locked
	var/mode = 1 //1 - gravsling 2 - gravpush


/obj/item/mecha_parts/mecha_equipment/gravcatapult/action(atom/movable/target)
	if(!action_checks(target))
		return
	switch(mode)
		if(1)
			if(!locked)
				if(!istype(target) || target.anchored || target.move_resist >= MOVE_FORCE_EXTREMELY_STRONG)
					occupant_message("<span class='warning'>Unable to lock on [target]!</span>")
					return
				if(ismob(target))
					var/mob/M = target
					if(M.mob_negates_gravity())
						occupant_message("<span class='warning'>Unable to lock on [target]!</span>")
						return
				locked = target
				occupant_message("<span class='notice'>Locked on [target].</span>")
				send_byjax(chassis.occupant,"exosuit.browser","[REF(src)]",src.get_equip_info())
			else if(target!=locked)
				if(locked in view(chassis))
					var/turf/targ = get_turf(target)
					var/turf/orig = get_turf(locked)
					locked.throw_at(target, 14, 1.5)
					locked = null
					send_byjax(chassis.occupant,"exosuit.browser","[REF(src)]",src.get_equip_info())
					log_game("[key_name(chassis.occupant)] used a Gravitational Catapult to throw [locked] (From [AREACOORD(orig)]) at [target] ([AREACOORD(targ)]).")
					return TRUE
				else
					locked = null
					occupant_message("<span class='notice'>Lock on [locked] disengaged.</span>")
					send_byjax(chassis.occupant,"exosuit.browser","[REF(src)]",src.get_equip_info())
		if(2)
			var/list/atoms = list()
			if(isturf(target))
				atoms = range(3, target)
			else
				atoms = orange(3, target)
			for(var/atom/movable/A in atoms)
				if(A.anchored || A.move_resist >= MOVE_FORCE_EXTREMELY_STRONG)
					continue
				if(ismob(A))
					var/mob/M = A
					if(M.mob_negates_gravity())
						continue
				INVOKE_ASYNC(src, .proc/do_scatter, A, target)

			var/turf/T = get_turf(target)
			log_game("[key_name(chassis.occupant)] used a Gravitational Catapult repulse wave on [AREACOORD(T)]")
			return TRUE

/obj/item/mecha_parts/mecha_equipment/gravcatapult/proc/do_scatter(atom/movable/A, atom/movable/target)
	var/iter = 5-get_dist(A,target)
	for(var/i in 0 to iter)
		step_away(A,target)
		sleep(2)

/obj/item/mecha_parts/mecha_equipment/gravcatapult/get_equip_info()
	return "[..()] [mode==1?"([locked||"Nothing"])":null] \[<a href='?src=[REF(src)];mode=1'>S</a>|<a href='?src=[REF(src)];mode=2'>P</a>\]"

/obj/item/mecha_parts/mecha_equipment/gravcatapult/Topic(href, href_list)
	..()
	if(href_list["mode"])
		mode = text2num(href_list["mode"])
		send_byjax(chassis.occupant,"exosuit.browser","[REF(src)]",src.get_equip_info())
	return




//////////////////////////// ARMOR BOOSTER MODULES //////////////////////////////////////////////////////////


/obj/item/mecha_parts/mecha_equipment/anticcw_armor_booster //what is that noise? A BAWWW from TK mutants.
	name = "armor booster module (Close Combat Weaponry)"
	desc = "Boosts exosuit armor against armed melee attacks. Requires energy to operate."
	icon_state = "mecha_abooster_ccw"
	equip_cooldown = 10
	energy_drain = 50
	range = 0
	var/deflect_coeff = 1.15
	var/damage_coeff = 0.8
	selectable = 0

/obj/item/mecha_parts/mecha_equipment/anticcw_armor_booster/proc/attack_react()
	if(action_checks(src))
		start_cooldown()
		return 1



/obj/item/mecha_parts/mecha_equipment/antiproj_armor_booster
	name = "armor booster module (Ranged Weaponry)"
	desc = "Boosts exosuit armor against ranged attacks. Completely blocks taser shots. Requires energy to operate."
	icon_state = "mecha_abooster_proj"
	equip_cooldown = 10
	energy_drain = 50
	range = 0
	var/deflect_coeff = 1.15
	var/damage_coeff = 0.8
	selectable = 0

/obj/item/mecha_parts/mecha_equipment/antiproj_armor_booster/proc/projectile_react()
	if(action_checks(src))
		start_cooldown()
		return 1


////////////////////////////////// REPAIR DROID //////////////////////////////////////////////////


/obj/item/mecha_parts/mecha_equipment/repair_droid
	name = "exosuit repair droid"
	desc = "An automated repair droid for exosuits. Scans for damage and repairs it. Can fix almost all types of external or internal damage."
	icon_state = "repair_droid"
	energy_drain = 50
	range = 0
	var/health_boost = 1
	var/icon/droid_overlay
	var/list/repairable_damage = list(MECHA_INT_TEMP_CONTROL,MECHA_INT_TANK_BREACH)
	selectable = 0

/obj/item/mecha_parts/mecha_equipment/repair_droid/Destroy()
	STOP_PROCESSING(SSobj, src)
	if(chassis)
		chassis.cut_overlay(droid_overlay)
	return ..()

/obj/item/mecha_parts/mecha_equipment/repair_droid/attach(obj/mecha/M as obj)
	..()
	droid_overlay = new(src.icon, icon_state = "repair_droid")
	M.add_overlay(droid_overlay)

/obj/item/mecha_parts/mecha_equipment/repair_droid/detach()
	chassis.cut_overlay(droid_overlay)
	STOP_PROCESSING(SSobj, src)
	..()

/obj/item/mecha_parts/mecha_equipment/repair_droid/get_equip_info()
	if(!chassis)
		return
	return "<span style=\"color:[equip_ready?"#0f0":"#f00"];\">*</span>&nbsp; [src.name] - <a href='?src=[REF(src)];toggle_repairs=1'>[equip_ready?"A":"Dea"]ctivate</a>"


/obj/item/mecha_parts/mecha_equipment/repair_droid/Topic(href, href_list)
	..()
	if(href_list["toggle_repairs"])
		chassis.cut_overlay(droid_overlay)
		if(equip_ready)
			START_PROCESSING(SSobj, src)
			droid_overlay = new(src.icon, icon_state = "repair_droid_a")
			log_message("Activated.", LOG_MECHA)
			set_ready_state(0)
		else
			STOP_PROCESSING(SSobj, src)
			droid_overlay = new(src.icon, icon_state = "repair_droid")
			log_message("Deactivated.", LOG_MECHA)
			set_ready_state(1)
		chassis.add_overlay(droid_overlay)
		send_byjax(chassis.occupant,"exosuit.browser","[REF(src)]",src.get_equip_info())


/obj/item/mecha_parts/mecha_equipment/repair_droid/process()
	if(!chassis)
		STOP_PROCESSING(SSobj, src)
		set_ready_state(1)
		return
	var/h_boost = health_boost
	var/repaired = 0
	if(chassis.internal_damage & MECHA_INT_SHORT_CIRCUIT)
		h_boost *= -2
	else if(chassis.internal_damage && prob(15))
		for(var/int_dam_flag in repairable_damage)
			if(chassis.internal_damage & int_dam_flag)
				chassis.clearInternalDamage(int_dam_flag)
				repaired = 1
				break
	if(h_boost<0 || chassis.obj_integrity < chassis.max_integrity)
		chassis.obj_integrity += min(h_boost, chassis.max_integrity-chassis.obj_integrity)
		repaired = 1
	if(repaired)
		if(!chassis.use_power(energy_drain))
			STOP_PROCESSING(SSobj, src)
			set_ready_state(1)
	else //no repair needed, we turn off
		STOP_PROCESSING(SSobj, src)
		set_ready_state(1)
		chassis.cut_overlay(droid_overlay)
		droid_overlay = new(src.icon, icon_state = "repair_droid")
		chassis.add_overlay(droid_overlay)




/////////////////////////////////// TESLA ENERGY RELAY ////////////////////////////////////////////////

/obj/item/mecha_parts/mecha_equipment/tesla_energy_relay
	name = "exosuit energy relay"
	desc = "An exosuit module that wirelessly drains energy from any available power channel in area. The performance index is quite low."
	icon_state = "tesla"
	energy_drain = 0
	range = 0
	var/coeff = 100
	var/list/use_channels = list(EQUIP,ENVIRON,LIGHT)
	selectable = 0

/obj/item/mecha_parts/mecha_equipment/tesla_energy_relay/Destroy()
	STOP_PROCESSING(SSobj, src)
	return ..()

/obj/item/mecha_parts/mecha_equipment/tesla_energy_relay/detach()
	STOP_PROCESSING(SSobj, src)
	..()
	return

/obj/item/mecha_parts/mecha_equipment/tesla_energy_relay/proc/get_charge()
	if(equip_ready) //disabled
		return
	var/area/A = get_area(chassis)
	var/pow_chan = GET_MUTATION_POWER_channel(A)
	if(pow_chan)
		return 1000 //making magic


/obj/item/mecha_parts/mecha_equipment/tesla_energy_relay/proc/GET_MUTATION_POWER_channel(var/area/A)
	var/pow_chan
	if(A)
		for(var/c in use_channels)
			if(A.powered(c))
				pow_chan = c
				break
	return pow_chan

/obj/item/mecha_parts/mecha_equipment/tesla_energy_relay/Topic(href, href_list)
	..()
	if(href_list["toggle_relay"])
		if(equip_ready) //inactive
			START_PROCESSING(SSobj, src)
			set_ready_state(0)
			log_message("Activated.", LOG_MECHA)
		else
			STOP_PROCESSING(SSobj, src)
			set_ready_state(1)
			log_message("Deactivated.", LOG_MECHA)

/obj/item/mecha_parts/mecha_equipment/tesla_energy_relay/get_equip_info()
	if(!chassis)
		return
	return "<span style=\"color:[equip_ready?"#0f0":"#f00"];\">*</span>&nbsp; [src.name] - <a href='?src=[REF(src)];toggle_relay=1'>[equip_ready?"A":"Dea"]ctivate</a>"


/obj/item/mecha_parts/mecha_equipment/tesla_energy_relay/process()
	if(!chassis || chassis.internal_damage & MECHA_INT_SHORT_CIRCUIT)
		STOP_PROCESSING(SSobj, src)
		set_ready_state(1)
		return
	var/cur_charge = chassis.get_charge()
	if(isnull(cur_charge) || !chassis.cell)
		STOP_PROCESSING(SSobj, src)
		set_ready_state(1)
		occupant_message("<span class='notice'>No powercell detected.</span>")
		return
	if(cur_charge < chassis.cell.maxcharge)
		var/area/A = get_area(chassis)
		if(A)
			var/pow_chan
			for(var/c in list(EQUIP,ENVIRON,LIGHT))
				if(A.powered(c))
					pow_chan = c
					break
			if(pow_chan)
				var/delta = min(20, chassis.cell.maxcharge-cur_charge)
				chassis.give_power(delta)
				A.use_power(delta*coeff, pow_chan)




/////////////////////////////////////////// GENERATOR /////////////////////////////////////////////


/obj/item/mecha_parts/mecha_equipment/generator
	name = "exosuit plasma converter"
	desc = "An exosuit module that generates power using solid plasma as fuel. Pollutes the environment."
	icon_state = "tesla"
	range = MECHA_MELEE
	var/coeff = 100
	var/obj/item/stack/sheet/fuel
	var/max_fuel = 150000
	var/fuel_per_cycle_idle = 25
	var/fuel_per_cycle_active = 200
	var/power_per_cycle = 20

/obj/item/mecha_parts/mecha_equipment/generator/Initialize()
	. = ..()
	generator_init()

/obj/item/mecha_parts/mecha_equipment/generator/Destroy()
	STOP_PROCESSING(SSobj, src)
	return ..()

/obj/item/mecha_parts/mecha_equipment/generator/proc/generator_init()
	fuel = new /obj/item/stack/sheet/mineral/plasma(src, 0)

/obj/item/mecha_parts/mecha_equipment/generator/detach()
	STOP_PROCESSING(SSobj, src)
	..()

/obj/item/mecha_parts/mecha_equipment/generator/Topic(href, href_list)
	..()
	if(href_list["toggle"])
		if(equip_ready) //inactive
			set_ready_state(0)
			START_PROCESSING(SSobj, src)
			log_message("Activated.", LOG_MECHA)
		else
			set_ready_state(1)
			STOP_PROCESSING(SSobj, src)
			log_message("Deactivated.", LOG_MECHA)

/obj/item/mecha_parts/mecha_equipment/generator/get_equip_info()
	var/output = ..()
	if(output)
		return "[output] \[[fuel]: [round(fuel.amount*MINERAL_MATERIAL_AMOUNT,0.1)] cm<sup>3</sup>\] - <a href='?src=[REF(src)];toggle=1'>[equip_ready?"A":"Dea"]ctivate</a>"

/obj/item/mecha_parts/mecha_equipment/generator/action(target)
	if(chassis)
		var/result = load_fuel(target)
		if(result)
			send_byjax(chassis.occupant,"exosuit.browser","[REF(src)]",src.get_equip_info())

/obj/item/mecha_parts/mecha_equipment/generator/proc/load_fuel(var/obj/item/stack/sheet/P)
	if(P.type == fuel.type && P.amount > 0)
		var/to_load = max(max_fuel - fuel.amount*MINERAL_MATERIAL_AMOUNT,0)
		if(to_load)
			var/units = min(max(round(to_load / MINERAL_MATERIAL_AMOUNT),1),P.amount)
			fuel.amount += units
			P.use(units)
			occupant_message("<span class='notice'>[units] unit\s of [fuel] successfully loaded.</span>")
			return units
		else
			occupant_message("<span class='notice'>Unit is full.</span>")
			return 0
	else
		occupant_message("<span class='warning'>[fuel] traces in target minimal! [P] cannot be used as fuel.</span>")
		return

/obj/item/mecha_parts/mecha_equipment/generator/attackby(weapon,mob/user, params)
	load_fuel(weapon)

/obj/item/mecha_parts/mecha_equipment/generator/process()
	if(!chassis)
		STOP_PROCESSING(SSobj, src)
		set_ready_state(1)
		return
	if(fuel.amount<=0)
		STOP_PROCESSING(SSobj, src)
		log_message("Deactivated - no fuel.", LOG_MECHA)
		set_ready_state(1)
		return
	var/cur_charge = chassis.get_charge()
	if(isnull(cur_charge))
		set_ready_state(1)
		occupant_message("<span class='notice'>No powercell detected.</span>")
		log_message("Deactivated.", LOG_MECHA)
		STOP_PROCESSING(SSobj, src)
		return
	var/use_fuel = fuel_per_cycle_idle
	if(cur_charge < chassis.cell.maxcharge)
		use_fuel = fuel_per_cycle_active
		chassis.give_power(power_per_cycle)
	fuel.amount -= min(use_fuel/MINERAL_MATERIAL_AMOUNT,fuel.amount)
	update_equip_info()
	return 1


/obj/item/mecha_parts/mecha_equipment/generator/nuclear
	name = "exonuclear reactor"
	desc = "An exosuit module that generates power using uranium as fuel. Pollutes the environment."
	icon_state = "tesla"
	max_fuel = 50000
	fuel_per_cycle_idle = 10
	fuel_per_cycle_active = 30
	power_per_cycle = 50
	var/rad_per_cycle = 30

/obj/item/mecha_parts/mecha_equipment/generator/nuclear/generator_init()
	fuel = new /obj/item/stack/sheet/mineral/uranium(src, 0)

/obj/item/mecha_parts/mecha_equipment/generator/nuclear/process()
	if(..())
		radiation_pulse(get_turf(src), rad_per_cycle)


/////////////////////////////////////////// THRUSTERS /////////////////////////////////////////////

/obj/item/mecha_parts/mecha_equipment/thrusters
	name = "generic exosuit thrusters" //parent object, in-game sources will be a child object
	desc = "A generic set of thrusters, from an unknown source. Uses not-understood methods to propel exosuits seemingly for free."
	icon_state = "thrusters"
	selectable = FALSE
	var/effect_type = /obj/effect/particle_effect/sparks

/obj/item/mecha_parts/mecha_equipment/thrusters/try_attach_part(mob/user, obj/mecha/M)
	for(var/obj/item/I in M.equipment)
		if(istype(I, src))
			to_chat(user, "<span class='warning'>[M] already has this thruster package!</span>")
			return FALSE
	. = ..()

/obj/item/mecha_parts/mecha_equipment/thrusters/attach(obj/mecha/M)
	M.active_thrusters = src //Enable by default
	. = ..()

/obj/item/mecha_parts/mecha_equipment/thrusters/detach()
	if(chassis?.active_thrusters == src)
		chassis.active_thrusters = null
	. = ..()

/obj/item/mecha_parts/mecha_equipment/thrusters/Destroy()
	if(chassis?.active_thrusters == src)
		chassis.active_thrusters = null
	. = ..()

/obj/item/mecha_parts/mecha_equipment/thrusters/Topic(href,href_list)
	..()
	if(!chassis)
		return
	if(href_list["mode"])
		var/mode = text2num(href_list["mode"])
		switch(mode)
			if(0)
				enable()
			if(1)
				disable()
	return

/obj/item/mecha_parts/mecha_equipment/thrusters/proc/enable()
	if (chassis.active_thrusters == src)
		return
	chassis.active_thrusters = src
	occupant_message("<span class='notice'>[src] enabled.</span>")

/obj/item/mecha_parts/mecha_equipment/thrusters/proc/disable()
	if(chassis.active_thrusters != src)
		return
	chassis.active_thrusters = null
	occupant_message("<span class='notice'>[src] disabled.</span>")

/obj/item/mecha_parts/mecha_equipment/thrusters/get_equip_info()
	return "[..()] \[<a href='?src=[REF(src)];mode=0'>Enable</a>|<a href='?src=[REF(src)];mode=1'>Disable</a>\]"

/obj/item/mecha_parts/mecha_equipment/thrusters/proc/thrust(var/movement_dir)
	if(!chassis)
		return FALSE
	generate_effect(movement_dir)
	return TRUE //This parent should never exist in-game outside admeme use, so why not let it be a creative thruster?

/obj/item/mecha_parts/mecha_equipment/thrusters/proc/generate_effect(var/movement_dir)
	var/obj/effect/particle_effect/E = new effect_type(get_turf(chassis))
	E.dir = turn(movement_dir, 180)
	step(E, turn(movement_dir, 180))
	QDEL_IN(E, 5)


/obj/item/mecha_parts/mecha_equipment/thrusters/gas
	name = "RCS thruster package"
	desc = "A set of thrusters that allow for exosuit movement in zero-gravity enviroments, by expelling gas from the internal life support tank."
	effect_type = /obj/effect/particle_effect/smoke
	var/move_cost = 20 //moles per step

/obj/item/mecha_parts/mecha_equipment/thrusters/gas/try_attach_part(mob/user, obj/mecha/M)
	if(!M.internal_tank)
		to_chat(user, "<span class='warning'>[M] does not have an internal tank and cannot support this upgrade!</span>")
		return FALSE
	. = ..()

/obj/item/mecha_parts/mecha_equipment/thrusters/gas/thrust(var/movement_dir)
	if(!chassis || !chassis.internal_tank)
		return FALSE
	var/moles = chassis.internal_tank.air_contents.total_moles()
	if(moles < move_cost)
		chassis.internal_tank.air_contents.remove(moles)
		return FALSE
	chassis.internal_tank.air_contents.remove(move_cost)
	generate_effect(movement_dir)
	return TRUE



/obj/item/mecha_parts/mecha_equipment/thrusters/ion //for mechs with built-in thrusters, should never really exist un-attached to a mech
	name = "Ion thruster package"
	desc = "A set of thrusters that allow for exosuit movement in zero-gravity enviroments."
	detachable = FALSE
	salvageable = FALSE
	effect_type = /obj/effect/particle_effect/ion_trails

/obj/item/mecha_parts/mecha_equipment/thrusters/ion/thrust(var/movement_dir)
	if(!chassis)
		return FALSE
	if(chassis.use_power(chassis.step_energy_drain))
		generate_effect(movement_dir)
		return TRUE
	return FALSE
