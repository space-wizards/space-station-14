cmd-unlocknode-desc = Unlocks a node on a given artifact
cmd-unlocknode-help = unlocknode <artifact uid> <node uid>

cmd-spawnartifactwithnode-desc = Spawns xeno artifact with single node, with set trigger and effect
cmd-spawnartifactwithnode-help = <artifact ent proto id> <effect ent proto id> <trigger proto id>
cmd-spawnartifactwithnode-spawn-artifact-item-hint = use hand-held artifact
cmd-spawnartifactwithnode-spawn-artifact-structure-hint = use structure-like stationary artifact
cmd-spawnartifactwithnode-spawn-artifact-type-hint = <artifact entity proto id>

cmd-spawnartifactwithnode-failed-to-find-current-player = Expected a player entity to be attached to current session so artifact could be spawned near it, but found null
cmd-spawnartifactwithnode-invalid-prototype-id = Spawned artifact from prototype {$entProtoId} but it got no XenoArtifactComponent component!

cmd-removenodefromartifact-desc = Removes node from xeno artifact, breaking all connected edges
cmd-removenodefromartifact-help = <artifact uid> <node uid>

cmd-addedgebetweeennodes-desc = Creates edge, linking two nodes inside one xeno artifact
cmd-addedgebetweeennodes-help = <artifact uid> <node uid from> <node uid to>

cmd-createnodeinartifact-desc = Create node in xeno artifact,
cmd-createnodeinartifact-help = <artifact uid> <effect ent proto id> <trigger proto id> <node uid from>

cmd-xenoartifact-common-failed-to-find-effect = Failed to parse {$entProtoId} as a valid EntProtoId for xeno artifact effect
cmd-xenoartifact-common-failed-to-find-trigger = Failed to parse {$protoId} as a valid ProtoId for XenoArchTriggerPrototype
cmd-xenoartifact-common-failed-to-find-artifact = Provided uid {$uid} does not reference valid artifact entity (either does not exist or doesn't have XenoArtifactComponent)
cmd-xenoartifact-common-failed-to-find-node = Provided uid {$uid} does not reference valid artifact node entity (either does not exist or doesn't have XenoArtifactNodeComponent)
cmd-xenoartifact-common-artifact-hint = <artifact uid>
cmd-xenoartifact-common-node-hint = <node uid>
cmd-xenoartifact-common-effect-type-hint = <ent proto id for xeno artifact effect>
cmd-xenoartifact-common-trigger-type-hint = <proto id for xeno artifact trigger>
