/*
Abilities that can be purchased by disease mobs. Most are just passive symptoms that will be
added to their disease, but some are active abilites that affect only the target the overmind
is currently following.
*/

GLOBAL_LIST_INIT(disease_ability_singletons, list(
new /datum/disease_ability/action/cough,
new /datum/disease_ability/action/sneeze,
new /datum/disease_ability/action/infect,
new /datum/disease_ability/symptom/mild/cough,
new /datum/disease_ability/symptom/mild/sneeze,
new /datum/disease_ability/symptom/medium/shedding,
new /datum/disease_ability/symptom/medium/beard,
new /datum/disease_ability/symptom/medium/hallucigen,
new /datum/disease_ability/symptom/medium/choking,
new /datum/disease_ability/symptom/medium/confusion,
new /datum/disease_ability/symptom/medium/vomit,
new /datum/disease_ability/symptom/medium/voice_change,
new /datum/disease_ability/symptom/medium/visionloss,
new /datum/disease_ability/symptom/medium/deafness,
new /datum/disease_ability/symptom/powerful/narcolepsy,
new /datum/disease_ability/symptom/medium/fever,
new /datum/disease_ability/symptom/medium/shivering,
new /datum/disease_ability/symptom/medium/headache,
new /datum/disease_ability/symptom/medium/nano_boost,
new /datum/disease_ability/symptom/medium/nano_destroy,
new /datum/disease_ability/symptom/medium/viraladaptation,
new /datum/disease_ability/symptom/medium/viralevolution,
new /datum/disease_ability/symptom/medium/disfiguration,
new /datum/disease_ability/symptom/medium/polyvitiligo,
new /datum/disease_ability/symptom/medium/itching,
new /datum/disease_ability/symptom/medium/heal/weight_loss,
new /datum/disease_ability/symptom/medium/heal/sensory_restoration,
new /datum/disease_ability/symptom/medium/heal/mind_restoration,
new /datum/disease_ability/symptom/powerful/fire,
new /datum/disease_ability/symptom/powerful/flesh_eating,
new /datum/disease_ability/symptom/powerful/genetic_mutation,
new /datum/disease_ability/symptom/powerful/inorganic_adaptation,
new /datum/disease_ability/symptom/powerful/heal/starlight,
new /datum/disease_ability/symptom/powerful/heal/oxygen,
new /datum/disease_ability/symptom/powerful/heal/chem,
new /datum/disease_ability/symptom/powerful/heal/metabolism,
new /datum/disease_ability/symptom/powerful/heal/dark,
new /datum/disease_ability/symptom/powerful/heal/water,
new /datum/disease_ability/symptom/powerful/heal/plasma,
new /datum/disease_ability/symptom/powerful/heal/radiation,
new /datum/disease_ability/symptom/powerful/heal/coma,
new /datum/disease_ability/symptom/powerful/youth
))

/datum/disease_ability
	var/name
	var/cost = 0
	var/required_total_points = 0
	var/start_with = FALSE
	var/short_desc = ""
	var/long_desc = ""
	var/stat_block = ""
	var/threshold_block = list()
	var/category = ""

	var/list/symptoms
	var/list/actions

/datum/disease_ability/New()
	..()
	if(symptoms)
		var/stealth = 0
		var/resistance = 0
		var/stage_speed = 0
		var/transmittable = 0
		for(var/T in symptoms)
			var/datum/symptom/S = T
			stealth += initial(S.stealth)
			resistance += initial(S.resistance)
			stage_speed += initial(S.stage_speed)
			transmittable += initial(S.transmittable)
			threshold_block += initial(S.threshold_descs) 
			stat_block = "Resistance: [resistance]<br>Stealth: [stealth]<br>Stage Speed: [stage_speed]<br>Transmissibility: [transmittable]<br><br>"
			if(symptoms.len == 1) //lazy boy's dream
				name = initial(S.name)
				if(short_desc == "")
					short_desc = initial(S.desc)
				if(long_desc == "")
					long_desc = initial(S.desc)

