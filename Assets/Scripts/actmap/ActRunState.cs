namespace BaaroForce.ActMap
{
    /// <summary>
    /// Act 1 run progress, held by <c>PartyManager.ActRun</c> (created once per run alongside
    /// Party, reset in ResetForNewRun). Owns the generated <see cref="ActMap"/> and the
    /// pointer into it; <c>ActMapController</c> is the only thing that mutates this.
    /// </summary>
    public class ActRunState
    {
        public ActMap Map { get; }
        public int CurrentSlotIndex { get; private set; }

        /// <summary>Set right before loading MapScene for a Fight/Elite/Boss node; read by
        /// MapGenerator.Start() and cleared once consumed.</summary>
        public PendingEncounter PendingEncounter { get; set; }

        /// <summary>Temporary max-HP bonus granted by a Village Rest, consumed by the next
        /// fight's setup.</summary>
        public int RestBonusHealth { get; set; }

        public ActRunState()
        {
            Map = ActMap.GenerateAct1();
            CurrentSlotIndex = 0;
        }

        public ActMapSlot CurrentSlot => CurrentSlotIndex >= 0 && CurrentSlotIndex < Map.Slots.Count
            ? Map.Slots[CurrentSlotIndex]
            : null;

        /// <summary>The node about to be resolved — null for an unresolved fork slot until
        /// <see cref="ChooseForkOption"/> is called.</summary>
        public ActMapNode CurrentNode => CurrentSlot?.ChosenNode;

        public bool IsActComplete => CurrentSlotIndex >= Map.Slots.Count;

        /// <summary>Picks which of a fork slot's two options the player is taking.</summary>
        public void ChooseForkOption(int optionIndex)
        {
            if (CurrentSlot == null || !CurrentSlot.IsFork) return;
            if (optionIndex < 0 || optionIndex >= CurrentSlot.Options.Count) return;
            CurrentSlot.ChosenOptionIndex = optionIndex;
        }

        /// <summary>Marks the current node resolved and advances to the next slot. No-ops if
        /// the current slot is an unresolved fork (call <see cref="ChooseForkOption"/> first).
        /// If the new current slot is a fork continuation, it isn't a fresh player choice — it
        /// automatically inherits the branch just chosen at the slot before it.</summary>
        public void CompleteCurrentNode()
        {
            ActMapNode node = CurrentNode;
            if (node == null) return;

            int justResolvedIndex = CurrentSlotIndex;
            node.Visited = true;
            CurrentSlot.Resolved = true;
            CurrentSlotIndex++;

            if (CurrentSlot != null && CurrentSlot.IsForkContinuation)
                CurrentSlot.ChosenOptionIndex = Map.Slots[justResolvedIndex].ChosenOptionIndex;
        }
    }
}
