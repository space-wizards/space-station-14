ent-BaseBorgModule = borg module
    .desc = A piece of tech that gives cyborgs new abilities.

ent-BaseProviderBorgModule = 

ent-ActionBorgSwapModule = Swap Module
    .desc = Select this module, enabling you to use the tools it provides.

ent-BaseBorgModuleCargo = { ent-BaseBorgModule }
    .desc = { ent-BaseBorgModule.desc }

ent-BaseBorgModuleEngineering = { ent-BaseBorgModule }
    .desc = { ent-BaseBorgModule.desc }

ent-BaseBorgModuleJanitor = { ent-BaseBorgModule }
    .desc = { ent-BaseBorgModule.desc }

ent-BaseBorgModuleMedical = { ent-BaseBorgModule }
    .desc = { ent-BaseBorgModule.desc }

ent-BaseBorgModuleService = { ent-BaseBorgModule }
    .desc = { ent-BaseBorgModule.desc }

ent-BorgModuleCable = cable cyborg module
    .desc = { ent-[ BaseBorgModule, BaseProviderBorgModule ].desc }

ent-BorgModuleFireExtinguisher = fire extinguisher cyborg module
    .desc = { ent-[ BaseBorgModule, BaseProviderBorgModule ].desc }

ent-BorgModuleGPS = GPS cyborg module
    .desc = { ent-[ BaseBorgModule, BaseProviderBorgModule ].desc }

ent-BorgModuleRadiationDetection = radiation detection cyborg module
    .desc = { ent-[ BaseBorgModule, BaseProviderBorgModule ].desc }

ent-BorgModuleTool = tool cyborg module
    .desc = { ent-[ BaseBorgModule, BaseProviderBorgModule ].desc }

ent-BorgModuleAppraisal = appraisal cyborg module
    .desc = { ent-[ BaseBorgModuleCargo, BaseProviderBorgModule ].desc }

ent-BorgModuleMining = mining cyborg module
    .desc = { ent-[ BaseBorgModuleCargo, BaseProviderBorgModule ].desc }

ent-BorgModuleGrapplingGun = grappling gun cyborg module
    .desc = { ent-[ BaseBorgModuleCargo, BaseProviderBorgModule ].desc }

ent-BorgModuleAdvancedTool = advanced tool cyborg module
    .desc = { ent-[ BaseBorgModuleEngineering, BaseProviderBorgModule ].desc }

ent-BorgModuleGasAnalyzer = gas analyzer cyborg module
    .desc = { ent-[ BaseBorgModuleEngineering, BaseProviderBorgModule ].desc }

ent-BorgModuleConstruction = construction cyborg module
    .desc = { ent-[ BaseBorgModuleEngineering, BaseProviderBorgModule ].desc }

ent-BorgModuleRCD = RCD cyborg module
    .desc = { ent-[ BaseBorgModuleEngineering, BaseProviderBorgModule ].desc }

ent-BorgModuleLightReplacer = light replacer cyborg module
    .desc = { ent-[ BaseBorgModuleJanitor, BaseProviderBorgModule ].desc }

ent-BorgModuleCleaning = cleaning cyborg module
    .desc = { ent-[ BaseBorgModuleJanitor, BaseProviderBorgModule ].desc }

ent-BorgModuleAdvancedCleaning = advanced cleaning cyborg module
    .desc = { ent-[ BaseBorgModuleJanitor, BaseProviderBorgModule ].desc }

ent-BorgModuleDiagnosis = diagnosis cyborg module
    .desc = { ent-[ BaseBorgModuleMedical, BaseProviderBorgModule ].desc }

ent-BorgModuleTreatment = treatment cyborg module
    .desc = { ent-[ BaseBorgModuleMedical, BaseProviderBorgModule ].desc }

ent-BorgModuleDefibrillator = defibrillator cyborg module
    .desc = { ent-[ BaseBorgModuleMedical, BaseProviderBorgModule ].desc }

ent-BorgModuleAdvancedTreatment = advanced treatment cyborg module
    .desc = { ent-[ BaseBorgModuleMedical, BaseProviderBorgModule ].desc }

ent-BorgModuleArtifact = artifact cyborg module
    .desc = { ent-[ BaseBorgModule, BaseProviderBorgModule ].desc }

ent-BorgModuleAnomaly = anomaly cyborg module
    .desc = { ent-[ BaseBorgModule, BaseProviderBorgModule ].desc }

ent-BorgModuleService = service cyborg module
    .desc = { ent-[ BaseBorgModuleService, BaseProviderBorgModule ].desc }

ent-BorgModuleMusique = musique cyborg module
    .desc = { ent-[ BaseBorgModuleService, BaseProviderBorgModule ].desc }

ent-BorgModuleGardening = gardening cyborg module
    .desc = { ent-[ BaseBorgModuleService, BaseProviderBorgModule ].desc }

ent-BorgModuleHarvesting = harvesting cyborg module
    .desc = { ent-[ BaseBorgModuleService, BaseProviderBorgModule ].desc }

ent-BorgModuleClowning = clowning cyborg module
    .desc = { ent-[ BaseBorgModuleService, BaseProviderBorgModule ].desc }

