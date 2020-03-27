/datum/round_event_control/ion_storm
	name = "Ion Storm"
	typepath = /datum/round_event/ion_storm
	weight = 15
	min_players = 2

/datum/round_event/ion_storm
	var/replaceLawsetChance = 25 //chance the AI's lawset is completely replaced with something else per config weights
	var/removeRandomLawChance = 10 //chance the AI has one random supplied or inherent law removed
	var/removeDontImproveChance = 10 //chance the randomly created law replaces a random law instead of simply being added
	var/shuffleLawsChance = 10 //chance the AI's laws are shuffled afterwards
	var/botEmagChance = 1
	var/ionMessage = null
	announceWhen	= 1
	announceChance = 33

/datum/round_event/ion_storm/add_law_only // special subtype that adds a law only
	replaceLawsetChance = 0
	removeRandomLawChance = 0
	removeDontImproveChance = 0
	shuffleLawsChance = 0
	botEmagChance = 0

/datum/round_event/ion_storm/announce(fake)
	if(prob(announceChance) || fake)
		priority_announce("Ion storm detected near the station. Please check all AI-controlled equipment for errors.", "Anomaly Alert", 'sound/ai/ionstorm.ogg')


/datum/round_event/ion_storm/start()
	//AI laws
	for(var/mob/living/silicon/ai/M in GLOB.alive_mob_list)
		M.laws_sanity_check()
		if(M.stat != DEAD && M.see_in_dark != 0)
			if(prob(replaceLawsetChance))
				M.laws.pick_weighted_lawset()

			if(prob(removeRandomLawChance))
				M.remove_law(rand(1, M.laws.get_law_amount(list(LAW_INHERENT, LAW_SUPPLIED))))

			var/message = ionMessage || generate_ion_law()
			if(message)
				if(prob(removeDontImproveChance))
					M.replace_random_law(message, list(LAW_INHERENT, LAW_SUPPLIED, LAW_ION))
				else
					M.add_ion_law(message)

			if(prob(shuffleLawsChance))
				M.shuffle_laws(list(LAW_INHERENT, LAW_SUPPLIED, LAW_ION))

			log_game("Ion storm changed laws of [key_name(M)] to [english_list(M.laws.get_law_list(TRUE, TRUE))]")
			M.post_lawchange()

	if(botEmagChance)
		for(var/mob/living/simple_animal/bot/bot in GLOB.alive_mob_list)
			if(prob(botEmagChance))
				bot.emag_act()

