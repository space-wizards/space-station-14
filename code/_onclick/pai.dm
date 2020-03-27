/mob/living/silicon/pai/ClickOn(atom/A, params)
	..()
	if(aicamera.in_camera_mode) //pAI picture taking
		aicamera.camera_mode_off()
		aicamera.captureimage(A, usr, null, aicamera.picture_size_x, aicamera.picture_size_y)
		return
