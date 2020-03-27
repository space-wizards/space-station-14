#define BYOND_EPOCH 2451544.5
#define HEBREW_EPOCH 347995.5

/*
Source for the method of calcuation
https://www.fourmilab.ch/documents/calendar/
by John Walker 2015, released under public domain
*/
/datum/hebrew_calendar
	var/dd
	var/mm
	var/yy

/datum/hebrew_calendar/New()
	var/julian = realtime_to_jd()
	set_date(julian)

/datum/hebrew_calendar/proc/hebrew_leap(year)
	switch (year % 19)
		if (0, 3, 6, 8, 11, 14, 17)
			return TRUE
		else
			return FALSE

// Hebrew to Julian
/datum/hebrew_calendar/proc/hebrew_to_jd(year, month, day)
	var/months = hebrew_year_months(year)
	var/jd = HEBREW_EPOCH + hebrew_delay_1(year) + hebrew_delay_2(year) + day + 1
	if (month < 7)
		for (var/mon = 7; mon <= months; mon++)
			jd += hebrew_month_days(year, mon)
		for (var/mon = 1; mon < month; mon++)
			jd += hebrew_month_days(year, mon)
	else
		for (var/mon = 7; mon < month; mon++)
			jd += hebrew_month_days(year, mon)
	return jd


// Julian to Hebrew
/datum/hebrew_calendar/proc/set_date(jd)
	jd = round(jd) + 0.5
	var/count = round(((jd - HEBREW_EPOCH) * 98496) / 35975351)
	var/year = count - 1
	for (var/i = count; jd >= hebrew_to_jd(i, 7, 1); i++)
		year++
	var/month = (jd < hebrew_to_jd(year, 1, 1)) ? 7 : 1
	for (var/i = month; jd > hebrew_to_jd(year, i, hebrew_month_days(year, i)); i++)
		month++
	var/day = (jd - hebrew_to_jd(year, month, 1)) + 1
	yy = year
	mm = month
	dd = day

/datum/hebrew_calendar/proc/hebrew_year_months(year)
	if (hebrew_leap(year))
		return 13
	else
		return 12

// Delay based on starting day of the year
/datum/hebrew_calendar/proc/hebrew_delay_1(year)
	var/months = round(((235 * year) - 234) / 19)
	var/parts = 12084 + (13753 * months)
	var/day = (months * 29) + round(parts / 25920)
	if (3 * (day + 1) % 7 < 3)
		day++
	return day

// Delay based on length of adjacent years
/datum/hebrew_calendar/proc/hebrew_delay_2(year)
	var/last = hebrew_delay_1(year - 1)
	var/present = hebrew_delay_1(year)
	var/next = hebrew_delay_1(year + 1)
	if (next - present == 356)
		return 2
	else if (present - last == 382)
		return 1
	else
		return 0

/datum/hebrew_calendar/proc/hebrew_year_days(year)
	return hebrew_to_jd(year + 1, 7, 1) - hebrew_to_jd(year, 7, 1)

/datum/hebrew_calendar/proc/hebrew_month_days(year, month)
	switch (month)
		//  First of all, dispose of fixed-length 29 day months
		if (2, 4, 6, 10, 13)
			return 29
   		//  If it's not a leap year, Adar has 29 days
		if (12)
			if (!hebrew_leap(year))
				return 29
		//  If it's Heshvan, days depend on length of year
		if (8)
			if (hebrew_year_days(year) % 10 != 5)
				return 29
		//  Similarly, Kislev varies with the length of year
		if (9)
			if (hebrew_year_days(year) % 10 == 3)
				return 29
	//  Nope, it's a 30 day month
	return 30

/datum/hebrew_calendar/proc/realtime_to_jd()
	return round(world.realtime / 864000) + BYOND_EPOCH

#undef BYOND_EPOCH
#undef HEBREW_EPOCH
