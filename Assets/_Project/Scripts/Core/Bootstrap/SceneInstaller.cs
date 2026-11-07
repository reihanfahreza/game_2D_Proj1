using UnityEngine;

/// <summary>
/// Inheritance (P4): setiap scene punya satu installer di root. SceneFlow mencarinya lalu memanggil Install.
/// WorldInstaller, BattleInstaller, TitleInstaller, EndInstaller adalah turunannya (polymorphism).
/// </summary>
public abstract class SceneInstaller : MonoBehaviour
{
    public abstract void Install(GameServices services);
}
