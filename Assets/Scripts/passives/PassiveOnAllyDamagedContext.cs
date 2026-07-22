using BaaroForce.Characters;
using BaaroForce.Map;

namespace BaaroForce.Passives
{
    /// <summary>
    /// All information a passive ability's Execute method needs to resolve its effects
    /// when a nearby ally (not the passive's owner) has just taken damage and survived.
    /// Built by TurnManager and passed to <see cref="PassiveAbility.Execute(PassiveOnAllyDamagedContext)"/>.
    ///
    /// Range/threshold checks (e.g. "is the ally within radius 2?", "did they drop below
    /// 50% health?") are the passive's own responsibility — see SpiritualProtector — same
    /// as every other passive owning its own effect logic.
    /// </summary>
    public sealed class PassiveOnAllyDamagedContext
    {
        /// <summary>The character who owns this passive ability.</summary>
        public readonly Character Owner;

        /// <summary>The tile the passive's owner is standing on.</summary>
        public readonly MapTile OwnerTile;

        /// <summary>The ally who just took damage (never the owner themselves).</summary>
        public readonly Character DamagedAlly;

        /// <summary>The tile the damaged ally is standing on.</summary>
        public readonly MapTile DamagedAllyTile;

        /// <summary>The full grid, so Execute can query surrounding tiles for range checks.</summary>
        public readonly MapTile[,] AllTiles;

        /// <summary>Side length of the grid.</summary>
        public readonly int GridSize;

        public PassiveOnAllyDamagedContext(
            Character owner,
            MapTile ownerTile,
            Character damagedAlly,
            MapTile damagedAllyTile,
            MapTile[,] allTiles,
            int gridSize)
        {
            Owner           = owner;
            OwnerTile       = ownerTile;
            DamagedAlly     = damagedAlly;
            DamagedAllyTile = damagedAllyTile;
            AllTiles        = allTiles;
            GridSize        = gridSize;
        }
    }
}
