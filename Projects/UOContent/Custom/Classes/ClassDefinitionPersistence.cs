using System;
using System.Collections.Generic;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.Classes;

public class ClassDefinitionPersistence
{
    private static Dictionary<PlayerMobile, ClassDefinition> ClassTable = new();
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ClassDefinitionPersistence));

    public static void Configure()
    {
        GenericPersistence.Register("ClassDefinitionPersistence", Serialize, Deserialize);
    }

    public static void Serialize(IGenericWriter writer)
    {
        // Do serialization here
        writer.WriteEncodedInt(0); // version

        writer.Write(ClassTable.Count);

        foreach (var kvp in ClassTable)
        {
            writer.Write(kvp.Key);
            writer.Write(kvp.Value);
        }

        logger.Information("Salvo informações de classes");
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

                    ClassTable = new Dictionary<PlayerMobile, ClassDefinition>();

                    try
                    {
                        for (int i = 0; i < tableCount; i++)
                        {
                            var playerMobile = reader.ReadEntity<PlayerMobile>();
                            if (playerMobile == null || playerMobile.Deleted)
                            {
                                continue;
                            }

                            var classDefinition = reader.ReadEntity<ClassDefinition>();
                            if (classDefinition == null)
                            {
                                continue;
                            }

                            ClassTable[playerMobile] = classDefinition;

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

    public static bool SetClass(PlayerMobile playerMobile, ClassDefinition playerClass, bool overwriteExisting)
    {
        if (playerMobile == null)
        {
            return false;
        }

        if (overwriteExisting)
        {
            ClassTable[playerMobile] = playerClass;

            return true;
        }

        if (!HasClass(playerMobile))
        {
            ClassTable[playerMobile] = playerClass;

            logger.Debug($"Atribuida classe {playerClass.Class} para a conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}");

            return true;
        }

        logger.Debug(
            $"Não foi possivel setar a classe da conta {playerMobile.Account.Username} para o personagem {playerMobile.Name}"
        );
        return false;
    }

    public static ClassDefinition GetPlayerClass(Mobile mobile)
    {
        if (mobile is not PlayerMobile)
        {
            return new ClassDefinition((Serial)System.Random.Shared.NextInt64());
        }

        if (!HasClass((PlayerMobile)mobile))
        {
            return new ClassDefinition((Serial)System.Random.Shared.NextInt64());
        }

        return ClassTable[(PlayerMobile)mobile];
    }

    public static bool HasClass(PlayerMobile playerMobile) => ClassTable.ContainsKey(playerMobile);
}
