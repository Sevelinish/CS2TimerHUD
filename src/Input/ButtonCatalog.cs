using CounterStrikeSharp.API;

namespace TimerHud.Input;

public static class ButtonCatalog
{
    private static readonly (string Name, PlayerButtons Button)[] Canonical =
    {
        ("use", PlayerButtons.Use),
        ("reload", PlayerButtons.Reload),
        ("inspect", PlayerButtons.Inspect),
        ("scoreboard", PlayerButtons.Scoreboard),
        ("attack", PlayerButtons.Attack),
        ("attack2", PlayerButtons.Attack2),
        ("attack3", PlayerButtons.Attack3),
        ("zoom", PlayerButtons.Zoom),
        ("jump", PlayerButtons.Jump),
        ("duck", PlayerButtons.Duck),
        ("speed", PlayerButtons.Speed),
        ("walk", PlayerButtons.Walk),
        ("forward", PlayerButtons.Forward),
        ("back", PlayerButtons.Back),
        ("moveleft", PlayerButtons.Moveleft),
        ("moveright", PlayerButtons.Moveright),
        ("turnleft", PlayerButtons.Left),
        ("turnright", PlayerButtons.Right),
        ("grenade1", PlayerButtons.Grenade1),
        ("grenade2", PlayerButtons.Grenade2),
        ("weapon1", PlayerButtons.Weapon1),
        ("weapon2", PlayerButtons.Weapon2),
        ("alt1", PlayerButtons.Alt1),
        ("alt2", PlayerButtons.Alt2),
        ("run", PlayerButtons.Run),
        ("bullrush", PlayerButtons.Bullrush),
        ("cancel", PlayerButtons.Cancel),
    };

    private static readonly Dictionary<string, PlayerButtons> Aliases = BuildAliases();

    public static string Names { get; } = string.Join(", ", Canonical.Select(entry => entry.Name));

    public static bool IsClearKeyword(string raw) =>
        raw.Trim().ToLowerInvariant() is "none" or "off" or "clear" or "unbind" or "-";

    public static bool TryParse(string? raw, out PlayerButtons button)
    {
        button = 0;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var value = raw.Trim().ToLowerInvariant().TrimStart('+');
        return Aliases.TryGetValue(value, out button);
    }

    public static PlayerButtons Resolve(string? raw) => TryParse(raw, out var button) ? button : 0;

    public static string NameOf(PlayerButtons button)
    {
        foreach (var entry in Canonical)
        {
            if (entry.Button == button)
                return entry.Name;
        }

        return button == 0 ? "none" : button.ToString().ToLowerInvariant();
    }

    private static Dictionary<string, PlayerButtons> BuildAliases()
    {
        var map = new Dictionary<string, PlayerButtons>(StringComparer.Ordinal);

        foreach (var entry in Canonical)
            map[entry.Name] = entry.Button;

        map["e"] = PlayerButtons.Use;
        map["r"] = PlayerButtons.Reload;
        map["f"] = PlayerButtons.Inspect;
        map["lookatweapon"] = PlayerButtons.Inspect;
        map["tab"] = PlayerButtons.Scoreboard;
        map["score"] = PlayerButtons.Scoreboard;
        map["showscores"] = PlayerButtons.Scoreboard;
        map["mouse1"] = PlayerButtons.Attack;
        map["m1"] = PlayerButtons.Attack;
        map["lmb"] = PlayerButtons.Attack;
        map["fire"] = PlayerButtons.Attack;
        map["mouse2"] = PlayerButtons.Attack2;
        map["m2"] = PlayerButtons.Attack2;
        map["rmb"] = PlayerButtons.Attack2;
        map["mouse3"] = PlayerButtons.Attack3;
        map["m3"] = PlayerButtons.Attack3;
        map["mwheel"] = PlayerButtons.Attack3;
        map["space"] = PlayerButtons.Jump;
        map["ctrl"] = PlayerButtons.Duck;
        map["control"] = PlayerButtons.Duck;
        map["crouch"] = PlayerButtons.Duck;
        map["shift"] = PlayerButtons.Speed;
        map["sprint"] = PlayerButtons.Speed;
        map["w"] = PlayerButtons.Forward;
        map["s"] = PlayerButtons.Back;
        map["a"] = PlayerButtons.Moveleft;
        map["d"] = PlayerButtons.Moveright;
        map["left"] = PlayerButtons.Left;
        map["right"] = PlayerButtons.Right;
        map["g"] = PlayerButtons.Grenade1;
        map["q"] = PlayerButtons.Weapon1;

        return map;
    }
}
