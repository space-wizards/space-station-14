/obj/screen/alert/status_effect/rainbow_protection
	name = "Rainbow Protection"
	desc = "You are defended from harm, but so are those you might seek to injure!"
	icon_state = "slime_rainbowshield"

/datum/status_effect/rainbow_protection
	id = "rainbow_protection"
	duration = 100
	alert_type = /obj/screen/alert/status_effect/rainbow_protection
	var/originalcolor

/datum/status_effect/rainbow_protection/on_apply()
	owner.status_flags |= GODMODE
	ADD_TRAIT(owner, TRAIT_PACIFISM, /datum/status_effect/rainbow_protection)
	owner.visible_message("<span class='warning'>[owner] shines with a brilliant rainbow light.</span>",
		"<span class='notice'>You feel protected by an unknown force!</span>")
	originalcolor = owner.color
	return ..()

/datum/status_effect/rainbow_protection/tick()
	owner.color = rgb(rand(0,255),rand(0,255),rand(0,255))
	return ..()

/datum/status_effect/rainbow_protection/on_remove()
	owner.status_flags &= ~GODMODE
	owner.color = originalcolor
	REMOVE_TRAIT(owner, TRAIT_PACIFISM, /datum/status_effect/rainbow_protection)
	owner.visible_message("<span class='notice'>[owner] stops glowing, the rainbow light fading away.</span>",
		"<span class='warning'>You no longer feel protected...</span>")

/obj/screen/alert/status_effect/slimeskin
	name = "Adamantine Slimeskin"
	desc = "You are covered in a thick, non-neutonian gel."
	icon_state = "slime_stoneskin"

/datum/status_effect/slimeskin
	id = "slimeskin"
	duration = 300
	alert_type = /obj/screen/alert/status_effect/slimeskin
	var/originalcolor

/datum/status_effect/slimeskin/on_apply()
	originalcolor = owner.color
	owner.color = "#3070CC"
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		H.physiology.damage_resistance += 10
	owner.visible_message("<span class='warning'>[owner] is suddenly covered in a strange, blue-ish gel!</span>",
		"<span class='notice'>You are covered in a thick, rubbery gel.</span>")
	return ..()

/datum/status_effect/slimeskin/on_remove()
	owner.color = originalcolor
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		H.physiology.damage_resistance -= 10
	owner.visible_message("<span class='warning'>[owner]'s gel coating liquefies and dissolves away.</span>",
		"<span class='notice'>Your gel second-skin dissolves!</span>")

/datum/status_effect/slimerecall
	id = "slime_recall"
	duration = -1 //Will be removed by the extract.
	alert_type = null
	var/interrupted = FALSE
	var/mob/target
	var/icon/bluespace

/datum/status_effect/slimerecall/on_apply()
	RegisterSignal(owner, COMSIG_LIVING_RESIST, .proc/resistField)
	to_chat(owner, "<span class='danger'>You feel a sudden tug from an unknown force, and feel a pull to bluespace!</span>")
	to_chat(owner, "<span class='notice'>Resist if you wish avoid the force!</span>")
	bluespace = icon('icons/effects/effects.dmi',"chronofield")
	owner.add_overlay(bluespace)
	return ..()

/datum/status_effect/slimerecall/proc/resistField()
	interrupted = TRUE
	owner.remove_status_effect(src)
/datum/status_effect/slimerecall/on_remove()
	UnregisterSignal(owner, COMSIG_LIVING_RESIST)
	owner.cut_overlay(bluespace)
	if(interrupted || !ismob(target))
		to_chat(owner, "<span class='warning'>The bluespace tug fades away, and you feel that the force has passed you by.</span>")
		return
	owner.visible_message("<span class='warning'>[owner] disappears in a flurry of sparks!</span>",
		"<span class='warning'>The unknown force snatches briefly you from reality, and deposits you next to [target]!</span>")
	do_sparks(3, TRUE, owner)
	owner.forceMove(target.loc)

/obj/screen/alert/status_effect/freon/stasis
	desc = "You're frozen inside of a protective ice cube! While inside, you can't do anything, but are immune to harm! Resist to get out."

/datum/status_effect/frozenstasis
	id = "slime_frozen"
	status_type = STATUS_EFFECT_UNIQUE
	duration = -1 //Will remove self when block breaks.
	alert_type = /obj/screen/alert/status_effect/freon/stasis
	var/obj/structure/ice_stasis/cube

/datum/status_effect/frozenstasis/on_apply()
	RegisterSignal(owner, COMSIG_LIVING_RESIST, .proc/breakCube)
	cube = new /obj/structure/ice_stasis(get_turf(owner))
	owner.forceMove(cube)
	owner.status_flags |= GODMODE
	return ..()

