using UnityEngine;
using Utility;
using ApplicationManagers;
using Xft;

namespace Characters
{
    class HorseComponentCache: BaseComponentCache
    {
        public ParticleSystem Dust;
        
        public HorseComponentCache(GameObject owner): base(owner)
        {
            Dust = owner.transform.Find("Dust").GetComponent<ParticleSystem>();
            LoadAudio("HorseSounds", Transform);
        }
    }
}