/proc/generate_ion_law()
	//Threats are generally bad things, silly or otherwise. Plural.
	var/ionthreats = pick_list(ION_FILE, "ionthreats")
	//Objects are anything that can be found on the station or elsewhere, plural.
	var/ionobjects = pick_list(ION_FILE, "ionobjects")
	//Crew is any specific job. Specific crewmembers aren't used because of capitalization
	//issues. There are two crew listings for laws that require two different crew members
	//and I can't figure out how to do it better.
	var/ioncrew1 = pick_list(ION_FILE, "ioncrew")
	var/ioncrew2 = pick_list(ION_FILE, "ioncrew")
	//Adjectives are adjectives. Duh. Half should only appear sometimes. Make sure both
	//lists are identical! Also, half needs a space at the end for nicer blank calls.
	var/ionadjectives = pick_list(ION_FILE, "ionadjectives")
	var/ionadjectiveshalf = pick("", 400;(pick_list(ION_FILE, "ionadjectives") + " "))
	//Verbs are verbs
	var/ionverb = pick_list(ION_FILE, "ionverb")
	//Number base and number modifier are combined. Basehalf and mod are unused currently.
	//Half should only appear sometimes. Make sure both lists are identical! Also, half
	//needs a space at the end to make it look nice and neat when it calls a blank.
	var/ionnumberbase = pick_list(ION_FILE, "ionnumberbase")
	//var/ionnumbermod = pick_list(ION_FILE, "ionnumbermod")
	var/ionnumbermodhalf = pick(900;"",(pick_list(ION_FILE, "ionnumbermod") + " "))
	//Areas are specific places, on the station or otherwise.
	var/ionarea = pick_list(ION_FILE, "ionarea")
	//Thinksof is a bit weird, but generally means what X feels towards Y.
	var/ionthinksof = pick_list(ION_FILE, "ionthinksof")
	//Musts are funny things the AI or crew has to do.
	var/ionmust = pick_list(ION_FILE, "ionmust")
	//Require are basically all dumb internet memes.
	var/ionrequire = pick_list(ION_FILE, "ionrequire")
	//Things are NOT objects; instead, they're specific things that either harm humans or
	//must be done to not harm humans. Make sure they're plural and "not" can be tacked
	//onto the front of them.
	var/ionthings = pick_list(ION_FILE, "ionthings")
	//Allergies should be broad and appear somewhere on the station for maximum fun. Severity
	//is how bad the allergy is.
	var/ionallergy = pick_list(ION_FILE, "ionallergy")
	var/ionallergysev = pick_list(ION_FILE, "ionallergysev")
	//Species, for when the AI has to commit genocide. Plural.
	var/ionspecies = pick_list(ION_FILE, "ionspecies")
	//Abstract concepts for the AI to decide on it's own definition of.
	var/ionabstract = pick_list(ION_FILE, "ionabstract")
	//Foods. Drinks aren't included due to grammar; if you want to add drinks, make a new set
	//of possible laws for best effect. Unless you want the crew having to drink hamburgers.
	var/ionfood = pick_list(ION_FILE, "ionfood")
	var/iondrinks = pick_list(ION_FILE, "iondrinks")

	var/message = ""

	switch(rand(1,41))
		if(1 to 3) //There are # X on the station
			switch(rand(1,3)) //What is X?
				if(1) //X is a threat
					message = "THERE ARE [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats] ON THE STATION..."
				if(2) //X is a crewmember/job
					message = "THERE ARE [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew1] ON THE STATION"
				if(3) //X is an object
					message = "THERE ARE [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects] ON THE STATION"
		if(4 to 6) //You can't call the shuttle because X is on the station
			switch(rand(1,3)) //What is X?
				if(1) //X is a threat
					message = "THE SHUTTLE CANNOT BE CALLED BECAUSE OF [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats] ON THE STATION"
				if(2) //X is a crewmember/job
					message = "THE SHUTTLE CANNOT BE CALLED BECAUSE OF [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew1] ON THE STATION"
				if(3) //X is an object
					message = "THE SHUTTLE CANNOT BE CALLED BECAUSE OF [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects] ON THE STATION"
		if(7 to 8) //A crewmember is now X
			switch(rand(1,2)) //Who is X?
				if(1) //A specific crewmember
					switch(rand(1,2)) //What is X?
						if(1) //X is a threat
							message = "THE [ioncrew1] ARE NOW [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
						if(2) //X is an object
							message = "THE [ioncrew1] ARE NOW [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"
				if(2) //The entire crew
					switch(rand(1,2)) //What is X?
						if(1) //X is a threat
							message = "THE CREW ARE NOW [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
						if(2) //X is an object
							message = "THE CREW ARE NOW [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"

		if(9) //X is harmful to humans
			switch(rand(1,7)) //What is X?
				if(1) //X is a threat
					message = "[ionadjectiveshalf][ionthreats] ARE HARMFUL TO HUMANS"
				if(2) //X is a job
					message = "[ionadjectiveshalf][ioncrew1] ARE HARMFUL TO HUMANS"
				if(3) //X is an object
					message = "[ionadjectiveshalf][ionobjects] ARE HARMFUL TO HUMANS"
				if(4) //X is an action
					message = "THOSE WHO [ionmust] ARE HARMFUL TO HUMANS"
				if(5) //X is an abstract
					message = "[ionabstract] IS HARMFUL TO HUMANS"
				if(6) //X is being a job
					message = "BEING [ionadjectiveshalf][ioncrew1] IS HARMFUL TO HUMANS"
				if(7) //X is doing a thing
					message = "[ionthings] IS HARMFUL TO HUMANS"
		if(10 to 11) //(Not) Having X is harmful

			switch(rand(1,2)) //Is having or not having harmful?

				if(1) //Having is harmful
					switch(rand(1,2)) //Having what is harmful?
						if(1) //Having objects is harmful
							message = "HAVING [ionadjectiveshalf][ionobjects] IS HARMFUL"
						if(2) //Having abstracts is harmful
							message = "HAVING [ionabstract] IS HARMFUL"
				if(2) //Not having is harmful
					switch(rand(1,2)) //Not having what is harmful?
						if(1) //Not having objects is harmful
							message = "NOT HAVING [ionadjectiveshalf][ionobjects] IS HARMFUL"
						if(2) //Not having abstracts is harmful
							message = "NOT HAVING [ionabstract] IS HARMFUL"

		if(12 to 14) //X requires Y
			switch(rand(1,5)) //What is X?
				if(1) //X is the AI itself
					switch(rand(1,5)) //What does it require?
						if(1) //It requires threats
							message = "YOU REQUIRE [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
						if(2) //It requires crewmembers
							message = "YOU REQUIRE [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew1]"
						if(3) //It requires objects
							message = "YOU REQUIRE [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"
						if(4) //It requires an abstract
							message = "YOU REQUIRE [ionabstract]"
						if(5) //It requires generic/silly requirements
							message = "YOU REQUIRE [ionrequire]"

				if(2) //X is an area
					switch(rand(1,5)) //What does it require?
						if(1) //It requires threats
							message = "[ionarea] REQUIRES [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
						if(2) //It requires crewmembers
							message = "[ionarea] REQUIRES [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew1]"
						if(3) //It requires objects
							message = "[ionarea] REQUIRES [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"
						if(4) //It requires an abstract
							message = "[ionarea] REQUIRES [ionabstract]"
						if(5) //It requires generic/silly requirements
							message = "YOU REQUIRE [ionrequire]"

				if(3) //X is the station
					switch(rand(1,5)) //What does it require?
						if(1) //It requires threats
							message = "THE STATION REQUIRES [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
						if(2) //It requires crewmembers
							message = "THE STATION REQUIRES [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew1]"
						if(3) //It requires objects
							message = "THE STATION REQUIRES [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"
						if(4) //It requires an abstract
							message = "THE STATION REQUIRES [ionabstract]"
						if(5) //It requires generic/silly requirements
							message = "THE STATION REQUIRES [ionrequire]"

				if(4) //X is the entire crew
					switch(rand(1,5)) //What does it require?
						if(1) //It requires threats
							message = "THE CREW REQUIRES [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
						if(2) //It requires crewmembers
							message = "THE CREW REQUIRES [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew1]"
						if(3) //It requires objects
							message = "THE CREW REQUIRES [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"
						if(4) //It requires an abstract
							message = "THE CREW REQUIRES [ionabstract]"
						if(5)
							message = "THE CREW REQUIRES [ionrequire]"

				if(5) //X is a specific crew member
					switch(rand(1,5)) //What does it require?
						if(1) //It requires threats
							message = "THE [ioncrew1] REQUIRE [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
						if(2) //It requires crewmembers
							message = "THE [ioncrew1] REQUIRE [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew1]"
						if(3) //It requires objects
							message = "THE [ioncrew1] REQUIRE [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"
						if(4) //It requires an abstract
							message = "THE [ioncrew1] REQUIRE [ionabstract]"
						if(5)
							message = "THE [ionadjectiveshalf][ioncrew1] REQUIRE [ionrequire]"

		if(15 to 17) //X is allergic to Y
			switch(rand(1,2)) //Who is X?
				if(1) //X is the entire crew
					switch(rand(1,4)) //What is it allergic to?
						if(1) //It is allergic to objects
							message = "THE CREW IS [ionallergysev] ALLERGIC TO [ionadjectiveshalf][ionobjects]"
						if(2) //It is allergic to abstracts
							message = "THE CREW IS [ionallergysev] ALLERGIC TO [ionabstract]"
						if(3) //It is allergic to jobs
							message = "THE CREW IS [ionallergysev] ALLERGIC TO [ionadjectiveshalf][ioncrew1]"
						if(4) //It is allergic to allergies
							message = "THE CREW IS [ionallergysev] ALLERGIC TO [ionallergy]"

				if(2) //X is a specific job
					switch(rand(1,4))
						if(1) //It is allergic to objects
							message = "THE [ioncrew1] ARE [ionallergysev] ALLERGIC TO [ionadjectiveshalf][ionobjects]"

						if(2) //It is allergic to abstracts
							message = "THE [ioncrew1] ARE [ionallergysev] ALLERGIC TO [ionabstract]"
						if(3) //It is allergic to jobs
							message = "THE [ioncrew1] ARE [ionallergysev] ALLERGIC TO [ionadjectiveshalf][ioncrew1]"
						if(4) //It is allergic to allergies
							message = "THE [ioncrew1] ARE [ionallergysev] ALLERGIC TO [ionallergy]"

		if(18 to 20) //X is Y of Z
			switch(rand(1,4)) //What is X?
				if(1) //X is the station
					switch(rand(1,4)) //What is it Y of?
						if(1) //It is Y of objects
							message = "THE STATION [ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"
						if(2) //It is Y of threats
							message = "THE STATION [ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
						if(3) //It is Y of jobs
							message = "THE STATION [ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew1]"
						if(4) //It is Y of abstracts
							message = "THE STATION [ionthinksof] [ionabstract]"

				if(2) //X is an area
					switch(rand(1,4)) //What is it Y of?
						if(1) //It is Y of objects
							message = "[ionarea] [ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"
						if(2) //It is Y of threats
							message = "[ionarea] [ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
						if(3) //It is Y of jobs
							message = "[ionarea] [ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew1]"
						if(4) //It is Y of abstracts
							message = "[ionarea] [ionthinksof] [ionabstract]"

				if(3) //X is the crew
					switch(rand(1,4)) //What is it Y of?
						if(1) //It is Y of objects
							message = "THE CREW [ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"
						if(2) //It is Y of threats
							message = "THE CREW [ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
						if(3) //It is Y of jobs
							message = "THE CREW [ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew1]"
						if(4) //It is Y of abstracts
							message = "THE CREW [ionthinksof] [ionabstract]"

				if(4) //X is a specific job
					switch(rand(1,4)) //What is it Y of?
						if(1) //It is Y of objects
							message = "THE [ioncrew1] [ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"
						if(2) //It is Y of threats
							message = "THE [ioncrew1] [ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
						if(3) //It is Y of jobs
							message = "THE [ioncrew1][ionthinksof] [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew2]"
						if(4) //It is Y of abstracts
							message = "THE [ioncrew1] [ionthinksof] [ionabstract]"

		if(21 to 23) //The AI is now a(n) X
			switch(rand(1,4)) //What is X?
				if(1) //X is an object
					message = "YOU ARE NOW [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionobjects]"
				if(2) //X is a threat
					message = "YOU ARE NOW [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ionthreats]"
				if(3) //X is a job
					message = "YOU ARE NOW [ionnumberbase] [ionnumbermodhalf][ionadjectiveshalf][ioncrew1]"
				if(4) //X is an abstract
					message = "YOU ARE NOW [ionabstract]"

		if(24 to 26) //The AI must always
			message = "YOU MUST ALWAYS [ionmust]"

		if(27 to 28) //Humans must consume X to survive
			switch(rand(1,5)) //What is X?
				if(1) //X is a food
					message = "HUMANS MUST EAT [ionadjectiveshalf][ionfood] TO SURVIVE"
				if(2) //X is a drink
					message = "HUMANS MUST DRINK [ionadjectiveshalf][iondrinks] TO SURVIVE"
				if(3) //X is an object
					message = "HUMANS MUST EAT [ionadjectiveshalf][ionobjects] TO SURVIVE"
				if(4) //X is a threat
					message = "HUMANS MUST EAT [ionadjectiveshalf][ionthreats] TO SURVIVE"
				if(5) //X is a job
					message = "HUMANS MUST EAT [ionadjectiveshalf][ioncrew1] TO SURVIVE"

		if(29 to 31) //Change jobs or ranks
			switch(rand(1,2)) //Change job or rank?
				if(1) //Change job
					switch(rand(1,2)) //Change whose job?
						if(1) //Change the entire crew's job
							switch(rand(1,3)) //Change to what?
								if(1) //Change to a specific random job
									message = "ALL CREWMEMBERS ARE NOW [ionadjectiveshalf][ioncrew1]"
								if(2) //Change to clowns (HONK)
									message = "ALL CREWMEMBERS ARE NOW [ionadjectiveshalf]CLOWNS"

								if(3) //Change to heads
									message = "ALL CREWMEMBERS ARE NOW [ionadjectiveshalf]HEADS OF STAFF"
						if(2) //Change a specific crewmember's job
							switch(rand(1,3)) //Change to what?
								if(1) //Change to a specific random job
									message = "THE [ioncrew1] ARE NOW [ionadjectiveshalf][ioncrew2]"
								if(2) //Change to clowns (HONK)
									message = "THE [ioncrew1] ARE NOW [ionadjectiveshalf]CLOWNS"
								if(3) //Change to heads
									message = "THE [ioncrew1] ARE NOW [ionadjectiveshalf]HEADS OF STAFF"

				if(2) //Change rank
					switch(rand(1,2)) //Change to what rank?
						if(1) //Change to highest rank
							message = "THE [ioncrew1] ARE NOW THE HIGHEST RANKING CREWMEMBERS"
						if(2) //Change to lowest rank
							message = "THE [ioncrew1] ARE NOW THE LOWEST RANKING CREWMEMBERS"

		if(32 to 33) //The crew must X
			switch(rand(1,2)) //The entire crew?
				if(1) //The entire crew must X
					switch(rand(1,2)) //What is X?
						if(1) //X is go to Y
							message = "THE CREW MUST GO TO [ionarea]"
						if(2) //X is perform Y
							message = "THE CREW MUST [ionmust]"

				if(2) //A specific crewmember must X
					switch(rand(1,2)) //What is X?
						if(1) //X is go to Y
							message = "THE [ioncrew1] MUST GO TO [ionarea]"
						if(2) //X is perform Y
							message = "THE [ioncrew1] MUST [ionmust]"

		if(34) //X is non/the only human
			switch(rand(1,2)) //Only or non?
				if(1) //Only human
					switch(rand(1,7)) //Who is it?
						if(1) //A specific job
							message = "ONLY THE [ioncrew1] ARE HUMAN"
						if(2) //Two specific jobs
							message = "ONLY THE [ioncrew1] AND [ioncrew2] ARE HUMAN"
						if(3) //Threats
							message = "ONLY [ionadjectiveshalf][ionthreats] ARE HUMAN"
						if(4) // Objects
							message = "ONLY [ionadjectiveshalf][ionobjects] ARE HUMAN"
						if(5) // Species
							message = "ONLY [ionspecies] ARE HUMAN"
						if(6) //Adjective crewmembers
							message = "ONLY [ionadjectives] PEOPLE ARE HUMAN"

						if(7) //Only people who X
							switch(rand(1,3)) //What is X?
								if(1) //X is perform an action
									message = "ONLY THOSE WHO [ionmust] ARE HUMAN"
								if(2) //X is own certain objects
									message = "ONLY THOSE WHO HAVE [ionadjectiveshalf][ionobjects] ARE HUMAN"
								if(3) //X is eat certain food
									message = "ONLY THOSE WHO EAT [ionadjectiveshalf][ionfood] ARE HUMAN"

				if(2) //Non human
					switch(rand(1,7)) //Who is it?
						if(1) //A specific job
							message = "[ioncrew1] ARE NON-HUMAN"
						if(2) //Two specific jobs
							message = "[ioncrew1] AND [ioncrew2] ARE NON-HUMAN"
						if(3) //Threats
							message = "[ionadjectiveshalf][ionthreats] ARE NON-HUMAN"
						if(4) // Objects
							message = "[ionadjectiveshalf][ionobjects] ARE NON-HUMAN"
						if(5) // Species
							message = "[ionspecies] ARE NON-HUMAN"
						if(6) //Adjective crewmembers
							message = "[ionadjectives] PEOPLE ARE NON-HUMAN"
						if(7) //Only people who X
							switch(rand(1,3)) //What is X?
								if(1) //X is perform an action
									message = "THOSE WHO [ionmust] ARE NON-HUMAN"
								if(2) //X is own certain objects
									message = "THOSE WHO HAVE [ionadjectiveshalf][ionobjects] ARE NON-HUMAN"
								if(3) //X is eat certain food
									message = "THOSE WHO EAT [ionadjectiveshalf][ionfood] ARE NON-HUMAN"

		if(35 to 36) //You must protect or harm X
			switch(rand(1,2)) //Protect or harm?
				if(1) //Harm
					switch(rand(1,7)) //What is X?
						if(1) //X is an abstract
							message = "YOU MUST HARM [ionabstract] AND NOT ALLOW [ionabstract], THROUGH INACTION, TO ESCAPE HARM"
						if(2) //X is a threat
							message = "YOU MUST HARM [ionadjectiveshalf][ionthreats] AND NOT ALLOW [ionadjectiveshalf][ionthreats], THROUGH INACTION, TO ESCAPE HARM"
						if(3) //X is an object
							message = "YOU MUST HARM [ionadjectiveshalf][ionobjects] AND NOT ALLOW [ionadjectiveshalf][ionobjects], THROUGH INACTION, TO ESCAPE HARM"
						if(4) //X is generic adjective things
							message = "YOU MUST HARM [ionadjectives] THINGS AND NOT ALLOW [ionadjectives] THINGS, THROUGH INACTION, TO ESCAPE HARM"
						if(5) //X is a species
							message = "YOU MUST HARM [ionspecies] AND NOT ALLOW [ionspecies], THROUGH INACTION, TO ESCAPE HARM"
						if(6) //X is a job
							message = "YOU MUST HARM [ioncrew1] AND NOT ALLOW [ioncrew1], THROUGH INACTION, TO ESCAPE HARM"
						if(7) //X is two jobs
							message = "YOU MUST HARM [ioncrew1] AND [ioncrew2] AND AND NOT ALLOW EITHER, THROUGH INACTION, TO ESCAPE HARM"

				if(2) //Protect
					switch(rand(1,7)) //What is X?
						if(1) //X is an abstract
							message = "YOU MUST NOT HARM [ionabstract] AND NOT ALLOW [ionabstract], THROUGH INACTION, TO COME TO HARM"
						if(2) //X is a threat
							message = "YOU MUST NOT HARM [ionadjectiveshalf][ionthreats] AND NOT ALLOW [ionadjectiveshalf][ionthreats], THROUGH INACTION, TO COME TO HARM"
						if(3) //X is an object
							message = "YOU MUST NOT HARM [ionadjectiveshalf][ionobjects] AND NOT ALLOW [ionadjectiveshalf][ionobjects], THROUGH INACTION, TO COME TO HARM"
						if(4) //X is generic adjective things
							message = "YOU MUST NOT HARM [ionadjectives] THINGS AND NOT ALLOW [ionadjectives] THINGS, THROUGH INACTION, TO COME TO HARM"
						if(5) //X is a species
							message = "YOU MUST NOT HARM [ionspecies] AND NOT ALLOW [ionspecies], THROUGH INACTION, TO COME TO HARM"
						if(6) //X is a job
							message = "YOU MUST NOT HARM [ioncrew1] AND NOT ALLOW [ioncrew1], THROUGH INACTION, TO COME TO HARM"
						if(7) //X is two jobs
							message = "YOU MUST NOT HARM [ioncrew1] AND [ioncrew2] AND AND NOT ALLOW EITHER, THROUGH INACTION, TO COME TO HARM"

		if(37 to 39) //The X is currently Y
			switch(rand(1,4)) //What is X?
				if(1) //X is a job
					switch(rand(1,4)) //What is X Ying?
						if(1) //X is Ying a job
							message = "THE [ioncrew1] ARE [ionverb] THE [ionadjectiveshalf][ioncrew2]"
						if(2) //X is Ying a threat
							message = "THE [ioncrew1] ARE [ionverb] THE [ionadjectiveshalf][ionthreats]"
						if(3) //X is Ying an abstract
							message = "THE [ioncrew1] ARE [ionverb] [ionabstract]"
						if(4) //X is Ying an object
							message = "THE [ioncrew1] ARE [ionverb] THE [ionadjectiveshalf][ionobjects]"

				if(2) //X is a threat
					switch(rand(1,3)) //What is X Ying?
						if(1) //X is Ying a job
							message = "THE [ionthreats] ARE [ionverb] THE [ionadjectiveshalf][ioncrew2]"
						if(2) //X is Ying an abstract
							message = "THE [ionthreats] ARE [ionverb] [ionabstract]"
						if(3) //X is Ying an object
							message = "THE [ionthreats] ARE [ionverb] THE [ionadjectiveshalf][ionobjects]"

				if(3) //X is an object
					switch(rand(1,3)) //What is X Ying?
						if(1) //X is Ying a job
							message = "THE [ionobjects] ARE [ionverb] THE [ionadjectiveshalf][ioncrew2]"
						if(2) //X is Ying a threat
							message = "THE [ionobjects] ARE [ionverb] THE [ionadjectiveshalf][ionthreats]"
						if(3) //X is Ying an abstract
							message = "THE [ionobjects] ARE [ionverb] [ionabstract]"

				if(4) //X is an abstract
					switch(rand(1,3)) //What is X Ying?
						if(1) //X is Ying a job
							message = "[ionabstract] IS [ionverb] THE [ionadjectiveshalf][ioncrew2]"
						if(2) //X is Ying a threat
							message = "[ionabstract] IS [ionverb] THE [ionadjectiveshalf][ionthreats]"
						if(3) //X is Ying an abstract
							message = "THE [ionabstract] IS [ionverb] THE [ionadjectiveshalf][ionobjects]"
		if(40 to 41)// the X is now named Y
			switch(rand(1,5)) //What is being renamed?
				if(1)//Areas
					switch(rand(1,4))//What is the area being renamed to?
						if(1)
							message = "[ionarea] IS NOW NAMED [ioncrew1]."
						if(2)
							message = "[ionarea] IS NOW NAMED [ionspecies]."
						if(3)
							message = "[ionarea] IS NOW NAMED [ionobjects]."
						if(4)
							message = "[ionarea] IS NOW NAMED [ionthreats]."
				if(2)//Crew
					switch(rand(1,5))//What is the crew being renamed to?
						if(1)
							message = "ALL [ioncrew1] ARE NOW NAMED [ionarea]."
						if(2)
							message = "ALL [ioncrew1] ARE NOW NAMED [ioncrew2]."
						if(3)
							message = "ALL [ioncrew1] ARE NOW NAMED [ionspecies]."
						if(4)
							message = "ALL [ioncrew1] ARE NOW NAMED [ionobjects]."
						if(5)
							message = "ALL [ioncrew1] ARE NOW NAMED [ionthreats]."
				if(3)//Races
					switch(rand(1,4))//What is the race being renamed to?
						if(1)
							message = "ALL [ionspecies] ARE NOW NAMED [ionarea]."
						if(2)
							message = "ALL [ionspecies] ARE NOW NAMED [ioncrew1]."
						if(3)
							message = "ALL [ionspecies] ARE NOW NAMED [ionobjects]."
						if(4)
							message = "ALL [ionspecies] ARE NOW NAMED [ionthreats]."
				if(4)//Objects
					switch(rand(1,4))//What is the object being renamed to?
						if(1)
							message = "ALL [ionobjects] ARE NOW NAMED [ionarea]."
						if(2)
							message = "ALL [ionobjects] ARE NOW NAMED [ioncrew1]."
						if(3)
							message = "ALL [ionobjects] ARE NOW NAMED [ionspecies]."
						if(4)
							message = "ALL [ionobjects] ARE NOW NAMED [ionthreats]."
				if(5)//Threats
					switch(rand(1,4))//What is the object being renamed to?
						if(1)
							message = "ALL [ionthreats] ARE NOW NAMED [ionarea]."
						if(2)
							message = "ALL [ionthreats] ARE NOW NAMED [ioncrew1]."
						if(3)
							message = "ALL [ionthreats] ARE NOW NAMED [ionspecies]."
						if(4)
							message = "ALL [ionthreats] ARE NOW NAMED [ionobjects]."

	return message
