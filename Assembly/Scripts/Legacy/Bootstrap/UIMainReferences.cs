using System;
using System.Collections;
using UnityEngine;
using ApplicationManagers;
using UI;
using GameProgress;

public class UIMainReferences : MonoBehaviour
{
    // legacy class used to bootstrap to ApplicationStart
    private void Awake()
    {
        ApplicationStart.Init();
    }
}
