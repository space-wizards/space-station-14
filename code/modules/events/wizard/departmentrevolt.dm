/datum/round_event_control/wizard/deprevolt //stationwide!
	name = "Departmental Uprising"
	weight = 0 //An order that requires order in a round of chaos was maybe not the best idea. Requiescat in pace departmental uprising August 2014 - March 2015
	typepath = /datum/round_event/wizard/deprevolt
	max_occurrences = 1
	earliest_start = 0 MINUTES

/datum/round_event/wizard/deprevolt/start()

	var/list/tidecolor
	var/list/jobs_to_revolt	= 	list()
	var/nation_name
	var/list/citizens	=		list()

	tidecolor = pick("grey", "white", "yellow", "purple", "brown", "whatevercolorrepresentstheservicepeople")
	switch(tidecolor)
		if("grey") //God help you
			jobs_to_revolt = list("Assistant")
			nation_name = pick("Assa", "Mainte", "Tunnel", "Gris", "Grey", "Liath", "Grigio", "Ass", "Assi")
		if("white")
			jobs_to_revolt = list("Chief Medical Officer", "Medical Doctor", "Paramedic", "Chemist", "Geneticist", "Virologist")
			nation_name = pick("Mede", "Healtha", "Recova", "Chemi", "Geneti", "Viro", "Psych")
		if("yellow")
			jobs_to_revolt = list("Chief Engineer", "Station Engineer", "Atmospheric Technician")
			nation_name = pick("Atomo", "Engino", "Power", "Teleco")
		if("purple")
			jobs_to_revolt = list("Research Director","Scientist", "Roboticist")
			nation_name = pick("Sci", "Griffa", "Explosi", "Mecha", "Xeno")
		if("brown")
			jobs_to_revolt = list("Quartermaster", "Cargo Technician", "Shaft Miner")
			nation_name = pick("Cargo", "Guna", "Suppli", "Mule", "Crate", "Ore", "Mini", "Shaf")
		if("whatevercolorrepresentstheservicepeople") //the few, the proud, the technically aligned
			jobs_to_revolt = list("Bartender", "Cook", "Botanist", "Clown", "Mime", "Janitor", "Chaplain")
			nation_name = pick("Honka", "Boozo", "Fatu", "Danka", "Mimi", "Libra", "Jani", "Religi")

	nation_name += pick("stan", "topia", "land", "nia", "ca", "tova", "dor", "ador", "tia", "sia", "ano", "tica", "tide", "cis", "marea", "co", "taoide", "slavia", "stotzka")

	var/datum/team/nation/nation = new
	nation.name = nation_name

	for(var/i in GLOB.human_list)
		var/mob/living/carbon/human/H = i
		if(H.mind)
			var/datum/mind/M = H.mind
			if(M.assigned_role && !(M.has_antag_datum(/datum/antagonist)))
				for(var/job in jobs_to_revolt)
					if(M.assigned_role == job)
						citizens += H
						M.add_antag_datum(/datum/antagonist/separatist,nation)
						H.log_message("Was made into a separatist, long live [nation_name]!", LOG_ATTACK, color="red")

	if(citizens.len)
		var/message
		for(var/job in jobs_to_revolt)
			message += "[job],"
		message_admins("The nation of [nation_name] has been formed. Affected jobs are [message]")
