/*NOTES:
These are general powers. Specific powers are stored under the appropriate alien creature type.
*/

/*Alien spit now works like a taser shot. It won't home in on the target but will act the same once it does hit.
Doesn't work on other aliens/AI.*/


/obj/effect/proc_holder/alien
	name = "Alien Power"
	panel = "Alien"
	var/plasma_cost = 0
	var/check_turf = FALSE
	has_action = TRUE
	base_action = /datum/action/spell_action/alien
	action_icon = 'icons/mob/actions/actions_xeno.dmi'
	action_icon_state = "spell_default"
	action_background_icon_state = "bg_alien"

/obj/effect/proc_holder/alien/Initialize()
	. = ..()
	action = new(src)

/obj/effect/proc_holder/alien/Click()
	if(!iscarbon(usr))
		return 1
	var/mob/living/carbon/user = usr
	if(cost_check(check_turf,user))
		if(fire(user) && user) // Second check to prevent runtimes when evolving
			user.adjustPlasma(-plasma_cost)
	return 1

/obj/effect/proc_holder/alien/on_gain(mob/living/carbon/user)
	return

/obj/effect/proc_holder/alien/on_lose(mob/living/carbon/user)
	return

/obj/effect/proc_holder/alien/fire(mob/living/carbon/user)
	return 1

/obj/effect/proc_holder/alien/get_panel_text()
	. = ..()
	if(plasma_cost > 0)
		return "[plasma_cost]"

/obj/effect/proc_holder/alien/proc/cost_check(check_turf = FALSE, mob/living/carbon/user, silent = FALSE)
	if(user.stat)
		if(!silent)
			to_chat(user, "<span class='noticealien'>You must be conscious to do this.</span>")
		return FALSE
	if(user.getPlasma() < plasma_cost)
		if(!silent)
			to_chat(user, "<span class='noticealien'>Not enough plasma stored.</span>")
		return FALSE
	if(check_turf && (!isturf(user.loc) || isspaceturf(user.loc)))
		if(!silent)
			to_chat(user, "<span class='noticealien'>Bad place for a garden!</span>")
		return FALSE
	return TRUE

/obj/effect/proc_holder/alien/proc/check_vent_block(mob/living/user)
	var/obj/machinery/atmospherics/components/unary/atmos_thing = locate() in user.loc
	if(atmos_thing)
		var/rusure = alert(user, "Laying eggs and shaping resin here would block access to [atmos_thing]. Do you want to continue?", "Blocking Atmospheric Component", "Yes", "No")
		if(rusure != "Yes")
			return FALSE
	return TRUE

/obj/effect/proc_holder/alien/plant
	name = "Plant Weeds"
	desc = "Plants some alien weeds."
	plasma_cost = 50
	check_turf = TRUE
	action_icon_state = "alien_plant"

/obj/effect/proc_holder/alien/plant/fire(mob/living/carbon/user)
	if(locate(/obj/structure/alien/weeds/node) in get_turf(user))
		to_chat(user, "<span class='warning'>There's already a weed node here!</span>")
		return 0
	user.visible_message("<span class='alertalien'>[user] has planted some alien weeds!</span>")
	new/obj/structure/alien/weeds/node(user.loc)
	return 1

/obj/effect/proc_holder/alien/whisper
	name = "Whisper"
	desc = "Whisper to someone."
	plasma_cost = 10
	action_icon_state = "alien_whisper"

/obj/effect/proc_holder/alien/whisper/fire(mob/living/carbon/user)
	var/list/options = list()
	for(var/mob/living/Ms in oview(user))
		options += Ms
	var/mob/living/M = input("Select who to whisper to:","Whisper to?",null) as null|mob in sortNames(options)
	if(!M)
		return 0
	if(M.anti_magic_check(FALSE, FALSE, TRUE, 0))
		to_chat(user, "<span class='noticealien'>As you try to communicate with [M], you're suddenly stopped by a vision of a massive tinfoil wall that streches beyond visible range. It seems you've been foiled.</span>")
		return FALSE
	var/msg = sanitize(input("Message:", "Alien Whisper") as text|null)
	if(msg)
		if(M.anti_magic_check(FALSE, FALSE, TRUE, 0))
			to_chat(user, "<span class='notice'>As you try to communicate with [M], you're suddenly stopped by a vision of a massive tinfoil wall that streches beyond visible range. It seems you've been foiled.</span>")
			return
		log_directed_talk(user, M, msg, LOG_SAY, tag="alien whisper")
		to_chat(M, "<span class='noticealien'>You hear a strange, alien voice in your head...</span>[msg]")
		to_chat(user, "<span class='noticealien'>You said: \"[msg]\" to [M]</span>")
		for(var/ded in GLOB.dead_mob_list)
			if(!isobserver(ded))
				continue
			var/follow_link_user = FOLLOW_LINK(ded, user)
			var/follow_link_whispee = FOLLOW_LINK(ded, M)
			to_chat(ded, "[follow_link_user] <span class='name'>[user]</span> <span class='alertalien'>Alien Whisper --> </span> [follow_link_whispee] <span class='name'>[M]</span> <span class='noticealien'>[msg]</span>")
	else
		return 0
	return 1

