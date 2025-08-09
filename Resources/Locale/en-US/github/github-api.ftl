github-command-test-name = testgithubapi

cmd-testgithubapi-desc = This command makes an issue request to the github api. Remember to check the servers console for errors.
cmd-testgithubapi-help = Usage: testgithubapi

github-command-not-enabled = The api is not enabled!
github-command-no-path = The key path is empty!
github-command-no-app-id = The app id is empty!
github-command-no-repo-name = The repository name is empty!
github-command-no-owner = The repository owner is empty!

github-command-issue-title-one = This is a test issue!
github-command-issue-description-one = This is the description of the first issue. :)

github-command-finish = Check your repository for a newly created issue. If you don't see any, check the server console for errors!

github-issue-format = ## Description:
                      {$description}

                      ## Meta Data:
                      Build version: {$buildVersion}
                      Engine version: {$engineVersion}

                      Server name: {$serverName}
                      Submitted time: {$submittedTime}

                      -- Round information --
                      Round number: {$roundNumber}
                      Round time: {$roundTime}
                      Round type: {$roundType}
                      Map: {$map}
                      Number of players: {$numberOfPlayers}

                      -- Submitter information --
                      Player name: {$username}
                      Player GUID: {$playerGUID}
