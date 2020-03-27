SUBSYSTEM_DEF(pai)
	name = "pAI"

	flags = SS_NO_INIT|SS_NO_FIRE

	var/list/candidates = list()
	var/ghost_spam = FALSE
	var/spam_delay = 100
	var/list/pai_card_list = list()

/datum/controller/subsystem/pai/Topic(href, href_list[])
	if(href_list["download"])
		var/datum/paiCandidate/candidate = locate(href_list["candidate"]) in candidates
		var/obj/item/paicard/card = locate(href_list["device"]) in pai_card_list
		if(card.pai)
			return
		if(istype(card, /obj/item/paicard) && istype(candidate, /datum/paiCandidate))
			if(check_ready(candidate) != candidate)
				return FALSE
			var/mob/living/silicon/pai/pai = new(card)
			if(!candidate.name)
				pai.name = pick(GLOB.ninja_names)
			else
				pai.name = candidate.name
			pai.real_name = pai.name
			pai.key = candidate.key

			card.setPersonality(pai)

			candidates -= candidate
			usr << browse(null, "window=findPai")

	if(href_list["new"])
		var/datum/paiCandidate/candidate = locate(href_list["candidate"]) in candidates
		var/option = href_list["option"]
		var/t = ""

		switch(option)
			if("name")
				t = sanitize_name(stripped_input(usr, "Enter a name for your pAI", "pAI Name", candidate.name, MAX_NAME_LEN))
				if(t)
					candidate.name = t
			if("desc")
				t = stripped_multiline_input(usr, "Enter a description for your pAI", "pAI Description", candidate.description, MAX_MESSAGE_LEN)
				if(t)
					candidate.description = t
			if("role")
				t = stripped_input(usr, "Enter a role for your pAI", "pAI Role", candidate.role, MAX_MESSAGE_LEN)
				if(t)
					candidate.role = t
			if("ooc")
				t = stripped_multiline_input(usr, "Enter any OOC comments", "pAI OOC Comments", candidate.comments, MAX_MESSAGE_LEN)
				if(t)
					candidate.comments = t
			if("save")
				candidate.savefile_save(usr)
			if("load")
				candidate.savefile_load(usr)
				//In case people have saved unsanitized stuff.
				if(candidate.name)
					candidate.name = copytext_char(sanitize(candidate.name),1,MAX_NAME_LEN)
				if(candidate.description)
					candidate.description = copytext_char(sanitize(candidate.description),1,MAX_MESSAGE_LEN)
				if(candidate.role)
					candidate.role = copytext_char(sanitize(candidate.role),1,MAX_MESSAGE_LEN)
				if(candidate.comments)
					candidate.comments = copytext_char(sanitize(candidate.comments),1,MAX_MESSAGE_LEN)

			if("submit")
				if(candidate)
					candidate.ready = 1
					for(var/obj/item/paicard/p in pai_card_list)
						if(!p.pai)
							p.alertUpdate()
				usr << browse(null, "window=paiRecruit")
				return
		recruitWindow(usr)

