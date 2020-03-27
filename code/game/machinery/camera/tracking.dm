/mob/living/silicon/ai/proc/get_camera_list()
	var/list/L = list()
	for (var/obj/machinery/camera/C in GLOB.cameranet.cameras)
		L.Add(C)

	camera_sort(L)

	var/list/T = list()

	for (var/obj/machinery/camera/C in L)
		var/list/tempnetwork = C.network&src.network
		if (tempnetwork.len)
			T[text("[][]", C.c_tag, (C.can_use() ? null : " (Deactivated)"))] = C

	return T

/mob/living/silicon/ai/proc/show_camera_list()
	var/list/cameras = get_camera_list()
	var/camera = input(src, "Choose which camera you want to view", "Cameras") as null|anything in cameras
	switchCamera(cameras[camera])

/datum/trackable
	var/initialized = FALSE
	var/list/names = list()
	var/list/namecounts = list()
	var/list/humans = list()
	var/list/others = list()

/mob/living/silicon/ai/proc/trackable_mobs()
	track.initialized = TRUE
	track.names.Cut()
	track.namecounts.Cut()
	track.humans.Cut()
	track.others.Cut()

	if(usr.stat == DEAD)
		return list()

	for(var/i in GLOB.mob_living_list)
		var/mob/living/L = i
		if(!L.can_track(usr))
			continue

		var/name = L.name
		while(name in track.names)
			track.namecounts[name]++
			name = text("[] ([])", name, track.namecounts[name])
		track.names.Add(name)
		track.namecounts[name] = 1

		if(ishuman(L))
			track.humans[name] = L
		else
			track.others[name] = L

	var/list/targets = sortList(track.humans) + sortList(track.others)

	return targets

/mob/living/silicon/ai/verb/ai_camera_track(target_name in trackable_mobs())
	set name = "track"
	set hidden = 1 //Don't display it on the verb lists. This verb exists purely so you can type "track Oldman Robustin" and follow his ass

	if(!target_name)
		return

	if(!track.initialized)
		trackable_mobs()

	var/mob/target = (isnull(track.humans[target_name]) ? track.others[target_name] : track.humans[target_name])

	ai_actual_track(target)

/mob/living/silicon/ai/proc/ai_actual_track(mob/living/target)
	if(!istype(target))
		return
	var/mob/living/silicon/ai/U = usr

	U.cameraFollow = target
	U.tracking = 1

	if(!target || !target.can_track(usr))
		to_chat(U, "<span class='warning'>Target is not near any active cameras.</span>")
		U.cameraFollow = null
		return

	to_chat(U, "<span class='notice'>Now tracking [target.get_visible_name()] on camera.</span>")

	INVOKE_ASYNC(src, .proc/do_track, target, U)

/mob/living/silicon/ai/proc/do_track(mob/living/target, mob/living/silicon/ai/U)
	var/cameraticks = 0

	while(U.cameraFollow == target)
		if(U.cameraFollow == null)
			return

		if(!target.can_track(usr))
			U.tracking = 1
			if(!cameraticks)
				to_chat(U, "<span class='warning'>Target is not near any active cameras. Attempting to reacquire...</span>")
			cameraticks++
			if(cameraticks > 9)
				U.cameraFollow = null
				to_chat(U, "<span class='warning'>Unable to reacquire, cancelling track...</span>")
				tracking = 0
				return
			else
				sleep(10)
				continue

		else
			cameraticks = 0
			U.tracking = 0

		if(U.eyeobj)
			U.eyeobj.setLoc(get_turf(target))

		else
			view_core()
			U.cameraFollow = null
			return

		sleep(10)

/proc/near_camera(mob/living/M)
	if (!isturf(M.loc))
		return FALSE
	if(issilicon(M))
		var/mob/living/silicon/S = M
		if((QDELETED(S.builtInCamera) || !S.builtInCamera.can_use()) && !GLOB.cameranet.checkCameraVis(M))
			return FALSE
	else if(!GLOB.cameranet.checkCameraVis(M))
		return FALSE
	return TRUE

/obj/machinery/camera/attack_ai(mob/living/silicon/ai/user)
	if (!istype(user))
		return
	if (!can_use())
		return
	user.switchCamera(src)

/proc/camera_sort(list/L)
	var/obj/machinery/camera/a
	var/obj/machinery/camera/b

	for (var/i = L.len, i > 0, i--)
		for (var/j = 1 to i - 1)
			a = L[j]
			b = L[j + 1]
			if (sorttext(a.c_tag, b.c_tag) < 0)
				L.Swap(j, j + 1)
	return L
