GLOBAL_LIST_EMPTY(custom_outfits) //Admin created outfits

/client/proc/outfit_manager()
	set category = "Debug"
	set name = "Outfit Manager"

	if(!check_rights(R_DEBUG))
		return
	holder.outfit_manager(usr)

/datum/admins/proc/outfit_manager(mob/admin)
	var/list/dat = list("<ul>")
	for(var/datum/outfit/O in GLOB.custom_outfits)
		var/vv = FALSE
		var/datum/outfit/varedit/VO = O
		if(istype(VO))
			vv = length(VO.vv_values)
		dat += "<li>[O.name][vv ? "(VV)" : ""]</li> <a href='?_src_=holder;[HrefToken()];save_outfit=1;chosen_outfit=[REF(O)]'>Save</a> <a href='?_src_=holder;[HrefToken()];delete_outfit=1;chosen_outfit=[REF(O)]'>Delete</a>"
	dat += "</ul>"
	dat += "<a href='?_src_=holder;[HrefToken()];create_outfit_menu=1'>Create</a><br>"
	dat += "<a href='?_src_=holder;[HrefToken()];load_outfit=1'>Load from file</a>"
	admin << browse(dat.Join(),"window=outfitmanager")

/datum/admins/proc/save_outfit(mob/admin,datum/outfit/O)
	O.save_to_file(admin)
	outfit_manager(admin)

/datum/admins/proc/delete_outfit(mob/admin,datum/outfit/O)
	GLOB.custom_outfits -= O
	qdel(O)
	to_chat(admin,"<span class='notice'>Outfit deleted.</span>")
	outfit_manager(admin)

/datum/admins/proc/load_outfit(mob/admin)
	var/outfit_file = input("Pick outfit json file:", "File") as null|file
	if(!outfit_file)
		return
	var/filedata = file2text(outfit_file)
	var/json = json_decode(filedata)
	if(!json)
		to_chat(admin,"<span class='warning'>JSON decode error.</span>")
		return
	var/otype = text2path(json["outfit_type"])
	if(!ispath(otype,/datum/outfit))
		to_chat(admin,"<span class='warning'>Malformed/Outdated file.</span>")
		return
	var/datum/outfit/O = new otype
	if(!O.load_from(json))
		to_chat(admin,"<span class='warning'>Malformed/Outdated file.</span>")
		return
	GLOB.custom_outfits += O
	outfit_manager(admin)

