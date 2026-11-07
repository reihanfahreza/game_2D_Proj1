public enum GrowthType { LevelUp, Evolve }

/// <summary>Satu kejadian pertumbuhan dari GainExp: naik level atau berevolusi.</summary>
public readonly struct GrowthEvent
{
    public GrowthEvent(GrowthType type, MonsterData from, MonsterData to, int level, string message)
    {
        Type = type;
        From = from;
        To = to;
        Level = level;
        Message = message;
    }

    public GrowthType Type { get; }
    public MonsterData From { get; }
    public MonsterData To { get; }
    public int Level { get; }
    public string Message { get; }
}
