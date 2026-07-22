using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using BaaroForce.Characters;
using BaaroForce.Utils;

namespace BaaroForce.UI
{
    /// <summary>
    /// Full-screen reveal screen shown after a won fight (before returning to the Act Map)
    /// when any party member has queued <see cref="Character.PendingLevelUpEvents"/>. Shows
    /// every party member's card and animates each level-up point being allocated — bars
    /// growing, numbers ticking up — rather than the change having happened invisibly. Built
    /// in the same chassis/rivet code-only style as FightResultUI, reusing CombatHud.uss's
    /// existing segmented-bar/portrait/level-badge classes.
    /// </summary>
    public class LevelUpUI : MonoBehaviour
    {
        private const float PointRevealDelay = 0.8f;
        private const float TalentRevealDelay = 0.5f;
        private const float PopDuration = 0.3f;

        // A "breath" is one slow ease-up/ease-down cycle; newly revealed stats glow and
        // swell through a few of these before settling back to their resting state.
        private const float BreathPeriod = 0.9f;
        private const float BreathCycles = 3f;
        private const float BarBreathScalePeak = 1.16f;
        private const float LabelBreathScalePeak = 1.1f;

        private static readonly Color HpFillColor   = new Color32(227, 91, 107, 255);  // matches .seg-fill-hp
        private static readonly Color ManaFillColor = new Color32(201, 163, 90, 255);  // matches --glow-color on .level-up-card
        private static readonly Color StatNumColor  = new Color32(243, 230, 200, 255); // matches .level-up-stat-num
        private static readonly Color BreathColor   = new Color32(140, 255, 150, 255); // the "just changed" highlight green
        private static readonly Color BadgeTextColor = new Color32(20, 16, 24, 255);   // dark text for contrast against the lit badge

        // Bouncy overshoot-undershoot-settle pop — same shape as CharacterCardHandler's
        // card-hover BounceCurve. Used only for the level badge's quick "level up!" beat;
        // the ongoing per-stat highlight below uses the slower breathing effect instead.
        private static readonly AnimationCurve PopCurve = new AnimationCurve(
            new Keyframe(0.00f, 0.00f, 0f,  4f),
            new Keyframe(0.40f, 1.20f, 2f, -3f),
            new Keyframe(0.70f, 0.90f, 1f,  2f),
            new Keyframe(1.00f, 1.00f, 0f,  0f));

        private VisualElement _overlay;
        private VisualElement _cardsRow;
        private Action _onContinue;
        private readonly List<CardData> _cards = new List<CardData>();

        private class CardData
        {
            public Character Character;
            public VisualElement HpBar;
            public Label HpNum;
            public VisualElement ManaBar;
            public Label ManaNum;
            public VisualElement AttackBadge;
            public Label AttackNum;
            public Label LevelLabel;
            public Label TalentLabel;
            public int MaxHp;
            public int Hp;
            public int MaxMana;
            public int Mana;
            public int Attack;
        }

        private void Awake()     => BuildShell();
        private void OnDestroy() => _overlay?.RemoveFromHierarchy();

        // ── Construction ─────────────────────────────────────────────────────

        private void BuildShell()
        {
            UIDocument doc = FindAnyObjectByType<UIDocument>();
            if (doc == null)
            {
                Debug.LogWarning("[LevelUpUI] No UIDocument found in scene.");
                return;
            }

            _overlay = new VisualElement();
            _overlay.AddToClassList("fight-result-overlay");
            _overlay.style.display = DisplayStyle.None;

            var chassis = new VisualElement();
            chassis.AddToClassList("chassis");
            chassis.AddToClassList("level-up-chassis");
            _overlay.Add(chassis);

            chassis.Add(MakeRivet("rivet-tl"));
            chassis.Add(MakeRivet("rivet-tr"));
            chassis.Add(MakeRivet("rivet-bl"));
            chassis.Add(MakeRivet("rivet-br"));

            var title = new Label("Level Up!");
            title.AddToClassList("fight-result-title");
            title.AddToClassList("fight-result-title-win");
            chassis.Add(title);

            _cardsRow = new VisualElement();
            _cardsRow.AddToClassList("level-up-cards-row");
            chassis.Add(_cardsRow);

            var continueButton = new Button(OnContinueClicked) { text = "Continue" };
            continueButton.AddToClassList("action-btn");
            continueButton.AddToClassList("fight-result-btn");
            chassis.Add(continueButton);

            doc.rootVisualElement.Add(_overlay);
        }