/datum/status_effect/frozenstasis/tick()
	if(!cube || owner.loc != cube)
		owner.remove_status_effect(src)

/datum/status_effect/frozenstasis/proc/breakCube()
	owner.remove_status_effect(src)

/datum/status_effect/frozenstasis/on_remove()
	if(cube)
		qdel(cube)
	owner.status_flags &= ~GODMODE
	UnregisterSignal(owner, COMSIG_LIVING_RESIST)

/datum/status_effect/slime_clone
	id = "slime_cloned"
	status_type = STATUS_EFFECT_UNIQUE
	duration = -1
	alert_type = null
	var/mob/living/clone
	var/datum/mind/originalmind //For when the clone gibs.

/datum/status_effect/slime_clone/on_apply()
	var/typepath = owner.type
	clone = new typepath(owner.loc)
	var/mob/living/carbon/O = owner
	var/mob/living/carbon/C = clone
	if(istype(C) && istype(O))
		C.real_name = O.real_name
		O.dna.transfer_identity(C)
		C.updateappearance(mutcolor_update=1)
	if(owner.mind)
		originalmind = owner.mind
		owner.mind.transfer_to(clone)
	clone.apply_status_effect(/datum/status_effect/slime_clone_decay)
	return ..()

/datum/status_effect/slime_clone/tick()
	if(!istype(clone) || clone.stat != CONSCIOUS)
		owner.remove_status_effect(src)

/datum/status_effect/slime_clone/on_remove()
	if(clone && clone.mind && owner)
		clone.mind.transfer_to(owner)
	else
		if(owner && originalmind)
			originalmind.transfer_to(owner)
			if(originalmind.key)
				owner.ckey = originalmind.key
	if(clone)
		clone.unequip_everything()
		qdel(clone)

/obj/screen/alert/status_effect/clone_decay
	name = "Clone Decay"
	desc = "You are simply a construct, and cannot maintain this form forever. You will be returned to your original body if you should fall."
	icon_state = "slime_clonedecay"

/datum/status_effect/slime_clone_decay
	id = "slime_clonedecay"
	status_type = STATUS_EFFECT_UNIQUE
	duration = -1
	alert_type = /obj/screen/alert/status_effect/clone_decay

/datum/status_effect/slime_clone_decay/tick()
	owner.adjustToxLoss(1, 0)
	owner.adjustOxyLoss(1, 0)
	owner.adjustBruteLoss(1, 0)
	owner.adjustFireLoss(1, 0)
	owner.color = "#007BA7"

/obj/screen/alert/status_effect/bloodchill
	name = "Bloodchilled"
	desc = "You feel a shiver down your spine after getting hit with a glob of cold blood. You'll move slower and get frostbite for a while!"
	icon_state = "bloodchill"

/datum/status_effect/bloodchill
	id = "bloodchill"
	duration = 100
	alert_type = /obj/screen/alert/status_effect/bloodchill

/datum/status_effect/bloodchill/on_apply()
	owner.add_movespeed_modifier("bloodchilled", TRUE, 100, NONE, override = TRUE, multiplicative_slowdown = 3)
	return ..()

/datum/status_effect/bloodchill/tick()
	if(prob(50))
		owner.adjustFireLoss(2)

/datum/status_effect/bloodchill/on_remove()
	owner.remove_movespeed_modifier("bloodchilled")

/datum/status_effect/bonechill
	id = "bonechill"
	duration = 80
	alert_type = /obj/screen/alert/status_effect/bonechill

/datum/status_effect/bonechill/on_apply()
	owner.add_movespeed_modifier("bonechilled", TRUE, 100, NONE, override = TRUE, multiplicative_slowdown = 3)
	return ..()

/datum/status_effect/bonechill/tick()
	if(prob(50))
		owner.adjustFireLoss(1)
		owner.Jitter(3)
		owner.adjust_bodytemperature(-10)

/datum/status_effect/bonechill/on_remove()
	owner.remove_movespeed_modifier("bonechilled")

/obj/screen/alert/status_effect/bonechill
	name = "Bonechilled"
	desc = "You feel a shiver down your spine after hearing the haunting noise of bone rattling. You'll move slower and get frostbite for a while!"
	icon_state = "bloodchill"

/datum/status_effect/rebreathing
	id = "rebreathing"
	duration = -1
	alert_type = null

datum/status_effect/rebreathing/tick()
	owner.adjustOxyLoss(-6, 0) //Just a bit more than normal breathing.

