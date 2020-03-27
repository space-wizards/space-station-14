<?php
/*
 *	Github webhook In-game PR Announcer and Changelog Generator for /tg/Station13
 *	Author: MrStonedOne
 *	For documentation on the changelog generator see https://tgstation13.org/phpBB/viewtopic.php?f=5&t=5157
 *	To hide prs from being announced in game, place a [s] in front of the title
 *	All runtime errors are echo'ed to the webhook's logs in github
 */

/**CREDITS:
 * GitHub webhook handler template.
 * 
 * @see  https://developer.github.com/webhooks/
 * @author  Miloslav Hula (https://github.com/milo)
 */

define('S_LINK_EMBED', 1<<0);
define('S_MENTIONS', 1<<1);
define('S_MARKDOWN', 1<<2);
define('S_HTML_COMMENTS', 1<<3);

define('F_UNVALIDATED_USER', 1<<0);
define('F_SECRET_PR', 1<<1);

//CONFIGS ARE IN SECRET.PHP, THESE ARE JUST DEFAULTS!

$hookSecret = '08ajh0qj93209qj90jfq932j32r';
$apiKey = '209ab8d879c0f987d06a09b9d879c0f987d06a09b9d8787d0a089c';
$repoOwnerAndName = "tgstation/tgstation";
$servers = array();
$enable_live_tracking = true;
$path_to_script = 'tools/WebhookProcessor/github_webhook_processor.php';
$tracked_branch = "master";
$trackPRBalance = true;
$prBalanceJson = '';
$startingPRBalance = 5;
$maintainer_team_id = 133041;
$validation = "org";
$validation_count = 1;
$tracked_branch = 'master';
$require_changelogs = false;
$discordWebHooks = array();

require_once 'secret.php';

//CONFIG END
function log_error($msg) {
	echo htmlSpecialChars($msg);
	file_put_contents('htwebhookerror.log', '['.date(DATE_ATOM).'] '.$msg.PHP_EOL, FILE_APPEND);
}
set_error_handler(function($severity, $message, $file, $line) {
	throw new \ErrorException($message, 0, $severity, $file, $line);
});
set_exception_handler(function($e) {
	header('HTTP/1.1 500 Internal Server Error');
	log_error('Error on line ' . $e->getLine() . ': ' . $e->getMessage());
	die();
});
$rawPost = NULL;
if (!$hookSecret || $hookSecret == '08ajh0qj93209qj90jfq932j32r')
	throw new \Exception("Hook secret is required and can not be default");
if (!isset($_SERVER['HTTP_X_HUB_SIGNATURE'])) {
	throw new \Exception("HTTP header 'X-Hub-Signature' is missing.");
} elseif (!extension_loaded('hash')) {
	throw new \Exception("Missing 'hash' extension to check the secret code validity.");
}
list($algo, $hash) = explode('=', $_SERVER['HTTP_X_HUB_SIGNATURE'], 2) + array('', '');
if (!in_array($algo, hash_algos(), TRUE)) {
	throw new \Exception("Hash algorithm '$algo' is not supported.");
}
$rawPost = file_get_contents('php://input');
if ($hash !== hash_hmac($algo, $rawPost, $hookSecret)) {
	throw new \Exception('Hook secret does not match.');
}

$contenttype = null;
//apache and nginx/fastcgi/phpfpm call this two different things.
if (!isset($_SERVER['HTTP_CONTENT_TYPE'])) {
	if (!isset($_SERVER['CONTENT_TYPE']))
		throw new \Exception("Missing HTTP 'Content-Type' header.");
	else
		$contenttype = $_SERVER['CONTENT_TYPE'];
} else {
	$contenttype = $_SERVER['HTTP_CONTENT_TYPE'];
}
if (!isset($_SERVER['HTTP_X_GITHUB_EVENT'])) {
	throw new \Exception("Missing HTTP 'X-Github-Event' header.");
}
switch ($contenttype) {
	case 'application/json':
		$json = $rawPost ?: file_get_contents('php://input');
		break;
	case 'application/x-www-form-urlencoded':
		$json = $_POST['payload'];
		break;
	default:
		throw new \Exception("Unsupported content type: $contenttype");
}
# Payload structure depends on triggered event
# https://developer.github.com/v3/activity/events/types/
$payload = json_decode($json, true);

switch (strtolower($_SERVER['HTTP_X_GITHUB_EVENT'])) {
	case 'ping':
		echo 'pong';
		break;
	case 'pull_request':
		handle_pr($payload);
		break;
	case 'pull_request_review':
		if($payload['action'] == 'submitted'){
			$lower_state = strtolower($payload['review']['state']);
			if(($lower_state == 'approved' || $lower_state == 'changes_requested') && is_maintainer($payload, $payload['review']['user']['login']))
				remove_ready_for_review($payload);
		}
		break;
	default:
		header('HTTP/1.0 404 Not Found');
		echo "Event:$_SERVER[HTTP_X_GITHUB_EVENT] Payload:\n";
		print_r($payload); # For debug only. Can be found in GitHub hook log.
		die();
}

