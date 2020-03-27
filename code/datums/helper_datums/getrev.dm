/datum/getrev
	var/commit  // git rev-parse HEAD
	var/date
	var/originmastercommit  // git rev-parse origin/master
	var/list/testmerge = list()

/datum/getrev/New()
	testmerge = world.TgsTestMerges()
	var/datum/tgs_revision_information/revinfo = world.TgsRevision()
	if(revinfo)
		commit = revinfo.commit
		originmastercommit = revinfo.origin_commit
	else
		commit = rustg_git_revparse("HEAD")
		if(commit)
			date = rustg_git_commit_date(commit)
		originmastercommit = rustg_git_revparse("origin/master")

	// goes to DD log and config_error.txt
	log_world(get_log_message())

/datum/getrev/proc/get_log_message()
	var/list/msg = list()
	msg += "Running /tg/ revision: [date]"
	if(originmastercommit)
		msg += "origin/master: [originmastercommit]"

	for(var/line in testmerge)
		var/datum/tgs_revision_information/test_merge/tm = line
		msg += "Test merge active of PR #[tm.number] commit [tm.pull_request_commit]"
		SSblackbox.record_feedback("associative", "testmerged_prs", 1, list("number" = "[tm.number]", "commit" = "[tm.pull_request_commit]", "title" = "[tm.title]", "author" = "[tm.author]"))

	if(commit && commit != originmastercommit)
		msg += "HEAD: [commit]"
	else if(!originmastercommit)
		msg += "No commit information"

	return msg.Join("\n")

/datum/getrev/proc/GetTestMergeInfo(header = TRUE)
	if(!testmerge.len)
		return ""
	. = header ? "The following pull requests are currently test merged:<br>" : ""
	for(var/line in testmerge)
		var/datum/tgs_revision_information/test_merge/tm = line
		var/cm = tm.pull_request_commit
		var/details = ": '" + html_encode(tm.title) + "' by " + html_encode(tm.author) + " at commit " + html_encode(copytext_char(cm, 1, 11))
		if(details && findtext(details, "\[s\]") && (!usr || !usr.client.holder))
			continue
		. += "<a href=\"[CONFIG_GET(string/githuburl)]/pull/[tm.number]\">#[tm.number][details]</a><br>"

/client/verb/showrevinfo()
	set category = "OOC"
	set name = "Show Server Revision"
	set desc = "Check the current server code revision"

	var/list/msg = list("")
	// Round ID
	if(GLOB.round_id)
		msg += "<b>Round ID:</b> [GLOB.round_id]"

	msg += "<b>BYOND Version:</b> [world.byond_version].[world.byond_build]"
	if(DM_VERSION != world.byond_version || DM_BUILD != world.byond_build)
		msg += "<b>Compiled with BYOND Version:</b> [DM_VERSION].[DM_BUILD]"

	// Revision information
	var/datum/getrev/revdata = GLOB.revdata
	msg += "<b>Server revision compiled on:</b> [revdata.date]"
	var/pc = revdata.originmastercommit
	if(pc)
		msg += "Master commit: <a href=\"[CONFIG_GET(string/githuburl)]/commit/[pc]\">[pc]</a>"
	if(revdata.testmerge.len)
		msg += revdata.GetTestMergeInfo()
	if(revdata.commit && revdata.commit != revdata.originmastercommit)
		msg += "Local commit: [revdata.commit]"
	else if(!pc)
		msg += "No commit information"
	if(world.TgsAvailable())
		var/datum/tgs_version/version = world.TgsVersion()
		msg += "Server tools version: [version.raw_parameter]"

	// Game mode odds
	msg += "<br><b>Current Informational Settings:</b>"
	msg += "Protect Authority Roles From Traitor: [CONFIG_GET(flag/protect_roles_from_antagonist)]"
	msg += "Protect Assistant Role From Traitor: [CONFIG_GET(flag/protect_assistant_from_antagonist)]"
	msg += "Enforce Human Authority: [CONFIG_GET(flag/enforce_human_authority)]"
	msg += "Allow Latejoin Antagonists: [CONFIG_GET(flag/allow_latejoin_antagonists)]"
	msg += "Enforce Continuous Rounds: [length(CONFIG_GET(keyed_list/continuous))] of [config.modes.len] roundtypes"
	msg += "Allow Midround Antagonists: [length(CONFIG_GET(keyed_list/midround_antag))] of [config.modes.len] roundtypes"
	if(CONFIG_GET(flag/show_game_type_odds))
		var/list/probabilities = CONFIG_GET(keyed_list/probability)
		if(SSticker.IsRoundInProgress())
			var/prob_sum = 0
			var/current_odds_differ = FALSE
			var/list/probs = list()
			var/list/modes = config.gamemode_cache
			var/list/min_pop = CONFIG_GET(keyed_list/min_pop)
			var/list/max_pop = CONFIG_GET(keyed_list/max_pop)
			for(var/mode in modes)
				var/datum/game_mode/M = mode
				var/ctag = initial(M.config_tag)
				if(!(ctag in probabilities))
					continue
				if((min_pop[ctag] && (min_pop[ctag] > SSticker.totalPlayersReady)) || (max_pop[ctag] && (max_pop[ctag] < SSticker.totalPlayersReady)) || (initial(M.required_players) > SSticker.totalPlayersReady))
					current_odds_differ = TRUE
					continue
				probs[ctag] = 1
				prob_sum += probabilities[ctag]
			if(current_odds_differ)
				msg += "<b>Game Mode Odds for current round:</b>"
				for(var/ctag in probs)
					if(probabilities[ctag] > 0)
						var/percentage = round(probabilities[ctag] / prob_sum * 100, 0.1)
						msg += "[ctag] [percentage]%"

		msg += "<b>All Game Mode Odds:</b>"
		var/sum = 0
		for(var/ctag in probabilities)
			sum += probabilities[ctag]
		for(var/ctag in probabilities)
			if(probabilities[ctag] > 0)
				var/percentage = round(probabilities[ctag] / sum * 100, 0.1)
				msg += "[ctag] [percentage]%"
	to_chat(src, msg.Join("<br>"))
