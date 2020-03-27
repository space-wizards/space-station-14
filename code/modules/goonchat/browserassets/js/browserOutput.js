
/*****************************************
*
* FUNCTION AND VAR DECLARATIONS
*
******************************************/

//DEBUG STUFF
var escaper = encodeURIComponent || escape;
var decoder = decodeURIComponent || unescape;
window.onerror = function(msg, url, line, col, error) {
	if (document.location.href.indexOf("proc=debug") <= 0) {
		var extra = !col ? '' : ' | column: ' + col;
		extra += !error ? '' : ' | error: ' + error;
		extra += !navigator.userAgent ? '' : ' | user agent: ' + navigator.userAgent;
		var debugLine = 'Error: ' + msg + ' | url: ' + url + ' | line: ' + line + extra;
		window.location = '?_src_=chat&proc=debug&param[error]='+escaper(debugLine);
	}
	return true;
};

//Globals
window.status = 'Output';
var $messages, $subOptions, $subAudio, $selectedSub, $contextMenu, $filterMessages, $last_message;
var opts = {
	//General
	'messageCount': 0, //A count...of messages...
	'messageLimit': 2053, //A limit...for the messages...
	'scrollSnapTolerance': 10, //If within x pixels of bottom
	'clickTolerance': 10, //Keep focus if outside x pixels of mousedown position on mouseup
	'imageRetryDelay': 50, //how long between attempts to reload images (in ms)
	'imageRetryLimit': 50, //how many attempts should we make?
	'popups': 0, //Amount of popups opened ever
	'wasd': false, //Is the user in wasd mode?
	'priorChatHeight': 0, //Thing for height-resizing detection
	'restarting': false, //Is the round restarting?
	'darkmode':false, //Are we using darkmode? If not WHY ARE YOU LIVING IN 2009???

	//Options menu
	'selectedSubLoop': null, //Contains the interval loop for closing the selected sub menu
	'suppressSubClose': false, //Whether or not we should be hiding the selected sub menu
	'highlightTerms': [],
	'highlightLimit': 5,
	'highlightColor': '#FFFF00', //The color of the highlighted message
	'pingDisabled': false, //Has the user disabled the ping counter

	//Ping display
	'lastPang': 0, //Timestamp of the last response from the server.
	'pangLimit': 35000,
	'pingTime': 0, //Timestamp of when ping sent
	'pongTime': 0, //Timestamp of when ping received
	'noResponse': false, //Tracks the state of the previous ping request
	'noResponseCount': 0, //How many failed pings?

	//Clicks
	'mouseDownX': null,
	'mouseDownY': null,
	'preventFocus': false, //Prevents switching focus to the game window

	//Client Connection Data
	'clientDataLimit': 5,
	'clientData': [],

	//Admin music volume update
	'volumeUpdateDelay': 5000, //Time from when the volume updates to data being sent to the server
	'volumeUpdating': false, //True if volume update function set to fire
	'updatedVolume': 0, //The volume level that is sent to the server
	'musicStartAt': 0, //The position the music starts playing
	'musicEndAt': 0, //The position the music... stops playing... if null, doesn't apply (so the music runs through)

	'defaultMusicVolume': 25,

	'messageCombining': true,

};
var replaceRegexes = {};

function clamp(val, min, max) {
	return Math.max(min, Math.min(val, max))
}

function outerHTML(el) {
    var wrap = document.createElement('div');
    wrap.appendChild(el.cloneNode(true));
    return wrap.innerHTML;
}

//Polyfill for fucking date now because of course IE8 and below don't support it
if (!Date.now) {
	Date.now = function now() {
		return new Date().getTime();
	};
}
//Polyfill for trim() (IE8 and below)
if (typeof String.prototype.trim !== 'function') {
	String.prototype.trim = function () {
		return this.replace(/^\s+|\s+$/g, '');
	};
}