function apisend($url, $method = 'GET', $content = null, $authorization = null) {
	if (is_array($content))
		$content = json_encode($content);
	
	$headers = array();
	$headers[] = 'Content-type: application/json';
	if ($authorization)
		$headers[] = 'Authorization: ' . $authorization;
	
	$scontext = array('http' => array(
		'method'		=> $method,
		'header'		=> implode("\r\n", $headers),
		'ignore_errors' => true,
		'user_agent' 	=> 'tgstation13.org-Github-Automation-Tools'
	));
	
	if ($content)
		$scontext['http']['content'] = $content;
	
	return file_get_contents($url, false, stream_context_create($scontext));
	
}

function github_apisend($url, $method = 'GET', $content = NULL) {
	global $apiKey;
	return apisend($url, $method, $content, 'token ' . $apiKey);
}

function discord_webhook_send($webhook, $content) {
	return apisend($webhook, 'POST', $content);
}

function validate_user($payload) {
	global $validation, $validation_count;
	$query = array();
	if (empty($validation))
		$validation = 'org';
	switch (strtolower($validation)) {
		case 'disable':
			return TRUE;
		case 'repo':
			$query['repo'] = $payload['pull_request']['base']['repo']['full_name'];
			break;
		default:
			$query['user'] = $payload['pull_request']['base']['repo']['owner']['login'];
			break;
	}
	$query['author'] = $payload['pull_request']['user']['login'];
	$query['is'] = 'merged';
	$querystring = '';
	foreach($query as $key => $value)
		$querystring .= ($querystring == '' ? '' : '+') . urlencode($key) . ':' . urlencode($value);
	$res = github_apisend('https://api.github.com/search/issues?q='.$querystring);
	$res = json_decode($res, TRUE);
	return $res['total_count'] >= (int)$validation_count;
	
}

function get_labels($payload){
	$url = $payload['pull_request']['issue_url'] . '/labels';
	$existing_labels = json_decode(github_apisend($url), true);
	$existing = array();
	foreach((array) $existing_labels as $label)
		$existing[] = $label['name'];
	return $existing;
}

function check_tag_and_replace($payload, $title_tag, $label, &$array_to_add_label_to){
	$title = $payload['pull_request']['title'];
	if(stripos($title, $title_tag) !== FALSE){
		$array_to_add_label_to[] = $label;
		return true;
	}
	return false;
}

function set_labels($payload, $labels, $remove) {
	$existing = get_labels($payload);
	$tags = array();

	$tags = array_merge($labels, $existing);
	$tags = array_unique($tags);
	if($remove) {
		$tags = array_diff($tags, $remove);
	}

	$final = array();
	foreach($tags as $t)
		$final[] = $t;

	$url = $payload['pull_request']['issue_url'] . '/labels';
	echo github_apisend($url, 'PUT', $final);
}

//rip bs-12
function tag_pr($payload, $opened) {
	//get the mergeable state
	$url = $payload['pull_request']['url'];
	$payload['pull_request'] = json_decode(github_apisend($url), TRUE);
	if($payload['pull_request']['mergeable'] == null) {
		//STILL not ready. Give it a bit, then try one more time
		sleep(10);
		$payload['pull_request'] = json_decode(github_apisend($url), TRUE);
	}
	
	$tags = array();
	$title = $payload['pull_request']['title'];
	if($opened) {	//you only have one shot on these ones so as to not annoy maintainers
		$tags = checkchangelog($payload, false);

		if(strpos(strtolower($title), 'refactor') !== FALSE)
			$tags[] = 'Refactor';
		
		if(strpos(strtolower($title), 'revert') !== FALSE)
			$tags[] = 'Revert';
		if(strpos(strtolower($title), 'removes') !== FALSE)
			$tags[] = 'Removal';
	}

	$remove = array('Test Merge Candidate');

	$mergeable = $payload['pull_request']['mergeable'];
	if($mergeable === TRUE)	//only look for the false value
		$remove[] = 'Merge Conflict';
	else if ($mergeable === FALSE)
		$tags[] = 'Merge Conflict';

	$treetags = array('_maps' => 'Map Edit', 'tools' => 'Tools', 'SQL' => 'SQL', '.github' => 'GitHub');
	$addonlytags = array('icons' => 'Sprites', 'sound' => 'Sound', 'config' => 'Config Update', 'code/controllers/configuration/entries' => 'Config Update', 'tgui' => 'UI');
	foreach($treetags as $tree => $tag)
		if(has_tree_been_edited($payload, $tree))
			$tags[] = $tag;
		else
			$remove[] = $tag;
	foreach($addonlytags as $tree => $tag)
		if(has_tree_been_edited($payload, $tree))
			$tags[] = $tag;

	check_tag_and_replace($payload, '[dnm]', 'Do Not Merge', $tags);
	if(!check_tag_and_replace($payload, '[wip]', 'Work In Progress', $tags) && check_tag_and_replace($payload, '[ready]', 'Work In Progress', $remove))
		$tags[] = 'Needs Review';

	return array($tags, $remove);
}

