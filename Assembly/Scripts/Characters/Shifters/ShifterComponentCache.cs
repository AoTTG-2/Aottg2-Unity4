using UnityEngine;

namespace Characters
{
    class ShifterComponentCache: BaseTitanComponentCache
    {
        public ShifterComponentCache(GameObject owner): base(owner)
        {
            LoadAudio("ShifterSounds", Neck);
            LoadAudio("TitanSounds", Neck);
        }
    }
}
