/// <summary>Satu state dalam Finite State Machine (P4). Dipakai monster liar.</summary>
public interface IState
{
    void Enter();
    void Tick(float deltaTime);
    void Exit();
}
