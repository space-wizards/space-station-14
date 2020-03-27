/datum/round_event_control/vent_clog
	name = "Clogged Vents: Normal"
	typepath = /datum/round_event/vent_clog
	weight = 10
	max_occurrences = 3
	min_players = 10

/datum/round_event/vent_clog
	announceWhen	= 1
	startWhen		= 5
	endWhen			= 35
	var/interval 	= 2
	var/list/vents  = list()
	var/randomProbability = 1
	var/reagentsAmount = 100
	var/list/saferChems = list(/datum/reagent/water,/datum/reagent/carbon,/datum/reagent/consumable/flour,/datum/reagent/space_cleaner,/datum/reagent/consumable/nutriment,/datum/reagent/consumable/condensedcapsaicin,/datum/reagent/drug/mushroomhallucinogen,/datum/reagent/lube,/datum/reagent/glitter/pink,/datum/reagent/cryptobiolin,
						 /datum/reagent/toxin/plantbgone,/datum/reagent/blood,/datum/reagent/medicine/C2/multiver,/datum/reagent/drug/space_drugs,/datum/reagent/medicine/morphine,/datum/reagent/water/holywater,/datum/reagent/consumable/ethanol,/datum/reagent/consumable/hot_coco,/datum/reagent/toxin/acid,/datum/reagent/toxin/mindbreaker,/datum/reagent/toxin/rotatium,/datum/reagent/bluespace,
						 /datum/reagent/pax,/datum/reagent/consumable/laughter,/datum/reagent/concentrated_barbers_aid,/datum/reagent/baldium,/datum/reagent/colorful_reagent,/datum/reagent/peaceborg/confuse,/datum/reagent/peaceborg/tire,/datum/reagent/consumable/sodiumchloride,/datum/reagent/consumable/ethanol/beer,/datum/reagent/hair_dye,/datum/reagent/consumable/sugar,/datum/reagent/glitter/white,/datum/reagent/growthserum)
	//needs to be chemid unit checked at some point

/datum/round_event/vent_clog/announce()
	priority_announce("The scrubbers network is experiencing a backpressure surge. Some ejection of contents may occur.", "Atmospherics alert")

/datum/round_event/vent_clog/setup()
	endWhen = rand(25, 100)
	for(var/obj/machinery/atmospherics/components/unary/vent_scrubber/temp_vent in GLOB.machines)
		var/turf/T = get_turf(temp_vent)
		if(T && is_station_level(T.z) && !temp_vent.welded)
			vents += temp_vent
	if(!vents.len)
		return kill()

/datum/round_event/vent_clog/start()
	for(var/obj/machinery/atmospherics/components/unary/vent in vents)
		if(vent && vent.loc)
			var/datum/reagents/R = new/datum/reagents(1000)
			R.my_atom = vent
			if (prob(randomProbability))
				R.add_reagent(get_random_reagent_id(), reagentsAmount)
			else
				R.add_reagent(pick(saferChems), reagentsAmount)

			R.create_foam(/datum/effect_system/foam_spread,200)

			var/cockroaches = prob(33) ? 3 : 0
			while(cockroaches)
				new /mob/living/simple_animal/cockroach(get_turf(vent))
				cockroaches--
		CHECK_TICK

/datum/round_event_control/vent_clog/threatening
	name = "Clogged Vents: Threatening"
	typepath = /datum/round_event/vent_clog/threatening
	weight = 4
	min_players = 25
	max_occurrences = 1
	earliest_start = 35 MINUTES

/datum/round_event/vent_clog/threatening
	randomProbability = 10
	reagentsAmount = 200

/datum/round_event_control/vent_clog/catastrophic
	name = "Clogged Vents: Catastrophic"
	typepath = /datum/round_event/vent_clog/catastrophic
	weight = 2
	min_players = 35
	max_occurrences = 1
	earliest_start = 45 MINUTES

/datum/round_event/vent_clog/catastrophic
	randomProbability = 30
	reagentsAmount = 250

/datum/round_event_control/vent_clog/beer
	name = "Foamy beer stationwide"
	typepath = /datum/round_event/vent_clog/beer
	max_occurrences = 0

/datum/round_event_control/vent_clog/plasma_decon
	name = "Plasma decontamination"
	typepath = /datum/round_event/vent_clog/plasma_decon
	max_occurrences = 0

/datum/round_event/vent_clog/beer
	reagentsAmount = 100

/datum/round_event/vent_clog/beer/announce()
	priority_announce("The scrubbers network is experiencing an unexpected surge of pressurized beer. Some ejection of contents may occur.", "Atmospherics alert")

/datum/round_event/vent_clog/beer/start()
	for(var/obj/machinery/atmospherics/components/unary/vent in vents)
		if(vent && vent.loc)
			var/datum/reagents/R = new/datum/reagents(1000)
			R.my_atom = vent
			R.add_reagent(/datum/reagent/consumable/ethanol/beer, reagentsAmount)

			R.create_foam(200)
		CHECK_TICK

/datum/round_event/vent_clog/plasma_decon/announce()
	priority_announce("We are deploying an experimental plasma decontamination system. Please stand away from the vents and do not breathe the smoke that comes out.", "Central Command Update")

/datum/round_event/vent_clog/plasma_decon/start()
	for(var/obj/machinery/atmospherics/components/unary/vent in vents)
		if(vent && vent.loc)
			var/datum/effect_system/smoke_spread/freezing/decon/smoke = new
			smoke.set_up(7, get_turf(vent), 7)
			smoke.start()
		CHECK_TICK
