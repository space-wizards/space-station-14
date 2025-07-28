github-command-test-name = testgithubapi

cmd-testgithubapi-desc = This command makes a few requests to the github api. Remember to check the servers console for errors.
cmd-testgithubapi-help = Usage: testgithubapi

github-command-not-enabled = The api is not enabled!
github-command-no-key = The key path is empty!
github-command-no-key = The app id is empty!
github-command-no-repo-name = The repository name is empty!
github-command-no-owner = The repository owner is empty!

github-command-issue-title-one = This is a test issue (1/2)
github-command-issue-description-one = This is the description of the first issue. :)
github-command-issue-title-two = This is a test issue (2/2)
github-command-issue-description-two = This is the description of the second issue. :P

github-command-finish = Check your repository for (2) newly created issues! If you don't see any, check the console for errors.

github-issue-title-format = {$title} [In game report]

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
