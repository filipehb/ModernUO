using System;
using System.Collections.Generic;
using System.Linq;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.FaintSystem;

public class FaintPersistence
{
    private static Dictionary<PlayerMobile, Faint> FaintTable = new();
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

                    FaintTable = new Dictionary<PlayerMobile, Faint>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            var playerMobile = reader.ReadEntity<PlayerMobile>();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            FaintTable[playerMobile] = reader.ReadEntity<Faint>();

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
            Faint faint = FaintTable[playerMobile];
            if (faint.Faints < 4)
            {
                ++faint.Faints;

                FaintTable[playerMobile] = faint;
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
            Faint faint = FaintTable[playerMobile];
            --faint.Faints;

            FaintTable[playerMobile] = faint;


            logger.Debug(
                $"Foi diminuido os valores de desmaios para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
            );

            return true;
        }

        return false;
    }

    public static bool SetRecoverFaintRunning(PlayerMobile playerMobile, bool isRecoverFaintRunning)
    {
        if (HasFaint(playerMobile))
        {
            Faint faint = FaintTable[playerMobile];
            faint.isRecoverFaintRunning = isRecoverFaintRunning;
            FaintTable[playerMobile] = faint;

            logger.Debug(
                $"Foi alterado o valor de isRecoverFaintRunning para a conta {playerMobile.Account.Username} com o personagem {playerMobile.Name}"
            );

            return true;
        }

        return false;
    }

    public static bool SetFaint(PlayerMobile playerMobile, Faint faint, bool overwriteExisting)
    {
        if (playerMobile == null)
        {
            return false;
        }

        if (faint.Faints < -1)
        {
            logger.Debug(
                $"Desmaio é menor que -1 para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
            );

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
                $"Atribuido desmaio {faint.Faints} para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
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

        return FaintTable[(PlayerMobile)mobile].Faints;
    }

    public static bool? GetRunningTimer(PlayerMobile mobile)
    {
        if (mobile is null)
        {
            return null;
        }

        if (!HasFaint(mobile))
        {
            return null;
        }

        return FaintTable[mobile].isRecoverFaintRunning;
    }

    public static bool HasFaint(PlayerMobile playerMobile) => FaintTable.ContainsKey(playerMobile);
}
