/obj/machinery/pinpointer_dispenser
	name = "wayfinding pinpointer synthesizer"
	icon = 'icons/obj/machines/wayfinding.dmi'
	icon_state = "pinpointersynth"
	desc = "Having trouble finding your way? This machine synthesizes pinpointers that point to common locations."
	density = FALSE
	layer = HIGH_OBJ_LAYER
	var/list/user_spawn_cooldowns = list()
	var/list/user_interact_cooldowns = list()
	var/spawn_cooldown = 5 MINUTES //time per person to spawn another pinpointer
	var/interact_cooldown = 20 SECONDS //time per person for subsequent interactions
	var/start_bal = 200 //how much money it starts with to cover wayfinder refunds
	var/refund_amt = 40 //how much money recycling a pinpointer rewards you
	var/datum/bank_account/synth_acc = new /datum/bank_account/remote
	var/ppt_cost = 65 //Jan 6 '20: Assistant can buy one roundstart (125 cr starting)
	var/expression_timer

/obj/machinery/pinpointer_dispenser/Initialize(mapload)
	..()
	var/datum/bank_account/civ_acc = SSeconomy.get_dep_account(ACCOUNT_CIV)
	if(civ_acc)
		synth_acc.transfer_money(civ_acc, start_bal) //float has to come from somewhere, right?

	synth_acc.account_holder = name

	desc += " Only [ppt_cost] credits! It also likes making costumes..."

	set_expression("neutral")

/obj/machinery/pinpointer_dispenser/attack_hand(mob/living/carbon/user)
	if(world.time < user_interact_cooldowns[user.real_name])
		to_chat(user, "<span class='warning'>It doesn't respond.</span>")
		return

	user_interact_cooldowns[user.real_name] = world.time + interact_cooldown

	for(var/obj/item/pinpointer/wayfinding/WP in user.GetAllContents())
		set_expression("unsure", 2 SECONDS)
		say("<span class='robot'>I can detect the pinpointer on you, [user.first_name()].</span>")
		user_spawn_cooldowns[user.real_name] = world.time + spawn_cooldown //spawn timer resets for trickers
		return

	var/msg
	var/dispense = TRUE
	var/obj/item/pinpointer/wayfinding/pointat
	for(var/obj/item/pinpointer/wayfinding/WP in range(7, user))
		if(WP.Adjacent(user))
			set_expression("facepalm", 2 SECONDS)
			say("<span class='robot'>[WP.owner == user.real_name ? "Your" : "A"] pinpointer is right there.</span>")
			pointat(WP)
			user_spawn_cooldowns[user.real_name] = world.time + spawn_cooldown
			return
		else if(WP in oview(7, user))
			pointat = WP
			break

	if(world.time < user_spawn_cooldowns[user.real_name])
		var/secsleft = (user_spawn_cooldowns[user.real_name] - world.time) / 10
		msg += "to wait another [secsleft/60 > 1 ? "[round(secsleft/60,1)] minute\s" : "[round(secsleft)] second\s"]"
		dispense = FALSE

	var/datum/bank_account/cust_acc = null
	if(ishuman(user))
		var/mob/living/carbon/human/H = user
		if(H.get_bank_account())
			cust_acc = H.get_bank_account()

	if(cust_acc)
		if(!cust_acc.has_money(ppt_cost))
			msg += "[!msg ? "to find [ppt_cost-cust_acc.account_balance] more credit\s" : " and find [ppt_cost-cust_acc.account_balance] more credit\s"]"
			dispense = FALSE

	if(!dispense)
		set_expression("sad", 2 SECONDS)
		if(pointat)
			msg += ". I suggest you get [pointat.owner == user.real_name ? "your" : "that"] pinpointer over there instead"
			pointat(pointat)
		say("<span class='robot'>You will need [msg], [user.first_name()].</span>")
		return

	if(synth_acc.transfer_money(cust_acc, ppt_cost))
		set_expression("veryhappy", 2 SECONDS)
		say("<span class='robot'>That is [ppt_cost] credits. Here is your pinpointer.</span>")
		var/obj/item/pinpointer/wayfinding/P = new /obj/item/pinpointer/wayfinding(get_turf(src))
		user_spawn_cooldowns[user.real_name] = world.time + spawn_cooldown
		user.put_in_hands(P)
		P.owner = user.real_name