function remove_ready_for_review($payload, $labels = null){
	if($labels == null)
		$labels = get_labels($payload);
	$index = array_search('Needs Review', $labels);
	if($index !== FALSE)
		unset($labels[$index]);
	$url = $payload['pull_request']['issue_url'] . '/labels';
	github_apisend($url, 'PUT', $labels);
}

function dismiss_review($payload, $id, $reason){
	$content = array('message' => $reason);
	github_apisend($payload['pull_request']['url'] . '/reviews/' . $id . '/dismissals', 'PUT', $content);
}

function get_reviews($payload){
	return json_decode(github_apisend($payload['pull_request']['url'] . '/reviews'), true);
}

function check_ready_for_review($payload, $labels = null, $remove = array()){
	$r4rlabel = 'Needs Review';
	$labels_which_should_not_be_ready = array('Do Not Merge', 'Work In Progress', 'Merge Conflict');
	$has_label_already = false;
	$should_not_have_label = false;
	if($labels == null)
		$labels = get_labels($payload);
	$returned = array($labels, $remove);
	//if the label is already there we may need to remove it
	foreach($labels as $L){
		if(in_array($L, $labels_which_should_not_be_ready))
			$should_not_have_label = true;
		if($L == $r4rlabel)
			$has_label_already = true;
	}
	
	if($has_label_already && $should_not_have_label){
		$remove[] = $r4rlabel;
		return $returned;
	}

	//find all reviews to see if changes were requested at some point
	$reviews = get_reviews($payload);

	$reviews_ids_with_changes_requested = array();
	$dismissed_an_approved_review = false;

	foreach($reviews as $R)
		if(is_maintainer($payload, $R['user']['login'])){
			$lower_state = strtolower($R['state']);
			if($lower_state == 'changes_requested')
				$reviews_ids_with_changes_requested[] = $R['id'];
			else if ($lower_state == 'approved'){
				dismiss_review($payload, $R['id'], 'Out of date review');
				$dismissed_an_approved_review = true;
			}
		}

	if(!$dismissed_an_approved_review && count($reviews_ids_with_changes_requested) == 0){
		if($has_label_already)
			$remove[] = $r4rlabel;
		return $returned;	//no need to be here
	}

	if(count($reviews_ids_with_changes_requested) > 0){
		//now get the review comments for the offending reviews

		$review_comments = json_decode(github_apisend($payload['pull_request']['review_comments_url']), true);

		foreach($review_comments as $C){
			//make sure they are part of an offending review
			if(!in_array($C['pull_request_review_id'], $reviews_ids_with_changes_requested))
				continue;
			
			//review comments which are outdated have a null position
			if($C['position'] !== null){
				if($has_label_already)
					$remove[] = $r4rlabel;
				return $returned;	//no need to tag
			}
		}
	}

	//finally, add it if necessary
	if(!$has_label_already){
		$labels[] = $r4rlabel;
	}
	return $returned;
}

function check_dismiss_changelog_review($payload){
	global $require_changelog;
	global $no_changelog;

	if(!$require_changelog)
		return;
	
	if(!$no_changelog)
		checkchangelog($payload, false);
	
	$review_message = 'Your changelog for this PR is either malformed or non-existent. Please create one to document your changes.';

	$reviews = get_reviews($payload);
	if($no_changelog){
		//check and see if we've already have this review
		foreach($reviews as $R)
			if($R['body'] == $review_message && strtolower($R['state']) == 'changes_requested')
				return;
		//otherwise make it ourself
		github_apisend($payload['pull_request']['url'] . '/reviews', 'POST', array('body' => $review_message, 'event' => 'REQUEST_CHANGES'));
	}
	else
		//kill previous reviews
		foreach($reviews as $R)
			if($R['body'] == $review_message && strtolower($R['state']) == 'changes_requested')
				dismiss_review($payload, $R['id'], 'Changelog added/fixed.');
}

