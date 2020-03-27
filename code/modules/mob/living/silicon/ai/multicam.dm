//Picture in picture

/obj/screen/movable/pic_in_pic/ai
	var/mob/living/silicon/ai/ai
	var/mutable_appearance/highlighted_background
	var/highlighted = FALSE
	var/mob/camera/aiEye/pic_in_pic/aiEye

/obj/screen/movable/pic_in_pic/ai/Initialize()
	. = ..()
	aiEye = new /mob/camera/aiEye/pic_in_pic()
	aiEye.screen = src

/obj/screen/movable/pic_in_pic/ai/Destroy()
	set_ai(null)
	QDEL_NULL(aiEye)
	return ..()

/obj/screen/movable/pic_in_pic/ai/Click()
	..()
	if(ai)
		ai.select_main_multicam_window(src)

/obj/screen/movable/pic_in_pic/ai/make_backgrounds()
	..()
	highlighted_background = new /mutable_appearance()
	highlighted_background.icon = 'icons/misc/pic_in_pic.dmi'
	highlighted_background.icon_state = "background_highlight"
	highlighted_background.layer = SPACE_LAYER

/obj/screen/movable/pic_in_pic/ai/add_background()
	if((width > 0) && (height > 0))
		var/matrix/M = matrix()
		M.Scale(width + 0.5, height + 0.5)
		M.Translate((width-1)/2 * world.icon_size, (height-1)/2 * world.icon_size)
		highlighted_background.transform = M
		standard_background.transform = M
		add_overlay(highlighted ? highlighted_background : standard_background)

/obj/screen/movable/pic_in_pic/ai/set_view_size(width, height, do_refresh = TRUE)
	aiEye.static_visibility_range =	(round(max(width, height) / 2) + 1)
	if(ai)
		ai.camera_visibility(aiEye)
	..()

/obj/screen/movable/pic_in_pic/ai/set_view_center(atom/target, do_refresh = TRUE)
	..()
	aiEye.setLoc(get_turf(target))

/obj/screen/movable/pic_in_pic/ai/refresh_view()
	..()
	aiEye.setLoc(get_turf(center))

/obj/screen/movable/pic_in_pic/ai/proc/highlight()
	if(highlighted)
		return
	highlighted = TRUE
	cut_overlay(standard_background)
	add_overlay(highlighted_background)

/obj/screen/movable/pic_in_pic/ai/proc/unhighlight()
	if(!highlighted)
		return
	highlighted = FALSE
	cut_overlay(highlighted_background)
	add_overlay(standard_background)

/obj/screen/movable/pic_in_pic/ai/proc/set_ai(mob/living/silicon/ai/new_ai)
	if(ai)
		ai.multicam_screens -= src
		ai.all_eyes -= aiEye
		if(ai.master_multicam == src)
			ai.master_multicam = null
		if(ai.multicam_on)
			unshow_to(ai.client)
	ai = new_ai
	if(new_ai)
		new_ai.multicam_screens += src
		ai.all_eyes += aiEye
		if(new_ai.multicam_on)
			show_to(new_ai.client)

//Turf, area, and landmark for the viewing room

/turf/open/ai_visible
	name = ""
	icon = 'icons/misc/pic_in_pic.dmi'
	icon_state = "room_background"
	flags_1 = NOJAUNT_1

/area/ai_multicam_room
	name = "ai_multicam_room"
	icon_state = "ai_camera_room"
	dynamic_lighting = DYNAMIC_LIGHTING_DISABLED
	valid_territory = FALSE
	ambientsounds = list()
	blob_allowed = FALSE
	noteleport = TRUE
	hidden = TRUE
	safe = TRUE
	flags_1 = NONE

GLOBAL_DATUM(ai_camera_room_landmark, /obj/effect/landmark/ai_multicam_room)

/obj/effect/landmark/ai_multicam_room
	name = "ai camera room"
	icon = 'icons/mob/landmarks.dmi'
	icon_state = "x"

/obj/effect/landmark/ai_multicam_room/Initialize()
	. = ..()
	qdel(GLOB.ai_camera_room_landmark)
	GLOB.ai_camera_room_landmark = src

/obj/effect/landmark/ai_multicam_room/Destroy()
	if(GLOB.ai_camera_room_landmark == src)
		GLOB.ai_camera_room_landmark = null
	return ..()

//Dummy camera eyes

/mob/camera/aiEye/pic_in_pic
	name = "Secondary AI Eye"
	invisibility = INVISIBILITY_OBSERVER
	mouse_opacity = MOUSE_OPACITY_ICON
	icon_state = "ai_pip_camera"
	var/obj/screen/movable/pic_in_pic/ai/screen
	var/list/cameras_telegraphed = list()
	var/telegraph_cameras = TRUE
	var/telegraph_range = 7
	ai_detector_color = COLOR_ORANGE

