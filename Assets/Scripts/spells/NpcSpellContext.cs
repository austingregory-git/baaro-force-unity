using BaaroForce.Characters;
using BaaroForce.Map;

namespace BaaroForce.Spells
{
    /// <summary>
    /// Mirrors <see cref="SpellContext"/> but for spells cast by an <see cref="NPC"/>.
    ///
    /// From the NPC's perspective the roles are reversed: the caster is an NPC and
    /// the targets are player Characters.  Individual spells override
    /// <see cref="Spell.Execute(NpcSpellContext)"/> to implement NPC-cast behaviour.
    /// </summary>
    public sealed class NpcSpellContext
    {
        /// <summary>The NPC who cast this spell.</summary>
        public readonly NPC     Caster;

        /// <summary>Caster's level for damage/duration scaling.</summary>
        public readonly int     CasterLevel;

        /// <summary>The tile the caster is standing on when the spell is cast.</summary>
        public readonly MapTile CasterTile;

        /// <summary>The tile the AI aimed at (null for self-targeting spells).</summary>
        public readonly MapTile TargetTile;

        /// <summary>The full grid, so Execute can query surrounding tiles for AoE.</summary>
        public readonly MapTile[,] AllTiles;

        /// <summary>Side length of the grid.</summary>
        public readonly int GridSize;

        public NpcSpellContext(NPC caster, int casterLevel,
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
