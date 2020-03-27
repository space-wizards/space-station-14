#define STATE_WRENCHED 1
#define STATE_WELDED 2
#define STATE_WIRED 3
#define STATE_FINISHED 4

/obj/item/wallframe/camera
	name = "camera assembly"
	desc = "The basic construction for Nanotrasen-Always-Watching-You cameras."
	icon = 'icons/obj/machines/camera.dmi'
	icon_state = "cameracase"
	custom_materials = list(/datum/material/iron=400, /datum/material/glass=250)
	result_path = /obj/structure/camera_assembly

/obj/structure/camera_assembly
	name = "camera assembly"
	desc = "The basic construction for Nanotrasen-Always-Watching-You cameras."
	icon = 'icons/obj/machines/camera.dmi'
	icon_state = "camera_assembly"
	max_integrity = 150
	//	Motion, EMP-Proof, X-ray
	var/obj/item/analyzer/xray_module
	var/malf_xray_firmware_active //used to keep from revealing malf AI upgrades for user facing isXRay() checks when they use Upgrade Camera Network ability
								//will be false if the camera is upgraded with the proper parts.
	var/malf_xray_firmware_present //so the malf upgrade is restored when the normal upgrade part is removed.
	var/obj/item/stack/sheet/mineral/plasma/emp_module
	var/malf_emp_firmware_active //used to keep from revealing malf AI upgrades for user facing isEmp() checks after they use Upgrade Camera Network ability
								//will be false if the camera is upgraded with the proper parts.
	var/malf_emp_firmware_present //so the malf upgrade is restored when the normal upgrade part is removed.
	var/obj/item/assembly/prox_sensor/proxy_module
	var/state = STATE_WRENCHED

/obj/structure/camera_assembly/examine(mob/user)
	. = ..()
	//upgrade messages
	var/has_upgrades
	if(emp_module)
		. += "It has electromagnetic interference shielding installed."
		has_upgrades = TRUE
	else if(state == STATE_WIRED)
		. += "<span class='info'>It can be shielded against electromagnetic interference with some <b>plasma</b>.</span>"
	if(xray_module)
		. += "It has an X-ray photodiode installed."
		has_upgrades = TRUE
	else if(state == STATE_WIRED)
		. += "<span class='info'>It can be upgraded with an X-ray photodiode with an <b>analyzer</b>.</span>"
	if(proxy_module)
		. += "It has a proximity sensor installed."
		has_upgrades = TRUE
	else if(state == STATE_WIRED)
		. += "<span class='info'>It can be upgraded with a <b>proximity sensor</b>.</span>"

	//construction states
	switch(state)
		if(STATE_WRENCHED)
			. += "<span class='info'>You can secure it in place with a <b>welder</b>, or removed with a <b>wrench</b>.</span>"
		if(STATE_WELDED)
			. += "<span class='info'>You can add <b>wires</b> to it, or <b>unweld</b> it from the wall.</span>"
		if(STATE_WIRED)
			if(has_upgrades)
				. += "<span class='info'>You can remove the contained upgrades with a <b>crowbar</b>.</span>"
			. += "<span class='info'>You can complete it with a <b>screwdriver</b>, or <b>unwire</b> it to start removal.</span>"
		if(STATE_FINISHED)
			. += "<span class='boldwarning'>You shouldn't be seeing this, tell a coder!</span>"

/obj/structure/camera_assembly/Initialize(mapload, ndir, building)
	. = ..()
	if(building)
		setDir(ndir)

/obj/structure/camera_assembly/update_icon_state()
	icon_state = "[xray_module ? "xray" : null][initial(icon_state)]"

