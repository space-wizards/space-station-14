﻿using System;
 using System.Collections.Generic;
 using Content.Shared.Maps;
 using Robust.Shared.ContentPack;
 using Robust.Shared.Interfaces.Map;
 using Robust.Shared.IoC;
 using Robust.Shared.Prototypes;

 namespace Content.Shared
{
    public class EntryPoint : GameShared
    {
#pragma warning disable 649
        [Dependency] private readonly IPrototypeManager _prototypeManager;
        [Dependency] private readonly ITileDefinitionManager _tileDefinitionManager;
#pragma warning restore 649

        public override void Init()
        {
            IoCManager.InjectDependencies(this);
        }

        public override void PostInit()
        {
            base.PostInit();


            _initTileDefinitions();
        }

        private void _initTileDefinitions()
        {
            // Register space first because I'm a hard coding hack.
            var spaceDef = _prototypeManager.Index<ContentTileDefinition>("space");

            _tileDefinitionManager.Register(spaceDef);

            var prototypeList = new List<ContentTileDefinition>();
            foreach (var tileDef in _prototypeManager.EnumeratePrototypes<ContentTileDefinition>())
            {
                if (tileDef.Name == "space")
                {
                    continue;
                }
                prototypeList.Add(tileDef);
            }

            // Sort ordinal to ensure it's consistent client and server.
            // So that tile IDs match up.
            prototypeList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            foreach (var tileDef in prototypeList)
            {
                _tileDefinitionManager.Register(tileDef);
            }

            _tileDefinitionManager.Initialize();
        }
    }
}
