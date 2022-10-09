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
        return Math.Truncate((AgeInDays(dataCriacao) % 365) / 30);
    }

    public static double AgeInMonthsWithoutTrucate(DateTime dataCriacao)
    {
        return (AgeInDays(dataCriacao) % 365) / 30;
    }

    public static string GenerateDeath()
    {
        var random = new System.Random();
        var resultList = new List<string>
        {
            "Uma flecha atingiu seu peito e voce morreu", "Voce pisou onde nao devia e morreu",
            "Algo caiu dos ceus e matou voce", "Voce faleceu de causas naturais",
            "Voce sofreu uma combustao espontanea", "Lembra daquela unha encravada?"
        };
        int index = random.Next(resultList.Count);
        return resultList[index];
    }

    public static bool IsLifeLimit(PlayerMobile playerMobile)
    {
        /*switch (playerMobile.RaceCustom)
        {
            case RacesCustom.Anao:
                {
                    //Criança 01-14
                    //Jovem 15-29
                    //Adulto 30-174
                    //Velho 175-219
                    //Idoso +220
                    playerMobile.Age = AgeInMonthsWithoutTrucate(playerMobile.CreationTime) * 100;
                    return playerMobile.Age >= 220;
                }
                break;
            case RacesCustom.Hobbit:
                {
                    //Criança 01-09
                    //Jovem 10-32
                    //Adulto 33-79
                    //Velho 80-90
                    //Idoso +91
                    playerMobile.Age = AgeInMonthsWithoutTrucate(playerMobile.CreationTime) * 100;
                    return playerMobile.Age >= 90;
                    return AgeInMonths(playerMobile.CreationTime) >= 2;
                }
                break;
            case RacesCustom.Humano:
                {
                    //Criança 01-09
                    //Jovem 10-15
                    //Adulto 16-44
                    //Velho 45-59
                    //Idoso +55
                    playerMobile.Age += AgeInMonthsWithoutTrucate(playerMobile.CreationTime) * 25;
                    return playerMobile.Age >= 50;
                    return AgeInMonths(playerMobile.CreationTime) >= 2;
                }
                break;
            case RacesCustom.Elfo:
                {
                    //Imortais para mortes naturais, talvez pensar em algo sobre isso
                    playerMobile.Age = AgeInMonthsWithoutTrucate(playerMobile.CreationTime) * 100;
                    return false;
                }
                break;
            case Races.Orc:
                {
                    return AgeInMonths(playerMobile.CreationTime) >= 1;
                }
            default:
                return false;
        }*/
        return false;
    }
}
