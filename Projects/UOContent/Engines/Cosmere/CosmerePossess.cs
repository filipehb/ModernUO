using System;
using System.Collections.Generic;
using Server.Mobiles;
using Server.Network;

namespace Server.Engines.Cosmere;

public static class CosmerePossess
{
    private static readonly Dictionary<Serial, PossessSession> _byStaff = new();
    private static readonly Dictionary<Serial, PossessSession> _byCreature = new();
    private static int _swapDepth;

    public static void Configure()
    {
        EventSink.BeforeDisconnected += OnBeforeDisconnected;
    }

    public static bool IsPossessing(PlayerMobile staff) =>
        staff != null && _byStaff.ContainsKey(staff.Serial);

    public static bool IsPossessed(BaseCreature creature) =>
        creature != null && _byCreature.ContainsKey(creature.Serial);

    public static bool IsSwapInProgress => _swapDepth > 0;

    public static bool TryValidateTarget(Mobile target, out BaseCreature creature, out string reason)
    {
        creature = null;
        reason = null;

        if (target == null || target.Deleted)
        {
            reason = "Invalid target.";
            return false;
        }

        if (target is PlayerMobile)
        {
            reason = "You cannot possess players.";
            return false;
        }

        if (target is not BaseCreature bc)
        {
            reason = "You can only possess creatures.";
            return false;
        }

        if (bc.Controlled || bc.Summoned)
        {
            reason = "You cannot possess player pets or summons.";
            return false;
        }

        if (bc is IVendor)
        {
            reason = "You cannot possess vendor NPCs.";
            return false;
        }

        if (bc.Map == null || bc.Map == Map.Internal)
        {
            reason = "That creature is not in the world.";
            return false;
        }

        if (IsPossessed(bc))
        {
            reason = "That creature is already possessed.";
            return false;
        }

        creature = bc;
        return true;
    }

    public static bool TryPossess(PlayerMobile staff, BaseCreature creature, out string reason)
    {
        reason = null;

        if (!CosmereConfig.PossessEnabled)
        {
            reason = "Possess is disabled on this shard.";
            return false;
        }

        if (staff?.Deleted != false || staff.AccessLevel < AccessLevel.GameMaster)
        {
            reason = "Insufficient access.";
            return false;
        }

        var ns = staff.NetState;
        if (ns == null)
        {
            reason = "You are not connected.";
            return false;
        }

        if (IsPossessing(staff))
        {
            reason = "Already possessing. Use Unpossess first.";
            return false;
        }

        if (!TryValidateTarget(creature, out var target, out reason))
        {
            return false;
        }

        creature = target;

        var staffSnap = StaffSnapshot.Capture(staff);
        var npcSnap = NpcSnapshot.Capture(creature);

        if (staff.Mounted)
        {
            staff.Mount?.Rider = null;
        }

        // Pause AI without Deactivate() — that can return spawners home.
        creature.AIObject?.AITimer.Stop();
        creature.Combatant = null;
        creature.Warmode = false;

        staff.Internalize();

        _swapDepth++;
        try
        {
            staff.NetState = null;
            ns.Mobile = creature;
            creature.NetState = ns;
        }
        finally
        {
            _swapDepth--;
        }

        var session = new PossessSession
        {
            Staff = staff,
            Creature = creature,
            StaffSnap = staffSnap,
            NpcSnap = npcSnap,
            LastNpcLocation = creature.Location,
            LastNpcMap = creature.Map
        };

        _byStaff[staff.Serial] = session;
        _byCreature[creature.Serial] = session;

        creature.SendEverything();
        creature.SendMessage("You are possessing this creature. Use [Unpossess] to exit.");
        return true;
    }

    public static bool TryUnpossess(Mobile from, out string reason)
    {
        reason = null;

        if (!TryFindSession(from, out var session))
        {
            reason = "You are not possessing anyone.";
            return false;
        }

        return CompleteUnpossess(session, reconnect: true, out reason);
    }

    /// <summary>Disconnect path: restore world state; do not reattach a dying NetState.</summary>
    public static void HandleDisconnect(Mobile m)
    {
        if (_swapDepth > 0 || m == null)
        {
            return;
        }

        if (!TryFindSession(m, out var session))
        {
            return;
        }

        CompleteUnpossess(session, reconnect: false, out _);
    }

    public static bool IsInPossessSession(Mobile m) => TryFindSession(m, out _);

    private static bool TryFindSession(Mobile m, out PossessSession session)
    {
        session = null;
        if (m == null)
        {
            return false;
        }

        if (m is PlayerMobile pm && _byStaff.TryGetValue(pm.Serial, out session))
        {
            return true;
        }

        if (m is BaseCreature && _byCreature.TryGetValue(m.Serial, out session))
        {
            return true;
        }

        return false;
    }

