/datum/antagonist/blood_contract
	name = "Blood Contract Target"
	show_in_roundend = FALSE
	show_in_antagpanel = FALSE
	show_name_in_check_antagonists = TRUE
	var/duration = 2 MINUTES

/datum/antagonist/blood_contract/on_gain()
	. = ..()
	give_objective()
	start_the_hunt()

/datum/antagonist/blood_contract/proc/give_objective()
	var/datum/objective/survive/survive = new
	survive.owner = owner
	objectives += survive

/datum/antagonist/blood_contract/greet()
	. = ..()
	to_chat(owner, "<span class='userdanger'>You've been marked for death! Don't let the demons get you! KILL THEM ALL!</span>")

/datum/antagonist/blood_contract/proc/start_the_hunt()
	var/mob/living/carbon/human/H = owner.current
	if(!istype(H))
		return

	H.add_atom_colour("#FF0000", ADMIN_COLOUR_PRIORITY)

	var/obj/effect/mine/pickup/bloodbath/B = new(H)
	B.duration = duration

	INVOKE_ASYNC(B, /obj/effect/mine/pickup/bloodbath/.proc/mineEffect, H) //could use moving out from the mine

	for(var/mob/living/carbon/human/P in GLOB.player_list)
		if(P == H)
			continue
		to_chat(P, "<span class='userdanger'>You have an overwhelming desire to kill [H]. [H.p_theyve(TRUE)] been marked red! Whoever [H.p_they()] [H.p_were()], friend or foe, go kill [H.p_them()]!</span>")

		var/obj/item/I = new /obj/item/kitchen/knife/butcher(get_turf(P))
		P.put_in_hands(I, del_on_fail=TRUE)
		QDEL_IN(I, duration)
