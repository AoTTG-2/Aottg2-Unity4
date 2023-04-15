using System;
using System.Collections.Generic;
using UnityEngine;
using Xft;

namespace CustomSkins
{
    class WeaponTrailCustomSkinPart : BaseCustomSkinPart
    {
        private List<XWeaponTrail> _weaponTrails;

        public WeaponTrailCustomSkinPart(BaseCustomSkinLoader loader, List<XWeaponTrail> weaponTrails, string rendererId, int maxSize, Vector2? textureScale = null) : base(loader, null, rendererId, maxSize, textureScale)
        {
            _weaponTrails = weaponTrails;
        }

        protected override bool IsValidPart()
        {
            return _weaponTrails.Count > 0 && _weaponTrails[0] != null;
        }

        protected override void DisableRenderers()
        {
            foreach (XWeaponTrail trail in _weaponTrails)
                trail.enabled = false;
        }

        protected override void SetMaterial(Material material)
        {
            foreach (XWeaponTrail trail in _weaponTrails)
                trail.MyMaterial = material;
        }

        protected override Material SetNewTexture(Texture2D texture)
        {
            _weaponTrails[0].MyMaterial.mainTexture = texture;
            if (_textureScale != _defaultTextureScale)
            {
                Vector2 scale = _weaponTrails[0].MyMaterial.mainTextureScale;
                _weaponTrails[0].MyMaterial.mainTextureScale = new Vector2(scale.x * _textureScale.x, scale.y * _textureScale.y);
            }
            SetMaterial(_weaponTrails[0].MyMaterial);
            return _weaponTrails[0].MyMaterial;
        }
    }
}
