
//Used \n[\s]*origin_tech[\s]*=[\s]*"[\S]+" to delete all origin techs.
//Or \n[\s]*origin_tech[\s]*=[\s]list\([A-Z_\s=0-9,]*\)
//Used \n[\s]*req_tech[\s]*=[\s]*list\(["a-z\s=0-9,]*\) to delete all req_techs.

//Techweb datums are meant to store unlocked research, being able to be stored on research consoles, servers, and disks. They are NOT global.
/datum/techweb
	var/list/researched_nodes = list()		//Already unlocked and all designs are now available. Assoc list, id = TRUE
	var/list/visible_nodes = list()			//Visible nodes, doesn't mean it can be researched. Assoc list, id = TRUE
	var/list/available_nodes = list()		//Nodes that can immediately be researched, all reqs met. assoc list, id = TRUE
	var/list/researched_designs = list()	//Designs that are available for use. Assoc list, id = TRUE
	var/list/custom_designs = list()		//Custom inserted designs like from disks that should survive recalculation.
	var/list/boosted_nodes = list()			//Already boosted nodes that can't be boosted again. node id = path of boost object.
	var/list/hidden_nodes = list()			//Hidden nodes. id = TRUE. Used for unhiding nodes when requirements are met by removing the entry of the node.
	var/list/deconstructed_items = list()						//items already deconstructed for a generic point boost. path = list(point_type = points)
	var/list/research_points = list()										//Available research points. type = number
	var/list/obj/machinery/computer/rdconsole/consoles_accessing = list()
	var/id = "generic"
	var/list/research_logs = list()								//IC logs.
	var/largest_bomb_value = 0
	var/organization = "Third-Party"							//Organization name, used for display.
	var/list/last_bitcoins = list()								//Current per-second production, used for display only.
	var/list/discovered_mutations = list()                           //Mutations discovered by genetics, this way they are shared and cant be destroyed by destroying a single console
	var/list/tiers = list()										//Assoc list, id = number, 1 is available, 2 is all reqs are 1, so on

/datum/techweb/New()
	SSresearch.techwebs += src
	for(var/i in SSresearch.techweb_nodes_starting)
		var/datum/techweb_node/DN = SSresearch.techweb_node_by_id(i)
		research_node(DN, TRUE, FALSE, FALSE)
	hidden_nodes = SSresearch.techweb_nodes_hidden.Copy()
	return ..()

/datum/techweb/admin
	id = "ADMIN"
	organization = "CentCom"

/datum/techweb/admin/New()	//All unlocked.
	. = ..()
	for(var/i in SSresearch.techweb_nodes)
		var/datum/techweb_node/TN = SSresearch.techweb_nodes[i]
		research_node(TN, TRUE, TRUE, FALSE)
	for(var/i in SSresearch.point_types)
		research_points[i] = INFINITY
	hidden_nodes = list()

/datum/techweb/science	//Global science techweb for RND consoles.
	id = "SCIENCE"
	organization = "Nanotrasen"

/datum/techweb/bepis	//Should contain only 1 BEPIS tech selected at random.
	id = "EXPERIMENTAL"
	organization = "Nanotrasen R&D"

/datum/techweb/bepis/New()
	. = ..()
	var/bepis_id = pick(SSresearch.techweb_nodes_experimental)	//To add a new tech to the BEPIS, add the ID to this pick list.
	var/datum/techweb_node/BN = (SSresearch.techweb_node_by_id(bepis_id))
	hidden_nodes -= BN.id				//Has to be removed from hidden nodes
	research_node(BN, TRUE, FALSE, FALSE)
	update_node_status(BN)
	SSresearch.techweb_nodes_experimental -= bepis_id

/datum/techweb/Destroy()
	researched_nodes = null
	researched_designs = null
	available_nodes = null
	visible_nodes = null
	custom_designs = null
	SSresearch.techwebs -= src
	return ..()

