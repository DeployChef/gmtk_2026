namespace TheyWillDescend.Core.Economy
{
    public interface IResourceCard
    {
        string ResourceId { get; }
        void Setup(string resourceId);
    }
}
