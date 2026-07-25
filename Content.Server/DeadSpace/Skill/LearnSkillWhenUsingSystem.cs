// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using Content.Server.DeadSpace.Languages;
using Content.Server.DeadSpace.Skill.Components;
using Content.Server.Inventory;
using Content.Server.Popups;
using Content.Shared.DeadSpace.Skills.Events;
using Content.Shared.DoAfter;
using Content.Shared.Interaction.Events;
using Content.Shared.Nutrition.EntitySystems;
using Robust.Server.Audio;
using Content.Shared.Hands.EntitySystems;

namespace Content.Server.DeadSpace.Skill;

public sealed class LearnSkillWhenUsingSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SkillSystem _skillSystem = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly LanguageSystem _languageSystem = default!;
	[Dependency] private readonly SharedHandsSystem _hands = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LearnSkillWhenUsingComponent, UseInHandEvent>(OnUseInHand, after: new[] { typeof(OpenableSystem), typeof(ServerInventorySystem) });
        SubscribeLocalEvent<LearnSkillWhenUsingComponent, LearnDoAfterEvent>(OnDoAfter);
    }

    private void OnUseInHand(EntityUid uid, LearnSkillWhenUsingComponent component, UseInHandEvent args)
    {
        if (args.Handled)
            return;

        if (component.LanguagesWhitelist != null &&
            component.LanguagesWhitelist.Count > 0)
        {
            bool knowsAtLeastOne = false;
            foreach (var lang in component.LanguagesWhitelist)
            {
                if (_languageSystem.KnowsLanguage(args.User, lang))
                {
                    knowsAtLeastOne = true;
                    break;
                }
            }

            if (!knowsAtLeastOne)
            {   
                _popup.PopupEntity(Loc.GetString("skill-canlearn-language-missing"), args.User, args.User);
                return;
            }
        }

        var doAfterArgs = new DoAfterArgs(EntityManager,
            args.User,
            TimeSpan.FromSeconds(component.Duration),
            new LearnDoAfterEvent(),
            eventTarget: uid,
            target: args.User,
            used: uid)
        {
            BreakOnHandChange = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            MovementThreshold = 0.01f,
            DistanceThreshold = 1f,
            DuplicateCondition = DuplicateConditions.SameEvent,
            BlockDuplicate = true,
            CancelDuplicate = false
        };

        int unknown = 0;

        foreach (var skill in component.Skills)
        {
            if (_skillSystem.CanLearn(args.User, skill))
                unknown += 1;
        }

        if (unknown > 0)
        {
            if (!_doAfter.TryStartDoAfter(doAfterArgs))
                _popup.PopupEntity(Loc.GetString("skill-canlearn-already-learning"), args.User, args.User);
        }
        else
        {
            return;
        }

        if (component.Sound != null)
            _audio.PlayPvs(component.Sound, uid);
    }

    private void OnDoAfter(EntityUid uid, LearnSkillWhenUsingComponent component, LearnDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled || !Exists(args.Used))
            return;
		
		 if (!_hands.IsHolding(args.User, uid))
            return;
		
        foreach (var skill in component.Skills)
        {
            _skillSystem.AddSkillProgress(args.User, skill, component.Points[skill]);
        }

        if (component.Sound != null)
            _audio.PlayPvs(component.Sound, uid);

        args.Repeat = true;
    }

}
