namespace TheyWillDescend.Core.Economy
{
    public interface IResourceCard
    {
        string ResourceId { get; }
        ResourceKind Kind { get; }
        void Setup(string resourceId);
        void Setup(ResourceDefinition definition);
    }
}
