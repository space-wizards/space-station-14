/datum/admins/proc/stickyban(action,data)
	if(!check_rights(R_BAN))
		return
	switch (action)
		if ("show")
			stickyban_show()
		if ("add")
			var/list/ban = list()
			var/ckey
			ban["admin"] = usr.ckey
			ban["type"] = list("sticky")
			ban["reason"] = "(InGameBan)([usr.key])" //this will be displayed in dd only

			if (data["ckey"])
				ckey = ckey(data["ckey"])
			else
				ckey = input(usr,"Ckey","Ckey","") as text|null
				if (!ckey)
					return
				ckey = ckey(ckey)
			ban["ckey"] = ckey

			if (get_stickyban_from_ckey(ckey))
				to_chat(usr, "<span class='adminnotice'>Error: Can not add a stickyban: User already has a current sticky ban</span>")
				return

			if (data["reason"])
				ban["message"] = data["reason"]
			else
				var/reason = input(usr,"Reason","Reason","Ban Evasion") as text|null
				if (!reason)
					return
				ban["message"] = "[reason]"

			if(SSdbcore.Connect())
				var/datum/DBQuery/query_create_stickyban = SSdbcore.NewQuery("INSERT INTO [format_table_name("stickyban")] (ckey, reason, banning_admin) VALUES ('[sanitizeSQL(ckey)]', '[sanitizeSQL(ban["message"])]', '[sanitizeSQL(usr.ckey)]')")
				if (query_create_stickyban.warn_execute())
					ban["fromdb"] = TRUE
				qdel(query_create_stickyban)

			world.SetConfig("ban",ckey,list2stickyban(ban))
			ban = stickyban2list(list2stickyban(ban))
			ban["matches_this_round"] = list()
			ban["existing_user_matches_this_round"] = list()
			ban["admin_matches_this_round"] = list()
			ban["pending_matches_this_round"] = list()
			SSstickyban.cache[ckey] = ban

			log_admin_private("[key_name(usr)] has stickybanned [ckey].\nReason: [ban["message"]]")
			message_admins("<span class='adminnotice'>[key_name_admin(usr)] has stickybanned [ckey].\nReason: [ban["message"]]</span>")

		if ("remove")
			if (!data["ckey"])
				return
			var/ckey = data["ckey"]

			var/ban = get_stickyban_from_ckey(ckey)
			if (!ban)
				to_chat(usr, "<span class='adminnotice'>Error: No sticky ban for [ckey] found!</span>")
				return
			if (alert("Are you sure you want to remove the sticky ban on [ckey]?","Are you sure","Yes","No") == "No")
				return
			if (!get_stickyban_from_ckey(ckey))
				to_chat(usr, "<span class='adminnotice'>Error: The ban disappeared.</span>")
				return
			world.SetConfig("ban",ckey, null)
			SSstickyban.cache -= ckey

			if (SSdbcore.Connect())
				SSdbcore.QuerySelect(list(
					SSdbcore.NewQuery("DELETE FROM [format_table_name("stickyban")] WHERE ckey = '[sanitizeSQL(ckey)]'"),
					SSdbcore.NewQuery("DELETE FROM [format_table_name("stickyban_matched_ckey")] WHERE stickyban = '[sanitizeSQL(ckey)]'"),
					SSdbcore.NewQuery("DELETE FROM [format_table_name("stickyban_matched_cid")] WHERE stickyban = '[sanitizeSQL(ckey)]'"),
					SSdbcore.NewQuery("DELETE FROM [format_table_name("stickyban_matched_ip")] WHERE stickyban = '[sanitizeSQL(ckey)]'")
				), warn = TRUE, qdel = TRUE)


			log_admin_private("[key_name(usr)] removed [ckey]'s stickyban")
			message_admins("<span class='adminnotice'>[key_name_admin(usr)] removed [ckey]'s stickyban</span>")

		if ("remove_alt")
			if (!data["ckey"])
				return
			var/ckey = data["ckey"]
			if (!data["alt"])
				return
			var/alt = ckey(data["alt"])
			var/ban = get_stickyban_from_ckey(ckey)
			if (!ban)
				to_chat(usr, "<span class='adminnotice'>Error: No sticky ban for [ckey] found!</span>")
				return

			var/key = LAZYACCESS(ban["keys"], alt)
			if (!key)
				to_chat(usr, "<span class='adminnotice'>Error: [alt] is not linked to [ckey]'s sticky ban!</span>")
				return

			if (alert("Are you sure you want to disassociate [alt] from [ckey]'s sticky ban? \nNote: Nothing stops byond from re-linking them, Use \[E] to exempt them","Are you sure","Yes","No") == "No")
				return

			//we have to do this again incase something changes
			ban = get_stickyban_from_ckey(ckey)
			if (!ban)
				to_chat(usr, "<span class='adminnotice'>Error: The ban disappeared.</span>")
				return

			key = LAZYACCESS(ban["keys"], alt)

			if (!key)
				to_chat(usr, "<span class='adminnotice'>Error: [alt] link to [ckey]'s sticky ban disappeared.</span>")
				return

			LAZYREMOVE(ban["keys"], alt)
			world.SetConfig("ban",ckey,list2stickyban(ban))

			SSstickyban.cache[ckey] = ban

			if (SSdbcore.Connect())
				var/datum/DBQuery/query_remove_stickyban_alt = SSdbcore.NewQuery("DELETE FROM [format_table_name("stickyban_matched_ckey")] WHERE stickyban = '[sanitizeSQL(ckey)]' AND matched_ckey = '[sanitizeSQL(alt)]'")
				query_remove_stickyban_alt.warn_execute()
				qdel(query_remove_stickyban_alt)

			log_admin_private("[key_name(usr)] has disassociated [alt] from [ckey]'s sticky ban")
			message_admins("<span class='adminnotice'>[key_name_admin(usr)] has disassociated [alt] from [ckey]'s sticky ban</span>")

		if ("edit")
			if (!data["ckey"])
				return
			var/ckey = data["ckey"]
			var/ban = get_stickyban_from_ckey(ckey)
			if (!ban)
				to_chat(usr, "<span class='adminnotice'>Error: No sticky ban for [ckey] found!</span>")
				return
			var/oldreason = ban["message"]
			var/reason = input(usr,"Reason","Reason","[ban["message"]]") as text|null
			if (!reason || reason == oldreason)
				return
			//we have to do this again incase something changed while we waited for input
			ban = get_stickyban_from_ckey(ckey)
			if (!ban)
				to_chat(usr, "<span class='adminnotice'>Error: The ban disappeared.</span>")
				return
			ban["message"] = "[reason]"

			world.SetConfig("ban",ckey,list2stickyban(ban))

			SSstickyban.cache[ckey] = ban

			if (SSdbcore.Connect())
				var/datum/DBQuery/query_edit_stickyban = SSdbcore.NewQuery("UPDATE [format_table_name("stickyban")] SET reason = '[sanitizeSQL(reason)]' WHERE ckey = '[sanitizeSQL(ckey)]'")
				query_edit_stickyban.warn_execute()
				qdel(query_edit_stickyban)

			log_admin_private("[key_name(usr)] has edited [ckey]'s sticky ban reason from [oldreason] to [reason]")
			message_admins("<span class='adminnotice'>[key_name_admin(usr)] has edited [ckey]'s sticky ban reason from [oldreason] to [reason]</span>")

		if ("exempt")
			if (!data["ckey"])
				return
			var/ckey = data["ckey"]
			if (!data["alt"])
				return
			var/alt = ckey(data["alt"])
			var/ban = get_stickyban_from_ckey(ckey)
			if (!ban)
				to_chat(usr, "<span class='adminnotice'>Error: No sticky ban for [ckey] found!</span>")
				return

			var/key = LAZYACCESS(ban["keys"], alt)
			if (!key)
				to_chat(usr, "<span class='adminnotice'>Error: [alt] is not linked to [ckey]'s sticky ban!</span>")
				return

			if (alert("Are you sure you want to exempt [alt] from [ckey]'s sticky ban?","Are you sure","Yes","No") == "No")
				return

			//we have to do this again incase something changes
			ban = get_stickyban_from_ckey(ckey)
			if (!ban)
				to_chat(usr, "<span class='adminnotice'>Error: The ban disappeared.</span>")
				return

			key = LAZYACCESS(ban["keys"], alt)

			if (!key)
				to_chat(usr, "<span class='adminnotice'>Error: [alt]'s link to [ckey]'s sticky ban disappeared.</span>")
				return
			LAZYREMOVE(ban["keys"], alt)
			key["exempt"] = TRUE
			LAZYSET(ban["whitelist"], alt, key)

			world.SetConfig("ban",ckey,list2stickyban(ban))

			SSstickyban.cache[ckey] = ban

			if (SSdbcore.Connect())
				var/datum/DBQuery/query_exempt_stickyban_alt = SSdbcore.NewQuery("UPDATE [format_table_name("stickyban_matched_ckey")] SET exempt = 1 WHERE stickyban = '[sanitizeSQL(ckey)]' AND matched_ckey = '[sanitizeSQL(alt)]'")
				query_exempt_stickyban_alt.warn_execute()
				qdel(query_exempt_stickyban_alt)

			log_admin_private("[key_name(usr)] has exempted [alt] from [ckey]'s sticky ban")
			message_admins("<span class='adminnotice'>[key_name_admin(usr)] has exempted [alt] from [ckey]'s sticky ban</span>")

		if ("unexempt")
			if (!data["ckey"])
				return
			var/ckey = data["ckey"]
			if (!data["alt"])
				return
			var/alt = ckey(data["alt"])
			var/ban = get_stickyban_from_ckey(ckey)
			if (!ban)
				to_chat(usr, "<span class='adminnotice'>Error: No sticky ban for [ckey] found!</span>")
				return

			var/key = LAZYACCESS(ban["whitelist"], alt)
			if (!key)
				to_chat(usr, "<span class='adminnotice'>Error: [alt] is not exempt from [ckey]'s sticky ban!</span>")
				return

			if (alert("Are you sure you want to unexempt [alt] from [ckey]'s sticky ban?","Are you sure","Yes","No") == "No")
				return

			//we have to do this again incase something changes
			ban = get_stickyban_from_ckey(ckey)
			if (!ban)
				to_chat(usr, "<span class='adminnotice'>Error: The ban disappeared.</span>")
				return

			key = LAZYACCESS(ban["whitelist"], alt)
			if (!key)
				to_chat(usr, "<span class='adminnotice'>Error: [alt]'s exemption from [ckey]'s sticky ban disappeared.</span>")
				return

			LAZYREMOVE(ban["whitelist"], alt)
			key["exempt"] = FALSE
			LAZYSET(ban["keys"], alt, key)

			world.SetConfig("ban",ckey,list2stickyban(ban))

			SSstickyban.cache[ckey] = ban

			if (SSdbcore.Connect())
				var/datum/DBQuery/query_unexempt_stickyban_alt = SSdbcore.NewQuery("UPDATE [format_table_name("stickyban_matched_ckey")] SET exempt = 0 WHERE stickyban = '[sanitizeSQL(ckey)]' AND matched_ckey = '[sanitizeSQL(alt)]'")
				query_unexempt_stickyban_alt.warn_execute()
				qdel(query_unexempt_stickyban_alt)

			log_admin_private("[key_name(usr)] has unexempted [alt] from [ckey]'s sticky ban")
			message_admins("<span class='adminnotice'>[key_name_admin(usr)] has unexempted [alt] from [ckey]'s sticky ban</span>")

		if ("timeout")
			if (!data["ckey"])
				return
			if (!SSdbcore.Connect())
				to_chat(usr, "<span class='adminnotice'>No database connection!</span>")
				return

			var/ckey = data["ckey"]

			if (alert("Are you sure you want to put [ckey]'s stickyban on timeout until next round (or removed)?","Are you sure","Yes","No") == "No")
				return
			var/ban = get_stickyban_from_ckey(ckey)
			if (!ban)
				to_chat(usr, "<span class='adminnotice'>Error: No sticky ban for [ckey] found!</span>")
				return

			ban["timeout"] = TRUE

			world.SetConfig("ban", ckey, null)

			var/cachedban = SSstickyban.cache[ckey]
			if (cachedban)
				cachedban["timeout"] = TRUE

			log_admin_private("[key_name(usr)] has put [ckey]'s sticky ban on timeout.")
			message_admins("<span class='adminnotice'>[key_name_admin(usr)] has put [ckey]'s sticky ban on timeout.</span>")

		if ("untimeout")
			if (!data["ckey"])
				return
			if (!SSdbcore.Connect())
				to_chat(usr, "<span class='adminnotice'>No database connection!</span>")
				return
			var/ckey = data["ckey"]

			if (alert("Are you sure you want to lift the timeout on [ckey]'s stickyban?","Are you sure","Yes","No") == "No")
				return

			var/ban = get_stickyban_from_ckey(ckey)
			var/cachedban = SSstickyban.cache[ckey]
			if (cachedban)
				cachedban["timeout"] = FALSE
			if (!ban)
				if (!cachedban)
					to_chat(usr, "<span class='adminnotice'>Error: No sticky ban for [ckey] found!</span>")
					return
				ban = cachedban

			ban["timeout"] = FALSE

			world.SetConfig("ban",ckey,list2stickyban(ban))

			log_admin_private("[key_name(usr)] has taken [ckey]'s sticky ban off of timeout.")
			message_admins("<span class='adminnotice'>[key_name_admin(usr)] has taken [ckey]'s sticky ban off of timeout.</span>")


		if ("revert")
			if (!data["ckey"])
				return
			var/ckey = data["ckey"]
			if (alert("Are you sure you want to revert the sticky ban on [ckey] to its state at round start (or last edit)?","Are you sure","Yes","No") == "No")
				return
			var/ban = get_stickyban_from_ckey(ckey)
			if (!ban)
				to_chat(usr, "<span class='adminnotice'>Error: No sticky ban for [ckey] found!</span>")
				return
			var/cached_ban = SSstickyban.cache[ckey]
			if (!cached_ban)
				to_chat(usr, "<span class='adminnotice'>Error: No cached sticky ban for [ckey] found!</span>")
			world.SetConfig("ban",ckey,null)

			log_admin_private("[key_name(usr)] has reverted [ckey]'s sticky ban to its state at round start.")
			message_admins("<span class='adminnotice'>[key_name_admin(usr)] has reverted [ckey]'s sticky ban to its state at round start.</span>")
			//revert is mostly used when shit goes rouge, so we have to set it to null
			//	and wait a byond tick before assigning it to ensure byond clears its shit.
			sleep(world.tick_lag)
			world.SetConfig("ban",ckey,list2stickyban(cached_ban))


