using System.Collections.Generic;

namespace BaaroForce.ActMap
{
    /// <summary>
    /// One step of the Act Map's path. Most slots hold exactly one fixed <see cref="ActMapNode"/>;
    /// the three "split node set" slots from the design doc (nodes 4/5, 7/8, 10/11) hold two
    /// alternative nodes the player picks between — the unchosen option is simply never resolved.
    /// </summary>
    public class ActMapSlot
    {
        public List<ActMapNode> Options { get; }
        public bool IsFork => Options.Count > 1;

        /// <summary>Index into <see cref="Options"/> of the node the player picked (or the only
        /// option, for non-fork slots) — -1 until resolved for fork slots.</summary>
        public int ChosenOptionIndex { get; set; }

        public bool Resolved { get; set; }

        public ActMapSlot(params ActMapNode[] options)
        {
            Options = new List<ActMapNode>(options);
            ChosenOptionIndex = Options.Count == 1 ? 0 : -1;
        }

        /// <summary>The chosen node once resolved (or the single option for non-fork slots);
        /// null for an unresolved fork.</summary>
        public ActMapNode ChosenNode => ChosenOptionIndex >= 0 && ChosenOptionIndex < Options.Count
            ? Options[ChosenOptionIndex]
            : null;
    }
}
