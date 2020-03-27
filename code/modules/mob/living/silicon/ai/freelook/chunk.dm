#define UPDATE_BUFFER 25 // 2.5 seconds

// CAMERA CHUNK
//
// A 16x16 grid of the map with a list of turfs that can be seen, are visible and are dimmed.
// Allows the AI Eye to stream these chunks and know what it can and cannot see.

/datum/camerachunk
	var/list/obscuredTurfs = list()
	var/list/visibleTurfs = list()
	var/list/cameras = list()
	var/list/turfs = list()
	var/list/seenby = list()
	var/changed = 0
	var/x = 0
	var/y = 0
	var/z = 0

// Add an AI eye to the chunk, then update if changed.

/datum/camerachunk/proc/add(mob/camera/aiEye/eye)
	eye.visibleCameraChunks += src
	seenby += eye
	if(changed)
		update()

// Remove an AI eye from the chunk, then update if changed.

/datum/camerachunk/proc/remove(mob/camera/aiEye/eye, remove_static_with_last_chunk = TRUE)
	eye.visibleCameraChunks -= src
	seenby -= eye
	if(remove_static_with_last_chunk && !eye.visibleCameraChunks.len)
		var/client/client = eye.GetViewerClient()
		if(client)
			switch(eye.use_static)
				if(USE_STATIC_TRANSPARENT)
					client.images -= GLOB.cameranet.obscured_transparent
				if(USE_STATIC_OPAQUE)
					client.images -= GLOB.cameranet.obscured

// Called when a chunk has changed. I.E: A wall was deleted.

/datum/camerachunk/proc/visibilityChanged(turf/loc)
	if(!visibleTurfs[loc])
		return
	hasChanged()

// Updates the chunk, makes sure that it doesn't update too much. If the chunk isn't being watched it will
// instead be flagged to update the next time an AI Eye moves near it.

/datum/camerachunk/proc/hasChanged(update_now = 0)
	if(seenby.len || update_now)
		addtimer(CALLBACK(src, .proc/update), UPDATE_BUFFER, TIMER_UNIQUE)
	else
		changed = 1

// The actual updating. It gathers the visible turfs from cameras and puts them into the appropiate lists.

/datum/camerachunk/proc/update()
	var/list/newVisibleTurfs = list()

	for(var/camera in cameras)
		var/obj/machinery/camera/c = camera

		if(!c)
			continue

		if(!c.can_use())
			continue

		var/turf/point = locate(src.x + (CHUNK_SIZE / 2), src.y + (CHUNK_SIZE / 2), src.z)
		if(get_dist(point, c) > CHUNK_SIZE + (CHUNK_SIZE / 2))
			continue

		for(var/turf/t in c.can_see())
			// Possible optimization: if(turfs[t]) here, rather than &= turfs afterwards.
			// List associations use a tree or hashmap of some sort (alongside the list itself)
			//  so are surprisingly fast. (significantly faster than var/thingy/x in list, in testing)
			newVisibleTurfs[t] = t

	// Removes turf that isn't in turfs.
	newVisibleTurfs &= turfs

	var/list/visAdded = newVisibleTurfs - visibleTurfs
	var/list/visRemoved = visibleTurfs - newVisibleTurfs

	visibleTurfs = newVisibleTurfs
	obscuredTurfs = turfs - newVisibleTurfs

	for(var/turf in visAdded)
		var/turf/t = turf
		t.vis_contents -= GLOB.cameranet.vis_contents_objects

	for(var/turf in visRemoved)
		var/turf/t = turf
		if(obscuredTurfs[t] && !istype(t, /turf/open/ai_visible))
			t.vis_contents += GLOB.cameranet.vis_contents_objects

	changed = 0

// Create a new camera chunk, since the chunks are made as they are needed.

/datum/camerachunk/New(x, y, z)
	x &= ~(CHUNK_SIZE - 1)
	y &= ~(CHUNK_SIZE - 1)

	src.x = x
	src.y = y
	src.z = z

	for(var/obj/machinery/camera/c in urange(CHUNK_SIZE, locate(x + (CHUNK_SIZE / 2), y + (CHUNK_SIZE / 2), z)))
		if(c.can_use())
			cameras += c

	for(var/turf/t in block(locate(max(x, 1), max(y, 1), z), locate(min(x + CHUNK_SIZE - 1, world.maxx), min(y + CHUNK_SIZE - 1, world.maxy), z)))
		turfs[t] = t

	for(var/camera in cameras)
		var/obj/machinery/camera/c = camera
		if(!c)
			continue

		if(!c.can_use())
			continue

		for(var/turf/t in c.can_see())
			// Possible optimization: if(turfs[t]) here, rather than &= turfs afterwards.
			// List associations use a tree or hashmap of some sort (alongside the list itself)
			//  so are surprisingly fast. (significantly faster than var/thingy/x in list, in testing)
			visibleTurfs[t] = t

	// Removes turf that isn't in turfs.
	visibleTurfs &= turfs

	obscuredTurfs = turfs - visibleTurfs

	for(var/turf in obscuredTurfs)
		var/turf/t = turf
		t.vis_contents += GLOB.cameranet.vis_contents_objects

#undef UPDATE_BUFFER
#undef CHUNK_SIZE
