using System;
using System.Collections.Generic;
using System.Linq;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.RolePlaySystem;

public class RolePlayPersistence : GenericPersistence
{
    private static RolePlayPersistence _rolePlayPersistence;
    private static Dictionary<PlayerMobile, List<Dictionary<PlayerMobile, int>>> RolePlayTable = new();
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(RolePlayPersistence));

    public static void Configure()
    {
        _rolePlayPersistence = new RolePlayPersistence();
    }

    public override void Serialize(IGenericWriter writer)
    {
        // Do serialization here
        writer.WriteEncodedInt(0); // version

        writer.Write(RolePlayTable.Count);

        foreach (var kvp in RolePlayTable)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value.Count); //Tamanho da lista com as notas
            foreach (var listDictionary in kvp.Value)
            {
                foreach (var kvpInner in listDictionary)
                {
                    writer.Write(kvpInner.Key);
                    writer.Write(kvpInner.Value);
                }
            }
        }

        logger.Information("Salvo informações de RolePlayRate");
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

                    RolePlayTable = new Dictionary<PlayerMobile, List<Dictionary<PlayerMobile, int>>>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            var playerMobile = reader.ReadEntity<PlayerMobile>();
                            int listOfRatesCount = reader.ReadInt();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            var listRates = new List<Dictionary<PlayerMobile, int>>();

                            for (int index = 0; index < listOfRatesCount; index++)
                            {
                                var RolePlayTableInner = new Dictionary<PlayerMobile, int>
                                {
                                    [reader.ReadEntity<PlayerMobile>()] = reader.ReadInt()
                                };
                                listRates.Add(RolePlayTableInner);
                            }

                            RolePlayTable[playerMobile] = listRates;
                            logger.Debug(
                                $"Carregada informações de RolePlayRate para a conta {playerMobile.Account.Username}"
                            );
                        }

                        logger.Information("Carregada informações de RolePlayRate");
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Erro ao carregar RolePlayRate: {e.Message}");
                    }

                    break;
                }
        }
    }

    public static bool SetRolePlayRate(PlayerMobile from, PlayerMobile target, int rate)
    {
        if (from == null || target == null)
        {
            return false;
        }

        if (rate > 5 || rate < 0)
        {
            logger.Debug(
                $"Atribuido valor inválido de RolePlayRate {rate} para a conta {target.Account.Username} para o personagem {target.Name}"
            );
            return false;
        }

        if (!HasRolePlayRate(target))
        {
            var list = new List<Dictionary<PlayerMobile, int>>();
            RolePlayTable.Add(target, list);

            logger.Debug(
                $"Atribuido RolePlayRate para a conta {target.Account.Username} para o personagem {target.Name}"
            );
        }

        var RolePlayTableInner = new Dictionary<PlayerMobile, int>
        {
            [from] = rate
        };

        foreach (Dictionary<PlayerMobile, int> dictionary in RolePlayTable[target])
        {
            if (dictionary.ContainsKey(from))
            {
                dictionary[from] = rate;
                logger.Debug(
                    $"Trocado o valor RolePlayRate {rate} para a conta {target.Account.Username} para o personagem {target.Name}"
                );

                return true;
            }
        }

        RolePlayTable[target].Add(RolePlayTableInner);

        logger.Debug(
            $"Atribuido RolePlayRate {rate} para a conta {target.Account.Username} para o personagem {target.Name}"
        );

        return true;
    }

    public static int GetPlayerRolePlayRate(PlayerMobile mobile)
    {
        if (mobile is not not null)
        {
            return 0;
        }

        if (!HasRolePlayRate(mobile))
        {
            return 0;
        }

        var points = 0;

        foreach (var kvp in RolePlayTable)
        {
            if (kvp.Key == mobile)
            {
                var count = kvp.Value.Count;
                points += kvp.Value.SelectMany(listDictionary => listDictionary).Sum(kvpInner => kvpInner.Value);
                return points / count;
            }
        }

        return 0;
    }

    public static bool HasRolePlayRate(PlayerMobile playerMobile) => RolePlayTable.ContainsKey(playerMobile);

    public RolePlayPersistence() : base("RolePlayRate", 10)
    {
    }
}
