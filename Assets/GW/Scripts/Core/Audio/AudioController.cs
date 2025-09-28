using System;
using System.Collections.Generic;
using UnityEngine;

namespace GW.Core
{
    /// <summary>
    /// Centralised audio service that handles pooled SFX playback and layered music.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AudioController : MonoBehaviour
    {
        private const float MinPitch = 0.01f;

        private static AudioController instance;

        [SerializeField]
        [Tooltip("List of short SFX cues that can be triggered by gameplay.")]
        private List<SfxEntry> sfxEntries = new();

        [SerializeField]
        [Tooltip("Looping music layers (ambient, percussion, bliss loop, etc.).")]
        private List<MusicLayerEntry> musicLayers = new();

        [SerializeField]
        [Tooltip("Number of one-shot AudioSources pre-created for SFX playback.")]
        [Min(0)]
        private int initialSfxPoolSize = 8;

        [SerializeField]
        [Tooltip("Random pitch variation range applied to SFX (min/max).")]
        private Vector2 pitchVariation = new(0.95f, 1.05f);

        private readonly Dictionary<SfxId, SfxEntry> sfxLookup = new();
        private readonly Dictionary<MusicLayerId, MusicLayerEntry> musicLookup = new();
        private readonly Queue<AudioSource> sfxPool = new();
        private readonly List<SfxPlayback> activeSfx = new();

        private System.Random random;
        private bool initialised;
        private bool settingsSubscribed;

        private float masterVolume = 1f;
        private float musicVolume = 1f;
        private float sfxVolume = 1f;

        /// <summary>
        /// Gets the active audio controller instance, creating one on first access if necessary.
        /// </summary>
        public static AudioController Instance
        {
            get
            {
                if (instance == null)
                {
                    var existing = FindObjectOfType<AudioController>();
                    if (existing != null)
                    {
                        instance = existing;
                        instance.EnsureInitialised();
                    }
                    else
                    {
                        var go = new GameObject("AudioController");
                        instance = go.AddComponent<AudioController>();
                    }
                }

                return instance;
            }
        }

        /// <summary>
        /// Returns true if an audio controller has been initialised.
        /// </summary>
        public static bool HasInstance => instance != null;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureInitialised();
        }

        private void Update()
        {
            if (!initialised)
            {
                return;
            }

            var deltaTime = Time.unscaledDeltaTime;
            UpdateActiveSfx(deltaTime);
            UpdateMusicLayers(deltaTime);
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }

