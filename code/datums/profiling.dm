//these are real globals so you can use profiling to profile early world init stuff.
GLOBAL_REAL_VAR(list/PROFILE_STORE)
GLOBAL_REAL_VAR(PROFILE_LINE)
GLOBAL_REAL_VAR(PROFILE_FILE)
GLOBAL_REAL_VAR(PROFILE_SLEEPCHECK)
GLOBAL_REAL_VAR(PROFILE_TIME)


/proc/profile_show(user, sort = /proc/cmp_profile_avg_time_dsc)
	sortTim(PROFILE_STORE, sort, TRUE)

	var/list/lines = list()

	for (var/entry in PROFILE_STORE)
		var/list/data = PROFILE_STORE[entry]
		lines += "[entry] => [num2text(data[PROFILE_ITEM_TIME], 10)]ms ([data[PROFILE_ITEM_COUNT]]) (avg:[num2text(data[PROFILE_ITEM_TIME]/(data[PROFILE_ITEM_COUNT] || 1), 99)])"

	user << browse("<ol><li>[lines.Join("</li><li>")]</li></ol>", "window=[url_encode(GUID())]")