/obj/structure/camera_assembly/handle_atom_del(atom/A)
	if(A == xray_module)
		xray_module = null
		update_icon()
		if(malf_xray_firmware_present)
			malf_xray_firmware_active = malf_xray_firmware_present //re-enable firmware based upgrades after the part is removed.
		if(istype(loc, /obj/machinery/camera))
			var/obj/machinery/camera/contained_camera = loc
			contained_camera.removeXRay(malf_xray_firmware_present) //make sure we don't remove MALF upgrades.

	else if(A == emp_module)
		emp_module = null
		if(malf_emp_firmware_present)
			malf_emp_firmware_active = malf_emp_firmware_present //re-enable firmware based upgrades after the part is removed.
		if(istype(loc, /obj/machinery/camera))
			var/obj/machinery/camera/contained_camera = loc
			contained_camera.removeEmpProof(malf_emp_firmware_present) //make sure we don't remove MALF upgrades

	else if(A == proxy_module)
		emp_module = null
		if(istype(loc, /obj/machinery/camera))
			var/obj/machinery/camera/contained_camera = loc
			contained_camera.removeMotion()

	return ..()


/obj/structure/camera_assembly/Destroy()
	QDEL_NULL(xray_module)
	QDEL_NULL(emp_module)
	QDEL_NULL(proxy_module)
	return ..()

/obj/structure/camera_assembly/proc/drop_upgrade(obj/item/I)
	I.forceMove(drop_location())
	if(I == xray_module)
		xray_module = null
		if(malf_xray_firmware_present)
			malf_xray_firmware_active = malf_xray_firmware_present //re-enable firmware based upgrades after the part is removed.
		update_icon()

	else if(I == emp_module)
		emp_module = null
		if(malf_emp_firmware_present)
			malf_emp_firmware_active = malf_emp_firmware_present //re-enable firmware based upgrades after the part is removed.

	else if(I == proxy_module)
		proxy_module = null


/obj/structure/camera_assembly/attackby(obj/item/W, mob/living/user, params)
	switch(state)
		if(STATE_WRENCHED)
			if(W.tool_behaviour == TOOL_WELDER)
				if(weld(W, user))
					to_chat(user, "<span class='notice'>You weld [src] securely into place.</span>")
					setAnchored(TRUE)
					state = STATE_WELDED
				return

		if(STATE_WELDED)
			if(istype(W, /obj/item/stack/cable_coil))
				var/obj/item/stack/cable_coil/C = W
				if(C.use(2))
					to_chat(user, "<span class='notice'>You add wires to [src].</span>")
					state = STATE_WIRED
				else
					to_chat(user, "<span class='warning'>You need two lengths of cable to wire a camera!</span>")
					return
				return

			else if(W.tool_behaviour == TOOL_WELDER)

				if(weld(W, user))
					to_chat(user, "<span class='notice'>You unweld [src] from its place.</span>")
					state = STATE_WRENCHED
					setAnchored(TRUE)
				return

		if(STATE_WIRED)	// Upgrades!
			if(istype(W, /obj/item/stack/sheet/mineral/plasma)) //emp upgrade
				if(emp_module)
					to_chat(user, "<span class='warning'>[src] already contains a [emp_module]!</span>")
					return
				if(!W.use_tool(src, user, 0, amount=1)) //only use one sheet, otherwise the whole stack will be consumed.
					return
				emp_module = new(src)
				if(malf_xray_firmware_active)
					malf_xray_firmware_active = FALSE //flavor reason: MALF AI Upgrade Camera Network ability's firmware is incompatible with the new part
														//real reason: make it a normal upgrade so the finished camera's icons and examine texts are restored.
				to_chat(user, "<span class='notice'>You attach [W] into [src]'s inner circuits.</span>")
				return

			else if(istype(W, /obj/item/analyzer)) //xray upgrade
				if(xray_module)
					to_chat(user, "<span class='warning'>[src] already contains a [xray_module]!</span>")
					return
				if(!user.transferItemToLoc(W, src))
					return
				to_chat(user, "<span class='notice'>You attach [W] into [src]'s inner circuits.</span>")
				xray_module = W
				if(malf_xray_firmware_active)
					malf_xray_firmware_active = FALSE //flavor reason: MALF AI Upgrade Camera Network ability's firmware is incompatible with the new part
														//real reason: make it a normal upgrade so the finished camera's icons and examine texts are restored.
				update_icon()
				return

			else if(istype(W, /obj/item/assembly/prox_sensor)) //motion sensing upgrade
				if(proxy_module)
					to_chat(user, "<span class='warning'>[src] already contains a [proxy_module]!</span>")
					return
				if(!user.transferItemToLoc(W, src))
					return
				to_chat(user, "<span class='notice'>You attach [W] into [src]'s inner circuits.</span>")
				proxy_module = W
				return

	return ..()