///////////////////////////////////////////////////////
//////////////////CONSUMING EXTRACTS///////////////////
///////////////////////////////////////////////////////

/datum/status_effect/firecookie
	id = "firecookie"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 100

/datum/status_effect/firecookie/on_apply()
	ADD_TRAIT(owner, TRAIT_RESISTCOLD,"firecookie")
	owner.adjust_bodytemperature(110)
	return ..()

/datum/status_effect/firecookie/on_remove()
	REMOVE_TRAIT(owner, TRAIT_RESISTCOLD,"firecookie")

/datum/status_effect/watercookie
	id = "watercookie"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 100

/datum/status_effect/watercookie/on_apply()
	ADD_TRAIT(owner, TRAIT_NOSLIPWATER,"watercookie")
	return ..()

/datum/status_effect/watercookie/tick()
	for(var/turf/open/T in range(get_turf(owner),1))
		T.MakeSlippery(TURF_WET_WATER, min_wet_time = 10, wet_time_to_add = 5)

/datum/status_effect/watercookie/on_remove()
	REMOVE_TRAIT(owner, TRAIT_NOSLIPWATER,"watercookie")

/datum/status_effect/metalcookie
	id = "metalcookie"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 100

/datum/status_effect/metalcookie/on_apply()
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		H.physiology.brute_mod *= 0.9
	return ..()

/datum/status_effect/metalcookie/on_remove()
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		H.physiology.brute_mod /= 0.9

/datum/status_effect/sparkcookie
	id = "sparkcookie"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 300
	var/original_coeff

/datum/status_effect/sparkcookie/on_apply()
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		original_coeff = H.physiology.siemens_coeff
		H.physiology.siemens_coeff = 0
	return ..()

/datum/status_effect/sparkcookie/on_remove()
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		H.physiology.siemens_coeff = original_coeff

/datum/status_effect/toxincookie
	id = "toxincookie"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 600

/datum/status_effect/toxincookie/on_apply()
	ADD_TRAIT(owner, TRAIT_TOXINLOVER,"toxincookie")
	return ..()

/datum/status_effect/toxincookie/on_remove()
	REMOVE_TRAIT(owner, TRAIT_TOXINLOVER,"toxincookie")

/datum/status_effect/timecookie
	id = "timecookie"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 600

/datum/status_effect/timecookie/on_apply()
	if(ishuman(owner))
		var/mob/living/carbon/human/H
		H.physiology.do_after_speed *= 0.95
	return ..()

/datum/status_effect/timecookie/on_remove()
	if(ishuman(owner))
		var/mob/living/carbon/human/H
		H.physiology.do_after_speed /= 0.95

/datum/status_effect/lovecookie
	id = "lovecookie"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 300

/datum/status_effect/lovecookie/tick()
	if(owner.stat != CONSCIOUS)
		return
	if(iscarbon(owner))
		var/mob/living/carbon/C = owner
		if(C.handcuffed)
			return
	var/list/huggables = list()
	for(var/mob/living/carbon/L in range(get_turf(owner),1))
		if(L != owner)
			huggables += L
	if(length(huggables))
		var/mob/living/carbon/hugged = pick(huggables)
		owner.visible_message("<span class='notice'>[owner] hugs [hugged]!</span>", "<span class='notice'>You hug [hugged]!</span>")

/datum/status_effect/tarcookie
	id = "tarcookie"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 100

/datum/status_effect/tarcookie/tick()
	for(var/mob/living/carbon/human/L in range(get_turf(owner),1))
		if(L != owner)
			L.apply_status_effect(/datum/status_effect/tarfoot)

/datum/status_effect/tarfoot
	id = "tarfoot"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 30

/datum/status_effect/tarfoot/on_apply()
	owner.add_movespeed_modifier(MOVESPEED_ID_TARFOOT, update=TRUE, priority=100, multiplicative_slowdown=0.5, blacklisted_movetypes=(FLYING|FLOATING))
	return ..()

/datum/status_effect/tarfoot/on_remove()
	owner.remove_movespeed_modifier(MOVESPEED_ID_TARFOOT)

/datum/status_effect/spookcookie
	id = "spookcookie"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 300

/datum/status_effect/spookcookie/on_apply()
	var/image/I = image(icon = 'icons/mob/simple_human.dmi', icon_state = "skeleton", layer = ABOVE_MOB_LAYER, loc = owner)
	I.override = 1
	owner.add_alt_appearance(/datum/atom_hud/alternate_appearance/basic/everyone, "spookyscary", I)
	return ..()