/datum/techweb/proc/recalculate_nodes(recalculate_designs = FALSE, wipe_custom_designs = FALSE)
	var/list/datum/techweb_node/processing = list()
	for(var/id in researched_nodes)
		processing[id] = TRUE
	for(var/id in visible_nodes)
		processing[id] = TRUE
	for(var/id in available_nodes)
		processing[id] = TRUE
	if(recalculate_designs)
		researched_designs = custom_designs.Copy()
		if(wipe_custom_designs)
			custom_designs = list()
	for(var/id in processing)
		update_node_status(SSresearch.techweb_node_by_id(id), FALSE)
		CHECK_TICK
	for(var/v in consoles_accessing)
		var/obj/machinery/computer/rdconsole/V = v
		V.rescan_views()
		V.updateUsrDialog()

/datum/techweb/proc/add_point_list(list/pointlist)
	for(var/i in pointlist)
		if(SSresearch.point_types[i] && pointlist[i] > 0)
			research_points[i] += pointlist[i]

/datum/techweb/proc/add_points_all(amount)
	var/list/l = SSresearch.point_types.Copy()
	for(var/i in l)
		l[i] = amount
	add_point_list(l)

/datum/techweb/proc/remove_point_list(list/pointlist)
	for(var/i in pointlist)
		if(SSresearch.point_types[i] && pointlist[i] > 0)
			research_points[i] = max(0, research_points[i] - pointlist[i])

/datum/techweb/proc/remove_points_all(amount)
	var/list/l = SSresearch.point_types.Copy()
	for(var/i in l)
		l[i] = amount
	remove_point_list(l)

/datum/techweb/proc/modify_point_list(list/pointlist)
	for(var/i in pointlist)
		if(SSresearch.point_types[i] && pointlist[i] != 0)
			research_points[i] = max(0, research_points[i] + pointlist[i])

/datum/techweb/proc/modify_points_all(amount)
	var/list/l = SSresearch.point_types.Copy()
	for(var/i in l)
		l[i] = amount
	modify_point_list(l)

/datum/techweb/proc/copy_research_to(datum/techweb/receiver, unlock_hidden = TRUE)				//Adds any missing research to theirs.
	if(unlock_hidden)
		for(var/i in receiver.hidden_nodes)
			CHECK_TICK
			if(!hidden_nodes[i])
				receiver.hidden_nodes -= i		//We can see it so let them see it too.
	for(var/i in researched_nodes)
		CHECK_TICK
		receiver.research_node_id(i, TRUE, FALSE, FALSE)
	for(var/i in researched_designs)
		CHECK_TICK
		receiver.add_design_by_id(i)
	receiver.recalculate_nodes()

/datum/techweb/proc/copy()
	var/datum/techweb/returned = new()
	returned.researched_nodes = researched_nodes.Copy()
	returned.visible_nodes = visible_nodes.Copy()
	returned.available_nodes = available_nodes.Copy()
	returned.researched_designs = researched_designs.Copy()
	returned.hidden_nodes = hidden_nodes.Copy()
	return returned

/datum/techweb/proc/get_visible_nodes()			//The way this is set up is shit but whatever.
	return visible_nodes - hidden_nodes

/datum/techweb/proc/get_available_nodes()
	return available_nodes - hidden_nodes

/datum/techweb/proc/get_researched_nodes()
	return researched_nodes - hidden_nodes

/datum/techweb/proc/add_point_type(type, amount)
	if(!SSresearch.point_types[type] || (amount <= 0))
		return FALSE
	research_points[type] += amount
	return TRUE

/datum/techweb/proc/modify_point_type(type, amount)
	if(!SSresearch.point_types[type])
		return FALSE
	research_points[type] = max(0, research_points[type] + amount)
	return TRUE

/datum/techweb/proc/remove_point_type(type, amount)
	if(!SSresearch.point_types[type] || (amount <= 0))
		return FALSE
	research_points[type] = max(0, research_points[type] - amount)
	return TRUE

/datum/techweb/proc/add_design_by_id(id, custom = FALSE)
	return add_design(SSresearch.techweb_design_by_id(id), custom)

/datum/techweb/proc/add_design(datum/design/design, custom = FALSE)
	if(!istype(design))
		return FALSE
	researched_designs[design.id] = TRUE
	if(custom)
		custom_designs[design.id] = TRUE
	return TRUE

