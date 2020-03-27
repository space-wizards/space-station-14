/datum/action/changeling/headcrab
	name = "Last Resort"
	desc = "We sacrifice our current body in a moment of need, placing us in control of a vessel that can plant our likeness in a new host. Costs 20 chemicals."
	helptext = "We will be placed in control of a small, fragile creature. We may attack a corpse like this to plant an egg which will slowly mature into a new form for us."
	button_icon_state = "last_resort"
	chemical_cost = 20
	dna_cost = 1
	req_human = 1

/datum/action/changeling/headcrab/sting_action(mob/user)
	set waitfor = FALSE
	if(alert("Are we sure we wish to kill ourself and create a headslug?",,"Yes", "No") == "No")
		return
	..()
	var/datum/mind/M = user.mind
	var/list/organs = user.getorganszone(BODY_ZONE_HEAD, 1)

	for(var/obj/item/organ/I in organs)
		I.Remove(user, 1)

	explosion(get_turf(user), 0, 0, 2, 0, TRUE)
	for(var/mob/living/carbon/human/H in range(2,user))
		var/obj/item/organ/eyes/eyes = H.getorganslot(ORGAN_SLOT_EYES)
		to_chat(H, "<span class='userdanger'>You are blinded by a shower of blood!</span>")
		H.Stun(20)
		H.blur_eyes(20)
		eyes?.applyOrganDamage(5)
		H.confused += 3
	for(var/mob/living/silicon/S in range(2,user))
		to_chat(S, "<span class='userdanger'>Your sensors are disabled by a shower of blood!</span>")
		S.Paralyze(60)
	var/turf = get_turf(user)
	user.gib()
	. = TRUE
	sleep(5) // So it's not killed in explosion
	var/mob/living/simple_animal/hostile/headcrab/crab = new(turf)
	for(var/obj/item/organ/I in organs)
		I.forceMove(crab)
	crab.origin = M
	if(crab.origin)
		crab.origin.active = 1
		crab.origin.transfer_to(crab)
		to_chat(crab, "<span class='warning'>You burst out of the remains of your former body in a shower of gore!</span>")
