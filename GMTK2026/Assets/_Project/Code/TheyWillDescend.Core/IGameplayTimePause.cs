namespace TheyWillDescend.Core
{
    /// <summary>
    /// Ref-counted <see cref="UnityEngine.Time.timeScale"/> pause so Esc menu and dialogue can stack.
    /// </summary>
    public interface IGameplayTimePause
    {
        bool IsPaused { get; }

        void Acquire(object key);

        void Release(object key);
    }
}