/datum/techweb/proc/remove_design_by_id(id, custom = FALSE)
	return remove_design(SSresearch.techweb_design_by_id(id), custom)

/datum/techweb/proc/remove_design(datum/design/design, custom = FALSE)
	if(!istype(design))
		return FALSE
	if(custom_designs[design.id] && !custom)
		return FALSE
	custom_designs -= design.id
	researched_designs -= design.id
	return TRUE

/datum/techweb/proc/get_point_total(list/pointlist)
	for(var/i in pointlist)
		. += pointlist[i]

/datum/techweb/proc/can_afford(list/pointlist)
	for(var/i in pointlist)
		if(research_points[i] < pointlist[i])
			return FALSE
	return TRUE

/datum/techweb/proc/printout_points()
	return techweb_point_display_generic(research_points)

/datum/techweb/proc/research_node_id(id, force, auto_update_points, get_that_dosh_id)
	return research_node(SSresearch.techweb_node_by_id(id), force, auto_update_points, get_that_dosh_id)

/datum/techweb/proc/research_node(datum/techweb_node/node, force = FALSE, auto_adjust_cost = TRUE, get_that_dosh = TRUE)
	if(!istype(node))
		return FALSE
	update_node_status(node)
	if(!force)
		if(!available_nodes[node.id] || (auto_adjust_cost && (!can_afford(node.get_price(src)))))
			return FALSE
	if(auto_adjust_cost)
		remove_point_list(node.get_price(src))
	researched_nodes[node.id] = TRUE				//Add to our researched list
	for(var/id in node.unlock_ids)
		visible_nodes[id] = TRUE
		update_node_status(SSresearch.techweb_node_by_id(id))
	for(var/id in node.design_ids)
		add_design_by_id(id)
	update_node_status(node)
	if(get_that_dosh)
		var/datum/bank_account/D = SSeconomy.get_dep_account(ACCOUNT_SCI)
		if(D)
			D.adjust_money(SSeconomy.techweb_bounty)
	return TRUE

/datum/techweb/science/research_node(datum/techweb_node/node, force = FALSE, auto_adjust_cost = TRUE, get_that_dosh = TRUE) //When something is researched, triggers the proc for this techweb only
	. = ..()
	if(.)
		node.on_research()

/datum/techweb/proc/unresearch_node_id(id)
	return unresearch_node(SSresearch.techweb_node_by_id(id))

/datum/techweb/proc/unresearch_node(datum/techweb_node/node)
	if(!istype(node))
		return FALSE
	researched_nodes -= node.id
	recalculate_nodes(TRUE)				//Fully rebuild the tree.

/datum/techweb/proc/boost_with_path(datum/techweb_node/N, itempath)
	if(!istype(N) || !ispath(itempath))
		return FALSE
	LAZYINITLIST(boosted_nodes[N.id])
	for(var/i in N.boost_item_paths[itempath])
		boosted_nodes[N.id][i] = max(boosted_nodes[N.id][i], N.boost_item_paths[itempath][i])
	if(N.autounlock_by_boost)
		hidden_nodes -= N.id
	update_node_status(N)
	return TRUE

/datum/techweb/proc/update_tiers(datum/techweb_node/base)
	var/list/current = list(base)
	while (current.len)
		var/list/next = list()
		for (var/node_ in current)
			var/datum/techweb_node/node = node_
			var/tier = 0
			if (!researched_nodes[node.id])  // researched is tier 0
				for (var/id in node.prereq_ids)
					var/prereq_tier = tiers[id]
					tier = max(tier, prereq_tier + 1)

			if (tier != tiers[node.id])
				tiers[node.id] = tier
				for (var/id in node.unlock_ids)
					next += SSresearch.techweb_node_by_id(id)
		current = next

