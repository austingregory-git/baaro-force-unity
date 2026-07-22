using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using BaaroForce.ActMap;
using BaaroForce.ActMap.Content;
using BaaroForce.ActMap.Encounters;
using BaaroForce.Characters;
using BaaroForce.Classes;
using BaaroForce.GameController;
using BaaroForce.Items;
using BaaroForce.Relics;
using BaaroForce.Spells;
using BaaroForce.Utils;

namespace BaaroForce.UI
{
    /// <summary>
    /// Drives the Act 1 map screen: renders the 16-slot path from <c>PartyManager.ActRun</c>,
    /// lets the player click the current node (or pick a fork option), and resolves whichever
    /// node type that is — either by loading another scene (character select, a fight) or by
    /// showing an in-place modal (Royal Decree, Event, SideQuest, Anvil, Treasure, Village).
    /// Built entirely in code in the same chassis/rivet UI Toolkit style as the Combat HUD
    /// (see CombatHud.uss / FightResultUI) so modals reuse that vocabulary directly.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ActMapController : MonoBehaviour
    {
        private const string CharacterSelectScene = "CharacterSelectionScene";
        private const string FightScene = "MapScene";
        private const string MainMenuScene = "MainMenu";

        private static readonly string[] TierOneClassIDs = { "Warrior", "Rogue", "Mage", "Archer", "Mystic" };

        [Tooltip("CombatHud.uss — reused for the chassis/rivet/action-btn modal vocabulary.")]
        [SerializeField] private StyleSheet _combatStyleSheet;
        [Tooltip("ActMap.uss — node-spine layout and modal content tweaks.")]
        [SerializeField] private StyleSheet _actMapStyleSheet;

        private ActRunState _run;
        private VisualElement _root;
        private ScrollView _scroll;
        private VisualElement _mapPath;
        private VisualElement _currentSlotElement;
        private Label _goldLabel;

        private VisualElement _modalOverlay;
        private VisualElement _modalContent;
        private Label _modalTitle;

        private void Awake()
        {
            _run = PartyManager.Instance.ActRun;
            BuildChrome();
            RefreshGold();

            if (_run.IsActComplete) { ShowActComplete(); return; }
            if (!TryShowPendingPromotion())
                RenderMap();
        }

        // ================================================================ //
        // Chrome: root layout, gold readout, modal shell                     //
        // ================================================================ //

        private void BuildChrome()
        {
            UIDocument doc = GetComponent<UIDocument>();
            _root = doc.rootVisualElement;
            if (_combatStyleSheet != null) _root.styleSheets.Add(_combatStyleSheet);
            if (_actMapStyleSheet != null) _root.styleSheets.Add(_actMapStyleSheet);

            var mapRoot = new VisualElement();
            mapRoot.AddToClassList("act-map-root");
            _root.Add(mapRoot);

            var header = new VisualElement();
            header.AddToClassList("act-map-header");
            mapRoot.Add(header);

            var title = new Label("Act 1");
            title.AddToClassList("act-map-title");
            header.Add(title);

            var subtitle = new Label("Choose your path.");
            subtitle.AddToClassList("act-map-subtitle");
            header.Add(subtitle);

            _goldLabel = new Label();
            _goldLabel.AddToClassList("act-map-gold");
            mapRoot.Add(_goldLabel);

            // Horizontal: the path reads left-to-right, earliest node first.
            _scroll = new ScrollView(ScrollViewMode.Horizontal);
            _scroll.AddToClassList("act-map-scroll");
            mapRoot.Add(_scroll);

            _mapPath = new VisualElement();
            _mapPath.AddToClassList("act-map-path");
            _scroll.Add(_mapPath);

            BuildModalShell();
        }

        private void BuildModalShell()
        {
            _modalOverlay = new VisualElement();
            _modalOverlay.AddToClassList("fight-result-overlay");
            _modalOverlay.style.display = DisplayStyle.None;
            _root.Add(_modalOverlay);

            var chassis = new VisualElement();
            chassis.AddToClassList("chassis");
            chassis.AddToClassList("fight-result-chassis");
            chassis.AddToClassList("act-modal-wide");
            _modalOverlay.Add(chassis);

            chassis.Add(MakeRivet("rivet-tl"));
            chassis.Add(MakeRivet("rivet-tr"));
            chassis.Add(MakeRivet("rivet-bl"));
            chassis.Add(MakeRivet("rivet-br"));

            _modalTitle = new Label();
            _modalTitle.AddToClassList("fight-result-title");
            _modalTitle.AddToClassList("fight-result-title-win");
            chassis.Add(_modalTitle);

            _modalContent = new VisualElement();
            chassis.Add(_modalContent);
        }

        private static VisualElement MakeRivet(string variantClass)
        {
            var rivet = new VisualElement();
            rivet.AddToClassList("rivet");
            rivet.AddToClassList(variantClass);
            return rivet;
        }

        private static Button MakeActionButton(string label, Action onClick)
        {
            var btn = new Button(onClick) { text = label };
            btn.AddToClassList("action-btn");
            btn.AddToClassList("fight-result-btn");
            return btn;
        }

        private void RefreshGold() => _goldLabel.text = $"{PartyManager.Instance.Party.Gold} Gold";

        private void OpenModal(string title)
        {
            _modalTitle.text = title;
            _modalContent.Clear();
            _modalOverlay.style.display = DisplayStyle.Flex;
        }

        private void CloseModal() => _modalOverlay.style.display = DisplayStyle.None;

        /// <summary>Marks the current node resolved, closes whatever modal was open, and
        /// either surfaces the next pending promotion or re-renders the map.</summary>
        private void CompleteAndRefresh()
        {
            _run.CompleteCurrentNode();
            CloseModal();
            RefreshGold();

            if (_run.IsActComplete) { ShowActComplete(); return; }
            if (!TryShowPendingPromotion())
                RenderMap();
        }

        private void ShowActComplete()
        {
            OpenModal("Act 1 Complete!");
            var label = new Label(
                "Your party has defeated the Act 1 boss. Later acts aren't built yet — thanks for playing this far!");
            label.AddToClassList("act-modal-description");
            _modalContent.Add(label);
            _modalContent.Add(MakeActionButton("Return to Main Menu", () =>
            {
                PartyManager.Instance.ResetForNewRun();
                SceneManager.LoadScene(MainMenuScene);
            }));
        }

        private static Realm RunRealm() => PartyManager.Instance.CurrentRealm ?? Realm.Earth;

        // ================================================================ //
        // Map rendering                                                       //
        // ================================================================ //

        private void RenderMap()
        {
            _mapPath.Clear();
            _currentSlotElement = null;

            for (int i = 0; i < _run.Map.Slots.Count; i++)
            {
                _mapPath.Add(BuildSlotColumn(i));
                if (i < _run.Map.Slots.Count - 1)
                {
                    var connector = new VisualElement();
                    connector.AddToClassList("act-map-connector");
                    _mapPath.Add(connector);
                }
            }

            _mapPath.schedule.Execute(() =>
            {
                if (_currentSlotElement != null) _scroll.ScrollTo(_currentSlotElement);
            }).ExecuteLater(30);
        }

        private VisualElement BuildSlotColumn(int slotIndex)
        {
            ActMapSlot slot = _run.Map.Slots[slotIndex];
            var column = new VisualElement();
            column.AddToClassList("act-map-slot");

            // A fork always shows both branch options stacked (top/bottom) — the map keeps
            // its shape even after a choice is made; BuildNodeWrap is what stops the
            // unchosen option from being clickable once the fork is decided.
            if (slot.IsFork)
            {
                column.Add(BuildNodeWrap(slot, slotIndex, 0));
                var gap = new Label("OR");
                gap.AddToClassList("act-fork-gap");
                column.Add(gap);
                column.Add(BuildNodeWrap(slot, slotIndex, 1));
            }
            else
            {
                column.Add(BuildNodeWrap(slot, slotIndex, 0));
            }

            if (slotIndex == _run.CurrentSlotIndex) _currentSlotElement = column;
            return column;
        }

        private VisualElement BuildNodeWrap(ActMapSlot slot, int slotIndex, int optionIndex)
        {
            ActMapNode node = slot.Options[optionIndex];

            var wrap = new VisualElement();
            wrap.AddToClassList("act-node-wrap");

            var badge = new VisualElement();
            badge.AddToClassList("act-node");
            if (node.Type == ActNodeType.Elite) badge.AddToClassList("act-node-elite");
            if (node.Type == ActNodeType.Boss) badge.AddToClassList("act-node-boss");

            Sprite icon = Resources.Load<Sprite>(IconFor(node.Type));
            if (icon != null) badge.style.backgroundImage = new StyleBackground(icon);

            // isTheChosenOption: true for a non-fork slot's only option, or the specific
            // fork option that was (or already auto-was) chosen. False for -1 (undecided).
            bool isTheChosenOption = !slot.IsFork || slot.ChosenOptionIndex == optionIndex;
            // isOpenForkChoice: a genuine still-undecided fork — every option is live.
            bool isOpenForkChoice = slot.IsFork && slot.ChosenOptionIndex == -1;
            bool isPast = slotIndex < _run.CurrentSlotIndex;
            bool isCurrentSlot = slotIndex == _run.CurrentSlotIndex;

            if (isPast)
            {
                badge.AddToClassList(isTheChosenOption ? "act-node-completed" : "act-node-locked");
            }
            else if (isCurrentSlot && (isOpenForkChoice || isTheChosenOption))
            {
                // Either a genuine open fork choice (both options are live) or this specific
                // option is the branch already locked in (a fork continuation auto-resolves
                // its branch before this ever renders — see ActRunState.CompleteCurrentNode).
                // Either way this is a node the player can act on right now.
                badge.AddToClassList("act-node-current");
                int capturedOption = optionIndex;
                badge.RegisterCallback<ClickEvent>(_ => OnNodeClicked(slotIndex, capturedOption));
            }
            else
            {
                // Current slot, but this option isn't the branch already locked in — show
                // it locked rather than current so the player can't click into the road not taken.
                badge.AddToClassList("act-node-locked");
            }

            wrap.Add(badge);

            var label = new Label(LabelFor(node.Type));
            label.AddToClassList("act-node-label");
            wrap.Add(label);

            return wrap;
        }

        private static string LabelFor(ActNodeType type)
        {
            switch (type)
            {
                case ActNodeType.CharacterSelect: return "Recruit";
                case ActNodeType.RoyalDecree:      return "Royal Decree";
                case ActNodeType.Fight:            return "Fight";
                case ActNodeType.Elite:            return "Elite Fight";
                case ActNodeType.Boss:             return "Boss Fight";
                case ActNodeType.Event:            return "Event";
                case ActNodeType.SideQuest:        return "Side Quest";
                case ActNodeType.Anvil:            return "Anvil";
                case ActNodeType.Treasure:         return "Treasure";
                case ActNodeType.Village:          return "Village";
                default:                           return type.ToString();
            }
        }

        private static string IconFor(ActNodeType type)
        {
            switch (type)
            {
                case ActNodeType.CharacterSelect: return "node_characterselect_128x128";
                case ActNodeType.RoyalDecree:      return "node_decree_128x128";
                case ActNodeType.Fight:            return "node_fight_128x128";
                case ActNodeType.Elite:            return "node_elite_128x128";
                case ActNodeType.Boss:             return "node_boss_128x128";
                case ActNodeType.Event:            return "node_event_128x128";
                case ActNodeType.SideQuest:        return "node_sidequest_128x128";
                case ActNodeType.Anvil:            return "node_anvil_128x128";
                case ActNodeType.Treasure:         return "node_treasure_128x128";
                case ActNodeType.Village:          return "node_village_128x128";
                default:                           return "node_fight_128x128";
            }
        }

        // ================================================================ //
        // Node resolution dispatch                                            //
        // ================================================================ //

        private void OnNodeClicked(int slotIndex, int optionIndex)
        {
            if (slotIndex != _run.CurrentSlotIndex) return;

            ActMapSlot slot = _run.Map.Slots[slotIndex];
            if (slot.IsFork) _run.ChooseForkOption(optionIndex);
            ResolveCurrentNode();
        }

        private void ResolveCurrentNode()
        {
            ActMapNode node = _run.CurrentNode;
            if (node == null) return;

            switch (node.Type)
            {
                case ActNodeType.CharacterSelect: SceneManager.LoadScene(CharacterSelectScene); break;
                case ActNodeType.Fight:
                case ActNodeType.Elite:
                case ActNodeType.Boss:            GoToFight(node); break;
                case ActNodeType.RoyalDecree:      ShowRoyalDecree(); break;
                case ActNodeType.Event:            ShowChoiceContent(ActEventRegistry.GetRandom(RunRealm())); break;
                case ActNodeType.SideQuest:        ShowChoiceContent(ActSideQuestRegistry.GetRandom(RunRealm())); break;
                case ActNodeType.Anvil:            ShowAnvil(); break;
                case ActNodeType.Treasure:         ShowTreasure(node); break;
                case ActNodeType.Village:          ShowVillage(); break;
            }
        }

        /// <summary>Builds the pending encounter and loads MapScene. CompleteCurrentNode is
        /// NOT called here — the node isn't done until TurnManager.OnMoveOn fires on victory.</summary>
        private void GoToFight(ActMapNode node)
        {
            Realm realm = RunRealm();
            EncounterPoolTier tier = node.Tier ?? EncounterPoolTier.Normal1;
            Encounter encounter = EncounterRegistry.GetRandom(realm, tier);

            if (_run.RestBonusHealth > 0)
            {
                foreach (Character member in PartyManager.Instance.Party.Members)
                {
                    member.CharacterStats.MaxHealthPoints += _run.RestBonusHealth;
                    member.CharacterStats.HealthPoints    += _run.RestBonusHealth;
                }
                _run.RestBonusHealth = 0;
            }

            _run.PendingEncounter = new PendingEncounter
            {
                Realm = realm,
                Tier = tier,
                MapSize = encounter.GridSize,
                EnemyLevel = node.EnemyLevel,
                Enemies = encounter.Enemies,
            };
            SceneManager.LoadScene(FightScene);
        }

        // ================================================================ //
        // Royal Decree                                                        //
        // ================================================================ //

        private void ShowRoyalDecree()
        {
            OpenModal("Royal Decree");
            var desc = new Label("The king offers a boon at the start of the act. Choose one:");
            desc.AddToClassList("act-modal-description");
            _modalContent.Add(desc);

            foreach (RoyalDecreeOptionType option in RoyalDecree.GetRandomThree())
            {
                var row = new VisualElement();
                row.AddToClassList("act-choice-row");
                var label = new Label(RoyalDecree.GetLabel(option));
                label.AddToClassList("act-choice-label");
                row.Add(label);
                var sub = new Label(RoyalDecree.GetDescription(option));
                sub.AddToClassList("act-choice-sub");
                row.Add(sub);
                row.RegisterCallback<ClickEvent>(_ => ResolveDecreeOption(option));
                _modalContent.Add(row);
            }
        }

        private void ResolveDecreeOption(RoyalDecreeOptionType option)
        {
            switch (option)
            {
                case RoyalDecreeOptionType.Gold:
                    PartyManager.Instance.Party.AddGold(100);
                    CompleteAndRefresh();
                    break;
                case RoyalDecreeOptionType.RandomRelic:
                    PartyManager.Instance.Relics.Add(RelicRegistry.GetRandom(Rarity.Common));
                    CompleteAndRefresh();
                    break;
                case RoyalDecreeOptionType.TwoCommonEquipment:
                    ActChoiceEffects.GrantEquipmentToRandomMember(PartyManager.Instance, Rarity.Common);
                    ActChoiceEffects.GrantEquipmentToRandomMember(PartyManager.Instance, Rarity.Common);
                    CompleteAndRefresh();
                    break;
                case RoyalDecreeOptionType.ChooseWeapon:
                    ShowWeaponChoice();
                    break;
                case RoyalDecreeOptionType.LearnTierOneSpell:
                    ShowMemberPicker("Choose who learns a new spell", ShowSpellChoiceFor);
                    break;
            }
        }

        private void ShowWeaponChoice()
        {
            ShowMemberPicker("Choose who receives a weapon", member =>
            {
                OpenModal("Choose a Weapon");
                for (int i = 0; i < 3; i++)
                {
                    Equipment weapon = EquipmentRegistry.GetRandomOfSlot(Rarity.Common, EquipmentSlotType.Weapon);
                    var row = new VisualElement();
                    row.AddToClassList("act-choice-row");
                    var label = new Label(weapon.Name);
                    label.AddToClassList("act-choice-label");
                    row.Add(label);
                    var sub = new Label(weapon.Description);
                    sub.AddToClassList("act-choice-sub");
                    row.Add(sub);
                    row.RegisterCallback<ClickEvent>(_ =>
                    {
                        member.AddEquipment(weapon);
                        CompleteAndRefresh();
                    });
                    _modalContent.Add(row);
                }
            });
        }

        private void ShowSpellChoiceFor(Character member)
        {
            OpenModal($"Choose a Spell for {member.CharacterName}");
            for (int i = 0; i < 3; i++)
            {
                string classID = TierOneClassIDs[UnityEngine.Random.Range(0, TierOneClassIDs.Length)];
                ClassSpell spell = SpellRegistry.GetRandomClassSpell(classID);
                if (spell == null) continue;

                var row = new VisualElement();
                row.AddToClassList("act-choice-row");
                var label = new Label(spell.Name);
                label.AddToClassList("act-choice-label");
                row.Add(label);
                var sub = new Label(spell.GetSummary(member));
                sub.AddToClassList("act-choice-sub");
                row.Add(sub);
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    member.CharacterSpells.Add(spell);
                    CompleteAndRefresh();
                });
                _modalContent.Add(row);
            }
        }

        private void ShowMemberPicker(string title, Action<Character> onPicked)
        {
            OpenModal(title);
            foreach (Character member in PartyManager.Instance.Party.Members)
            {
                var row = new VisualElement();
                row.AddToClassList("act-choice-row");
                var label = new Label($"{member.CharacterName} (Lv {member.Level} {member.CharacterClass?.ClassID})");
                label.AddToClassList("act-choice-label");
                row.Add(label);
                row.RegisterCallback<ClickEvent>(_ => onPicked(member));
                _modalContent.Add(row);
            }
        }

        // ================================================================ //
        // Event / SideQuest                                                   //
        // ================================================================ //

        private void ShowChoiceContent(ActChoiceContent content)
        {
            if (content == null)
            {
                // No authored content for this realm yet — grant a small consolation
                // reward rather than blocking progress.
                PartyManager.Instance.Party.AddGold(20);
                CompleteAndRefresh();
                return;
            }

            OpenModal(content.Title);
            var desc = new Label(content.Description);
            desc.AddToClassList("act-modal-description");
            _modalContent.Add(desc);

            foreach (ActChoiceOption choice in content.Choices)
            {
                var row = new VisualElement();
                row.AddToClassList("act-choice-row");
                var label = new Label(choice.Label);
                label.AddToClassList("act-choice-label");
                row.Add(label);
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    choice.Apply?.Invoke(PartyManager.Instance);
                    ShowChoiceResult(content.Title, choice.ResultText);
                });
                _modalContent.Add(row);
            }
        }

        private void ShowChoiceResult(string title, string resultText)
        {
            OpenModal(title);
            var label = new Label(resultText);
            label.AddToClassList("act-modal-description");
            _modalContent.Add(label);
            _modalContent.Add(MakeActionButton("Continue", CompleteAndRefresh));
        }

        // ================================================================ //
        // Anvil                                                               //
        // ================================================================ //

        private void ShowAnvil()
        {
            OpenModal("Anvil");
            var desc = new Label("Upgrade a piece of equipment to its enhanced form.");
            desc.AddToClassList("act-modal-description");
            _modalContent.Add(desc);

            bool any = false;
            foreach (Character member in PartyManager.Instance.Party.Members)
                foreach (Equipment item in member.CharacterEquipment)
                {
                    if (item.IsUpgraded) continue;
                    any = true;

                    var row = new VisualElement();
                    row.AddToClassList("act-choice-row");
                    var label = new Label($"{item.Name} ({member.CharacterName})");
                    label.AddToClassList("act-choice-label");
                    row.Add(label);
                    row.RegisterCallback<ClickEvent>(_ =>
                    {
                        member.UpgradeEquipment(item);
                        CompleteAndRefresh();
                    });
                    _modalContent.Add(row);
                }

            if (!any)
            {
                var none = new Label("Your party carries nothing that can be upgraded yet.");
                none.AddToClassList("act-modal-description");
                _modalContent.Add(none);
                _modalContent.Add(MakeActionButton("Continue", CompleteAndRefresh));
            }
        }

        // ================================================================ //
        // Treasure                                                            //
        // ================================================================ //

        private void ShowTreasure(ActMapNode node)
        {
            // Per the design doc: +5% uncommon and +0.5% rare chance per node index into the act.
            float uncommonChance = Mathf.Clamp01(node.Index * 0.05f);
            float rareChance = Mathf.Clamp01(node.Index * 0.005f);

            Rarity rarity = Rarity.Common;
            float roll = UnityEngine.Random.value;
            if (roll < rareChance) rarity = Rarity.Rare;
            else if (roll < rareChance + uncommonChance) rarity = Rarity.Uncommon;

            Equipment reward = EquipmentRegistry.GetRandom(rarity);
            List<Character> members = PartyManager.Instance.Party.Members;
            if (members.Count > 0)
                members[UnityEngine.Random.Range(0, members.Count)].AddEquipment(reward);

            OpenModal("Treasure");
            var label = new Label($"You found: {reward.Name} ({reward.Rarity})");
            label.AddToClassList("act-modal-description");
            _modalContent.Add(label);
            _modalContent.Add(MakeActionButton("Continue", CompleteAndRefresh));
        }

        // ================================================================ //
        // Village: Shop / Smith / Recruit / Rest                             //
        // ================================================================ //

        private void ShowVillage()
        {
            OpenModal("Village");

            var tabRow = new VisualElement();
            tabRow.AddToClassList("act-tab-row");
            _modalContent.Add(tabRow);

            var body = new VisualElement();
            _modalContent.Add(body);

            Button shopBtn = null, smithBtn = null, recruitBtn = null, restBtn = null;
            Button[] allTabs = null;

            void Activate(Button active, Action<VisualElement> render)
            {
                foreach (Button b in allTabs) b.RemoveFromClassList("act-tab-btn-active");
                active.AddToClassList("act-tab-btn-active");
                render(body);
            }

            shopBtn    = MakeActionButton("Shop",    () => Activate(shopBtn, RenderShop));
            smithBtn   = MakeActionButton("Smith",   () => Activate(smithBtn, RenderSmith));
            recruitBtn = MakeActionButton("Recruit", () => Activate(recruitBtn, RenderRecruit));
            restBtn    = MakeActionButton("Rest",    () => Activate(restBtn, RenderRest));
            allTabs = new[] { shopBtn, smithBtn, recruitBtn, restBtn };
            foreach (Button b in allTabs)
            {
                b.AddToClassList("act-tab-btn");
                tabRow.Add(b);
            }

            _modalContent.Add(MakeActionButton("Leave Village", CompleteAndRefresh));

            Activate(shopBtn, RenderShop);
        }

        private void RenderShop(VisualElement body)
        {
            body.Clear();
            var desc = new Label("Spend gold on equipment and potions.");
            desc.AddToClassList("act-modal-description");
            body.Add(desc);

            AddShopEntry(body, "Common Equipment", 30,
                () => ActChoiceEffects.GrantEquipmentToRandomMember(PartyManager.Instance, Rarity.Common));
            AddShopEntry(body, "Uncommon Equipment", 60,
                () => ActChoiceEffects.GrantEquipmentToRandomMember(PartyManager.Instance, Rarity.Uncommon));
            AddShopEntry(body, "Common Potion", 20,
                () => ActChoiceEffects.GrantPotion(PartyManager.Instance, Rarity.Common));
            AddShopEntry(body, "Uncommon Potion", 40,
                () => ActChoiceEffects.GrantPotion(PartyManager.Instance, Rarity.Uncommon));
        }

        private void AddShopEntry(VisualElement body, string label, int cost, Action grant)
        {
            var row = new VisualElement();
            row.AddToClassList("act-choice-row");
            var l = new Label($"{label} — {cost} Gold");
            l.AddToClassList("act-choice-label");
            row.Add(l);
            row.RegisterCallback<ClickEvent>(_ =>
            {
                if (!PartyManager.Instance.Party.SpendGold(cost)) return;
                grant();
                RefreshGold();
            });
            body.Add(row);
        }

        private const int SmithCost = 40;

        private void RenderSmith(VisualElement body)
        {
            body.Clear();
            var desc = new Label($"Upgrade a piece of equipment for {SmithCost} gold.");
            desc.AddToClassList("act-modal-description");
            body.Add(desc);

            bool any = false;
            foreach (Character member in PartyManager.Instance.Party.Members)
                foreach (Equipment item in member.CharacterEquipment)
                {
                    if (item.IsUpgraded) continue;
                    any = true;

                    var row = new VisualElement();
                    row.AddToClassList("act-choice-row");
                    var label = new Label($"{item.Name} ({member.CharacterName}) — {SmithCost} Gold");
                    label.AddToClassList("act-choice-label");
                    row.Add(label);
                    row.RegisterCallback<ClickEvent>(_ =>
                    {
                        if (!PartyManager.Instance.Party.SpendGold(SmithCost)) return;
                        member.UpgradeEquipment(item);
                        RefreshGold();
                        RenderSmith(body);
                    });
                    body.Add(row);
                }

            if (!any)
            {
                var none = new Label("Nothing to upgrade yet.");
                none.AddToClassList("act-modal-description");
                body.Add(none);
            }
        }

        private const int RecruitCost = 80;

        private void RenderRecruit(VisualElement body)
        {
            body.Clear();
            var desc = new Label($"Hire a new party member for {RecruitCost} gold.");
            desc.AddToClassList("act-modal-description");
            body.Add(desc);

            var party = PartyManager.Instance.Party;
            if (party.Members.Count >= party.MaximumPartySize)
            {
                var full = new Label("Your party is full.");
                full.AddToClassList("act-modal-description");
                body.Add(full);
                return;
            }

            foreach (Character candidate in CharacterUtils.GetRandomCharacters(3, RunRealm()))
            {
                var row = new VisualElement();
                row.AddToClassList("act-choice-row");
                var label = new Label($"{candidate.CharacterName} ({candidate.CharacterClass?.ClassID}) — {RecruitCost} Gold");
                label.AddToClassList("act-choice-label");
                row.Add(label);
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    if (!PartyManager.Instance.Party.SpendGold(RecruitCost)) return;
                    PartyManager.Instance.AddMember(candidate);
                    RefreshGold();
                    RenderRecruit(body);
                });
                body.Add(row);
            }
        }

        private const int RestBonus = 5;

        private void RenderRest(VisualElement body)
        {
            body.Clear();
            var desc = new Label($"Rest here and gain +{RestBonus} max health for your next fight.");
            desc.AddToClassList("act-modal-description");
            body.Add(desc);

            if (_run.RestBonusHealth > 0)
            {
                var already = new Label("Your party is already well-rested.");
                already.AddToClassList("act-modal-description");
                body.Add(already);
                return;
            }

            body.Add(MakeActionButton("Rest", () =>
            {
                _run.RestBonusHealth = RestBonus;
                RenderRest(body);
            }));
        }

        // ================================================================ //
        // Level-5 class promotion                                            //
        // ================================================================ //

        /// <summary>Shows a promotion-choice modal for the first party member with a pending
        /// promotion, if any. Returns true if a modal was shown (caller should not also
        /// render the map underneath it).</summary>
        private bool TryShowPendingPromotion()
        {
            Character member = PartyManager.Instance.Party.Members.Find(c => c.HasPendingPromotion);
            if (member == null) return false;

            List<string> pool = new List<string>(ClassTree.GetPromotions(member.CharacterClass?.ClassID));
            List<string> options = new List<string>();
            for (int i = 0; i < 3 && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                options.Add(pool[index]);
                pool.RemoveAt(index);
            }

            if (options.Count == 0)
            {
                Debug.LogWarning($"[ActMapController] '{member.CharacterName}' has no promotion " +
                                  $"targets registered for class '{member.CharacterClass?.ClassID}'.");
                return false;
            }

            OpenModal($"{member.CharacterName} Reaches Level 5!");
            var desc = new Label("Choose a class promotion:");
            desc.AddToClassList("act-modal-description");
            _modalContent.Add(desc);

            foreach (string target in options)
            {
                var row = new VisualElement();
                row.AddToClassList("act-choice-row");
                var label = new Label(target);
                label.AddToClassList("act-choice-label");
                row.Add(label);
                row.RegisterCallback<ClickEvent>(_ =>
                {
                    member.Promote(target);
                    CloseModal();
                    if (!TryShowPendingPromotion())
                        RenderMap();
                });
                _modalContent.Add(row);
            }
            return true;
        }
    }
}