/datum/status_effect/spookcookie/on_remove()
	owner.remove_alt_appearance("spookyscary")

/datum/status_effect/peacecookie
	id = "peacecookie"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 100

/datum/status_effect/peacecookie/tick()
	for(var/mob/living/L in range(get_turf(owner),1))
		L.apply_status_effect(/datum/status_effect/plur)

/datum/status_effect/plur
	id = "plur"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 30

/datum/status_effect/plur/on_apply()
	ADD_TRAIT(owner, TRAIT_PACIFISM, "peacecookie")
	return ..()

/datum/status_effect/plur/on_remove()
	REMOVE_TRAIT(owner, TRAIT_PACIFISM, "peacecookie")

/datum/status_effect/adamantinecookie
	id = "adamantinecookie"
	status_type = STATUS_EFFECT_REPLACE
	alert_type = null
	duration = 100

/datum/status_effect/adamantinecookie/on_apply()
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		H.physiology.burn_mod *= 0.9
	return ..()

/datum/status_effect/adamantinecookie/on_remove()
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		H.physiology.burn_mod /= 0.9

///////////////////////////////////////////////////////
//////////////////STABILIZED EXTRACTS//////////////////
///////////////////////////////////////////////////////

/datum/status_effect/stabilized //The base stabilized extract effect, has no effect of its' own.
	id = "stabilizedbase"
	duration = -1
	alert_type = null
	var/obj/item/slimecross/stabilized/linked_extract
	var/colour = "null"

/datum/status_effect/stabilized/tick()
	if(!linked_extract || !linked_extract.loc) //Sanity checking
		qdel(src)
		return
	if(linked_extract && linked_extract.loc != owner && linked_extract.loc.loc != owner)
		linked_extract.linked_effect = null
		if(!QDELETED(linked_extract))
			linked_extract.owner = null
			START_PROCESSING(SSobj,linked_extract)
		qdel(src)
	return ..()

/datum/status_effect/stabilized/null //This shouldn't ever happen, but just in case.
	id = "stabilizednull"


//Stabilized effects start below.
/datum/status_effect/stabilized/grey
	id = "stabilizedgrey"
	colour = "grey"

/datum/status_effect/stabilized/grey/tick()
	for(var/mob/living/simple_animal/slime/S in range(1, get_turf(owner)))
		if(!(owner in S.Friends))
			to_chat(owner, "<span class='notice'>[linked_extract] pulses gently as it communicates with [S].</span>")
			S.Friends[owner] = 1
	return ..()

/datum/status_effect/stabilized/orange
	id = "stabilizedorange"
	colour = "orange"

/datum/status_effect/stabilized/orange/tick()
	var/body_temperature_difference = BODYTEMP_NORMAL - owner.bodytemperature
	owner.adjust_bodytemperature(min(5,body_temperature_difference))
	return ..()

/datum/status_effect/stabilized/purple
	id = "stabilizedpurple"
	colour = "purple"

/datum/status_effect/stabilized/purple/tick()
	var/is_healing = FALSE
	if(owner.getBruteLoss() > 0)
		owner.adjustBruteLoss(-0.2)
		is_healing = TRUE
	if(owner.getFireLoss() > 0)
		owner.adjustFireLoss(-0.2)
		is_healing = TRUE
	if(owner.getToxLoss() > 0)
		owner.adjustToxLoss(-0.2, forced = TRUE) //Slimepeople should also get healed.
		is_healing = TRUE
	if(is_healing)
		examine_text = "<span class='warning'>SUBJECTPRONOUN is regenerating slowly, purplish goo filling in small injuries!</span>"
		new /obj/effect/temp_visual/heal(get_turf(owner), "#FF0000")
	else
		examine_text = null
	..()

/datum/status_effect/stabilized/blue
	id = "stabilizedblue"
	colour = "blue"

/datum/status_effect/stabilized/blue/on_apply()
	ADD_TRAIT(owner, TRAIT_NOSLIPWATER, "slimestatus")
	return ..()

datum/status_effect/stabilized/blue/on_remove()
	REMOVE_TRAIT(owner, TRAIT_NOSLIPWATER, "slimestatus")

/datum/status_effect/stabilized/metal
	id = "stabilizedmetal"
	colour = "metal"
	var/cooldown = 30
	var/max_cooldown = 30

/datum/status_effect/stabilized/metal/tick()
	if(cooldown > 0)
		cooldown--
	else
		cooldown = max_cooldown
		var/list/sheets = list()
		for(var/obj/item/stack/sheet/S in owner.GetAllContents())
			if(S.amount < S.max_amount)
				sheets += S

		if(sheets.len > 0)
			var/obj/item/stack/sheet/S = pick(sheets)
			S.amount++
			to_chat(owner, "<span class='notice'>[linked_extract] adds a layer of slime to [S], which metamorphosizes into another sheet of material!</span>")
	return ..()


