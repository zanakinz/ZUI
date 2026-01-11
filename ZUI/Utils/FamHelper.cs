using ProjectM;
using Unity.Entities;

namespace ZUI.Utils
{
    internal static class FamHelper
    {
        //static readonly PrefabGUID _dominateBuff = new(-1447419822);
        
        public static Entity FindActiveFamiliar(Entity playerCharacter)
        {
            if (playerCharacter.TryGetBuffer<FollowerBuffer>(out var followers) && !followers.IsEmpty)
            {
                foreach (var follower in followers)
                {
                    //TODO find some fam check if any
                    Entity familiar = follower.Entity._Entity;
                    return familiar;
                }
            }

            return Entity.Null;
        }
    }
}
