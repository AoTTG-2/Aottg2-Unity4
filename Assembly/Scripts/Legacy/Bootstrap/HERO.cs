using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Xft;
using CustomSkins;
using Settings;
using UI;
using ApplicationManagers;
using Weather;
using GameProgress;
using Characters;

public class HERO : Photon.MonoBehaviour
{
    public GameObject hookRefL1;
    public GameObject hookRefL2;
    public GameObject hookRefR1;
    public GameObject hookRefR2;
    public GameObject checkBoxLeft;
    public GameObject checkBoxRight;

    public void Awake()
    {
        if (SceneLoader.SceneName != SceneName.CharacterEditor)
            gameObject.AddComponent<Human>();
    }
}