/datum/status_effect/stabilized/yellow
	id = "stabilizedyellow"
	colour = "yellow"
	var/cooldown = 10
	var/max_cooldown = 10
	examine_text = "<span class='warning'>Nearby electronics seem just a little more charged wherever SUBJECTPRONOUN goes.</span>"

/datum/status_effect/stabilized/yellow/tick()
	if(cooldown > 0)
		cooldown--
		return ..()
	cooldown = max_cooldown
	var/list/batteries = list()
	for(var/obj/item/stock_parts/cell/C in owner.GetAllContents())
		if(C.charge < C.maxcharge)
			batteries += C
	if(batteries.len)
		var/obj/item/stock_parts/cell/ToCharge = pick(batteries)
		ToCharge.charge += min(ToCharge.maxcharge - ToCharge.charge, ToCharge.maxcharge/10) //10% of the cell, or to maximum.
		to_chat(owner, "<span class='notice'>[linked_extract] discharges some energy into a device you have.</span>")
	return ..()

/obj/item/hothands
	name = "burning fingertips"
	desc = "You shouldn't see this."

/obj/item/hothands/get_temperature()
	return 290 //Below what's required to ignite plasma.

/datum/status_effect/stabilized/darkpurple
	id = "stabilizeddarkpurple"
	colour = "dark purple"
	var/obj/item/hothands/fire
	examine_text = "<span class='notice'>Their fingertips burn brightly!</span>"

/datum/status_effect/stabilized/darkpurple/on_apply()
	ADD_TRAIT(owner, TRAIT_RESISTHEATHANDS, "slimestatus")
	fire = new(owner)
	return ..()

/datum/status_effect/stabilized/darkpurple/tick()
	var/obj/item/I = owner.get_active_held_item()
	var/obj/item/reagent_containers/food/snacks/F = I
	if(istype(F))
		if(F.cooked_type)
			to_chat(owner, "<span class='warning'>[linked_extract] flares up brightly, and your hands alone are enough cook [F]!</span>")
			var/obj/item/result = F.microwave_act()
			if(istype(result))
				owner.put_in_hands(result)
	else
		I.attackby(fire, owner)
	return ..()

/datum/status_effect/stabilized/darkpurple/on_remove()
	REMOVE_TRAIT(owner, TRAIT_RESISTHEATHANDS, "slimestatus")
	qdel(fire)

/datum/status_effect/stabilized/darkblue
	id = "stabilizeddarkblue"
	colour = "dark blue"

/datum/status_effect/stabilized/darkblue/tick()
	if(owner.fire_stacks > 0 && prob(80))
		owner.fire_stacks--
		if(owner.fire_stacks <= 0)
			to_chat(owner, "<span class='notice'>[linked_extract] coats you in a watery goo, extinguishing the flames.</span>")
	var/obj/O = owner.get_active_held_item()
	if(O)
		O.extinguish() //All shamelessly copied from water's reaction_obj, since I didn't seem to be able to get it here for some reason.
		O.acid_level = 0
	// Monkey cube
	if(istype(O, /obj/item/reagent_containers/food/snacks/monkeycube))
		to_chat(owner, "<span class='warning'>[linked_extract] kept your hands wet! It makes [O] expand!</span>")
		var/obj/item/reagent_containers/food/snacks/monkeycube/cube = O
		cube.Expand()

	// Dehydrated carp
	else if(istype(O, /obj/item/toy/plush/carpplushie/dehy_carp))
		to_chat(owner, "<span class='warning'>[linked_extract] kept your hands wet! It makes [O] expand!</span>")
		var/obj/item/toy/plush/carpplushie/dehy_carp/dehy = O
		dehy.Swell() // Makes a carp

	else if(istype(O, /obj/item/stack/sheet/hairlesshide))
		to_chat(owner, "<span class='warning'>[linked_extract] kept your hands wet! It wets [O]!</span>")
		var/obj/item/stack/sheet/hairlesshide/HH = O
		new /obj/item/stack/sheet/wetleather(get_turf(HH), HH.amount)
		qdel(HH)
	..()

/datum/status_effect/stabilized/silver
	id = "stabilizedsilver"
	colour = "silver"

/datum/status_effect/stabilized/silver/on_apply()
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		H.physiology.hunger_mod *= 0.8 //20% buff
	return ..()

