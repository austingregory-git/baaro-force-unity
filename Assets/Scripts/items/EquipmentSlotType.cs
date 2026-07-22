namespace BaaroForce.Items
{
    /// <summary>Which equipment slot an <see cref="Equipment"/> occupies. Weapons are a
    /// subset of equipment rather than a separate concept, per the Act 1 design doc.</summary>
    public enum EquipmentSlotType
    {
        Weapon,
        Armor,
        Accessory
    }
}
