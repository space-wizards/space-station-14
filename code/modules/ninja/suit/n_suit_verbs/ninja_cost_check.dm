

//Cost function for suit Procs/Verbs/Abilities
/obj/item/clothing/suit/space/space_ninja/proc/ninjacost(cost = 0, specificCheck = 0)
	var/mob/living/carbon/human/H = affecting
	var/actualCost = cost*10
	if(cost && cell.charge < actualCost)
		to_chat(H, "<span class='warning'>Not enough energy!</span>")
		return 1
	else
		//This shit used to be handled individually on every proc.. why even bother with a universal check proc then?
		cell.charge-=(actualCost)

	switch(specificCheck)
		if(N_STEALTH_CANCEL)
			cancel_stealth()//Get rid of it.
		if(N_SMOKE_BOMB)
			if(!s_bombs)
				to_chat(H, "<span class='warning'>There are no more smoke bombs remaining!</span>")
				return 1
		if(N_ADRENALINE)
			if(!a_boost)
				to_chat(H, "<span class='warning'>You do not have any more adrenaline boosters!</span>")
				return 1
	return (s_coold)//Returns the value of the variable which counts down to zero.
