//Healer
/mob/living/simple_animal/hostile/guardian/healer
	a_intent = INTENT_HARM
	friendly_verb_continuous = "heals"
	friendly_verb_simple = "heal"
	speed = 0
	damage_coeff = list(BRUTE = 0.7, BURN = 0.7, TOX = 0.7, CLONE = 0.7, STAMINA = 0, OXY = 0.7)
	melee_damage_lower = 15
	melee_damage_upper = 15
	playstyle_string = "<span class='holoparasite'>As a <b>support</b> type, you may toggle your basic attacks to a healing mode. In addition, Alt-Clicking on an adjacent object or mob will warp them to your bluespace beacon after a short delay.</span>"
	magic_fluff_string = "<span class='holoparasite'>..And draw the CMO, a potent force of life... and death.</span>"
	carp_fluff_string = "<span class='holoparasite'>CARP CARP CARP! You caught a support carp. It's a kleptocarp!</span>"
	tech_fluff_string = "<span class='holoparasite'>Boot sequence complete. Support modules active. Holoparasite swarm online.</span>"
	toggle_button_type = /obj/screen/guardian/ToggleMode
	var/obj/structure/receiving_pad/beacon
	var/beacon_cooldown = 0
	var/toggle = FALSE

/mob/living/simple_animal/hostile/guardian/healer/Initialize()
	. = ..()
	var/datum/atom_hud/medsensor = GLOB.huds[DATA_HUD_MEDICAL_ADVANCED]
	medsensor.add_hud_to(src)

/mob/living/simple_animal/hostile/guardian/healer/Stat()
	..()
	if(statpanel("Status"))
		if(beacon_cooldown >= world.time)
			stat(null, "Beacon Cooldown Remaining: [DisplayTimeText(beacon_cooldown - world.time)]")

/mob/living/simple_animal/hostile/guardian/healer/AttackingTarget()
	. = ..()
	if(is_deployed() && toggle && iscarbon(target))
		var/mob/living/carbon/C = target
		C.adjustBruteLoss(-5)
		C.adjustFireLoss(-5)
		C.adjustOxyLoss(-5)
		C.adjustToxLoss(-5)
		var/obj/effect/temp_visual/heal/H = new /obj/effect/temp_visual/heal(get_turf(C))
		if(namedatum)
			H.color = namedatum.colour
		if(C == summoner)
			update_health_hud()
			med_hud_set_health()
			med_hud_set_status()

/mob/living/simple_animal/hostile/guardian/healer/ToggleMode()
	if(src.loc == summoner)
		if(toggle)
			a_intent = INTENT_HARM
			speed = 0
			damage_coeff = list(BRUTE = 0.7, BURN = 0.7, TOX = 0.7, CLONE = 0.7, STAMINA = 0, OXY = 0.7)
			melee_damage_lower = 15
			melee_damage_upper = 15
			to_chat(src, "<span class='danger'><B>You switch to combat mode.</span></B>")
			toggle = FALSE
		else
			a_intent = INTENT_HELP
			speed = 1
			damage_coeff = list(BRUTE = 1, BURN = 1, TOX = 1, CLONE = 1, STAMINA = 0, OXY = 1)
			melee_damage_lower = 0
			melee_damage_upper = 0
			to_chat(src, "<span class='danger'><B>You switch to healing mode.</span></B>")
			toggle = TRUE
	else
		to_chat(src, "<span class='danger'><B>You have to be recalled to toggle modes!</span></B>")


/mob/living/simple_animal/hostile/guardian/healer/verb/Beacon()
	set name = "Place Bluespace Beacon"
	set category = "Guardian"
	set desc = "Mark a floor as your beacon point, allowing you to warp targets to it. Your beacon will not work at extreme distances."

	if(beacon_cooldown >= world.time)
		to_chat(src, "<span class='danger'><B>Your power is on cooldown. You must wait five minutes between placing beacons.</span></B>")
		return

	var/turf/beacon_loc = get_turf(src.loc)
	if(!isfloorturf(beacon_loc))
		return

	if(beacon)
		beacon.disappear()
		beacon = null

	beacon = new(beacon_loc, src)

	to_chat(src, "<span class='danger'><B>Beacon placed! You may now warp targets and objects to it, including your user, via Alt+Click.</span></B>")

	beacon_cooldown = world.time + 3000

/obj/structure/receiving_pad
	name = "bluespace receiving pad"
	icon = 'icons/turf/floors.dmi'
	desc = "A receiving zone for bluespace teleportations."
	icon_state = "light_on-w"
	light_range = MINIMUM_USEFUL_LIGHT_RANGE
	density = FALSE
	anchored = TRUE
	layer = ABOVE_OPEN_TURF_LAYER

/obj/structure/receiving_pad/New(loc, mob/living/simple_animal/hostile/guardian/healer/G)
	. = ..()
	if(G.namedatum)
		add_atom_colour(G.namedatum.colour, FIXED_COLOUR_PRIORITY)

/obj/structure/receiving_pad/proc/disappear()
	visible_message("<span class='notice'>[src] vanishes!</span>")
	qdel(src)

/mob/living/simple_animal/hostile/guardian/healer/AltClickOn(atom/movable/A)
	if(!istype(A))
		return
	if(src.loc == summoner)
		to_chat(src, "<span class='danger'><B>You must be manifested to warp a target!</span></B>")
		return
	if(!beacon)
		to_chat(src, "<span class='danger'><B>You need a beacon placed to warp things!</span></B>")
		return
	if(!Adjacent(A))
		to_chat(src, "<span class='danger'><B>You must be adjacent to your target!</span></B>")
		return
	if(A.anchored)
		to_chat(src, "<span class='danger'><B>Your target cannot be anchored!</span></B>")
		return

	var/turf/T = get_turf(A)
	if(beacon.z != T.z)
		to_chat(src, "<span class='danger'><B>The beacon is too far away to warp to!</span></B>")
		return

	to_chat(src, "<span class='danger'><B>You begin to warp [A].</span></B>")
	A.visible_message("<span class='danger'>[A] starts to glow faintly!</span>", \
	"<span class='userdanger'>You start to faintly glow, and you feel strangely weightless!</span>")
	do_attack_animation(A)

	if(!do_mob(src, A, 60)) //now start the channel
		to_chat(src, "<span class='danger'><B>You need to hold still!</span></B>")
		return

	new /obj/effect/temp_visual/guardian/phase/out(T)
	if(isliving(A))
		var/mob/living/L = A
		L.flash_act()
	A.visible_message("<span class='danger'>[A] disappears in a flash of light!</span>", \
	"<span class='userdanger'>Your vision is obscured by a flash of light!</span>")
	do_teleport(A, beacon, 0, channel = TELEPORT_CHANNEL_BLUESPACE)
	new /obj/effect/temp_visual/guardian/phase(get_turf(A))
