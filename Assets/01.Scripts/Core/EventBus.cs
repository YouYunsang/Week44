using System;
using System.Collections.Generic;

public interface IEvent { }

public static class EventBus<T> where T : IEvent
{
    static readonly Dictionary<object, Action<T>> listenerMap = new();
    static event Action<T> OnEvent;

    public static void Subscribe(Action<T> listener)
    {
        OnEvent += listener;
        listenerMap[listener] = listener;
    }

    public static void Unsubscribe(Action<T> listener)
    {
        OnEvent -= listener;
        listenerMap.Remove(listener);
    }

    public static void Publish(T evt)
    {
        OnEvent?.Invoke(evt);
    }

    public static void Clear()
    {
        OnEvent = null;
        listenerMap.Clear();
    }
}
