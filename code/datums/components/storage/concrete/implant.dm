/datum/component/storage/concrete/implant
	max_w_class = WEIGHT_CLASS_NORMAL
	max_combined_w_class = 6
	max_items = 2
	drop_all_on_destroy = TRUE
	drop_all_on_deconstruct = TRUE
	silent = TRUE
	allow_big_nesting = TRUE

/datum/component/storage/concrete/implant/Initialize()
	. = ..()
	set_holdable(null, list(/obj/item/disk/nuclear))

/datum/component/storage/concrete/implant/InheritComponent(datum/component/storage/concrete/implant/I, original)
	if(!istype(I))
		return ..()
	max_combined_w_class += I.max_combined_w_class
	max_items += I.max_items
