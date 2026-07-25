using System.Threading;
using Cysharp.Threading.Tasks;

namespace TheyWillDescend.Core
{
    /// <summary>Opening cinematic before gameplay time starts ticking.</summary>
    public interface IOpeningSequence
    {
        UniTask PlayAsync(CancellationToken cancellationToken = default);
    }
}
