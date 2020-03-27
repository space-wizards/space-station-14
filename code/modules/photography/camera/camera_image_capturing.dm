/obj/effect/appearance_clone

/obj/effect/appearance_clone/New(loc, atom/A)			//Intentionally not Initialize(), to make sure the clone assumes the intended appearance in time for the camera getFlatIcon.
	if(istype(A))
		appearance = A.appearance
		dir = A.dir
		if(ismovableatom(A))
			var/atom/movable/AM = A
			step_x = AM.step_x
			step_y = AM.step_y
	. = ..()

/obj/item/camera/proc/camera_get_icon(list/turfs, turf/center, psize_x = 96, psize_y = 96, datum/turf_reservation/clone_area, size_x, size_y, total_x, total_y)
	var/list/atoms = list()
	var/skip_normal = FALSE
	var/wipe_atoms = FALSE

	if(istype(clone_area) && total_x == clone_area.width && total_y == clone_area.height && size_x >= 0 && size_y > 0)
		var/cloned_center_x = round(clone_area.bottom_left_coords[1] + ((total_x - 1) / 2))
		var/cloned_center_y = round(clone_area.bottom_left_coords[2] + ((total_y - 1) / 2))
		for(var/t in turfs)
			var/turf/T = t
			var/offset_x = T.x - center.x
			var/offset_y = T.y - center.y
			var/turf/newT = locate(cloned_center_x + offset_x, cloned_center_y + offset_y, clone_area.bottom_left_coords[3])
			if(!(newT in clone_area.reserved_turfs))		//sanity check so we don't overwrite other areas somehow
				continue
			atoms += new /obj/effect/appearance_clone(newT, T)
			if(T.loc.icon_state)
				atoms += new /obj/effect/appearance_clone(newT, T.loc)
			for(var/i in T.contents)
				var/atom/A = i
				if(!A.invisibility || (see_ghosts && isobserver(A)))
					atoms += new /obj/effect/appearance_clone(newT, A)
		skip_normal = TRUE
		wipe_atoms = TRUE
		center = locate(cloned_center_x, cloned_center_y, clone_area.bottom_left_coords[3])

	if(!skip_normal)
		for(var/i in turfs)
			var/turf/T = i
			atoms += T
			for(var/atom/movable/A in T)
				if(A.invisibility)
					if(!(see_ghosts && isobserver(A)))
						continue
				atoms += A
			CHECK_TICK

	var/icon/res = icon('icons/effects/96x96.dmi', "transparent")
	res.Scale(psize_x, psize_y)

	var/list/sorted = list()
	var/j
	for(var/i in 1 to atoms.len)
		var/atom/c = atoms[i]
		for(j = sorted.len, j > 0, --j)
			var/atom/c2 = sorted[j]
			if(c2.layer <= c.layer)
				break
		sorted.Insert(j+1, c)
		CHECK_TICK

	var/xcomp = FLOOR(psize_x / 2, 1) - 15
	var/ycomp = FLOOR(psize_y / 2, 1) - 15


	for(var/atom/A in sorted)
		var/xo = (A.x - center.x) * world.icon_size + A.pixel_x + xcomp
		var/yo = (A.y - center.y) * world.icon_size + A.pixel_y + ycomp
		if(ismovableatom(A))
			var/atom/movable/AM = A
			xo += AM.step_x
			yo += AM.step_y
		var/icon/img = getFlatIcon(A)
		if(img)
			res.Blend(img, blendMode2iconMode(A.blend_mode), xo, yo)
		CHECK_TICK

	if(!silent)
		if(istype(custom_sound))				//This is where the camera actually finishes its exposure.
			playsound(loc, custom_sound, 75, TRUE, -3)
		else
			playsound(loc, pick('sound/items/polaroid1.ogg', 'sound/items/polaroid2.ogg'), 75, TRUE, -3)

	if(wipe_atoms)
		QDEL_LIST(atoms)

	return res
