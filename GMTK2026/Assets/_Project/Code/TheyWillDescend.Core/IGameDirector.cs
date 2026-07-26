using Cysharp.Threading.Tasks;

namespace TheyWillDescend.Core
{
    public interface IGameDirector
    {
        UniTask StartAsync();
        UniTask RestartAsync();

        /// <summary>
        /// Soft reset to phase 1 without unloading the Game scene (pause-menu restart).
        /// </summary>
        UniTask SoftRestartToFirstPhaseAsync();
    }
}
