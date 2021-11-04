using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.U2D;

namespace com.aaframework.Runtime.Provider
{
    public class AtlasSpriteProvider : ResourceProviderBase
    {
        public override void Provide(ProvideHandle provideHandle) {
            var dependencies = new List<object>();
            provideHandle.GetDependencies(dependencies);
            if (dependencies.Count == 0) {
                CompleteWithFail(provideHandle, "Dependencies count is 0.");
                return;
            }

            var dependency = dependencies[0];
            if (dependency.GetType() != typeof(SpriteAtlas)) {
                CompleteWithFail(provideHandle, "Dependencies[0] is not a SpriteAtlas.");
                return;
            }
            
            var atlas = (SpriteAtlas) dependency;
            if (atlas == null) {
                CompleteWithFail(provideHandle, "Dependencies[0] is not a SpriteAtlas. atlas == null");
                return;
            }

            var spriteKey = provideHandle.ResourceManager.TransformInternalId(provideHandle.Location);
            var sprite = atlas.GetSprite(spriteKey);
            var success = sprite != null;
            provideHandle.Complete(sprite, success, success ? null : new System.Exception($"Sprite failed to load for location {provideHandle.Location.PrimaryKey}. atlas.GetSprite({spriteKey}) is null."));
        }

        private void CompleteWithFail(ProvideHandle provideHandle, string msg) {
            provideHandle.Complete<Sprite>(null, false, new System.Exception($"Sprite atlas failed to load for location {provideHandle.Location.PrimaryKey}. {msg}"));
        }
    }
}
