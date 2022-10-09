using System;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.FaintSystem;

public class FaintRecoverTimer : Timer
{
    private readonly PlayerMobile _playerMobile;
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(FaintRecoverTimer));

    public FaintRecoverTimer(PlayerMobile playerMobile) : base(TimeSpan.FromMinutes(15.0), TimeSpan.FromSeconds(15.0))
    {
        _playerMobile = playerMobile;
    }

    protected override void OnTick()
    {
        if (FaintPersistence.GetPlayerFaint(_playerMobile) >= 4 || FaintPersistence.GetPlayerFaint(_playerMobile) == -1)
        {
            logger.Debug($"Timer de Faint foi parado para o player {_playerMobile.Account.Username} com o personagem {_playerMobile.Name}");
            FaintPersistence.SetRecoverFaintRunning(_playerMobile, false);
            Stop();
        }

        FaintPersistence.IncreaseFaint(_playerMobile);
    }
}
