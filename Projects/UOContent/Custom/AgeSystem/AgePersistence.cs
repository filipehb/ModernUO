using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.AgeSystem;

public class AgePersistence
{
    private static Dictionary<PlayerMobile, int> AgeTable = new();
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(AgePersistence));

    public static void Configure()
    {
        GenericPersistence.Register("Age", Serialize, Deserialize);
    }

    public static void Serialize(IGenericWriter writer)
    {
        // Do serialization here
        writer.WriteEncodedInt(0); // version

        writer.Write(AgeTable.Count);

        foreach (var kvp in AgeTable)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }

        logger.Information("Salvo informações de idades");
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

                    AgeTable = new Dictionary<PlayerMobile, int>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            var playerMobile = reader.ReadEntity<PlayerMobile>();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            AgeTable[playerMobile] = reader.ReadInt();

                            logger.Information("Carregada informações de idades");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Erro ao carregar idades: {e.Message}");
                    }

                    break;
                }
        }
    }

    public static bool SetAge(PlayerMobile playerMobile, int age, bool overwriteExisting)
    {
        if (playerMobile == null)
        {
            return false;
        }

        if (age < 16)
        {
            logger.Debug($"Idade é menor que 16 anos para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}");

            return false;
        }

        if (overwriteExisting)
        {
            AgeTable[playerMobile] = age;

            return true;
        }

        if (!HasAge(playerMobile))
        {
            AgeTable[playerMobile] = age;

            logger.Debug($"Atribuida idade {age} para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}");

            return true;
        }

        logger.Debug(
            $"Não foi possivel setar a idade da conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
        );
        return false;
    }

    public static void IncreaseAges()
    {
        foreach (KeyValuePair<PlayerMobile,int> keyValuePair in AgeTable)
        {
            int age = keyValuePair.Value;
            SetAge(keyValuePair.Key, ++age, true);
        }
    }

    public static int GetPlayerAge(Mobile mobile)
    {
        if (mobile is not PlayerMobile)
        {
            return 0;
        }

        if (!HasAge((PlayerMobile)mobile))
        {
            return 0;
        }

        return AgeTable[(PlayerMobile)mobile];
    }

    public static bool HasAge(PlayerMobile playerMobile) => AgeTable.ContainsKey(playerMobile);
}
