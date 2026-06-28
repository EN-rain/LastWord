// Scripts/World/MonologuePool.cs
// Phase 3 Final Broadcast monologues (GAME_DESIGN.md §7.3).
//
// 60 dramatic — gothic prose, fragmented journal entries, estate history,
//   delivered under maximum pressure from the radio while the Listener is in
//   permanent Frenzy.
// 20 absurdist — mundane / comic content rendered horrifying by context.
//
// Weighting per design doc §7.3: 75% dramatic / 25% absurdist, drawn once per
// run at broadcaster pickup. Distribution is per-run probability; consecutive
// runs may draw the same register.
//
// Length guidance: each monologue is ~25–35 spoken words at normal pace,
// targeting the documented 10-second sustained-Tier-2 broadcast window at the
// default monologue speed (Text Broadcaster mode runs at 0.6x = ~16.7 seconds).
//
// Tone: gothic horror-comedy. Pillar 6 from the design doc — frightening in
// systems, occasionally absurd in content. Both registers should feel like
// they belong in Ashford Estate.
//
// Source: writer-agent generation, 2026-06-25.

using System;

public static class MonologuePool
{
    public static int DramaticCount => Dramatic.Length;
    public static int AbsurdistCount => Absurdist.Length;
    public static int TotalCount => DramaticCount + AbsurdistCount;

    /// <summary>75% dramatic / 25% absurdist per GAME_DESIGN.md §7.3.</summary>
    public const float DramaticWeight = 0.75f;
    public const float AbsurdistWeight = 0.25f;