/datum/controller/subsystem/pai/proc/recruitWindow(mob/M)
	var/datum/paiCandidate/candidate
	for(var/datum/paiCandidate/c in candidates)
		if(c.key == M.key)
			candidate = c
	if(!candidate)
		candidate = new /datum/paiCandidate()
		candidate.key = M.key
		candidates.Add(candidate)


	var/dat = ""
	dat += {"
			<style type="text/css">

			p.top {
				background-color: #AAAAAA; color: black;
			}

			tr.d0 td {
				background-color: #CC9999; color: black;
			}
			tr.d1 td {
				background-color: #9999CC; color: black;
			}
			</style>
			"}

	dat += "<p class=\"top\">Please configure your pAI personality's options. Remember, what you enter here could determine whether or not the user requesting a personality chooses you!</p>"
	dat += "<table>"
	dat += "<tr class=\"d0\"><td>Name:</td><td>[candidate.name]</td></tr>"
	dat += "<tr class=\"d1\"><td><a href='byond://?src=[REF(src)];option=name;new=1;candidate=[REF(candidate)]'>\[Edit\]</a></td><td>What you plan to call yourself. Suggestions: Any character name you would choose for a station character OR an AI.</td></tr>"

	dat += "<tr class=\"d0\"><td>Description:</td><td>[candidate.description]</td></tr>"
	dat += "<tr class=\"d1\"><td><a href='byond://?src=[REF(src)];option=desc;new=1;candidate=[REF(candidate)]'>\[Edit\]</a></td><td>What sort of pAI you typically play; your mannerisms, your quirks, etc. This can be as sparse or as detailed as you like.</td></tr>"

	dat += "<tr class=\"d0\"><td>Preferred Role:</td><td>[candidate.role]</td></tr>"
	dat += "<tr class=\"d1\"><td><a href='byond://?src=[REF(src)];option=role;new=1;candidate=[REF(candidate)]'>\[Edit\]</a></td><td>Do you like to partner with sneaky social ninjas? Like to help security hunt down thugs? Enjoy watching an engineer's back while he saves the station yet again? This doesn't have to be limited to just station jobs. Pretty much any general descriptor for what you'd like to be doing works here.</td></tr>"

	dat += "<tr class=\"d0\"><td>OOC Comments:</td><td>[candidate.comments]</td></tr>"
	dat += "<tr class=\"d1\"><td><a href='byond://?src=[REF(src)];option=ooc;new=1;candidate=[REF(candidate)]'>\[Edit\]</a></td><td>Anything you'd like to address specifically to the player reading this in an OOC manner. \"I prefer more serious RP.\", \"I'm still learning the interface!\", etc. Feel free to leave this blank if you want.</td></tr>"

	dat += "</table>"

	dat += "<br>"
	dat += "<h3><a href='byond://?src=[REF(src)];option=submit;new=1;candidate=[REF(candidate)]'>Submit Personality</a></h3><br>"
	dat += "<a href='byond://?src=[REF(src)];option=save;new=1;candidate=[REF(candidate)]'>Save Personality</a><br>"
	dat += "<a href='byond://?src=[REF(src)];option=load;new=1;candidate=[REF(candidate)]'>Load Personality</a><br>"

	M << browse(dat, "window=paiRecruit")

/datum/controller/subsystem/pai/proc/spam_again()
	ghost_spam = FALSE

/datum/controller/subsystem/pai/proc/check_ready(var/datum/paiCandidate/C)
	if(!C.ready)
		return FALSE
	for(var/mob/dead/observer/O in GLOB.player_list)
		if(O.key == C.key)
			return C
	return FALSE

/datum/controller/subsystem/pai/proc/findPAI(obj/item/paicard/p, mob/user)
	if(!ghost_spam)
		ghost_spam = TRUE
		for(var/mob/dead/observer/G in GLOB.player_list)
			if(!G.key)
				continue
			if(!(ROLE_PAI in G.client.prefs.be_special))
				continue
			to_chat(G, "<span class='ghostalert'>[user] is requesting a pAI personality! Use the pAI button to submit yourself as one.</span>")
		addtimer(CALLBACK(src, .proc/spam_again), spam_delay)
	var/list/available = list()
	for(var/datum/paiCandidate/c in SSpai.candidates)
		available.Add(check_ready(c))
	var/dat = ""

	dat += {"
			<style type="text/css">

			p.top {
				background-color: #AAAAAA; color: black;
			}

			tr.d0 td {
				background-color: #CC9999; color: black;
			}
			tr.d1 td {
				background-color: #9999CC; color: black;
			}
			tr.d2 td {
				background-color: #99CC99; color: black;
			}
			</style>
			"}
	dat += "<p class=\"top\">Requesting AI personalities from central database... If there are no entries, or if a suitable entry is not listed, check again later as more personalities may be added.</p>"

	dat += "<table>"

	for(var/datum/paiCandidate/c in available)
		dat += "<tr class=\"d0\"><td>Name:</td><td>[c.name]</td></tr>"
		dat += "<tr class=\"d1\"><td>Description:</td><td>[c.description]</td></tr>"
		dat += "<tr class=\"d0\"><td>Preferred Role:</td><td>[c.role]</td></tr>"
		dat += "<tr class=\"d1\"><td>OOC Comments:</td><td>[c.comments]</td></tr>"
		dat += "<tr class=\"d2\"><td><a href='byond://?src=[REF(src)];download=1;candidate=[REF(c)];device=[REF(p)]'>\[Download [c.name]\]</a></td><td></td></tr>"

	dat += "</table>"

	user << browse(dat, "window=findPai")

/datum/paiCandidate
	var/name
	var/key
	var/description
	var/role
	var/comments
	var/ready = 0
