/obj/effect/buildmode_line
	var/image/I
	var/client/cl

/obj/effect/buildmode_line/New(client/C, atom/atom_a, atom/atom_b, linename)
	name = linename
	loc = get_turf(atom_a)
	I = image('icons/misc/mark.dmi', src, "line", 19.0)
	var/x_offset = ((atom_b.x * 32) + atom_b.pixel_x) - ((atom_a.x * 32) + atom_a.pixel_x)
	var/y_offset = ((atom_b.y * 32) + atom_b.pixel_y) - ((atom_a.y * 32) + atom_a.pixel_y)

	var/matrix/mat = matrix()
	mat.Translate(0, 16)
	mat.Scale(1, sqrt((x_offset * x_offset) + (y_offset * y_offset)) / 32)
	mat.Turn(90 - ATAN2(x_offset, y_offset)) // So... You pass coords in order x,y to this version of atan2. It should be called acsc2.
	mat.Translate(atom_a.pixel_x, atom_a.pixel_y)

	transform = mat
	cl = C
	cl.images += I

/obj/effect/buildmode_line/Destroy()
	if(I)
		if(istype(cl))
			cl.images -= I
			cl = null
		QDEL_NULL(I)
	return ..()
