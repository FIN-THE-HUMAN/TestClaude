namespace Game.Balls
{
    /// <summary>
    /// Identity of a ball. Defined as an enum (not a string id) because:
    /// - matching is the hot inner loop and integer comparison wins over strings;
    /// - the editor can render colour swatches via a custom drawer.
    /// New colours are added here; do not invent ad-hoc colours at call sites.
    /// </summary>
    public enum BallColor : byte
    {
        None   = 0,
        Red    = 1,
        Green  = 2,
        Blue   = 3,
        Yellow = 4,
        Purple = 5,
        White  = 6,
        Black  = 7,
    }
}
