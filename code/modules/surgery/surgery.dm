/datum/surgery
	var/name = "surgery"
	var/desc = "surgery description"
	var/status = 1
	var/list/steps = list()										//Steps in a surgery
	var/step_in_progress = FALSE								//Actively performing a Surgery
	var/can_cancel = TRUE										//Can cancel this surgery after step 1 with cautery
	var/list/target_mobtypes = list(/mob/living/carbon/human)	//Acceptable Species
	var/location = BODY_ZONE_CHEST								//Surgery location
	var/requires_bodypart_type = BODYPART_ORGANIC				//Prevents you from performing an operation on incorrect limbs. 0 for any limb type
	var/list/possible_locs = list() 							//Multiple locations
	var/ignore_clothes = FALSE									//This surgery ignores clothes
	var/mob/living/carbon/target								//Operation target mob
	var/obj/item/bodypart/operated_bodypart						//Operable body part
	var/requires_bodypart = TRUE								//Surgery available only when a bodypart is present, or only when it is missing.
	var/speed_modifier = 0										//Step speed modifier
	var/requires_real_bodypart = FALSE							//Some surgeries don't work on limbs that don't really exist
	var/lying_required = TRUE									//Does the vicitm needs to be lying down.
	var/self_operable = FALSE									//Can the surgery be performed on yourself.
	var/requires_tech = FALSE									//handles techweb-oriented surgeries, previously restricted to the /advanced subtype (You still need to add designs)
	var/replaced_by												//type; doesn't show up if this type exists. Set to /datum/surgery if you want to hide a "base" surgery (useful for typing parents IE healing.dm just make sure to null it out again)

/datum/surgery/New(surgery_target, surgery_location, surgery_bodypart)
	..()
	if(surgery_target)
		target = surgery_target
		target.surgeries += src
		if(surgery_location)
			location = surgery_location
		if(surgery_bodypart)
			operated_bodypart = surgery_bodypart

/datum/surgery/Destroy()
	if(target)
		target.surgeries -= src
	target = null
	operated_bodypart = null
	return ..()


/datum/surgery/proc/can_start(mob/user, mob/living/patient) //FALSE to not show in list
	. = TRUE
	if(replaced_by == /datum/surgery)
		return FALSE

	// True surgeons (like abductor scientists) need no instructions
	if(HAS_TRAIT(user, TRAIT_SURGEON) || HAS_TRAIT(user.mind, TRAIT_SURGEON))
		if(replaced_by) // only show top-level surgeries
			return FALSE
		else
			return TRUE

	if(!requires_tech && !replaced_by)
		return TRUE

	if(requires_tech)
		. = FALSE

	if(iscyborg(user))
		var/mob/living/silicon/robot/R = user
		var/obj/item/surgical_processor/SP = locate() in R.module.modules
		if(SP) //no early return for !SP since we want to check optable should this not exist.
			if(replaced_by in SP.advanced_surgeries)
				return FALSE
			if(type in SP.advanced_surgeries)
				return TRUE

	var/turf/T = get_turf(patient)

	//Get the relevant operating computer
	var/obj/machinery/computer/operating/opcomputer
	var/obj/structure/table/optable/table = locate(/obj/structure/table/optable, T)
	if(table?.computer)
		opcomputer = table.computer
	else
		var/obj/machinery/stasis/the_stasis_bed = locate(/obj/machinery/stasis, T)
		if(the_stasis_bed?.op_computer)
			opcomputer = the_stasis_bed.op_computer

	if(!opcomputer)
		return
	if(opcomputer.stat & (NOPOWER|BROKEN))
		return .
	if(replaced_by in opcomputer.advanced_surgeries)
		return FALSE
	if(type in opcomputer.advanced_surgeries)
		return TRUE

/datum/surgery/proc/next_step(mob/user, intent)
	if(location != user.zone_selected)
		return FALSE
	if(step_in_progress)
		return TRUE

	var/try_to_fail = FALSE
	if(intent == INTENT_DISARM)
		try_to_fail = TRUE

	var/datum/surgery_step/S = get_surgery_step()
	if(S)
		var/obj/item/tool = user.get_active_held_item()
		if(S.try_op(user, target, user.zone_selected, tool, src, try_to_fail))
			return TRUE
		if(tool && tool.item_flags & SURGICAL_TOOL) //Just because you used the wrong tool it doesn't mean you meant to whack the patient with it
			to_chat(user, "<span class='warning'>This step requires a different tool!</span>")
			return TRUE
	return FALSE

/datum/surgery/proc/get_surgery_step()
	var/step_type = steps[status]
	return new step_type

/datum/surgery/proc/get_surgery_next_step()
	if(status < steps.len)
		var/step_type = steps[status + 1]
		return new step_type
	else
		return null

/datum/surgery/proc/complete()
	SSblackbox.record_feedback("tally", "surgeries_completed", 1, type)
	qdel(src)

/datum/surgery/advanced
	name = "advanced surgery"
	requires_tech = TRUE

/obj/item/disk/surgery
	name = "Surgery Procedure Disk"
	desc = "A disk that contains advanced surgery procedures, must be loaded into an Operating Console."
	icon_state = "datadisk1"
	custom_materials = list(/datum/material/iron=300, /datum/material/glass=100)
	var/list/surgeries

/obj/item/disk/surgery/debug
	name = "Debug Surgery Disk"
	desc = "A disk that contains all existing surgery procedures."
	icon_state = "datadisk1"
	custom_materials = list(/datum/material/iron=300, /datum/material/glass=100)

/obj/item/disk/surgery/debug/Initialize()
	. = ..()
	surgeries = list()
	var/list/req_tech_surgeries = subtypesof(/datum/surgery)
	for(var/i in req_tech_surgeries)
		var/datum/surgery/beep = i
		if(initial(beep.requires_tech))
			surgeries += beep

//INFO
//Check /mob/living/carbon/attackby for how surgery progresses, and also /mob/living/carbon/attack_hand.
//As of Feb 21 2013 they are in code/modules/mob/living/carbon/carbon.dm, lines 459 and 51 respectively.
//Other important variables are var/list/surgeries (/mob/living) and var/list/internal_organs (/mob/living/carbon)
// var/list/bodyparts (/mob/living/carbon/human) is the LIMBS of a Mob.
//Surgical procedures are initiated by attempt_initiate_surgery(), which is called by surgical drapes and bedsheets.


//TODO
//specific steps for some surgeries (fluff text)
//more interesting failure options
//randomised complications
//more surgeries!
//add a probability modifier for the state of the surgeon- health, twitching, etc. blindness, god forbid.
//helper for converting a zone_sel.selecting to body part (for damage)


//RESOLVED ISSUES //"Todo" jobs that have been completed
//combine hands/feet into the arms - Hands/feet were removed - RR
//surgeries (not steps) that can be initiated on any body part (corresponding with damage locations) - Call this one done, see possible_locs var - c0
