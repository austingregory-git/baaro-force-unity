using BaaroForce.Characters;
using BaaroForce.Map;

namespace BaaroForce.Spells
{
    /// <summary>
    /// All information a spell's Execute method needs to resolve its effects.
    /// Built by TurnManager and passed to <see cref="Spell.Execute"/>.
    ///
    /// Keeping resolution data here means individual spells never need to reach
    /// into the map or turn-management systems directly — they receive everything
    /// they need in one clean package.
    /// </summary>
    public sealed class SpellContext
    {
        /// <summary>The character who cast this spell.</summary>
        public readonly Character Caster;

        /// <summary>Caster's level for damage/duration scaling.</summary>
        public readonly int CasterLevel;

        /// <summary>The tile the caster is standing on when the spell is cast.</summary>
        public readonly MapTile CasterTile;

        /// <summary>The tile the player aimed at.</summary>
        public readonly MapTile TargetTile;

        /// <summary>The full grid, so Execute can query surrounding tiles for AoE.</summary>
        public readonly MapTile[,] AllTiles;

        /// <summary>Side length of the grid.</summary>
        public readonly int GridSize;

        public SpellContext(Character caster, int casterLevel,
                            MapTile casterTile, MapTile targetTile,
                            MapTile[,] allTiles, int gridSize)
        {
            Caster      = caster;
            CasterLevel = casterLevel;
            CasterTile  = casterTile;
            TargetTile  = targetTile;
            AllTiles    = allTiles;
            GridSize    = gridSize;
        }
    }
}
