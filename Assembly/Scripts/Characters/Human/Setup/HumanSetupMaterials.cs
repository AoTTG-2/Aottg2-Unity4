using ApplicationManagers;
using System;
using System.Collections.Generic;
using UnityEngine;
using Utility;

namespace Characters
{
    public class HumanSetupMaterials
    {
        public static Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static void Init()
        {
            AddMaterial("AOTTG_HERO_3DMG");
            AddMaterial("aottg_hero_AHSS_3dmg");
            AddMaterial("aottg_hero_annie_cap_causal");
            AddMaterial("aottg_hero_annie_cap_uniform");
            AddMaterial("aottg_hero_brand_sc");
            AddMaterial("aottg_hero_brand_mp");
            AddMaterial("aottg_hero_brand_g");
            AddMaterial("aottg_hero_brand_ts");
            AddMaterial("skin_blades1");
            AddMaterial("skin_blades2");
            AddMaterial("skin_AHSS1");
            AddMaterial("skin_AHSS2");
            AddMaterial("skin_TS1");
            AddMaterial("skin_TS2");
            AddMaterial("aottg_hero_skin_2");
            AddMaterial("aottg_hero_skin_3");
            AddMaterial("aottg_hero_casual_fa_1");
            AddMaterial("aottg_hero_casual_fa_2");
            AddMaterial("aottg_hero_casual_fa_3");
            AddMaterial("aottg_hero_casual_fb_1");
            AddMaterial("aottg_hero_casual_fb_2");
            AddMaterial("aottg_hero_casual_ma_1");
            AddMaterial("aottg_hero_casual_ma_1_ahss");
            AddMaterial("aottg_hero_casual_fa_1_ahss");
            AddMaterial("aottg_hero_casual_ma_2");
            AddMaterial("aottg_hero_casual_ma_3");
            AddMaterial("aottg_hero_casual_mb_1");
            AddMaterial("aottg_hero_casual_mb_2");
            AddMaterial("aottg_hero_casual_mb_3");
            AddMaterial("aottg_hero_casual_mb_4");
            AddMaterial("aottg_hero_uniform_fa_1");
            AddMaterial("aottg_hero_uniform_fa_2");
            AddMaterial("aottg_hero_uniform_fa_3");
            AddMaterial("aottg_hero_uniform_fb_1");
            AddMaterial("aottg_hero_uniform_fb_2");
            AddMaterial("aottg_hero_uniform_ma_1");
            AddMaterial("aottg_hero_uniform_ma_2");
            AddMaterial("aottg_hero_uniform_ma_3");
            AddMaterial("aottg_hero_uniform_mb_1");
            AddMaterial("aottg_hero_uniform_mb_2");
            AddMaterial("aottg_hero_uniform_mb_3");
            AddMaterial("aottg_hero_uniform_mb_4");
            AddMaterial("hair_annie");
            AddMaterial("hair_armin");
            AddMaterial("hair_boy1");
            AddMaterial("hair_boy2");
            AddMaterial("hair_boy3");
            AddMaterial("hair_boy4");
            AddMaterial("hair_eren");
            AddMaterial("hair_girl1");
            AddMaterial("hair_girl2");
            AddMaterial("hair_girl3");
            AddMaterial("hair_girl4");
            AddMaterial("hair_girl5");
            AddMaterial("hair_hanji");
            AddMaterial("hair_jean");
            AddMaterial("hair_levi");
            AddMaterial("hair_marco");
            AddMaterial("hair_mike");
            AddMaterial("hair_petra");
            AddMaterial("hair_rico");
            AddMaterial("hair_sasha");
            AddMaterial("hair_mikasa");
            AddMaterial("HumanFace", "HumanFace");
        }

        private static void AddMaterial(string tex, string mat = "HumanCostume")
        {
            Texture texture = (Texture2D)AssetBundleManager.LoadAsset(tex + "Tex");
            Material material = AssetBundleManager.InstantiateAsset<Material>(mat + "Mat");
            material.mainTexture = texture;
            Materials.Add(tex, material);
        }
    }
}