function handle_pr($payload) {
	global $no_changelog;
	$action = 'opened';
	$validated = validate_user($payload);
	switch ($payload["action"]) {
		case 'opened':
			list($labels, $remove) = tag_pr($payload, true);
			set_labels($payload, $labels, $remove);
			if($no_changelog)
				check_dismiss_changelog_review($payload);
			if(get_pr_code_friendliness($payload) <= 0){
				$balances = pr_balances();
				$author = $payload['pull_request']['user']['login'];
				if(isset($balances[$author]) && $balances[$author] < 0 && !is_maintainer($payload, $author))
					create_comment($payload, 'You currently have a negative Fix/Feature pull request delta of ' . $balances[$author] . '. Maintainers may close this PR at will. Fixing issues or improving the codebase will improve this score.');
			}
			break;
		case 'edited':
			check_dismiss_changelog_review($payload);
		case 'synchronize':
			list($labels, $remove) = tag_pr($payload, false);
			if($payload['action'] == 'synchronize')
				list($labels, $remove) = check_ready_for_review($payload, $labels, $remove);
			set_labels($payload, $labels, $remove);
			return;
		case 'reopened':
			$action = $payload['action'];
			break;
		case 'closed':
			if (!$payload['pull_request']['merged']) {
				$action = 'closed';
			}
			else {
				$action = 'merged';
				auto_update($payload);
				checkchangelog($payload, true);
				update_pr_balance($payload);
				$validated = TRUE; //pr merged events always get announced.
			}
			break;
		default:
			return;
	} 
	
	$pr_flags = 0;
	if (strpos(strtolower($payload['pull_request']['title']), '[s]') !== false) {
		$pr_flags |= F_SECRET_PR;
	}
	if (!$validated) {
		$pr_flags |= F_UNVALIDATED_USER;
	}
	discord_announce($action, $payload, $pr_flags);
	game_announce($action, $payload, $pr_flags);
	
}

function filter_announce_targets($targets, $owner, $repo, $action, $pr_flags) {
	foreach ($targets as $i=>$target) {
		if (isset($target['exclude_events']) && in_array($action, array_map('strtolower', $target['exclude_events']))) {
			unset($targets[$i]);
			continue;
		}
		
		if (isset($target['announce_secret']) && $target['announce_secret']) {
			if (!($pr_flags & F_SECRET_PR) && $target['announce_secret'] === 'only') {
				unset($targets[$i]);
				continue;
			}
		} else if ($pr_flags & F_SECRET_PR) {
			unset($targets[$i]);
			continue;
		}
		
		if (isset($target['announce_unvalidated']) && $target['announce_unvalidated']) {
			if (!($pr_flags & F_UNVALIDATED_USER) && $target['announce_unvalidated'] === 'only') {
				unset($targets[$i]);
				continue;
			}
		} else if ($pr_flags & F_UNVALIDATED_USER) {
			unset($targets[$i]);
			continue;
		}
		
		$wildcard = false;
		if (isset($target['include_repos'])) {
			foreach ($target['include_repos'] as $match_string) {
				$owner_repo_pair = explode('/', strtolower($match_string));
				if (count($owner_repo_pair) != 2) {
					log_error('Bad include repo: `'. $match_string.'`');
					continue;
				}
				if (strtolower($owner) == $owner_repo_pair[0]) {
					if (strtolower($repo) == $owner_repo_pair[1])
						continue 2; //don't parse excludes when we have an exact include match
					if ($owner_repo_pair[1] == '*') {
						$wildcard = true;
						continue; //do parse excludes when we have a wildcard match (but check the other entries for exact matches first)
					}
				}
			}
			if (!$wildcard) {
				unset($targets[$i]);
				continue;
			}
		}
		
		if (isset($target['exclude_repos']))
			foreach ($target['exclude_repos'] as $match_string) {
				$owner_repo_pair = explode('/', strtolower($match_string));
				if (count($owner_repo_pair) != 2) {
					log_error('Bad exclude repo: `'. $match_string.'`');
					continue;
				}
				if (strtolower($owner) == $owner_repo_pair[0]) {
					if (strtolower($repo) == $owner_repo_pair[1]) {
						unset($targets[$i]);
						continue 2;
					}
					if ($owner_repo_pair[1] == '*') {
						if ($wildcard)
							log_error('Identical wildcard include and exclude: `'.$match_string.'`. Excluding.');
						unset($targets[$i]);
						continue 2;
					}
				}
			}
	}
	return $targets;
}

function game_announce($action, $payload, $pr_flags) {
	global $servers;
	
	$msg = '['.$payload['pull_request']['base']['repo']['full_name'].'] Pull Request '.$action.' by '.htmlSpecialChars($payload['sender']['login']).': <a href="'.$payload['pull_request']['html_url'].'">'.htmlSpecialChars('#'.$payload['pull_request']['number'].' '.$payload['pull_request']['user']['login'].' - '.$payload['pull_request']['title']).'</a>';

	$game_servers = filter_announce_targets($servers, $payload['pull_request']['base']['repo']['owner']['login'], $payload['pull_request']['base']['repo']['name'], $action, $pr_flags);
	
	$msg = '?announce='.urlencode($msg).'&payload='.urlencode(json_encode($payload));
	
	foreach ($game_servers as $serverid => $server) {
		$server_message = $msg;
		if (isset($server['comskey']))
			$server_message .= '&key='.urlencode($server['comskey']);
		game_server_send($server['address'], $server['port'], $server_message);
	}

}

