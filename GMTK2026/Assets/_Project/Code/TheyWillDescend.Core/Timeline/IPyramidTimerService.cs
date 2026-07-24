namespace TheyWillDescend.Core.Timeline
{
    public interface IPyramidTimerService
    {
        float RemainingSeconds { get; }
        bool IsExpired { get; }

        void ResetToBaseline();
        void AddSeconds(float delta);
        void Tick(float deltaTime);
    }
}