/datum/disease_ability/proc/CanBuy(mob/camera/disease/D)
	if(world.time < D.next_adaptation_time)
		return FALSE
	if(!D.unpurchased_abilities[src])
		return FALSE
	return (D.points >= cost) && (D.total_points >= required_total_points)

/datum/disease_ability/proc/Buy(mob/camera/disease/D, silent = FALSE, trigger_cooldown = TRUE)
	if(!silent)
		to_chat(D, "<span class='notice'>Purchased [name].</span>")
	D.points -= cost
	D.unpurchased_abilities -= src
	if(trigger_cooldown)
		D.adapt_cooldown()
	D.purchased_abilities[src] = TRUE
	for(var/V in (D.disease_instances+D.disease_template))
		var/datum/disease/advance/sentient_disease/SD = V
		if(symptoms)
			for(var/T in symptoms)
				var/datum/symptom/S = new T()
				SD.symptoms += S
				S.OnAdd(SD)
				if(SD.processing)
					if(S.Start(SD))
						S.next_activation = world.time + rand(S.symptom_delay_min * 10, S.symptom_delay_max * 10)
			SD.Refresh()
	for(var/T in actions)
		var/datum/action/A = new T()
		A.Grant(D)


/datum/disease_ability/proc/CanRefund(mob/camera/disease/D)
	if(world.time < D.next_adaptation_time)
		return FALSE
	return D.purchased_abilities[src]

/datum/disease_ability/proc/Refund(mob/camera/disease/D, silent = FALSE, trigger_cooldown = TRUE)
	if(!silent)
		to_chat(D, "<span class='notice'>Refunded [name].</span>")
	D.points += cost
	D.unpurchased_abilities[src] = TRUE
	if(trigger_cooldown)
		D.adapt_cooldown()
	D.purchased_abilities -= src
	for(var/V in (D.disease_instances+D.disease_template))
		var/datum/disease/advance/sentient_disease/SD = V
		if(symptoms)
			for(var/T in symptoms)
				var/datum/symptom/S = locate(T) in SD.symptoms
				if(S)
					SD.symptoms -= S
					S.OnRemove(SD)
					if(SD.processing)
						S.End(SD)
					qdel(S)
			SD.Refresh()
	for(var/T in actions)
		var/datum/action/A = locate(T) in D.actions
		qdel(A)

//these sybtypes are for conveniently separating the different categories, they have no unique code.

/datum/disease_ability/action
	category = "Active"

/datum/disease_ability/symptom
	category = "Symptom"

//active abilities and their associated actions

/datum/disease_ability/action/cough
	name = "Voluntary Coughing"
	actions = list(/datum/action/cooldown/disease_cough)
	cost = 0
	required_total_points = 0
	start_with = TRUE
	short_desc = "Force the host you are following to cough, spreading your infection to those nearby."
	long_desc = "Force the host you are following to cough with extra force, spreading your infection to those within two meters of your host even if your transmissibility is low.<br>Cooldown: 10 seconds"


/datum/action/cooldown/disease_cough
	name = "Cough"
	icon_icon = 'icons/mob/actions/actions_minor_antag.dmi'
	button_icon_state = "cough"
	desc = "Force the host you are following to cough with extra force, spreading your infection to those within two meters of your host even if your transmissibility is low.<br>Cooldown: 10 seconds"
	cooldown_time = 100

/datum/action/cooldown/disease_cough/Trigger()
	if(!..())
		return FALSE
	var/mob/camera/disease/D = owner
	var/mob/living/L = D.following_host
	if(!L)
		return FALSE
	if(L.stat != CONSCIOUS)
		to_chat(D, "<span class='warning'>Your host must be conscious to cough.</span>")
		return FALSE
	to_chat(D, "<span class='notice'>You force [L.real_name] to cough.</span>")
	L.emote("cough")
	if(L.CanSpreadAirborneDisease()) //don't spread germs if they covered their mouth
		var/datum/disease/advance/sentient_disease/SD = D.hosts[L]
		SD.spread(2)
	StartCooldown()
	return TRUE


