using System;
using Server.Logging;
using Server.Mobiles;

namespace Server.Custom.FaintSystem;

public class FaintTimer : Timer
{
    private readonly PlayerMobile _playerMobile;
    private static readonly ILogger logger = LogFactory.GetLogger(typeof(FaintTimer));

    public FaintTimer(PlayerMobile playerMobile) : base(TimeSpan.FromMinutes(5.0), TimeSpan.FromSeconds(5.0))
    {
        _playerMobile = playerMobile;
    }

    protected override void OnTick()
    {
        //TODO Implementar a parte de reviver a pessoa
        base.OnTick();
    }
}
