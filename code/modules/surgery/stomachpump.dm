/datum/surgery/stomach_pump
	name = "Stomach Pump"
	steps = list(/datum/surgery_step/incise,
				/datum/surgery_step/retract_skin,
				/datum/surgery_step/incise,
				/datum/surgery_step/clamp_bleeders,
				/datum/surgery_step/stomach_pump,
				/datum/surgery_step/close)

	target_mobtypes = list(/mob/living/carbon/human, /mob/living/carbon/monkey)
	possible_locs = list(BODY_ZONE_CHEST)
	requires_bodypart_type = TRUE
	ignore_clothes = FALSE

/datum/surgery/stomach_pump/can_start(mob/user, mob/living/carbon/target)
	var/obj/item/organ/stomach/S = target.getorganslot(ORGAN_SLOT_STOMACH)
	if(target.stat != DEAD)	//shamelessly lifted off the revival surgery but we're looking for the same critera here, a dead, non-husked, revivable patient.
		return FALSE
	if(HAS_TRAIT(target, TRAIT_HUSK))
		return FALSE
	if(!S)
		return FALSE
	return ..()

//Working the stomach by hand in such a way that you induce vomiting.
/datum/surgery_step/stomach_pump
	name = "Pump Stomach"
	accept_hand = TRUE
	repeatable = TRUE
	time = 20

/datum/surgery_step/stomach_pump/preop(mob/user, mob/living/carbon/target, target_zone, obj/item/tool, datum/surgery/surgery)
	display_results(user, target, "<span class='notice'>You begin pumping [target]'s stomach...</span>",
		"<span class='notice'>[user] begins to pump [target]'s stomach.</span>",
		"<span class='notice'>[user] begins to press on [target]'s chest.</span>")

/datum/surgery_step/stomach_pump/success(mob/user, mob/living/carbon/target, target_zone, obj/item/tool, datum/surgery/surgery, default_display_results = FALSE)
	if(ishuman(target))
		var/mob/living/carbon/human/H = target
		display_results(user, target, "<span class='notice'>[user] forces [H] to vomit, cleansing their stomach of some chemicals!</span>",
				"<span class='notice'>[user] forces [H] to vomit, cleansing their stomach of some chemicals!</span>",
				"[user] forces [H] to vomit!")
		H.vomit(20, FALSE, TRUE, 1, TRUE, FALSE, purge = TRUE) //called with purge as true to lose more reagents
	return ..()

/datum/surgery_step/stomach_pump/failure(mob/user, mob/living/carbon/target, target_zone, obj/item/tool, datum/surgery/surgery)
	if(ishuman(target))
		var/mob/living/carbon/human/H = target
		display_results(user, target, "<span class='warning'>You screw up, brusing [H]'s chest!</span>",
			"<span class='warning'>[user] screws up, brusing [H]'s chest!</span>",
			"<span class='warning'>[user] screws up!</span>")
		H.adjustOrganLoss(ORGAN_SLOT_STOMACH, 5)
		H.adjustBruteLoss(5)
