/mob/camera/blob/proc/can_buy(cost = 15)
	if(blob_points < cost)
		to_chat(src, "<span class='warning'>You cannot afford this, you need at least [cost] resources!</span>")
		return 0
	add_points(-cost)
	return 1

// Power verbs

/mob/camera/blob/proc/place_blob_core(placement_override, pop_override = FALSE)
	if(placed && placement_override != -1)
		return 1
	if(!placement_override)
		if(!pop_override)
			for(var/mob/living/M in range(7, src))
				if(ROLE_BLOB in M.faction)
					continue
				if(M.client)
					to_chat(src, "<span class='warning'>There is someone too close to place your blob core!</span>")
					return 0
			for(var/mob/living/M in view(13, src))
				if(ROLE_BLOB in M.faction)
					continue
				if(M.client)
					to_chat(src, "<span class='warning'>Someone could see your blob core from here!</span>")
					return 0
		var/turf/T = get_turf(src)
		if(T.density)
			to_chat(src, "<span class='warning'>This spot is too dense to place a blob core on!</span>")
			return 0
		for(var/obj/O in T)
			if(istype(O, /obj/structure/blob))
				if(istype(O, /obj/structure/blob/normal))
					qdel(O)
				else
					to_chat(src, "<span class='warning'>There is already a blob here!</span>")
					return 0
			else if(O.density)
				to_chat(src, "<span class='warning'>This spot is too dense to place a blob core on!</span>")
				return 0
		if(!pop_override && world.time <= manualplace_min_time && world.time <= autoplace_max_time)
			to_chat(src, "<span class='warning'>It is too early to place your blob core!</span>")
			return 0
	else if(placement_override == 1)
		var/turf/T = pick(GLOB.blobstart)
		forceMove(T) //got overrided? you're somewhere random, motherfucker
	if(placed && blob_core)
		blob_core.forceMove(loc)
	else
		var/obj/structure/blob/core/core = new(get_turf(src), src, 1)
		core.overmind = src
		blobs_legit += src
		blob_core = core
		core.update_icon()
	update_health_hud()
	placed = 1
	return 1

/mob/camera/blob/verb/transport_core()
	set category = "Blob"
	set name = "Jump to Core"
	set desc = "Move your camera to your core."
	if(blob_core)
		forceMove(blob_core.drop_location())

/mob/camera/blob/verb/jump_to_node()
	set category = "Blob"
	set name = "Jump to Node"
	set desc = "Move your camera to a selected node."
	if(GLOB.blob_nodes.len)
		var/list/nodes = list()
		for(var/i in 1 to GLOB.blob_nodes.len)
			var/obj/structure/blob/node/B = GLOB.blob_nodes[i]
			nodes["Blob Node #[i] ([get_area_name(B)])"] = B
		var/node_name = input(src, "Choose a node to jump to.", "Node Jump") in nodes
		var/obj/structure/blob/node/chosen_node = nodes[node_name]
		if(chosen_node)
			forceMove(chosen_node.loc)

/mob/camera/blob/proc/createSpecial(price, blobstrain, nearEquals, needsNode, turf/T)
	if(!T)
		T = get_turf(src)
	var/obj/structure/blob/B = (locate(/obj/structure/blob) in T)
	if(!B)
		to_chat(src, "<span class='warning'>There is no blob here!</span>")
		return
	if(!istype(B, /obj/structure/blob/normal))
		to_chat(src, "<span class='warning'>Unable to use this blob, find a normal one.</span>")
		return
	if(needsNode && nodes_required)
		if(!(locate(/obj/structure/blob/node) in orange(3, T)) && !(locate(/obj/structure/blob/core) in orange(4, T)))
			to_chat(src, "<span class='warning'>You need to place this blob closer to a node or core!</span>")
			return //handholdotron 2000
	if(nearEquals)
		for(var/obj/structure/blob/L in orange(nearEquals, T))
			if(L.type == blobstrain)
				to_chat(src, "<span class='warning'>There is a similar blob nearby, move more than [nearEquals] tiles away from it!</span>")
				return
	if(!can_buy(price))
		return
	var/obj/structure/blob/N = B.change_to(blobstrain, src)
	return N