/obj/machinery/pinpointer_dispenser/attackby(obj/item/I, mob/user, params)
	if(istype(I, /obj/item/pinpointer/wayfinding))
		var/obj/item/pinpointer/wayfinding/WP = I
		to_chat(user, "<span class='notice'>You put \the [WP] in the return slot.</span>")
		var/rfnd_amt
		if((!WP.roundstart || WP.owner != user.real_name) && synth_acc.has_money(TRUE)) //can't recycle own pinpointer for money if not bought; given by a neutral quirk
			if(synth_acc.has_money(refund_amt))
				rfnd_amt = refund_amt
			else
				rfnd_amt = synth_acc.account_balance
			synth_acc._adjust_money(-rfnd_amt)
			var/obj/item/holochip/HC = new /obj/item/holochip(user.loc)
			HC.credits = rfnd_amt
			HC.name = "[HC.credits] credit holochip"
			if(istype(user, /mob/living/carbon/human))
				var/mob/living/carbon/human/H = user
				H.put_in_hands(HC)
		else
			var/crap = pick(subtypesof(/obj/effect/spawner/bundle/costume)) //harmless garbage some people may appreciate
			new crap(user.loc)
		qdel(WP)
		set_expression("happy", 2 SECONDS)
		say("<span class='robot'>Thank you for recycling, [user.first_name()]! Here is [rfnd_amt ? "[rfnd_amt] credits." : "a freshly synthesized costume!"]</span>")

/obj/machinery/pinpointer_dispenser/proc/set_expression(type, duration)
	cut_overlays()
	deltimer(expression_timer)
	add_overlay(type)
	if(duration)
		expression_timer = addtimer(CALLBACK(src, .proc/set_expression, "neutral"), duration, TIMER_STOPPABLE)

/obj/machinery/pinpointer_dispenser/proc/pointat(atom)
	visible_message("<span class='name'>[src]</span> points at [atom].")
	new /obj/effect/temp_visual/point(atom,invisibility)

//Pinpointer itself
/obj/item/pinpointer/wayfinding //Help players new to a station find their way around
	name = "wayfinding pinpointer"
	desc = "A handheld tracking device that points to useful places."
	icon_state = "pinpointer_way"
	resistance_flags = NONE
	var/owner = null
	var/list/beacons = list()
	var/roundstart = FALSE

/obj/item/pinpointer/wayfinding/attack_self(mob/living/user)
	if(active)
		toggle_on()
		to_chat(user, "<span class='notice'>You deactivate your pinpointer.</span>")
		return

	if (!owner)
		owner = user.real_name

	if(beacons.len)
		beacons.Cut()
	for(var/obj/machinery/navbeacon/B in GLOB.wayfindingbeacons)
		beacons[B.codes["wayfinding"]] = B

	if(!beacons.len)
		to_chat(user, "<span class='notice'>Your pinpointer fails to detect a signal.</span>")
		return

	var/A = input(user, "", "Pinpoint") as null|anything in sortList(beacons)
	if(!A || QDELETED(src) || !user || !user.is_holding(src) || user.incapacitated())
		return

	target = beacons[A]
	toggle_on()
	to_chat(user, "<span class='notice'>You activate your pinpointer.</span>")

/obj/item/pinpointer/wayfinding/examine(mob/user)
	. = ..()
	var/msg = "Its tracking indicator reads "
	if(target)
		var/obj/machinery/navbeacon/wayfinding/B  = target
		msg += "\"[B.codes["wayfinding"]]\"."
	else
		msg = "Its tracking indicator is blank."
	if(owner)
		msg += " It belongs to [owner]."
	. += msg

/obj/item/pinpointer/wayfinding/scan_for_target()
	if(!target) //target can be set to null from above code, or elsewhere
		active = FALSE

//Navbeacon that initialises with wayfinding codes
/obj/machinery/navbeacon/wayfinding
	wayfinding = TRUE
