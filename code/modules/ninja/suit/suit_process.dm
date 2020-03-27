/obj/item/clothing/suit/space/space_ninja/proc/ntick(mob/living/carbon/human/U = affecting)
	//Runs in the background while the suit is initialized.
	//Requires charge or stealth to process.
	spawn while(s_initialized)
		if(!affecting)
			terminate()//Kills the suit and attached objects.

		else if(cell.charge > 0)
			if(s_coold)
				s_coold--//Checks for ability s_cooldown first.

			cell.charge -= s_cost//s_cost is the default energy cost each ntick, usually 5.
			if(stealth)//If stealth is active.
				cell.charge -= s_acost

		else
			cell.charge = 0
			cancel_stealth()

		sleep(10)//Checks every second.
