using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.Classes;

public class ClassPersistence : GenericPersistence
{
    private static ClassPersistence _classPersistence;
    private static Dictionary<PlayerMobile, Classes> ClassTable = new();
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ClassPersistence));

    public static void Configure()
    {
        _classPersistence = new ClassPersistence();
    }

    public ClassPersistence() : base("Class", 10)
    {
    }

    public override void Serialize(IGenericWriter writer)
    {
        // Do serialization here
        writer.WriteEncodedInt(0); // version

        writer.Write(ClassTable.Count);

        foreach (var kvp in ClassTable)
        {
            writer.Write(kvp.Key);
            writer.WriteEnum(kvp.Value);
        }

        logger.Information("Salvo informações de classes");
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

                    ClassTable = new Dictionary<PlayerMobile, Classes>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            var playerMobile = reader.ReadEntity<PlayerMobile>();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            ClassTable[playerMobile] = reader.ReadEnum<Classes>();

                            logger.Information("Carregada informações de classes");
                        }
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Erro ao carregar classes: {e.Message}");
                    }

                    break;
                }
        }
    }

    public static bool SetClass(PlayerMobile playerMobile, Classes classes, bool overwriteExisting)
    {
        if (playerMobile == null)
        {
            return false;
        }

        if (overwriteExisting)
        {
            ClassTable[playerMobile] = classes;

            return true;
        }

        if (!HasClass(playerMobile))
        {
            ClassTable[playerMobile] = classes;

            logger.Debug($"Atribuida classe {classes} para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}");

            return true;
        }

        logger.Debug(
            $"Não foi possivel setar a classe da conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
        );
        return false;
    }

    public static Classes GetPlayerClass(Mobile mobile)
    {
        if (mobile is not PlayerMobile)
        {
            return Classes.None;
        }

        if (!HasClass((PlayerMobile)mobile))
        {
            return Classes.None;
        }

        return ClassTable[(PlayerMobile)mobile];
    }

    public static bool HasClass(PlayerMobile playerMobile) => ClassTable.ContainsKey(playerMobile);
}