/datum/disease_ability/action/sneeze
	name = "Voluntary Sneezing"
	actions = list(/datum/action/cooldown/disease_sneeze)
	cost = 2
	required_total_points = 3
	short_desc = "Force the host you are following to sneeze, spreading your infection to those in front of them."
	long_desc = "Force the host you are following to sneeze with extra force, spreading your infection to any victims in a 4 meter cone in front of your host.<br>Cooldown: 20 seconds"

/datum/action/cooldown/disease_sneeze
	name = "Sneeze"
	icon_icon = 'icons/mob/actions/actions_minor_antag.dmi'
	button_icon_state = "sneeze"
	desc = "Force the host you are following to sneeze with extra force, spreading your infection to any victims in a 4 meter cone in front of your host even if your transmissibility is low.<br>Cooldown: 20 seconds"
	cooldown_time = 200

/datum/action/cooldown/disease_sneeze/Trigger()
	if(!..())
		return FALSE
	var/mob/camera/disease/D = owner
	var/mob/living/L = D.following_host
	if(!L)
		return FALSE
	if(L.stat != CONSCIOUS)
		to_chat(D, "<span class='warning'>Your host must be conscious to sneeze.</span>")
		return FALSE
	to_chat(D, "<span class='notice'>You force [L.real_name] to sneeze.</span>")
	L.emote("sneeze")
	if(L.CanSpreadAirborneDisease()) //don't spread germs if they covered their mouth
		var/datum/disease/advance/sentient_disease/SD = D.hosts[L]

		for(var/mob/living/M in oview(4, SD.affected_mob))
			if(is_A_facing_B(SD.affected_mob, M) && disease_air_spread_walk(get_turf(SD.affected_mob), get_turf(M)))
				M.AirborneContractDisease(SD, TRUE)

	StartCooldown()
	return TRUE


/datum/disease_ability/action/infect
	name = "Secrete Infection"
	actions = list(/datum/action/cooldown/disease_infect)
	cost = 2
	required_total_points = 3
	short_desc = "Cause all objects your host is touching to become infectious for a limited time, spreading your infection to anyone who touches them."
	long_desc = "Cause the host you are following to excrete an infective substance from their pores, causing all objects touching their skin to transmit your infection to anyone who touches them for the next 30 seconds. This includes the floor, if they are not wearing shoes, and any items they are holding, if they are not wearing gloves.<br>Cooldown: 40 seconds"

/datum/action/cooldown/disease_infect
	name = "Secrete Infection"
	icon_icon = 'icons/mob/actions/actions_minor_antag.dmi'
	button_icon_state = "infect"
	desc = "Cause the host you are following to excrete an infective substance from their pores, causing all objects touching their skin to transmit your infection to anyone who touches them for the next 30 seconds.<br>Cooldown: 40 seconds"
	cooldown_time = 400

/datum/action/cooldown/disease_infect/Trigger()
	if(!..())
		return FALSE
	var/mob/camera/disease/D = owner
	var/mob/living/carbon/human/H = D.following_host
	if(!H)
		return FALSE
	for(var/V in H.get_equipped_items(FALSE))
		var/obj/O = V
		O.AddComponent(/datum/component/infective, D.disease_template, 300)
	//no shoes? infect the floor.
	if(!H.shoes)
		var/turf/T = get_turf(H)
		if(T && !isspaceturf(T))
			T.AddComponent(/datum/component/infective, D.disease_template, 300)
	//no gloves? infect whatever we are holding.
	if(!H.gloves)
		for(var/V in H.held_items)
			if(!V)
				continue
			var/obj/O = V
			O.AddComponent(/datum/component/infective, D.disease_template, 300)
	StartCooldown()
	return TRUE

