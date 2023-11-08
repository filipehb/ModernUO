using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.BackgroundSystem;

public class BackgroundPersistence : GenericPersistence
{
    private static BackgroundPersistence _backgroundPersistence;
    private static Dictionary<PlayerMobile, Backgrounds> BackgroundTable = new();
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(BackgroundPersistence));

    public static void Configure()
    {
        _backgroundPersistence = new BackgroundPersistence();
    }

    public BackgroundPersistence() : base("Background", 10)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        // Do serialization here
        writer.WriteEncodedInt(0); // version

        writer.Write(BackgroundTable.Count);

        foreach (var kvp in BackgroundTable)
        {
            writer.Write(kvp.Key);
            writer.WriteEnum(kvp.Value);
        }

        logger.Information("Salvo informações de virtudes");
    }

    public override void Deserialize(IGenericReader reader)
    {
        // Do deserialization here
        var version = reader.ReadEncodedInt();

        switch (version)
        {
            case 1:
                {
                    goto case 0;
                }
            case 0:
                {
                    int tableCount = reader.ReadInt();

                    BackgroundTable = new Dictionary<PlayerMobile, Backgrounds>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            var playerMobile = reader.ReadEntity<PlayerMobile>();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            BackgroundTable[playerMobile] = reader.ReadEnum<Backgrounds>();

                            logger.Information("Carregada informações de background");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Erro ao carregar backgrounds: {e.Message}");
                    }

                    break;
                }
        }
    }

    public static bool SetVirtue(PlayerMobile playerMobile, Backgrounds backgrounds, bool overwriteExisting)
    {
        if (playerMobile == null)
        {
            return false;
        }

        if (overwriteExisting)
        {
            BackgroundTable[playerMobile] = backgrounds;

            return true;
        }

        if (!HasBackground(playerMobile))
        {
            BackgroundTable[playerMobile] = backgrounds;

            logger.Debug($"Atribuido background {backgrounds} para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}");

            return true;
        }

        logger.Debug(
            $"Não foi possivel setar o background da conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
        );
        return false;
    }

    public static Backgrounds GetPlayerBackground(Mobile mobile)
    {
        if (mobile is not PlayerMobile playerMobile)
        {
            return Backgrounds.None;
        }

        return !HasBackground(playerMobile) ? Backgrounds.None : BackgroundTable[playerMobile];
    }

    public static bool HasBackground(PlayerMobile playerMobile) => BackgroundTable.ContainsKey(playerMobile);
}
