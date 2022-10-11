using System;
using System.Collections.Generic;
using Server.Custom.Race;
using Server.Mobiles;

namespace Server.Custom.AgeSystem;

public class AgeUtils
{
    public static double AgeInDays(DateTime creationDate)
    {
        return (DateTime.UtcNow - creationDate).TotalDays;
    }

    public static double AgeInMonths(DateTime dataCriacao)
    {
        return Math.Truncate(AgeInDays(dataCriacao) % 365 / 30);
    }

    public static double AgeInMonthsWithoutTrucate(DateTime dataCriacao)
    {
        return AgeInDays(dataCriacao) % 365 / 30;
    }

    public static string GenerateDeath()
    {
        var random = new System.Random();
        var resultList = new List<string>
        {
            "Uma flecha atingiu seu peito e você morreu", "Você pisou onde não devia e morreu",
            "Algo caiu dos céus e foi bem em você", "Você faleceu de causas naturais",
            "você sofreu uma combustão espontânea", "Lembra daquela unha encravada? Você morreu"
        };
        int index = random.Next(resultList.Count);
        return resultList[index];
    }

    public static bool IsLifeLimit(PlayerMobile playerMobile)
    {
        switch (RacePersistence.GetPlayerRace(playerMobile))
        {
            //TODO Precisa rever essas idades
            case Races.Dwarve:
                {
                    //Criança 01-14
                    //Jovem 15-29
                    //Adulto 30-174
                    //Velho 175-219
                    //Idoso +220
                    int age = (int)AgePersistence.GetPlayerAge(playerMobile);
                    return age >= 120;
                }
                break;
            case Races.Hobbit:
                {
                    //Criança 01-09
                    //Jovem 10-32
                    //Adulto 33-79
                    //Velho 80-90
                    //Idoso +91
                    int age = (int)AgePersistence.GetPlayerAge(playerMobile);
                    return age >= 90;
                }
                break;
            case Races.Human:
                {
                    //Criança 01-09
                    //Jovem 10-15
                    //Adulto 16-44
                    //Velho 45-59
                    //Idoso +55
                    int age = (int)AgePersistence.GetPlayerAge(playerMobile);
                    return age >= 60;
                }
                break;
            case Races.Elve:
                {
                    //Imortais para mortes naturais, talvez pensar em algo sobre isso
                    return false;
                }
                break;
            case Races.Uruk:
                {
                    return false;
                }
            default:
                return false;
        }
    }
}
