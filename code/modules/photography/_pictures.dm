/datum/picture
	var/picture_name = "picture"
	var/picture_desc = "This is a picture."
	var/list/mobs_seen = list()
	var/list/dead_seen = list()
	var/caption
	var/icon/picture_image
	var/icon/picture_icon
	var/psize_x = 96
	var/psize_y = 96
	var/has_blueprints = FALSE
	var/logpath						//If the picture has been logged this is the path.
	var/id							//this var is NOT protected because the worst you can do with this that you couldn't do otherwise is overwrite photos, and photos aren't going to be used as attack logs/investigations anytime soon.

/datum/picture/New(name, desc, mobs_spotted, dead_spotted, image, icon, size_x, size_y, bp, caption_, autogenerate_icon)
	if(!isnull(name))
		picture_name = name
	if(!isnull(desc))
		picture_desc = desc
	if(!isnull(mobs_spotted))
		mobs_seen = mobs_spotted
	if(!isnull(dead_spotted))
		dead_seen = dead_spotted
	if(!isnull(image))
		picture_image = image
	if(!isnull(icon))
		picture_icon = icon
	if(!isnull(psize_x))
		psize_x = size_x
	if(!isnull(psize_y))
		psize_y = size_y
	if(!isnull(bp))
		has_blueprints = bp
	if(!isnull(caption_))
		caption = caption_
	if(autogenerate_icon && !picture_icon && picture_image)
		regenerate_small_icon()

/datum/picture/proc/get_small_icon(iconstate)
	if(!picture_icon)
		regenerate_small_icon(iconstate)
	return picture_icon

/datum/picture/proc/regenerate_small_icon(iconstate)
	if(!picture_image)
		return
	var/icon/small_img = icon(picture_image)
	var/icon/ic = icon('icons/obj/items_and_weapons.dmi', iconstate ? iconstate :"photo")
	small_img.Scale(8, 8)
	ic.Blend(small_img,ICON_OVERLAY, 13, 13)
	picture_icon = ic

/datum/picture/serialize_list(list/options)
	. = list()
	.["id"] = id
	.["desc"] = picture_desc
	.["name"] = picture_name
	.["caption"] = caption
	.["pixel_size_x"] = psize_x
	.["pixel_size_y"] = psize_y
	.["blueprints"] = has_blueprints
	.["logpath"] = logpath

/datum/picture/deserialize_list(list/input, list/options)
	if(!input["logpath"] || !fexists(input["logpath"]) || !input["id"] || !input["pixel_size_x"] || !input["pixel_size_y"])
		return
	picture_image = icon(file(input["logpath"]))
	logpath = input["logpath"]
	id = input["id"]
	psize_x = input["pixel_size_x"]
	psize_y = input["pixel_size_y"]
	if(input["blueprints"])
		has_blueprints = input["blueprints"]
	if(input["caption"])
		caption = input["caption"]
	if(input["desc"])
		picture_desc = input["desc"]
	if(input["name"])
		picture_name = input["name"]
	return src

/proc/load_photo_from_disk(id, location)
	var/datum/picture/P = load_picture_from_disk(id)
	if(istype(P))
		var/obj/item/photo/old/p = new(location, P)
		return p

/proc/load_picture_from_disk(id)
	var/pathstring = log_path_from_picture_ID(id)
	if(!pathstring)
		return
	var/path = file(pathstring)
	if(!fexists(path))
		return
	var/dir_index = findlasttext(pathstring, "/")
	var/dir = copytext(pathstring, 1, dir_index)
	var/json_path = file("[dir]/metadata.json")
	if(!fexists(json_path))
		return
	var/list/json = json_decode(file2text(json_path))
	if(!json[id])
		return
	var/datum/picture/P = new
	P.deserialize_json(json[id])
	return P

/proc/log_path_from_picture_ID(id)
	if(!istext(id))
		return
	. = "data/picture_logs/"
	var/list/data = splittext(id, "_")
	if(data.len < 3)
		return null
	var/mode = data[1]
	switch(mode)
		if("L")
			if(data.len < 5)
				return null
			var/timestamp = data[2]
			var/year = copytext_char(timestamp, 1, 5)
			var/month = copytext_char(timestamp, 5, 7)
			var/day = copytext_char(timestamp, 7, 9)
			var/round = data[4]
			. += "[year]/[month]/[day]/round-[round]"
		if("O")
			var/list/path = data.Copy(2, data.len)
			. += path.Join("")
		else
			return null
	var/n = data[data.len]
	. += "/[n].png"

//BE VERY CAREFUL WITH THIS PROC, TO AVOID DUPLICATION.
/datum/picture/proc/log_to_file()
	if(!picture_image)
		return
	if(!CONFIG_GET(flag/log_pictures))
		return
	if(logpath)
		return			//we're already logged
	var/number = GLOB.picture_logging_id++
	var/finalpath = "[GLOB.picture_log_directory]/[number].png"
	fcopy(icon(picture_image, dir = SOUTH, frame = 1), finalpath)
	logpath = finalpath
	id = "[GLOB.picture_logging_prefix][number]"
	var/jsonpath = "[GLOB.picture_log_directory]/metadata.json"
	jsonpath = file(jsonpath)
	var/list/json
	if(fexists(jsonpath))
		json = json_decode(file2text(jsonpath))
		fdel(jsonpath)
	else
		json = list()
	json[id] = serialize_json()
	WRITE_FILE(jsonpath, json_encode(json))

/datum/picture/proc/Copy(greyscale = FALSE, cropx = 0, cropy = 0)
	var/datum/picture/P = new
	P.picture_name = picture_name
	P.picture_desc = picture_desc
	if(picture_image)
		P.picture_image = icon(picture_image)	//Copy, not reference.
	if(picture_icon)
		P.picture_icon = icon(picture_icon)
	P.psize_x = psize_x - cropx * 2
	P.psize_y = psize_y - cropy * 2
	P.has_blueprints = has_blueprints
	if(greyscale)
		if(picture_image)
			P.picture_image.MapColors(rgb(77,77,77), rgb(150,150,150), rgb(28,28,28), rgb(0,0,0))
		if(picture_icon)
			P.picture_icon.MapColors(rgb(77,77,77), rgb(150,150,150), rgb(28,28,28), rgb(0,0,0))
	if(cropx || cropy)
		if(picture_image)
			P.picture_image.Crop(cropx, cropy, psize_x - cropx, psize_y - cropy)
		P.regenerate_small_icon()
	return P
