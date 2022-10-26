using System;
using System.Collections.Generic;
using Server.Logging;

namespace Server.Custom.Classes;

public class ClassDefinition
    : ISerializable
{
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(ClassDefinition));
    public Classes Class { get; set; }
    private Levels Level { get; set; }
    private int LifeIncremant { get; set; }
    private int PrimaryAbility { get; set; }

    private HashSet<Status> SavingThrowsProficiencie { get; set; }
    private HashSet<ArmourCategories> ArmoursProficiencie { get; set; }
    private HashSet<WeaponCategories> WeaponsProficiencie { get; set; }
    private HashSet<Feature> Features { get; set; }

    public ClassDefinition(Serial serial) => Serial = serial;

    DateTime ISerializable.Created { get; set; } = Core.Now;

    DateTime ISerializable.LastSerialized { get; set; } = DateTime.MaxValue;

    long ISerializable.SavePosition { get; set; } = -1;

    BufferWriter ISerializable.SaveBuffer { get; set; }

    public Serial Serial { get; }

    public void BeforeSerialize()
    {
    }

    public void Deserialize(IGenericReader reader)
    {
        var version = reader.ReadEncodedInt();

        switch (version)
        {
            case 1:
                {
                    goto case 0;
                }
            case 0:
                {
                    try
                    {
                        Class = reader.ReadEnum<Classes>();
                        Level = reader.ReadEnum<Levels>();
                        LifeIncremant = reader.ReadInt();
                        PrimaryAbility = reader.ReadInt();

                        SavingThrowsProficiencie = new HashSet<Status>();
                        for (int i = 0; i < reader.ReadInt(); i++)
                        {
                            SavingThrowsProficiencie.Add(reader.ReadEnum<Status>());
                        }

                        ArmoursProficiencie = new HashSet<ArmourCategories>();
                        for (int i = 0; i < reader.ReadInt(); i++)
                        {
                            ArmoursProficiencie.Add(reader.ReadEnum<ArmourCategories>());
                        }

                        WeaponsProficiencie = new HashSet<WeaponCategories>();
                        for (int i = 0; i < reader.ReadInt(); i++)
                        {
                            WeaponsProficiencie.Add(reader.ReadEnum<WeaponCategories>());
                        }

                        Features = new HashSet<Feature>();
                        for (int i = 0; i < reader.ReadInt(); i++)
                        {
                            Features.Add(reader.ReadEnum<Feature>());
                        }

                        logger.Information("Carregada informação de classe do jogador");
                    }
                    catch (Exception e)
                    {
                        logger.Error($"Erro ao carregar classe: {e.Message}");
                    }

                    break;
                }
        }
    }

    public void Serialize(IGenericWriter writer)
    {
        writer.WriteEncodedInt(0); // version

        writer.WriteEnum(Class);
        writer.WriteEnum(Level);
        writer.Write(LifeIncremant);
        writer.Write(PrimaryAbility);

        writer.Write(SavingThrowsProficiencie.Count);
        foreach (Status status in SavingThrowsProficiencie)
        {
            writer.WriteEnum(status);
        }

        writer.Write(ArmoursProficiencie.Count);
        foreach (ArmourCategories armourCategories in ArmoursProficiencie)
        {
            writer.WriteEnum(armourCategories);
        }

        writer.Write(WeaponsProficiencie.Count);
        foreach (WeaponCategories weaponCategories in WeaponsProficiencie)
        {
            writer.WriteEnum(weaponCategories);
        }

        writer.Write(Features.Count);
        foreach (Feature feature in Features)
        {
            writer.WriteEnum(feature);
        }
    }

    public bool Deleted { get; }

    public void Delete()
    {
    }
}
