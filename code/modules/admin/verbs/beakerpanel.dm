/proc/reagentsforbeakers()
	. = list()
	for(var/t in subtypesof(/datum/reagent))
		var/datum/reagent/R = t
		. += list(list("id" = t, "text" = initial(R.name)))

	. = json_encode(.)

/proc/beakersforbeakers()
	. = list()
	for(var/t in subtypesof(/obj/item/reagent_containers))
		var/obj/item/reagent_containers/C = t
		. += list(list("id" = t, "text" = initial(C.name), "volume" = initial(C.volume)))

	. = json_encode(.)

/datum/admins/proc/beaker_panel_act(list/href_list)
	switch (href_list["beakerpanel"])
		if ("spawncontainer")
			var/containerdata = json_decode(href_list["container"])
			var/obj/item/reagent_containers/container = beaker_panel_create_container(containerdata, get_turf(usr))
			log_game("[key_name(usr)] spawned a [container] containing [pretty_string_from_reagent_list(container.reagents.reagent_list)]")
		if ("spawngrenade")
			var/obj/item/grenade/chem_grenade/grenade = new(get_turf(usr))
			var/containersdata = json_decode(href_list["containers"])
			var/reagent_string
			for (var/i in 1 to 2)
				grenade.beakers += beaker_panel_create_container(containersdata[i], grenade)
				reagent_string += " ([grenade.beakers[i].name] [i] : " + pretty_string_from_reagent_list(grenade.beakers[i].reagents.reagent_list) + ");"
			grenade.stage_change(GRENADE_READY)
			var/grenadedata = json_decode(href_list["grenadedata"])
			switch (href_list["grenadetype"])
				if ("normal") // Regular cable coil-timed grenade
					var/det_time = text2num(grenadedata["grenade-timer"])
					if (det_time)
						grenade.det_time = det_time
			log_game("[key_name(usr)] spawned a [grenade] containing: [reagent_string]")

/datum/admins/proc/beaker_panel_prep_assembly(obj/item/assembly/towrap, grenade)
	var/obj/item/assembly/igniter/igniter = new
	igniter.secured = FALSE
	var/obj/item/assembly_holder/assholder = new(grenade)
	towrap.forceMove(assholder)
	igniter.forceMove(assholder)
	assholder.assemble(igniter, towrap, usr)
	assholder.master = grenade
	return assholder

/datum/admins/proc/beaker_panel_create_container(list/containerdata, location)
	var/containertype = text2path(containerdata["container"])
	var/obj/item/reagent_containers/container =  new containertype(location)
	var/datum/reagents/reagents = container.reagents
	for(var/datum/reagent/R in reagents.reagent_list) // clear the container of reagents
		reagents.remove_reagent(R.type,R.volume)
	for (var/list/item in containerdata["reagents"])
		var/datum/reagent/reagenttype = text2path(item["reagent"])
		var/amount = text2num(item["volume"])
		if ((reagents.total_volume + amount) > reagents.maximum_volume)
			reagents.maximum_volume = reagents.total_volume + amount
		reagents.add_reagent(reagenttype, amount)
	return container

