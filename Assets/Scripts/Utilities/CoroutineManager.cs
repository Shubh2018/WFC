using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class CoroutineManager
{
    private static Dictionary<MonoBehaviour, Dictionary<string, IEnumerator>> coroutineStatus = new();

    public static void StartCoroutine(MonoBehaviour mono, string name, IEnumerator func)
    {
        if (IsAlive(mono, name)) return;
        if (!HasMonobehaviour(mono)) coroutineStatus[mono] = new();
        Debug.Log($"{mono.name} ({mono.GetType()}) started coroutine {name}");
        IEnumerator wrapper = CoroutineWrapper(mono, name, func);
        mono.StartCoroutine(wrapper);
        coroutineStatus[mono][name] = wrapper;
    }

    public static void StopCoroutine(MonoBehaviour mono, string name)
    {
        if (!IsAlive(mono, name)) return;
        Debug.Log($"{mono.name} ({mono.GetType()}) stopped coroutine {name}");
        mono.StopCoroutine(coroutineStatus[mono][name]);
        coroutineStatus[mono].Remove(name);
        if (coroutineStatus[mono].Values.Count() == 0) coroutineStatus.Remove(mono);
    }

    public static void StopAllCoroutines(MonoBehaviour mono)
    {
        Debug.Log($"{mono.name} ({mono.GetType()}) stopped all its coroutines");
        mono.StopAllCoroutines();
        coroutineStatus.Remove(mono);
    }

    public static void StopAllCoroutines()
    {
        List<MonoBehaviour> keys = coroutineStatus.Keys.ToList();
        foreach (MonoBehaviour key in keys)
            StopAllCoroutines(key);
    }

    public static void EndOfRoutine(MonoBehaviour mono, string name)
    {
        if (!IsAlive(mono, name)) return;
        coroutineStatus[mono].Remove(name);
        if (coroutineStatus[mono].Values.Count() == 0) coroutineStatus.Remove(mono);
        Debug.Log($"{mono.name} ({mono.GetType()}) coroutine {name} finished");
    }

    private static IEnumerator CoroutineWrapper(MonoBehaviour mono, string name, IEnumerator func)
    {
        yield return mono.StartCoroutine(func);
        EndOfRoutine(mono, name);
    }

    public static bool IsAlive(MonoBehaviour mono, string name)
    {
        return coroutineStatus.Any(m => m.Key == mono && m.Key.GetType() == mono.GetType() && m.Value.ContainsKey(name));
    }

    public static bool IsAlive(MonoBehaviour mono, string[] names)
    {
        if (!HasMonobehaviour(mono)) return false;
        return names.All(n => coroutineStatus[mono].ContainsKey(n));
    }

    private static bool HasMonobehaviour(MonoBehaviour mono)
    {
        return coroutineStatus.Any(m => m.Key == mono && m.Key.GetType() == mono.GetType());
    }

    public static bool HasAliveRoutines(MonoBehaviour mono)
    {
        return HasMonobehaviour(mono);
    }

    public static bool HasAliveRoutinesExcept(MonoBehaviour mono)
    {
        return HasMonobehaviour(mono) && coroutineStatus.Count() > 1;
    }

    public static bool HasAliveRoutines()
    {
        return coroutineStatus.Count() > 0;
    }
}