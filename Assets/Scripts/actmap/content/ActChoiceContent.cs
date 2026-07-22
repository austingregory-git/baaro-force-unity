using System;
using System.Collections.Generic;
using BaaroForce.Characters;
using BaaroForce.GameController;

namespace BaaroForce.ActMap.Content
{
    /// <summary>One selectable outcome of an <see cref="ActEvent"/> or <see cref="ActSideQuest"/>.</summary>
    public class ActChoiceOption
    {
        public string Label { get; }
        public string ResultText { get; }
        public Action<PartyManager> Apply { get; }

        public ActChoiceOption(string label, string resultText, Action<PartyManager> apply)
        {
            Label = label;
            ResultText = resultText;
            Apply = apply;
        }
    }

    /// <summary>Shared shape for text + 2-choice map content, scoped to a single realm.</summary>
    public abstract class ActChoiceContent
    {
        public string Title { get; }
        public Realm Realm { get; }
        public string Description { get; }
        public List<ActChoiceOption> Choices { get; }

        protected ActChoiceContent(string title, Realm realm, string description, List<ActChoiceOption> choices)
        {
            Title = title;
            Realm = realm;
            Description = description;
            Choices = choices;
        }
    }

    /// <summary>Risk/reward text+choice content (question-mark nodes). On average a boost,
    /// but some choices gamble a bigger reward against a smaller consolation one.</summary>
    public class ActEvent : ActChoiceContent
    {
        public ActEvent(string title, Realm realm, string description, List<ActChoiceOption> choices)
            : base(title, realm, description, choices) { }
    }

    /// <summary>Low-risk, near-guaranteed-positive text+choice content (exclamation-mark
    /// nodes) — always grants gold + XP plus a choice-specific third reward.</summary>
    public class ActSideQuest : ActChoiceContent
    {
        public ActSideQuest(string title, Realm realm, string description, List<ActChoiceOption> choices)
            : base(title, realm, description, choices) { }
    }
}
