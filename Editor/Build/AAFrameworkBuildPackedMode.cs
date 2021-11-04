using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace com.aaframework.Editor.Build
{
    [CreateAssetMenu(fileName = "AAFrameworkBuildPackedMode.asset", menuName = "Addressables/AAFramework Builders/Packed Mode")]
    public class AAFrameworkBuildPackedMode : BuildScriptPackedMode
    {
        public override string Name => "AAFramework Build Script";

        protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext) {
            var result = base.ProcessGroup(assetGroup, aaContext);
            
            foreach (var entry in aaContext.assetEntries) {
                AtlasSpriteLocator.ProcessEntry(entry, aaContext);
            }

            return result;
        }
    }
}
