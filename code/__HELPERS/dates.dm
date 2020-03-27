//Curse you calenders...
/proc/IsLeapYear(y)
	return ((y) % 4 == 0 && ((y) % 100 != 0 || (y) % 400 == 0))

//Y, eg: 2017, 2018, 2019, in num form (not string)
//etc. Between 1583 and 4099
//Adapted from a free algorithm written in BASIC (https://www.assa.org.au/edm#Computer)
/proc/EasterDate(y)
	var/FirstDig, Remain19, temp	//Intermediate Results
	var/tA, tB, tC, tD, tE			//Table A-E results
	var/d, m						//Day and Month returned

	FirstDig = round((y / 100))
	Remain19 = y % 19

	temp = (round((FirstDig - 15) / 2)) + 202 - 11 * Remain19

	switch(FirstDig)
		if(21,24,25,27,28,29,30,31,32,34,35,38)
			temp -= 1
		if(33,36,37,39,40)
			temp -= 2
	temp %= 30

	tA = temp + 21
	if(temp == 29)
		tA -= 1
	if(temp == 28 && (Remain19 > 10))
		tA -= 1
	tB = (tA - 19) % 7

	tC = (40 - FirstDig) % 4
	if(tC == 3)
		tC += 1
	if(tC > 1)
		tC += 1
	temp = y % 100
	tD = (temp + round((temp / 4))) % 7

	tE = ((20 - tB - tC - tD) % 7) + 1
	d = tA + tE
	if(d > 31)
		d -= 31
		m = 4
	else
		m = 3
	return list("day" = d, "month" = m)
