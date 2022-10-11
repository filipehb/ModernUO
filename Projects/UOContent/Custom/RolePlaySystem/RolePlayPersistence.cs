using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.RolePlaySystem;

public class RolePlayPersistence
{
    private static Dictionary<PlayerMobile, Dictionary<PlayerMobile, int>> RolePlayTable = new();
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(RolePlayPersistence));

    public static void Configure()
    {
        GenericPersistence.Register("RolePlayPoints", Serialize, Deserialize);
    }

    public static void Serialize(IGenericWriter writer)
    {
        // Do serialization here
        writer.WriteEncodedInt(0); // version

        writer.Write(RolePlayTable.Count);

        foreach (var kvp in RolePlayTable)
        {
            writer.Write(kvp.Key);
            foreach (var kvpInner in kvp.Value)
            {
                writer.Write(kvpInner.Key);
                writer.Write(kvpInner.Value);
            }
        }

        logger.Information("Salvo informações de RolePlayPoints");
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

                    RolePlayTable = new Dictionary<PlayerMobile, Dictionary<PlayerMobile, int>>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            var playerMobile = reader.ReadEntity<PlayerMobile>();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            var RolePlayTableInner = new Dictionary<PlayerMobile, int>
                            {
                                [reader.ReadEntity<PlayerMobile>()] = reader.ReadInt()
                            };

                            RolePlayTable[playerMobile] = RolePlayTableInner;

                            logger.Information("Carregada informações de RolePlayPoints");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Erro ao carregar RolePlayPoints: {e.Message}");
                    }

                    break;
                }
        }
    }

    public static bool SetRolePlayPoint(PlayerMobile from, PlayerMobile target, int point, bool overwriteExisting)
    {
        if (from == null || target == null)
        {
            return false;
        }

        if (overwriteExisting)
        {
            var RolePlayTableInner = new Dictionary<PlayerMobile, int>
            {
                [from] = point
            };

            RolePlayTable[target] = RolePlayTableInner;

            return true;
        }

        if (!HasFaintRunning(target))
        {
            var RolePlayTableInner = new Dictionary<PlayerMobile, int>
            {
                [from] = point
            };

            RolePlayTable[target] = RolePlayTableInner;

            logger.Debug(
                $"Atribuido RolePlayPoint {point} para a conta {target.Account.Username} para o personagem {target.Name}"
            );

            return true;
        }

        logger.Debug(
            $"Não foi possivel setar RolePlayPoint da conta {target.Account.Username} para o personagem {target.Name}"
        );
        return false;
    }

    public static int? GetPlayerRolePlayPoint(Mobile mobile)
    {
        if (mobile is not PlayerMobile)
        {
            return null;
        }

        if (!HasFaintRunning((PlayerMobile)mobile))
        {
            return null;
        }

        var count = RolePlayTable.Count;
        var points = 0;

        foreach (var kvp in RolePlayTable)
        {
            foreach (var kvpInner in kvp.Value)
            {
                points += kvpInner.Value;
            }
        }

        return points/count;
    }

    public static bool HasFaintRunning(PlayerMobile playerMobile) => RolePlayTable.ContainsKey(playerMobile);
}
