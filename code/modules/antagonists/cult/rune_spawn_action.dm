//after a delay, creates a rune below you. for constructs creating runes.
/datum/action/innate/cult/create_rune
	name = "Summon Rune"
	desc = "Summons a rune"
	background_icon_state = "bg_demon"
	var/obj/effect/rune/rune_type
	var/cooldown = 0
	var/base_cooldown = 1800
	var/scribe_time = 60
	var/damage_interrupt = TRUE
	var/action_interrupt = TRUE
	var/obj/effect/temp_visual/cult/rune_spawn/rune_word_type
	var/obj/effect/temp_visual/cult/rune_spawn/rune_innerring_type
	var/obj/effect/temp_visual/cult/rune_spawn/rune_center_type
	var/rune_color

/datum/action/innate/cult/create_rune/IsAvailable()
	if(!rune_type || cooldown > world.time)
		return FALSE
	return ..()

/datum/action/innate/cult/create_rune/proc/turf_check(turf/T)
	if(!T)
		return FALSE
	if(isspaceturf(T))
		to_chat(owner, "<span class='warning'>You cannot scribe runes in space!</span>")
		return FALSE
	if(locate(/obj/effect/rune) in T)
		to_chat(owner, "<span class='cult'>There is already a rune here.</span>")
		return FALSE
	if(!is_station_level(T.z) && !is_mining_level(T.z))
		to_chat(owner, "<span class='warning'>The veil is not weak enough here.</span>")
		return FALSE
	return TRUE


/datum/action/innate/cult/create_rune/Activate()
	var/turf/T = get_turf(owner)
	if(turf_check(T))
		var/chosen_keyword
		if(initial(rune_type.req_keyword))
			chosen_keyword = stripped_input(owner, "Enter a keyword for the new rune.", "Words of Power")
			if(!chosen_keyword)
				return
	//the outer ring is always the same across all runes
		var/obj/effect/temp_visual/cult/rune_spawn/R1 = new(T, scribe_time, rune_color)
	//the rest are not always the same, so we need types for em
		var/obj/effect/temp_visual/cult/rune_spawn/R2
		if(rune_word_type)
			R2 = new rune_word_type(T, scribe_time, rune_color)
		var/obj/effect/temp_visual/cult/rune_spawn/R3
		if(rune_innerring_type)
			R3 = new rune_innerring_type(T, scribe_time, rune_color)
		var/obj/effect/temp_visual/cult/rune_spawn/R4
		if(rune_center_type)
			R4 = new rune_center_type(T, scribe_time, rune_color)

		cooldown = base_cooldown + world.time
		owner.update_action_buttons_icon()
		addtimer(CALLBACK(owner, /mob.proc/update_action_buttons_icon), base_cooldown)
		var/list/health
		if(damage_interrupt && isliving(owner))
			var/mob/living/L = owner
			health = list("health" = L.health)
		var/scribe_mod = scribe_time
		if(istype(T, /turf/open/floor/engine/cult))
			scribe_mod *= 0.5
		playsound(T, 'sound/magic/enter_blood.ogg', 100, FALSE)
		if(do_after(owner, scribe_mod, target = owner, extra_checks = CALLBACK(owner, /mob.proc/break_do_after_checks, health, action_interrupt)))
			var/obj/effect/rune/new_rune = new rune_type(owner.loc)
			new_rune.keyword = chosen_keyword
		else
			qdel(R1)
			if(R2)
				qdel(R2)
			if(R3)
				qdel(R3)
			if(R4)
				qdel(R4)
			cooldown = 0
			owner.update_action_buttons_icon()

//teleport rune
/datum/action/innate/cult/create_rune/tele
	name = "Summon Teleport Rune"
	desc = "Summons a teleport rune to your location, as though it has been there all along..."
	button_icon_state = "telerune"
	rune_type = /obj/effect/rune/teleport
	rune_word_type = /obj/effect/temp_visual/cult/rune_spawn/rune2
	rune_innerring_type = /obj/effect/temp_visual/cult/rune_spawn/rune2/inner
	rune_center_type = /obj/effect/temp_visual/cult/rune_spawn/rune2/center
	rune_color = RUNE_COLOR_TELEPORT

/datum/action/innate/cult/create_rune/wall
	name = "Summon Barrier Rune"
	desc = "Summons an active barrier rune to your location, as though it has been there all along..."
	button_icon_state = "barrier"
	rune_type = /obj/effect/rune/wall
	rune_word_type = /obj/effect/temp_visual/cult/rune_spawn/rune4
	rune_innerring_type = /obj/effect/temp_visual/cult/rune_spawn/rune4/inner
	rune_center_type = /obj/effect/temp_visual/cult/rune_spawn/rune4/center
	rune_color = RUNE_COLOR_DARKRED

/datum/action/innate/cult/create_rune/wall/Activate()
	. = ..()
	var/obj/effect/rune/wall/W = locate(/obj/effect/rune/wall) in owner.loc
	if(W)
		W.spread_density()

/datum/action/innate/cult/create_rune/revive
	name = "Summon Revive Rune"
	desc = "Summons a revive rune to your location, as though it has been there all along..."
	button_icon_state = "revive"
	rune_type = /obj/effect/rune/raise_dead
	rune_word_type = /obj/effect/temp_visual/cult/rune_spawn/rune1
	rune_innerring_type = /obj/effect/temp_visual/cult/rune_spawn/rune1/inner
	rune_center_type = /obj/effect/temp_visual/cult/rune_spawn/rune1/center
	rune_color = RUNE_COLOR_MEDIUMRED
