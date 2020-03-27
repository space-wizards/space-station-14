

/*****BRUTE*****/

/datum/chemical_reaction/helbital
	name = "helbital"
	id = /datum/reagent/medicine/C2/helbital
	results = list(/datum/reagent/medicine/C2/helbital = 3)
	required_reagents = list(/datum/reagent/consumable/sugar = 1, /datum/reagent/fluorine = 1, /datum/reagent/carbon = 1)
	mix_message = "The mixture turns into a thick, yellow powder."

/datum/chemical_reaction/libital
	name = "Libital"
	id = /datum/reagent/medicine/C2/libital
	results = list(/datum/reagent/medicine/C2/libital = 3)
	required_reagents = list(/datum/reagent/phenol = 1, /datum/reagent/oxygen = 1, /datum/reagent/nitrogen = 1)

/*****BURN*****/

/datum/chemical_reaction/lenturi
	name = "Lenturi"
	id = /datum/reagent/medicine/C2/lenturi
	results = list(/datum/reagent/medicine/C2/lenturi = 5)
	required_reagents = list(/datum/reagent/ammonia = 1, /datum/reagent/silver = 1, /datum/reagent/sulfur = 1, /datum/reagent/oxygen = 1, /datum/reagent/chlorine = 1)

/datum/chemical_reaction/aiuri
	name = "Aiuri"
	id = /datum/reagent/medicine/C2/aiuri
	results = list(/datum/reagent/medicine/C2/aiuri = 4)
	required_reagents = list(/datum/reagent/ammonia = 1, /datum/reagent/toxin/acid = 1, /datum/reagent/hydrogen = 2)

/*****OXY*****/

/datum/chemical_reaction/convermol
	name = "Convermol"
	id = /datum/reagent/medicine/C2/convermol
	results = list(/datum/reagent/medicine/C2/convermol = 3)
	required_reagents = list(/datum/reagent/hydrogen = 1, /datum/reagent/fluorine = 1, /datum/reagent/fuel/oil = 1)
	required_temp = 370
	mix_message = "The mixture rapidly turns into a dense pink liquid."

/datum/chemical_reaction/tirimol
	name = "Tirimol"
	id = /datum/reagent/medicine/C2/tirimol
	results = list(/datum/reagent/medicine/C2/tirimol = 5)
	required_reagents = list(/datum/reagent/nitrogen = 3, /datum/reagent/acetone = 2)
	required_catalysts = list(/datum/reagent/toxin/acid = 1)

/*****TOX*****/

/datum/chemical_reaction/seiver
	name = "Seiver"
	id = /datum/reagent/medicine/C2/seiver
	results = list(/datum/reagent/medicine/C2/seiver = 3)
	required_reagents = list(/datum/reagent/nitrogen = 1, /datum/reagent/potassium = 1, /datum/reagent/aluminium = 1)

/datum/chemical_reaction/multiver
	name = "Multiver"
	id = /datum/reagent/medicine/C2/multiver
	results = list(/datum/reagent/medicine/C2/multiver = 2)
	required_reagents = list(/datum/reagent/ash = 1, /datum/reagent/consumable/sodiumchloride = 1)
	mix_message = "The mixture yields a fine black powder."
	required_temp = 380

/datum/chemical_reaction/syriniver
	name = "Syriniver"
	id = /datum/reagent/medicine/C2/syriniver
	results = list(/datum/reagent/medicine/C2/syriniver = 5)
	required_reagents = list(/datum/reagent/sulfur = 1, /datum/reagent/fluorine = 1, /datum/reagent/toxin = 1, /datum/reagent/nitrous_oxide = 2)

/datum/chemical_reaction/penthrite
	name = "Penthrite"
	id  = /datum/reagent/medicine/C2/penthrite
	results = list(/datum/reagent/medicine/C2/penthrite = 4)
	required_reagents = list(/datum/reagent/pentaerythritol = 4, /datum/reagent/acetone = 1,  /datum/reagent/toxin/acid/nitracid = 1)
