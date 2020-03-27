
/*

Contents:
- Stealth Verbs

*/


/obj/item/clothing/suit/space/space_ninja/proc/toggle_stealth()
	var/mob/living/carbon/human/U = affecting
	if(!U)
		return
	if(stealth)
		cancel_stealth()
	else
		if(cell.charge <= 0)
			to_chat(U, "<span class='warning'>You don't have enough power to enable Stealth!</span>")
			return
		stealth = !stealth
		animate(U, alpha = 50,time = 15)
		U.visible_message("<span class='warning'>[U.name] vanishes into thin air!</span>", \
						"<span class='notice'>You are now mostly invisible to normal detection.</span>")


/obj/item/clothing/suit/space/space_ninja/proc/cancel_stealth()
	var/mob/living/carbon/human/U = affecting
	if(!U)
		return 0
	if(stealth)
		stealth = !stealth
		animate(U, alpha = 255, time = 15)
		U.visible_message("<span class='warning'>[U.name] appears from thin air!</span>", \
						"<span class='notice'>You are now visible.</span>")
		return 1
	return 0


/obj/item/clothing/suit/space/space_ninja/proc/stealth()
	if(!s_busy)
		toggle_stealth()
	else
		to_chat(affecting, "<span class='danger'>Stealth does not appear to work!</span>")
