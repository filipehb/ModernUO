using ModernUO.CodeGeneratedEvents;
using Server.Engines.CharacterCreation;
using Server.Mobiles;
using CharCreation = Server.Engines.CharacterCreation.CharacterCreation;

namespace Server.Engines.Cosmere;

public static class CosmereBootstrap
{
    public static void Initialize()
    {
        CosmereTime.Initialize();
        CosmereLivingWorld.Initialize();
    }

    [OnEvent(nameof(CharCreation.CharacterCreatedEvent))]
    public static void OnCharacterCreated(CharacterCreatedEventArgs args)
    {
        if (args.Mobile is not PlayerMobile pm)
        {
            return;
        }

        pm.CosmereSheet = CosmereSheet.CreateMundaneStarter();
        CosmereFacade.Apply(pm);
    }

    [OnEvent(nameof(PlayerMobile.PlayerLoginEvent))]
    public static void OnLogin(PlayerMobile pm)
    {
        pm.EnsureCosmereSheet();
        CosmereFacade.Apply(pm);
        CosmereEquipment.SyncCumbersome(pm);
        CosmereSustenance.Sync(pm);
        pm.CosmereLastDayPeriod = CosmereTime.GetPeriod(pm);

        var sheet = pm.CosmereSheet;
        if (!sheet.Dead)
        {
            return;
        }

        pm.Frozen = true;

        if (CosmereConfig.PermanentDeath)
        {
            pm.SendMessage("This character is permanently dead on this shard.");
        }
        else
        {
            pm.SendMessage("This character is dead. Ask staff for CosmereRez.");
        }
    }
}