/mob/camera/blob/verb/toggle_node_req()
	set category = "Blob"
	set name = "Toggle Node Requirement"
	set desc = "Toggle requiring nodes to place resource and factory blobs."
	nodes_required = !nodes_required
	if(nodes_required)
		to_chat(src, "<span class='warning'>You now require a nearby node or core to place factory and resource blobs.</span>")
	else
		to_chat(src, "<span class='warning'>You no longer require a nearby node or core to place factory and resource blobs.</span>")

/mob/camera/blob/verb/create_shield_power()
	set category = "Blob"
	set name = "Create/Upgrade Shield Blob (15)"
	set desc = "Create a shield blob, which will block fire and is hard to kill. Using this on an existing shield blob turns it into a reflective blob, capable of reflecting most projectiles but making it twice as weak to brute attacks."
	create_shield()

/mob/camera/blob/proc/create_shield(turf/T)
	var/obj/structure/blob/shield/S = locate(/obj/structure/blob/shield) in T
	if(S)
		if(!can_buy(BLOB_REFLECTOR_COST))
			return
		if(S.obj_integrity < S.max_integrity * 0.5)
			add_points(BLOB_REFLECTOR_COST)
			to_chat(src, "<span class='warning'>This shield blob is too damaged to be modified properly!</span>")
			return
		to_chat(src, "<span class='warning'>You secrete a reflective ooze over the shield blob, allowing it to reflect projectiles at the cost of reduced integrity.</span>")
		S.change_to(/obj/structure/blob/shield/reflective, src)
	else
		createSpecial(15, /obj/structure/blob/shield, 0, 0, T)

/mob/camera/blob/verb/create_resource()
	set category = "Blob"
	set name = "Create Resource Blob (40)"
	set desc = "Create a resource tower which will generate resources for you."
	createSpecial(40, /obj/structure/blob/resource, 4, 1)

/mob/camera/blob/verb/create_node()
	set category = "Blob"
	set name = "Create Node Blob (50)"
	set desc = "Create a node, which will power nearby factory and resource blobs."
	createSpecial(50, /obj/structure/blob/node, 5, 0)

/mob/camera/blob/verb/create_factory()
	set category = "Blob"
	set name = "Create Factory Blob (60)"
	set desc = "Create a spore tower that will spawn spores to harass your enemies."
	createSpecial(60, /obj/structure/blob/factory, 7, 1)

