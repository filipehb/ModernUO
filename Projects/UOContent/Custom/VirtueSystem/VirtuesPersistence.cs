using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.VirtueSystem;

public class VirtuesPersistence : GenericPersistence
{
    private static VirtuesPersistence _virtuesPersistence;
    private static Dictionary<PlayerMobile, Virtues> VirtueTable = new();
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(VirtuesPersistence));

    public static void Configure()
    {
        _virtuesPersistence = new VirtuesPersistence();
    }

    public VirtuesPersistence() : base("Virtue", 10)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        // Do serialization here
        writer.WriteEncodedInt(0); // version

        writer.Write(VirtueTable.Count);

        foreach (var kvp in VirtueTable)
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

                    VirtueTable = new Dictionary<PlayerMobile, Virtues>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            var playerMobile = reader.ReadEntity<PlayerMobile>();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            VirtueTable[playerMobile] = reader.ReadEnum<Virtues>();

                            logger.Information("Carregada informações de virtudes");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Erro ao carregar virtudes: {e.Message}");
                    }

                    break;
                }
        }
    }

    public static bool SetVirtue(PlayerMobile playerMobile, Virtues virtues, bool overwriteExisting)
    {
        if (playerMobile == null)
        {
            return false;
        }

        if (overwriteExisting)
        {
            VirtueTable[playerMobile] = virtues;

            return true;
        }

        if (!HasVirtue(playerMobile))
        {
            VirtueTable[playerMobile] = virtues;

            logger.Debug($"Atribuida virtue {virtues} para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}");

            return true;
        }

        logger.Debug(
            $"Não foi possivel setar a virtude da conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
        );
        return false;
    }

    public static Virtues GetPlayerVirtue(Mobile mobile)
    {
        if (mobile is not PlayerMobile playerMobile)
        {
            return Virtues.None;
        }

        return !HasVirtue(playerMobile) ? Virtues.None : VirtueTable[playerMobile];
    }

    public static bool HasVirtue(PlayerMobile playerMobile) => VirtueTable.ContainsKey(playerMobile);
}
