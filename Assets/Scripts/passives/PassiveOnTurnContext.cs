using BaaroForce.Characters;
using BaaroForce.Map;

namespace BaaroForce.Passives
{
    /// <summary>
    /// All information a passive ability's Execute method needs to resolve its effects.
    /// Built by TurnManager and passed to <see cref="PassiveAbility.Execute"/>.
    ///
    /// Keeping resolution data here means individual passive abilities never need to reach
    /// into the map or turn-management systems directly — they receive everything
    /// they need in one clean package.
    /// </summary>
    public sealed class PassiveOnTurnContext
    {
        /// <summary>The character who activated this passive ability.</summary>
        public readonly Character Character;


        /// <summary>Character's level for damage/duration scaling.</summary>
        public readonly int CharacterLevel;

        /// <summary>The tile the caster is standing on when the passive ability is activated.</summary>
        public readonly MapTile CharacterTile;

        /// <summary>The tile targeted by the passive ability, if applicable.</summary>
        public readonly MapTile TargetTile;

        /// <summary>The full grid, so Execute can query surrounding tiles for AoE.</summary>
        public readonly MapTile[,] AllTiles;

        /// <summary>Side length of the grid.</summary>
        public readonly int GridSize;

        public PassiveOnTurnContext(Character character, int characterLevel,
                                      MapTile characterTile,
                                      MapTile[,] allTiles, int gridSize)
        {
            Character      = character;
            CharacterLevel = characterLevel;
            CharacterTile  = characterTile;
            AllTiles       = allTiles;
            GridSize       = gridSize;
        }
    }
}
