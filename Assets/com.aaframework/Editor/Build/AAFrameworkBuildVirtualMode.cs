using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace com.aaframework.Editor.Build
{
    [CreateAssetMenu(fileName = "AAFrameworkBuildVirtualMode.asset", menuName = "Addressables/AAFramework Builders/Virtual Mode")]
    public class AAFrameworkBuildVirtualMode : BuildScriptVirtualMode
    {
        public override string Name => "AAFramework Simulate (Virtual Mode)";

        protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext) {
            var result = base.ProcessGroup(assetGroup, aaContext);
            
            foreach (var entry in aaContext.assetEntries) {
                AtlasSpriteLocator.ProcessEntry(entry, aaContext);
            }

            return result;
        }
    }
}
