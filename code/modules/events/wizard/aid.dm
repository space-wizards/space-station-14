//in this file: Various events that directly aid the wizard. This is the "lets entice the wizard to use summon events!" file.

/datum/round_event_control/wizard/robelesscasting //EI NUDTH!
	name = "Robeless Casting"
	weight = 2
	typepath = /datum/round_event/wizard/robelesscasting
	max_occurrences = 1
	earliest_start = 0 MINUTES

/datum/round_event/wizard/robelesscasting/start()

	for(var/i in GLOB.mob_living_list) //Hey if a corgi has magic missle he should get the same benifit as anyone
		var/mob/living/L = i
		if(L.mind && L.mind.spell_list.len != 0)
			var/spell_improved = FALSE
			for(var/obj/effect/proc_holder/spell/S in L.mind.spell_list)
				if(S.clothes_req)
					S.clothes_req = 0
					spell_improved = TRUE
			if(spell_improved)
				to_chat(L, "<span class='notice'>You suddenly feel like you never needed those garish robes in the first place...</span>")

//--//

/datum/round_event_control/wizard/improvedcasting //blink x5 disintergrate x5 here I come!
	name = "Improved Casting"
	weight = 3
	typepath = /datum/round_event/wizard/improvedcasting
	max_occurrences = 4 //because that'd be max level spells
	earliest_start = 0 MINUTES

/datum/round_event/wizard/improvedcasting/start()
	for(var/i in GLOB.mob_living_list)
		var/mob/living/L = i
		if(L.mind && L.mind.spell_list.len != 0)
			for(var/obj/effect/proc_holder/spell/S in L.mind.spell_list)
				S.name = initial(S.name)
				S.spell_level++
				if(S.spell_level >= 6 || S.charge_max <= 0) //Badmin checks, these should never be a problem in normal play
					continue
				if(S.level_max <= 0)
					continue
				S.charge_max = round(initial(S.charge_max) - S.spell_level * (initial(S.charge_max) - S.cooldown_min) / S.level_max)
				if(S.charge_max < S.charge_counter)
					S.charge_counter = S.charge_max
				switch(S.spell_level)
					if(1)
						S.name = "Efficient [S.name]"
					if(2)
						S.name = "Quickened [S.name]"
					if(3)
						S.name = "Free [S.name]"
					if(4)
						S.name = "Instant [S.name]"
					if(5)
						S.name = "Ludicrous [S.name]"

			to_chat(L, "<span class='notice'>You suddenly feel more competent with your casting!</span>")
