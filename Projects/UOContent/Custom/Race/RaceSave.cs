using System;
using System.Collections.Generic;
using System.IO;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.Race;

public class RaceSave
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(RaceSave));
    private static readonly string SavePath = "Saves/Race";
    private static readonly string SaveFile = Path.Combine(SavePath, "race.bin");
    public static Dictionary<PlayerMobile, Races> RaceTable { get; private set; }

    public static void Configure()
    {
        RaceTable = new Dictionary<PlayerMobile, Races>();
        EventSink.WorldLoad += EventSink_WorldLoad;
    }

    public static void Initialize()
    {
        EventSink.WorldSave += Event_WorldSave;
    }

    private static void Event_WorldSave()
    {
        try
        {
            if (!Directory.Exists(SavePath))
            {
                Directory.CreateDirectory(SavePath);
            }

            BinaryFileWriter writer = new BinaryFileWriter(SaveFile, true);

            Serialize(writer);

            writer.Close();
            writer.Dispose();
        }

        catch (ArgumentException e)
        {
            logger.Error($"Race load Event_WorldSave Failed {e.Message}");
        }
    }

    private static void EventSink_WorldLoad()
    {
        if (!File.Exists(SaveFile))
        {
            return;
        }

        try
        {
            using FileStream stream = new FileStream(SaveFile, FileMode.Open, FileAccess.Read, FileShare.Read);
            BinaryFileReader reader = new BinaryFileReader(new BinaryReader(stream));
            Deserialize(reader);
            reader.Close();
        }

        catch (ArgumentException e)
        {
            logger.Error($"Race Save Event_WorldLoad Failed: {e.Message}");
        }
    }

    public static void Deserialize(BinaryFileReader reader)
    {
        int version = reader.ReadInt();

        switch (version)
        {
            case 1:
                {
                    goto case 0;
                }
            case 0:
                {
                    int tableCount = reader.ReadInt();

                    PlayerMobile playerMobile;

                    RaceTable = new Dictionary<PlayerMobile, Races>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            playerMobile = reader.ReadEntity<PlayerMobile>();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            RaceTable[playerMobile] = (Races)reader.ReadInt();

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

    public static void Serialize(BinaryFileWriter writer)
    {
        writer.Write(0); //Version
        writer.Write(RaceTable.Count);
        foreach (var kvp in RaceTable)
        {
            writer.Write(kvp.Key);
            writer.Write((int)kvp.Value);
        }

        logger.Information("Salvo informações de raças");
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

        if(!HasRace(playerMobile))
        {
            RaceTable[playerMobile] = race;

            return true;
        }

        logger.Information($"Não foi possivel setar a raça do player {playerMobile.Account.Username}");
        return false;
    }

    public static Races? GetPlayerRace(Mobile mobile)
    {
        if (mobile is not PlayerMobile)
        {
            return null;
        }

        if (!HasRace((PlayerMobile)mobile))
        {
            return null;
        }

        return RaceTable[(PlayerMobile)mobile];
    }
}
