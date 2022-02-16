using System.Collections.Generic;
using Content.Shared.CharacterInfo;
using Content.Shared.Objectives;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Content.Client.CharacterInfo.Components;

public sealed class CharacterInfoSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<CharacterInfoEvent>(OnCharacterInfoEvent);
        SubscribeLocalEvent<CharacterInfoComponent, ComponentAdd>(OnComponentAdd);
    }

    private void OnComponentAdd(EntityUid uid, CharacterInfoComponent component, ComponentAdd args)
    {
        component.Scene = component.Control = new CharacterInfoComponent.CharacterInfoControl();
    }

    public void RequestCharacterInfo(EntityUid entityUid)
    {
        RaiseNetworkEvent(new RequestCharacterInfoEvent(entityUid));
    }

    private void OnCharacterInfoEvent(CharacterInfoEvent msg, EntitySessionEventArgs args)
    {
        if (!EntityManager.TryGetComponent(msg.EntityUid, out CharacterInfoComponent characterInfoComponent))
            return;

        UpdateUI(characterInfoComponent, msg.JobTitle, msg.Objectives, msg.Briefing);
        if (EntityManager.TryGetComponent(msg.EntityUid, out ISpriteComponent? spriteComponent))
        {
            characterInfoComponent.Control.SpriteView.Sprite = spriteComponent;
        }

        if (!EntityManager.TryGetComponent(msg.EntityUid, out MetaDataComponent metadata))
            return;
        characterInfoComponent.Control.NameLabel.Text = metadata.EntityName;
    }

    private void UpdateUI(CharacterInfoComponent comp, string jobTitle, Dictionary<string, List<ConditionInfo>> objectives, string briefing)
    {
        comp.Control.SubText.Text = jobTitle;

        comp.Control.ObjectivesContainer.RemoveAllChildren();
        foreach (var (groupId, objectiveConditions) in objectives)
        {
            var vbox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                Modulate = Color.Gray
            };

            vbox.AddChild(new Label
            {
                Text = groupId,
                Modulate = Color.LightSkyBlue
            });

            foreach (var objectiveCondition in objectiveConditions)
            {
                var hbox = new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal
                };
                hbox.AddChild(new ProgressTextureRect
                {
                    Texture = objectiveCondition.SpriteSpecifier.Frame0(),
                    Progress = objectiveCondition.Progress,
                    VerticalAlignment = Control.VAlignment.Center
                });
                hbox.AddChild(new Control
                {
                    MinSize = (10,0)
                });
                hbox.AddChild(new BoxContainer
                    {
                        Orientation = BoxContainer.LayoutOrientation.Vertical,
                        Children =
                        {
                            new Label{Text = objectiveCondition.Title},
                            new Label{Text = objectiveCondition.Description}
                        }
                    }
                );
                vbox.AddChild(hbox);
            }
            var briefinghBox = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Horizontal
            };

            briefinghBox.AddChild(new Label
            {
                Text = briefing,
                Modulate = Color.Yellow
            });

            vbox.AddChild(briefinghBox);
            comp.Control.ObjectivesContainer.AddChild(vbox);
        }
    }
}
