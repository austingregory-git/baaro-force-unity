using System.Collections;

namespace BaaroForce.Characters
{
    /// <summary>
    /// Abstract base for all NPC AI strategies.
    /// Each concrete subclass encapsulates one <see cref="NpcPersonality"/>.
    ///
    /// <see cref="ExecuteTurn"/> is a coroutine so animations (movement, spell
    /// effects) can be interleaved with decision logic via <c>yield return</c>.
    /// TurnManager starts this coroutine and waits for it to finish before
    /// moving on to the next NPC.
    /// </summary>
    public abstract class NpcAI
    {
        /// <summary>The personality this AI implements.</summary>
        public abstract NpcPersonality Personality { get; }

        /// <summary>
        /// Coroutine that carries out the NPC's full turn.
        /// Use <c>yield return context.AnimateNpcMove(path)</c> to trigger
        /// movement animations; TurnManager provides the implementation
        /// via the context delegates.
        /// </summary>
        public abstract IEnumerator ExecuteTurn(NpcTurnContext context);
    }
}
