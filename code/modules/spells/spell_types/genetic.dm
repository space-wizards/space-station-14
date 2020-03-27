/obj/effect/proc_holder/spell/targeted/genetic
	name = "Genetic"
	desc = "This spell inflicts a set of mutations and disabilities upon the target."

	var/list/active_on = list()
	var/list/traits = list() //disabilities
	var/list/mutations = list() //mutation defines
	var/duration = 100 //deciseconds
	/*
		Disabilities
			1st bit - ?
			2nd bit - ?
			3rd bit - ?
			4th bit - ?
			5th bit - ?
			6th bit - ?
	*/

/obj/effect/proc_holder/spell/targeted/genetic/cast(list/targets,mob/user = usr)
	playMagSound()
	for(var/mob/living/carbon/target in targets)
		if(target.anti_magic_check())
			continue
		if(!target.dna)
			continue
		for(var/A in mutations)
			target.dna.add_mutation(A)
		for(var/A in traits)
			ADD_TRAIT(target, A, GENETICS_SPELL)
		active_on += target
		if(duration < charge_max)
			addtimer(CALLBACK(src, .proc/remove, target), duration, TIMER_OVERRIDE|TIMER_UNIQUE)

/obj/effect/proc_holder/spell/targeted/genetic/Destroy()
	. = ..()
	for(var/V in active_on)
		remove(V)

/obj/effect/proc_holder/spell/targeted/genetic/proc/remove(mob/living/carbon/target)
	active_on -= target
	if(!QDELETED(target))
		for(var/A in mutations)
			target.dna.remove_mutation(A)
		for(var/A in traits)
			REMOVE_TRAIT(target, A, GENETICS_SPELL)
