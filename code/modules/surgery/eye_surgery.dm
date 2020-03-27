/datum/surgery/eye_surgery
	name = "Eye surgery"
	steps = list(/datum/surgery_step/incise, /datum/surgery_step/retract_skin, /datum/surgery_step/clamp_bleeders, /datum/surgery_step/fix_eyes, /datum/surgery_step/close)
	target_mobtypes = list(/mob/living/carbon/human, /mob/living/carbon/monkey)
	possible_locs = list(BODY_ZONE_PRECISE_EYES)
	requires_bodypart_type = 0

//fix eyes
/datum/surgery_step/fix_eyes
	name = "fix eyes"
	implements = list(TOOL_HEMOSTAT = 100, TOOL_SCREWDRIVER = 45, /obj/item/pen = 25)
	time = 64
	experience_given = 5

/datum/surgery/eye_surgery/can_start(mob/user, mob/living/carbon/target)
	var/obj/item/organ/eyes/E = target.getorganslot(ORGAN_SLOT_EYES)
	if(!E)
		to_chat(user, "<span class='warning'>It's hard to do surgery on someone's eyes when [target.p_they()] [target.p_do()]n't have any.</span>")
		return FALSE
	return TRUE

/datum/surgery_step/fix_eyes/preop(mob/user, mob/living/carbon/target, target_zone, obj/item/tool, datum/surgery/surgery)
	display_results(user, target, "<span class='notice'>You begin to fix [target]'s eyes...</span>",
		"<span class='notice'>[user] begins to fix [target]'s eyes.</span>",
		"<span class='notice'>[user] begins to perform surgery on [target]'s eyes.</span>")

/datum/surgery_step/fix_eyes/success(mob/user, mob/living/carbon/target, target_zone, obj/item/tool, datum/surgery/surgery, default_display_results = FALSE)
	var/obj/item/organ/eyes/E = target.getorganslot(ORGAN_SLOT_EYES)
	user.visible_message("<span class='notice'>[user] successfully fixes [target]'s eyes!</span>", "<span class='notice'>You succeed in fixing [target]'s eyes.</span>")
	display_results(user, target, "<span class='notice'>You succeed in fixing [target]'s eyes.</span>",
		"<span class='notice'>[user] successfully fixes [target]'s eyes!</span>",
		"<span class='notice'>[user] completes the surgery on [target]'s eyes.</span>")
	target.cure_blind(list(EYE_DAMAGE))
	target.set_blindness(0)
	target.cure_nearsighted(list(EYE_DAMAGE))
	target.blur_eyes(35)	//this will fix itself slowly.
	E.setOrganDamage(0)
	return ..()

/datum/surgery_step/fix_eyes/failure(mob/user, mob/living/carbon/target, target_zone, obj/item/tool, datum/surgery/surgery)
	if(target.getorgan(/obj/item/organ/brain))
		display_results(user, target, "<span class='warning'>You accidentally stab [target] right in the brain!</span>",
			"<span class='warning'>[user] accidentally stabs [target] right in the brain!</span>",
			"<span class='warning'>[user] accidentally stabs [target] right in the brain!</span>")
		target.adjustOrganLoss(ORGAN_SLOT_BRAIN, 70)
	else
		display_results(user, target, "<span class='warning'>You accidentally stab [target] right in the brain! Or would have, if [target] had a brain.</span>",
			"<span class='warning'>[user] accidentally stabs [target] right in the brain! Or would have, if [target] had a brain.</span>",
			"<span class='warning'>[user] accidentally stabs [target] right in the brain!</span>")
	return FALSE
