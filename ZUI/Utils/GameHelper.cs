using System;
using ProjectM;
using UnityEngine;

namespace ZUI.Utils
{
    internal static class GameHelper
    {
        internal static ColorNameData GetColorNameFromSchool(AbilitySchoolType? schoolType)
        {
            if(schoolType == null)
                return new ColorNameData { Name = "Normal", Color = Color.white };
            switch (schoolType)
            {
                case AbilitySchoolType.Blood:
                    return new ColorNameData { Name = "Blood", Color = new Color(255f, 0f, 0f) };
                case AbilitySchoolType.Unholy:
                    return new ColorNameData { Name = "Unholy", Color = new Color(0f, 255f, 0f) };
                case AbilitySchoolType.Illusion:
                    return new ColorNameData { Name = "Illusion", Color = new Color(0f, 128f, 128f) };
                case AbilitySchoolType.Frost:
                    return new ColorNameData { Name = "Frost", Color = new Color(0f, 255f, 255f) };
                case AbilitySchoolType.Chaos:
                    return new ColorNameData { Name = "Chaos", Color = new Color(160f, 32f, 240f) };
                case AbilitySchoolType.Storm:
                    return new ColorNameData { Name = "Storm", Color = new Color(255f, 215f, 0f) };
                default:
                    throw new ArgumentOutOfRangeException(nameof(schoolType));
            }
        }

        internal static AbilitySchoolType? GetSchoolFromHexColor(string colorText)
        {
            switch (colorText)
            {
                case "#008080":
                    return AbilitySchoolType.Illusion;
                case "#00FFFF":
                    return AbilitySchoolType.Frost;
                case "#FF0000":
                    return AbilitySchoolType.Blood;
                case "#FFD700":
                    return AbilitySchoolType.Storm;
                case "#A020F0":
                    return AbilitySchoolType.Chaos;
                case "#00FF00":
                    return AbilitySchoolType.Unholy;
                default:
                    return null;

            }
        }

        public class ColorNameData
        {
            public Color Color { get; set; }
            public string Name { get; set; }
        }
    }
}
