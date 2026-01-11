using System;
using System.Globalization;
using System.Reflection;
using System.Threading;
using ZUI.Behaviors;
using ZUI.UI;
using ZUI.UI.ModContent;
using ZUI.UI.UniverseLib.UI.Panels;
using ZUI.Utils;
using ProjectM;
using Stunlock.Core;
using Unity.Entities;

namespace ZUI.Services
{
    internal static class FamiliarStateService
    {
        private static Entity _familiar;

        //// FLAG PROPERTIES
        public static bool IsFamUnbound { get; private set; }
        public static bool IsFamBound => !IsFamUnbound;

        public static FamStats FamStats { get; private set; } = new();

        public static void Initialize()
        {
            CoreUpdateBehavior.Actions.Add(OnUpdate);
        }

        private static volatile int _skipCounter;

        private static void OnUpdate()
        {
            if(Plugin.IsClientNull() || Plugin.LocalCharacter == Entity.Null)
                return;

            if(_skipCounter < 3)
            {
                Interlocked.Increment(ref _skipCounter);
                return;
            }
            _skipCounter = 0;

            if (!_familiar.Exists())
            {
                _familiar = FamHelper.FindActiveFamiliar(Plugin.LocalCharacter);
            }
            
            if (_familiar == Entity.Null)
            {
                if (!IsFamUnbound)
                {
                    IsFamUnbound = true;
                    FamStats = new();
                    Plugin.UIManager.GetPanel<FamStatsPanel>()?.UpdateData(FamStats);
                }
            }
            else
            {
                var isFirst = FamStats.Level == 0;
                FamStats.Level = _familiar.GetUnitLevel();

                if (_familiar.TryGetComponent(out UnitStats unitStats))
                {
                    FamStats.PhysicalPower = unitStats.PhysicalPower.Value.ToString(CultureInfo.InvariantCulture);
                    FamStats.SpellPower = unitStats.SpellPower.Value.ToString(CultureInfo.InvariantCulture);
                    FamStats.Stats.Clear();
                    foreach (var property in unitStats.GetType().GetFields())
                    {
                        if(property.Name is nameof(unitStats.PhysicalPower) or nameof(unitStats.SpellPower) or "CorruptionDamageReduction")
                            continue;
                        var value = property.GetValue(unitStats);

                        if (value is ModifiableFloat mFloat)
                        {
                            if(mFloat.Value is 0f or 1f)
                                continue;
                            FamStats.Stats.Add(property.Name, mFloat.Value.ToString("N1",CultureInfo.InvariantCulture));
                        }
                    } 
                }
                if (_familiar.TryGetComponent(out Health health))
                {
                    if (health.IsDead)
                    {
                        _familiar = Entity.Null;
                        LogUtils.LogError("DEAD");
                        return;
                    }
                    FamStats.MaxHealth = health.MaxHealth.Value.ToString(CultureInfo.InvariantCulture);
                    FamStats.CurrentHealth = Math.Floor(health.Value).ToString(CultureInfo.InvariantCulture);
                }
                if (_familiar.TryGetComponent(out PrefabGUID targetPrefabGuid))
                {
                    FamStats.Name = targetPrefabGuid.GetLocalizedName();
                }

                IsFamUnbound = false;
                if(isFirst)
                    Plugin.UIManager.GetPanel<FamStatsPanel>()?.UpdateData(FamStats);

            }
        }
    }
}
