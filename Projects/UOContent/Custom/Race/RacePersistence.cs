using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.Race;

public class RacePersistence : GenericPersistence
{
    private static RacePersistence _racePersistence;
    private static Dictionary<PlayerMobile, Races> RaceTable = new();
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(RacePersistence));


    public static void Configure()
    {
        _racePersistence = new RacePersistence();
    }

    public override void Serialize(IGenericWriter writer)
    {
        // Do serialization here
        writer.WriteEncodedInt(0); // version

        writer.Write(RaceTable.Count);

        foreach (var kvp in RaceTable)
        {
            writer.Write(kvp.Key);
            writer.WriteEnum(kvp.Value);
        }

        logger.Information("Salvo informações de raças");
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

                    RaceTable = new Dictionary<PlayerMobile, Races>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            var playerMobile = reader.ReadEntity<PlayerMobile>();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            RaceTable[playerMobile] = reader.ReadEnum<Races>();

                            logger.Information("Carregada informações de raças");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Erro ao carregar raças: {e.Message}");
                    }

                    break;
                }
        }
    }

    public static bool HasRace(PlayerMobile playerMobile) => RaceTable.ContainsKey(playerMobile);

    public static bool SetRace(PlayerMobile playerMobile, Races race, bool overwriteExisting)
    {
        if (playerMobile == null)
        {
            return false;
        }

        if (overwriteExisting)
        {
            RaceTable[playerMobile] = race;

            return true;
        }

        if (!HasRace(playerMobile))
        {
            RaceTable[playerMobile] = race;

            logger.Debug($"Atribuida raça {race} para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}");

            return true;
        }

        logger.Debug(
            $"Não foi possivel setar a raça da conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
        );
        return false;
    }

    public static Races GetPlayerRace(Mobile mobile)
    {
        if (mobile is not PlayerMobile)
        {
            return Races.None;
        }

        if (!HasRace((PlayerMobile)mobile))
        {
            return Races.None;
        }

        return RaceTable[(PlayerMobile)mobile];
    }

    public RacePersistence() : base("Race", 10)
    {
    }
}