function discord_announce($action, $payload, $pr_flags) {
	global $discordWebHooks;
	$color;
	switch ($action) {
		case 'reopened':
		case 'opened':
			$color = 0x2cbe4e;
			break;
		case 'closed':
			$color = 0xcb2431;
			break;
		case 'merged':
			$color = 0x6f42c1;
			break;
		default:
			return;
	}
	$data = array(
		'username' => 'GitHub',
		'avatar_url' => $payload['pull_request']['base']['user']['avatar_url'],
	);
	
	$content = 'Pull Request #'.$payload['pull_request']['number'].' *'.$action.'* by '.discord_sanitize($payload['sender']['login'])."\n".discord_sanitize($payload['pull_request']['user']['login']).' - __**'.discord_sanitize($payload['pull_request']['title']).'**__'."\n".'<'.$payload['pull_request']['html_url'].'>';
	
	$embeds = array(
			array(
				'title' => '__**'.discord_sanitize($payload['pull_request']['title'], S_MARKDOWN).'**__',
				'description' => discord_sanitize(str_replace(array("\r\n", "\n"), array(' ', ' '), substr($payload['pull_request']['body'], 0, 320)), S_HTML_COMMENTS),
				'url' => $payload['pull_request']['html_url'],
				'color' => $color,
				'author' => array(
					'name' => discord_sanitize($payload['pull_request']['user']['login'], S_MARKDOWN),
					'url' => $payload['pull_request']['user']['html_url'],
					'icon_url' => $payload['pull_request']['user']['avatar_url']
				),
				'footer' => array(
					'text' => '#'.$payload['pull_request']['number'].' '.discord_sanitize($payload['pull_request']['base']['repo']['full_name'], S_MARKDOWN).' '.discord_sanitize($payload['pull_request']['head']['ref'], S_MARKDOWN).' -> '.discord_sanitize($payload['pull_request']['base']['ref'], S_MARKDOWN),
					'icon_url' => $payload['pull_request']['base']['user']['avatar_url']
				)
			)
	);
	$discordWebHook_targets = filter_announce_targets($discordWebHooks, $payload['pull_request']['base']['repo']['owner']['login'], $payload['pull_request']['base']['repo']['name'], $action, $pr_flags);
	foreach ($discordWebHook_targets as $discordWebHook) {
		$sending_data = $data;
		if (isset($discordWebHook['embed']) && $discordWebHook['embed']) {
			$sending_data['embeds'] = $embeds;
			if (!isset($discordWebHook['no_text']) || !$discordWebHook['no_text'])
				$sending_data['content'] = $content;
		} else {
			$sending_data['content'] = $content;
		}
		discord_webhook_send($discordWebHook['url'], $sending_data);
	}
	
}

function discord_sanitize($text, $flags = S_MENTIONS|S_LINK_EMBED|S_MARKDOWN) { 
	if ($flags & S_MARKDOWN)
		$text = str_ireplace(array('\\', '*', '_', '~', '`', '|'), (array('\\\\', '\\*', '\\_', '\\~', '\\`', '\\|')), $text);
	
	if ($flags & S_HTML_COMMENTS)
		$text = preg_replace('/<!--(.*)-->/Uis', '', $text);
	
	if ($flags & S_MENTIONS)
		$text = str_ireplace(array('@everyone', '@here', '<@'), array('`@everyone`', '`@here`', '@<'), $text);

	if ($flags & S_LINK_EMBED)
		$text = preg_replace("/((https?|ftp|byond)\:\/\/)([a-z0-9-.]*)\.([a-z]{2,3})(\:[0-9]{2,5})?(\/(?:[a-z0-9+\$_-]\.?)+)*\/?(\?[a-z+&\$_.-][a-z0-9;:@&%=+\/\$_.-]*)?(#[a-z_.-][a-z0-9+\$_.-]*)?/mi", '<$0>', $text);
	
	return $text;
}

//creates a comment on the payload issue
function create_comment($payload, $comment){
	github_apisend($payload['pull_request']['comments_url'], 'POST', json_encode(array('body' => $comment)));
}

//returns the payload issue's labels as a flat array
function get_pr_labels_array($payload){
	$url = $payload['pull_request']['issue_url'] . '/labels';
	$issue = json_decode(github_apisend($url), true);
	$result = array();
	foreach($issue as $l)
		$result[] = $l['name'];
	return $result;
}

//helper for getting the path the the balance json file
function pr_balance_json_path(){
	global $prBalanceJson;
	return $prBalanceJson != '' ? $prBalanceJson : 'pr_balances.json';
}

//return the assoc array of login -> balance for prs
function pr_balances(){
	$path = pr_balance_json_path();
	if(file_exists($path))
		return json_decode(file_get_contents($path), true);
	else
		return array();
}

