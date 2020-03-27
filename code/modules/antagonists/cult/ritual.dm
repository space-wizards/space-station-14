/*

This file contains the cult dagger and rune list code

*/


/obj/item/melee/cultblade/dagger/Initialize()
	. = ..()
	if(!LAZYLEN(GLOB.rune_types))
		GLOB.rune_types = list()
		var/static/list/non_revealed_runes = (subtypesof(/obj/effect/rune) - /obj/effect/rune/malformed)
		for(var/i_can_do_loops_now_thanks_remie in non_revealed_runes)
			var/obj/effect/rune/R = i_can_do_loops_now_thanks_remie
			GLOB.rune_types[initial(R.cultist_name)] = R //Uses the cultist name for displaying purposes

/obj/item/melee/cultblade/dagger/examine(mob/user)
	. = ..()
	if(iscultist(user) || isobserver(user))
		. += {"<span class='cult'>The scriptures of the Geometer. Allows the scribing of runes and access to the knowledge archives of the cult of Nar'Sie.\n
		Striking a cult structure will unanchor or reanchor it.\n
		Striking another cultist with it will purge holy water from them.\n
		Striking a noncultist, however, will tear their flesh.</span>"}

/obj/item/melee/cultblade/dagger/attack(mob/living/M, mob/living/user)
	if(iscultist(M))
		if(M.reagents && M.reagents.has_reagent(/datum/reagent/water/holywater)) //allows cultists to be rescued from the clutches of ordained religion
			to_chat(user, "<span class='cult'>You remove the taint from [M].</span>" )
			var/holy2unholy = M.reagents.get_reagent_amount(/datum/reagent/water/holywater)
			M.reagents.del_reagent(/datum/reagent/water/holywater)
			M.reagents.add_reagent(/datum/reagent/fuel/unholywater,holy2unholy)
			log_combat(user, M, "smacked", src, " removing the holy water from them")
		return FALSE
	. = ..()

/obj/item/melee/cultblade/dagger/attack_self(mob/user)
	if(!iscultist(user))
		to_chat(user, "<span class='warning'>[src] is covered in unintelligible shapes and markings.</span>")
		return
	scribe_rune(user)

/obj/item/melee/cultblade/dagger/proc/scribe_rune(mob/living/user)
	if(drawing_rune)
		return
	drawing_rune = TRUE
	scribe_rune_attempt(user)
	drawing_rune = FALSE

/obj/item/melee/cultblade/dagger/proc/scribe_rune_attempt(mob/living/user)
	var/turf/Turf = get_turf(user)
	var/chosen_keyword
	var/obj/effect/rune/rune_to_scribe
	var/entered_rune_name
	var/list/shields = list()
	var/area/A = get_area(src)
	var/datum/antagonist/cult/user_antag = user.mind.has_antag_datum(/datum/antagonist/cult,TRUE)
	if(!user_antag)
		return
	if(!check_rune_turf(Turf, user))
		return
	entered_rune_name = input(user, "Choose a rite to scribe.", "Sigils of Power") as null|anything in GLOB.rune_types
	if(!src || QDELETED(src) || !Adjacent(user) || user.incapacitated() || !check_rune_turf(Turf, user))
		return
	rune_to_scribe = GLOB.rune_types[entered_rune_name]
	if(!rune_to_scribe)
		return
	if(initial(rune_to_scribe.req_keyword))
		chosen_keyword = stripped_input(user, "Enter a keyword for the new rune.", "Words of Power")
		if(!chosen_keyword)
			drawing_rune = FALSE
			scribe_rune(user) //Go back a menu!
			return
	Turf = get_turf(user) //we may have moved. adjust as needed...
	A = get_area(src)
	if(!src || QDELETED(src) || !Adjacent(user) || user.incapacitated() || !check_rune_turf(Turf, user))
		return
	if(ispath(rune_to_scribe, /obj/effect/rune/summon) && (!is_station_level(Turf.z) || A.map_name == "Space"))
		to_chat(user, "<span class='cultitalic'><b>The veil is not weak enough here to summon a cultist, you must be on station!</b></span>")
		return
	if(ispath(rune_to_scribe, /obj/effect/rune/apocalypse))
		if((world.time - SSticker.round_start_time) <= 6000)
			var/wait = 6000 - (world.time - SSticker.round_start_time)
			to_chat(user, "<span class='cult italic'>The veil is not yet weak enough for this rune - it will be available in [DisplayTimeText(wait)].</span>")
			return
		var/datum/objective/eldergod/summon_objective = locate() in user_antag.cult_team.objectives
		if(!(A in summon_objective.summon_spots))
			to_chat(user, "<span class='cultlarge'>The Apocalypse rune will remove a ritual site (where Nar'Sie can be summoned), it can only be scribed in [english_list(summon_objective.summon_spots)]!</span>")
			return
		if(summon_objective.summon_spots.len < 2)
			to_chat(user, "<span class='cultlarge'>Only one ritual site remains - it must be reserved for the final summoning!</span>")
			return
	if(ispath(rune_to_scribe, /obj/effect/rune/narsie))
		var/datum/objective/eldergod/summon_objective = locate() in user_antag.cult_team.objectives
		var/datum/objective/sacrifice/sac_objective = locate() in user_antag.cult_team.objectives
		if(!summon_objective)
			to_chat(user, "<span class='warning'>Nar'Sie does not wish to be summoned!</span>")
			return
		if(sac_objective && !sac_objective.check_completion())
			to_chat(user, "<span class='warning'>The sacrifice is not complete. The portal would lack the power to open if you tried!</span>")
			return
		if(summon_objective.check_completion())
			to_chat(user, "<span class='cultlarge'>\"I am already here. There is no need to try to summon me now.\"</span>")
			return
		if(!(A in summon_objective.summon_spots))
			to_chat(user, "<span class='cultlarge'>The Geometer can only be summoned where the veil is weak - in [english_list(summon_objective.summon_spots)]!</span>")
			return
		var/confirm_final = alert(user, "This is the FINAL step to summon Nar'Sie; it is a long, painful ritual and the crew will be alerted to your presence", "Are you prepared for the final battle?", "My life for Nar'Sie!", "No")
		if(confirm_final == "No")
			to_chat(user, "<span class='cult'>You decide to prepare further before scribing the rune.</span>")
			return
		Turf = get_turf(user)
		A = get_area(src)
		if(!(A in summon_objective.summon_spots))  // Check again to make sure they didn't move
			to_chat(user, "<span class='cultlarge'>The Geometer can only be summoned where the veil is weak - in [english_list(summon_objective.summon_spots)]!</span>")
			return
		priority_announce("Figments from an eldritch god are being summoned by [user] into [A.map_name] from an unknown dimension. Disrupt the ritual at all costs!","Central Command Higher Dimensional Affairs", 'sound/ai/spanomalies.ogg')
		for(var/B in spiral_range_turfs(1, user, 1))
			var/obj/structure/emergency_shield/sanguine/N = new(B)
			shields += N
	user.visible_message("<span class='warning'>[user] [user.blood_volume ? "cuts open [user.p_their()] arm and begins writing in [user.p_their()] own blood":"begins sketching out a strange design"]!</span>", \
						 "<span class='cult'>You [user.blood_volume ? "slice open your arm and ":""]begin drawing a sigil of the Geometer.</span>")
	if(user.blood_volume)
		user.apply_damage(initial(rune_to_scribe.scribe_damage), BRUTE, pick(BODY_ZONE_L_ARM, BODY_ZONE_R_ARM))
	var/scribe_mod = initial(rune_to_scribe.scribe_delay)
	if(istype(get_turf(user), /turf/open/floor/engine/cult) && !(ispath(rune_to_scribe, /obj/effect/rune/narsie)))
		scribe_mod *= 0.5
	if(!do_after(user, scribe_mod, target = get_turf(user)))
		for(var/V in shields)
			var/obj/structure/emergency_shield/sanguine/S = V
			if(S && !QDELETED(S))
				qdel(S)
		return
	if(!check_rune_turf(Turf, user))
		return
	user.visible_message("<span class='warning'>[user] creates a strange circle[user.blood_volume ? " in [user.p_their()] own blood":""].</span>", \
						 "<span class='cult'>You finish drawing the arcane markings of the Geometer.</span>")
	for(var/V in shields)
		var/obj/structure/emergency_shield/S = V
		if(S && !QDELETED(S))
			qdel(S)
	var/obj/effect/rune/R = new rune_to_scribe(Turf, chosen_keyword)
	R.add_mob_blood(user)
	to_chat(user, "<span class='cult'>The [lowertext(R.cultist_name)] rune [R.cultist_desc]</span>")
	SSblackbox.record_feedback("tally", "cult_runes_scribed", 1, R.cultist_name)

/obj/item/melee/cultblade/dagger/proc/check_rune_turf(turf/T, mob/user)
	if(isspaceturf(T))
		to_chat(user, "<span class='warning'>You cannot scribe runes in space!</span>")
		return FALSE
	if(locate(/obj/effect/rune) in T)
		to_chat(user, "<span class='cult'>There is already a rune here.</span>")
		return FALSE
	var/area/A = get_area(T)
	if((!is_station_level(T.z) && !is_mining_level(T.z)) || (A && !(A.flags_1 & CULT_PERMITTED_1)))
		to_chat(user, "<span class='warning'>The veil is not weak enough here.</span>")
		return FALSE
	return TRUE
