using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.FaintSystem;

public class FaintAction
{
    public static void Initialize()
    {
        EventSink.PlayerDeath += OnDeath;
        EventSink.Disconnected += OnDisconnect;
        EventSink.Login += OnLogin;
        EventSink.BandageTargetRequest += OnBandageTarget;
    }

    private static void OnBandageTarget(Mobile mobile, Item item, Mobile target)
    {
        if (target is PlayerMobile { AccessLevel: AccessLevel.Player } playerMobile &&
            FaintPersistence.GetPlayerFaint(playerMobile) > 0 && !playerMobile.Alive)
        {
            // TODO Fazer aqui a logica de reviver o player
        }
    }

    private static void OnLogin(Mobile obj)
    {
        if (obj is PlayerMobile { AccessLevel: AccessLevel.Player } playerMobile &&
            FaintPersistence.GetPlayerFaint(playerMobile) >= 0)
        {
            if (!FaintTimerPersistence.GetPlayerFaintRunning(playerMobile))
            {
                FaintRecoverTimer faintRecoverTimer = new FaintRecoverTimer(playerMobile);
                FaintTimerPersistence.SetFaintRunning(playerMobile, true, true);
                faintRecoverTimer.Start();
            }

            if (!playerMobile.Alive)
            {
                FaintTimer faintTimer = new FaintTimer(playerMobile);
                faintTimer.Start();
            }
        }
    }

    private static void OnDisconnect(Mobile obj)
    {
        if (obj is PlayerMobile { AccessLevel: AccessLevel.Player } playerMobile &&
            FaintPersistence.GetPlayerFaint(playerMobile) >= 0 && !playerMobile.Alive)
        {
            FaintTimerPersistence.SetFaintRunning(playerMobile, false, true);
        }
    }

    private static void OnDeath(Mobile obj)
    {
        if (obj is PlayerMobile { AccessLevel: AccessLevel.Player } playerMobile && FaintPersistence.GetPlayerFaint(playerMobile) > 0) //Se morrer com 0 pontos, é a morte definitiva
        {
            Corpse playerMobileCorpse = playerMobile.Corpse as Corpse;
            // O corpo vai sumir por padrão após 5 dias, assim evitando corpos espalhados pelo mapa
            playerMobileCorpse?.BeginDecay(TimeSpan.FromDays(5));

            FaintPersistence.DecreaseFaint(playerMobile);


            if (!FaintTimerPersistence.GetPlayerFaintRunning(playerMobile))
            {
                FaintRecoverTimer faintRecoverTimer = new FaintRecoverTimer(playerMobile);
                FaintTimerPersistence.SetFaintRunning(playerMobile, true, true);
                faintRecoverTimer.Start();
            }

            FaintTimer faintTimer = new FaintTimer(playerMobile);
            faintTimer.Start();

            // TODO Mover o player morto para algum outro local no mapa
            // playerMobile.MoveToWorld(playerMobileLocation, Map.Felucca);
        }
    }
}