//returns the difference in PR balance a pull request would cause
function get_pr_code_friendliness($payload, $oldbalance = null){
	global $startingPRBalance;
	if($oldbalance == null)
		$oldbalance = $startingPRBalance;
	$labels = get_pr_labels_array($payload);
	//anything not in this list defaults to 0
	$label_values = array(
		'Fix' => 2,
		'Refactor' => 2,
		'CI/Tests' => 3,
		'Code Improvement' => 1,
		'Grammar and Formatting' => 1,
		'Priority: High' => 4,
		'Priority: CRITICAL' => 5,
		'Logging' => 1,
		'Feedback' => 1,
		'Performance' => 3,
		'Feature' => -1,
		'Balance/Rebalance' => -1,
		'PRB: Reset' => $startingPRBalance - $oldbalance,
	);

	$affecting = 0;
	$is_neutral = FALSE;
	$found_something_positive = false;
	foreach($labels as $l){
		if($l == 'PRB: No Update') {	//no effect on balance
			$affecting = 0;
			break;
		}
		else if(isset($label_values[$l])) {
			$friendliness = $label_values[$l];
			if($friendliness > 0)
				$found_something_positive = true;
			$affecting = $found_something_positive ? max($affecting, $friendliness) : $friendliness;
		}
	}
	return $affecting;
}

function is_maintainer($payload, $author){
	global $maintainer_team_id;
	$repo_is_org = $payload['pull_request']['base']['repo']['owner']['type'] == 'Organization';
	if($maintainer_team_id == null || !$repo_is_org) {
		$collaburl = str_replace('{/collaborator}', '/' . $author, $payload['pull_request']['base']['repo']['collaborators_url']) . '/permission';
		$perms = json_decode(github_apisend($collaburl), true);
		$permlevel = $perms['permission'];
		return $permlevel == 'admin' || $permlevel == 'write';
	}
	else {
		$check_url = 'https://api.github.com/teams/' . $maintainer_team_id . '/memberships/' . $author;
		$result = json_decode(github_apisend($check_url), true);
		return isset($result['state']) && $result['state'] == 'active';
	}
}

//payload is a merged pull request, updates the pr balances file with the correct positive or negative balance based on comments
function update_pr_balance($payload) {
	global $startingPRBalance;
	global $trackPRBalance;
	if(!$trackPRBalance)
		return;
	$author = $payload['pull_request']['user']['login'];
	$balances = pr_balances();
	if(!isset($balances[$author]))
		$balances[$author] = $startingPRBalance;
	$friendliness = get_pr_code_friendliness($payload, $balances[$author]);
	$balances[$author] += $friendliness;
	if(!is_maintainer($payload, $author)){	//immune
		if($balances[$author] < 0 && $friendliness < 0)
			create_comment($payload, 'Your Fix/Feature pull request delta is currently below zero (' . $balances[$author] . '). Maintainers may close future Feature/Tweak/Balance PRs. Fixing issues or helping to improve the codebase will raise this score.');
		else if($balances[$author] >= 0 && ($balances[$author] - $friendliness) < 0)
			create_comment($payload, 'Your Fix/Feature pull request delta is now above zero (' . $balances[$author] . '). Feel free to make Feature/Tweak/Balance PRs.');
	}
	$balances_file = fopen(pr_balance_json_path(), 'w');
	fwrite($balances_file, json_encode($balances));
	fclose($balances_file);
}

$github_diff = null;

function get_diff($payload) {
	global $github_diff;
	if ($github_diff === null && $payload['pull_request']['diff_url']) {
		//go to the diff url
		$url = $payload['pull_request']['diff_url'];
		$github_diff = file_get_contents($url);
	}
	return $github_diff;
}

function auto_update($payload){
	global $enable_live_tracking;
	global $path_to_script;
	global $repoOwnerAndName;
	global $tracked_branch;
	global $github_diff;
	if(!$enable_live_tracking || !has_tree_been_edited($payload, $path_to_script) || $payload['pull_request']['base']['ref'] != $tracked_branch)
		return;

	get_diff($payload);
	$content = file_get_contents('https://raw.githubusercontent.com/' . $repoOwnerAndName . '/' . $tracked_branch . '/'. $path_to_script);
	$content_diff = "### Diff not available. :slightly_frowning_face:";
	if($github_diff && preg_match('/(diff --git a\/' . preg_quote($path_to_script, '/') . '.+?)(?:\Rdiff|$)/s', $github_diff, $matches)) {
		$script_diff = $matches[1];
		if($script_diff) {
			$content_diff = "``" . "`DIFF\n" . $script_diff ."\n``" . "`";
		}
	}
	create_comment($payload, "Edit detected. Self updating... \n<details><summary>Here are my changes:</summary>\n\n" . $content_diff . "\n</details>\n<details><summary>Here is my new code:</summary>\n\n``" . "`HTML+PHP\n" . $content . "\n``" . '`\n</details>');

	$code_file = fopen(basename($path_to_script), 'w');
	fwrite($code_file, $content);
	fclose($code_file);
}

function has_tree_been_edited($payload, $tree){
	global $github_diff;
	get_diff($payload);
	//find things in the _maps/map_files tree
	//e.g. diff --git a/_maps/map_files/Cerestation/cerestation.dmm b/_maps/map_files/Cerestation/cerestation.dmm
	return ($github_diff !== FALSE) && (preg_match('/^diff --git a\/' . preg_quote($tree, '/') . '/m', $github_diff) !== 0);
}

