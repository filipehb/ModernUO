namespace Server.Custom.Classes;

public abstract class ClassDefinition
{
    private int Life { get; set; }
    private int LifeIncremant { get; set; }
    private int PrimaryAbility { get; set; }

    private Proficiencie Proficiencie { get; set; }
    private Feature Features  { get; set; }
}
