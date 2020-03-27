/datum/polloption
	var/optionid
	var/optiontext

/mob/dead/new_player/proc/handle_player_polling()
	if(!SSdbcore.IsConnected())
		to_chat(usr, "<span class='danger'>Failed to establish database connection.</span>")
		return
	var/datum/DBQuery/query_poll_get = SSdbcore.NewQuery("SELECT id, question FROM [format_table_name("poll_question")] WHERE Now() BETWEEN starttime AND endtime [(client.holder ? "" : "AND adminonly = false")]")
	if(!query_poll_get.warn_execute())
		qdel(query_poll_get)
		return
	var/output = "<div align='center'><B>Player polls</B><hr><table>"
	var/i = 0
	var/rs = REF(src)
	while(query_poll_get.NextRow())
		var/pollid = query_poll_get.item[1]
		var/pollquestion = query_poll_get.item[2]
		output += "<tr bgcolor='#[ (i % 2 == 1) ? "e2e2e2" : "e2e2e2" ]'><td><a href=\"byond://?src=[rs];pollid=[pollid]\"><b>[pollquestion]</b></a></td></tr>"
		i++
	qdel(query_poll_get)
	output += "</table>"
	if(!QDELETED(src))
		src << browse(output,"window=playerpolllist;size=500x300")