// Linkify the contents of a node, within its parent.
function linkify(parent, insertBefore, text) {
	var start = 0;
	var match;
	var regex = /(?:(?:https?:\/\/)|(?:www\.))(?:[^ ]*?\.[^ ]*?)+[-A-Za-z0-9+&@#\/%?=~_|$!:,.;()]+/ig;
	while ((match = regex.exec(text)) !== null) {
		// add the unmatched text
		parent.insertBefore(document.createTextNode(text.substring(start, match.index)), insertBefore);

		var href = match[0];
		if (!/^https?:\/\//i.test(match[0])) {
			href = "http://" + match[0];
		}

		// add the link
		var link = document.createElement("a");
		link.href = href;
		link.textContent = match[0];
		parent.insertBefore(link, insertBefore);

		start = regex.lastIndex;
	}
	if (start !== 0) {
		// add the remaining text and remove the original text node
		parent.insertBefore(document.createTextNode(text.substring(start)), insertBefore);
		parent.removeChild(insertBefore);
	}
}

// Recursively linkify the children of a given node.
function linkify_node(node) {
	var children = node.childNodes;
	// work backwards to avoid the risk of looping forever on our own output
	for (var i = children.length - 1; i >= 0; --i) {
		var child = children[i];
		if (child.nodeType == Node.TEXT_NODE) {
			// text is to be linkified
			linkify(node, child, child.textContent);
		} else if (child.nodeName != "A" && child.nodeName != "a") {
			// do not linkify existing links
			linkify_node(child);
		}
	}
}

//Shit fucking piece of crap that doesn't work god fuckin damn it
function linkify_fallback(text) {
	var rex = /((?:<a|<iframe|<img)(?:.*?(?:src="|href=").*?))?(?:(?:https?:\/\/)|(?:www\.))+(?:[^ ]*?\.[^ ]*?)+[-A-Za-z0-9+&@#\/%?=~_|$!:,.;]+/ig;
	return text.replace(rex, function ($0, $1) {
		if(/^https?:\/\/.+/i.test($0)) {
			return $1 ? $0: '<a href="'+$0+'">'+$0+'</a>';
		}
		else {
			return $1 ? $0: '<a href="http://'+$0+'">'+$0+'</a>';
		}
	});
}

function byondDecode(message) {
	// Basically we url_encode twice server side so we can manually read the encoded version and actually do UTF-8.
	// The replace for + is because FOR SOME REASON, BYOND replaces spaces with a + instead of %20, and a plus with %2b.
	// Marvelous.
	message = message.replace(/\+/g, "%20");
	try {
		// This is a workaround for the above not always working when BYOND's shitty url encoding breaks. (byond bug id:2399401)
		if (decodeURIComponent) {
			message = decodeURIComponent(message);
		} else {
			throw new Error("Easiest way to trigger the fallback")
		}
	} catch (err) {
		message = unescape(message);
	}
	return message;
}

function replaceRegex() {
	var selectedRegex = replaceRegexes[$(this).attr('replaceRegex')];
	if (selectedRegex) {
		var replacedText = $(this).html().replace(selectedRegex[0], selectedRegex[1]);
		$(this).html(replacedText);
	}
	$(this).removeAttr('replaceRegex');
}

//Actually turns the highlight term match into appropriate html
function addHighlightMarkup(match) {
	var extra = '';
	if (opts.highlightColor) {
		extra += ' style="background-color: '+opts.highlightColor+'"';
	}
	return '<span class="highlight"'+extra+'>'+match+'</span>';
}

//Highlights words based on user settings
function highlightTerms(el) {
	if (el.children.length > 0) {
		for(var h = 0; h < el.children.length; h++){
			highlightTerms(el.children[h]);
		}
	}

	var hasTextNode = false;
	for (var node = 0; node < el.childNodes.length; node++)
	{
		if (el.childNodes[node].nodeType === 3)
		{
			hasTextNode = true;
			break;
		}
	}

	if (hasTextNode) { //If element actually has text
		var newText = '';
		for (var c = 0; c < el.childNodes.length; c++) { //Each child element
			if (el.childNodes[c].nodeType === 3) { //Is it text only?
				var words = el.childNodes[c].data.split(' ');
				for (var w = 0; w < words.length; w++) { //Each word in the text
					var newWord = null;
					for (var i = 0; i < opts.highlightTerms.length; i++) { //Each highlight term
						if (opts.highlightTerms[i] && words[w].toLowerCase().indexOf(opts.highlightTerms[i].toLowerCase()) > -1) { //If a match is found
							newWord = words[w].replace("<", "&lt;").replace(new RegExp(opts.highlightTerms[i], 'gi'), addHighlightMarkup);
							break;
						}
						if (window.console)
							console.log(newWord)
					}
					newText += newWord || words[w].replace("<", "&lt;");
					newText += w >= words.length ? '' : ' ';
				}
			} else { //Every other type of element
				newText += outerHTML(el.childNodes[c]);
			}
		}
		el.innerHTML = newText;
	}
}

function iconError(E) {
	var that = this;
	setTimeout(function() {
		var attempts = $(that).data('reload_attempts');
		if (typeof attempts === 'undefined' || !attempts) {
			attempts = 1;
		}
		if (attempts > opts.imageRetryLimit)
			return;
		var src = that.src;
		that.src = null;
		that.src = src+'#'+attempts;
		$(that).data('reload_attempts', ++attempts);
	}, opts.imageRetryDelay);
}

//Send a message to the client
function output(message, flag) {
	if (typeof message === 'undefined') {
		return;
	}
	if (typeof flag === 'undefined') {
		flag = '';
	}

	if (flag !== 'internal')
		opts.lastPang = Date.now();

	message = byondDecode(message).trim();

	//The behemoth of filter-code (for Admin message filters)
	//Note: This is proooobably hella inefficient
	var filteredOut = false;
	if (opts.hasOwnProperty('showMessagesFilters') && !opts.showMessagesFilters['All'].show) {
		//Get this filter type (defined by class on message)
		var messageHtml = $.parseHTML(message),
			messageClasses;
		if (opts.hasOwnProperty('filterHideAll') && opts.filterHideAll) {
			var internal = false;
			messageClasses = (!!$(messageHtml).attr('class') ? $(messageHtml).attr('class').split(/\s+/) : false);
			if (messageClasses) {
				for (var i = 0; i < messageClasses.length; i++) { //Every class
					if (messageClasses[i] == 'internal') {
						internal = true;
						break;
					}
				}
			}
			if (!internal) {
				filteredOut = 'All';
			}
		} else {
			//If the element or it's child have any classes
			if (!!$(messageHtml).attr('class') || !!$(messageHtml).children().attr('class')) {
				messageClasses = $(messageHtml).attr('class').split(/\s+/);
				if (!!$(messageHtml).children().attr('class')) {
					messageClasses = messageClasses.concat($(messageHtml).children().attr('class').split(/\s+/));
				}
				var tempCount = 0;
				for (var i = 0; i < messageClasses.length; i++) { //Every class
					var thisClass = messageClasses[i];
					$.each(opts.showMessagesFilters, function(key, val) { //Every filter
						if (key !== 'All' && val.show === false && typeof val.match != 'undefined') {
							for (var i = 0; i < val.match.length; i++) {
								var matchClass = val.match[i];
								if (matchClass == thisClass) {
									filteredOut = key;
									break;
								}
							}
						}
						if (filteredOut) return false;
					});
					if (filteredOut) break;
					tempCount++;
				}
			} else {
				if (!opts.showMessagesFilters['Misc'].show) {
					filteredOut = 'Misc';
				}
			}
		}
	}

	//Stuff we do along with appending a message
	var atBottom = false;
	if (!filteredOut) {
		var bodyHeight = $('body').height();
		var messagesHeight = $messages.outerHeight();
		var scrollPos = $('body,html').scrollTop();

		//Should we snap the output to the bottom?
		if (bodyHeight + scrollPos >= messagesHeight - opts.scrollSnapTolerance) {
			atBottom = true;
			if ($('#newMessages').length) {
				$('#newMessages').remove();
			}
		//If not, put the new messages box in
		} else {
			if ($('#newMessages').length) {
				var messages = $('#newMessages .number').text();
				messages = parseInt(messages);
				messages++;
				$('#newMessages .number').text(messages);
				if (messages == 2) {
					$('#newMessages .messageWord').append('s');
				}
			} else {
				$messages.after('<a href="#" id="newMessages"><span class="number">1</span> new <span class="messageWord">message</span> <i class="icon-double-angle-down"></i></a>');
			}
		}
	}

	opts.messageCount++;

	//Pop the top message off if history limit reached
	if (opts.messageCount >= opts.messageLimit) {
		$messages.children('div.entry:first-child').remove();
		opts.messageCount--; //I guess the count should only ever equal the limit
	}

	// Create the element - if combining is off, we use it, and if it's on, we
	// might discard it bug need to check its text content. Some messages vary
	// only in HTML markup, have the same text content, and should combine.
	var entry = document.createElement('div');
	entry.innerHTML = message;
	var trimmed_message = entry.textContent || entry.innerText || "";

	var handled = false;
	if (opts.messageCombining) {
		var lastmessages = $messages.children('div.entry:last-child').last();
		if (lastmessages.length && $last_message && $last_message == trimmed_message) {
			var badge = lastmessages.children('.r').last();
			if (badge.length) {
				badge = badge.detach();
				badge.text(parseInt(badge.text()) + 1);
			} else {
				badge = $('<span/>', {'class': 'r', 'text': 2});
			}
			lastmessages.html(message);
			lastmessages.find('[replaceRegex]').each(replaceRegex);
			lastmessages.append(badge);
			badge.animate({
				"font-size": "0.9em"
			}, 100, function() {
				badge.animate({
					"font-size": "0.7em"
				}, 100);
			});
			opts.messageCount--;
			handled = true;
		}
	}

	if (!handled) {
		//Actually append the message
		entry.className = 'entry';

		if (filteredOut) {
			entry.className += ' hidden';
			entry.setAttribute('data-filter', filteredOut);
		}

		$(entry).find('[replaceRegex]').each(replaceRegex);

		$last_message = trimmed_message;
		$messages[0].appendChild(entry);
		$(entry).find("img.icon").error(iconError);

		var to_linkify = $(entry).find(".linkify");
		if (typeof Node === 'undefined') {
			// Linkify fallback for old IE
			for(var i = 0; i < to_linkify.length; ++i) {
				to_linkify[i].innerHTML = linkify_fallback(to_linkify[i].innerHTML);
			}
		} else {
			// Linkify for modern IE versions
			for(var i = 0; i < to_linkify.length; ++i) {
				linkify_node(to_linkify[i]);
			}
		}

		//Actually do the snap
		//Stuff we can do after the message shows can go here, in the interests of responsiveness
		if (opts.highlightTerms && opts.highlightTerms.length > 0) {
			highlightTerms(entry);
		}
	}

	if (!filteredOut && atBottom) {
		$('body,html').scrollTop($messages.outerHeight());
	}
}

function internalOutput(message, flag)
{
	output(escaper(message), flag)
}

//Runs a route within byond, client or server side. Consider this "ehjax" for byond.
function runByond(uri) {
	window.location = uri;
}

function setCookie(cname, cvalue, exdays) {
	cvalue = escaper(cvalue);
	var d = new Date();
	d.setTime(d.getTime() + (exdays*24*60*60*1000));
	var expires = 'expires='+d.toUTCString();
	document.cookie = cname + '=' + cvalue + '; ' + expires + "; path=/";
}

function getCookie(cname) {
	var name = cname + '=';
	var ca = document.cookie.split(';');
	for(var i=0; i < ca.length; i++) {
	var c = ca[i];
	while (c.charAt(0)==' ') c = c.substring(1);
		if (c.indexOf(name) === 0) {
			return decoder(c.substring(name.length,c.length));
		}
	}
	return '';
}

function rgbToHex(R,G,B) {return toHex(R)+toHex(G)+toHex(B);}
function toHex(n) {
	n = parseInt(n,10);
	if (isNaN(n)) return "00";
	n = Math.max(0,Math.min(n,255));
	return "0123456789ABCDEF".charAt((n-n%16)/16) + "0123456789ABCDEF".charAt(n%16);
}

function swap() { //Swap to darkmode
	if (opts.darkmode){
		document.getElementById("sheetofstyles").href = "browserOutput_white.css";
		opts.darkmode = false;
		runByond('?_src_=chat&proc=swaptolightmode');
	} else {
		document.getElementById("sheetofstyles").href = "browserOutput.css";
		opts.darkmode = true;
		runByond('?_src_=chat&proc=swaptodarkmode');
	}
	setCookie('darkmode', (opts.darkmode ? 'true' : 'false'), 365);
}

function handleClientData(ckey, ip, compid) {
	//byond sends player info to here
	var currentData = {'ckey': ckey, 'ip': ip, 'compid': compid};
	if (opts.clientData && !$.isEmptyObject(opts.clientData)) {
		runByond('?_src_=chat&proc=analyzeClientData&param[cookie]='+JSON.stringify({'connData': opts.clientData}));

		for (var i = 0; i < opts.clientData.length; i++) {
			var saved = opts.clientData[i];
			if (currentData.ckey == saved.ckey && currentData.ip == saved.ip && currentData.compid == saved.compid) {
				return; //Record already exists
			}
		}
		//Lets make sure we obey our limit (can connect from server with higher limit)
		while (opts.clientData.length >= opts.clientDataLimit) {
			opts.clientData.shift();
		}
	} else {
		runByond('?_src_=chat&proc=analyzeClientData&param[cookie]=none');
	}

	//Update the cookie with current details
	opts.clientData.push(currentData);
	setCookie('connData', JSON.stringify(opts.clientData), 365);
}

//Server calls this on ehjax response
//Or, y'know, whenever really
function ehjaxCallback(data) {
	opts.lastPang = Date.now();
	if (data == 'softPang') {
		return;
	} else if (data == 'pang') {
		opts.pingCounter = 0; //reset
		opts.pingTime = Date.now();
		runByond('?_src_=chat&proc=ping');

	} else if (data == 'pong') {
		if (opts.pingDisabled) {return;}
		opts.pongTime = Date.now();
		var pingDuration = Math.ceil((opts.pongTime - opts.pingTime) / 2);
		$('#pingMs').text(pingDuration+'ms');
		pingDuration = Math.min(pingDuration, 255);
		var red = pingDuration;
		var green = 255 - pingDuration;
		var blue = 0;
		var hex = rgbToHex(red, green, blue);
		$('#pingDot').css('color', '#'+hex);

	} else if (data == 'roundrestart') {
		opts.restarting = true;
		internalOutput('<div class="connectionClosed internal restarting">The connection has been closed because the server is restarting. Please wait while you automatically reconnect.</div>', 'internal');
	} else if (data == 'stopMusic') {
		$('#adminMusic').prop('src', '');
	} else {
		//Oh we're actually being sent data instead of an instruction
		var dataJ;
		try {
			dataJ = $.parseJSON(data);
		} catch (e) {
			//But...incorrect :sadtrombone:
			window.onerror('JSON: '+e+'. '+data, 'browserOutput.html', 327);
			return;
		}
		data = dataJ;

		if (data.clientData) {
			if (opts.restarting) {
				opts.restarting = false;
				$('.connectionClosed.restarting:not(.restored)').addClass('restored').text('The round restarted and you successfully reconnected!');
			}
			if (!data.clientData.ckey && !data.clientData.ip && !data.clientData.compid) {
				//TODO: Call shutdown perhaps
				return;
			} else {
				handleClientData(data.clientData.ckey, data.clientData.ip, data.clientData.compid);
			}
			sendVolumeUpdate();
		} else if (data.adminMusic) {
			if (typeof data.adminMusic === 'string') {
				var adminMusic = byondDecode(data.adminMusic);
				var bindLoadedData = false;
				adminMusic = adminMusic.match(/https?:\/\/\S+/) || '';
				if (data.musicRate) {
					var newRate = Number(data.musicRate);
					if(newRate) {
						$('#adminMusic').prop('defaultPlaybackRate', newRate);
					}
				} else {
					$('#adminMusic').prop('defaultPlaybackRate', 1.0);
				}
				if (data.musicSeek) {
					opts.musicStartAt = Number(data.musicSeek) || 0;
					bindLoadedData = true;
				} else {
					opts.musicStartAt = 0;
				}
				if (data.musicHalt) {
					opts.musicEndAt = Number(data.musicHalt) || null;
					bindLoadedData = true;
				}
				if (bindLoadedData) {
					$('#adminMusic').one('loadeddata', adminMusicLoadedData);
				}
				$('#adminMusic').prop('src', adminMusic);
				$('#adminMusic').trigger("play");
			}
		} else if (data.syncRegex) {
			for (var i in data.syncRegex) {

				var regexData = data.syncRegex[i];
				var regexName = regexData[0];
				var regexFlags = regexData[1];
				var regexReplaced = regexData[2];

				replaceRegexes[i] = [new RegExp(regexName, regexFlags), regexReplaced];
			}
		}
	}
}

function createPopup(contents, width) {
	opts.popups++;
	$('body').append('<div class="popup" id="popup'+opts.popups+'" style="width: '+width+'px;">'+contents+' <a href="#" class="close"><i class="icon-remove"></i></a></div>');

	//Attach close popup event
	var $popup = $('#popup'+opts.popups);
	var height = $popup.outerHeight();
	$popup.css({'height': height+'px', 'margin': '-'+(height/2)+'px 0 0 -'+(width/2)+'px'});

	$popup.on('click', '.close', function(e) {
		e.preventDefault();
		$popup.remove();
	});
}

function toggleWasd(state) {
	opts.wasd = (state == 'on' ? true : false);
}

function sendVolumeUpdate() {
	opts.volumeUpdating = false;
	if(opts.updatedVolume) {
		runByond('?_src_=chat&proc=setMusicVolume&param[volume]='+opts.updatedVolume);
	}
}

function adminMusicEndCheck(event) {
	if (opts.musicEndAt) {
		if ($('#adminMusic').prop('currentTime') >= opts.musicEndAt) {
			$('#adminMusic').off(event);
			$('#adminMusic').trigger('pause');
			$('#adminMusic').prop('src', '');
		}
	} else {
		$('#adminMusic').off(event);
	}
}

function adminMusicLoadedData(event) {
	if (opts.musicStartAt && ($('#adminMusic').prop('duration') === Infinity || (opts.musicStartAt <= $('#adminMusic').prop('duration'))) ) {
		$('#adminMusic').prop('currentTime', opts.musicStartAt);
	}
	if (opts.musicEndAt) {
		$('#adminMusic').on('timeupdate', adminMusicEndCheck);
	}
}

function subSlideUp() {
	$(this).removeClass('scroll');
	$(this).css('height', '');
}

function startSubLoop() {
	if (opts.selectedSubLoop) {
		clearInterval(opts.selectedSubLoop);
	}
	return setInterval(function() {
		if (!opts.suppressSubClose && $selectedSub.is(':visible')) {
			$selectedSub.slideUp('fast', subSlideUp);
			clearInterval(opts.selectedSubLoop);
		}
	}, 5000); //every 5 seconds
}

function handleToggleClick($sub, $toggle) {
	if ($selectedSub !== $sub && $selectedSub.is(':visible')) {
		$selectedSub.slideUp('fast', subSlideUp);
	}
	$selectedSub = $sub
	if ($selectedSub.is(':visible')) {
		$selectedSub.slideUp('fast', subSlideUp);
		clearInterval(opts.selectedSubLoop);
	} else {
		$selectedSub.slideDown('fast', function() {
			var windowHeight = $(window).height();
			var toggleHeight = $toggle.outerHeight();
			var priorSubHeight = $selectedSub.outerHeight();
			var newSubHeight = windowHeight - toggleHeight;
			$(this).height(newSubHeight);
			if (priorSubHeight > (windowHeight - toggleHeight)) {
				$(this).addClass('scroll');
			}
		});
		opts.selectedSubLoop = startSubLoop();
	}
}

/*****************************************
*
* DOM READY
*
******************************************/

if (typeof $ === 'undefined') {
	var div = document.getElementById('loading').childNodes[1];
	div += '<br><br>ERROR: Jquery did not load.';
}

$(function() {
	$messages = $('#messages');
	$subOptions = $('#subOptions');
	$subAudio = $('#subAudio');
	$selectedSub = $subOptions;

	//Hey look it's a controller loop!
	setInterval(function() {
		if (opts.lastPang + opts.pangLimit < Date.now() && !opts.restarting) { //Every pingLimit
				if (!opts.noResponse) { //Only actually append a message if the previous ping didn't also fail (to prevent spam)
					opts.noResponse = true;
					opts.noResponseCount++;
					internalOutput('<div class="connectionClosed internal" data-count="'+opts.noResponseCount+'">You are either AFK, experiencing lag or the connection has closed.</div>', 'internal');
				}
		} else if (opts.noResponse) { //Previous ping attempt failed ohno
				$('.connectionClosed[data-count="'+opts.noResponseCount+'"]:not(.restored)').addClass('restored').text('Your connection has been restored (probably)!');
				opts.noResponse = false;
		}
	}, 2000); //2 seconds


	/*****************************************
	*
	* LOAD SAVED CONFIG
	*
	******************************************/
	var savedConfig = {
		fontsize: getCookie('fontsize'),
		lineheight: getCookie('lineheight'),
		'spingDisabled': getCookie('pingdisabled'),
		'shighlightTerms': getCookie('highlightterms'),
		'shighlightColor': getCookie('highlightcolor'),
		'smusicVolume': getCookie('musicVolume'),
		'smessagecombining': getCookie('messagecombining'),
		'sdarkmode': getCookie('darkmode'),
	};

	if (savedConfig.fontsize) {
		$messages.css('font-size', savedConfig.fontsize);
		internalOutput('<span class="internal boldnshit">Loaded font size setting of: '+savedConfig.fontsize+'</span>', 'internal');
	}
	if (savedConfig.lineheight) {
		$("body").css('line-height', savedConfig.lineheight);
		internalOutput('<span class="internal boldnshit">Loaded line height setting of: '+savedConfig.lineheight+'</span>', 'internal');
	}
	if(savedConfig.sdarkmode == 'true'){
		swap();
	}
	if (savedConfig.spingDisabled) {
		if (savedConfig.spingDisabled == 'true') {
			opts.pingDisabled = true;
			$('#ping').hide();
		}
		internalOutput('<span class="internal boldnshit">Loaded ping display of: '+(opts.pingDisabled ? 'hidden' : 'visible')+'</span>', 'internal');
	}
	if (savedConfig.shighlightTerms) {
		var savedTerms = $.parseJSON(savedConfig.shighlightTerms);
		var actualTerms = '';
		for (var i = 0; i < savedTerms.length; i++) {
			if (savedTerms[i]) {
				actualTerms += savedTerms[i] + ', ';
			}
		}
		if (actualTerms) {
			actualTerms = actualTerms.substring(0, actualTerms.length - 2);
			internalOutput('<span class="internal boldnshit">Loaded highlight strings of: ' + actualTerms+'</span>', 'internal');
			opts.highlightTerms = savedTerms;
		}
	}
	if (savedConfig.shighlightColor) {
		opts.highlightColor = savedConfig.shighlightColor;
		internalOutput('<span class="internal boldnshit">Loaded highlight color of: '+savedConfig.shighlightColor+'</span>', 'internal');
	}
	if (savedConfig.smusicVolume) {
		var newVolume = clamp(savedConfig.smusicVolume, 0, 100);
		$('#adminMusic').prop('volume', newVolume / 100);
		$('#musicVolume').val(newVolume);
		opts.updatedVolume = newVolume;
		sendVolumeUpdate();
		internalOutput('<span class="internal boldnshit">Loaded music volume of: '+savedConfig.smusicVolume+'</span>', 'internal');
	}
	else{
		$('#adminMusic').prop('volume', opts.defaultMusicVolume / 100);
	}

	if (savedConfig.smessagecombining) {
		if (savedConfig.smessagecombining == 'false') {
			opts.messageCombining = false;
		} else {
			opts.messageCombining = true;
		}
	}
	(function() {
		var dataCookie = getCookie('connData');
		if (dataCookie) {
			var dataJ;
			try {
				dataJ = $.parseJSON(dataCookie);
			} catch (e) {
				window.onerror('JSON '+e+'. '+dataCookie, 'browserOutput.html', 434);
				return;
			}
			opts.clientData = dataJ;
		}
	})();


	/*****************************************
	*
	* BASE CHAT OUTPUT EVENTS
	*
	******************************************/

	$('body').on('click', 'a', function(e) {
		e.preventDefault();
	});

	$('body').on('mousedown', function(e) {
		var $target = $(e.target);

		if ($contextMenu && opts.hasOwnProperty('contextMenuTarget') && opts.contextMenuTarget) {
			hideContextMenu();
			return false;
		}

		if ($target.is('a') || $target.parent('a').length || $target.is('input') || $target.is('textarea')) {
			opts.preventFocus = true;
		} else {
			opts.preventFocus = false;
			opts.mouseDownX = e.pageX;
			opts.mouseDownY = e.pageY;
		}
	});

	$messages.on('mousedown', function(e) {
		if ($selectedSub && $selectedSub.is(':visible')) {
			$selectedSub.slideUp('fast', subSlideUp);
			clearInterval(opts.selectedSubLoop);
		}
	});

	$('body').on('mouseup', function(e) {
		if (!opts.preventFocus &&
			(e.pageX >= opts.mouseDownX - opts.clickTolerance && e.pageX <= opts.mouseDownX + opts.clickTolerance) &&
			(e.pageY >= opts.mouseDownY - opts.clickTolerance && e.pageY <= opts.mouseDownY + opts.clickTolerance)
		) {
			opts.mouseDownX = null;
			opts.mouseDownY = null;
			runByond('byond://winset?mapwindow.map.focus=true');
		}
	});

	$messages.on('click', 'a', function(e) {
		var href = $(this).attr('href');
		$(this).addClass('visited');
		if (href[0] == '?' || (href.length >= 8 && href.substring(0,8) == 'byond://')) {
			runByond(href);
		} else {
			href = escaper(href);
			runByond('?action=openLink&link='+href);
		}
	});

	//Fuck everything about this event. Will look into alternatives.
	$('body').on('keydown', function(e) {
		if (e.target.nodeName == 'INPUT' || e.target.nodeName == 'TEXTAREA') {
			return;
		}

		if (e.ctrlKey || e.altKey || e.shiftKey) { //Band-aid "fix" for allowing ctrl+c copy paste etc. Needs a proper fix.
			return;
		}

		e.preventDefault()

		var k = e.which;
		// Hardcoded because else there would be no feedback message.
		if (k == 113) { // F2
			runByond('byond://winset?screenshot=auto');
			internalOutput('Screenshot taken', 'internal');
		}

		var c = "";
		switch (k) {
			case 8:
				c = 'BACK';
			case 9:
				c = 'TAB';
			case 13:
				c = 'ENTER';
			case 19:
				c = 'PAUSE';
			case 27:
				c = 'ESCAPE';
			case 33: // Page up
				c = 'NORTHEAST';
			case 34: // Page down
				c = 'SOUTHEAST';
			case 35: // End
				c = 'SOUTHWEST';
			case 36: // Home
				c = 'NORTHWEST';
			case 37:
				c = 'WEST';
			case 38:
				c = 'NORTH';
			case 39:
				c = 'EAST';
			case 40:
				c = 'SOUTH';
			case 45:
				c = 'INSERT';
			case 46:
				c = 'DELETE';
			case 93: // That weird thing to the right of alt gr.
				c = 'APPS';

			default:
				c = String.fromCharCode(k);
		}

		if (c.length == 0) {
			if (!e.shiftKey) {
				c = c.toLowerCase();
			}
			runByond('byond://winset?mapwindow.map.focus=true;mainwindow.input.text='+c);
			return false;
		} else {
			runByond('byond://winset?mapwindow.map.focus=true');
			return false;
		}
	});

	//Mildly hacky fix for scroll issues on mob change (interface gets resized sometimes, messing up snap-scroll)
	$(window).on('resize', function(e) {
		if ($(this).height() !== opts.priorChatHeight) {
			$('body,html').scrollTop($messages.outerHeight());
			opts.priorChatHeight = $(this).height();
		}
	});


	/*****************************************
	*
	* OPTIONS INTERFACE EVENTS
	*
	******************************************/

	$('body').on('click', '#newMessages', function(e) {
		var messagesHeight = $messages.outerHeight();
		$('body,html').scrollTop(messagesHeight);
		$('#newMessages').remove();
		runByond('byond://winset?mapwindow.map.focus=true');
	});

	$('#toggleOptions').click(function(e) {
		handleToggleClick($subOptions, $(this));
	});
	$('#darkmodetoggle').click(function(e) {
		swap();
	});
	$('#toggleAudio').click(function(e) {
		handleToggleClick($subAudio, $(this));
	});

	$('.sub, .toggle').mouseenter(function() {
		opts.suppressSubClose = true;
	});

	$('.sub, .toggle').mouseleave(function() {
		opts.suppressSubClose = false;
	});

	$('#decreaseFont').click(function(e) {
		savedConfig.fontsize = Math.max(parseInt(savedConfig.fontsize || 13) - 1, 1) + 'px';
		$messages.css({'font-size': savedConfig.fontsize});
		setCookie('fontsize', savedConfig.fontsize, 365);
		internalOutput('<span class="internal boldnshit">Font size set to '+savedConfig.fontsize+'</span>', 'internal');
	});

	$('#increaseFont').click(function(e) {
		savedConfig.fontsize = (parseInt(savedConfig.fontsize || 13) + 1) + 'px';
		$messages.css({'font-size': savedConfig.fontsize});
		setCookie('fontsize', savedConfig.fontsize, 365);
		internalOutput('<span class="internal boldnshit">Font size set to '+savedConfig.fontsize+'</span>', 'internal');
	});

	$('#decreaseLineHeight').click(function(e) {
		savedConfig.lineheight = Math.max(parseFloat(savedConfig.lineheight || 1.2) - 0.1, 0.1).toFixed(1);
		$("body").css({'line-height': savedConfig.lineheight});
		setCookie('lineheight', savedConfig.lineheight, 365);
		internalOutput('<span class="internal boldnshit">Line height set to '+savedConfig.lineheight+'</span>', 'internal');
	});

	$('#increaseLineHeight').click(function(e) {
		savedConfig.lineheight = (parseFloat(savedConfig.lineheight || 1.2) + 0.1).toFixed(1);
		$("body").css({'line-height': savedConfig.lineheight});
		setCookie('lineheight', savedConfig.lineheight, 365);
		internalOutput('<span class="internal boldnshit">Line height set to '+savedConfig.lineheight+'</span>', 'internal');
	});

	$('#togglePing').click(function(e) {
		if (opts.pingDisabled) {
			$('#ping').slideDown('fast');
			opts.pingDisabled = false;
		} else {
			$('#ping').slideUp('fast');
			opts.pingDisabled = true;
		}
		setCookie('pingdisabled', (opts.pingDisabled ? 'true' : 'false'), 365);
	});

	$('#saveLog').click(function(e) {
		// Requires IE 10+ to issue download commands. Just opening a popup
		// window will cause Ctrl+S to save a blank page, ignoring innerHTML.
		if (!window.Blob) {
			output('<span class="big red">This function is only supported on IE 10 and up. Upgrade if possible.</span>', 'internal');
			return;
		}

		$.ajax({
			type: 'GET',
			url: 'browserOutput_white.css',
			success: function(styleData) {
				var blob = new Blob(['<head><title>Chat Log</title><style>', styleData, '</style></head><body>', $messages.html(), '</body>']);

				var fname = 'SS13 Chat Log';
				var date = new Date(), month = date.getMonth(), day = date.getDay(), hours = date.getHours(), mins = date.getMinutes(), secs = date.getSeconds();
				fname += ' ' + date.getFullYear() + '-' + (month < 10 ? '0' : '') + month + '-' + (day < 10 ? '0' : '') + day;
				fname += ' ' + (hours < 10 ? '0' : '') + hours + (mins < 10 ? '0' : '') + mins + (secs < 10 ? '0' : '') + secs;
				fname += '.html';

				window.navigator.msSaveBlob(blob, fname);
			}
		});
	});

	$('#highlightTerm').click(function(e) {
		if ($('.popup .highlightTerm').is(':visible')) {return;}
		var termInputs = '';
		for (var i = 0; i < opts.highlightLimit; i++) {
			termInputs += '<div><input type="text" name="highlightTermInput'+i+'" id="highlightTermInput'+i+'" class="highlightTermInput'+i+'" maxlength="255" value="'+(opts.highlightTerms[i] ? opts.highlightTerms[i] : '')+'" /></div>';
		}
		var popupContent = '<div class="head">String Highlighting</div>' +
			'<div class="highlightPopup" id="highlightPopup">' +
				'<div>Choose up to '+opts.highlightLimit+' strings that will highlight the line when they appear in chat.</div>' +
				'<form id="highlightTermForm">' +
					termInputs +
					'<div><input type="text" name="highlightColor" id="highlightColor" class="highlightColor" '+
						'style="background-color: '+(opts.highlightColor ? opts.highlightColor : '#FFFF00')+'" value="'+(opts.highlightColor ? opts.highlightColor : '#FFFF00')+'" maxlength="7" /></div>' +
					'<div><input type="submit" name="highlightTermSubmit" id="highlightTermSubmit" class="highlightTermSubmit" value="Save" /></div>' +
				'</form>' +
			'</div>';
		createPopup(popupContent, 250);
	});

	$('body').on('keyup', '#highlightColor', function() {
		var color = $('#highlightColor').val();
		color = color.trim();
		if (!color || color.charAt(0) != '#') return;
		$('#highlightColor').css('background-color', color);
	});

	$('body').on('submit', '#highlightTermForm', function(e) {
		e.preventDefault();

		var count = 0;
		while (count < opts.highlightLimit) {
			var term = $('#highlightTermInput'+count).val();
			if (term) {
				term = term.trim();
				if (term === '') {
					opts.highlightTerms[count] = null;
				} else {
					opts.highlightTerms[count] = term.toLowerCase();
				}
			} else {
				opts.highlightTerms[count] = null;
			}
			count++;
		}

		var color = $('#highlightColor').val();
		color = color.trim();
		if (color == '' || color.charAt(0) != '#') {
			opts.highlightColor = '#FFFF00';
		} else {
			opts.highlightColor = color;
		}
		var $popup = $('#highlightPopup').closest('.popup');
		$popup.remove();

		setCookie('highlightterms', JSON.stringify(opts.highlightTerms), 365);
		setCookie('highlightcolor', opts.highlightColor, 365);
	});

	$('#clearMessages').click(function() {
		$messages.empty();
		opts.messageCount = 0;
	});

	$('#musicVolumeSpan').hover(function() {
		$('#musicVolumeText').addClass('hidden');
		$('#musicVolume').removeClass('hidden');
	}, function() {
		$('#musicVolume').addClass('hidden');
		$('#musicVolumeText').removeClass('hidden');
	});

	$('#musicVolume').change(function() {
		var newVolume = $('#musicVolume').val();
		newVolume = clamp(newVolume, 0, 100);
		$('#adminMusic').prop('volume', newVolume / 100);
		setCookie('musicVolume', newVolume, 365);
		opts.updatedVolume = newVolume;
		if(!opts.volumeUpdating) {
			setTimeout(sendVolumeUpdate, opts.volumeUpdateDelay);
			opts.volumeUpdating = true;
		}
	});

	$('#toggleCombine').click(function(e) {
		opts.messageCombining = !opts.messageCombining;
		setCookie('messagecombining', (opts.messageCombining ? 'true' : 'false'), 365);
	});

	$('img.icon').error(iconError);




	/*****************************************
	*
	* KICK EVERYTHING OFF
	*
	******************************************/

	runByond('?_src_=chat&proc=doneLoading');
	if ($('#loading').is(':visible')) {
		$('#loading').remove();
	}
	$('#userBar').show();
	opts.priorChatHeight = $(window).height();
});
