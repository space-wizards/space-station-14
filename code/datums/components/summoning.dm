/datum/component/summoning
	var/list/mob_types = list()
	var/spawn_chance // chance for the mob to spawn on hit in percent
	var/max_mobs
	var/spawn_delay // delay in spawning between mobs (deciseconds)
	var/spawn_text
	var/spawn_sound
	var/list/faction

	var/last_spawned_time = 0
	var/list/spawned_mobs = list()

/datum/component/summoning/Initialize(mob_types, spawn_chance=100, max_mobs=3, spawn_delay=100, spawn_text="appears out of nowhere", spawn_sound='sound/magic/summon_magic.ogg', faction)
	if(!isitem(parent) && !ishostile(parent) && !isgun(parent) && !ismachinery(parent) && !isstructure(parent))
		return COMPONENT_INCOMPATIBLE

	src.mob_types = mob_types
	src.spawn_chance = spawn_chance
	src.max_mobs = max_mobs
	src.spawn_delay = spawn_delay
	src.spawn_text = spawn_text
	src.spawn_sound = spawn_sound
	src.faction = faction

/datum/component/summoning/RegisterWithParent()
	if(ismachinery(parent) || isstructure(parent) || isgun(parent)) // turrets, etc
		RegisterSignal(parent, COMSIG_PROJECTILE_ON_HIT, .proc/projectile_hit)
	else if(isitem(parent))
		RegisterSignal(parent, COMSIG_ITEM_AFTERATTACK, .proc/item_afterattack)
	else if(ishostile(parent))
		RegisterSignal(parent, COMSIG_HOSTILE_ATTACKINGTARGET, .proc/hostile_attackingtarget)

/datum/component/summoning/UnregisterFromParent()
	UnregisterSignal(parent, list(COMSIG_ITEM_AFTERATTACK, COMSIG_HOSTILE_ATTACKINGTARGET, COMSIG_PROJECTILE_ON_HIT))

/datum/component/summoning/proc/item_afterattack(obj/item/source, atom/target, mob/user, proximity_flag, click_parameters)
	if(!proximity_flag)
		return
	do_spawn_mob(get_turf(target), user)

/datum/component/summoning/proc/hostile_attackingtarget(mob/living/simple_animal/hostile/attacker, atom/target)
	do_spawn_mob(get_turf(target), attacker)

/datum/component/summoning/proc/projectile_hit(atom/fired_from, atom/movable/firer, atom/target, Angle)
	do_spawn_mob(get_turf(target), firer)

/datum/component/summoning/proc/do_spawn_mob(atom/spawn_location, summoner)
	if(spawned_mobs.len >= max_mobs)
		return 0
	if(last_spawned_time > world.time)
		return 0
	if(!prob(spawn_chance))
		return 0
	last_spawned_time = world.time + spawn_delay
	var/chosen_mob_type = pick(mob_types)
	var/mob/living/simple_animal/L = new chosen_mob_type(spawn_location)
	if(ishostile(L))
		var/mob/living/simple_animal/hostile/H = L
		H.friends += summoner // do not attack our summon boy
	spawned_mobs += L
	if(faction != null)
		L.faction = faction
	RegisterSignal(L, COMSIG_MOB_DEATH, .proc/on_spawned_death) // so we can remove them from the list, etc (for mobs with corpses)
	playsound(spawn_location,spawn_sound, 50, TRUE)
	spawn_location.visible_message("<span class='danger'>[L] [spawn_text].</span>")

/datum/component/summoning/proc/on_spawned_death(mob/killed, gibbed)
	spawned_mobs -= killed
