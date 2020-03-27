<?php
//This file contains things that should not be touched by the automatic live tracker
 
//Github lets you have it sign the message with a secret that you can validate. This prevents people from faking events.
//This var should match the secret you configured for this webhook on github.
//This is required as otherwise somebody could trick the script into leaking the api key.
$hookSecret = '08ajh0qj93209qj90jfq932j32r';

//Api key for pushing changelogs.
//This requires the public_repo (or repo for private repositories) and read:org permissions
$apiKey = '209ab8d879c0f987d06a09b9d879c0f987d06a09b9d8787d0a089c';

//Used to prevent potential RCEs
$repoOwnerAndName = "tgstation/tgstation";

//Auto update settings
$enable_live_tracking = true;	//auto update this file from the repository
$path_to_script = 'tools/WebhookProcessor/github_webhook_processor.php';
$tracked_branch = "master";

//PR balance settings.
$trackPRBalance = true;	//set this to false to disable PR balance tracking
$prBalanceJson = '';	//Set this to the path you'd like the writable pr balance file to be stored, not setting it writes it to the working directory
$startingPRBalance = 5;	//Starting balance for never before seen users
//team 133041: tgstation/commit-access
$maintainer_team_id = 133041;	//org team id that is exempt from PR balance system, setting this to null will use anyone with write access to the repo. Get from https://api.github.com/orgs/:org/teams


//anti-spam measures. Don't announce PRs in game to people unless they've gotten a pr merged before
//options are:
//	"repo" - user has to have a pr merged in the repo before.
//	"org" - user has to have a pr merged in any repo in the organization (for repos owned directly by users, this applies to any repo directly owned by the same user.)
//	"disable" - disables.
//defaults to org if left blank or given invalid values.
//This can also be ignored on a per webhook or per game server bases.
$validation = "org";

//how many merged prs must they have under the rules above to have their pr announced to the game servers.
$validation_count = 1;

//enforce changelogs on PRs
$require_changelogs = false;

/*
 * Announcement Settings.
 * 	Allows you to announce prs to discord webhooks or the game servers
 */

/* Common configs:
The following config items can be added to both game server and discord announcement endpoints. Simply replace the $servers part with $discordWebHooks:

include_repos - List of repos in owner/repo format to send to this endpoint. (defaults to all repos if not defined)
	* can be given in place of repo to match all repos under an organization
$servers[$configitem]['include_repos'][] = "tgstation/*";

exclude_repos - List of repos in owner/repo format to not send to this endpoint.
	* can be given in place of repo to match all repos under an organization
$servers[$configitem]['exclude_repos'][] = 'tgstation/TerraGov-Marine-Corps';
$servers[$configitem]['exclude_repos'][] = 'tgstation/tgstation13.org';

exclude_events - List of events to not announce, values: opened, closed, reopened, or merged
$servers[$configitem]['exclude_events'][] = 'closed';
$servers[$configitem]['exclude_events'][] = 'reopened';

announce_secret - Announce secret/security prs that have a [s] in front of the title? Defaults to no.
	Can also be set to 'only' to only announce secret prs.
$servers[$configitem]['announce_secret'] = false;
$servers[$configitem]['announce_secret'] = 'only';

announce_unvalidated - Announce prs by unvalidated users (see the validation setting above)? Defaults to no. 
	Can also be set to 'only' to only announce prs by unvalidated users.
$servers[$configitem]['announce_unvalidated'] = false;

//Note: the same webhook or game server can be given in mutiple announce endpoints with different settings, allowing you to say, have embeds only show on prs to certain repos by excluding the repo in a endpoint with embed = false, and including the repo in a endpoint with embed = true true. This could also be used to only block closed and reopened events on prs by unvalidated users.



//Game servers to announce PRs to.
/*
$configitem = -1;//ignore me

//Game Server Start
$servers[++$configitem] = array();
$servers[$configitem]['address'] = 'game.tgstation13.org';
$servers[$configitem]['port'] = '1337';
$servers[$configitem]['comskey'] = '89aj90cq2fm0amc90832mn9rm90';
//Game Server End

//Game Server Start
$servers[++$configitem] = array();
$servers[$configitem]['address'] = 'game.tgstation13.org';
$servers[$configitem]['port'] = '2337';
$servers[$configitem]['comskey'] = '89aj90cq2fm0amc90832mn9rm90';
//Game Server End

unset($configitem);//ignore
*/

//discord webhooks to announce PRs to.
/*
$configitem = -1;//ignore me

//Discord Webhook Start
$discordWebHooks[++$configitem] = array();

// Webhook Url (you can get this from discord via the webhook setting menu of the server or a channel.)
$discordWebHooks[$configitem]['url'] = 'https://discordapp.com/api/webhooks/538933489920245771/xaoYtVuype-P1rb_uthQLkh_C4iVL3sjtIvFEp7rsfhbBs8tDsSJgE0a9MNWJaoSPBPK';

// show an embed with more info?
$discordWebHooks[$configitem]['embed'] = true;

// if the above is true, don't include the text portion before the embed.
//	 (This option is not advised as it's not compatible with users who disable embeds).
$discordWebHooks[$configitem]['no_text'] = false;
//Discord Webhook End

//Discord Webhook Start
$discordWebHooks[++$configitem] = array();

// Webhook Url (you can get this from discord via the webhook setting menu of the server or a channel.)
$discordWebHooks[$configitem]['url'] = 'https://discordapp.com/api/webhooks/538933686956064769/q0uDel7S6eutvRIyEwsuZo_ppzAoxqUNeU2PRChYVsYoJmmn2f2YYSDoMjy9FhhXKqpI';

// show an embed with more info?
$discordWebHooks[$configitem]['embed'] = true;

// if the above is true, don't include the text portion before the embed.
//	 (This option is not advised as it's not compatible with users who disable embeds).
$discordWebHooks[$configitem]['no_text'] = false;
//Discord Webhook End
*/

unset($configitem); //ignore