/mob/camera/blob/verb/create_blobbernaut()
	set category = "Blob"
	set name = "Create Blobbernaut (40)"
	set desc = "Create a powerful blobbernaut which is mildly smart and will attack enemies."
	var/turf/T = get_turf(src)
	var/obj/structure/blob/factory/B = locate(/obj/structure/blob/factory) in T
	if(!B)
		to_chat(src, "<span class='warning'>You must be on a factory blob!</span>")
		return
	if(B.naut) //if it already made a blobbernaut, it can't do it again
		to_chat(src, "<span class='warning'>This factory blob is already sustaining a blobbernaut.</span>")
		return
	if(B.obj_integrity < B.max_integrity * 0.5)
		to_chat(src, "<span class='warning'>This factory blob is too damaged to sustain a blobbernaut.</span>")
		return
	if(!can_buy(40))
		return

	B.naut = TRUE	//temporary placeholder to prevent creation of more than one per factory.
	to_chat(src, "<span class='notice'>You attempt to produce a blobbernaut.</span>")
	var/list/mob/dead/observer/candidates = pollGhostCandidates("Do you want to play as a [blobstrain.name] blobbernaut?", ROLE_BLOB, null, ROLE_BLOB, 50) //players must answer rapidly
	if(LAZYLEN(candidates)) //if we got at least one candidate, they're a blobbernaut now.
		B.max_integrity = initial(B.max_integrity) * 0.25 //factories that produced a blobbernaut have much lower health
		B.obj_integrity = min(B.obj_integrity, B.max_integrity)
		B.update_icon()
		B.visible_message("<span class='warning'><b>The blobbernaut [pick("rips", "tears", "shreds")] its way out of the factory blob!</b></span>")
		playsound(B.loc, 'sound/effects/splat.ogg', 50, TRUE)
		var/mob/living/simple_animal/hostile/blob/blobbernaut/blobber = new /mob/living/simple_animal/hostile/blob/blobbernaut(get_turf(B))
		flick("blobbernaut_produce", blobber)
		B.naut = blobber
		blobber.factory = B
		blobber.overmind = src
		blobber.update_icons()
		blobber.adjustHealth(blobber.maxHealth * 0.5)
		blob_mobs += blobber
		var/mob/dead/observer/C = pick(candidates)
		blobber.key = C.key
		SEND_SOUND(blobber, sound('sound/effects/blobattack.ogg'))
		SEND_SOUND(blobber, sound('sound/effects/attackblob.ogg'))
		to_chat(blobber, "<b>You are a blobbernaut!</b>")
		to_chat(blobber, "You are powerful, hard to kill, and slowly regenerate near nodes and cores, <span class='cultlarge'>but will slowly die if not near the blob</span> or if the factory that made you is killed.")
		to_chat(blobber, "You can communicate with other blobbernauts and overminds via <b>:b</b>")
		to_chat(blobber, "Your overmind's blob reagent is: <b><font color=\"[blobstrain.color]\">[blobstrain.name]</b></font>!")
		to_chat(blobber, "The <b><font color=\"[blobstrain.color]\">[blobstrain.name]</b></font> reagent [blobstrain.shortdesc ? "[blobstrain.shortdesc]" : "[blobstrain.description]"]")
	else
		to_chat(src, "<span class='warning'>You could not conjure a sentience for your blobbernaut. Your points have been refunded. Try again later.</span>")
		add_points(40)
		B.naut = null

/mob/camera/blob/verb/relocate_core()
	set category = "Blob"
	set name = "Relocate Core (80)"
	set desc = "Swaps the locations of your core and the selected node."
	var/turf/T = get_turf(src)
	var/obj/structure/blob/node/B = locate(/obj/structure/blob/node) in T
	if(!B)
		to_chat(src, "<span class='warning'>You must be on a blob node!</span>")
		return
	if(!blob_core)
		to_chat(src, "<span class='userdanger'>You have no core and are about to die! May you rest in peace.</span>")
		return
	var/area/A = get_area(T)
	if(isspaceturf(T) || A && !A.blob_allowed)
		to_chat(src, "<span class='warning'>You cannot relocate your core here!</span>")
		return
	if(!can_buy(80))
		return
	var/turf/old_turf = get_turf(blob_core)
	var/olddir = blob_core.dir
	blob_core.forceMove(T)
	blob_core.setDir(B.dir)
	B.forceMove(old_turf)
	B.setDir(olddir)

/mob/camera/blob/verb/revert()
	set category = "Blob"
	set name = "Remove Blob"
	set desc = "Removes a blob, giving you back some resources."
	var/turf/T = get_turf(src)
	remove_blob(T)

/mob/camera/blob/proc/remove_blob(turf/T)
	var/obj/structure/blob/B = locate() in T
	if(!B)
		to_chat(src, "<span class='warning'>There is no blob there!</span>")
		return
	if(B.point_return < 0)
		to_chat(src, "<span class='warning'>Unable to remove this blob.</span>")
		return
	if(max_blob_points < B.point_return + blob_points)
		to_chat(src, "<span class='warning'>You have too many resources to remove this blob!</span>")
		return
	if(B.point_return)
		add_points(B.point_return)
		to_chat(src, "<span class='notice'>Gained [B.point_return] resources from removing \the [B].</span>")
	qdel(B)

