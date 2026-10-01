namespace GHA.AvatarFramework
{
    /// <summary>
    /// Optional Home provider adapter. The host owns Save/apply, startup, presentation
    /// and scene transitions; providers own construction and teardown of their instance.
    /// This local seam precedes the shared Home/network runtime registry (D3).
    /// </summary>
    public interface IHomeAvatarProvider
    {
        byte Mode { get; }
        byte DefaultMode { get; }
        // Preserve unknown IDs. A provider must not reinterpret another provider as itself.
        byte ResolveMode(byte requested);
        IHumanoidAvatarInstance Instance { get; }
        void BuildSavedAvatar();
        void Teardown();
    }
}
