/obj/item/implant/spell
	name = "spell implant"
	desc = "Allows you to cast a spell as if you were a wizard."
	activated = FALSE

	var/autorobeless = TRUE // Whether to automagically make the spell robeless on implant
	var/obj/effect/proc_holder/spell/spell


/obj/item/implant/spell/get_data()
	var/dat = {"<b>Implant Specifications:</b><BR>
				<b>Name:</b> Spell Implant<BR>
				<b>Life:</b> 4 hours after death of host<BR>
				<b>Implant Details:</b> <BR>
				<b>Function:</b> [spell ? "Allows a non-wizard to cast [spell] as if they were a wizard." : "None"]"}
	return dat

/obj/item/implant/spell/implant(mob/living/target, mob/user, silent = FALSE, force = FALSE)
	. = ..()
	if (.)
		if (!spell)
			return FALSE
		if (autorobeless && spell.clothes_req)
			spell.clothes_req = FALSE
		target.AddSpell(spell)
		return TRUE

/obj/item/implant/spell/removed(mob/target, silent = FALSE, special = 0)
	. = ..()
	if (.)
		target.RemoveSpell(spell)
		if(target.stat != DEAD && !silent)
			to_chat(target, "<span class='boldnotice'>The knowledge of how to cast [spell] slips out from your mind.</span>")

/obj/item/implanter/spell
	name = "implanter (spell)"
	imp_type = /obj/item/implant/spell

/obj/item/implantcase/spell
	name = "implant case - 'Wizardry'"
	desc = "A glass case containing an implant that can teach the user the arts of Wizardry."
	imp_type = /obj/item/implant/spell