/mob/camera/blob/verb/expand_blob_power()
	set category = "Blob"
	set name = "Expand/Attack Blob ([BLOB_SPREAD_COST])"
	set desc = "Attempts to create a new blob in this tile. If the tile isn't clear, instead attacks it, damaging mobs and objects and refunding [BLOB_ATTACK_REFUND] points."
	var/turf/T = get_turf(src)
	expand_blob(T)

/mob/camera/blob/proc/expand_blob(turf/T)
	if(world.time < last_attack)
		return
	var/list/possibleblobs = list()
	for(var/obj/structure/blob/AB in range(T, 1))
		possibleblobs += AB
	if(!possibleblobs.len)
		to_chat(src, "<span class='warning'>There is no blob adjacent to the target tile!</span>")
		return
	if(can_buy(BLOB_SPREAD_COST))
		var/attacksuccess = FALSE
		for(var/mob/living/L in T)
			if(ROLE_BLOB in L.faction) //no friendly/dead fire
				continue
			if(L.stat != DEAD)
				attacksuccess = TRUE
			blobstrain.attack_living(L, possibleblobs)
		var/obj/structure/blob/B = locate() in T
		if(B)
			if(attacksuccess) //if we successfully attacked a turf with a blob on it, only give an attack refund
				B.blob_attack_animation(T, src)
				add_points(BLOB_ATTACK_REFUND)
			else
				to_chat(src, "<span class='warning'>There is a blob there!</span>")
				add_points(BLOB_SPREAD_COST) //otherwise, refund all of the cost
		else
			var/list/cardinalblobs = list()
			var/list/diagonalblobs = list()
			for(var/I in possibleblobs)
				var/obj/structure/blob/IB = I
				if(get_dir(IB, T) in GLOB.cardinals)
					cardinalblobs += IB
				else
					diagonalblobs += IB
			var/obj/structure/blob/OB
			if(cardinalblobs.len)
				OB = pick(cardinalblobs)
				if(!OB.expand(T, src))
					add_points(BLOB_ATTACK_REFUND) //assume it's attacked SOMETHING, possibly a structure
			else
				OB = pick(diagonalblobs)
				if(attacksuccess)
					OB.blob_attack_animation(T, src)
					playsound(OB, 'sound/effects/splat.ogg', 50, TRUE)
					add_points(BLOB_ATTACK_REFUND)
				else
					add_points(BLOB_SPREAD_COST) //if we're attacking diagonally and didn't hit anything, refund
		if(attacksuccess)
			last_attack = world.time + CLICK_CD_MELEE
		else
			last_attack = world.time + CLICK_CD_RAPID

/mob/camera/blob/verb/rally_spores_power()
	set category = "Blob"
	set name = "Rally Spores"
	set desc = "Rally your spores to move to a target location."
	var/turf/T = get_turf(src)
	rally_spores(T)

/mob/camera/blob/proc/rally_spores(turf/T)
	to_chat(src, "You rally your spores.")
	var/list/surrounding_turfs = block(locate(T.x - 1, T.y - 1, T.z), locate(T.x + 1, T.y + 1, T.z))
	if(!surrounding_turfs.len)
		return
	for(var/mob/living/simple_animal/hostile/blob/blobspore/BS in blob_mobs)
		if(isturf(BS.loc) && get_dist(BS, T) <= 35)
			BS.LoseTarget()
			BS.Goto(pick(surrounding_turfs), BS.move_to_delay)

/mob/camera/blob/verb/blob_broadcast()
	set category = "Blob"
	set name = "Blob Broadcast"
	set desc = "Speak with your blob spores and blobbernauts as your mouthpieces."
	var/speak_text = input(src, "What would you like to say with your minions?", "Blob Broadcast", null) as text|null
	if(!speak_text)
		return
	else
		to_chat(src, "You broadcast with your minions, <B>[speak_text]</B>")
	for(var/BLO in blob_mobs)
		var/mob/living/simple_animal/hostile/blob/BM = BLO
		if(BM.stat == CONSCIOUS)
			BM.say(speak_text)

