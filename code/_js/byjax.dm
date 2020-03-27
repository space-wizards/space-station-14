//this function places received data into element with specified id.
#define js_byjax {"

function replaceContent() {
	var args = Array.prototype.slice.call(arguments);
	var id = args\[0\];
	var content = args\[1\];
	var callback  = null;
	if(args\[2\]){
		callback = args\[2\];
		if(args\[3\]){
			args = args.slice(3);
		}
	}
	var parent = document.getElementById(id);
	if(typeof(parent)!=='undefined' && parent!=null){
		parent.innerHTML = content?content:'';
	}
	if(callback && window\[callback\]){
		window\[callback\].apply(null,args);
	}
}
"}

/*
sends data to control_id:replaceContent

receiver - mob
control_id - window id (for windows opened with browse(), it'll be "windowname.browser")
target_element - HTML element id
new_content - HTML content
callback - js function that will be called after the data is sent
callback_args - arguments for callback function

Be sure to include required js functions in your page, or it'll raise an exception.
*/
/proc/send_byjax(receiver, control_id, target_element, new_content=null, callback=null, list/callback_args=null)
	if(receiver && target_element && control_id) // && winexists(receiver, control_id))
		var/list/argums = list(target_element, new_content)
		if(callback)
			argums += callback
			if(callback_args)
				argums += callback_args
		argums = list2params(argums)

		receiver << output(argums,"[control_id]:replaceContent")
	return

