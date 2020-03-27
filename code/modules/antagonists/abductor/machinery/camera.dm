/obj/machinery/computer/camera_advanced/abductor
	name = "Human Observation Console"
	var/team_number = 0
	networks = list("ss13", "abductor")
	var/datum/action/innate/teleport_in/tele_in_action = new
	var/datum/action/innate/teleport_out/tele_out_action = new
	var/datum/action/innate/teleport_self/tele_self_action = new
	var/datum/action/innate/vest_mode_swap/vest_mode_action = new
	var/datum/action/innate/vest_disguise_swap/vest_disguise_action = new
	var/datum/action/innate/set_droppoint/set_droppoint_action = new
	var/obj/machinery/abductor/console/console
	lock_override = TRUE

	icon = 'icons/obj/abductor.dmi'
	icon_state = "camera"
	resistance_flags = INDESTRUCTIBLE | LAVA_PROOF | FIRE_PROOF | ACID_PROOF

/obj/machinery/computer/camera_advanced/abductor/CreateEye()
	..()
	eyeobj.visible_icon = TRUE
	eyeobj.icon = 'icons/mob/cameramob.dmi'
	eyeobj.icon_state = "abductor_camera"
	eyeobj.invisibility = INVISIBILITY_OBSERVER

/obj/machinery/computer/camera_advanced/abductor/GrantActions(mob/living/carbon/user)
	..()

	if(tele_in_action)
		tele_in_action.target = console.pad
		tele_in_action.Grant(user)
		actions += tele_in_action

	if(tele_out_action)
		tele_out_action.target = console
		tele_out_action.Grant(user)
		actions += tele_out_action

	if(tele_self_action)
		tele_self_action.target = console.pad
		tele_self_action.Grant(user)
		actions += tele_self_action

	if(vest_mode_action)
		vest_mode_action.target = console
		vest_mode_action.Grant(user)
		actions += vest_mode_action

	if(vest_disguise_action)
		vest_disguise_action.target = console
		vest_disguise_action.Grant(user)
		actions += vest_disguise_action

	if(set_droppoint_action)
		set_droppoint_action.target = console
		set_droppoint_action.Grant(user)
		actions += set_droppoint_action

/obj/machinery/computer/camera_advanced/abductor/proc/IsScientist(mob/living/carbon/human/H)
	return HAS_TRAIT(H, TRAIT_ABDUCTOR_SCIENTIST_TRAINING)

/datum/action/innate/teleport_in
	name = "Send To"
	icon_icon = 'icons/mob/actions/actions_minor_antag.dmi'
	button_icon_state = "beam_down"

/datum/action/innate/teleport_in/Activate()
	if(!target || !iscarbon(owner))
		return
	var/mob/living/carbon/human/C = owner
	var/mob/camera/aiEye/remote/remote_eye = C.remote_control
	var/obj/machinery/abductor/pad/P = target

	if(GLOB.cameranet.checkTurfVis(remote_eye.loc))
		P.PadToLoc(remote_eye.loc)

/datum/action/innate/teleport_out
	name = "Retrieve"
	icon_icon = 'icons/mob/actions/actions_minor_antag.dmi'
	button_icon_state = "beam_up"

/datum/action/innate/teleport_out/Activate()
	if(!target || !iscarbon(owner))
		return
	var/obj/machinery/abductor/console/console = target

	console.TeleporterRetrieve()

/datum/action/innate/teleport_self
	name = "Send Self"
	icon_icon = 'icons/mob/actions/actions_minor_antag.dmi'
	button_icon_state = "beam_down"

/datum/action/innate/teleport_self/Activate()
	if(!target || !iscarbon(owner))
		return
	var/mob/living/carbon/human/C = owner
	var/mob/camera/aiEye/remote/remote_eye = C.remote_control
	var/obj/machinery/abductor/pad/P = target

	if(GLOB.cameranet.checkTurfVis(remote_eye.loc))
		P.MobToLoc(remote_eye.loc,C)

/datum/action/innate/vest_mode_swap
	name = "Switch Vest Mode"
	icon_icon = 'icons/mob/actions/actions_minor_antag.dmi'
	button_icon_state = "vest_mode"

/datum/action/innate/vest_mode_swap/Activate()
	if(!target || !iscarbon(owner))
		return
	var/obj/machinery/abductor/console/console = target
	console.FlipVest()


/datum/action/innate/vest_disguise_swap
	name = "Switch Vest Disguise"
	icon_icon = 'icons/mob/actions/actions_minor_antag.dmi'
	button_icon_state = "vest_disguise"

/datum/action/innate/vest_disguise_swap/Activate()
	if(!target || !iscarbon(owner))
		return
	var/obj/machinery/abductor/console/console = target
	console.SelectDisguise(remote=1)

/datum/action/innate/set_droppoint
	name = "Set Experiment Release Point"
	icon_icon = 'icons/mob/actions/actions_minor_antag.dmi'
	button_icon_state = "set_drop"

/datum/action/innate/set_droppoint/Activate()
	if(!target || !iscarbon(owner))
		return

	var/mob/living/carbon/human/C = owner
	var/mob/camera/aiEye/remote/remote_eye = C.remote_control

	var/obj/machinery/abductor/console/console = target
	console.SetDroppoint(remote_eye.loc,owner)
