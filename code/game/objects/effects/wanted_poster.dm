/*
	This type poster is of 3 parts: picture, foreground, background

	background - Poster paper printed on
	foreground - Text on top of poster
	picture - The image on the poster: Typically a missing person or wanted individual.

*/

/obj/item/poster/wanted
	icon_state = "rolled_poster"
	var/postHeaderText = "WANTED" // MAX 7 Characters
	var/postHeaderColor = "#FF0000"
	var/background = "wanted_background"
	var/postName = "wanted poster"
	var/postDesc = "A wanted poster for"

/obj/item/poster/wanted/missing
	postName = "missing poster"
	postDesc = "A missing poster for"
	postHeaderText = "MISSING" // MAX 7 Characters
	postHeaderColor = "#0000FF"

/obj/item/poster/wanted/Initialize(mapload, icon/person_icon, wanted_name, description, headerText)
	. = ..(mapload, new /obj/structure/sign/poster/wanted(src, person_icon, wanted_name, description, headerText, postHeaderColor, background, postName, postDesc))
	name = "[postName] ([wanted_name])"
	desc = "[postDesc] [wanted_name]."
	postHeaderText = headerText

/obj/structure/sign/poster/wanted
	var/wanted_name
	var/postName
	var/postDesc
	var/posterHeaderText
	var/posterHeaderColor

/obj/structure/sign/poster/wanted/Initialize(mapload, icon/person_icon, person_name, description, postHeaderText, postHeaderColor, background, pname, pdesc)
	. = ..()
	if(!person_icon)
		return INITIALIZE_HINT_QDEL

	postName = pname
	postDesc = pdesc
	posterHeaderText = postHeaderText
	posterHeaderColor = postHeaderColor
	wanted_name = person_name

	name = "[postName] ([wanted_name])"	
	desc = description

	person_icon = icon(person_icon, dir = SOUTH)//copy the image so we don't mess with the one in the record.
	var/icon/the_icon = icon("icon" = 'icons/obj/poster_wanted.dmi', "icon_state" = background)
	person_icon.Shift(SOUTH, 7)
	person_icon.Crop(7,4,26,30)
	person_icon.Crop(-5,-2,26,29)
	the_icon.Blend(person_icon, ICON_OVERLAY)

	// Print text on top of poster.
	print_across_top(the_icon, postHeaderText, postHeaderColor)
	
	the_icon.Insert(the_icon, "wanted")
	the_icon.Insert(icon('icons/obj/contraband.dmi', "poster_being_set"), "poster_being_set")
	the_icon.Insert(icon('icons/obj/contraband.dmi', "poster_ripped"), "poster_ripped")
	
	icon = the_icon

/*
	This proc will write "WANTED" or MISSING" at the top of the poster.

	You can put other variables in like text and color

	text: Up to 7 characters of text to be printed on the top of the poster.
	color: This set the text color: #ff00ff
*/
/obj/structure/sign/poster/wanted/proc/print_across_top(icon/poster_icon, text, color)
	var/textLen = min(length(text), 7)
	var/startX = 16 - (2*textLen)
	var/i
	for(i=1; i <= textLen, i++)
		var/letter = uppertext(text[i])
		var/icon/letter_icon = icon("icon" = 'icons/Font_Minimal.dmi', "icon_state" = letter)
		letter_icon.Shift(EAST, startX) //16 - (2*n)
		letter_icon.Shift(SOUTH, 2)
		letter_icon.SwapColor(rgb(255,255,255), color)
		poster_icon.Blend(letter_icon, ICON_OVERLAY)
		startX = startX + 4	

/obj/structure/sign/poster/wanted/roll_and_drop(turf/location)
	var/obj/item/poster/wanted/P = ..(location)
	P.name = "[postName] ([wanted_name])"
	P.desc = "[postDesc] [wanted_name]."
	P.postHeaderText = posterHeaderText
	P.postHeaderColor = posterHeaderColor
	return P
	
