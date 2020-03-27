
/datum/guardianname
	var/prefixname = "Default" //the prefix the guardian uses for its name
	var/suffixcolour = "Name" //the suffix the guardian uses for its name
	var/parasiteicon = "techbase" //the icon of the guardian
	var/bubbleicon = "holo" //the speechbubble icon of the guardian
	var/theme = "tech" //what the actual theme of the guardian is
	var/colour = "#C3C3C3" //what color the guardian's name is in chat and what color is used for effects from the guardian
	var/stainself = 0 //whether to use the color var to literally dye ourself our chosen colour, for lazy spriting

/datum/guardianname/carp
	bubbleicon = "guardian"
	theme = "carp"
	parasiteicon = "holocarp"
	stainself = 1

/datum/guardianname/carp/New()
	prefixname = pick(GLOB.carp_names)

/datum/guardianname/carp/sand
	suffixcolour = "Sand"
	colour = "#C2B280"

/datum/guardianname/carp/seashell
	suffixcolour = "Seashell"
	colour = "#FFF5EE"

/datum/guardianname/carp/coral
	suffixcolour = "Coral"
	colour = "#FF7F50"

/datum/guardianname/carp/salmon
	suffixcolour = "Salmon"
	colour = "#FA8072"

/datum/guardianname/carp/sunset
	suffixcolour = "Sunset"
	colour = "#FAD6A5"

/datum/guardianname/carp/riptide
	suffixcolour = "Riptide"
	colour = "#89D9C8"

/datum/guardianname/carp/seagreen
	suffixcolour = "Sea Green"
	colour = "#2E8B57"

/datum/guardianname/carp/ultramarine
	suffixcolour = "Ultramarine"
	colour = "#3F00FF"

/datum/guardianname/carp/cerulean
	suffixcolour = "Cerulean"
	colour = "#007BA7"

/datum/guardianname/carp/aqua
	suffixcolour = "Aqua"
	colour = "#00FFFF"

/datum/guardianname/carp/paleaqua
	suffixcolour = "Pale Aqua"
	colour = "#BCD4E6"

/datum/guardianname/carp/hookergreen
	suffixcolour = "Hooker Green"
	colour = "#49796B"

/datum/guardianname/magic
	bubbleicon = "guardian"
	theme = "magic"

/datum/guardianname/magic/New()
	prefixname = pick("Aries", "Leo", "Sagittarius", "Taurus", "Virgo", "Capricorn", "Gemini", "Libra", "Aquarius", "Cancer", "Scorpio", "Pisces", "Ophiuchus")

/datum/guardianname/magic/red
	suffixcolour = "Red"
	parasiteicon = "magicRed"
	colour = "#E32114"

/datum/guardianname/magic/pink
	suffixcolour = "Pink"
	parasiteicon = "magicPink"
	colour = "#FB5F9B"

/datum/guardianname/magic/orange
	suffixcolour = "Orange"
	parasiteicon = "magicOrange"
	colour = "#F3CF24"

/datum/guardianname/magic/green
	suffixcolour = "Green"
	parasiteicon = "magicGreen"
	colour = "#A4E836"

/datum/guardianname/magic/blue
	suffixcolour = "Blue"
	parasiteicon = "magicBlue"
	colour = "#78C4DB"

/datum/guardianname/tech/New()
	prefixname = pick("Gallium", "Indium", "Thallium", "Bismuth", "Aluminium", "Mercury", "Iron", "Silver", "Zinc", "Titanium", "Chromium", "Nickel", "Platinum", "Tellurium", "Palladium", "Rhodium", "Cobalt", "Osmium", "Tungsten", "Iridium")

/datum/guardianname/tech/rose
	suffixcolour = "Rose"
	parasiteicon = "techRose"
	colour = "#F62C6B"

/datum/guardianname/tech/peony
	suffixcolour = "Peony"
	parasiteicon = "techPeony"
	colour = "#E54750"

/datum/guardianname/tech/lily
	suffixcolour = "Lily"
	parasiteicon = "techLily"
	colour = "#F6562C"

/datum/guardianname/tech/daisy
	suffixcolour = "Daisy"
	parasiteicon = "techDaisy"
	colour = "#ECCD39"

/datum/guardianname/tech/zinnia
	suffixcolour = "Zinnia"
	parasiteicon = "techZinnia"
	colour = "#89F62C"

/datum/guardianname/tech/ivy
	suffixcolour = "Ivy"
	parasiteicon = "techIvy"
	colour = "#5DF62C"

/datum/guardianname/tech/iris
	suffixcolour = "Iris"
	parasiteicon = "techIris"
	colour = "#2CF6B8"

/datum/guardianname/tech/petunia
	suffixcolour = "Petunia"
	parasiteicon = "techPetunia"
	colour = "#51A9D4"

/datum/guardianname/tech/violet
	suffixcolour = "Violet"
	parasiteicon = "techViolet"
	colour = "#8A347C"

/datum/guardianname/tech/lotus
	suffixcolour = "Lotus"
	parasiteicon = "techLotus"
	colour = "#463546"

/datum/guardianname/tech/lilac
	suffixcolour = "Lilac"
	parasiteicon = "techLilac"
	colour = "#C7A0F6"

/datum/guardianname/tech/orchid
	suffixcolour = "Orchid"
	parasiteicon = "techOrchid"
	colour = "#F62CF5"
