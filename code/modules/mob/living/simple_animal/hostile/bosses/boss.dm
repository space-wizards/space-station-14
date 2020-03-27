/mob/living/simple_animal/hostile/boss
	name = "A Perfectly Generic Boss Placeholder"
	desc = ""
	robust_searching = 1
	stat_attack = UNCONSCIOUS
	status_flags = 0
	a_intent = INTENT_HARM
	gender = NEUTER
	var/list/boss_abilities = list() //list of /datum/action/boss
	var/datum/boss_active_timed_battle/atb
	var/point_regen_delay = 1


/mob/living/simple_animal/hostile/boss/Initialize()
	. = ..()

	atb = new()
	atb.point_regen_delay = point_regen_delay
	atb.boss = src

	for(var/ab in boss_abilities)
		boss_abilities -= ab
		var/datum/action/boss/AB = new ab()
		AB.boss = src
		AB.Grant(src)
		boss_abilities += AB

	atb.assign_abilities(boss_abilities)


/mob/living/simple_animal/hostile/boss/Destroy()
	qdel(atb)
	atb = null
	for(var/ab in boss_abilities)
		var/datum/action/boss/AB = ab
		AB.boss = null
		AB.Remove(src)
		qdel(AB)
	boss_abilities.Cut()
	return ..()


//Action datum for bosses
//Override Trigger() as shown below to do things
/datum/action/boss
	check_flags = AB_CHECK_CONSCIOUS //Incase the boss is given a player
	var/boss_cost = 100 //Cost of usage for the boss' AI 1-100
	var/usage_probability = 100
	var/mob/living/simple_animal/hostile/boss/boss
	var/boss_type = /mob/living/simple_animal/hostile/boss
	var/needs_target = TRUE //Does the boss need to have a target? (Only matters for the AI)
	var/say_when_triggered = "" //What does the boss Say() when the ability triggers?

/datum/action/boss/Trigger()
	. = ..()
	if(.)
		if(!istype(boss, boss_type))
			return 0
		if(!boss.atb)
			return 0
		if(boss.atb.points < boss_cost)
			return 0
		if(!boss.client)
			if(needs_target && !boss.target)
				return 0
		if(boss)
			if(say_when_triggered)
				boss.say(say_when_triggered, forced = "boss action")
			if(!boss.atb.spend(boss_cost))
				return 0

//Example:
/*
/datum/action/boss/selfgib/Trigger()
	if(..())
		boss.gib()
*/


//Designed for boss mobs only
/datum/boss_active_timed_battle
	var/list/abilities //a list of /datum/action/boss owned by a boss mob
	var/point_regen_delay = 5
	var/points = 50 //1-100, start with 50 so we can use some abilities but not insta-buttfug somebody
	var/next_point_time = 0
	var/chance_to_hold_onto_points = 50
	var/highest_cost = 0
	var/mob/living/simple_animal/hostile/boss/boss


/datum/boss_active_timed_battle/New()
	..()
	START_PROCESSING(SSobj, src)


/datum/boss_active_timed_battle/proc/assign_abilities(list/L)
	if(!L)
		return 0
	abilities = L
	for(var/ab in abilities)
		var/datum/action/boss/AB = ab
		if(AB.boss_cost > highest_cost)
			highest_cost = AB.boss_cost


/datum/boss_active_timed_battle/proc/spend(cost)
	if(cost <= points)
		points = max(0,points-cost)
		return 1
	return 0


/datum/boss_active_timed_battle/proc/refund(cost)
	points = min(points+cost, 100)


/datum/boss_active_timed_battle/process()
	if(world.time >= next_point_time)
		next_point_time = world.time + point_regen_delay
		points = min(100, ++points) //has to be out of 100

	if(abilities)
		chance_to_hold_onto_points = highest_cost*0.5
		if(points != 100 && prob(chance_to_hold_onto_points))
			return //Let's save our points for a better ability (unless we're at max points, in which case we can't save anymore!)
		if(!boss.client)
			abilities = shuffle(abilities)
			for(var/ab in abilities)
				var/datum/action/boss/AB = ab
				if(prob(AB.usage_probability) && AB.Trigger())
					break


/datum/boss_active_timed_battle/Destroy()
	abilities = null
	SSobj.processing.Remove(src)
	return ..()
