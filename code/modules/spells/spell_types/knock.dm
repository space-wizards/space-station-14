/obj/effect/proc_holder/spell/aoe_turf/knock
	name = "Knock"
	desc = "This spell opens nearby doors and closets."

	school = "transmutation"
	charge_max = 100
	clothes_req = FALSE
	invocation = "AULIE OXIN FIERA"
	invocation_type = "whisper"
	range = 3
	cooldown_min = 20 //20 deciseconds reduction per rank

	action_icon_state = "knock"

/obj/effect/proc_holder/spell/aoe_turf/knock/cast(list/targets,mob/user = usr)
	SEND_SOUND(user, sound('sound/magic/knock.ogg'))
	for(var/turf/T in targets)
		for(var/obj/machinery/door/door in T.contents)
			INVOKE_ASYNC(src, .proc/open_door, door)
		for(var/obj/structure/closet/C in T.contents)
			INVOKE_ASYNC(src, .proc/open_closet, C)

/obj/effect/proc_holder/spell/aoe_turf/knock/proc/open_door(var/obj/machinery/door/door)
	if(istype(door, /obj/machinery/door/airlock))
		var/obj/machinery/door/airlock/A = door
		A.locked = FALSE
	door.open()

/obj/effect/proc_holder/spell/aoe_turf/knock/proc/open_closet(var/obj/structure/closet/C)
	C.locked = FALSE
	C.open()
