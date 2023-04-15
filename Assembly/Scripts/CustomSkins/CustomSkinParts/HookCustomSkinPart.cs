using System;
using System.Collections.Generic;
using UnityEngine;

namespace CustomSkins
{
    class HookCustomSkinPart : BaseCustomSkinPart
    {
        public Material HookMaterial;
        public bool Transparent;

        public HookCustomSkinPart(BaseCustomSkinLoader loader, string rendererId, int maxSize, Vector2? textureScale = null) : base(loader, null, rendererId, maxSize, textureScale)
        {
        }

        protected override bool IsValidPart()
        {
            return true;
        }

        protected override void DisableRenderers()
        {
            Transparent = true;
        }

        protected override void SetMaterial(Material material)
        {
            HookMaterial = material;
        }

        protected override Material SetNewTexture(Texture2D texture)
        {
            Material material = new Material(Shader.Find("Transparent/Diffuse"));
            material.mainTexture = texture;
            SetMaterial(material);
            return material;
        }
    }
}