    // -- Dramatic monologues -------------------------------------------------
    // Sorted by emotional register then alphabetically by opening phrase for
    // editorial review.
    public static readonly string[] Dramatic = new[]
    {
        // -- Direct address, present-tense --
        "I am the last voice in Ashford Estate. The Listener has heard every word I have spoken. It will hear these too.",
        "I am the last to speak. The Listener comes for me. But I will not be silent. Not yet. Not until the words are mine.",
        "I am broadcasting my last will and testament. To my sister. To my friends. To the estate. To the Listener. To whoever is still listening.",
        "I am not afraid of death. I am afraid of being silent. I am afraid of the silence that comes after the last word has been spoken.",
        "I have made peace with what happens after I stop speaking. The Listener will have me. The silence will have what remains.",
        "I am writing this to be read aloud. Not silently. Out loud. Into the microphone. So that the Listener can hear me finish.",
        "I have watched three of my friends die tonight. Each of them died speaking. Each of them died trying to warn the rest of us.",
        "I have been running for twenty minutes. The Listener has been walking. The Listener does not need to run. The Listener only needs to listen.",
        "I have come to believe the Listener is not hunting us. The Listener is collecting us. One by one. Word by word. Into its silence.",
        "I have been silent for ninety seconds. The Listener has been patient. I do not deserve its patience. I do not want its patience.",
        "I count my breaths between sentences. I count the seconds between my words. The Listener counts with me. We have both been counting for hours.",
        "I am broadcasting because I promised. I promised the dead I would finish what they started. I do not break my promises to the dead.",

        // -- Family history, journal entries --
        "The walls remember what the family forgot. Four names. Spoken in order. Return what was taken. Or so the story goes.",
        "My grandmother told me this place held secrets. She did not say the secrets would listen. She did not say they would hunt.",
        "My father said the Ashford bloodline ended with a scream. I am here to prove him wrong. Or to make him right.",
        "My sister died. She left a note. Three words. I have carried them for thirty years. Tonight I will speak them aloud.",
        "My brother told me the Listener cannot hear what is written. He was wrong. The Listener hears everything. Even the writing. Even the thought.",
        "My mother was dying. She said listen. She did not say to whom. I understand now. She meant the Listener. She was warning me.",
        "Mother. Father. Forgive me. I could not stay silent. I could not let them die for my silence. Forgive me. Forgive me.",
        "I am reading from a letter my great-grandmother wrote. She addressed it to me. She knew. One hundred years ago she knew I would be here.",
        "I am not the first Ashford to stand in this room with the radio. I am the fourth. The first three are buried in the garden. I can hear them.",
        "When my mother was dying she said listen. She did not say to whom. I understand now. She meant the Listener. She was warning me.",
        "I write this on the third night. My candle is almost out. The Listener is at the door. I have run out of candles.",

        // -- The Listener, character study --
        "The Listener does not tire. The Listener does not sleep. The Listener only listens. And so I speak. Knowing it hears.",
        "I have seen the Listener's face. It does not have one. It has only the listening. Always the listening. Forever the listening.",
        "The Listener does not have a heart. I know this because I listened. I listened for one. There is only the listening. There is only the wait.",
        "The Listener does not kill the silent. It kills the ones who almost stayed silent. The ones who spoke one word too many. The ones like me.",
        "There are no ghosts in Ashford Estate. There is only the Listener. And the Listener is worse than any ghost. The Listener is awake.",
        "Some say the Listener is the first victim. Some say it is the last. I say it is neither. It is what remains when everyone has spoken their final word.",
        "The Listener is not evil. The Listener is duty. It is the oldest duty. To listen. To remember. To take what was promised.",
        "The Listener stands at the edge of every sound. It waits for the sound to end. When the sound ends, the Listener begins.",
        "The Listener is not a ghost. Ghosts are echoes. The Listener is the silence that remains after every echo has been spoken.",
        "The estate takes voices the way the sea takes ships. Slowly. Without apology. Without witness. I am what remains.",

        // -- The estate, geography --
        "Ashford. The name itself is a warning. Ash. Ford. Where the fire crossed, and what crossed with it never left.",
        "The mirror in the master bedroom does not reflect. It remembers. And it has been waiting. Patiently. For someone to listen.",
        "There is a room on the third floor that has been locked since 1887. Tonight I opened it. I should not have opened it.",
        "The library contains seven thousand volumes. Every one of them is addressed to me. Every one of them is signed in my own hand.",
        "There is a word that has not been spoken in this house for one hundred and thirty-one years. I am about to speak it.",
        "The garden outside has no flowers anymore. It has only the Listener. It stands among the roses and it does not move and it does not blink.",
        "When the manor was built, the masons sealed something inside the walls. They sealed it alive. They sealed it listening.",
        "The fire in the hearth has gone out three times. Each time I have relit it. Each time the Listener has been closer when I turned around.",
        "The fourth floor smells of incense and old paper. The Listener smells of nothing. The contrast is what frightens me most.",
        "There is a clock in the tower that has not moved in one hundred years. Tonight it moved. It moved one tick. One single tick.",

        // -- Family secrets --
        "We were warned about this place. Every record. Every journal. Every whispered story. We came anyway. We came because we did not believe.",
        "I did not believe the stories. None of us did. We thought they were metaphors. We were wrong about every single one.",
        "Four generations of Ashfords kept the secret. Tonight I break it. Tonight I speak the truth into the listening dark.",
        "The first Ashford buried his brother beneath the staircase. The second Ashford heard the brother's voice in the walls. The third went mad.",
        "I am reading from the family journal. Entry four hundred and twelve. The ink is still wet. The ink has always been wet.",
        "When the first Ashford died, his last word echoed in the walls for forty days. The family recorded it. They wrote it down. They sealed it in the vault.",
        "The Ashford family secret is this. The Listener was invited. By the first lord of the house. By his own hand. By his own voice.",

        // -- Procedural, ritual --
        "Four words. That is all. Four words to undo what four generations built in silence. Speak them. Speak them now.",
        "Four words. The first is a name. The second is a place. The third is a promise. The fourth is a price. I am paying it now.",
        "The broadcast is the only protection we have. The words spoken into the radio do not reach the Listener. They reach somewhere else.",

        // -- Token, broadcast, last words --
        "The Token shows above my head. I know it does. Let it show. Let the Listener see. I will not stop speaking until the broadcast is done.",
        "The Token passes when we speak. The Listener follows the Token. So we are the Token. We are the bait. We are the hunt.",
        "This is my last broadcast. Not because I have chosen to stop. Because the Listener has chosen for me. Make of that what you will.",
        "The Listener has my voice now. Let it have my voice. As long as it does not have theirs. As long as they reach the dawn.",
        "The radio is hot in my hand. The Token burns above my head. The Listener stands at the door. I have spoken for ten seconds. I will speak for ten more.",
        "There were five of us when the night began. Now I speak for those who cannot. This is for them. This is the only voice left."
    };

