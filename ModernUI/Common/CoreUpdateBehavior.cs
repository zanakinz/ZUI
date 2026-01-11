using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace ModernUI.Common
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
            ExecuteOnUpdate?.Invoke();
        }

        public void Dispose()
        {
            if(_obj)
                Destroy(_obj);
        }
    }
}