        private static VisualElement MakeRivet(string variantClass)
        {
            var rivet = new VisualElement();
            rivet.AddToClassList("rivet");
            rivet.AddToClassList(variantClass);
            return rivet;
        }

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Shows a card for every member of <paramref name="members"/> and animates a reveal
        /// for any of them with pending level-up events. If nobody has any, skips straight to
        /// <paramref name="onContinue"/> without showing anything.
        /// </summary>
        public void Show(List<Character> members, Action onContinue)
        {
            _onContinue = onContinue;

            if (_overlay == null || members.Find(c => c.PendingLevelUpEvents.Count > 0) == null)
            {
                onContinue?.Invoke();
                return;
            }

            _cardsRow.Clear();
            _cards.Clear();

            foreach (Character member in members)
                _cards.Add(BuildCard(member));

            _overlay.style.display = DisplayStyle.Flex;

            foreach (CardData card in _cards)
                if (card.Character.PendingLevelUpEvents.Count > 0)
                    StartCoroutine(RevealCard(card));
        }

        // ── Card construction ────────────────────────────────────────────────

        private CardData BuildCard(Character character)
        {
            int hpGain = 0, manaGain = 0, atkGain = 0;
            int levelsGained = character.PendingLevelUpEvents.Count;
            foreach (LevelUpEvent evt in character.PendingLevelUpEvents)
                foreach (StatPointGain gain in evt.StatGains)
                {
                    if (gain.Stat == LevelUpStat.Health) hpGain += gain.Amount;
                    else if (gain.Stat == LevelUpStat.Mana) manaGain += gain.Amount;
                    else atkGain += gain.Amount;
                }

            CharacterStats stats = character.CharacterStats;
            var data = new CardData
            {
                Character = character,
                MaxHp  = stats.MaxHealthPoints - hpGain,
                Hp     = stats.HealthPoints - hpGain,
                MaxMana = stats.MaxMana - manaGain,
                Mana    = stats.Mana - manaGain,
                Attack  = stats.BaseAttack - atkGain,
            };

            var card = new VisualElement();
            card.AddToClassList("level-up-card");

            var portrait = new VisualElement();
            portrait.AddToClassList("portrait");
            portrait.AddToClassList("level-up-portrait");
            if (!string.IsNullOrEmpty(character.CharacterProfilePicPath))
            {
                Sprite sprite = Resources.Load<Sprite>(character.CharacterProfilePicPath);
                if (sprite != null) portrait.style.backgroundImage = new StyleBackground(sprite);
            }
            card.Add(portrait);

            var name = new Label(character.CharacterName);
            name.AddToClassList("level-up-name");
            card.Add(name);

            data.LevelLabel = new Label($"Lv {character.Level - levelsGained}");
            data.LevelLabel.AddToClassList("lv");
            data.LevelLabel.AddToClassList("level-up-level");
            card.Add(data.LevelLabel);

            data.HpBar   = MakeStatRow(card, "health_128x128", out data.HpNum);
            data.ManaBar = MakeStatRow(card, "mana_128x128", out data.ManaNum);

            var atkRow = new VisualElement();
            atkRow.AddToClassList("level-up-stat-row");
            var atkIcon = new VisualElement();
            atkIcon.AddToClassList("level-up-stat-icon");
            Sprite atkSprite = Resources.Load<Sprite>("melee_attack_128x128");
            if (atkSprite != null) atkIcon.style.backgroundImage = new StyleBackground(atkSprite);
            atkRow.Add(atkIcon);

            // Attack has no bar to carry the "this changed" weight, unlike HP/Mana, so its
            // number breathes inside its own pill badge (background glow, not just text
            // color) to read just as clearly as a bar segment lighting up.
            data.AttackBadge = new VisualElement();
            data.AttackBadge.AddToClassList("level-up-stat-badge");
            data.AttackNum = new Label(data.Attack.ToString());
            data.AttackNum.AddToClassList("level-up-stat-num");
            data.AttackBadge.Add(data.AttackNum);
            atkRow.Add(data.AttackBadge);
            card.Add(atkRow);

            data.TalentLabel = new Label();
            data.TalentLabel.AddToClassList("level-up-talent");
            card.Add(data.TalentLabel);

            SetSegmentedBar(data.HpBar, data.Hp, data.MaxHp, "seg-fill-hp");
            data.HpNum.text = $"{data.Hp}/{data.MaxHp}";
            SetSegmentedBar(data.ManaBar, data.Mana, data.MaxMana, "seg-fill-mana");
            data.ManaNum.text = $"{data.Mana}/{data.MaxMana}";

            _cardsRow.Add(card);
            return data;
        }

