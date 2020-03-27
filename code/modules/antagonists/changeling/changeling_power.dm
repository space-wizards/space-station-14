/*
 * Don't use the apostrophe in name or desc. Causes script errors.//probably no longer true
 */

/datum/action/changeling
	name = "Prototype Sting - Debug button, ahelp this"
	background_icon_state = "bg_changeling"
	icon_icon = 'icons/mob/actions/actions_changeling.dmi'
	var/needs_button = TRUE//for passive abilities like hivemind that dont need a button
	var/helptext = "" // Details
	var/chemical_cost = 0 // negative chemical cost is for passive abilities (chemical glands)
	var/dna_cost = -1 //cost of the sting in dna points. 0 = auto-purchase (see changeling.dm), -1 = cannot be purchased
	var/req_dna = 0  //amount of dna needed to use this ability. Changelings always have atleast 1
	var/req_human = 0 //if you need to be human to use this ability
	var/req_absorbs = 0 //similar to req_dna, but only gained from absorbing, not DNA sting
	var/req_stat = CONSCIOUS // CONSCIOUS, UNCONSCIOUS or DEAD
	var/ignores_fakedeath = FALSE // usable with the FAKEDEATH flag
	var/active = FALSE//used by a few powers that toggle

/*
changeling code now relies on on_purchase to grant powers.
if you override it, MAKE SURE you call parent or it will not be usable
the same goes for Remove(). if you override Remove(), call parent or else your power wont be removed on respec
*/

/datum/action/changeling/proc/on_purchase(mob/user, is_respec)
	if(!is_respec)
		SSblackbox.record_feedback("tally", "changeling_power_purchase", 1, name)
	if(needs_button)
		Grant(user)//how powers are added rather than the checks in mob.dm

/datum/action/changeling/Trigger()
	var/mob/user = owner
	if(!user || !user.mind || !user.mind.has_antag_datum(/datum/antagonist/changeling))
		return
	try_to_sting(user)

/**
  *Contrary to the name, this proc isn't just used by changeling stings. It handles the activation of the action and the deducation of its cost.
  *The order of the proc chain is:
  *can_sting(). Should this fail, the process gets aborted early.
  *sting_action(). This proc usually handles the actual effect of the action.
  *Should sting_action succeed the following will be done:
  *sting_feedback(). Produces feedback on the performed action. Don't ask me why this isn't handled in sting_action()
  *The deduction of the cost of this power.
  *Returns TRUE on a successful activation.
  */
/datum/action/changeling/proc/try_to_sting(mob/user, mob/target)
	if(!can_sting(user, target))
		return FALSE
	var/datum/antagonist/changeling/c = user.mind.has_antag_datum(/datum/antagonist/changeling)
	if(sting_action(user, target))
		sting_feedback(user, target)
		c.chem_charges -= chemical_cost
		return TRUE
	return FALSE

/datum/action/changeling/proc/sting_action(mob/user, mob/target)
	SSblackbox.record_feedback("nested tally", "changeling_powers", 1, list("[name]"))
	return 0

/datum/action/changeling/proc/sting_feedback(mob/user, mob/target)
	return 0

//Fairly important to remember to return 1 on success >.<

/datum/action/changeling/proc/can_sting(mob/living/user, mob/target)
	if(!ishuman(user) && !ismonkey(user)) //typecast everything from mob to carbon from this point onwards
		return FALSE
	if(req_human && !ishuman(user))
		to_chat(user, "<span class='warning'>We cannot do that in this form!</span>")
		return FALSE
	var/datum/antagonist/changeling/c = user.mind.has_antag_datum(/datum/antagonist/changeling)
	if(c.chem_charges < chemical_cost)
		to_chat(user, "<span class='warning'>We require at least [chemical_cost] unit\s of chemicals to do that!</span>")
		return FALSE
	if(c.absorbedcount < req_dna)
		to_chat(user, "<span class='warning'>We require at least [req_dna] sample\s of compatible DNA.</span>")
		return FALSE
	if(c.trueabsorbs < req_absorbs)
		to_chat(user, "<span class='warning'>We require at least [req_absorbs] sample\s of DNA gained through our Absorb ability.</span>")
		return FALSE
	if(req_stat < user.stat)
		to_chat(user, "<span class='warning'>We are incapacitated.</span>")
		return FALSE
	if((HAS_TRAIT(user, TRAIT_DEATHCOMA)) && (!ignores_fakedeath))
		to_chat(user, "<span class='warning'>We are incapacitated.</span>")
		return FALSE
	return TRUE

/datum/action/changeling/proc/can_be_used_by(mob/user)
	if(!user || QDELETED(user))
		return 0
	if(!ishuman(user) && !ismonkey(user))
		return FALSE
	if(req_human && !ishuman(user))
		return FALSE
	return TRUE
