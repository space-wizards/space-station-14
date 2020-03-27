/datum/language/narsie
	name = "Nar'Sian"
	desc = "The ancient, blood-soaked, impossibly complex language of Nar'Sian cultists."
	speech_verb = "intones"
	ask_verb = "inquires"
	exclaim_verb = "invokes"
	key = "n"
	sentence_chance = 8
	space_chance = 95 //very high due to the potential length of each syllable
	var/static/list/base_syllables = list(
		"h", "v", "c", "e", "g", "d", "r", "n", "h", "o", "p",
		"ra", "so", "at", "il", "ta", "gh", "sh", "ya", "te", "sh", "ol", "ma", "om", "ig", "ni", "in",
		"sha", "mir", "sas", "mah", "zar", "tok", "lyr", "nqa", "nap", "olt", "val", "qha",
		"fwe", "ath", "yro", "eth", "gal", "gib", "bar", "jin", "kla", "atu", "kal", "lig",
		"yoka", "drak", "loso", "arta", "weyh", "ines", "toth", "fara", "amar", "nyag", "eske", "reth", "dedo", "btoh", "nikt", "neth",
		"kanas", "garis", "uloft", "tarat", "khari", "thnor", "rekka", "ragga", "rfikk", "harfr", "andid", "ethra", "dedol", "totum",
		"ntrath", "keriam"
	) //the list of syllables we'll combine with itself to get a larger list of syllables
	syllables = list(
		"sha", "mir", "sas", "mah", "hra", "zar", "tok", "lyr", "nqa", "nap", "olt", "val",
		"yam", "qha", "fel", "det", "fwe", "mah", "erl", "ath", "yro", "eth", "gal", "mud",
		"gib", "bar", "tea", "fuu", "jin", "kla", "atu", "kal", "lig",
		"yoka", "drak", "loso", "arta", "weyh", "ines", "toth", "fara", "amar", "nyag", "eske", "reth", "dedo", "btoh", "nikt", "neth", "abis",
		"kanas", "garis", "uloft", "tarat", "khari", "thnor", "rekka", "ragga", "rfikk", "harfr", "andid", "ethra", "dedol", "totum",
		"verbot", "pleggh", "ntrath", "barhah", "pasnar", "keriam", "usinar", "savrae", "amutan", "tannin", "remium", "barada",
		"forbici"
	) //the base syllables, which include a few rare ones that won't appear in the mixed syllables
	icon_state = "narsie"
	default_priority = 10

/datum/language/narsie/New()
	for(var/syllable in base_syllables) //we only do this once, since there's only ever a single one of each language datum.
		for(var/target_syllable in base_syllables)
			if(syllable != target_syllable) //don't combine with yourself
				if(length(syllable) + length(target_syllable) > 8) //if the resulting syllable would be very long, don't put anything between it
					syllables += "[syllable][target_syllable]"
				else if(prob(80)) //we'll be minutely different each round.
					syllables += "[syllable]'[target_syllable]"
				else if(prob(25)) //5% chance of - instead of '
					syllables += "[syllable]-[target_syllable]"
				else //15% chance of no ' or - at all
					syllables += "[syllable][target_syllable]"
	..()
