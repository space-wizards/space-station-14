command-description-visualize =
    Takes the input list of entities and puts them into a UI window for easy browsing.
command-description-runverbas =
    Runs a verb over the input entities with the given user.
command-description-acmd-perms =
    Returns the admin permissions of the given command, if any.
command-description-acmd-caninvoke =
    Check if the given player can invoke the given command.
command-description-jobs-jobs =
    Returns all jobs on a station.
command-description-jobs-job =
    Returns a given job on a station.
command-description-jobs-isinfinite =
    Returns true if the input job is infinite, otherwise false.
command-description-jobs-adjust =
    Adjusts the number of slots for the given job.
command-description-jobs-set =
    Sets the number of slots for the given job.
command-description-jobs-amount =
    Returns the number of slots for the given job.
command-description-laws-list =
    Returns a list of all law bound entities.
command-description-laws-get =
    Returns all of the laws for a given entity.
command-description-stations-list =
    Returns a list of all stations.
command-description-stations-get =
    Gets the active station, if and only if there is only one.
command-description-stations-getowningstation =
    Gets the station that a given entity is "owned by" (within)
command-description-stations-grids =
    Returns all grids associated with the input station.
command-description-stations-config =
    Returns the config associated with the input station, if any.
command-description-stations-addgrid =
    Adds a grid to the given station.
command-description-stations-rmgrid =
    Removes a grid from the given station.
command-description-stations-rename =
    Renames the given station.
command-description-stations-largestgrid =
    Returns the largest grid the given station has, if any.
command-description-stations-rerollBounties =
    Clears all the current bounties for the station and gets a new selection.
command-description-stationevent-lsprob =
    Given a BasicStationEventScheduler prototype, lists the probability of different station events occuring out of the entire pool with current conditions.
command-description-stationevent-lsprobtheoretical =
    Given a BasicStationEventScheduler prototype, player count, and round time, lists the probability of different station events occuring based on the specified number of players and round time.
command-description-stationevent-prob =
    Given a BasicStationEventScheduler prototype and an event prototype, returns the probability of a single station event occuring out of the entire pool with current conditions.
command-description-admins-active =
    Returns a list of active admins.
command-description-admins-all =
    Returns a list of ALL admins, including deadmined ones.
command-description-marked =
    Returns the value of $marked as a List<EntityUid>.
command-description-rejuvenate =
    Rejuvenates the given entities, restoring them to full health, clearing status effects, etc.
command-description-tag-list =
    Lists tags on the given entities.
command-description-tag-with =
    Returns only the entities with the given tag from the piped list of entities.
command-description-tag-add =
    Adds a tag to the given entities.
command-description-tag-rm =
    Removes a tag from the given entities.
command-description-tag-addmany =
    Adds a list of tags to the given entities.
command-description-tag-rmmany =
    Removes a list of tags from the given entities.
command-description-polymorph =
    Polymorphs the input entity with the given prototype.
command-description-unpolymorph =
    Reverts a polymorph.
command-description-solution-get =
    Grabs the given solution off the given entity.
command-description-solution-adjreagent =
    Adjusts the given reagent on the given solution.
command-description-mind-get =
    Grabs the mind from the entity, if any.
command-description-mind-control =
    Assumes control of an entity with the given player.
command-description-addaccesslog =
    Adds an access log to this entity. Do note that this bypasses the log's default limit and pause check.
command-description-stationevent-simulate =
    Given a BasicStationEventScheduler prototype, N Rounds, N Players, mean round end, and stddev of round end, Simulates N number of rounds in which events will occur and prints the occurrences of every event after.
command-description-xenoartifact-list =
    List all EntityUids of spawned artifacts.
command-description-xenoartifact-printMatrix =
    Prints out matrix that displays all edges between nodes.
command-description-xenoartifact-totalResearch =
    Gets all research points that can be extracted from artifact currently.
command-description-xenoartifact-averageResearch =
    Calculates amount of research points average generated xeno artifact will output when fully activated.
command-description-xenoartifact-unlockAllNodes =
    Unlocks all nodes of artifact.
command-description-jobboard-completeJob =
    Completes a given salvage job board job for the station.
command-description-scale-set =
    Sets an entity's sprite size to a certain scale (without changing its fixture).
command-description-scale-get =
    Get an entity's sprite scale as set by ScaleVisualsComponent. Does not include any changes directly made in the SpriteComponent.
command-description-scale-multiply =
    Multiply an entity's sprite size with a certain factor (without changing its fixture).
command-description-scale-multiplyvector =
    Multiply an entity's sprite size with a certain 2d vector (without changing its fixture).
command-description-scale-multiplywithfixture =
    Multiply an entity's sprite size with a certain factor (including its fixture).
command-description-dynamicrule-list =
    Lists all currently active dynamic rules, usually this is just one.
command-description-dynamicrule-get =
    Gets the currently active dynamic rule.
command-description-dynamicrule-budget =
    Gets the current budget of the piped dynamic rule(s).
command-description-dynamicrule-adjust =
    Adjusts the budget of the piped dynamic rule(s) by the specified amount.
command-description-dynamicrule-set =
    Sets the budget of the piped dynamic rule(s) to the specified amount.
command-description-dynamicrule-dryrun =
    Returns a list of rules that could be activated if the rule ran at this moment with all current context. This is not a complete list of every single rule that could be run, just a sample of the current valid ones.
command-description-dynamicrule-executenow =
    Executes the piped dynamic rule as if it had reached its regular update time.
command-description-dynamicrule-rules =
    Gets a list of all the rules spawned by the piped dynamic rule.
