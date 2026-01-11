using System;
using ZUI.Services;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace ZUI.UI.ModernLib
{
    public class CoreUpdateBehavior: MonoBehaviour, IDisposable
    {
        private GameObject _obj;

        public void Setup()
        {
            ClassInjector.RegisterTypeInIl2Cpp<CoreUpdateBehavior>();
            _obj = new GameObject("ModernUICoreUpdateBehavior");
            DontDestroyOnLoad(_obj);
            _obj.hideFlags = HideFlags.HideAndDontSave;
            _obj.AddComponent<CoreUpdateBehavior>();
        }
        /// <summary>
        /// This Action is executed each tick in the Update method
        /// </summary>
        public Action ExecuteOnUpdate;

        protected void Update()
        {
            ExecuteOnUpdate?.Invoke(); //todo why it is null?
            MessageService.ProcessAllMessages(); 
        }

        public void Dispose()
        {
            if(_obj)
                Destroy(_obj);
        }
    }
}