    private static bool CompleteUnpossess(PossessSession session, bool reconnect, out string reason)
    {
        reason = null;
        var staff = session.Staff;
        var creature = session.Creature;

        _byStaff.Remove(session.Staff.Serial);
        if (creature != null)
        {
            _byCreature.Remove(creature.Serial);
        }

        var exitMap = session.LastNpcMap;
        var exitLoc = session.LastNpcLocation;

        if (creature is { Deleted: false })
        {
            exitMap = creature.Map ?? exitMap;
            exitLoc = creature.Location;
            RestoreNpc(creature, session.NpcSnap);
        }
        else
        {
            reason = "Possessed creature was lost; restoring your body nearby.";
        }

        if (exitMap == null || exitMap == Map.Internal)
        {
            exitMap = Map.Felucca;
            exitLoc = new Point3D(1432, 1697, 2); // Britain fallback
        }

        exitLoc = FindExitLocation(exitMap, exitLoc);

        if (staff is { Deleted: false })
        {
            RestoreStaffExceptPlacement(staff, session.StaffSnap);
            staff.Hidden = true;
            staff.MoveToWorld(exitLoc, exitMap);

            if (reconnect)
            {
                var ns = creature is { Deleted: false } ? creature.NetState : null;

                if (ns != null)
                {
                    _swapDepth++;
                    try
                    {
                        if (creature is { Deleted: false })
                        {
                            creature.NetState = null;
                        }

                        ns.Mobile = staff;
                        staff.NetState = ns;
                    }
                    finally
                    {
                        _swapDepth--;
                    }

                    staff.SendEverything();
                    staff.SendMessage(
                        string.IsNullOrEmpty(reason)
                            ? "You unpossess and stand hidden beside the body."
                            : reason
                    );
                }
            }
            else
            {
                // Disconnect: body is placed Hidden for the next login; NetState is dying.
                if (creature is { Deleted: false } && creature.NetState != null)
                {
                    _swapDepth++;
                    try
                    {
                        creature.NetState = null;
                    }
                    finally
                    {
                        _swapDepth--;
                    }
                }
            }
        }

        return true;
    }

    private static void RestoreNpc(BaseCreature creature, NpcSnapshot snap)
    {
        creature.Frozen = snap.Frozen;
        creature.CantWalk = snap.CantWalk;
        creature.Warmode = snap.Warmode;
        creature.Combatant = snap.Combatant is { Deleted: false } c ? c : null;
        creature.BardPacified = snap.BardPacified;

        if (snap.AiWasRunning)
        {
            creature.AIObject?.Activate();
        }
        else
        {
            creature.AIObject?.AITimer.Stop();
        }
    }

    private static void RestoreStaffExceptPlacement(PlayerMobile staff, StaffSnapshot snap)
    {
        staff.Direction = snap.Direction;
        staff.Blessed = snap.Blessed;
        staff.Warmode = snap.Warmode;
        staff.Combatant = snap.Combatant is { Deleted: false } c ? c : null;
        // Hidden forced true by caller; Map/Location forced to exit.
    }

    private static Point3D FindExitLocation(Map map, Point3D loc)
    {
        if (map.CanSpawnMobile(loc))
        {
            return loc;
        }

        ReadOnlySpan<(int x, int y)> offsets =
        [
            (0, 1), (1, 0), (0, -1), (-1, 0),
            (1, 1), (1, -1), (-1, 1), (-1, -1)
        ];

        for (var i = 0; i < offsets.Length; i++)
        {
            var tryLoc = new Point3D(loc.X + offsets[i].x, loc.Y + offsets[i].y, loc.Z);
            if (map.CanSpawnMobile(tryLoc))
            {
                return tryLoc;
            }
        }

        return loc;
    }

    private static void OnBeforeDisconnected(Mobile m) => HandleDisconnect(m);

    private sealed class PossessSession
    {
        public PlayerMobile Staff { get; init; }
        public BaseCreature Creature { get; init; }
        public StaffSnapshot StaffSnap { get; init; }
        public NpcSnapshot NpcSnap { get; init; }
        public Point3D LastNpcLocation { get; init; }
        public Map LastNpcMap { get; init; }
    }

    private readonly struct StaffSnapshot
    {
        public Map Map { get; init; }
        public Point3D Location { get; init; }
        public Direction Direction { get; init; }
        public bool Hidden { get; init; }
        public bool Blessed { get; init; }
        public bool Warmode { get; init; }
        public Mobile Combatant { get; init; }

        public static StaffSnapshot Capture(PlayerMobile staff) =>
            new()
            {
                Map = staff.Map,
                Location = staff.Location,
                Direction = staff.Direction,
                Hidden = staff.Hidden,
                Blessed = staff.Blessed,
                Warmode = staff.Warmode,
                Combatant = staff.Combatant
            };
    }

    private readonly struct NpcSnapshot
    {
        public bool AiWasRunning { get; init; }
        public Mobile Combatant { get; init; }
        public bool Warmode { get; init; }
        public bool Frozen { get; init; }
        public bool CantWalk { get; init; }
        public bool BardPacified { get; init; }

        public static NpcSnapshot Capture(BaseCreature creature) =>
            new()
            {
                AiWasRunning = creature.AIObject?.AITimer.Running == true,
                Combatant = creature.Combatant,
                Warmode = creature.Warmode,
                Frozen = creature.Frozen,
                CantWalk = creature.CantWalk,
                BardPacified = creature.BardPacified
            };
    }
}
