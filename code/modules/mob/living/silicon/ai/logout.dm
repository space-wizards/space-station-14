/mob/living/silicon/ai/Logout()
	..()
	for(var/each in GLOB.ai_status_displays) //change status
		var/obj/machinery/status_display/ai/O = each
		O.mode = 0
		O.update()
	set_eyeobj_visible(FALSE)
	view_core()
