




using Settings;
using System;
using UnityEngine;

public class SpectatorMovement : MonoBehaviour
{
    public bool disable;
    private float speed = 100f;

    private void Start()
    {
    }

    private void Update()
    {
        if (!this.disable)
        {
            float num2;
            float num3;
            float speed = this.speed;
            if (SettingsManager.InputSettings.Human.Jump.GetKey())
            {
                speed *= 3f;
            }
            if (SettingsManager.InputSettings.General.Forward.GetKey())
            {
                num2 = 1f;
            }
            else if (SettingsManager.InputSettings.General.Back.GetKey())
            {
                num2 = -1f;
            }
            else
            {
                num2 = 0f;
            }
            if (SettingsManager.InputSettings.General.Left.GetKey())
            {
                num3 = -1f;
            }
            else if (SettingsManager.InputSettings.General.Right.GetKey())
            {
                num3 = 1f;
            }
            else
            {
                num3 = 0f;
            }
            Transform transform = base.transform;
            if (num2 > 0f)
            {
                transform.position += (Vector3) ((base.transform.forward * speed) * Time.deltaTime);
            }
            else if (num2 < 0f)
            {
                transform.position -= (Vector3) ((base.transform.forward * speed) * Time.deltaTime);
            }
            if (num3 > 0f)
            {
                transform.position += (Vector3) ((base.transform.right * speed) * Time.deltaTime);
            }
            else if (num3 < 0f)
            {
                transform.position -= (Vector3) ((base.transform.right * speed) * Time.deltaTime);
            }
            if (SettingsManager.InputSettings.Human.HookLeft.GetKey())
            {
                transform.position -= (Vector3) ((base.transform.up * speed) * Time.deltaTime);
            }
            else if (SettingsManager.InputSettings.Human.HookRight.GetKey())
            {
                transform.position += (Vector3) ((base.transform.up * speed) * Time.deltaTime);
            }
        }
    }
}

