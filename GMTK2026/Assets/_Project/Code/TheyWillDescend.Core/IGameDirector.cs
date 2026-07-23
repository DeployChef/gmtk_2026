using Cysharp.Threading.Tasks;

namespace TheyWillDescend.Core
{
    public interface IGameDirector
    {
        UniTask StartAsync();
        UniTask RestartAsync();
    }
}
