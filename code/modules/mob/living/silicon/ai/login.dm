/mob/living/silicon/ai/Login()
	..()
	if(stat != DEAD)
		for(var/each in GLOB.ai_status_displays) //change status
			var/obj/machinery/status_display/ai/O = each
			O.mode = 1
			O.emotion = "Neutral"
			O.update()
	set_eyeobj_visible(TRUE)
	if(multicam_on)
		end_multicam()
	view_core()
