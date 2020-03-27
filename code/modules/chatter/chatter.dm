/proc/chatter(message, phomeme, atom/A)
	// We want to transform any message into a list of numbers
	// and punctuation marks
	// For example:
	// "Hi." -> [2, '.']
	// "HALP GEROGE MELLONS, that swine, is GRIFFIN ME!"
	// -> [4, 6, 7, ',', 4, 5, ',', '2', 7, 2, '!']
	// "fuck,thissentenceissquashed" -> [4, ',', 21]

	var/list/punctuation = list(",",":",";",".","?","!","\'","-")
	var/regex/R = regex("(\[\\l\\d]*)(\[^\\l\\d\\s])?", "g")
	var/list/letter_count = list()
	while(R.Find(message) != 0)
		if(R.group[1])
			letter_count += length(R.group[1])
		if(R.group[2])
			letter_count += R.group[2]

	spawn(0)
		for(var/item in letter_count)
			if (item in punctuation)
				// simulate pausing in talking
				// ignore semi-colons because of their use in HTML escaping
				if (item in list(",", ":"))
					sleep(3)
				if (item in list("!", "?", "."))
					sleep(6)
				continue

			if(isnum(item))
				var/length = min(item, 10)
				if (length == 0)
					// "verbalise" long spaces
					sleep(1)
				chatter_speak_word(A.loc, phomeme, length)

/proc/chatter_speak_word(loc, phomeme, length)
	var/path = "sound/chatter/[phomeme]_[length].ogg"

	playsound(loc, path,
		vol = 40, vary = 0, extrarange = 3, falloff = FALSE)

	sleep((length + 1) * chatter_get_sleep_multiplier(phomeme))

/proc/chatter_get_sleep_multiplier(phomeme)
	// These values are tenths of seconds, so 0.5 == 0.05seconds
	. = 1
	switch(phomeme)
		if("papyrus")
			. = 0.5
		if("griffin")
			. = 0.5
		if("sans")
			. = 0.7
		if("owl")
			. = 0.7