/datum/admins/proc/stickyban_gethtml(ckey)
	var/ban = get_stickyban_from_ckey(ckey)
	if (!ban)
		return
	var/timeout
	if (SSdbcore.Connect())
		timeout = "<a href='?_src_=holder;[HrefToken()];stickyban=[(ban["timeout"] ? "untimeout" : "timeout")]&ckey=[ckey]'>\[[(ban["timeout"] ? "untimeout" : "timeout" )]\]</a>"
	else
		timeout = "<a href='?_src_=holder;[HrefToken()];stickyban=revert&ckey=[ckey]'>\[revert\]</a>"
	. = list({"
		<a href='?_src_=holder;[HrefToken()];stickyban=remove&ckey=[ckey]'>\[-\]</a>
		[timeout]
		<b>[ckey]</b>
		<br />"
		[ban["message"]] <b><a href='?_src_=holder;[HrefToken()];stickyban=edit&ckey=[ckey]'>\[Edit\]</a></b><br />
	"})
	if (ban["admin"])
		. += "[ban["admin"]]<br />"
	else
		. += "LEGACY<br />"
	. += "Caught keys<br />\n<ol>"
	for (var/key in ban["keys"])
		if (ckey(key) == ckey)
			continue
		. += "<li><a href='?_src_=holder;[HrefToken()];stickyban=remove_alt&ckey=[ckey]&alt=[ckey(key)]'>\[-\]</a>[key]<a href='?_src_=holder;[HrefToken()];stickyban=exempt&ckey=[ckey]&alt=[ckey(key)]'>\[E\]</a></li>"

	for (var/key in ban["whitelist"])
		if (ckey(key) == ckey)
			continue
		. += "<li><a href='?_src_=holder;[HrefToken()];stickyban=remove_alt&ckey=[ckey]&alt=[ckey(key)]'>\[-\]</a>[key]<a href='?_src_=holder;[HrefToken()];stickyban=unexempt&ckey=[ckey]&alt=[ckey(key)]'>\[UE\]</a></li>"

	. += "</ol>\n"

/datum/admins/proc/stickyban_show()
	if(!check_rights(R_BAN))
		return
	var/list/bans = sticky_banned_ckeys()
	var/list/banhtml = list()
	for(var/key in bans)
		var/ckey = ckey(key)
		banhtml += "<br /><hr />\n"
		banhtml += stickyban_gethtml(ckey)

	var/html = {"
	<head>
		<title>Sticky Bans</title>
	</head>
	<body>
		<h2>All Sticky Bans:</h2> <a href='?_src_=holder;[HrefToken()];stickyban=add'>\[+\]</a><br>
		[banhtml.Join("")]
	</body>
	"}
	usr << browse(html,"window=stickybans;size=700x400")

/proc/sticky_banned_ckeys()
	if (SSdbcore.Connect() || length(SSstickyban.dbcache))
		if (SSstickyban.dbcacheexpire < world.time)
			SSstickyban.Populatedbcache()
		if (SSstickyban.dbcacheexpire)
			return SSstickyban.dbcache.Copy()

	return sortList(world.GetConfig("ban"))


/proc/get_stickyban_from_ckey(var/ckey)
	. = list()
	if (!ckey)
		return null
	if (SSdbcore.Connect() || length(SSstickyban.dbcache))
		if (SSstickyban.dbcacheexpire < world.time)
			SSstickyban.Populatedbcache()
		if (SSstickyban.dbcacheexpire)
			. = SSstickyban.dbcache[ckey]
			//reset the cache incase its a newer ban (but only if we didn't update the cache recently)
			if (!. && SSstickyban.dbcacheexpire != world.time+STICKYBAN_DB_CACHE_TIME)
				SSstickyban.dbcacheexpire = 1
				SSstickyban.Populatedbcache()
				. = SSstickyban.dbcache[ckey]
			if (.)
				var/list/cachedban = SSstickyban.cache["[ckey]"]
				if (cachedban)
					.["timeout"] = cachedban["timeout"]

				.["fromdb"] = TRUE
			return

	. = stickyban2list(world.GetConfig("ban", ckey)) || stickyban2list(world.GetConfig("ban", ckey(ckey))) || list()

	if (!length(.))
		return null

/proc/stickyban2list(ban, strictdb = TRUE)
	if (!ban)
		return null
	. = params2list(ban)
	if (.["keys"])
		var/keys = splittext(.["keys"], ",")
		var/ckeys = list()
		for (var/key in keys)
			var/ckey = ckey(key)
			ckeys[ckey] = ckey //to make searching faster.
		.["keys"] = ckeys
	if (.["whitelist"])
		var/keys = splittext(.["whitelist"], ",")
		var/ckeys = list()
		for (var/key in keys)
			var/ckey = ckey(key)
			ckeys[ckey] = ckey //to make searching faster.
		.["whitelist"] = ckeys
	.["type"] = splittext(.["type"], ",")
	.["IP"] = splittext(.["IP"], ",")
	.["computer_id"] = splittext(.["computer_id"], ",")
	. -= "fromdb"


/proc/list2stickyban(list/ban)
	if (!ban || !islist(ban))
		return null
	. = ban.Copy()
	if (.["keys"])
		.["keys"] = jointext(.["keys"], ",")
	if (.["IP"])
		.["IP"] = jointext(.["IP"], ",")
	if (.["computer_id"])
		.["computer_id"] = jointext(.["computer_id"], ",")
	if (.["whitelist"])
		.["whitelist"] = jointext(.["whitelist"], ",")
	if (.["type"])
		.["type"] = jointext(.["type"], ",")

	. -= "reverting"
	. -= "matches_this_round"
	. -= "existing_user_matches_this_round"
	. -= "admin_matches_this_round"
	. -= "pending_matches_this_round"


	. = list2params(.)


/client/proc/stickybanpanel()
	set name = "Sticky Ban Panel"
	set category = "Admin"
	if (!holder)
		return
	holder.stickyban_show()
