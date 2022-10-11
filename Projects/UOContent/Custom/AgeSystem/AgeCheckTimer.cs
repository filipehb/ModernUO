using System;
using Server.Custom.FaintSystem;
using Server.Mobiles;
using Server.Network;

namespace Server.Custom.AgeSystem;

public class AgeCheckTimer : Timer
{
    public AgeCheckTimer() : base(TimeSpan.FromHours(24), TimeSpan.FromHours(24))
    {
    }

    public static void Initialize()
    {
        new AgeCheckTimer().Start();
    }

    protected override void OnTick()
    {
        CheckAge();
    }

    private void CheckAge()
    {
        var isGoingToDie = Utility.RandomBool();
        foreach (NetState state in TcpServer.Instances)
        {
            if (state.Mobile == null)
            {
                continue;
            }

            if (state.Mobile.AccessLevel == AccessLevel.Player && state.Mobile.Alive && isGoingToDie &&
                AgeUtils.IsLifeLimit(state.Mobile as PlayerMobile))
            {
                if (state.Mobile is PlayerMobile playerMobile)
                {
                    playerMobile.SendMessage(38, "Seu tempo por essas terras chegou ao fim");
                    playerMobile.SendMessage(38, AgeUtils.GenerateDeath());
                    playerMobile.Kill();
                    FaintPersistence.SetFaint(playerMobile, -1, true);
                }
            }
            else
            {
                AgePersistence.IncreaseAges();
            }
        }
    }
}
