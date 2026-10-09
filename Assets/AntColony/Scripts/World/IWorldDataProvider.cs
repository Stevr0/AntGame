namespace AntColony.World
{
    /// <summary>
    /// Small read-only seam for scene components that present authoritative world
    /// data. Providers still own creation and mutation; consumers receive only the
    /// same ChunkedWorldData instance.
    /// </summary>
    public interface IWorldDataProvider
    {
        bool IsWorldDataAvailable { get; }
        ChunkedWorldData WorldData { get; }
    }
}