/obj/structure/camera_assembly/crowbar_act(mob/user, obj/item/tool)
	if(state != STATE_WIRED)
		return FALSE
	var/list/droppable_parts = list()
	if(xray_module)
		droppable_parts += xray_module
	if(emp_module)
		droppable_parts += emp_module
	if(proxy_module)
		droppable_parts += proxy_module
	if(!droppable_parts.len)
		return
	var/obj/item/choice = input(user, "Select a part to remove:", src) as null|obj in sortNames(droppable_parts)
	if(!choice || !user.canUseTopic(src, BE_CLOSE, FALSE, NO_TK))
		return
	to_chat(user, "<span class='notice'>You remove [choice] from [src].</span>")
	drop_upgrade(choice)
	tool.play_tool_sound(src)
	return TRUE

/obj/structure/camera_assembly/screwdriver_act(mob/user, obj/item/tool)
	. = ..()
	if(.)
		return TRUE
	if(state != STATE_WIRED)
		return FALSE

	tool.play_tool_sound(src)
	var/input = stripped_input(user, "Which networks would you like to connect this camera to? Separate networks with a comma. No Spaces!\nFor example: SS13,Security,Secret ", "Set Network", "SS13")
	if(!input)
		to_chat(user, "<span class='warning'>No input found, please hang up and try your call again!</span>")
		return
	var/list/tempnetwork = splittext(input, ",")
	if(tempnetwork.len < 1)
		to_chat(user, "<span class='warning'>No network found, please hang up and try your call again!</span>")
		return
	for(var/i in tempnetwork)
		tempnetwork -= i
		tempnetwork += lowertext(i)
	state = STATE_FINISHED
	var/obj/machinery/camera/C = new(loc, src)
	forceMove(C)
	C.setDir(src.dir)

	C.network = tempnetwork
	var/area/A = get_area(src)
	C.c_tag = "[A.name] ([rand(1, 999)])"
	return TRUE

/obj/structure/camera_assembly/wirecutter_act(mob/user, obj/item/I)
	. = ..()
	if(state != STATE_WIRED)
		return

	new /obj/item/stack/cable_coil(drop_location(), 2)
	I.play_tool_sound(src)
	to_chat(user, "<span class='notice'>You cut the wires from the circuits.</span>")
	state = STATE_WELDED
	return TRUE

/obj/structure/camera_assembly/wrench_act(mob/user, obj/item/I)
	. = ..()
	if(state != STATE_WRENCHED)
		return
	I.play_tool_sound(src)
	to_chat(user, "<span class='notice'>You detach [src] from its place.</span>")
	new /obj/item/wallframe/camera(drop_location())
	//drop upgrades
	if(xray_module)
		drop_upgrade(xray_module)
	if(emp_module)
		drop_upgrade(emp_module)
	if(proxy_module)
		drop_upgrade(proxy_module)

	qdel(src)
	return TRUE

/obj/structure/camera_assembly/proc/weld(obj/item/weldingtool/W, mob/living/user)
	if(!W.tool_start_check(user, amount=3))
		return FALSE
	to_chat(user, "<span class='notice'>You start to weld [src]...</span>")
	if(W.use_tool(src, user, 20, amount=3, volume = 50))
		return TRUE
	return FALSE

/obj/structure/camera_assembly/deconstruct(disassembled = TRUE)
	if(!(flags_1 & NODECONSTRUCT_1))
		new /obj/item/stack/sheet/metal(loc)
	qdel(src)


#undef STATE_WRENCHED
#undef STATE_WELDED
#undef STATE_WIRED
#undef STATE_FINISHED
