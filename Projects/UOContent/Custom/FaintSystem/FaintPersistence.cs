using System;
using System.Collections.Generic;
using System.Linq;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.FaintSystem;

public class FaintPersistence
{
    private static Dictionary<PlayerMobile, int> FaintTable = new();
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(FaintPersistence));

    public static void Configure()
    {
        GenericPersistence.Register("Faint", Serialize, Deserialize);
    }

    public static void Serialize(IGenericWriter writer)
    {
        // Do serialization here
        writer.WriteEncodedInt(0); // version

        writer.Write(FaintTable.Count);

        foreach (var kvp in FaintTable)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }

        logger.Information("Salvo informações de desmaios");
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

                    FaintTable = new Dictionary<PlayerMobile, int>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            var playerMobile = reader.ReadEntity<PlayerMobile>();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            FaintTable[playerMobile] = reader.ReadInt();

                            logger.Information("Carregada informações de desmaios");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Erro ao carregar desmaios: {e.Message}");
                    }

                    break;
                }
        }
    }

    public static bool IncreaseFaint(PlayerMobile playerMobile)
    {
        if (HasFaint(playerMobile))
        {
            int faint = FaintTable[playerMobile];
            if (faint < 4)
            {
                ++faint;
                SetFaint(playerMobile, faint, true);
            }

            logger.Debug(
                $"Foi aumentado os valores de desmaios para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
            );

            return true;
        }

        return false;
    }

    public static bool DecreaseFaint(PlayerMobile playerMobile)
    {
        if (HasFaint(playerMobile))
        {
            int faint = FaintTable[playerMobile];
            --faint;
            SetFaint(playerMobile, faint, true);


            logger.Debug(
                $"Foi diminuido os valores de desmaios para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
            );

            return true;
        }

        return false;
    }

    public static bool SetFaint(PlayerMobile playerMobile, int faint, bool overwriteExisting)
    {
        if (playerMobile == null)
        {
            return false;
        }

        if (overwriteExisting)
        {
            FaintTable[playerMobile] = faint;

            return true;
        }

        if (!HasFaint(playerMobile))
        {
            FaintTable[playerMobile] = faint;

            logger.Debug(
                $"Atribuido desmaio {faint} para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
            );

            return true;
        }

        logger.Debug(
            $"Não foi possivel setar desmaios da conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
        );
        return false;
    }

    public static int? GetPlayerFaint(Mobile mobile)
    {
        if (mobile is not PlayerMobile)
        {
            return null;
        }

        if (!HasFaint((PlayerMobile)mobile))
        {
            return null;
        }

        return FaintTable[(PlayerMobile)mobile];
    }

    public static bool HasFaint(PlayerMobile playerMobile) => FaintTable.ContainsKey(playerMobile);
}