$no_changelog = false;
function checkchangelog($payload, $compile = true) {
	global $no_changelog;
	if (!isset($payload['pull_request']) || !isset($payload['pull_request']['body'])) {
		return;
	}
	if (!isset($payload['pull_request']['user']) || !isset($payload['pull_request']['user']['login'])) {
		return;
	}
	$body = $payload['pull_request']['body'];

	$tags = array();

	if(preg_match('/(?i)(fix|fixes|fixed|resolve|resolves|resolved)\s*#[0-9]+/',$body))	//github autoclose syntax
		$tags[] = 'Fix';

	$body = str_replace("\r\n", "\n", $body);
	$body = explode("\n", $body);

	$username = $payload['pull_request']['user']['login'];
	$incltag = false;
	$changelogbody = array();
	$currentchangelogblock = array();
	$foundcltag = false;
	foreach ($body as $line) {
		$line = trim($line);
		if (substr($line,0,4) == ':cl:' || substr($line,0,1) == '??') {
			$incltag = true;
			$foundcltag = true;
			$pos = strpos($line, " ");
			if ($pos) {
				$tmp = substr($line, $pos+1);
				if (trim($tmp) != 'optional name here')
					$username = $tmp;
			}
			continue;
		} else if (substr($line,0,5) == '/:cl:' || substr($line,0,6) == '/ :cl:' || substr($line,0,5) == ':/cl:' || substr($line,0,5) == '/??' || substr($line,0,6) == '/ ??' ) {
			$incltag = false;
			$changelogbody = array_merge($changelogbody, $currentchangelogblock);
			continue;
		}
		if (!$incltag)
			continue;
		
		$firstword = explode(' ', $line)[0];
		$pos = strpos($line, " ");
		$item = '';
		if ($pos) {
			$firstword = trim(substr($line, 0, $pos));
			$item = trim(substr($line, $pos+1));
		} else {
			$firstword = $line;
		}
		
		if (!strlen($firstword)) {
			$currentchangelogblock[count($currentchangelogblock)-1]['body'] .= "\n";
			continue;
		}
		//not a prefix line.
		//so we add it to the last changelog entry as a separate line
		if (!strlen($firstword) || $firstword[strlen($firstword)-1] != ':') {
			if (count($currentchangelogblock) <= 0)
				continue;
			$currentchangelogblock[count($currentchangelogblock)-1]['body'] .= "\n".$line;
			continue;
		}
		$cltype = strtolower(substr($firstword, 0, -1));
		switch ($cltype) {
			case 'fix':
			case 'fixes':
			case 'bugfix':
				if($item != 'fixed a few things') {
					$tags[] = 'Fix';
					$currentchangelogblock[] = array('type' => 'bugfix', 'body' => $item);
				}
				break;
			case 'rsctweak':
			case 'tweaks':
			case 'tweak':
				if($item != 'tweaked a few things') {
					$tags[] = 'Tweak';
					$currentchangelogblock[] = array('type' => 'tweak', 'body' => $item);
				}
				break;
			case 'soundadd':
				if($item != 'added a new sound thingy') {
					$tags[] = 'Sound';
					$currentchangelogblock[] = array('type' => 'soundadd', 'body' => $item);
				}
				break;
			case 'sounddel':
				if($item != 'removed an old sound thingy') {
					$tags[] = 'Sound';
					$tags[] = 'Removal';
					$currentchangelogblock[] = array('type' => 'sounddel', 'body' => $item);
				}
				break;
			case 'add':
			case 'adds':
			case 'rscadd':
				if($item != 'Added new things' && $item != 'Added more things') {
					$tags[] = 'Feature';
					$currentchangelogblock[] = array('type' => 'rscadd', 'body' => $item);
				}
				break;
			case 'del':
			case 'dels':
			case 'rscdel':
				if($item != 'Removed old things') {
					$tags[] = 'Removal';
					$currentchangelogblock[] = array('type' => 'rscdel', 'body' => $item);
				}
				break;
			case 'imageadd':
				if($item != 'added some icons and images') {
					$tags[] = 'Sprites';
					$currentchangelogblock[] = array('type' => 'imageadd', 'body' => $item);
				}
				break;
			case 'imagedel':
				if($item != 'deleted some icons and images') {
					$tags[] = 'Sprites';
					$tags[] = 'Removal';
					$currentchangelogblock[] = array('type' => 'imagedel', 'body' => $item);
				}
				break;
			case 'typo':
			case 'spellcheck':
				if($item != 'fixed a few typos') {
					$tags[] = 'Grammar and Formatting';
					$currentchangelogblock[] = array('type' => 'spellcheck', 'body' => $item);
				}
				break;
			case 'balance':
			case 'rebalance':
				if($item != 'rebalanced something'){
					$tags[] = 'Balance/Rebalance';
					$currentchangelogblock[] = array('type' => 'balance', 'body' => $item);
				}
				break;
			case 'tgs':
				$currentchangelogblock[] = array('type' => 'tgs', 'body' => $item);
				break;
			case 'code_imp':
			case 'code':
				if($item != 'changed some code'){
					$tags[] = 'Code Improvement';
					$currentchangelogblock[] = array('type' => 'code_imp', 'body' => $item);
				}
				break;
			case 'refactor':
				if($item != 'refactored some code'){
					$tags[] = 'Refactor';
					$currentchangelogblock[] = array('type' => 'refactor', 'body' => $item);
				}
				break;
			case 'config':
				if($item != 'changed some config setting'){
					$tags[] = 'Config Update';
					$currentchangelogblock[] = array('type' => 'config', 'body' => $item);
				}
				break;
			case 'admin':
				if($item != 'messed with admin stuff'){
					$tags[] = 'Administration';
					$currentchangelogblock[] = array('type' => 'admin', 'body' => $item);
				}
				break;
			case 'server':
				if($item != 'something server ops should know')
					$currentchangelogblock[] = array('type' => 'server', 'body' => $item);
				break;			
			default:
				//we add it to the last changelog entry as a separate line
				if (count($currentchangelogblock) > 0)
					$currentchangelogblock[count($currentchangelogblock)-1]['body'] .= "\n".$line;
				break;
		}
	}

	if(!count($changelogbody))
		$no_changelog = true;

	if ($no_changelog || !$compile)
		return $tags;

	$file = 'author: "'.trim(str_replace(array("\\", '"'), array("\\\\", "\\\""), $username)).'"'."\n";
	$file .= "delete-after: True\n";
	$file .= "changes: \n";
	foreach ($changelogbody as $changelogitem) {
		$type = $changelogitem['type'];
		$body = trim(str_replace(array("\\", '"'), array("\\\\", "\\\""), $changelogitem['body']));
		$file .= '  - '.$type.': "'.$body.'"';
		$file .= "\n";
	}
	$content = array (
		'branch' 	=> $payload['pull_request']['base']['ref'],
		'message' 	=> 'Automatic changelog generation for PR #'.$payload['pull_request']['number'].' [ci skip]',
		'content' 	=> base64_encode($file)
	);

	$filename = '/html/changelogs/AutoChangeLog-pr-'.$payload['pull_request']['number'].'.yml';
	echo github_apisend($payload['pull_request']['base']['repo']['url'].'/contents'.$filename, 'PUT', $content);
}