    // -- Absurdist monologues -----------------------------------------------
    // Mundane or comic content rendered horrifying by context. Same length
    // budget as Dramatic. Sorted alphabetically by opening phrase.
    public static readonly string[] Absurdist = new[]
    {
        "Customer service. My name is Daniel. How may I help you. I am sorry to hear about the haunting. Have you tried turning the estate off and on again.",
        "Have you considered that perhaps the Listener is simply lonely. Have you tried talking to it like a normal person. It does not respond. But perhaps it is shy.",
        "I have a question for the Listener. Why. Just why. Was the library necessary. Was the long staircase necessary. Was any of this necessary.",
        "I had a lovely evening planned. A dinner party. Eight guests. The Listener was not on the list. Please ask it to leave.",
        "I tried to tip the Listener for its service. The Listener did not accept gratuity. The Listener also did not accept my wallet.",
        "I would like to file a complaint. The Listener arrived forty-five minutes late. In my day we would not have stood for this.",
        "I would like to register a formal complaint about the Token system. I did not consent to being hunted. I consented to a game.",
        "I am not late. I am simply arriving at the speed of dread. My ETA is now. My ETA has always been now.",
        "If anyone finds my journal. I was last seen near the radio. I was making excellent time on my shopping list. I had not yet reached the beans.",
        "Item number four on this week's shopping list. Eggs. Milk. Bread. And one large tin of beans. I have not forgotten the beans.",
        "My therapist said I should set boundaries with the Listener. I told the Listener. The Listener did not acknowledge my boundaries.",
        "Please leave a message after the tone. The tone is the Listener. The Listener is the tone. I will not be returning your call.",
        "Recipe for disaster. Take one haunted manor. Add four frightened players. Stir gently. Simmer for thirty minutes. Serve cold.",
        "The Listener is in the foyer. The Listener is in the parlor. The Listener is in every room I check. I am running out of rooms.",
        "The weather today is overcast with a chance of Listener. Highs in the low fifties. Please remember to stay hydrated.",
        "The WiFi password is Ashford. Capital A. Lowercase s-h-f-o-r-d. One two three four. The Listener does not need the password. The Listener just listens.",
        "This is a reminder that your library books are overdue. Please return them at your earliest convenience. Or we will send the Listener.",
        "Today's top story. Man trapped in haunted estate for thirty minutes. Experts agree this is far too long. More at eleven.",
        "Would someone please tell me where I left my car keys. They were in my pocket when I arrived. They are no longer in my pocket.",
        "Yelp review. One star. Would not recommend. Haunted. Will not be returning. The Listener has asked me to revise my review."
    };

    /// <summary>
    /// Draws one monologue per the 75/25 weighting. The result is deterministic
    /// per (runSeed, broadcasterIndex) pair when the same Random is threaded
    /// through every call — the host seeds once and draws once per pickup.
    /// </summary>
    public static string DrawMonologue(Random rng)
    {
        if (rng == null) rng = new Random();

        bool drawDramatic = rng.NextDouble() < DramaticWeight;
        var pool = drawDramatic ? Dramatic : Absurdist;
        return pool[rng.Next(pool.Length)];
    }

    /// <summary>Returns the full count of monologues available.</summary>
    public static int GetTotalMonologueCount() => TotalCount;
}