/datum/status_effect/stabilized/silver/on_remove()
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		H.physiology.hunger_mod /= 0.8

//Bluespace has an icon because it's kinda active.
/obj/screen/alert/status_effect/bluespaceslime
	name = "Stabilized Bluespace Extract"
	desc = "You shouldn't see this, since we set it to change automatically!"
	icon_state = "slime_bluespace_on"

/datum/status_effect/bluespacestabilization
	id = "stabilizedbluespacecooldown"
	duration = 1200
	alert_type = null

/datum/status_effect/stabilized/bluespace
	id = "stabilizedbluespace"
	colour = "bluespace"
	alert_type = /obj/screen/alert/status_effect/bluespaceslime
	var/healthcheck

/datum/status_effect/stabilized/bluespace/tick()
	if(owner.has_status_effect(/datum/status_effect/bluespacestabilization))
		linked_alert.desc = "The stabilized bluespace extract is still aligning you with the bluespace axis."
		linked_alert.icon_state = "slime_bluespace_off"
		return ..()
	else
		linked_alert.desc = "The stabilized bluespace extract will try to redirect you from harm!"
		linked_alert.icon_state = "slime_bluespace_on"

	if(healthcheck && (healthcheck - owner.health) > 5)
		owner.visible_message("<span class='warning'>[linked_extract] notices the sudden change in [owner]'s physical health, and activates!</span>")
		do_sparks(5,FALSE,owner)
		var/F = find_safe_turf(zlevels = owner.z, extended_safety_checks = TRUE)
		var/range = 0
		if(!F)
			F = get_turf(owner)
			range = 50
		if(do_teleport(owner, F, range, channel = TELEPORT_CHANNEL_BLUESPACE))
			to_chat(owner, "<span class='notice'>[linked_extract] will take some time to re-align you on the bluespace axis.</span>")
			do_sparks(5,FALSE,owner)
			owner.apply_status_effect(/datum/status_effect/bluespacestabilization)
	healthcheck = owner.health
	return ..()

/datum/status_effect/stabilized/sepia
	id = "stabilizedsepia"
	colour = "sepia"
	var/mod = 0

/datum/status_effect/stabilized/sepia/tick()
	if(prob(50) && mod > -1)
		mod--
		owner.add_movespeed_modifier(MOVESPEED_ID_SEPIA, override = TRUE, update=TRUE, priority=100, multiplicative_slowdown=-0.5, blacklisted_movetypes=(FLYING|FLOATING))
	else if(mod < 1)
		mod++
		// yeah a value of 0 does nothing but replacing the trait in place is cheaper than removing and adding repeatedly
		owner.add_movespeed_modifier(MOVESPEED_ID_SEPIA, override = TRUE, update=TRUE, priority=100, multiplicative_slowdown=0, blacklisted_movetypes=(FLYING|FLOATING))
	return ..()

/datum/status_effect/stabilized/sepia/on_remove()
	owner.remove_movespeed_modifier(MOVESPEED_ID_SEPIA)

/datum/status_effect/stabilized/cerulean
	id = "stabilizedcerulean"
	colour = "cerulean"
	var/mob/living/clone

/datum/status_effect/stabilized/cerulean/on_apply()
	var/typepath = owner.type
	clone = new typepath(owner.loc)
	var/mob/living/carbon/O = owner
	var/mob/living/carbon/C = clone
	if(istype(C) && istype(O))
		C.real_name = O.real_name
		O.dna.transfer_identity(C)
		C.updateappearance(mutcolor_update=1)
	return ..()

/datum/status_effect/stabilized/cerulean/tick()
	if(owner.stat == DEAD)
		if(clone && clone.stat != DEAD)
			owner.visible_message("<span class='warning'>[owner] blazes with brilliant light, [linked_extract] whisking [owner.p_their()] soul away.</span>",
				"<span class='notice'>You feel a warm glow from [linked_extract], and you open your eyes... elsewhere.</span>")
			if(owner.mind)
				owner.mind.transfer_to(clone)
			clone = null
			qdel(linked_extract)
		if(!clone || clone.stat == DEAD)
			to_chat(owner, "<span class='notice'>[linked_extract] desperately tries to move your soul to a living body, but can't find one!</span>")
			qdel(linked_extract)
	..()

/datum/status_effect/stabilized/cerulean/on_remove()
	if(clone)
		clone.visible_message("<span class='warning'>[clone] dissolves into a puddle of goo!</span>")
		clone.unequip_everything()
		qdel(clone)

/datum/status_effect/stabilized/pyrite
	id = "stabilizedpyrite"
	colour = "pyrite"
	var/originalcolor