/datum/admins/proc/create_outfit(mob/admin)
	var/list/uniforms = typesof(/obj/item/clothing/under)
	var/list/suits = typesof(/obj/item/clothing/suit)
	var/list/gloves = typesof(/obj/item/clothing/gloves)
	var/list/shoes = typesof(/obj/item/clothing/shoes)
	var/list/headwear = typesof(/obj/item/clothing/head)
	var/list/glasses = typesof(/obj/item/clothing/glasses)
	var/list/masks = typesof(/obj/item/clothing/mask)
	var/list/ids = typesof(/obj/item/card/id)

	var/uniform_select = "<select name=\"outfit_uniform\"><option value=\"\">None</option>"
	for(var/path in uniforms)
		uniform_select += "<option value=\"[path]\">[path]</option>"
	uniform_select += "</select>"

	var/suit_select = "<select name=\"outfit_suit\"><option value=\"\">None</option>"
	for(var/path in suits)
		suit_select += "<option value=\"[path]\">[path]</option>"
	suit_select += "</select>"

	var/gloves_select = "<select name=\"outfit_gloves\"><option value=\"\">None</option>"
	for(var/path in gloves)
		gloves_select += "<option value=\"[path]\">[path]</option>"
	gloves_select += "</select>"

	var/shoes_select = "<select name=\"outfit_shoes\"><option value=\"\">None</option>"
	for(var/path in shoes)
		shoes_select += "<option value=\"[path]\">[path]</option>"
	shoes_select += "</select>"

	var/head_select = "<select name=\"outfit_head\"><option value=\"\">None</option>"
	for(var/path in headwear)
		head_select += "<option value=\"[path]\">[path]</option>"
	head_select += "</select>"

	var/glasses_select = "<select name=\"outfit_glasses\"><option value=\"\">None</option>"
	for(var/path in glasses)
		glasses_select += "<option value=\"[path]\">[path]</option>"
	glasses_select += "</select>"

	var/mask_select = "<select name=\"outfit_mask\"><option value=\"\">None</option>"
	for(var/path in masks)
		mask_select += "<option value=\"[path]\">[path]</option>"
	mask_select += "</select>"

	var/id_select = "<select name=\"outfit_id\"><option value=\"\">None</option>"
	for(var/path in ids)
		id_select += "<option value=\"[path]\">[path]</option>"
	id_select += "</select>"

	var/dat = {"
	<html><head><title>Create Outfit</title></head><body>
	<form name="outfit" action="byond://?src=[REF(src)];[HrefToken()]" method="get">
	<input type="hidden" name="src" value="[REF(src)]">
	[HrefTokenFormField()]
	<input type="hidden" name="create_outfit_finalize" value="1">
	<table>
		<tr>
			<th>Name:</th>
			<td>
				<input type="text" name="outfit_name" value="Custom Outfit">
			</td>
		</tr>
		<tr>
			<th>Uniform:</th>
			<td>
			   [uniform_select]
			</td>
		</tr>
		<tr>
			<th>Suit:</th>
			<td>
				[suit_select]
			</td>
		</tr>
		<tr>
			<th>Back:</th>
			<td>
				<input type="text" name="outfit_back" value="">
			</td>
		</tr>
		<tr>
			<th>Belt:</th>
			<td>
				<input type="text" name="outfit_belt" value="">
			</td>
		</tr>
		<tr>
			<th>Gloves:</th>
			<td>
				[gloves_select]
			</td>
		</tr>
		<tr>
			<th>Shoes:</th>
			<td>
				[shoes_select]
			</td>
		</tr>
		<tr>
			<th>Head:</th>
			<td>
				[head_select]
			</td>
		</tr>
		<tr>
			<th>Mask:</th>
			<td>
				[mask_select]
			</td>
		</tr>
		<tr>
			<th>Ears:</th>
			<td>
				<input type="text" name="outfit_ears" value="">
			</td>
		</tr>
		<tr>
			<th>Glasses:</th>
			<td>
				[glasses_select]
			</td>
		</tr>
		<tr>
			<th>ID:</th>
			<td>
				[id_select]
			</td>
		</tr>
		<tr>
			<th>Left Pocket:</th>
			<td>
				<input type="text" name="outfit_l_pocket" value="">
			</td>
		</tr>
		<tr>
			<th>Right Pocket:</th>
			<td>
				<input type="text" name="outfit_r_pocket" value="">
			</td>
		</tr>
		<tr>
			<th>Suit Store:</th>
			<td>
				<input type="text" name="outfit_s_store" value="">
			</td>
		</tr>
		<tr>
			<th>Right Hand:</th>
			<td>
				<input type="text" name="outfit_r_hand" value="">
			</td>
		</tr>
		<tr>
			<th>Left Hand:</th>
			<td>
				<input type="text" name="outfit_l_hand" value="">
			</td>
		</tr>
	</table>
	<br>
	<input type="submit" value="Save">
	</form></body></html>
	"}
	admin << browse(dat, "window=dressup;size=550x600")


/datum/admins/proc/create_outfit_finalize(mob/admin, list/href_list)
	var/datum/outfit/O = new

	O.name = href_list["outfit_name"]
	O.uniform = text2path(href_list["outfit_uniform"])
	O.shoes = text2path(href_list["outfit_shoes"])
	O.gloves = text2path(href_list["outfit_gloves"])
	O.suit = text2path(href_list["outfit_suit"])
	O.head = text2path(href_list["outfit_head"])
	O.back = text2path(href_list["outfit_back"])
	O.mask = text2path(href_list["outfit_mask"])
	O.glasses = text2path(href_list["outfit_glasses"])
	O.r_hand = text2path(href_list["outfit_r_hand"])
	O.l_hand = text2path(href_list["outfit_l_hand"])
	O.suit_store = text2path(href_list["outfit_s_store"])
	O.l_pocket = text2path(href_list["outfit_l_pocket"])
	O.r_pocket = text2path(href_list["outfit_r_pocket"])
	O.id = text2path(href_list["outfit_id"])
	O.belt = text2path(href_list["outfit_belt"])
	O.ears = text2path(href_list["outfit_ears"])

	GLOB.custom_outfits.Add(O)
	message_admins("[key_name(usr)] created \"[O.name]\" outfit!")
