using BaaroForce.ActMap.Encounters;
using BaaroForce.Characters;
using BaaroForce.Map;

namespace BaaroForce.ActMap
{
    /// <summary>
    /// Fight/Elite/Boss node configuration written by <c>ActMapController</c> right before
    /// loading <c>MapScene</c>, and read by <c>MapGenerator.Start()</c> in place of its
    /// Inspector defaults — the same override pattern already used for Realm. Also read by
    /// <c>TurnManager</c> at fight-end to scale loot/XP (<see cref="Tier"/>).
    /// </summary>
    public class PendingEncounter
    {
        public Realm Realm { get; set; }
        public EncounterPoolTier Tier { get; set; }
        public MapSize MapSize { get; set; }
        public int EnemyLevel { get; set; }
        public int TargetStrength { get; set; }
    }
}
