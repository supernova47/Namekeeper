using System;
using System.Collections.Generic;
using UnityEngine;

namespace SuperNova.ValheimNamekeeper
{
    // Local presentation attached to the existing local visual, never a new network NPC.
    internal sealed class MerchantDialogue : MonoBehaviour
    {
        private const float GestureBlendSeconds = .18f;
        private static readonly List<MerchantDialogue> Speakers = new List<MerchantDialogue>();
        private static float s_nextAmbientAt;
        private static MerchantDialogue s_currentVoice;
        private readonly DialogueScheduler _scheduler = new DialogueScheduler();
        private readonly Dictionary<int, List<AudioClip>> _voices = new Dictionary<int, List<AudioClip>>();
        private readonly List<AudioClip> _allVoices = new List<AudioClip>();
        private AudioSource _voice;
        private Animator _animator;
        private ShopState _shop;
        private float _nextProximity, _nextIdle;
        private bool _near;
        private bool _warned;
        private bool _warnedAnimation;
        private float _dialogHeight = 2f;

        internal void Initialize(ShopState shop)
        {
            _shop = shop;
            _animator = GetComponentInChildren<Animator>(true);
            _nextIdle = Time.unscaledTime + 40f;
            var voiceObject = new GameObject("NamekeeperVoice");
            voiceObject.transform.SetParent(transform, false);
            voiceObject.transform.localPosition = Vector3.up * 1.35f;
            _voice = voiceObject.AddComponent<AudioSource>();
            _voice.playOnAwake = false;
            _voice.loop = false;
            _voice.spatialBlend = 1f;
            _voice.minDistance = 2f;
            _voice.maxDistance = 14f;
            _voice.rolloffMode = AudioRolloffMode.Logarithmic;
            try
            {
                var prefab = ZNetScene.instance != null ? ZNetScene.instance.GetPrefab("Hildir") : null;
                var trader = prefab != null ? prefab.GetComponentInChildren<Trader>(true) : null;
                if (trader != null)
                {
                    _dialogHeight = Mathf.Clamp(trader.m_dialogHeight, 1.5f, 3f);
                    ReadVoices(0, trader.m_randomTalkFX);
                    ReadVoices(1, trader.m_randomGreetFX);
                    ReadVoices(2, trader.m_randomStartTradeFX);
                    ReadVoices(3, trader.m_randomBuyFX);
                    ReadVoices(4, trader.m_randomGoodbyeFX);
                    ReadVoices(5, trader.m_randomSellFX);
                }
                NamekeeperPlugin.Log?.LogInfo("Namekeeper merchant dialogue: " + _allVoices.Count +
                    " Hildir voice clips discovered; language follows the game.");
                if (_allVoices.Count == 0)
                    NamekeeperPlugin.Log?.LogWarning("Hildir voice clips unavailable; merchant text remains enabled.");
            }
            catch (Exception ex) { Warn(ex); }
            Request(DialogueCue.Arrival);
        }

        private void ReadVoices(int group, EffectList effects)
        {
            var clips = new List<AudioClip>();
            if (effects?.m_effectPrefabs != null)
                foreach (var effect in effects.m_effectPrefabs)
                {
                    if (effect == null || !effect.m_enabled || effect.m_prefab == null) continue;
                    foreach (var sfx in effect.m_prefab.GetComponentsInChildren<ZSFX>(true))
                    {
                        if (sfx.m_audioClips == null) continue;
                        foreach (AudioClip clip in sfx.m_audioClips)
                            if (clip != null)
                            {
                                if (!clips.Contains(clip)) clips.Add(clip);
                                if (!_allVoices.Contains(clip)) _allVoices.Add(clip);
                            }
                    }
                    var nativeSource = effect.m_prefab.GetComponentInChildren<AudioSource>(true);
                    if (nativeSource != null && nativeSource.outputAudioMixerGroup != null)
                        _voice.outputAudioMixerGroup = nativeSource.outputAudioMixerGroup;
                }
            _voices[group] = clips;
        }

        internal void Request(DialogueCue cue)
        {
            if (NamekeeperPlugin.DialogueEnabled.Value)
                _scheduler.Request(cue, Time.unscaledTime);
        }

        private void OnEnable() { if (!Speakers.Contains(this)) Speakers.Add(this); }
        private void OnDisable()
        {
            Speakers.Remove(this);
            _scheduler.Clear();
            if (_voice != null) _voice.Stop();
            if (s_currentVoice == this) s_currentVoice = null;
            if (Chat.instance != null) Chat.instance.ClearNpcText(gameObject);
        }

