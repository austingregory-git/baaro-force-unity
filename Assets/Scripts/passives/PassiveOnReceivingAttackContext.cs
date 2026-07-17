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
    public sealed class PassiveOnReceivingAttackContext
    {
        /// <summary>
        ///  The unit who is attacking the character with this passive ability.
        /// </summary>
        public readonly Character Attacker;

        /// <summary>The character who is receiving the attack.</summary>
        public readonly Character ReceivingCharacter;


        /// <summary>The tile the attacker is standing on when the passive ability is activated.</summary>
        public readonly MapTile AttackerTile;

        /// <summary>The tile targeted by the passive ability, if applicable.</summary>
        public readonly MapTile ReceivingCharacterTile;

        /// <summary>The full grid, so Execute can query surrounding tiles for AoE.</summary>
        public readonly MapTile[,] AllTiles;

        /// <summary>Side length of the grid.</summary>
        public readonly int GridSize;

        public PassiveOnReceivingAttackContext(
            Character attacker,
            Character receivingCharacter,
            MapTile attackerTile,
            MapTile receivingCharacterTile,
            MapTile[,] allTiles,
            int gridSize)
        {
            ReceivingCharacter = receivingCharacter;
            Attacker           = attacker;
            AttackerTile       = attackerTile;
            ReceivingCharacterTile = receivingCharacterTile;
            AllTiles           = allTiles;
            GridSize           = gridSize;
        }
    }
}
