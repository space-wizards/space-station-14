cmd-substepatmos-desc = Substeps the atmosphere simulation by a single atmostick for the provided grid entity. Implicitly pauses atmospherics simulation.
cmd-substepatmos-help = Usage: {$command} <EntityUid>

cmd-error-no-grid-provided-or-invalid-grid = You must either provide a grid entity or be standing on a grid to substep.
cmd-error-couldnt-parse-entity = Entity provided could not be parsed or does not exist. Try standing on a grid you want to substep.
cmd-error-no-gridatmosphere = Entity provided doesn't have a GridAtmosphereComponent.
cmd-error-no-gastileoverlay = Entity provided doesn't have a GasTileOverlayComponent.
cmd-error-no-mapgrid = Entity provided doesn't have a MapGridComponent.
cmd-error-no-xform = Entity provided doesn't have a TransformComponent?
cmd-error-no-valid-map = The grid provided is not on a valid map?

cmd-substepatmos-info-implicitly-paused-simulation = Implicitly paused atmospherics simulation on {$grid}.
cmd-substepatmos-info-substepped-grid = Substepped atmospherics simulation by one atmostick on {$grid}.

cmd-substepatmos-completion-grid-substep = EntityUid of the grid you want to substep. Automatically uses the grid you're standing on if empty.
