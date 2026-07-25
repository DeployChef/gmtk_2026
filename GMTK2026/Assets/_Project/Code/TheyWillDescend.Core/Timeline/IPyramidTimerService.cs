namespace TheyWillDescend.Core.Timeline
{
    public interface IPyramidTimerService
    {
        float RemainingSeconds { get; }
        bool IsExpired { get; }

        void ResetToBaseline();
        /// <summary>Cheat / debug: set remaining doomsday seconds directly.</summary>
        void SetRemainingSeconds(float seconds);
        void AddSeconds(float delta);
        void Tick(float deltaTime);
    }
}
