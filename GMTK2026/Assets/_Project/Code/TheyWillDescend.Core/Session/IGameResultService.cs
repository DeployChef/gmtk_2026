namespace TheyWillDescend.Core.Session
{
    /// <summary>
    /// Session win/lose stub. Publishes <c>GameWonEvent</c> / <c>GameLostEvent</c> once per run.
    /// </summary>
    public interface IGameResultService
    {
        bool HasResult { get; }
        bool IsVictory { get; }

        void DeclareWin(GameResultCause cause);
        void DeclareLose(GameResultCause cause);
        void Clear();
    }
}