/mob/camera/blob/verb/strain_reroll()
	set category = "Blob"
	set name = "Reactive Strain Adaptation (40)"
	set desc = "Replaces your strain with a random, different one."
	if(!rerolling && (free_strain_rerolls || can_buy(40)))
		rerolling = TRUE
		reroll_strain()
		rerolling = FALSE
		if(free_strain_rerolls)
			free_strain_rerolls--
		last_reroll_time = world.time

/mob/camera/blob/proc/reroll_strain()
	var/list/choices = list()
	while (length(choices) < 4)
		var/datum/blobstrain/bs = pick((GLOB.valid_blobstrains))
		choices[initial(bs.name)] = bs

	var/choice = input(usr, "Please choose a new strain","Strain") as anything in sortList(choices, /proc/cmp_typepaths_asc)
	if (choice && choices[choice] && !QDELETED(src))
		var/datum/blobstrain/bs = choices[choice]
		set_strain(bs)


/mob/camera/blob/verb/blob_help()
	set category = "Blob"
	set name = "*Blob Help*"
	set desc = "Help on how to blob."
	to_chat(src, "<b>As the overmind, you can control the blob!</b>")
	to_chat(src, "Your blob reagent is: <b><font color=\"[blobstrain.color]\">[blobstrain.name]</b></font>!")
	to_chat(src, "The <b><font color=\"[blobstrain.color]\">[blobstrain.name]</b></font> reagent [blobstrain.description]")
	if(blobstrain.effectdesc)
		to_chat(src, "The <b><font color=\"[blobstrain.color]\">[blobstrain.name]</b></font> reagent [blobstrain.effectdesc]")
	to_chat(src, "<b>You can expand, which will attack people, damage objects, or place a Normal Blob if the tile is clear.</b>")
	to_chat(src, "<i>Normal Blobs</i> will expand your reach and can be upgraded into special blobs that perform certain functions.")
	to_chat(src, "<b>You can upgrade normal blobs into the following types of blob:</b>")
	to_chat(src, "<i>Shield Blobs</i> are strong and expensive blobs which take more damage. In additon, they are fireproof and can block air, use these to protect yourself from station fires. Upgrading them again will result in a reflective blob, capable of reflecting most projectiles at the cost of the strong blob's extra health.")
	to_chat(src, "<i>Resource Blobs</i> are blobs which produce more resources for you, build as many of these as possible to consume the station. This type of blob must be placed near node blobs or your core to work.")
	to_chat(src, "<i>Factory Blobs</i> are blobs that spawn blob spores which will attack nearby enemies. This type of blob must be placed near node blobs or your core to work.")
	to_chat(src, "<i>Blobbernauts</i> can be produced from factories for a cost, and are hard to kill, powerful, and moderately smart. The factory used to create one will become fragile and briefly unable to produce spores.")
	to_chat(src, "<i>Node Blobs</i> are blobs which grow, like the core. Like the core it can activate resource and factory blobs.")
	to_chat(src, "<b>In addition to the buttons on your HUD, there are a few click shortcuts to speed up expansion and defense.</b>")
	to_chat(src, "<b>Shortcuts:</b> Click = Expand Blob <b>|</b> Middle Mouse Click = Rally Spores <b>|</b> Ctrl Click = Create Shield Blob <b>|</b> Alt Click = Remove Blob")
	to_chat(src, "Attempting to talk will send a message to all other overminds, allowing you to coordinate with them.")
	if(!placed && autoplace_max_time <= world.time)
		to_chat(src, "<span class='big'><font color=\"#EE4000\">You will automatically place your blob core in [DisplayTimeText(autoplace_max_time - world.time)].</font></span>")
		to_chat(src, "<span class='big'><font color=\"#EE4000\">You [manualplace_min_time ? "will be able to":"can"] manually place your blob core by pressing the Place Blob Core button in the bottom right corner of the screen.</font></span>")