/mob/camera/aiEye/pic_in_pic/GetViewerClient()
	if(screen && screen.ai)
		return screen.ai.client

/mob/camera/aiEye/pic_in_pic/setLoc(turf/T)
	if (T)
		forceMove(T)
	else
		moveToNullspace()
	if(screen && screen.ai)
		screen.ai.camera_visibility(src)
	else
		GLOB.cameranet.visibility(src)
	update_camera_telegraphing()
	update_ai_detect_hud()

/mob/camera/aiEye/pic_in_pic/get_visible_turfs()
	return screen ? screen.get_visible_turfs() : list()

/mob/camera/aiEye/pic_in_pic/proc/update_camera_telegraphing()
	if(!telegraph_cameras)
		return
	var/list/obj/machinery/camera/add = list()
	var/list/obj/machinery/camera/remove = list()
	var/list/obj/machinery/camera/visible = list()
	for (var/VV in visibleCameraChunks)
		var/datum/camerachunk/CC = VV
		for (var/V in CC.cameras)
			var/obj/machinery/camera/C = V
			if (!C.can_use() || (get_dist(C, src) > telegraph_range))
				continue
			visible |= C

	add = visible - cameras_telegraphed
	remove = cameras_telegraphed - visible

	for (var/V in remove)
		var/obj/machinery/camera/C = V
		if(QDELETED(C))
			continue
		cameras_telegraphed -= C
		C.in_use_lights--
		C.update_icon()
	for (var/V in add)
		var/obj/machinery/camera/C = V
		if(QDELETED(C))
			continue
		cameras_telegraphed |= C
		C.in_use_lights++
		C.update_icon()

/mob/camera/aiEye/pic_in_pic/proc/disable_camera_telegraphing()
	telegraph_cameras = FALSE
	for (var/V in cameras_telegraphed)
		var/obj/machinery/camera/C = V
		if(QDELETED(C))
			continue
		C.in_use_lights--
		C.update_icon()
	cameras_telegraphed.Cut()

/mob/camera/aiEye/pic_in_pic/Destroy()
	disable_camera_telegraphing()
	return ..()

//AI procs

/mob/living/silicon/ai/proc/drop_new_multicam(silent = FALSE)
	if(!CONFIG_GET(flag/allow_ai_multicam))
		if(!silent)
			to_chat(src, "<span class='warning'>This action is currently disabled. Contact an administrator to enable this feature.</span>")
		return
	if(!eyeobj)
		return
	if(multicam_screens.len >= max_multicams)
		if(!silent)
			to_chat(src, "<span class='warning'>Cannot place more than [max_multicams] multicamera windows.</span>")
		return
	var/obj/screen/movable/pic_in_pic/ai/C = new /obj/screen/movable/pic_in_pic/ai()
	C.set_view_size(3, 3, FALSE)
	C.set_view_center(get_turf(eyeobj))
	C.set_ai(src)
	if(!silent)
		to_chat(src, "<span class='notice'>Added new multicamera window.</span>")
	return C

/mob/living/silicon/ai/proc/toggle_multicam()
	if(!CONFIG_GET(flag/allow_ai_multicam))
		to_chat(src, "<span class='warning'>This action is currently disabled. Contact an administrator to enable this feature.</span>")
		return
	if(multicam_on)
		end_multicam()
	else
		start_multicam()

/mob/living/silicon/ai/proc/start_multicam()
	if(multicam_on || aiRestorePowerRoutine || !isturf(loc))
		return
	if(!GLOB.ai_camera_room_landmark)
		to_chat(src, "<span class='warning'>This function is not available at this time.</span>")
		return
	multicam_on = TRUE
	refresh_multicam()
	to_chat(src, "<span class='notice'>Multiple-camera viewing mode activated.</span>")

/mob/living/silicon/ai/proc/refresh_multicam()
	reset_perspective(GLOB.ai_camera_room_landmark)
	if(client)
		for(var/V in multicam_screens)
			var/obj/screen/movable/pic_in_pic/P = V
			P.show_to(client)

/mob/living/silicon/ai/proc/end_multicam()
	if(!multicam_on)
		return
	multicam_on = FALSE
	select_main_multicam_window(null)
	if(client)
		for(var/V in multicam_screens)
			var/obj/screen/movable/pic_in_pic/P = V
			P.unshow_to(client)
	reset_perspective()
	to_chat(src, "<span class='notice'>Multiple-camera viewing mode deactivated.</span>")


/mob/living/silicon/ai/proc/select_main_multicam_window(obj/screen/movable/pic_in_pic/ai/P)
	if(master_multicam == P)
		return

	if(master_multicam)
		master_multicam.set_view_center(get_turf(eyeobj), FALSE)
		master_multicam.unhighlight()
		master_multicam = null

	if(P)
		P.highlight()
		eyeobj.setLoc(get_turf(P.center))
		P.set_view_center(eyeobj)
		master_multicam = P