        private void Update()
        {
            if (_shop == null || Player.m_localPlayer == null) return;
            if (!NamekeeperPlugin.DialogueEnabled.Value)
            {
                _scheduler.Clear();
                if (_voice != null && _voice.isPlaying) _voice.Stop();
                return;
            }
            if (!NamekeeperPlugin.VoiceEnabled.Value && _voice != null && _voice.isPlaying) _voice.Stop();
            float now = Time.unscaledTime;
            float distance = Vector3.Distance(Player.m_localPlayer.transform.position, transform.position);
            if (now >= _nextProximity)
            {
                _nextProximity = now + .5f;
                bool near = distance <= (_near ? 9f : 6f);
                bool nearest = IsNearest(Player.m_localPlayer.transform.position, distance);
                if (near && !_near && nearest) Request(DialogueCue.Greeting);
                else if (!near && _near && distance < 15f) Request(DialogueCue.Goodbye);
                _near = near;
                bool shopOpen = NamekeeperPlugin.Instance?.Ui?.IsOpen == true;
                if (near && nearest && !shopOpen && now >= _nextIdle)
                {
                    _nextIdle = now + UnityEngine.Random.Range(40f, 65f);
                    Request(DialogueCue.Idle);
                }
            }
            if (distance > 16f || Chat.instance == null) return;
            // Prevent several nearby shops from greeting the player as a chorus.
            if (_scheduler.PendingPriority == 1 &&
                (now < s_nextAmbientAt || !IsNearest(Player.m_localPlayer.transform.position, distance))) return;
            if (_scheduler.TryTake(now, out DialogueCue cue))
            {
                if (DialogueLines.Priority(cue) == 1) s_nextAmbientAt = now + 6f;
                Speak(cue);
            }
        }

        private bool IsNearest(Vector3 playerPosition, float distance)
        {
            foreach (MerchantDialogue other in Speakers)
                if (other != null && other != this && Vector3.Distance(playerPosition, other.transform.position) < distance) return false;
            return true;
        }

        private void Speak(DialogueCue cue)
        {
            try
            {
                Animate(cue);
                string language = global::Localization.instance.GetSelectedLanguage();
                string text = DialogueLines.Get(cue, language, UnityEngine.Random.Range(0, 1000));
                if (string.IsNullOrEmpty(text)) return;
                Chat.instance.SetNpcText(gameObject, Vector3.up * _dialogHeight, 20f, 5f, "", text, false);
                if (!NamekeeperPlugin.VoiceEnabled.Value || _voice == null || _allVoices.Count == 0) return;
                int group = cue == DialogueCue.Greeting || cue == DialogueCue.Arrival ? 1 :
                    cue == DialogueCue.Open ? 2 : cue == DialogueCue.Goodbye ? 4 :
                    cue >= DialogueCue.SuccessName && cue <= DialogueCue.SuccessReset ? 3 : 0;
                List<AudioClip> clips = _voices.TryGetValue(group, out List<AudioClip> selected) && selected.Count > 0 ? selected : _allVoices;
                if (s_currentVoice != null && s_currentVoice != this && s_currentVoice._voice != null)
                    s_currentVoice._voice.Stop();
                s_currentVoice = this;
                _voice.Stop();
                _voice.volume = NamekeeperPlugin.VoiceVolume.Value;
                _voice.pitch = 1f;
                _voice.clip = clips[UnityEngine.Random.Range(0, clips.Count)];
                _voice.Play();
            }
            catch (Exception ex) { Warn(ex); }
        }

        private void Animate(DialogueCue cue)
        {
            if (_animator == null || !_animator.isActiveAndEnabled) return;

            string emote = cue switch
            {
                DialogueCue.Arrival or DialogueCue.Greeting or DialogueCue.Open => "Emote_wave",
                DialogueCue.SelectItem or DialogueCue.NameEdited or DialogueCue.DescriptionEdited => "Emote_point",
                DialogueCue.ResetName or DialogueCue.ResetDescription or DialogueCue.CancelReset => "Emote_shrug",
                DialogueCue.SuccessName or DialogueCue.SuccessDescription or DialogueCue.SuccessBoth or DialogueCue.SuccessReset => "Emote_cheer",
                DialogueCue.Goodbye => "Emote_bow",
                DialogueCue.NoChanges or DialogueCue.NotEnoughCoins or DialogueCue.InvalidText or
                DialogueCue.ItemUnavailable or DialogueCue.NoEligibleItems or DialogueCue.NoRoof or
                DialogueCue.TooFar or DialogueCue.Failure => "Emote_nonono",
                _ => null
            };
            if (emote == null)
            {
                return;
            }

            int state = Animator.StringToHash("Base Layer.Emotes." + emote);
            if (!_animator.HasState(0, state))
            {
                if (!_warnedAnimation)
                {
                    _warnedAnimation = true;
                    NamekeeperPlugin.Log?.LogWarning("Player animation state unavailable: " + emote);
                }
                return;
            }

            // Blend into the gesture, then let the vanilla one-shot state transition
            // return to its standing idle. A cached Animator state can be transient.
            _animator.CrossFadeInFixedTime(state, GestureBlendSeconds, 0, 0f);
        }

        private void Warn(Exception ex)
        {
            if (_warned) return;
            _warned = true;
            NamekeeperPlugin.Log?.LogWarning("Merchant dialogue presentation unavailable: " + ex);
        }
    }
}
