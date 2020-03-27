/mob/living/carbon/proc/handle_dreams()
	if(prob(10) && !dreaming)
		dream()

/mob/living/carbon/proc/dream()
	set waitfor = FALSE
	var/list/dream_fragments = list()
	var/list/custom_dream_nouns = list()
	var/fragment = ""

	for(var/obj/item/bedsheet/sheet in loc)
		custom_dream_nouns += sheet.dream_messages

	dream_fragments += "you see"

	//Subject
	if(custom_dream_nouns.len && prob(90))
		fragment += pick(custom_dream_nouns)
	else
		fragment += pick(GLOB.dream_strings)

	if(prob(50))
		fragment = replacetext(fragment, "%ADJECTIVE%", pick(GLOB.adjectives))
	else
		fragment = replacetext(fragment, "%ADJECTIVE% ", "")
	if(findtext(fragment, "%A% "))
		fragment = "\a [replacetext(fragment, "%A% ", "")]"
	dream_fragments += fragment

	//Verb
	fragment = ""
	if(prob(50))
		if(prob(35))
			fragment += "[pick(GLOB.adverbs)] "
		fragment += pick(GLOB.ing_verbs)
	else
		fragment += "will "
		fragment += pick(GLOB.verbs)
	dream_fragments += fragment

	if(prob(25))
		dream_sequence(dream_fragments)
		return

	//Object
	fragment = ""
	fragment += pick(GLOB.dream_strings)
	if(prob(50))
		fragment = replacetext(fragment, "%ADJECTIVE%", pick(GLOB.adjectives))
	else
		fragment = replacetext(fragment, "%ADJECTIVE% ", "")
	if(findtext(fragment, "%A% "))
		fragment = "\a [replacetext(fragment, "%A% ", "")]"
	dream_fragments += fragment

	dreaming = TRUE
	dream_sequence(dream_fragments)

/mob/living/carbon/proc/dream_sequence(list/dream_fragments)
	if(stat != UNCONSCIOUS || InCritical())
		dreaming = FALSE
		return
	var/next_message = dream_fragments[1]
	dream_fragments.Cut(1,2)
	to_chat(src, "<span class='notice'><i>... [next_message] ...</i></span>")
	if(LAZYLEN(dream_fragments))
		addtimer(CALLBACK(src, .proc/dream_sequence, dream_fragments), rand(10,30))
	else
		dreaming = FALSE
