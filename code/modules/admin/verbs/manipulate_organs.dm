/client/proc/manipulate_organs(mob/living/carbon/C in world)
	set name = "Manipulate Organs"
	set category = "Debug"
	var/operation = input("Select organ operation.", "Organ Manipulation", "cancel") as null|anything in list("add organ", "add implant", "drop organ/implant", "remove organ/implant", "cancel")
	if (!operation)
		return

	var/list/organs = list()
	switch(operation)
		if("add organ")
			for(var/path in subtypesof(/obj/item/organ))
				var/dat = replacetext("[path]", "/obj/item/organ/", ":")
				organs[dat] = path

			var/obj/item/organ/organ = input("Select organ type:", "Organ Manipulation", null) as null|anything in organs
			if(!organ)
				return
			organ = organs[organ]
			organ = new organ
			organ.Insert(C)
			log_admin("[key_name(usr)] has added organ [organ.type] to [key_name(C)]")
			message_admins("[key_name_admin(usr)] has added organ [organ.type] to [ADMIN_LOOKUPFLW(C)]")

		if("add implant")
			for(var/path in subtypesof(/obj/item/implant))
				var/dat = replacetext("[path]", "/obj/item/implant/", ":")
				organs[dat] = path

			var/obj/item/implant/organ = input("Select implant type:", "Organ Manipulation", null) as null|anything in organs
			if(!organ)
				return
			organ = organs[organ]
			organ = new organ
			organ.implant(C)
			log_admin("[key_name(usr)] has added implant [organ.type] to [key_name(C)]")
			message_admins("[key_name_admin(usr)] has added implant [organ.type] to [ADMIN_LOOKUPFLW(C)]")

		if("drop organ/implant", "remove organ/implant")
			for(var/X in C.internal_organs)
				var/obj/item/organ/I = X
				organs["[I.name] ([I.type])"] = I

			for(var/X in C.implants)
				var/obj/item/implant/I = X
				organs["[I.name] ([I.type])"] = I

			var/obj/item/organ = input("Select organ/implant:", "Organ Manipulation", null) as null|anything in organs
			if(!organ)
				return
			organ = organs[organ]
			if(!organ)
				return
			var/obj/item/organ/O
			var/obj/item/implant/I

			log_admin("[key_name(usr)] has removed [organ.type] from [key_name(C)]")
			message_admins("[key_name_admin(usr)] has removed [organ.type] from [ADMIN_LOOKUPFLW(C)]")

			if(isorgan(organ))
				O = organ
				O.Remove(C)
			else
				I = organ
				I.removed(C)

			organ.forceMove(get_turf(C))

			if(operation == "remove organ/implant")
				qdel(organ)
			else if(I) // Put the implant in case.
				var/obj/item/implantcase/case = new(get_turf(C))
				case.imp = I
				I.forceMove(case)
				case.update_icon()
