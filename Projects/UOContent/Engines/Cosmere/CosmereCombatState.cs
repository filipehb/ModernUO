using System;

namespace Server.Engines.Cosmere;

/// <summary>
/// Runtime combat engagement state for a Cosmere PC (Phase 3).
/// </summary>
public class CosmereCombatState
{
    public bool InCombat { get; set; }
    public int SceneId { get; set; }
    public DateTime LastCombatActivity { get; set; } = DateTime.MinValue;
    public bool RecoverUsedThisScene { get; set; }
    public bool ReactionAvailable { get; set; }
    public bool DodgeArmed { get; set; }
    public bool Braced { get; set; }

    public void BeginScene(int sceneId)
    {
        InCombat = true;
        SceneId = sceneId;
        RecoverUsedThisScene = false;
        ReactionAvailable = true;
        DodgeArmed = false;
        Braced = false;
        Touch();
    }

    public void EndScene()
    {
        InCombat = false;
        RecoverUsedThisScene = false;
        ReactionAvailable = false;
        DodgeArmed = false;
        Braced = false;
        LastCombatActivity = DateTime.MinValue;
    }

    public void Touch() => LastCombatActivity = Core.Now;
}
