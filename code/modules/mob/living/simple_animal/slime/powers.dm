#define SIZE_DOESNT_MATTER 	-1
#define BABIES_ONLY			0
#define ADULTS_ONLY			1

#define NO_GROWTH_NEEDED	0
#define GROWTH_NEEDED		1

/datum/action/innate/slime
	check_flags = AB_CHECK_CONSCIOUS
	icon_icon = 'icons/mob/actions/actions_slime.dmi'
	background_icon_state = "bg_alien"
	var/needs_growth = NO_GROWTH_NEEDED

/datum/action/innate/slime/IsAvailable()
	if(..())
		var/mob/living/simple_animal/slime/S = owner
		if(needs_growth == GROWTH_NEEDED)
			if(S.amount_grown >= SLIME_EVOLUTION_THRESHOLD)
				return 1
			return 0
		return 1

/mob/living/simple_animal/slime/verb/Feed()
	set category = "Slime"
	set desc = "This will let you feed on any valid creature in the surrounding area. This should also be used to halt the feeding process."

	if(stat)
		return 0

	var/list/choices = list()
	for(var/mob/living/C in view(1,src))
		if(C!=src && Adjacent(C))
			choices += C

	var/mob/living/M = input(src,"Who do you wish to feed on?") in null|sortNames(choices)
	if(!M)
		return 0
	if(CanFeedon(M))
		Feedon(M)
		return 1

/datum/action/innate/slime/feed
	name = "Feed"
	button_icon_state = "slimeeat"


/datum/action/innate/slime/feed/Activate()
	var/mob/living/simple_animal/slime/S = owner
	S.Feed()

/mob/living/simple_animal/slime/proc/CanFeedon(mob/living/M, silent = FALSE)
	if(!Adjacent(M))
		return FALSE

	if(buckled)
		Feedstop()
		return FALSE

	if(issilicon(M))
		return FALSE

	if(isanimal(M))
		var/mob/living/simple_animal/S = M
		if(S.damage_coeff[TOX] <= 0 && S.damage_coeff[CLONE] <= 0) //The creature wouldn't take any damage, it must be too weird even for us.
			if(silent)
				return FALSE
			to_chat(src, "<span class='warning'>[pick("This subject is incompatible", \
			"This subject does not have life energy", "This subject is empty", \
			"I am not satisified", "I can not feed from this subject", \
			"I do not feel nourished", "This subject is not food")]!</span>")
			return FALSE

	if(isslime(M))
		if(silent)
			return FALSE
		to_chat(src, "<span class='warning'><i>I can't latch onto another slime...</i></span>")
		return FALSE

	if(docile)
		if(silent)
			return FALSE
		to_chat(src, "<span class='notice'><i>I'm not hungry anymore...</i></span>")
		return FALSE

	if(stat)
		if(silent)
			return FALSE
		to_chat(src, "<span class='warning'><i>I must be conscious to do this...</i></span>")
		return FALSE

	if(M.stat == DEAD)
		if(silent)
			return FALSE
		to_chat(src, "<span class='warning'><i>This subject does not have a strong enough life energy...</i></span>")
		return FALSE

	if(locate(/mob/living/simple_animal/slime) in M.buckled_mobs)
		if(silent)
			return FALSE
		to_chat(src, "<span class='warning'><i>Another slime is already feeding on this subject...</i></span>")
		return FALSE
	return TRUE

/mob/living/simple_animal/slime/proc/Feedon(mob/living/M)
	M.unbuckle_all_mobs(force=1) //Slimes rip other mobs (eg: shoulder parrots) off (Slimes Vs Slimes is already handled in CanFeedon())
	if(M.buckle_mob(src, force=TRUE))
		layer = M.layer+0.01 //appear above the target mob
		M.visible_message("<span class='danger'>[name] has latched onto [M]!</span>", \
						"<span class='userdanger'>[name] has latched onto [M]!</span>")
	else
		to_chat(src, "<span class='warning'><i>I have failed to latch onto the subject!</i></span>")