        private static VisualElement MakeStatRow(VisualElement card, string iconResource, out Label numberLabel)
        {
            var row = new VisualElement();
            row.AddToClassList("level-up-stat-row");

            var icon = new VisualElement();
            icon.AddToClassList("level-up-stat-icon");
            Sprite sprite = Resources.Load<Sprite>(iconResource);
            if (sprite != null) icon.style.backgroundImage = new StyleBackground(sprite);
            row.Add(icon);

            var bar = new VisualElement();
            bar.AddToClassList("seg-bar");
            bar.AddToClassList("level-up-bar");
            row.Add(bar);

            numberLabel = new Label();
            numberLabel.AddToClassList("level-up-stat-num");
            row.Add(numberLabel);

            card.Add(row);
            return bar;
        }

        /// <summary>Rebuilds a segmented bar from scratch — mirrors
        /// CharacterHudController.SetSegmentedBar (one child per max point, first
        /// <paramref name="filled"/> of them get <paramref name="fillClass"/>).</summary>
        private static void SetSegmentedBar(VisualElement bar, int filled, int total, string fillClass)
        {
            bar.Clear();
            total = Mathf.Max(total, 1);
            filled = Mathf.Clamp(filled, 0, total);
            for (int i = 0; i < total; i++)
            {
                var seg = new VisualElement();
                seg.AddToClassList("seg");
                if (i < filled) seg.AddToClassList(fillClass);
                bar.Add(seg);
            }
        }

        // ── Reveal animation ─────────────────────────────────────────────────

        private IEnumerator RevealCard(CardData card)
        {
            // Snapshot: PendingLevelUpEvents is only ever mutated by a future fight's
            // GrantExperience (long after this coroutine finishes) or by OnContinueClicked's
            // skip path, never concurrently with this loop — copied defensively anyway.
            foreach (LevelUpEvent evt in new List<LevelUpEvent>(card.Character.PendingLevelUpEvents))
            {
                card.LevelLabel.text = $"Lv {evt.Level}";
                StartCoroutine(PopIn(card.LevelLabel));

                foreach (StatPointGain gain in evt.StatGains)
                {
                    RevealPoint(card, gain);
                    yield return new WaitForSeconds(PointRevealDelay);
                }

                if (!string.IsNullOrEmpty(evt.TalentGained))
                {
                    card.TalentLabel.text = $"New Talent: {evt.TalentGained}";
                    card.TalentLabel.AddToClassList("level-up-talent-visible");
                    yield return new WaitForSeconds(TalentRevealDelay);
                }
            }

            card.Character.PendingLevelUpEvents.Clear();
        }

        private void RevealPoint(CardData card, StatPointGain gain)
        {
            switch (gain.Stat)
            {
                case LevelUpStat.Health:
                    GrowSegmentedBar(card.HpBar, ref card.Hp, ref card.MaxHp, gain.Amount, "seg-fill-hp", HpFillColor);
                    card.HpNum.text = $"{card.Hp}/{card.MaxHp}";
                    StartCoroutine(BreathLabel(card.HpNum));
                    break;
                case LevelUpStat.Mana:
                    GrowSegmentedBar(card.ManaBar, ref card.Mana, ref card.MaxMana, gain.Amount, "seg-fill-mana", ManaFillColor);
                    card.ManaNum.text = $"{card.Mana}/{card.MaxMana}";
                    StartCoroutine(BreathLabel(card.ManaNum));
                    break;
                case LevelUpStat.Attack:
                    card.Attack += gain.Amount;
                    card.AttackNum.text = card.Attack.ToString();
                    StartCoroutine(BreathBadge(card.AttackBadge, card.AttackNum));
                    break;
            }
        }

        /// <summary>
        /// Grows a segmented bar by <paramref name="amount"/> points, rebuilding it from
        /// scratch via <see cref="SetSegmentedBar"/> rather than appending — appending would
        /// tack the new filled segments on after any pre-existing empty (current-less-than-max)
        /// segments, scattering the fill instead of extending it contiguously. The freshly
        /// added segments (indices [oldMax, newMax)) then breathe in place — see
        /// <see cref="BreathSegment"/>.
        /// </summary>
        private void GrowSegmentedBar(VisualElement bar, ref int current, ref int max, int amount, string fillClass, Color baseColor)
        {
            int oldMax = max;
            current += amount;
            max += amount;
            SetSegmentedBar(bar, current, max, fillClass);

            for (int i = oldMax; i < max && i < bar.childCount; i++)
                StartCoroutine(BreathSegment(bar[i], baseColor));
        }

        private IEnumerator PopIn(VisualElement element)
        {
            float elapsed = 0f;
            while (elapsed < PopDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / PopDuration);
                float s = PopCurve.Evaluate(t);
                element.style.scale = new Scale(new Vector3(s, s, 1f));
                yield return null;
            }
            element.style.scale = new Scale(Vector3.one);
        }

