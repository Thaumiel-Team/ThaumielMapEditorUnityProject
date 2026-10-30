#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Assets.Scripts
{
    public class ColorHelper
    {
        public class ColorData
        {
            public ColorData(MonoBehaviour source, Color color)
            {
                Source = source;
                Color = color;
            }

            public MonoBehaviour Source { get; set; }
            public Color Color { get; set; }
        }

        public static List<ColorData> UsedColors = new();

        public static Color GetColor(MonoBehaviour source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            UsedColors.RemoveAll(d => d.Source == null);

            ColorData existing = UsedColors.FirstOrDefault(d => d.Source == source);
            if (existing != null)
                return existing.Color;

            HashSet<Color> taken = new(UsedColors.Where(d => d.Source.GetType() == source.GetType()).Select(d => d.Color));

            Color color;
            int attempts = 0;
            do
            {
                float r = UnityEngine.Random.Range(50, 255) / 255f;
                float g = UnityEngine.Random.Range(50, 255) / 255f;
                float b = UnityEngine.Random.Range(50, 255) / 255f;
                color = new Color(r, g, b, 1f);
                attempts++;
            }
            while (taken.Contains(color) && attempts < 100);

            UsedColors.Add(new ColorData(source, color));
            return color;
        }

        public static void ReleaseColor(MonoBehaviour source)
        {
            UsedColors.RemoveAll(d => d.Source == null || d.Source == source);
        }
    }
}
#endif