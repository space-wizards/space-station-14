//Vars that will not be copied when using /DuplicateObject
GLOBAL_LIST_INIT(duplicate_forbidden_vars,list(
	"tag", "datum_components", "area", "type", "loc", "locs", "vars", "parent", "parent_type", "verbs", "ckey", "key",
	"power_supply", "contents", "reagents", "stat", "x", "y", "z", "group", "atmos_adjacent_turfs", "comp_lookup"
	))

/proc/DuplicateObject(atom/original, perfectcopy = TRUE, sameloc, atom/newloc = null, nerf, holoitem)
	RETURN_TYPE(original.type)
	if(!original)
		return
	var/atom/O

	if(sameloc)
		O = new original.type(original.loc)
	else
		O = new original.type(newloc)

	if(perfectcopy && O && original)
		for(var/V in original.vars - GLOB.duplicate_forbidden_vars)
			if(islist(original.vars[V]))
				var/list/L = original.vars[V]
				O.vars[V] = L.Copy()
			else if(istype(original.vars[V], /datum))
				continue	// this would reference the original's object, that will break when it is used or deleted.
			else
				O.vars[V] = original.vars[V]

	if(isobj(O))
		var/obj/N = O
		if(holoitem)
			N.resistance_flags = LAVA_PROOF | FIRE_PROOF | UNACIDABLE | ACID_PROOF // holoitems do not burn

		if(nerf && isitem(O))
			var/obj/item/I = O
			I.damtype = STAMINA // thou shalt not

		N.update_icon()
		if(ismachinery(O))
			var/obj/machinery/M = O
			M.power_change()
			if(istype(O, /obj/machinery/button))
				var/obj/machinery/button/B = O
				B.setup_device()

	if(holoitem)
		O.flags_1 |= HOLOGRAM_1
		for(var/atom/thing in O)
			thing.flags_1 |= HOLOGRAM_1
		if(ismachinery(O))
			var/obj/machinery/M = O
			for(var/atom/contained_atom in M.component_parts)
				contained_atom.flags_1 |= HOLOGRAM_1
			if(M.circuit)
				M.circuit.flags_1 |= HOLOGRAM_1
	return O


/area/proc/copy_contents_to(area/A , platingRequired = 0, nerf_weapons = 0 )
	//Takes: Area. Optional: If it should copy to areas that don't have plating
	//Returns: Nothing.
	//Notes: Attempts to move the contents of one area to another area.
	//       Movement based on lower left corner. Tiles that do not fit
	//		 into the new area will not be moved.

	if(!A || !src)
		return 0

	var/list/turfs_src = get_area_turfs(src.type)
	var/list/turfs_trg = get_area_turfs(A.type)

	var/src_min_x = 99999
	var/src_min_y = 99999
	var/list/refined_src = new/list()

	for (var/turf/T in turfs_src)
		src_min_x = min(src_min_x,T.x)
		src_min_y = min(src_min_y,T.y)
	for (var/turf/T in turfs_src)
		refined_src[T] = "[T.x - src_min_x].[T.y - src_min_y]"

	var/trg_min_x = 99999
	var/trg_min_y = 99999
	var/list/refined_trg = new/list()

	for (var/turf/T in turfs_trg)
		trg_min_x = min(trg_min_x,T.x)
		trg_min_y = min(trg_min_y,T.y)
	for (var/turf/T in turfs_trg)
		refined_trg["[T.x - trg_min_x].[T.y - trg_min_y]"] = T

	var/list/toupdate = new/list()

	var/copiedobjs = list()

	for (var/turf/T in refined_src)
		var/coordstring = refined_src[T]
		var/turf/B = refined_trg[coordstring]
		if(!istype(B))
			continue

		if(platingRequired)
			if(isspaceturf(B))
				continue

		var/old_dir1 = T.dir
		var/old_icon_state1 = T.icon_state
		var/old_icon1 = T.icon

		B = B.ChangeTurf(T.type)
		B.setDir(old_dir1)
		B.icon = old_icon1
		B.icon_state = old_icon_state1

		for(var/obj/O in T)
			var/obj/O2 = DuplicateObject(O , perfectcopy=TRUE, newloc = B, nerf=nerf_weapons, holoitem=TRUE)
			if(!O2)
				continue
			copiedobjs += O2.GetAllContents()

		for(var/mob/M in T)
			if(iscameramob(M))
				continue // If we need to check for more mobs, I'll add a variable
			var/mob/SM = DuplicateObject(M , perfectcopy=TRUE, newloc = B, holoitem=TRUE)
			copiedobjs += SM.GetAllContents()

		for(var/V in T.vars - GLOB.duplicate_forbidden_vars)
			if(V == "air")
				var/turf/open/O1 = B
				var/turf/open/O2 = T
				O1.air.copy_from(O2.return_air())
				continue
			B.vars[V] = T.vars[V]
		toupdate += B

	if(toupdate.len)
		for(var/turf/T1 in toupdate)
			CALCULATE_ADJACENT_TURFS(T1)
			SSair.add_to_active(T1,1)


	return copiedobjs
