/datum/round_event_control/bureaucratic_error
	name = "Bureaucratic Error"
	typepath = /datum/round_event/bureaucratic_error
	max_occurrences = 1
	weight = 5

/datum/round_event/bureaucratic_error
	announceWhen = 1

/datum/round_event/bureaucratic_error/announce(fake)
	priority_announce("A recent bureaucratic error in the Organic Resources Department may result in personnel shortages in some departments and redundant staffing in others.", "Paperwork Mishap Alert")

/datum/round_event/bureaucratic_error/start()
	var/list/jobs = SSjob.occupations.Copy()		
	if(prob(33))	// Only allows latejoining as a single role. Add latejoin AI bluespace pods for fun later.
		var/datum/job/overflow = pick_n_take(jobs)
		SSjob.set_overflow_role(overflow.title)	// Ensures infinite slots as this role. Assistant will still be open for those that cant.
		for(var/job in jobs)
			var/datum/job/current = job
			if(current.title == "AI") // AI currently doesnt support latejoining past one total.
				continue
			current.total_positions = 0
	else	// Adds/removes a random amount of job slots from all jobs.
		for(var/job in jobs)
			var/datum/job/current = job
			if(current.title == "AI") // AI currently doesnt support latejoining past one total.
				continue
			var/ran = rand(-2,4)
			current.total_positions = max(current.total_positions + ran, 0)