/datum/status_effect/stabilized/pyrite/on_apply()
	originalcolor = owner.color
	return ..()

/datum/status_effect/stabilized/pyrite/tick()
	owner.color = rgb(rand(0,255),rand(0,255),rand(0,255))
	return ..()

/datum/status_effect/stabilized/pyrite/on_remove()
	owner.color = originalcolor

/datum/status_effect/stabilized/red
	id = "stabilizedred"
	colour = "red"

/datum/status_effect/stabilized/red/on_apply()
	owner.ignore_slowdown("slimestatus")
	return ..()

/datum/status_effect/stabilized/red/on_remove()
	owner.unignore_slowdown("slimestatus")

/datum/status_effect/stabilized/green
	id = "stabilizedgreen"
	colour = "green"
	var/datum/dna/originalDNA
	var/originalname

/datum/status_effect/stabilized/green/on_apply()
	to_chat(owner, "<span class='warning'>You feel different...</span>")
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		originalDNA = new H.dna.type
		originalname = H.real_name
		H.dna.copy_dna(originalDNA)
		randomize_human(H)
	return ..()

/datum/status_effect/stabilized/green/tick() //Only occasionally give examiners a warning.
	if(prob(50))
		examine_text = "<span class='warning'>SUBJECTPRONOUN looks a bit green and gooey...</span>"
	else
		examine_text = null
	return ..()

/datum/status_effect/stabilized/green/on_remove()
	to_chat(owner, "<span class='notice'>You feel more like yourself.</span>")
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		originalDNA.transfer_identity(H)
		H.real_name = originalname
		H.updateappearance(mutcolor_update=1)

/datum/status_effect/brokenpeace
	id = "brokenpeace"
	duration = 1200
	alert_type = null

/datum/status_effect/pinkdamagetracker
	id = "pinkdamagetracker"
	duration = -1
	alert_type = null
	var/damage = 0
	var/lasthealth

/datum/status_effect/pinkdamagetracker/tick()
	if((lasthealth - owner.health) > 0)
		damage += (lasthealth - owner.health)
	lasthealth = owner.health

/datum/status_effect/stabilized/pink
	id = "stabilizedpink"
	colour = "pink"
	var/list/mobs = list()
	var/faction_name

/datum/status_effect/stabilized/pink/on_apply()
	faction_name = owner.real_name
	return ..()

/datum/status_effect/stabilized/pink/tick()
	for(var/mob/living/simple_animal/M in view(7,get_turf(owner)))
		if(!(M in mobs))
			mobs += M
			M.apply_status_effect(/datum/status_effect/pinkdamagetracker)
			M.faction |= faction_name
	for(var/mob/living/simple_animal/M in mobs)
		if(!(M in view(7,get_turf(owner))))
			M.faction -= faction_name
			M.remove_status_effect(/datum/status_effect/pinkdamagetracker)
			mobs -= M
		var/datum/status_effect/pinkdamagetracker/C = M.has_status_effect(/datum/status_effect/pinkdamagetracker)
		if(istype(C) && C.damage > 0)
			C.damage = 0
			owner.apply_status_effect(/datum/status_effect/brokenpeace)
	var/HasFaction = FALSE
	for(var/i in owner.faction)
		if(i == faction_name)
			HasFaction = TRUE

	if(HasFaction && owner.has_status_effect(/datum/status_effect/brokenpeace))
		owner.faction -= faction_name
		to_chat(owner, "<span class='userdanger'>The peace has been broken! Hostile creatures will now react to you!</span>")
	if(!HasFaction && !owner.has_status_effect(/datum/status_effect/brokenpeace))
		to_chat(owner, "<span class='notice'>[linked_extract] pulses, generating a fragile aura of peace.</span>")
		owner.faction |= faction_name
	return ..()

/datum/status_effect/stabilized/pink/on_remove()
	for(var/mob/living/simple_animal/M in mobs)
		M.faction -= faction_name
		M.remove_status_effect(/datum/status_effect/pinkdamagetracker)
	for(var/i in owner.faction)
		if(i == faction_name)
			owner.faction -= faction_name

/datum/status_effect/stabilized/oil
	id = "stabilizedoil"
	colour = "oil"
	examine_text = "<span class='warning'>SUBJECTPRONOUN smells of sulfer and oil!</span>"

/datum/status_effect/stabilized/oil/tick()
	if(owner.stat == DEAD)
		explosion(get_turf(owner),1,2,4,flame_range = 5)
	return ..()

