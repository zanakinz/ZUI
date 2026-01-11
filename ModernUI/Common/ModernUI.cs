namespace ModernUI.Common
{
    public static class ModernUI
    {
        public static CoreUpdateBehavior CoreUpdateBehavior { get; set; }
        private static readonly List<UIBehaviourModelEx> BehaviourModels = new();
        private static bool _isInitialized;

        static ModernUI() { }

        /// <summary>
        /// Initialize ModernUI
        /// </summary>
        public static void Initialize()
        {
            CoreUpdateBehavior = new CoreUpdateBehavior();
            CoreUpdateBehavior.Setup();
            CoreUpdateBehavior.ExecuteOnUpdate += ProcessRegisteredBehaviors;
            _isInitialized = true;
        }

        #region Beahviour Models handling
        private static void ProcessRegisteredBehaviors()
        {
            if (!_isInitialized || !BehaviourModels.Any())
                return;

            try
            {
                for (int i = BehaviourModels.Count - 1; i >= 0; i--)
                {
                    var instance = BehaviourModels[i];
                    if (instance == null || !instance.UIRoot)
                    {
                        BehaviourModels.RemoveAt(i);
                        continue;
                    }
                    if (instance.IsActive)
                        instance.Update();
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError(ex.ToString());
            }
        }

        public static void AddBehaviourModel(UIModelEx model)
        {
            if(model is UIBehaviourModelEx bModel)
                BehaviourModels.Add(bModel);
        }

        public static void DestroyBehaviourModel(UIModelEx model)
        {
            if (model is UIBehaviourModelEx bModel)
            {
                if (BehaviourModels.Contains(bModel))
                    BehaviourModels.Remove(bModel);
            }
        }
        #endregion
    }
}
