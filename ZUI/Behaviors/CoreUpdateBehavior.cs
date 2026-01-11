using System;
using System.Collections.Generic;
using System.Linq;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace ZUI.Behaviors;

public class CoreUpdateBehavior : MonoBehaviour
{
    public static List<Action> Actions = new ();
    private GameObject _obj;

    public void Setup()
    {
        ClassInjector.RegisterTypeInIl2Cpp<CoreUpdateBehavior>();
        _obj = new GameObject();
        DontDestroyOnLoad(_obj);
        _obj.hideFlags = HideFlags.HideAndDontSave;
        _obj.AddComponent<CoreUpdateBehavior>();
    }

    public void Dispose()
    {
        if (_obj)
            Destroy(_obj);
    }

    protected void Update()
    {
        if (!Plugin.IsInitialized) return;

        foreach (var action in Actions.ToList())
        {
            action?.Invoke();
        }
    }
}
