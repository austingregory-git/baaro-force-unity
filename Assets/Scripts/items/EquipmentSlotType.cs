namespace BaaroForce.Items
{
    /// <summary>Which equipment slot an <see cref="Equipment"/> occupies. MainHand/OffHand
    /// items may additionally be flagged as weapons via <see cref="Equipment.IsWeapon"/> —
    /// OffHand covers non-weapon offhands too (shields, tomes).</summary>
    public enum EquipmentSlotType
    {
        Helmet,
        Chest,
        Legs,
        MainHand,
        OffHand
    }
}