        /// <summary>
        /// Drives a slow, smooth "breathe" — a handful of gentle ease-in/ease-out cycles
        /// whose amplitude fades to nothing by the end — via <paramref name="applyIntensity"/>,
        /// called every frame with a 0..1 value (0 = resting state, 1 = full glow/expand).
        /// Shared by <see cref="BreathSegment"/> and <see cref="BreathLabel"/> so both animate
        /// with exactly the same slow, calm rhythm.
        /// </summary>
        private static IEnumerator Breathe(Action<float> applyIntensity)
        {
            float duration = BreathPeriod * BreathCycles;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float phase = (elapsed % BreathPeriod) / BreathPeriod;
                float intensity = (1f - Mathf.Cos(phase * Mathf.PI * 2f)) * 0.5f; // 0 -> 1 -> 0, eased
                float envelope = 1f - Mathf.Clamp01(elapsed / duration);          // fades the swing out
                applyIntensity(intensity * envelope);
                yield return null;
            }
            applyIntensity(0f);
        }

        /// <summary>Newly revealed bar segment: slowly glows green and swells a little,
        /// breathing back down to its resting fill color/size a few times before settling.</summary>
        private IEnumerator BreathSegment(VisualElement segment, Color baseColor) =>
            Breathe(t =>
            {
                segment.style.backgroundColor = Color.Lerp(baseColor, BreathColor, t);
                float s = Mathf.Lerp(1f, BarBreathScalePeak, t);
                segment.style.scale = new Scale(new Vector3(s, s, 1f));
            });

        /// <summary>Same slow breathing highlight as <see cref="BreathSegment"/>, for a stat
        /// number label instead of a bar segment (text color instead of background).</summary>
        private IEnumerator BreathLabel(Label label) =>
            Breathe(t =>
            {
                label.style.color = Color.Lerp(StatNumColor, BreathColor, t);
                float s = Mathf.Lerp(1f, LabelBreathScalePeak, t);
                label.style.scale = new Scale(new Vector3(s, s, 1f));
            });

        /// <summary>Breathing highlight for a stat with no bar to carry the "this changed"
        /// weight (Attack): the badge itself glows and swells like a bar segment would, and
        /// its number inverts to a dark color so it stays readable against the lit badge.</summary>
        private IEnumerator BreathBadge(VisualElement badge, Label label) =>
            Breathe(t =>
            {
                badge.style.backgroundColor = Color.Lerp(Color.clear, BreathColor, t);
                label.style.color = Color.Lerp(StatNumColor, BadgeTextColor, t);
                float s = Mathf.Lerp(1f, BarBreathScalePeak, t);
                badge.style.scale = new Scale(new Vector3(s, s, 1f));
            });

        // ── Continue / skip ──────────────────────────────────────────────────

        private void OnContinueClicked()
        {
            // Stops the RevealCard coroutines and every child pop/blink coroutine they've
            // spawned in one go — nothing else on this component uses coroutines.
            StopAllCoroutines();

            foreach (CardData card in _cards)
            {
                card.Character.PendingLevelUpEvents.Clear();
                FinalizeCard(card);
            }

            _overlay.style.display = DisplayStyle.None;
            _onContinue?.Invoke();
        }

        /// <summary>Snaps a card straight to its character's real (already-applied) stats —
        /// used when Continue is clicked mid-animation so nothing is left half-revealed. Bar
        /// segments are rebuilt from scratch (fresh elements, no leftover inline style), but
        /// the stat labels are reused instances, so any breathing color/scale a StopAllCoroutines
        /// interrupted mid-cycle needs to be explicitly cleared here.</summary>
        private static void FinalizeCard(CardData card)
        {
            CharacterStats stats = card.Character.CharacterStats;
            card.LevelLabel.text = $"Lv {card.Character.Level}";
            ResetTransientStyle(card.LevelLabel);

            SetSegmentedBar(card.HpBar, stats.HealthPoints, stats.MaxHealthPoints, "seg-fill-hp");
            card.HpNum.text = $"{stats.HealthPoints}/{stats.MaxHealthPoints}";
            ResetTransientStyle(card.HpNum);

            SetSegmentedBar(card.ManaBar, stats.Mana, stats.MaxMana, "seg-fill-mana");
            card.ManaNum.text = $"{stats.Mana}/{stats.MaxMana}";
            ResetTransientStyle(card.ManaNum);

            card.AttackNum.text = stats.BaseAttack.ToString();
            ResetTransientStyle(card.AttackNum);
            ResetTransientStyle(card.AttackBadge);
        }

        private static void ResetTransientStyle(VisualElement element)
        {
            element.style.scale = new Scale(Vector3.one);
            element.style.color = StyleKeyword.Null;
            element.style.backgroundColor = StyleKeyword.Null;
        }
    }
}
