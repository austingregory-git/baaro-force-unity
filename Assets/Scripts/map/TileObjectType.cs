namespace BaaroForce.Map
{
    /// <summary>
    /// An optional prop placed on a tile independently of its terrain — the middle of the three
    /// authoring layers (terrain / object / unit) a hand-drawn .map file can specify per cell.
    /// Distinct from MapTile.SpawnTerrainProps' Forest/Mountain decoration, which stays
    /// automatic and terrain-driven; these are extra, deliberately-placed props on top of
    /// whatever terrain is underneath. None is the default — most tiles won't have one.
    /// </summary>
    public enum TileObjectType
    {
        None,
        Boulder,
        Crate,
        Rubble,
    }
}
