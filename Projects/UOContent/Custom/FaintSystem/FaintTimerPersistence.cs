using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.FaintSystem;

public class FaintTimerPersistence
{
    private static Dictionary<PlayerMobile, bool> FaintRunningTable = new();
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(FaintTimerPersistence));

    public static void Configure()
    {
        GenericPersistence.Register("FaintRunning", Serialize, Deserialize);
    }

    public static void Serialize(IGenericWriter writer)
    {
        // Do serialization here
        writer.WriteEncodedInt(0); // version

        writer.Write(FaintRunningTable.Count);

        foreach (var kvp in FaintRunningTable)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }

        logger.Information("Salvo informações de FaintRunning");
    }

    public static void Deserialize(IGenericReader reader)
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

                    FaintRunningTable = new Dictionary<PlayerMobile, bool>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            var playerMobile = reader.ReadEntity<PlayerMobile>();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            FaintRunningTable[playerMobile] = reader.ReadBool();

                            logger.Information("Carregada informações de FaintRunning");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Erro ao carregar FaintRunning: {e.Message}");
                    }

                    break;
                }
        }
    }

    public static bool SetFaintRunning(PlayerMobile playerMobile, bool faintRunning, bool overwriteExisting)
    {
        if (playerMobile == null)
        {
            return false;
        }

        if (overwriteExisting)
        {
            FaintRunningTable[playerMobile] = faintRunning;

            return true;
        }

        if (!HasFaintRunning(playerMobile))
        {
            FaintRunningTable[playerMobile] = faintRunning;

            logger.Debug(
                $"Atribuido FaintRunning {faintRunning} para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
            );

            return true;
        }

        logger.Debug(
            $"Não foi possivel setar FaintRunning da conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
        );
        return false;
    }

    public static bool? GetPlayerFaintRunning(Mobile mobile)
    {
        if (mobile is not PlayerMobile)
        {
            return null;
        }

        if (!HasFaintRunning((PlayerMobile)mobile))
        {
            return null;
        }

        return FaintRunningTable[(PlayerMobile)mobile];
    }

    public static bool HasFaintRunning(PlayerMobile playerMobile) => FaintRunningTable.ContainsKey(playerMobile);
}