/mob/dead/new_player/proc/poll_player(pollid)
	if(!pollid)
		return
	if (!SSdbcore.Connect())
		to_chat(usr, "<span class='danger'>Failed to establish database connection.</span>")
		return
	var/datum/DBQuery/query_poll_get_details = SSdbcore.NewQuery("SELECT starttime, endtime, question, polltype, multiplechoiceoptions FROM [format_table_name("poll_question")] WHERE id = [pollid]")
	if(!query_poll_get_details.warn_execute())
		qdel(query_poll_get_details)
		return
	var/pollstarttime = ""
	var/pollendtime = ""
	var/pollquestion = ""
	var/polltype = ""
	var/multiplechoiceoptions = 0
	if(query_poll_get_details.NextRow())
		pollstarttime = query_poll_get_details.item[1]
		pollendtime = query_poll_get_details.item[2]
		pollquestion = query_poll_get_details.item[3]
		polltype = query_poll_get_details.item[4]
		multiplechoiceoptions = text2num(query_poll_get_details.item[5])
	qdel(query_poll_get_details)
	switch(polltype)
		if(POLLTYPE_OPTION)
			var/datum/DBQuery/query_option_get_votes = SSdbcore.NewQuery("SELECT optionid FROM [format_table_name("poll_vote")] WHERE pollid = [pollid] AND ckey = '[ckey]'")
			if(!query_option_get_votes.warn_execute())
				qdel(query_option_get_votes)
				return
			var/votedoptionid = 0
			if(query_option_get_votes.NextRow())
				votedoptionid = text2num(query_option_get_votes.item[1])
			qdel(query_option_get_votes)
			var/list/datum/polloption/options = list()
			var/datum/DBQuery/query_option_options = SSdbcore.NewQuery("SELECT id, text FROM [format_table_name("poll_option")] WHERE pollid = [pollid]")
			if(!query_option_options.warn_execute())
				qdel(query_option_options)
				return
			while(query_option_options.NextRow())
				var/datum/polloption/PO = new()
				PO.optionid = text2num(query_option_options.item[1])
				PO.optiontext = query_option_options.item[2]
				options += PO
			qdel(query_option_options)
			var/output = "<div align='center'><B>Player poll</B><hr>"
			output += "<b>Question: [pollquestion]</b><br>"
			output += "<font size='2'>Poll runs from <b>[pollstarttime]</b> until <b>[pollendtime]</b></font><p>"
			if(!votedoptionid)
				output += "<form name='cardcomp' action='?src=[REF(src)]' method='get'>"
				output += "<input type='hidden' name='src' value='[REF(src)]'>"
				output += "<input type='hidden' name='votepollid' value='[pollid]'>"
				output += "<input type='hidden' name='votetype' value=[POLLTYPE_OPTION]>"
			output += "<table><tr><td>"
			for(var/datum/polloption/O in options)
				if(O.optionid && O.optiontext)
					if(votedoptionid)
						if(votedoptionid == O.optionid)
							output += "<b>[O.optiontext]</b><br>"
						else
							output += "[O.optiontext]<br>"
					else
						output += "<input type='radio' name='voteoptionid' value='[O.optionid]'>[O.optiontext]<br>"
			output += "</td></tr></table>"
			if(!votedoptionid)
				output += "<p><input type='submit' value='Vote'>"
				output += "</form>"
			output += "</div>"
			src << browse(null ,"window=playerpolllist")
			src << browse(output,"window=playerpoll;size=500x250")
		if(POLLTYPE_TEXT)
			var/datum/DBQuery/query_text_get_votes = SSdbcore.NewQuery("SELECT replytext FROM [format_table_name("poll_textreply")] WHERE pollid = [pollid] AND ckey = '[ckey]'")
			if(!query_text_get_votes.warn_execute())
				qdel(query_text_get_votes)
				return
			var/vote_text = ""
			if(query_text_get_votes.NextRow())
				vote_text = query_text_get_votes.item[1]
			qdel(query_text_get_votes)
			var/output = "<div align='center'><B>Player poll</B><hr>"
			output += "<b>Question: [pollquestion]</b><br>"
			output += "<font size='2'>Feedback gathering runs from <b>[pollstarttime]</b> until <b>[pollendtime]</b></font><p>"
			output += "<form name='cardcomp' action='?src=[REF(src)]' method='get'>"
			output += "<input type='hidden' name='src' value='[REF(src)]'>"
			output += "<input type='hidden' name='votepollid' value='[pollid]'>"
			output += "<input type='hidden' name='votetype' value=[POLLTYPE_TEXT]>"
			output += "<font size='2'>Please provide feedback below. You can use any letters of the English alphabet, numbers and the symbols: . , ! ? : ; -</font><br>"
			output += "<textarea name='replytext' cols='50' rows='14'>[vote_text]</textarea>"
			output += "<p><input type='submit' value='Submit'></form>"
			output += "<form name='cardcomp' action='?src=[REF(src)]' method='get'>"
			output += "<input type='hidden' name='src' value='[REF(src)]'>"
			output += "<input type='hidden' name='votepollid' value='[pollid]'>"
			output += "<input type='hidden' name='votetype' value=[POLLTYPE_TEXT]>"
			output += "<input type='hidden' name='replytext' value='ABSTAIN'>"
			output += "<input type='submit' value='Abstain'></form>"

			src << browse(null ,"window=playerpolllist")
			src << browse(output,"window=playerpoll;size=500x500")
		if(POLLTYPE_RATING)
			var/datum/DBQuery/query_rating_get_votes = SSdbcore.NewQuery("SELECT o.text, v.rating FROM [format_table_name("poll_option")] o, [format_table_name("poll_vote")] v WHERE o.pollid = [pollid] AND v.ckey = '[ckey]' AND o.id = v.optionid")
			if(!query_rating_get_votes.warn_execute())
				qdel(query_rating_get_votes)
				return
			var/output = "<div align='center'><B>Player poll</B><hr>"
			output += "<b>Question: [pollquestion]</b><br>"
			output += "<font size='2'>Poll runs from <b>[pollstarttime]</b> until <b>[pollendtime]</b></font><p>"
			var/rating
			while(query_rating_get_votes.NextRow())
				var/optiontext = query_rating_get_votes.item[1]
				rating = query_rating_get_votes.item[2]
				output += "<br><b>[optiontext] - [rating]</b>"
			qdel(query_rating_get_votes)
			if(!rating)
				output += "<form name='cardcomp' action='?src=[REF(src)]' method='get'>"
				output += "<input type='hidden' name='src' value='[REF(src)]'>"
				output += "<input type='hidden' name='votepollid' value='[pollid]'>"
				output += "<input type='hidden' name='votetype' value=[POLLTYPE_RATING]>"
				var/minid = 999999
				var/maxid = 0
				var/datum/DBQuery/query_rating_options = SSdbcore.NewQuery("SELECT id, text, minval, maxval, descmin, descmid, descmax FROM [format_table_name("poll_option")] WHERE pollid = [pollid]")
				if(!query_rating_options.warn_execute())
					qdel(query_rating_options)
					return
				while(query_rating_options.NextRow())
					var/optionid = text2num(query_rating_options.item[1])
					var/optiontext = query_rating_options.item[2]
					var/minvalue = text2num(query_rating_options.item[3])
					var/maxvalue = text2num(query_rating_options.item[4])
					var/descmin = query_rating_options.item[5]
					var/descmid = query_rating_options.item[6]
					var/descmax = query_rating_options.item[7]
					if(optionid < minid)
						minid = optionid
					if(optionid > maxid)
						maxid = optionid
					var/midvalue = round( (maxvalue + minvalue) / 2)
					output += "<br>[optiontext]: <select name='o[optionid]'>"
					output += "<option value='abstain'>abstain</option>"
					for (var/j = minvalue; j <= maxvalue; j++)
						if(j == minvalue && descmin)
							output += "<option value='[j]'>[j] ([descmin])</option>"
						else if (j == midvalue && descmid)
							output += "<option value='[j]'>[j] ([descmid])</option>"
						else if (j == maxvalue && descmax)
							output += "<option value='[j]'>[j] ([descmax])</option>"
						else
							output += "<option value='[j]'>[j]</option>"
					output += "</select>"
				qdel(query_rating_options)
				output += "<input type='hidden' name='minid' value='[minid]'>"
				output += "<input type='hidden' name='maxid' value='[maxid]'>"
				output += "<p><input type='submit' value='Submit'></form>"
			if(!QDELETED(src))
				src << browse(null ,"window=playerpolllist")
				src << browse(output,"window=playerpoll;size=500x500")
		if(POLLTYPE_MULTI)
			var/datum/DBQuery/query_multi_get_votes = SSdbcore.NewQuery("SELECT optionid FROM [format_table_name("poll_vote")] WHERE pollid = [pollid] AND ckey = '[ckey]'")
			if(!query_multi_get_votes.warn_execute())
				qdel(query_multi_get_votes)
				return
			var/list/votedfor = list()
			while(query_multi_get_votes.NextRow())
				votedfor.Add(text2num(query_multi_get_votes.item[1]))
			qdel(query_multi_get_votes)
			var/list/datum/polloption/options = list()
			var/maxoptionid = 0
			var/minoptionid = 0
			var/datum/DBQuery/query_multi_options = SSdbcore.NewQuery("SELECT id, text FROM [format_table_name("poll_option")] WHERE pollid = [pollid]")
			if(!query_multi_options.warn_execute())
				qdel(query_multi_options)
				return
			while(query_multi_options.NextRow())
				var/datum/polloption/PO = new()
				PO.optionid = text2num(query_multi_options.item[1])
				PO.optiontext = query_multi_options.item[2]
				if(PO.optionid > maxoptionid)
					maxoptionid = PO.optionid
				if(PO.optionid < minoptionid || !minoptionid)
					minoptionid = PO.optionid
				options += PO
			qdel(query_multi_options)
			var/output = "<div align='center'><B>Player poll</B><hr>"
			output += "<b>Question: [pollquestion]</b><br>You can select up to [multiplechoiceoptions] options. If you select more, the first [multiplechoiceoptions] will be saved.<br>"
			output += "<font size='2'>Poll runs from <b>[pollstarttime]</b> until <b>[pollendtime]</b></font><p>"
			if(!votedfor.len)
				output += "<form name='cardcomp' action='?src=[REF(src)]' method='get'>"
				output += "<input type='hidden' name='src' value='[REF(src)]'>"
				output += "<input type='hidden' name='votepollid' value='[pollid]'>"
				output += "<input type='hidden' name='votetype' value=[POLLTYPE_MULTI]>"
				output += "<input type='hidden' name='maxoptionid' value='[maxoptionid]'>"
				output += "<input type='hidden' name='minoptionid' value='[minoptionid]'>"
			output += "<table><tr><td>"
			for(var/datum/polloption/O in options)
				if(O.optionid && O.optiontext)
					if(votedfor.len)
						if(O.optionid in votedfor)
							output += "<b>[O.optiontext]</b><br>"
						else
							output += "[O.optiontext]<br>"
					else
						output += "<input type='checkbox' name='option_[O.optionid]' value='[O.optionid]'>[O.optiontext]<br>"
			output += "</td></tr></table>"
			if(!votedfor.len)
				output += "<p><input type='submit' value='Vote'></form>"
			output += "</div>"
			src << browse(null ,"window=playerpolllist")
			src << browse(output,"window=playerpoll;size=500x250")
		if(POLLTYPE_IRV)
			var/datum/asset/irv_assets = get_asset_datum(/datum/asset/group/IRV)
			irv_assets.send(src)

			var/datum/DBQuery/query_irv_get_votes = SSdbcore.NewQuery("SELECT optionid FROM [format_table_name("poll_vote")] WHERE pollid = [pollid] AND ckey = '[ckey]'")
			if(!query_irv_get_votes.warn_execute())
				qdel(query_irv_get_votes)
				return

			var/list/votedfor = list()
			while(query_irv_get_votes.NextRow())
				votedfor.Add(text2num(query_irv_get_votes.item[1]))
			qdel(query_irv_get_votes)

			var/list/datum/polloption/options = list()

			var/datum/DBQuery/query_irv_options = SSdbcore.NewQuery("SELECT id, text FROM [format_table_name("poll_option")] WHERE pollid = [pollid]")
			if(!query_irv_options.warn_execute())
				qdel(query_irv_options)
				return
			while(query_irv_options.NextRow())
				var/datum/polloption/PO = new()
				PO.optionid = text2num(query_irv_options.item[1])
				PO.optiontext = query_irv_options.item[2]
				options["[PO.optionid]"] += PO
			qdel(query_irv_options)

			//if they already voted, use their sort
			if (votedfor.len)
				var/list/datum/polloption/newoptions = list()
				for (var/V in votedfor)
					var/datum/polloption/PO = options["[V]"]
					if(PO)
						newoptions["[V]"] = PO
						options -= "[V]"
				//add any options that they didn't vote on (some how, some way)
				options = shuffle(options)
				for (var/V in options)
					newoptions["[V]"] = options["[V]"]
				options = newoptions
			//otherwise, lets shuffle it.
			else
				var/list/datum/polloption/newoptions = list()
				while (options.len)
					var/list/local_options = options.Copy()
					var/key
					//the jist is we randomly remove all options from a copy of options until only one reminds,
					//	move that over to our new list
					//	and repeat until we've moved all of them
					while (local_options.len)
						key = local_options[rand(1, local_options.len)]
						local_options -= key
					var/value = options[key]
					options -= key
					newoptions[key] = value
				options = newoptions

			var/output = {"
				<html>
				<head>
				<meta http-equiv="X-UA-Compatible" content="IE=edge" />
				<script src="jquery.min.js"></script>
				<script src="jquery-ui.custom-core-widgit-mouse-sortable-min.js"></script>
				<style>
					#sortable { list-style-type: none; margin: 0; padding: 2em; }
					#sortable li { min-height: 1em; margin: 0px 1px 1px 1px; padding: 1px; border: 1px solid black; border-radius: 5px; background-color: white; cursor:move;}
					#sortable .sortable-placeholder-highlight { min-height: 1em; margin: 0 2px 2px 2px; padding: 2px; border: 1px dotted blue; border-radius: 5px; background-color: GhostWhite; }
					span.grippy { content: '....'; width: 10px; height: 20px; display: inline-block; overflow: hidden; line-height: 5px; padding: 3px 1px; cursor: move; vertical-align: middle; margin-top: -.7em; margin-right: .3em; font-size: 12px; font-family: sans-serif; letter-spacing: 2px; color: #cccccc; text-shadow: 1px 0 1px black; }
					span.grippy::after { content: '.. .. .. ..';}
				</style>
				<script>
					$(function() {
						$( "#sortable" ).sortable({
							placeholder: "sortable-placeholder-highlight",
							axis: "y",
							containment: "#ballot",
							scroll: false,
							cursor: "ns-resize",
							tolerance: "pointer"
						});
						$( "#sortable" ).disableSelection();
						$('form').submit(function(){
						    $('#IRVdata').val($( "#sortable" ).sortable("toArray", { attribute: "voteid" }));
						});
					});

				</script>
				</head>
				<body>
				<div align='center'><B>Player poll</B><hr>
				<b>Question: [pollquestion]</b><br>Please sort the options in the order of <b>most preferred</b> to <b>least preferred</b><br>
				<font size='2'>Revoting has been enabled on this poll, if you think you made a mistake, simply revote<br></font>
				<font size='2'>Poll runs from <b>[pollstarttime]</b> until <b>[pollendtime]</b></font><p>
				</div>
				<form name='cardcomp' action='?src=[REF(src)]' method='POST'>
				<input type='hidden' name='src' value='[REF(src)]'>
				<input type='hidden' name='votepollid' value='[pollid]'>
				<input type='hidden' name='votetype' value=[POLLTYPE_IRV]>
				<input type='hidden' name='IRVdata' id='IRVdata'>
				<div id="ballot" class="center">
				<b><center>Most Preferred</center></b>
				<ol id="sortable" class="rankings" style="padding:0px">
			"}
			for(var/O in options)
				var/datum/polloption/PO = options["[O]"]
				if(PO.optionid && PO.optiontext)
					output += "<li voteid='[PO.optionid]' class='ranking'><span class='grippy'></span> [PO.optiontext]</li>\n"
			output += {"
				</ol>
					<b><center>Least Preferred</center></b><br>
				</div>
					<p><input type='submit' value='[( votedfor.len ? "Re" : "")]Vote'></form>
			"}
			src << browse(null ,"window=playerpolllist")
			src << browse(output,"window=playerpoll;size=500x500")
	return

//Returns null on failure, TRUE if already voted, FALSE if not voted yet.
/mob/dead/new_player/proc/poll_check_voted(pollid, text = FALSE, silent = FALSE)
	var/table = "poll_vote"
	if (text)
		table = "poll_textreply"
	if (!SSdbcore.Connect())
		to_chat(usr, "<span class='danger'>Failed to establish database connection.</span>")
		return
	var/datum/DBQuery/query_hasvoted = SSdbcore.NewQuery("SELECT id FROM `[format_table_name(table)]` WHERE pollid = [pollid] AND ckey = '[ckey]'")
	if(!query_hasvoted.warn_execute())
		qdel(query_hasvoted)
		return
	if(query_hasvoted.NextRow())
		qdel(query_hasvoted)
		if(!silent)
			to_chat(usr, "<span class='danger'>You've already replied to this poll.</span>")
		return TRUE
	qdel(query_hasvoted)
	return FALSE

//Returns adminrank for use in polls.
/mob/dead/new_player/proc/poll_rank()
	. = "Player"
	if(client.holder)
		. = client.holder.rank.name


/mob/dead/new_player/proc/vote_rig_check()
	if (usr != src)
		if (!usr || !src)
			return 0
		//we gots ourselfs a dirty cheater on our hands!
		log_game("[key_name(usr)] attempted to rig the vote by voting as [key]")
		message_admins("[key_name_admin(usr)] attempted to rig the vote by voting as [key]")
		to_chat(usr, "<span class='danger'>You don't seem to be [key].</span>")
		to_chat(src, "<span class='danger'>Something went horribly wrong processing your vote. Please contact an administrator, they should have gotten a message about this</span>")
		return 0
	return 1

/mob/dead/new_player/proc/vote_valid_check(pollid, holder, type)
	if (!SSdbcore.Connect())
		to_chat(src, "<span class='danger'>Failed to establish database connection.</span>")
		return 0
	pollid = text2num(pollid)
	if (!pollid || pollid < 0)
		return 0
	//validate the poll is actually the right type of poll and its still active
	var/datum/DBQuery/query_validate_poll = SSdbcore.NewQuery("SELECT id FROM [format_table_name("poll_question")] WHERE id = [pollid] AND Now() BETWEEN starttime AND endtime AND polltype = '[type]' [(holder ? "" : "AND adminonly = false")]")
	if(!query_validate_poll.warn_execute())
		qdel(query_validate_poll)
		return 0
	if (!query_validate_poll.NextRow())
		qdel(query_validate_poll)
		return 0
	qdel(query_validate_poll)
	return 1

/mob/dead/new_player/proc/vote_on_irv_poll(pollid, list/votelist)
	if (!SSdbcore.Connect())
		to_chat(src, "<span class='danger'>Failed to establish database connection.</span>")
		return 0
	if (!vote_rig_check())
		return 0
	pollid = text2num(pollid)
	if (!pollid || pollid < 0)
		return 0
	if (!votelist || !istype(votelist) || !votelist.len)
		return 0
	if (!client)
		return 0
	//save these now so we can still process the vote if the client goes away while we process.
	var/datum/admins/holder = client.holder
	var/rank = "Player"
	if (holder)
		rank = holder.rank.name
	var/ckey = client.ckey
	var/address = client.address

	//validate the poll
	if (!vote_valid_check(pollid, holder, POLLTYPE_IRV))
		return 0

	//lets collect the options
	var/datum/DBQuery/query_irv_id = SSdbcore.NewQuery("SELECT id FROM [format_table_name("poll_option")] WHERE pollid = [pollid]")
	if(!query_irv_id.warn_execute())
		qdel(query_irv_id)
		return 0
	var/list/optionlist = list()
	while (query_irv_id.NextRow())
		optionlist += text2num(query_irv_id.item[1])
	qdel(query_irv_id)

	//validate their votes are actually in the list of options and actually numbers
	var/list/numberedvotelist = list()
	for (var/vote in votelist)
		vote = text2num(vote)
		numberedvotelist += vote
		if (!vote) //this is fine because voteid starts at 1, so it will never be 0
			to_chat(src, "<span class='danger'>Error: Invalid (non-numeric) votes in the vote data.</span>")
			return 0
		if (!(vote in optionlist))
			to_chat(src, "<span class='danger'>Votes for choices that do not appear to be in the poll detected.</span>")
			return 0
	if (!numberedvotelist.len)
		to_chat(src, "<span class='danger'>Invalid vote data</span>")
		return 0

	//lets add the vote, first we generate an insert statement.

	var/sqlrowlist = ""
	for (var/vote in numberedvotelist)
		if (sqlrowlist != "")
			sqlrowlist += ", " //a comma (,) at the start of the first row to insert will trigger a SQL error
		sqlrowlist += "(Now(), [pollid], [vote], '[sanitizeSQL(ckey)]', INET_ATON('[sanitizeSQL(address)]'), '[sanitizeSQL(rank)]')"

	//now lets delete their old votes (if any)
	var/datum/DBQuery/query_irv_del_old = SSdbcore.NewQuery("DELETE FROM [format_table_name("poll_vote")] WHERE pollid = [pollid] AND ckey = '[ckey]'")
	if(!query_irv_del_old.warn_execute())
		qdel(query_irv_del_old)
		return 0
	qdel(query_irv_del_old)

	//now to add the new ones.
	var/datum/DBQuery/query_irv_vote = SSdbcore.NewQuery("INSERT INTO [format_table_name("poll_vote")] (datetime, pollid, optionid, ckey, ip, adminrank) VALUES [sqlrowlist]")
	if(!query_irv_vote.warn_execute())
		qdel(query_irv_vote)
		return 0
	qdel(query_irv_vote)
	if(!QDELETED(src))
		src << browse(null,"window=playerpoll")
	return 1


/mob/dead/new_player/proc/vote_on_poll(pollid, optionid)
	if (!SSdbcore.Connect())
		to_chat(src, "<span class='danger'>Failed to establish database connection.</span>")
		return 0
	if (!vote_rig_check())
		return 0
	if(!pollid || !optionid)
		return
	//validate the poll
	if (!vote_valid_check(pollid, client.holder, POLLTYPE_OPTION))
		return 0
	var/voted = poll_check_voted(pollid)
	if(isnull(voted) || voted) //Failed or already voted.
		return
	var/adminrank = sanitizeSQL(poll_rank())
	if(!adminrank)
		return
	var/datum/DBQuery/query_option_vote = SSdbcore.NewQuery("INSERT INTO [format_table_name("poll_vote")] (datetime, pollid, optionid, ckey, ip, adminrank) VALUES (Now(), [pollid], [optionid], '[ckey]', INET_ATON('[client.address]'), '[adminrank]')")
	if(!query_option_vote.warn_execute())
		qdel(query_option_vote)
		return
	qdel(query_option_vote)
	if(!QDELETED(usr))
		usr << browse(null,"window=playerpoll")
	return 1

/mob/dead/new_player/proc/log_text_poll_reply(pollid, replytext)
	if (!SSdbcore.Connect())
		to_chat(src, "<span class='danger'>Failed to establish database connection.</span>")
		return 0
	if (!vote_rig_check())
		return 0
	if(!pollid)
		return
	//validate the poll
	if (!vote_valid_check(pollid, client.holder, POLLTYPE_TEXT))
		return 0
	if(!replytext)
		to_chat(usr, "The text you entered was blank. Please correct the text and submit again.")
		return
	var/voted = poll_check_voted(pollid, text = TRUE, silent = TRUE)
	if(isnull(voted))
		return
	var/adminrank = sanitizeSQL(poll_rank())
	if(!adminrank)
		return
	replytext = sanitizeSQL(replytext)
	if(!(length(replytext) > 0) || !(length(replytext) <= 8000))
		to_chat(usr, "The text you entered was invalid or too long. Please correct the text and submit again.")
		return
	var/datum/DBQuery/query_text_vote
	if(!voted)
		query_text_vote  = SSdbcore.NewQuery("INSERT INTO [format_table_name("poll_textreply")] (datetime ,pollid ,ckey ,ip ,replytext ,adminrank) VALUES (Now(), [pollid], '[ckey]', INET_ATON('[client.address]'), '[replytext]', '[adminrank]')")
	else
		query_text_vote  = SSdbcore.NewQuery("UPDATE [format_table_name("poll_textreply")] SET datetime = Now(), ip = INET_ATON('[client.address]'), replytext = '[replytext]' WHERE pollid = '[pollid]' AND ckey = '[ckey]'")
	if(!query_text_vote.warn_execute())
		qdel(query_text_vote)
		return
	qdel(query_text_vote)
	if(!QDELETED(usr))
		usr << browse(null,"window=playerpoll")
	return 1

/mob/dead/new_player/proc/vote_on_numval_poll(pollid, optionid, rating)
	if (!SSdbcore.Connect())
		to_chat(src, "<span class='danger'>Failed to establish database connection.</span>")
		return 0
	if (!vote_rig_check())
		return 0
	if(!pollid || !optionid || !rating)
		return
	//validate the poll
	if (!vote_valid_check(pollid, client.holder, POLLTYPE_RATING))
		return 0
	var/datum/DBQuery/query_numval_hasvoted = SSdbcore.NewQuery("SELECT id FROM [format_table_name("poll_vote")] WHERE optionid = [optionid] AND ckey = '[ckey]'")
	if(!query_numval_hasvoted.warn_execute())
		qdel(query_numval_hasvoted)
		return
	if(query_numval_hasvoted.NextRow())
		qdel(query_numval_hasvoted)
		to_chat(usr, "<span class='danger'>You've already replied to this poll.</span>")
		return
	qdel(query_numval_hasvoted)
	var/adminrank = "Player"
	if(client.holder)
		adminrank = client.holder.rank.name
	adminrank = sanitizeSQL(adminrank)
	var/datum/DBQuery/query_numval_vote = SSdbcore.NewQuery("INSERT INTO [format_table_name("poll_vote")] (datetime ,pollid ,optionid ,ckey ,ip ,adminrank, rating) VALUES (Now(), [pollid], [optionid], '[ckey]', INET_ATON('[client.address]'), '[adminrank]', [(isnull(rating)) ? "null" : rating])")
	if(!query_numval_vote.warn_execute())
		qdel(query_numval_vote)
		return
	qdel(query_numval_vote)
	if(!QDELETED(usr))
		usr << browse(null,"window=playerpoll")
	return 1

/mob/dead/new_player/proc/vote_on_multi_poll(pollid, optionid)
	if (!SSdbcore.Connect())
		to_chat(src, "<span class='danger'>Failed to establish database connection.</span>")
		return 0
	if (!vote_rig_check())
		return 0
	if(!pollid || !optionid)
		return 1
	//validate the poll
	if (!vote_valid_check(pollid, client.holder, POLLTYPE_MULTI))
		return 0
	var/datum/DBQuery/query_multi_choicelen = SSdbcore.NewQuery("SELECT multiplechoiceoptions FROM [format_table_name("poll_question")] WHERE id = [pollid]")
	if(!query_multi_choicelen.warn_execute())
		qdel(query_multi_choicelen)
		return 1
	var/i
	if(query_multi_choicelen.NextRow())
		i = text2num(query_multi_choicelen.item[1])
	qdel(query_multi_choicelen)
	var/datum/DBQuery/query_multi_hasvoted = SSdbcore.NewQuery("SELECT id FROM [format_table_name("poll_vote")] WHERE pollid = [pollid] AND ckey = '[ckey]'")
	if(!query_multi_hasvoted.warn_execute())
		qdel(query_multi_hasvoted)
		return 1
	while(i)
		if(query_multi_hasvoted.NextRow())
			i--
		else
			break
	qdel(query_multi_hasvoted)
	if(!i)
		return 2
	var/adminrank = "Player"
	if(!QDELETED(client) && client.holder)
		adminrank = client.holder.rank.name
	adminrank = sanitizeSQL(adminrank)
	var/datum/DBQuery/query_multi_vote = SSdbcore.NewQuery("INSERT INTO [format_table_name("poll_vote")] (datetime, pollid, optionid, ckey, ip, adminrank) VALUES (Now(), [pollid], [optionid], '[ckey]', INET_ATON('[client.address]'), '[adminrank]')")
	if(!query_multi_vote.warn_execute())
		qdel(query_multi_vote)
		return 1
	qdel(query_multi_vote)
	if(!QDELETED(usr))
		usr << browse(null,"window=playerpoll")
	return 0