/datum/status_effect/stabilized/black
	id = "stabilizedblack"
	colour = "black"
	var/messagedelivered = FALSE
	var/heal_amount = 1

/datum/status_effect/stabilized/black/tick()
	if(owner.pulling && isliving(owner.pulling) && owner.grab_state == GRAB_KILL)
		var/mob/living/M = owner.pulling
		if(M.stat == DEAD)
			return
		if(!messagedelivered)
			to_chat(owner,"<span class='notice'>You feel your hands melt around [M]'s neck and start to drain [M.p_them()] of life.</span>")
			to_chat(owner.pulling, "<span class='userdanger'>[owner]'s hands melt around your neck, and you can feel your life starting to drain away!</span>")
			messagedelivered = TRUE
		examine_text = "<span class='warning'>SUBJECTPRONOUN is draining health from [owner.pulling]!</span>"
		var/list/healing_types = list()
		if(owner.getBruteLoss() > 0)
			healing_types += BRUTE
		if(owner.getFireLoss() > 0)
			healing_types += BURN
		if(owner.getToxLoss() > 0)
			healing_types += TOX
		if(owner.getCloneLoss() > 0)
			healing_types += CLONE

		owner.apply_damage_type(-heal_amount, damagetype=pick(healing_types))
		owner.adjust_nutrition(3)
		M.adjustCloneLoss(heal_amount * 1.2) //This way, two people can't just convert each other's damage away.
	else
		messagedelivered = FALSE
		examine_text = null
	return ..()

/datum/status_effect/stabilized/lightpink
	id = "stabilizedlightpink"
	colour = "light pink"

/datum/status_effect/stabilized/lightpink/on_apply()
	owner.add_movespeed_modifier(MOVESPEED_ID_SLIME_STATUS, update=TRUE, priority=100, multiplicative_slowdown=-0.5, blacklisted_movetypes=(FLYING|FLOATING))
	return ..()

/datum/status_effect/stabilized/lightpink/tick()
	for(var/mob/living/carbon/human/H in range(1, get_turf(owner)))
		if(H != owner && H.stat != DEAD && H.health <= 0 && !H.reagents.has_reagent(/datum/reagent/medicine/epinephrine))
			to_chat(owner, "[linked_extract] pulses in sync with [H]'s heartbeat, trying to keep [H.p_them()] alive.")
			H.reagents.add_reagent(/datum/reagent/medicine/epinephrine,5)
	return ..()

/datum/status_effect/stabilized/lightpink/on_remove()
	owner.remove_movespeed_modifier(MOVESPEED_ID_SLIME_STATUS)

/datum/status_effect/stabilized/adamantine
	id = "stabilizedadamantine"
	colour = "adamantine"
	examine_text = "<span class='warning'>SUBJECTPRONOUN has a strange metallic coating on their skin.</span>"

/datum/status_effect/stabilized/gold
	id = "stabilizedgold"
	colour = "gold"
	var/mob/living/simple_animal/familiar

/datum/status_effect/stabilized/gold/tick()
	var/obj/item/slimecross/stabilized/gold/linked = linked_extract
	if(QDELETED(familiar))
		familiar = new linked.mob_type(get_turf(owner.loc))
		familiar.name = linked.mob_name
		familiar.del_on_death = TRUE
		familiar.copy_languages(owner, LANGUAGE_MASTER)
		if(linked.saved_mind)
			linked.saved_mind.transfer_to(familiar)
			familiar.update_atom_languages()
			familiar.ckey = linked.saved_mind.key
	else
		if(familiar.mind)
			linked.saved_mind = familiar.mind
	return ..()

/datum/status_effect/stabilized/gold/on_remove()
	if(familiar)
		qdel(familiar)

/datum/status_effect/stabilized/adamantine/on_apply()
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		H.physiology.damage_resistance += 5
	return ..()

/datum/status_effect/stabilized/adamantine/on_remove()
	if(ishuman(owner))
		var/mob/living/carbon/human/H = owner
		H.physiology.damage_resistance -= 5

/datum/status_effect/stabilized/rainbow
	id = "stabilizedrainbow"
	colour = "rainbow"

/datum/status_effect/stabilized/rainbow/tick()
	if(owner.health <= 0)
		var/obj/item/slimecross/stabilized/rainbow/X = linked_extract
		if(istype(X))
			if(X.regencore)
				X.regencore.afterattack(owner,owner,TRUE)
				X.regencore = null
				owner.visible_message("<span class='warning'>[owner] flashes a rainbow of colors, and [owner.p_their()] skin is coated in a milky regenerative goo!</span>")
				qdel(src)
				qdel(linked_extract)
	return ..()
