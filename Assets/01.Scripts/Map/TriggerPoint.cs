using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

[Serializable]
public class EventTypeReference
{
    [SerializeField] string assemblyQualifiedName;

    public Type ResolveType()
    {
        if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
            return null;

        return Type.GetType(assemblyQualifiedName);
    }
}

public class TriggerPoint : Checkpoint
{
    [Header("Trigger")]
    [SerializeField] bool oneShot = true;
    [SerializeField] bool useAsSavePoint = true;

    [Header("Audio")]
    [SerializeField] bool playBgm;
    [SerializeField] List<AudioBgmKey> bgmKeys = new List<AudioBgmKey>();
    [SerializeField] bool playSfx;
    [SerializeField] List<AudioSfxKey> sfxKeys = new List<AudioSfxKey>();

    [Header("EventBus")]
    [SerializeField] List<EventTypeReference> publishEvents = new List<EventTypeReference>();

    bool _triggered;

    public override bool UseAsSavePoint => useAsSavePoint;

    protected override void OnCheckpointTriggered(Collider other)
    {
        if (_triggered && oneShot) return;

        if (useAsSavePoint)
            base.OnCheckpointTriggered(other);

        ExecuteTriggerActions();

        if (oneShot)
            _triggered = true;
    }

    public void ExecuteTrigger()
    {
        if (_triggered && oneShot) return;

        if (useAsSavePoint)
            TryActivateCheckpoint();

        ExecuteTriggerActions();

        if (oneShot)
            _triggered = true;
    }

    void ExecuteTriggerActions()
    {
        if (playBgm && AudioManager.Instance != null)
            PlayConfiguredBgm();

        if (playSfx && AudioManager.Instance != null)
            PlayConfiguredSfx();

        PublishBusEvents();
    }

    void PlayConfiguredBgm()
    {
        if (bgmKeys == null || bgmKeys.Count == 0) return;

        var validKeys = new List<AudioBgmKey>();
        for (int i = 0; i < bgmKeys.Count; i++)
        {
            if (bgmKeys[i] == AudioBgmKey.None) continue;
            validKeys.Add(bgmKeys[i]);
        }

        if (validKeys.Count == 0) return;

        if (validKeys.Count == 1)
            AudioManager.Instance.PlayBGM(validKeys[0]);
        else
            AudioManager.Instance.PlayBGMLayers(validKeys.ToArray());
    }

    void PlayConfiguredSfx()
    {
        if (sfxKeys == null || sfxKeys.Count == 0) return;

        for (int i = 0; i < sfxKeys.Count; i++)
        {
            if (sfxKeys[i] == AudioSfxKey.None) continue;
            AudioManager.Instance.PlaySFX(sfxKeys[i]);
        }
    }

    void PublishBusEvents()
    {
        if (publishEvents == null || publishEvents.Count == 0) return;

        for (int i = 0; i < publishEvents.Count; i++)
        {
            EventTypeReference eventRef = publishEvents[i];
            if (eventRef == null) continue;

            Type eventType = eventRef.ResolveType();
            if (eventType == null) continue;
            if (!typeof(IEvent).IsAssignableFrom(eventType)) continue;

            object eventInstance;
            try
            {
                eventInstance = Activator.CreateInstance(eventType);
            }
            catch
            {
                continue;
            }

            if (eventInstance == null) continue;

            Type busType = typeof(EventBus<>).MakeGenericType(eventType);
            MethodInfo publishMethod = busType.GetMethod("Publish", BindingFlags.Public | BindingFlags.Static);
            if (publishMethod == null) continue;

            publishMethod.Invoke(null, new[] { eventInstance });
        }
    }
}
