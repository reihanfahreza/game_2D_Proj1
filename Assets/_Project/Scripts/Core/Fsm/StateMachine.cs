/// <summary>Hanya satu state aktif. Exit lama lalu Enter baru, selalu berurutan.</summary>
public class StateMachine
{
    public IState Current { get; private set; }

    public void ChangeTo(IState next)
    {
        if (next == Current) return;
        Current?.Exit();
        Current = next;
        Current?.Enter();
    }

    public void Tick(float deltaTime) => Current?.Tick(deltaTime);
}
