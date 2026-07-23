namespace TheyWillDescend.Core.Economy
{
    public interface IResourceCard
    {
        string ResourceId { get; }
        CardKind Kind { get; }
        void Setup(string resourceId);
        void Setup(CardDefinition definition);
    }
}
