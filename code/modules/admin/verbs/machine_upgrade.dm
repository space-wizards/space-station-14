/proc/machine_upgrade(obj/machinery/M in world)
	set name = "Tweak Component Ratings"
	set category = "Debug"
	if (!istype(M))
		return

	var/new_rating = input("Enter new rating:","Num") as num|null
	if(new_rating && M.component_parts)
		for(var/obj/item/stock_parts/P in M.component_parts)
			P.rating = new_rating
		M.RefreshParts()

	SSblackbox.record_feedback("nested tally", "admin_toggle", 1, list("Machine Upgrade", "[new_rating]")) //If you are copy-pasting this, ensure the 2nd parameter is unique to the new proc!
