/obj/effect/proc_holder/spell/aoe_turf/conjure/the_traps
	name = "The Traps!"
	desc = "Summon a number of traps around you. They will damage and enrage any enemies that step on them."

	charge_max = 250
	cooldown_min = 50

	clothes_req = TRUE
	invocation = "CAVERE INSIDIAS"
	invocation_type = "shout"
	range = 3

	summon_type = list(
		/obj/structure/trap/stun,
		/obj/structure/trap/fire,
		/obj/structure/trap/chill,
		/obj/structure/trap/damage
	)
	summon_lifespan = 3000
	summon_amt = 5

	action_icon_state = "the_traps"

/obj/effect/proc_holder/spell/aoe_turf/conjure/the_traps/post_summon(obj/structure/trap/T, mob/user)
	T.immune_minds += user.mind
	T.charges = 1