/obj/effect/proc_holder/alien/transfer
	name = "Transfer Plasma"
	desc = "Transfer Plasma to another alien."
	plasma_cost = 0
	action_icon_state = "alien_transfer"

/obj/effect/proc_holder/alien/transfer/fire(mob/living/carbon/user)
	var/list/mob/living/carbon/aliens_around = list()
	for(var/mob/living/carbon/A  in oview(user))
		if(A.getorgan(/obj/item/organ/alien/plasmavessel))
			aliens_around.Add(A)
	var/mob/living/carbon/M = input("Select who to transfer to:","Transfer plasma to?",null) as mob in sortNames(aliens_around)
	if(!M)
		return 0
	var/amount = input("Amount:", "Transfer Plasma to [M]") as num|null
	if (amount)
		amount = min(abs(round(amount)), user.getPlasma())
		if (get_dist(user,M) <= 1)
			M.adjustPlasma(amount)
			user.adjustPlasma(-amount)
			to_chat(M, "<span class='noticealien'>[user] has transferred [amount] plasma to you.</span>")
			to_chat(user, "<span class='noticealien'>You transfer [amount] plasma to [M].</span>")
		else
			to_chat(user, "<span class='noticealien'>You need to be closer!</span>")
	return

/obj/effect/proc_holder/alien/acid
	name = "Corrosive Acid"
	desc = "Drench an object in acid, destroying it over time."
	plasma_cost = 200
	action_icon_state = "alien_acid"

/obj/effect/proc_holder/alien/acid/on_gain(mob/living/carbon/user)
	user.verbs.Add(/mob/living/carbon/proc/corrosive_acid)

/obj/effect/proc_holder/alien/acid/on_lose(mob/living/carbon/user)
	user.verbs.Remove(/mob/living/carbon/proc/corrosive_acid)

/obj/effect/proc_holder/alien/acid/proc/corrode(atom/target,mob/living/carbon/user = usr)
	if(target in oview(1,user))
		if(target.acid_act(200, 100))
			user.visible_message("<span class='alertalien'>[user] vomits globs of vile stuff all over [target]. It begins to sizzle and melt under the bubbling mess of acid!</span>")
			return 1
		else
			to_chat(user, "<span class='noticealien'>You cannot dissolve this object.</span>")


			return 0
	else
		to_chat(src, "<span class='noticealien'>[target] is too far away.</span>")
		return 0


/obj/effect/proc_holder/alien/acid/fire(mob/living/carbon/alien/user)
	var/O = input("Select what to dissolve:","Dissolve",null) as obj|turf in oview(1,user)
	if(!O || user.incapacitated())
		return 0
	else
		return corrode(O,user)

/mob/living/carbon/proc/corrosive_acid(O as obj|turf in oview(1)) // right click menu verb ugh
	set name = "Corrosive Acid"

	if(!iscarbon(usr))
		return
	var/mob/living/carbon/user = usr
	var/obj/effect/proc_holder/alien/acid/A = locate() in user.abilities
	if(!A)
		return
	if(user.getPlasma() > A.plasma_cost && A.corrode(O))
		user.adjustPlasma(-A.plasma_cost)

/obj/effect/proc_holder/alien/neurotoxin
	name = "Spit Neurotoxin"
	desc = "Spits neurotoxin at someone, paralyzing them for a short time."
	action_icon_state = "alien_neurotoxin_0"
	active = FALSE

/obj/effect/proc_holder/alien/neurotoxin/fire(mob/living/carbon/user)
	var/message
	if(active)
		message = "<span class='notice'>You empty your neurotoxin gland.</span>"
		remove_ranged_ability(message)
	else
		message = "<span class='notice'>You prepare your neurotoxin gland. <B>Left-click to fire at a target!</B></span>"
		add_ranged_ability(user, message, TRUE)

/obj/effect/proc_holder/alien/neurotoxin/update_icon()
	action.button_icon_state = "alien_neurotoxin_[active]"
	action.UpdateButtonIcon()

