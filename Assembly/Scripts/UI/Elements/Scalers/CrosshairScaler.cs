using System;
using UnityEngine;
using Settings;
using UnityEngine.UI;

namespace UI
{
    class CrosshairScaler: IgnoreScaler
    {
        public override void ApplyScale()
        {
            base.ApplyScale();
            float scale = SettingsManager.UISettings.CrosshairScale.Value;
            RectTransform rect = GetComponent<RectTransform>();
            Vector3 currentScale = rect.localScale;
            rect.localScale = new Vector2(currentScale.x * scale, currentScale.y * scale);
            int fontSize = 16;
            if (scale > 1f)
            {
                fontSize = (int)(16 * scale);
            }
            scale = 16f / fontSize;
            transform.Find("DefaultLabel").GetComponent<Text>().fontSize = fontSize;
            transform.Find("DefaultLabel").GetComponent<RectTransform>().localScale = new Vector2(scale, scale);
        }
    }
}
