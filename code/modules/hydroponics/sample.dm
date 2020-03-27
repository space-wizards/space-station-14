/obj/item/seeds/sample
	name = "plant sample"
	icon_state = "sample-empty"
	potency = -1
	yield = -1
	var/sample_color = "#FFFFFF"

/obj/item/seeds/sample/Initialize()
	. = ..()
	if(sample_color)
		var/mutable_appearance/filling = mutable_appearance(icon, "sample-filling")
		filling.color = sample_color
		add_overlay(filling)

/obj/item/seeds/sample/get_analyzer_text()
	return " The DNA of this sample is damaged beyond recovery, it can't support life on its own.\n*---------*"

/obj/item/seeds/sample/alienweed
	name = "alien weed sample"
	icon_state = "alienweed"
	sample_color = null
