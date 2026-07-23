using Cysharp.Threading.Tasks;

namespace TheyWillDescend.Core
{
    public interface IGameDirector
    {
        UniTask InitializeGameAsync();
        UniTask StartNewRunAsync();
        UniTask RestartRunAsync();
        void NotifyRunWon();
        void NotifyRunLost();
    }
}
