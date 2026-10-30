using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Serialization;

namespace Content.Shared._RuCM.Kaban;

// CMU14: only prototype IDs from the server's compatibility catalog are accepted.
[Serializable, NetSerializable]
public sealed record KabanForceChoice(string Mode = "", string Planet = "", string Govfor = "", string Opfor = "")
{
    // An explicit array avoids the compiler's params-Span inline-array helper, which the client IL verifier rejects.
    public string Encode() => string.Join("|", new[] { Mode, Planet, Govfor, Opfor });

    public static KabanForceChoice? Decode(string value)
    {
        var parts = value.Split('|');
        return parts.Length == 4 && parts.All(p => p.Length <= 128 && p.All(c => char.IsAsciiLetterOrDigit(c) || c is '_' or '-'))
            ? new(parts[0], parts[1], parts[2], parts[3]) : null;
    }

    public bool ValidFor(string product) => product switch
    {
        "mode" => Mode.Length > 0 && Planet == "" && Govfor == "" && Opfor == "",
        "map" => Planet.Length > 0 && Mode == "" && Govfor == "" && Opfor == "",
        "govfor" => Govfor.Length > 0 && Mode == "" && Planet == "" && Opfor == "",
        "opfor" => Opfor.Length > 0 && Mode == "" && Planet == "" && Govfor == "",
        "full" => Mode.Length > 0 && Planet.Length > 0 && Govfor.Length > 0 && Opfor.Length > 0,
        _ => false,
    };

    public bool Matches(KabanForceOption option) => (Mode == "" || Mode == option.Mode)
        && (Planet == "" || Planet == option.Planet)
        && (Govfor == "" || option.Govfor.ContainsKey(Govfor))
        && (Opfor == "" || option.Opfor.ContainsKey(Opfor))
        && (Govfor == "" || option.Opfor.Count == 0 || option.Opfor.Keys.Any(id => id != Govfor))
        && (Opfor == "" || option.Govfor.Count == 0 || option.Govfor.Keys.Any(id => id != Opfor))
        && (Govfor == "" || Opfor == "" || Govfor != Opfor);
}

[Serializable, NetSerializable]
public sealed record KabanForceOption(string Mode, string ModeName, string Planet, string PlanetName,
    Dictionary<string, string> Govfor, Dictionary<string, string> Opfor);
