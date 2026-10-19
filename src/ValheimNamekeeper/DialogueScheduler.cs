using System.Collections.Generic;

namespace SuperNova.ValheimNamekeeper
{
    // One pending line per NPC; only important outcomes may interrupt a recent line.
    internal sealed class DialogueScheduler
    {
        private readonly Dictionary<DialogueCue, double> _lastByCue = new Dictionary<DialogueCue, double>();
        private DialogueCue _pending;
        private double _requestedAt, _startedAt = -100, _nextAt;
        private int _lastPriority;
        internal int PendingPriority => DialogueLines.Priority(_pending);

        internal void Request(DialogueCue cue, double now)
        {
            if (cue == DialogueCue.None) return;
            if (cue == DialogueCue.Goodbye && PendingPriority < 3) _pending = DialogueCue.None;
            double repeatDelay = DialogueLines.Priority(cue) == 1 ? 30 : 8;
            if (_lastByCue.TryGetValue(cue, out double last) && now - last < repeatDelay) return;
            if (now < _nextAt && _lastPriority == 3 && DialogueLines.Priority(cue) < 3) return;
            if (_pending != DialogueCue.None && now - _requestedAt < 8 &&
                DialogueLines.Priority(cue) < DialogueLines.Priority(_pending)) return;
            _pending = cue;
            _requestedAt = now;
        }

        internal bool TryTake(double now, out DialogueCue cue)
        {
            cue = DialogueCue.None;
            if (_pending == DialogueCue.None) return false;
            if (now - _requestedAt > 8) { _pending = DialogueCue.None; return false; }
            int priority = DialogueLines.Priority(_pending);
            if (now < _nextAt && !(priority == 3 && now - _startedAt >= 1.5)) return false;
            cue = _pending;
            _pending = DialogueCue.None;
            _startedAt = now;
            _nextAt = now + 5;
            _lastPriority = priority;
            _lastByCue[cue] = now;
            return true;
        }
        internal void Clear() { _pending = DialogueCue.None; }
    }
}
