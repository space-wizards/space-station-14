////////////////////
//Clusterbang
////////////////////
/obj/item/grenade/clusterbuster
	desc = "Use of this weapon may constitute a war crime in your area, consult your local captain."
	name = "clusterbang"
	icon = 'icons/obj/grenade.dmi'
	icon_state = "clusterbang"
	var/base_state = "clusterbang"
	var/payload = /obj/item/grenade/flashbang/cluster
	var/payload_spawner = /obj/effect/payload_spawner
	var/prime_sound = 'sound/weapons/armbomb.ogg'
	var/min_spawned = 4
	var/max_spawned = 8
	var/segment_chance = 35

/obj/item/grenade/clusterbuster/prime()
	update_mob()
	var/numspawned = rand(min_spawned,max_spawned)
	var/again = 0

	for(var/more = numspawned,more > 0,more--)
		if(prob(segment_chance))
			again++
			numspawned--

	for(var/loop in 1 to again)
		new /obj/item/grenade/clusterbuster/segment(drop_location(), src)//Creates 'segments' that launches a few more payloads

	new payload_spawner(drop_location(), payload, numspawned)//Launches payload
	playsound(src, prime_sound, 75, TRUE, -3)
	qdel(src)

//////////////////////
//Clusterbang segment
//////////////////////
/obj/item/grenade/clusterbuster/segment
	desc = "A smaller segment of a clusterbang. Better run."
	name = "clusterbang segment"
	icon = 'icons/obj/grenade.dmi'
	icon_state = "clusterbang_segment"
	base_state = "clusterbang_segment"

/obj/item/grenade/clusterbuster/segment/Initialize(mapload, obj/item/grenade/clusterbuster/base)
	. = ..()
	if(base)
		name = "[base.name] segment"
		base_state = "[base.base_state]_segment"
		icon_state = base_state
		payload_spawner = base.payload_spawner
		payload = base.payload
		prime_sound = base.prime_sound
		min_spawned = base.min_spawned
		max_spawned = base.max_spawned
	icon_state = "[base_state]_active"
	active = TRUE
	var/steps = rand(1,4)
	for(var/i in 1 to steps)
		step_away(src,loc)
	addtimer(CALLBACK(src, .proc/prime), rand(15,60))

/obj/item/grenade/clusterbuster/segment/prime()
	new payload_spawner(drop_location(), payload, rand(min_spawned,max_spawned))
	playsound(src, prime_sound, 75, TRUE, -3)
	qdel(src)

//////////////////////////////////
//The payload spawner effect
/////////////////////////////////
/obj/effect/payload_spawner/Initialize(mapload, type, numspawned)
	..()
	spawn_payload(type, numspawned)
	return INITIALIZE_HINT_QDEL

/obj/effect/payload_spawner/proc/spawn_payload(type, numspawned)
	for(var/loop in 1 to numspawned)
		var/obj/item/grenade/P = new type(loc)
		if(istype(P))
			P.active = TRUE
			addtimer(CALLBACK(P, /obj/item/grenade/proc/prime), rand(15,60))
		var/steps = rand(1,4)
		for(var/i in 1 to steps)
			step_away(src,loc)

/obj/effect/payload_spawner/random_slime
	var/volatile = FALSE

/obj/effect/payload_spawner/random_slime/volatile
	volatile = TRUE

/obj/item/slime_extract/proc/activate_slime()
	var/list/slime_chems = src.activate_reagents
	if(!QDELETED(src))
		var/chem = pick(slime_chems)
		var/amount = 5
		if(chem == "lesser plasma") //In the rare case we get another rainbow.
			chem = /datum/reagent/toxin/plasma
			amount = 4
		if(chem == "holy water and uranium")
			chem = /datum/reagent/uranium
			reagents.add_reagent(/datum/reagent/water/holywater)
		reagents.add_reagent(chem,amount)

/obj/effect/payload_spawner/random_slime/spawn_payload(type, numspawned)
	for(var/loop = numspawned ,loop > 0, loop--)
		var/chosen = pick(subtypesof(/obj/item/slime_extract))
		var/obj/item/slime_extract/P = new chosen(loc)
		if(volatile)
			addtimer(CALLBACK(P, /obj/item/slime_extract/proc/activate_slime), rand(15,60))
		var/steps = rand(1,4)
		for(var/i in 1 to steps)
			step_away(src,loc)

//////////////////////////////////
//Custom payload clusterbusters
/////////////////////////////////
/obj/item/grenade/flashbang/cluster
	icon_state = "flashbang_active"

/obj/item/grenade/clusterbuster/emp
	name = "Electromagnetic Storm"
	payload = /obj/item/grenade/empgrenade

/obj/item/grenade/clusterbuster/smoke
	name = "Ninja Vanish"
	payload = /obj/item/grenade/smokebomb

/obj/item/grenade/clusterbuster/metalfoam
	name = "Instant Concrete"
	payload = /obj/item/grenade/chem_grenade/metalfoam

/obj/item/grenade/clusterbuster/inferno
	name = "Inferno"
	payload = /obj/item/grenade/chem_grenade/incendiary

/obj/item/grenade/clusterbuster/antiweed
	name = "RoundDown"
	payload = /obj/item/grenade/chem_grenade/antiweed

/obj/item/grenade/clusterbuster/cleaner
	name = "Mr. Proper"
	payload = /obj/item/grenade/chem_grenade/cleaner

/obj/item/grenade/clusterbuster/teargas
	name = "Oignon Grenade"
	payload = /obj/item/grenade/chem_grenade/teargas

/obj/item/grenade/clusterbuster/facid
	name = "Aciding Rain"
	payload = /obj/item/grenade/chem_grenade/facid

/obj/item/grenade/clusterbuster/syndieminibomb
	name = "SyndiWrath"
	payload = /obj/item/grenade/syndieminibomb

/obj/item/grenade/clusterbuster/spawner_manhacks
	name = "iViscerator"
	payload = /obj/item/grenade/spawnergrenade/manhacks

/obj/item/grenade/clusterbuster/spawner_spesscarp
	name = "Invasion of the Space Carps"
	payload = /obj/item/grenade/spawnergrenade/spesscarp

/obj/item/grenade/clusterbuster/soap
	name = "Slipocalypse"
	payload = /obj/item/grenade/spawnergrenade/syndiesoap

/obj/item/grenade/clusterbuster/clf3
	name = "WELCOME TO HELL"
	payload = /obj/item/grenade/chem_grenade/clf3

//random clusterbuster spawner
/obj/item/grenade/clusterbuster/random
	icon_state = "random_clusterbang"

/obj/item/grenade/clusterbuster/random/Initialize()
	..()
	var/real_type = pick(subtypesof(/obj/item/grenade/clusterbuster))
	new real_type(loc)
	return INITIALIZE_HINT_QDEL

//rainbow slime effect
/obj/item/grenade/clusterbuster/slime
	name = "Blorble Blorble"
	icon_state = "slimebang"
	base_state = "slimebang"
	payload_spawner = /obj/effect/payload_spawner/random_slime
	prime_sound = 'sound/effects/bubbles.ogg'

/obj/item/grenade/clusterbuster/slime/volatile
	payload_spawner = /obj/effect/payload_spawner/random_slime/volatile