/obj/effect/proc_holder/alien/neurotoxin/InterceptClickOn(mob/living/caller, params, atom/target)
	if(..())
		return
	var/p_cost = 50
	if(!iscarbon(ranged_ability_user) || ranged_ability_user.stat)
		remove_ranged_ability()
		return

	var/mob/living/carbon/user = ranged_ability_user

	if(user.getPlasma() < p_cost)
		to_chat(user, "<span class='warning'>You need at least [p_cost] plasma to spit.</span>")
		remove_ranged_ability()
		return

	var/turf/T = user.loc
	var/turf/U = get_step(user, user.dir) // Get the tile infront of the move, based on their direction
	if(!isturf(U) || !isturf(T))
		return FALSE

	user.visible_message("<span class='danger'>[user] spits neurotoxin!</span>", "<span class='alertalien'>You spit neurotoxin.</span>")
	var/obj/projectile/bullet/neurotoxin/A = new /obj/projectile/bullet/neurotoxin(user.loc)
	A.preparePixelProjectile(target, user, params)
	A.fire()
	user.newtonian_move(get_dir(U, T))
	user.adjustPlasma(-p_cost)

	return TRUE

/obj/effect/proc_holder/alien/neurotoxin/on_lose(mob/living/carbon/user)
	remove_ranged_ability()

/obj/effect/proc_holder/alien/neurotoxin/add_ranged_ability(mob/living/user,msg,forced)
	..()
	if(isalienadult(user))
		var/mob/living/carbon/alien/humanoid/A = user
		A.drooling = 1
		A.update_icons()

/obj/effect/proc_holder/alien/neurotoxin/remove_ranged_ability(msg)
	if(isalienadult(ranged_ability_user))
		var/mob/living/carbon/alien/humanoid/A = ranged_ability_user
		A.drooling = 0
		A.update_icons()
	..()

/obj/effect/proc_holder/alien/resin
	name = "Secrete Resin"
	desc = "Secrete tough malleable resin."
	plasma_cost = 55
	check_turf = TRUE
	var/list/structures = list(
		"resin wall" = /obj/structure/alien/resin/wall,
		"resin membrane" = /obj/structure/alien/resin/membrane,
		"resin nest" = /obj/structure/bed/nest)

	action_icon_state = "alien_resin"

/obj/effect/proc_holder/alien/resin/fire(mob/living/carbon/user)
	if(locate(/obj/structure/alien/resin) in user.loc)
		to_chat(user, "<span class='warning'>There is already a resin structure there!</span>")
		return FALSE

	if(!check_vent_block(user))
		return FALSE

	var/choice = input("Choose what you wish to shape.","Resin building") as null|anything in structures
	if(!choice)
		return FALSE
	if (!cost_check(check_turf,user))
		return FALSE
	to_chat(user, "<span class='notice'>You shape a [choice].</span>")
	user.visible_message("<span class='notice'>[user] vomits up a thick purple substance and begins to shape it.</span>")

	choice = structures[choice]
	new choice(user.loc)
	return TRUE

/obj/effect/proc_holder/alien/sneak
	name = "Sneak"
	desc = "Blend into the shadows to stalk your prey."
	active = 0

	action_icon_state = "alien_sneak"

/obj/effect/proc_holder/alien/sneak/fire(mob/living/carbon/alien/humanoid/user)
	if(!active)
		user.alpha = 75 //Still easy to see in lit areas with bright tiles, almost invisible on resin.
		user.sneaking = 1
		active = 1
		to_chat(user, "<span class='noticealien'>You blend into the shadows...</span>")
	else
		user.alpha = initial(user.alpha)
		user.sneaking = 0
		active = 0
		to_chat(user, "<span class='noticealien'>You reveal yourself!</span>")


/mob/living/carbon/proc/getPlasma()
	var/obj/item/organ/alien/plasmavessel/vessel = getorgan(/obj/item/organ/alien/plasmavessel)
	if(!vessel)
		return 0
	return vessel.storedPlasma


/mob/living/carbon/proc/adjustPlasma(amount)
	var/obj/item/organ/alien/plasmavessel/vessel = getorgan(/obj/item/organ/alien/plasmavessel)
	if(!vessel)
		return 0
	vessel.storedPlasma = max(vessel.storedPlasma + amount,0)
	vessel.storedPlasma = min(vessel.storedPlasma, vessel.max_plasma) //upper limit of max_plasma, lower limit of 0
	for(var/X in abilities)
		var/obj/effect/proc_holder/alien/APH = X
		if(APH.has_action)
			APH.action.UpdateButtonIcon()
	return 1

/mob/living/carbon/alien/adjustPlasma(amount)
	. = ..()
	updatePlasmaDisplay()

/mob/living/carbon/proc/usePlasma(amount)
	if(getPlasma() >= amount)
		adjustPlasma(-amount)
		return 1

	return 0