/*******************BASE SYMPTOM TYPES*******************/
// cost is for convenience and can be changed. If you're changing req_tot_points then don't use the subtype...
//healing costs more so you have to techswitch from naughty disease otherwise we'd have friendly disease for easy greentext (no fun!)

/datum/disease_ability/symptom/mild
	cost = 2
	required_total_points = 4
	category = "Symptom (Weak)"

/datum/disease_ability/symptom/medium
	cost = 4
	required_total_points = 8
	category = "Symptom"

/datum/disease_ability/symptom/medium/heal
	cost = 5
	category = "Symptom (+)"

/datum/disease_ability/symptom/powerful
	cost = 4
	required_total_points = 16
	category = "Symptom (Strong)"

/datum/disease_ability/symptom/powerful/heal
	cost = 8
	category = "Symptom (Strong+)"

/******MILD******/

/datum/disease_ability/symptom/mild/cough
	name = "Involuntary Coughing"
	symptoms = list(/datum/symptom/cough)
	short_desc = "Cause victims to cough intermittently."
	long_desc = "Cause victims to cough intermittently, spreading your infection."

/datum/disease_ability/symptom/mild/sneeze
	name = "Involuntary Sneezing"
	symptoms = list(/datum/symptom/sneeze)
	short_desc = "Cause victims to sneeze intermittently."
	long_desc = "Cause victims to sneeze intermittently, spreading your infection and also increasing transmissibility and resistance, at the cost of stealth."

/******MEDIUM******/

/datum/disease_ability/symptom/medium/shedding
	symptoms = list(/datum/symptom/shedding)

/datum/disease_ability/symptom/medium/beard
	symptoms = list(/datum/symptom/beard)
	short_desc = "Cause all victims to grow a luscious beard."
	long_desc = "Cause all victims to grow a luscious beard. Ineffective against Santa Claus."

/datum/disease_ability/symptom/medium/hallucigen
	symptoms = list(/datum/symptom/hallucigen)
	short_desc = "Cause victims to hallucinate."
	long_desc = "Cause victims to hallucinate. Decreases stats, especially resistance."

/datum/disease_ability/symptom/medium/choking
	symptoms = list(/datum/symptom/choking)
	short_desc = "Cause victims to choke."
	long_desc = "Cause victims to choke, threatening asphyxiation. Decreases stats, especially transmissibility."

/datum/disease_ability/symptom/medium/confusion
	symptoms = list(/datum/symptom/confusion)
	short_desc = "Cause victims to become confused."
	long_desc = "Cause victims to become confused intermittently."

/datum/disease_ability/symptom/medium/vomit
	symptoms = list(/datum/symptom/vomit)
	short_desc = "Cause victims to vomit."
	long_desc = "Cause victims to vomit. Slightly increases transmissibility. Vomiting also also causes the victims to lose nutrition and removes some toxin damage."

/datum/disease_ability/symptom/medium/voice_change
	symptoms = list(/datum/symptom/voice_change)
	short_desc = "Change the voice of victims."
	long_desc = "Change the voice of victims, causing confusion in communications."

/datum/disease_ability/symptom/medium/visionloss
	symptoms = list(/datum/symptom/visionloss)
	short_desc = "Damage the eyes of victims, eventually causing blindness."
	long_desc = "Damage the eyes of victims, eventually causing blindness. Decreases all stats."

/datum/disease_ability/symptom/medium/deafness
	symptoms = list(/datum/symptom/deafness)

/datum/disease_ability/symptom/medium/fever
	symptoms = list(/datum/symptom/fever)

/datum/disease_ability/symptom/medium/shivering
	symptoms = list(/datum/symptom/shivering)

/datum/disease_ability/symptom/medium/headache
	symptoms = list(/datum/symptom/headache)

/datum/disease_ability/symptom/medium/nano_boost
	symptoms = list(/datum/symptom/nano_boost)

/datum/disease_ability/symptom/medium/nano_destroy
	symptoms = list(/datum/symptom/nano_destroy)