/datum/techweb/proc/update_node_status(datum/techweb_node/node, autoupdate_consoles = TRUE)
	var/researched = FALSE
	var/available = FALSE
	var/visible = FALSE
	if(researched_nodes[node.id])
		researched = TRUE
	var/needed = node.prereq_ids.len
	for(var/id in node.prereq_ids)
		if(researched_nodes[id])
			visible = TRUE
			needed--
	if(!needed)
		available = TRUE
	researched_nodes -= node.id
	available_nodes -= node.id
	visible_nodes -= node.id
	if(hidden_nodes[node.id])	//Hidden.
		return
	if(researched)
		researched_nodes[node.id] = TRUE
		for(var/id in node.design_ids)
			add_design(SSresearch.techweb_design_by_id(id))
	else
		if(available)
			available_nodes[node.id] = TRUE
		else
			if(visible)
				visible_nodes[node.id] = TRUE
	update_tiers(node)
	if(autoupdate_consoles)
		for(var/v in consoles_accessing)
			var/obj/machinery/computer/rdconsole/V = v
			V.rescan_views()
			V.updateUsrDialog()

//Laggy procs to do specific checks, just in case. Don't use them if you can just use the vars that already store all this!
/datum/techweb/proc/designHasReqs(datum/design/D)
	for(var/i in researched_nodes)
		var/datum/techweb_node/N = SSresearch.techweb_node_by_id(i)
		if(N.design_ids[D.id])
			return TRUE
	return FALSE

/datum/techweb/proc/isDesignResearched(datum/design/D)
	return isDesignResearchedID(D.id)

/datum/techweb/proc/isDesignResearchedID(id)
	return researched_designs[id]? SSresearch.techweb_design_by_id(id) : FALSE

/datum/techweb/proc/isNodeResearched(datum/techweb_node/N)
	return isNodeResearchedID(N.id)

/datum/techweb/proc/isNodeResearchedID(id)
	return researched_nodes[id]? SSresearch.techweb_node_by_id(id) : FALSE

/datum/techweb/proc/isNodeVisible(datum/techweb_node/N)
	return isNodeResearchedID(N.id)

/datum/techweb/proc/isNodeVisibleID(id)
	return visible_nodes[id]? SSresearch.techweb_node_by_id(id) : FALSE

/datum/techweb/proc/isNodeAvailable(datum/techweb_node/N)
	return isNodeAvailableID(N.id)

/datum/techweb/proc/isNodeAvailableID(id)
	return available_nodes[id]? SSresearch.techweb_node_by_id(id) : FALSE

/datum/techweb/specialized
	var/allowed_buildtypes = ALL

/datum/techweb/specialized/add_design(datum/design/D)
	if(!(D.build_type & allowed_buildtypes))
		return FALSE
	return ..()

/datum/techweb/specialized/autounlocking
	var/design_autounlock_buildtypes = NONE
	var/design_autounlock_categories = list("initial")		//if a design has a buildtype that matches the abovea and either has a category in this or this is null, unlock it.
	var/node_autounlock_ids = list()				//autounlock nodes of this type.

/datum/techweb/specialized/autounlocking/New()
	..()
	autounlock()

/datum/techweb/specialized/autounlocking/proc/autounlock()
	for(var/id in node_autounlock_ids)
		research_node_id(id, TRUE, FALSE, FALSE)
	for(var/id in SSresearch.techweb_designs)
		var/datum/design/D = SSresearch.techweb_design_by_id(id)
		if(D.build_type & design_autounlock_buildtypes)
			for(var/i in D.category)
				if(i in design_autounlock_categories)
					add_design_by_id(D.id)
					break

/datum/techweb/specialized/autounlocking/autolathe
	design_autounlock_buildtypes = AUTOLATHE
	allowed_buildtypes = AUTOLATHE

/datum/techweb/specialized/autounlocking/limbgrower
	design_autounlock_buildtypes = LIMBGROWER
	allowed_buildtypes = LIMBGROWER

/datum/techweb/specialized/autounlocking/biogenerator
	design_autounlock_buildtypes = BIOGENERATOR
	allowed_buildtypes = BIOGENERATOR

/datum/techweb/specialized/autounlocking/smelter
	design_autounlock_buildtypes = SMELTER
	allowed_buildtypes = SMELTER

/datum/techweb/specialized/autounlocking/exofab
	allowed_buildtypes = MECHFAB