/mob/living/simple_animal/slime/proc/Feedstop(silent = FALSE, living=1)
	if(buckled)
		if(!living)
			to_chat(src, "<span class='warning'>[pick("This subject is incompatible", \
			"This subject does not have life energy", "This subject is empty", \
			"I am not satisified", "I can not feed from this subject", \
			"I do not feel nourished", "This subject is not food")]!</span>")
		if(!silent)
			visible_message("<span class='warning'>[src] has let go of [buckled]!</span>", \
							"<span class='notice'><i>I stopped feeding.</i></span>")
		layer = initial(layer)
		buckled.unbuckle_mob(src,force=TRUE)

/mob/living/simple_animal/slime/verb/Evolve()
	set category = "Slime"
	set desc = "This will let you evolve from baby to adult slime."

	if(stat)
		to_chat(src, "<i>I must be conscious to do this...</i>")
		return
	if(!is_adult)
		if(amount_grown >= SLIME_EVOLUTION_THRESHOLD)
			is_adult = 1
			maxHealth = 200
			amount_grown = 0
			for(var/datum/action/innate/slime/evolve/E in actions)
				E.Remove(src)
			regenerate_icons()
			update_name()
		else
			to_chat(src, "<i>I am not ready to evolve yet...</i>")
	else
		to_chat(src, "<i>I have already evolved...</i>")

/datum/action/innate/slime/evolve
	name = "Evolve"
	button_icon_state = "slimegrow"
	needs_growth = GROWTH_NEEDED

/datum/action/innate/slime/evolve/Activate()
	var/mob/living/simple_animal/slime/S = owner
	S.Evolve()
	if(S.is_adult)
		var/datum/action/innate/slime/reproduce/A = new
		A.Grant(S)

/mob/living/simple_animal/slime/verb/Reproduce()
	set category = "Slime"
	set desc = "This will make you split into four Slimes."

	if(stat)
		to_chat(src, "<i>I must be conscious to do this...</i>")
		return

	if(is_adult)
		if(amount_grown >= SLIME_EVOLUTION_THRESHOLD)
			if(stat)
				to_chat(src, "<i>I must be conscious to do this...</i>")
				return

			var/list/babies = list()
			var/new_nutrition = round(nutrition * 0.9)
			var/new_powerlevel = round(powerlevel / 4)
			var/datum/component/nanites/original_nanites = GetComponent(/datum/component/nanites)

			for(var/i=1,i<=4,i++)
				var/child_colour
				if(mutation_chance >= 100)
					child_colour = "rainbow"
				else if(prob(mutation_chance))
					child_colour = slime_mutation[rand(1,4)]
				else
					child_colour = colour
				var/mob/living/simple_animal/slime/M
				M = new(loc, child_colour)
				if(ckey)
					M.set_nutrition(new_nutrition) //Player slimes are more robust at spliting. Once an oversight of poor copypasta, now a feature!
				M.powerlevel = new_powerlevel
				if(i != 1)
					step_away(M,src)
				M.Friends = Friends.Copy()
				babies += M
				M.mutation_chance = CLAMP(mutation_chance+(rand(5,-5)),0,100)
				SSblackbox.record_feedback("tally", "slime_babies_born", 1, M.colour)

				if(original_nanites)
					M.AddComponent(/datum/component/nanites, original_nanites.nanite_volume*0.25)
					SEND_SIGNAL(M, COMSIG_NANITE_SYNC, original_nanites, TRUE, TRUE) //The trues are to copy activation as well

			var/mob/living/simple_animal/slime/new_slime = pick(babies)
			new_slime.a_intent = INTENT_HARM
			if(src.mind)
				src.mind.transfer_to(new_slime)
			else
				new_slime.key = src.key
			qdel(src)
		else
			to_chat(src, "<i>I am not ready to reproduce yet...</i>")
	else
		to_chat(src, "<i>I am not old enough to reproduce yet...</i>")

/datum/action/innate/slime/reproduce
	name = "Reproduce"
	button_icon_state = "slimesplit"
	needs_growth = GROWTH_NEEDED

/datum/action/innate/slime/reproduce/Activate()
	var/mob/living/simple_animal/slime/S = owner
	S.Reproduce()