/datum/disease_ability/symptom/medium/viraladaptation
	symptoms = list(/datum/symptom/viraladaptation)
	short_desc = "Cause your infection to become more resistant to detection and eradication."
	long_desc = "Cause your infection to mimic the function of normal body cells, becoming much harder to spot and to eradicate, but reducing its speed."

/datum/disease_ability/symptom/medium/viralevolution
	symptoms = list(/datum/symptom/viralevolution)

/datum/disease_ability/symptom/medium/polyvitiligo
	symptoms = list(/datum/symptom/polyvitiligo)

/datum/disease_ability/symptom/medium/disfiguration
	symptoms = list(/datum/symptom/disfiguration)

/datum/disease_ability/symptom/medium/itching
	symptoms = list(/datum/symptom/itching)
	short_desc = "Cause victims to itch."
	long_desc = "Cause victims to itch, increasing all stats except stealth."

/datum/disease_ability/symptom/medium/heal/weight_loss
	symptoms = list(/datum/symptom/weight_loss)
	short_desc = "Cause victims to lose weight."
	long_desc = "Cause victims to lose weight, and make it almost impossible for them to gain nutrition from food. Reduced nutrition allows your infection to spread more easily from hosts, especially by sneezing."

/datum/disease_ability/symptom/medium/heal/sensory_restoration
	symptoms = list(/datum/symptom/sensory_restoration)
	short_desc = "Regenerate eye and ear damage of victims."
	long_desc = "Regenerate eye and ear damage of victims."

/datum/disease_ability/symptom/medium/heal/mind_restoration
	symptoms = list(/datum/symptom/mind_restoration)

/******POWERFUL******/

/datum/disease_ability/symptom/powerful/fire
	symptoms = list(/datum/symptom/fire)

/datum/disease_ability/symptom/powerful/flesh_eating
	symptoms = list(/datum/symptom/flesh_eating)

/datum/disease_ability/symptom/powerful/genetic_mutation
	symptoms = list(/datum/symptom/genetic_mutation)
	cost = 8

/datum/disease_ability/symptom/powerful/inorganic_adaptation
	symptoms = list(/datum/symptom/inorganic_adaptation)

/datum/disease_ability/symptom/powerful/narcolepsy
	symptoms = list(/datum/symptom/narcolepsy)

/datum/disease_ability/symptom/powerful/youth
	symptoms = list(/datum/symptom/youth)
	short_desc = "Cause victims to become eternally young."
	long_desc = "Cause victims to become eternally young. Provides boosts to all stats except transmissibility."

/****HEALING SUBTYPE****/

/datum/disease_ability/symptom/powerful/heal/starlight
	symptoms = list(/datum/symptom/heal/starlight)

/datum/disease_ability/symptom/powerful/heal/oxygen
	symptoms = list(/datum/symptom/oxygen)

/datum/disease_ability/symptom/powerful/heal/chem
	symptoms = list(/datum/symptom/heal/chem)

/datum/disease_ability/symptom/powerful/heal/metabolism
	symptoms = list(/datum/symptom/heal/metabolism)
	short_desc = "Increase the metabolism of victims, causing them to process chemicals and grow hungry faster."
	long_desc = "Increase the metabolism of victims, causing them to process chemicals twice as fast and grow hungry more quickly."

/datum/disease_ability/symptom/powerful/heal/dark
	symptoms = list(/datum/symptom/heal/darkness)

/datum/disease_ability/symptom/powerful/heal/water
	symptoms = list(/datum/symptom/heal/water)

/datum/disease_ability/symptom/powerful/heal/plasma
	symptoms = list(/datum/symptom/heal/plasma)

/datum/disease_ability/symptom/powerful/heal/radiation
	symptoms = list(/datum/symptom/heal/radiation)

/datum/disease_ability/symptom/powerful/heal/coma
	symptoms = list(/datum/symptom/heal/coma)
	short_desc = "Cause victims to fall into a healing coma when hurt."
	long_desc = "Cause victims to fall into a healing coma when hurt."