            if (settingsSubscribed)
            {
                SettingsService.SettingsChanged -= HandleSettingsChanged;
                settingsSubscribed = false;
            }
        }

        private void EnsureInitialised()
        {
            if (initialised)
            {
                return;
            }

            random ??= new System.Random(Environment.TickCount);
            BuildLookups();
            PrewarmPool(initialSfxPoolSize);
            InitialiseMusicSources();
            SetMusicLayerActive(MusicLayerId.BaseAmbient, true, true);

            if (!SettingsService.IsInitialised)
            {
                SettingsService.Initialise();
            }

            if (!settingsSubscribed)
            {
                SettingsService.SettingsChanged += HandleSettingsChanged;
                settingsSubscribed = true;
                HandleSettingsChanged(SettingsService.Settings);
            }

            initialised = true;
        }

        /// <summary>
        /// Plays an SFX one-shot with subtle pitch variation.
        /// </summary>
        public void PlaySfx(SfxId id, float volumeScale = 1f)
        {
            if (!initialised || id == SfxId.None)
            {
                return;
            }

            if (!sfxLookup.TryGetValue(id, out var entry))
            {
                return;
            }

            var clip = entry.GetNextClip(random);
            if (clip == null)
            {
                return;
            }

            var source = GetSfxSource();
            source.clip = clip;
            source.loop = false;
            var baseVolume = Mathf.Clamp01(entry.Volume * volumeScale);
            source.volume = ApplySfxVolume(baseVolume);
            source.pitch = GetRandomPitch();
            source.Play();

            var playback = new SfxPlayback
            {
                Source = source,
                Id = id,
                Remaining = clip.length / Mathf.Max(MinPitch, source.pitch),
                BaseVolume = baseVolume,
            };

            activeSfx.Add(playback);
        }

        /// <summary>
        /// Starts or stops the percussion layer based on gameplay combo state.
        /// </summary>
        public void SetPercussionActive(bool active)
        {
            SetMusicLayerActive(MusicLayerId.Percussion, active);
        }

        /// <summary>
        /// Signals that Bliss mode has entered its active state.
        /// </summary>
        public void NotifyBlissActivated()
        {
            PlaySfx(SfxId.BlissEnter);
            PlaySfx(SfxId.BlissDrop);
            SetMusicLayerActive(MusicLayerId.BlissLoop, true);
        }

        /// <summary>
        /// Signals that Bliss mode has ended.
        /// </summary>
        public void NotifyBlissDeactivated()
        {
            PlaySfx(SfxId.BlissExit);
            SetMusicLayerActive(MusicLayerId.BlissLoop, false);
        }

        /// <summary>
        /// Plays an audio cue appropriate for the supplied seal grade.
        /// </summary>
        public void PlaySealGrade(SealGrade grade)
        {
            switch (grade)
            {
                case SealGrade.Perfect:
                    PlaySfx(SfxId.SealPerfect);
                    break;
                case SealGrade.Good:
                    PlaySfx(SfxId.SealGood);
                    break;
                case SealGrade.Fail:
                    PlaySfx(SfxId.SealFail);
                    break;
            }
        }

        /// <summary>
        /// Plays the milestone cue associated with the provided combo value (10/25/50).
        /// </summary>
        public void PlayComboMilestone(int combo)
        {
            switch (combo)
            {
                case 10:
                    PlaySfx(SfxId.ComboMilestone10);
                    break;
                case 25:
                    PlaySfx(SfxId.ComboMilestone25);
                    break;
                case 50:
                    PlaySfx(SfxId.ComboMilestone50);
                    break;
            }
        }

        private void BuildLookups()
        {
            sfxLookup.Clear();
            for (var i = 0; i < sfxEntries.Count; i++)
            {
                var entry = sfxEntries[i];
                if (entry == null || entry.Id == SfxId.None)
                {
                    continue;
                }

                if (!sfxLookup.ContainsKey(entry.Id))
                {
                    entry.Reset();
                    sfxLookup.Add(entry.Id, entry);
                }
            }

            musicLookup.Clear();
            for (var i = 0; i < musicLayers.Count; i++)
            {
                var layer = musicLayers[i];
                if (layer == null)
                {
                    continue;
                }

                if (musicLookup.ContainsKey(layer.Id))
                {
                    continue;
                }

                musicLookup.Add(layer.Id, layer);
            }
        }

        private void PrewarmPool(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var source = CreateSfxSource($"SFX_{i:00}");
                sfxPool.Enqueue(source);
            }
        }

        private AudioSource GetSfxSource()
        {
            if (sfxPool.Count > 0)
            {
                var pooled = sfxPool.Dequeue();
                pooled.clip = null;
                pooled.loop = false;
                pooled.volume = 1f;
                pooled.pitch = 1f;
                return pooled;
            }

            return CreateSfxSource($"SFX_{transform.childCount:00}");
        }

        private AudioSource CreateSfxSource(string name)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;
            source.name = name;
            return source;
        }

        private void UpdateActiveSfx(float deltaTime)
        {
            for (var i = activeSfx.Count - 1; i >= 0; i--)
            {
                var playback = activeSfx[i];
                playback.Remaining -= deltaTime;
                if (playback.Remaining > 0f)
                {
                    continue;
                }

                var source = playback.Source;
                if (source != null)
                {
                    source.Stop();
                    source.clip = null;
                    sfxPool.Enqueue(source);
                }

                activeSfx.RemoveAt(i);
            }
        }

        private void InitialiseMusicSources()
        {
            for (var i = 0; i < musicLayers.Count; i++)
            {
                var layer = musicLayers[i];
                if (layer == null)
                {
                    continue;
                }

                if (layer.Source == null)
                {
                    var source = gameObject.AddComponent<AudioSource>();
                    source.playOnAwake = false;
                    source.loop = layer.Loop;
                    source.spatialBlend = 0f;
                    source.clip = layer.Clip;
                    source.volume = 0f;
                    layer.Source = source;
                }
                else
                {
                    layer.Source.loop = layer.Loop;
                    layer.Source.clip = layer.Clip;
                }

            if (layer.PlayOnAwake && layer.Clip != null)
            {
                layer.TargetVolume = layer.BaseVolume;
                layer.Source.volume = ApplyMusicVolume(layer.BaseVolume);
                if (!layer.Source.isPlaying)
                {
                    layer.Source.Play();
                }
            }
            else
            {
                layer.TargetVolume = 0f;
                layer.Source.volume = 0f;
                if (layer.Source.isPlaying)
                {
                    layer.Source.Stop();
                }
            }
            }
        }

        private void UpdateMusicLayers(float deltaTime)
        {
            for (var i = 0; i < musicLayers.Count; i++)
            {
                var layer = musicLayers[i];
                if (layer?.Source == null)
                {
                    continue;
                }

                var target = ApplyMusicVolume(layer.TargetVolume);
                var current = layer.Source.volume;

                if (Mathf.Approximately(current, target))
                {
                    if (target <= 0f && layer.Source.isPlaying)
                    {
                        layer.Source.Stop();
                    }

                    continue;
                }

                var duration = target > current ? layer.FadeInSeconds : layer.FadeOutSeconds;
                duration = Mathf.Max(0.001f, duration);
                var step = deltaTime / duration;
                var next = Mathf.MoveTowards(current, target, step);
                layer.Source.volume = next;

                if (target > 0f && !layer.Source.isPlaying && layer.Clip != null)
                {
                    layer.Source.Play();
                }
                else if (target <= 0f && Mathf.Approximately(next, 0f) && layer.Source.isPlaying)
                {
                    layer.Source.Stop();
                }
            }
        }

        private void SetMusicLayerActive(MusicLayerId id, bool active, bool immediate = false)
        {
            if (!musicLookup.TryGetValue(id, out var layer) || layer?.Source == null)
            {
                return;
            }

            layer.TargetVolume = active ? layer.BaseVolume : 0f;

            if (active && layer.Clip != null && !layer.Source.isPlaying)
            {
                layer.Source.Play();
            }

            if (immediate)
            {
                layer.Source.volume = ApplyMusicVolume(layer.TargetVolume);
                if (!active)
                {
                    layer.Source.Stop();
                }
            }
        }

        private float GetRandomPitch()
        {
            var min = Mathf.Min(pitchVariation.x, pitchVariation.y);
            var max = Mathf.Max(pitchVariation.x, pitchVariation.y);
            if (Mathf.Approximately(min, max))
            {
                return Mathf.Clamp(min, MinPitch, 3f);
            }

            var t = (float)random.NextDouble();
            var pitch = Mathf.Lerp(min, max, t);
            return Mathf.Clamp(pitch, MinPitch, 3f);
        }

        private float ApplyMusicVolume(float baseVolume)
        {
            return Mathf.Clamp01(baseVolume * masterVolume * musicVolume);
        }

        private float ApplySfxVolume(float baseVolume)
        {
            return Mathf.Clamp01(baseVolume * masterVolume * sfxVolume);
        }

        private void HandleSettingsChanged(PlayerSettingsData data)
        {
            if (data == null)
            {
                return;
            }

            musicVolume = Mathf.Clamp01(data.musicVolume);
            sfxVolume = Mathf.Clamp01(data.sfxVolume);
            masterVolume = 1f;

            for (var i = 0; i < musicLayers.Count; i++)
            {
                var layer = musicLayers[i];
                if (layer?.Source == null)
                {
                    continue;
                }

                var target = ApplyMusicVolume(layer.TargetVolume);
                layer.Source.volume = Mathf.Min(layer.Source.volume, target);

                if (target > 0f && !layer.Source.isPlaying && layer.Clip != null)
                {
                    layer.Source.Play();
                }
                else if (Mathf.Approximately(target, 0f) && layer.TargetVolume <= 0f && layer.Source.isPlaying)
                {
                    layer.Source.Stop();
                }
            }

            for (var i = 0; i < activeSfx.Count; i++)
            {
                var playback = activeSfx[i];
                if (playback?.Source == null)
                {
                    continue;
                }

                playback.Source.volume = ApplySfxVolume(playback.BaseVolume);
            }
        }

        [Serializable]
        private sealed class SfxEntry
        {
            [SerializeField]
            private SfxId id = SfxId.None;

            [SerializeField]
            [Range(0f, 1f)]
            private float volume = 1f;

            [SerializeField]
            private List<AudioClip> clips = new();

            [NonSerialized]
            private int lastClip = -1;

            public SfxId Id => id;
            public float Volume => volume;

            public AudioClip GetNextClip(System.Random rng)
            {
                if (clips == null || clips.Count == 0)
                {
                    return null;
                }

                if (clips.Count == 1)
                {
                    lastClip = 0;
                    return clips[0];
                }

                var index = rng.Next(clips.Count);
                if (index == lastClip)
                {
                    index = (index + 1) % clips.Count;
                }

                lastClip = index;
                return clips[index];
            }

            public void Reset()
            {
                lastClip = -1;
            }
        }

        [Serializable]
        private sealed class MusicLayerEntry
        {
            [SerializeField]
            private MusicLayerId id = MusicLayerId.BaseAmbient;

            [SerializeField]
            private AudioClip clip;

            [SerializeField]
            [Range(0f, 1f)]
            private float baseVolume = 0.75f;

            [SerializeField]
            private bool loop = true;

            [SerializeField]
            private bool playOnAwake;

            [SerializeField]
            [Range(0.01f, 5f)]
            private float fadeInSeconds = 0.5f;

            [SerializeField]
            [Range(0.01f, 5f)]
            private float fadeOutSeconds = 0.5f;

            [NonSerialized]
            private AudioSource source;

            [NonSerialized]
            private float targetVolume;

            public MusicLayerId Id => id;
            public AudioClip Clip => clip;
            public float BaseVolume => baseVolume;
            public bool Loop => loop;
            public bool PlayOnAwake => playOnAwake;
            public float FadeInSeconds => fadeInSeconds;
            public float FadeOutSeconds => fadeOutSeconds;
            public AudioSource Source { get => source; set => source = value; }
            public float TargetVolume { get => targetVolume; set => targetVolume = value; }
        }

        private sealed class SfxPlayback
        {
            public AudioSource Source;
            public SfxId Id;
            public float Remaining;
            public float BaseVolume;
        }

    }
}
