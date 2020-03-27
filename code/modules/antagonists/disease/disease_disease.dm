/datum/disease/advance/sentient_disease
	form = "Virus"
	name = "Sentient Virus"
	desc = "An apparently sentient virus, extremely adaptable and resistant to outside sources of mutation."
	viable_mobtypes = list(/mob/living/carbon/human)
	mutable = FALSE
	var/mob/camera/disease/overmind

/datum/disease/advance/sentient_disease/New()
	..()
	GLOB.sentient_disease_instances += src

/datum/disease/advance/sentient_disease/Destroy()
	. = ..()
	GLOB.sentient_disease_instances -= src

/datum/disease/advance/sentient_disease/remove_disease()
	if(overmind)
		overmind.remove_infection(src)
	..()

/datum/disease/advance/sentient_disease/infect(var/mob/living/infectee, make_copy = TRUE)
	if(make_copy && overmind && (overmind.disease_template != src))
		overmind.disease_template.infect(infectee, TRUE) //get an updated version of the virus
	else
		..()


/datum/disease/advance/sentient_disease/IsSame(datum/disease/D)
	if(istype(D, /datum/disease/advance/sentient_disease))
		var/datum/disease/advance/sentient_disease/V = D
		if(V.overmind == overmind)
			return TRUE
	return FALSE


/datum/disease/advance/sentient_disease/Copy()
	var/datum/disease/advance/sentient_disease/D = ..()
	D.overmind = overmind
	return D

/datum/disease/advance/sentient_disease/after_add()
	if(overmind)
		overmind.add_infection(src)


/datum/disease/advance/sentient_disease/GetDiseaseID()
	return "[type]|[overmind ? overmind.tag : null]"

/datum/disease/advance/sentient_disease/GenerateCure()
	if(cures.len)
		return
	var/list/not_used = advance_cures.Copy()
	not_used.Cut(1, 6)	// Removes the first five tiers of cures.
	cures = list(pick(pick_n_take(not_used)), pick(pick_n_take(not_used)))

	// Get the cure name from the cure_id
	var/datum/reagent/D1 = GLOB.chemical_reagents_list[cures[1]]
	var/datum/reagent/D2 = GLOB.chemical_reagents_list[cures[2]]
	cure_text = "[D1.name] and [D2.name]"
