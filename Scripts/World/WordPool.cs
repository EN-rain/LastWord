// Scripts/World/WordPool.cs
// Gothic-coded-word pool for Phase 1 note registration (GAME_DESIGN.md §7.1).
//
// Tier 1: monosyllabic, whispered easily. Default for any run.
// Tier 2: polysyllabic, harder to whisper cleanly. "Higher difficulty / Lights Out" per design doc.
//
// Total: 90 words (50 Tier 1 + 40 Tier 2). The game draws 4 words per run from a
// randomised subset of these pools — never reused mid-run.
//
// Tone: gothic horror-comedy, Ashford Estate register. Every word should sit
// comfortably in a haunted-manor note and be speakable in one breath.
//
// Source: writer-agent generation, 2026-06-25.

using Godot;
using System;

public static class WordPool
{
    public enum Tier
    {
        One = 1,   // Monosyllabic. Default for note registration.
        Two = 2    // Polysyllabic. Higher detection risk / harder to whisper.
    }

    // Tier 1 — monosyllabic. Lower detection cost when spoken at Tier 1.
    // Sorted alphabetically for easy review.
    public static readonly string[] Tier1 = new[]
    {
        "Ash",      // 01 — residue, what remains
        "Bleak",    // 02 — atmosphere
        "Blood",    // 03 — cost, family
        "Bone",     // 04 — buried, ancestors
        "Chime",    // 05 — clock tower
        "Cold",     // 06 — the Listener's touch
        "Crypt",    // 07 — what lies below
        "Dark",     // 08 — the estate at night
        "Doom",     // 09 — inevitable
        "Dusk",     // 10 — the hour we entered
        "Dust",     // 11 — what the manor keeps
        "End",      // 12 — the promise
        "Fell",     // 13 — what we did to the family
        "Fern",     // 14 — overgrown garden
        "Flesh",    // 15 — the mortal cost
        "Ghost",    // 16 — the first Ashford
        "Gloom",    // 17 — what fills the halls
        "Gaunt",    // 18 — the Listener's shape
        "Grim",     // 19 — the work ahead
        "Hark",     // 20 — listen
        "Hate",     // 21 — what feeds the house
        "Hearth",   // 22 — the master bedroom
        "Helm",     // 23 — the family crest
        "Jaws",     // 24 — what closes behind
        "Knife",    // 25 — what was found upstairs
        "Light",    // 26 — what we lost
        "Loom",     // 27 — the weaving, the trap
        "Mire",     // 28 — where the first lord fell
        "Mold",     // 29 — the library smell
        "Mourn",    // 30 — what the dead require
        "Night",    // 31 — when the rules change
        "Null",     // 32 — the silence after
        "Oath",     // 33 — what we swore
        "Plague",   // 34 — what the family carried
        "Plume",    // 35 — smoke from the furnace
        "Quake",    // 36 — what the walls do at hour three
        "Rift",     // 37 — between the floors
        "Rot",      // 38 — under the floorboards
        "Shroud",   // 39 — what the dead wear
        "Spite",    // 40 — the Listener's fuel
        "Stone",    // 41 — the basement walls
        "Thorn",    // 42 — the garden's last survivor
        "Tomb",     // 43 — the staircase hub
        "Urn",      // 44 — the family ashes
        "Veil",     // 45 — between us and the listening
        "Void",     // 46 — what waits upstairs
        "Vow",      // 47 — what was spoken before us
        "Wane",     // 48 — what the moon does
        "Wisp",     // 49 — what follows the Listener
        "Wraith"    // 50 — what I fear I will become
    };

    // Tier 2 — polysyllabic. Higher difficulty; requires sustained Tier 2 speech
    // to register if the design ever escalates, or reserved as optional harder
    // pull after Lights Out. Sorted alphabetically.
    public static readonly string[] Tier2 = new[]
    {
        "Ashen",        // 01
        "Burial",       // 02
        "Candle",       // 03
        "Charnel",      // 04
        "Coffin",       // 05
        "Corrupt",      // 06
        "Eclipse",      // 07
        "Embers",       // 08
        "Ephemeral",    // 09
        "Forsaken",     // 10
        "Fracture",     // 11
        "Gallows",      // 12
        "Grimly",       // 13
        "Haunted",      // 14
        "Hollow",       // 15
        "Lament",       // 16
        "Molder",       // 17
        "Obscure",      // 18
        "Penance",      // 18
        "Petrify",      // 19
        "Phantasm",     // 20
        "Plunder",      // 21
        "Pyre",         // 22
        "Quiver",       // 23
        "Raven",        // 24
        "Repent",       // 25
        "Restless",     // 26
        "Shriven",      // 27
        "Sorrow",       // 28
        "Specter",      // 29
        "Stillness",    // 30
        "Sunder",       // 31
        "Threnody",     // 32
        "Twilight",     // 33
        "Umbral",       // 34
        "Verdant",      // 35
        "Vestige",      // 36
        "Whispers",     // 37
        "Wither",       // 38
        "Wretched"      // 39
    };

    public const int Tier1Count = 50;
    public const int Tier2Count = 40;
    public const int TotalCount = Tier1Count + Tier2Count;

    /// <summary>
    /// Returns a random word from the requested tier.
    /// Caller supplies the RNG so the pool draw stays deterministic per host.
    /// </summary>
    public static string GetRandomWord(Tier tier, Random rng)
    {
        if (rng == null) rng = new Random();
        return tier == Tier.One
            ? Tier1[rng.Next(Tier1.Length)]
            : Tier2[rng.Next(Tier2.Length)];
    }

    /// <summary>
    /// Returns <paramref name="count"/> distinct words from the requested tier,
    /// drawn without replacement. Throws if the request exceeds pool size.
    /// </summary>
    public static string[] DrawDistinct(Tier tier, int count, Random rng)
    {
        if (rng == null) rng = new Random();
        var pool = tier == Tier.One ? Tier1 : Tier2;
        if (count > pool.Length)
            throw new ArgumentException(
                $"WordPool.DrawDistinct: requested {count} but tier {(int)tier} only has {pool.Length} words.");

        // Fisher-Yates partial shuffle on a copy.
        var working = new string[pool.Length];
        Array.Copy(pool, working, pool.Length);
        for (int i = 0; i < count; i++)
        {
            int j = rng.Next(i, working.Length);
            (working[i], working[j]) = (working[j], working[i]);
        }
        var result = new string[count];
        Array.Copy(working, result, count);
        return result;
    }
}