/datum/admins/proc/beaker_panel()
	set category = "Debug"
	set name = "Spawn reagent container"
	if(!check_rights())
		return

	var/dat = {"
		<!DOCTYPE HTML PUBLIC "-//W3C//DTD HTML 4.01 Transitional//EN" "http://www.w3.org/TR/html4/loose.dtd">
		<html>
			<meta http-equiv="Content-Type" content="text/html; charset=ISO-8859-1">
			<meta http-equiv="X-UA-Compatible" content="IE=edge">
			<head>
				<link rel='stylesheet' type='text/css' href='common.css'>
				<script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/jquery/3.3.1/jquery.js"></script>
				<script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.7/js/select2.full.min.js"></script>
				<link rel="stylesheet" type="text/css" href="https://cdnjs.cloudflare.com/ajax/libs/select2/4.0.7/css/select2.min.css">
				<script type="text/javascript" src="https://kit.fontawesome.com/8d67455b41.js"></script>
				<style>
					.select2-search { color: #40628a; background-color: #272727; }
					.select2-results { color: #40628a; background-color: #272727; }
					.select2-selection { border-radius: 0px !important; }

					ul {
					  list-style-type: none; /* Remove bullets */
					  padding: 0; /* Remove padding */
					  margin: 0; /* Remove margins */
					}

					ul li {
					  margin-top: -1px; /* Prevent double borders */
					  padding: 12px; /* Add some padding */
					  color: #ffffff;
						text-decoration: none;
						background: #40628a;
						border: 1px solid #161616;
						margin: 0 2px 0 0;
						cursor:default;
					}

					.remove-reagent {
					 background-color: #d03000;
					}

					.container-control {
					  width: 48%;
					  float: left;
					  padding-right: 10px;
					}
					.reagent > div, .reagent-div {
						float: right;
						width: 200px;
					}
					input.reagent {
					  width: 50%;
					}
					.grenade-data {
					  display: inline-block;
					}
				</style>
				<script>
				window.onload=function(){

					var reagents = [reagentsforbeakers()];

					var containers = [beakersforbeakers()];

					$('select\[name="containertype"\]').select2({
						data: containers,
						escapeMarkup: noEscape,
						templateResult: formatContainer,
						templateSelection: textSelection,
						width: "300px"
						});
					$('.select-new-reagent').select2({
					data: reagents,
					escapeMarkup: noEscape,
					templateResult: formatReagent,
					templateSelection: textSelection
					});

					$('.remove-reagent').click(function() { $(this).parents('li').remove(); });

					$('#spawn-grenade').click(function() {
						var containers = $('div.container-control').map(function() {
					  	  var type = $(this).children('select\[name=containertype\]').select2("data")\[0\].id;
					      var reagents = $(this).find("li.reagent").map(function() {
					        return { "reagent": $(this).data("type"), "volume": $(this).find('input').val()};
					        }).get();
					     return {"container": type, "reagents": reagents };
					  }).get();
						var grenadeType = $('#grenade-type').val()
						var grenadeData = {};
						$('.grenade-data.'+grenadeType).find(':input').each(function() {
							var ret = {};
							grenadeData\[$(this).attr('name')\] = $(this).val();
						});
					  $.ajax({
					      url: '',
					      data: {
									"_src_": "holder",
									"admin_token": "[RawHrefToken()]",
									"beakerpanel": "spawngrenade",
									"containers": JSON.stringify(containers),
									"grenadetype": grenadeType,
									"grenadedata": JSON.stringify(grenadeData)
								}
					    });
					});

					$('.spawn-container').click(function() {
						var container = $(this).parents('div.container-control')\[0\];
					  var type = $(container).children('select\[name=containertype\]').select2("data")\[0\].id;
					  var reagents = $(container).find("li.reagent").map(function() {
					  	return { "reagent": $(this).data("type"), "volume": $(this).find('input').val()};
					    }).get();
					  $.ajax({
					  	url: '',
					    data: {
								"_src_": "holder",
								"admin_token": "[RawHrefToken()]",
								"beakerpanel": "spawncontainer",
								"container": JSON.stringify({"container": type, "reagents": reagents }),

							}
						});
					});

					$('.add-reagent').click(function() {
						var select = $(this).parents('li').children('select').select2("data")\[0\];
					  var amount = $(this).parent().children('input').val();
					  addReagent($(this).parents('ul'), select.id, select.text, amount)
					})

					$('.export-reagents').click(function() {
						var container = $(this).parents('div.container-control')\[0\];
					  var ret = \[\];
					  var reagents = $(container).find("li.reagent").each(function() {
					  	var reagentname = $(this).contents().filter(function(){ return this.nodeType == 3; })\[0\].nodeValue.toLowerCase().replace(/\\W/g, '');
					    ret.push(reagentname+"="+$(this).find('input').val());
					    });
					  prompt("Copy this value", ret.join(';'));

					});

					$('.import-reagents').click(function() {
						var macro = prompt("Enter a chemistry macro", "");
					  var parts = macro.split(';');
					  var container = $(this).parents('div.container-control')\[0\];
					  var ul = $(container).find("ul");

					  $(parts).each(function() {
					  	var reagentArr = this.split('=');
					    var thisReagent = $(reagents).filter(function() { return this.text.toLowerCase().replace(/\\W/g, '') == reagentArr\[0\] })\[0\];
					    addReagent(ul, thisReagent.id, thisReagent.text, reagentArr\[1\]);
					  });

					});

					$('#grenade-type').change(function() {
						$('.grenade-data').hide();
					  $('.grenade-data.'+$(this).val()).show();
					})

					function addReagent(ul, reagentType, reagentName, amount)
					{
						$('<li class="reagent" data-type="'+reagentType+'">'+reagentName+'<div><input class="reagent" value="'+amount+'" />&nbsp;&nbsp;<button class="remove-reagent"><i class="far fa-trash-alt"></i>&nbsp;Remove</button></div></li>').insertBefore($(ul).children('li').last());
					  $(ul).children('li').last().prev().find('button').click(function() { $(this).parents('li').remove(); });
					}

					function textSelection(selection)
					{
					return selection.text;
					}

					function noEscape(markup)
					{
					return markup;
					}

					function formatReagent(result)
					{
					return '<span>'+result.text+'</span><br/><span><small>'+result.id+'</small></span>';
					}

					function formatContainer(result)
					{
					return '<span>'+result.text+" ("+result.volume+'u)</span><br/><span><small>'+result.id+'</small></span>';
					}


			}
			</script>
			</head>
			<body scroll=auto>
				<div class='uiWrapper'>
					<div class='uiTitleWrapper'><div class='uiTitle'><tt>Beaker panel</tt></div></div>
					<div class='uiContent'>

		<div class="width: 100%">
		  <button id="spawn-grenade">
		<i class="fas fa-bomb"></i>&nbsp;Spawn grenade
		  </button>
			<label for="grenade-type">Grenade type: </label>
		 <select id="grenade-type">
			 <option value="normal">Normal</option>
		 </select>
		 <div class="grenade-data normal">
		 </div>
			<br />
<small>note: beakers recommended, other containers may have issues</small>
		</div>

	"}
	for (var/i in 1 to 2 )
		dat += {"
			<div class="container-control">
			<h4>
			Container [i]:
			</h4>
			<br />
			<label for="beaker[i]type">Container type</label>
			<select name="containertype" id="beaker[i]type"></select>
			<br />
			<br />
			<div>
			<button class="spawn-container">
			<i class="fas fa-cog"></i>&nbsp;Spawn
			  </button>
			  &nbsp;&nbsp;&nbsp;
				<button class="import-reagents">
			<i class="fas fa-file-import"></i>&nbsp;Import
			  </button>
			  &nbsp;&nbsp;&nbsp;
			  <button class="export-reagents">
			<i class="fas fa-file-export"></i>&nbsp;Export
			  </button>

			</div>
				 <ul>
			  <li>

			    <select class="select-new-reagent"></select><div class="reagent-div"><input style="width: 50%" type="text" name="newreagent" value="40" />&nbsp;&nbsp;<button class="add-reagent">
			  <i class="fas fa-plus"></i>&nbsp;Add
			  </button>

			  </div>
			</li>
			</ul>
			</div>
		"}

	dat += {"
					</div>
				</div>
			</body>
		</html>
	"}

	usr << browse(dat, "window=beakerpanel;size=1100x720")
