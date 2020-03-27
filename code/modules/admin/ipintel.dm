/datum/ipintel
	var/ip
	var/intel = 0
	var/cache = FALSE
	var/cacheminutesago = 0
	var/cachedate = ""
	var/cacherealtime = 0

/datum/ipintel/New()
	cachedate = SQLtime()
	cacherealtime = world.realtime

/datum/ipintel/proc/is_valid()
	. = FALSE
	if (intel < 0)
		return
	if (intel <= CONFIG_GET(number/ipintel_rating_bad))
		if (world.realtime < cacherealtime + (CONFIG_GET(number/ipintel_save_good) * 60 * 60 * 10))
			return TRUE
	else
		if (world.realtime < cacherealtime + (CONFIG_GET(number/ipintel_save_bad) * 60 * 60 * 10))
			return TRUE

/proc/get_ip_intel(ip, bypasscache = FALSE, updatecache = TRUE)
	var/datum/ipintel/res = new()
	res.ip = ip
	. = res
	if (!ip || !CONFIG_GET(string/ipintel_email) || !SSipintel.enabled)
		return
	if (!bypasscache)
		var/datum/ipintel/cachedintel = SSipintel.cache[ip]
		if (cachedintel && cachedintel.is_valid())
			cachedintel.cache = TRUE
			return cachedintel

		if(SSdbcore.Connect())
			var/rating_bad = CONFIG_GET(number/ipintel_rating_bad)
			var/datum/DBQuery/query_get_ip_intel = SSdbcore.NewQuery({"
				SELECT date, intel, TIMESTAMPDIFF(MINUTE,date,NOW())
				FROM [format_table_name("ipintel")]
				WHERE
					ip = INET_ATON('[ip]')
					AND ((
							intel < [rating_bad]
							AND
							date + INTERVAL [CONFIG_GET(number/ipintel_save_good)] HOUR > NOW()
						) OR (
							intel >= [rating_bad]
							AND
							date + INTERVAL [CONFIG_GET(number/ipintel_save_bad)] HOUR > NOW()
					))
				"})
			if(!query_get_ip_intel.Execute())
				qdel(query_get_ip_intel)
				return
			if (query_get_ip_intel.NextRow())
				res.cache = TRUE
				res.cachedate = query_get_ip_intel.item[1]
				res.intel = text2num(query_get_ip_intel.item[2])
				res.cacheminutesago = text2num(query_get_ip_intel.item[3])
				res.cacherealtime = world.realtime - (text2num(query_get_ip_intel.item[3])*10*60)
				SSipintel.cache[ip] = res
				qdel(query_get_ip_intel)
				return
			qdel(query_get_ip_intel)
	res.intel = ip_intel_query(ip)
	if (updatecache && res.intel >= 0)
		SSipintel.cache[ip] = res
		if(SSdbcore.Connect())
			var/datum/DBQuery/query_add_ip_intel = SSdbcore.NewQuery("INSERT INTO [format_table_name("ipintel")] (ip, intel) VALUES (INET_ATON('[ip]'), [res.intel]) ON DUPLICATE KEY UPDATE intel = VALUES(intel), date = NOW()")
			query_add_ip_intel.Execute()
			qdel(query_add_ip_intel)


/proc/ip_intel_query(ip, retryed=0)
	. = -1 //default
	if (!ip)
		return
	if (SSipintel.throttle > world.timeofday)
		return
	if (!SSipintel.enabled)
		return

	var/list/http[] = world.Export("http://[CONFIG_GET(string/ipintel_domain)]/check.php?ip=[ip]&contact=[CONFIG_GET(string/ipintel_email)]&format=json&flags=f")

	if (http)
		var/status = text2num(http["STATUS"])

		if (status == 200)
			var/response = json_decode(file2text(http["CONTENT"]))
			if (response)
				if (response["status"] == "success")
					var/intelnum = text2num(response["result"])
					if (isnum(intelnum))
						return text2num(response["result"])
					else
						ipintel_handle_error("Bad intel from server: [response["result"]].", ip, retryed)
						if (!retryed)
							sleep(25)
							return .(ip, 1)
				else
					ipintel_handle_error("Bad response from server: [response["status"]].", ip, retryed)
					if (!retryed)
						sleep(25)
						return .(ip, 1)

		else if (status == 429)
			ipintel_handle_error("Error #429: We have exceeded the rate limit.", ip, 1)
			return
		else
			ipintel_handle_error("Unknown status code: [status].", ip, retryed)
			if (!retryed)
				sleep(25)
				return .(ip, 1)
	else
		ipintel_handle_error("Unable to connect to API.", ip, retryed)
		if (!retryed)
			sleep(25)
			return .(ip, 1)


/proc/ipintel_handle_error(error, ip, retryed)
	if (retryed)
		SSipintel.errors++
		error += " Could not check [ip]. Disabling IPINTEL for [SSipintel.errors] minute[( SSipintel.errors == 1 ? "" : "s" )]"
		SSipintel.throttle = world.timeofday + (10 * 120 * SSipintel.errors)
	else
		error += " Attempting retry on [ip]."
	log_ipintel(error)

/proc/log_ipintel(text)
	log_game("IPINTEL: [text]")
	debug_admins("IPINTEL: [text]")





