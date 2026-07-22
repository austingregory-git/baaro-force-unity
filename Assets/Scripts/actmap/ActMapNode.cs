using BaaroForce.ActMap.Encounters;

namespace BaaroForce.ActMap
{
    /// <summary>One node on the Act Map — a single index from the Act 1 design doc's
    /// 0-15 path. See <see cref="ActMapSlot"/> for how fork nodes group two of these
    /// together as alternatives for the same step of the path.</summary>
    public class ActMapNode
    {
        public int Index { get; }
        public ActNodeType Type { get; }

        /// <summary>Only set for Fight/Elite/Boss nodes — which encounter pool to draw from.</summary>
        public EncounterPoolTier? Tier { get; }

        /// <summary>Only set for Fight/Elite/Boss nodes — the enemy level for this specific
        /// fight (see Encounter's remarks on why level isn't baked into the pool itself).</summary>
        public int EnemyLevel { get; }

        public bool Visited { get; set; }

        public ActMapNode(int index, ActNodeType type, EncounterPoolTier? tier = null, int enemyLevel = 0)
        {
            Index = index;
            Type = type;
            Tier = tier;
            EnemyLevel = enemyLevel;
        }
    }
}
