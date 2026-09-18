using System;

namespace Server.Engines.Cosmere;

public enum CosmerePersuasiveness
{
    Normal = 0,
    Strong = 1,
    Overwhelm = 2
}

public enum CosmereSocialDefense
{
    Spiritual = 0,
    Cognitive = 1
}

public class CosmereConversationState
{
    public bool InConversation { get; set; }
    public int SceneId { get; set; }
    public bool Formal { get; set; }
    public DateTime LastActivity { get; set; } = DateTime.MinValue;
    public bool ResistArmed { get; set; }
    public int NextTestAdv { get; set; }
    public int NextTestDisadv { get; set; }
    public string LastInfluenceSummary { get; set; }
    public bool SocialOptIn { get; set; }

    public void Begin(int sceneId, bool formal)
    {
        InConversation = true;
        SceneId = sceneId;
        Formal = formal;
        ResistArmed = false;
        NextTestAdv = 0;
        NextTestDisadv = 0;
        LastInfluenceSummary = null;
        Touch();
    }

    public void End()
    {
        InConversation = false;
        ResistArmed = false;
        NextTestAdv = 0;
        NextTestDisadv = 0;
        LastActivity = DateTime.MinValue;
    }

    public void Touch() => LastActivity = Core.Now;
}

public class CosmereNpcSocialState
{
    public int MaxFocus { get; set; }
    public int Focus { get; set; }
    public bool Conceded { get; set; }

    public void Clamp()
    {
        if (MaxFocus < 0)
        {
            MaxFocus = 0;
        }

        Focus = Math.Clamp(Focus, 0, MaxFocus);
        if (Focus <= 0 && MaxFocus >= 0)
        {
            // Conceded is set explicitly when Focus hits 0 after interaction
        }
    }
}
