#define CHANGELING_PHEROMONE_MIN_DISTANCE 10 //More generous than the agent pinpointer because you don't know who you're looking for.
#define CHANGELING_PHEROMONE_MAX_DISTANCE 25 //They can smell your fear a mile away.  Well, 50 meters.
#define CHANGELING_PHEROMONE_PING_TIME 20 //2s update time.


/datum/action/changeling/pheromone_receptors
	name = "Pheromone Receptors"
	desc = "We attune our senses to track other changelings by scent.  The closer they are, the easier we can find them."
	helptext = "We will know the general direction of nearby changelings, with closer scents being stronger.  Our chemical generation is slowed while this is active."
	chemical_cost = 0 //Reduces regain rate while active.
	dna_cost = 2
	var/receptors_active = FALSE

/datum/action/changeling/pheromone_receptors/sting_action(mob/living/carbon/user)
	..()
	var/datum/antagonist/changeling/changeling = user.mind.has_antag_datum(/datum/antagonist/changeling)
	if(!receptors_active)
		to_chat(user, "<span class='warning'>We search for the scent of any nearby changelings.</span>")
		changeling.chem_recharge_slowdown += 0.5
		user.apply_status_effect(/datum/status_effect/agent_pinpointer/changeling)
	else
		to_chat(user, "<span class='notice'>We stop searching for now.</span>")
		changeling.chem_recharge_slowdown -= 0.5
		user.remove_status_effect(/datum/status_effect/agent_pinpointer/changeling)

	receptors_active = !receptors_active

//Modified IA pinpointer - Points to the NEAREST changeling, but will only get you within a few tiles of the target.
//You'll still have to rely on intuition and observation to make the identification.  Lings can 'hide' in public places.
/datum/status_effect/agent_pinpointer/changeling
	alert_type = /obj/screen/alert/status_effect/agent_pinpointer/changeling
	minimum_range = CHANGELING_PHEROMONE_MIN_DISTANCE
	tick_interval = CHANGELING_PHEROMONE_PING_TIME
	range_fuzz_factor = 0

/datum/status_effect/agent_pinpointer/changeling/scan_for_target()
	var/turf/my_loc = get_turf(owner)

	var/list/mob/living/carbon/changelings = list()

	for(var/mob/living/carbon/C in GLOB.alive_mob_list)
		if(C != owner && C.mind)
			var/datum/antagonist/changeling/antag_datum = C.mind.has_antag_datum(/datum/antagonist/changeling)
			if(istype(antag_datum))
				var/their_loc = get_turf(C)
				var/distance = get_dist_euclidian(my_loc, their_loc)
				if (distance < CHANGELING_PHEROMONE_MAX_DISTANCE)
					changelings[C] = (CHANGELING_PHEROMONE_MAX_DISTANCE ** 2) - (distance ** 2)

	if(changelings.len)
		scan_target = pickweight(changelings) //Point at a 'random' changeling, biasing heavily towards closer ones.
	else
		scan_target = null


/obj/screen/alert/status_effect/agent_pinpointer/changeling
	name = "Pheromone Scent"
	desc = "The nose always knows."
