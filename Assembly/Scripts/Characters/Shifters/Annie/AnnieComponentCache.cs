using UnityEngine;

namespace Characters
{
    class AnnieComponentCache: ShifterComponentCache
    {
        public AnnieComponentCache(GameObject owner): base(owner)
        {
            Head = Transform.Find("Armature_FemT/Core/Controller_Body/hip/spine/chest/neck/head");
            Neck = Transform.Find("Armature_FemT/Core/Controller_Body/hip/spine/chest/neck");
            Core = Transform.Find("Armature_FemT/Core");
            Hip = Transform.Find("Armature_FemT/Core/Controller_Body/hip");
            LoadAudio("ShifterSounds", Neck);
            LoadAudio("TitanSounds", Neck);
        }
    }
}