function game_server_send($addr, $port, $str) {
	// All queries must begin with a question mark (ie "?players")
	if($str{0} != '?') $str = ('?' . $str);
	
	/* --- Prepare a packet to send to the server (based on a reverse-engineered packet structure) --- */
	$query = "\x00\x83" . pack('n', strlen($str) + 6) . "\x00\x00\x00\x00\x00" . $str . "\x00";
	
	/* --- Create a socket and connect it to the server --- */
	$server = socket_create(AF_INET,SOCK_STREAM,SOL_TCP) or exit("ERROR");
	socket_set_option($server, SOL_SOCKET, SO_SNDTIMEO, array('sec' => 2, 'usec' => 0)); //sets connect and send timeout to 2 seconds
	if(!socket_connect($server,$addr,$port)) {
		return "ERROR: Connection failed";
	}

	
	/* --- Send bytes to the server. Loop until all bytes have been sent --- */
	$bytestosend = strlen($query);
	$bytessent = 0;
	while ($bytessent < $bytestosend) {
		//echo $bytessent.'<br>';
		$result = socket_write($server,substr($query,$bytessent),$bytestosend-$bytessent);
		//echo 'Sent '.$result.' bytes<br>';
		if ($result===FALSE) 
			return "ERROR: " . socket_strerror(socket_last_error());
		$bytessent += $result;
	}
	
	/* --- Idle for a while until recieved bytes from game server --- */
	$result = socket_read($server, 10000, PHP_BINARY_READ);
	socket_close($server); // we don't need this anymore
	
	if($result != "") {
		if($result{0} == "\x00" || $result{1} == "\x83") { // make sure it's the right packet format
			
			// Actually begin reading the output:
			$sizebytes = unpack('n', $result{2} . $result{3}); // array size of the type identifier and content
			$size = $sizebytes[1] - 1; // size of the string/floating-point (minus the size of the identifier byte)
			
			if($result{4} == "\x2a") { // 4-byte big-endian floating-point
				$unpackint = unpack('f', $result{5} . $result{6} . $result{7} . $result{8}); // 4 possible bytes: add them up together, unpack them as a floating-point
				return $unpackint[1];
			}
			else if($result{4} == "\x06") { // ASCII string
				$unpackstr = ""; // result string
				$index = 5; // string index
				
				while($size > 0) { // loop through the entire ASCII string
					$size--;
					$unpackstr .= $result{$index}; // add the string position to return string
					$index++;
				}
				return $unpackstr;
			}
		}
	}
	return "";
}
?>
