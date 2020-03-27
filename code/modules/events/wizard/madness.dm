/datum/round_event_control/wizard/madness
	name = "Curse of Madness"
	weight = 1
	typepath = /datum/round_event/wizard/madness
	earliest_start = 0 MINUTES

	var/forced_secret

/datum/round_event_control/wizard/madness/admin_setup()
	if(!check_rights(R_FUN))
		return

	var/suggested = pick(strings(REDPILL_FILE, "redpill_questions"))

	forced_secret = (input(usr, "What horrifying truth will you reveal?", "Curse of Madness", sortList(suggested)) as text|null) || suggested

/datum/round_event/wizard/madness/start()
	var/datum/round_event_control/wizard/madness/C = control

	var/horrifying_truth

	if(C.forced_secret)
		horrifying_truth = C.forced_secret
		C.forced_secret = null
	else
		horrifying_truth = pick(strings(REDPILL_FILE, "redpill_questions"))

	curse_of_madness(null, horrifying_truth)
