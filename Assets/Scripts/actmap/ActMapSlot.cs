using System.Collections.Generic;

namespace BaaroForce.ActMap
{
    /// <summary>
    /// One step of the Act Map's path. Most slots hold exactly one fixed <see cref="ActMapNode"/>;
    /// the design doc's three "split node set" forks are each two branches of two sequential
    /// nodes, represented as a pair of consecutive fork slots — a first slot (the real choice,
    /// two options) and a second slot (<see cref="IsForkContinuation"/>, auto-inherits whichever
    /// branch was picked at the first slot — see <c>ActRunState.CompleteCurrentNode</c>). The
    /// unchosen branch's nodes are simply never resolved.
    /// </summary>
    public class ActMapSlot
    {
        public List<ActMapNode> Options { get; }
        public bool IsFork => Options.Count > 1;

        /// <summary>True for a fork's second slot — its branch isn't a free player choice, it
        /// automatically continues whichever branch was chosen at the fork's first slot.</summary>
        public bool IsForkContinuation { get; }

        /// <summary>Index into <see cref="Options"/> of the node the player picked (or the only
        /// option, for non-fork slots) — -1 until resolved for fork slots.</summary>
        public int ChosenOptionIndex { get; set; }

        public bool Resolved { get; set; }

        public ActMapSlot(params ActMapNode[] options) : this(false, options) { }

        public ActMapSlot(bool isForkContinuation, params ActMapNode[] options)
        {
            Options = new List<ActMapNode>(options);
            IsForkContinuation = isForkContinuation;
            ChosenOptionIndex = Options.Count == 1 ? 0 : -1;
        }

        /// <summary>The chosen node once resolved (or the single option for non-fork slots);
        /// null for an unresolved fork.</summary>
        public ActMapNode ChosenNode => ChosenOptionIndex >= 0 && ChosenOptionIndex < Options.Count
            ? Options[ChosenOptionIndex]
            : null;
    }
}
