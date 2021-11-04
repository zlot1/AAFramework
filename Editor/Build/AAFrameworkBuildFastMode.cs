using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets.Initialization;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.AddressableAssets.ResourceProviders;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.Util;

namespace com.aaframework.Editor.Build
{
    /// <summary>
    /// 由于 Addressables 1.16.19 后的 'BuildScriptFastMode' 并不生成 asset catalog，这会导致无法对 entries 进行处理
    /// 所有下面的代码主要自 Addressables 1.10.0 拷贝而来
    /// </summary>
    [CreateAssetMenu(fileName = "AAFrameworkBuildFastMode.asset", menuName = "Addressables/AAFramework Builders/Fast Mode")]
    public class AAFrameworkBuildFastMode : BuildScriptBase
    {
        private const string EditorSceneListPath = "Scenes In Build";
        private const string ResourcesPath = "*/Resources/";

        public override string Name => "AAFramework AssetDatabase (Fast Mode)";

        public override bool CanBuildData<T>() {
            return typeof(T).IsAssignableFrom(typeof(AddressablesPlayModeBuildResult));
        }
        
        private string PathFormat = "{0}Library/com.unity.addressables/{1}_BuildScriptFastMode.json";
        public override void ClearCachedData() {
            DeleteFile(string.Format(PathFormat, "", "catalog"));
            DeleteFile(string.Format(PathFormat, "", "settings"));
        }

        public override bool IsDataBuilt() {
            var catalogPath = string.Format(PathFormat, "", "catalog");
            var settingsPath = string.Format(PathFormat, "", "settings");
            return File.Exists(catalogPath) && File.Exists(settingsPath);
        }

        protected override TResult BuildDataImplementation<TResult>(AddressablesDataBuilderInput context) {
            TResult result = default(TResult);

            var aaSettings = context.AddressableSettings;

            //create runtime data
            var aaContext = new AddressableAssetsBuildContext
            {
                Settings = aaSettings,
                runtimeData = new ResourceManagerRuntimeData(),
                bundleToAssetGroup = null,
                locations = new List<ContentCatalogDataEntry>(),
                providerTypes = new HashSet<Type>()
            };
            aaContext.runtimeData.BuildTarget = context.Target.ToString();
            aaContext.runtimeData.LogResourceManagerExceptions = aaSettings.buildSettings.LogResourceManagerExceptions;
            aaContext.runtimeData.CatalogLocations.Add(new ResourceLocationData(
                new[] { ResourceManagerRuntimeData.kCatalogAddress },
                string.Format(PathFormat, "file://{UnityEngine.Application.dataPath}/../", "catalog"),
                typeof(ContentCatalogProvider),
                typeof(ContentCatalogData)));

            var errorString = ProcessAllGroups(aaContext);
            if (!string.IsNullOrEmpty(errorString)) {
                result = AddressableAssetBuildResult.CreateResult<TResult>(null, 0, errorString);
            }

            if (result == null) {
                foreach (var io in aaSettings.InitializationObjects) {
                    if (io is IObjectInitializationDataProvider) {
                        var initData = (io as IObjectInitializationDataProvider)
                            .CreateObjectInitializationData();

                        aaContext
                            .runtimeData
                            .InitializationObjects
                            .Add(initData);
                    }
                }

                var settingsPath = string.Format(PathFormat, "", "settings");
                WriteFile(settingsPath, JsonUtility.ToJson(aaContext.runtimeData), context.Registry);

                // save catalog
                var catalogData = new ContentCatalogData(
                    aaContext.locations,
                    ResourceManagerRuntimeData.kCatalogAddress);

                foreach (var t in aaContext.providerTypes) {
                    var provider = ObjectInitializationData.CreateSerializedInitializationData(t);
                    catalogData.ResourceProviderData.Add(provider);
                }

                catalogData.ResourceProviderData.Add(ObjectInitializationData.CreateSerializedInitializationData<AssetDatabaseProvider>());
                catalogData.InstanceProviderData = ObjectInitializationData.CreateSerializedInitializationData(instanceProviderType.Value);
                catalogData.SceneProviderData = ObjectInitializationData.CreateSerializedInitializationData(sceneProviderType.Value);
                WriteFile(string.Format(PathFormat, "", "catalog"), JsonUtility.ToJson(catalogData), context.Registry);

                //inform runtime of the init data path
                var runtimeSettingsPath = string.Format(PathFormat, "file://{UnityEngine.Application.dataPath}/../", "settings");
                PlayerPrefs.SetString(UnityEngine.AddressableAssets.Addressables.kAddressablesRuntimeDataPath, runtimeSettingsPath);
                result = AddressableAssetBuildResult.CreateResult<TResult>(settingsPath, aaContext.locations.Count);
            }

            return result;
        }

        protected override string ProcessGroup(AddressableAssetGroup assetGroup, AddressableAssetsBuildContext aaContext) {
            foreach (var entry in assetGroup.entries) {
                if (entry == null || string.IsNullOrEmpty(entry.AssetPath)) {
                    continue;
                }

                // If the entry is a folder, process each of the assets in the folder. Otherwise,
                // process the entry as a single asset.
                if (entry.AssetPath == ResourcesPath
                    || entry.AssetPath == EditorSceneListPath
                    || File.GetAttributes(entry.AssetPath).HasFlag(FileAttributes.Directory)) {
                    var allEntries = new List<AddressableAssetEntry>();
                    entry.GatherAllAssets(allEntries, false, true, false);
                    foreach (var folderEntry in allEntries) {
                        // Create default catalog entries.
                        folderEntry.CreateCatalogEntries(
                            aaContext.locations,
                            false,
                            typeof(AssetDatabaseProvider).FullName,
                            null,
                            null,
                            aaContext.providerTypes);

                        // Create additional catalog entries if the asset is a `SpriteAtlas`.
                        AtlasSpriteLocator.ProcessEntry(folderEntry, aaContext);
                    }
                }
                else {
                    // Create default catalog entries.
                    entry.CreateCatalogEntries(
                        aaContext.locations,
                        false,
                        typeof(AssetDatabaseProvider).FullName,
                        null,
                        null,
                        aaContext.providerTypes);

                    // Create additional catalog entries if the asset is a `SpriteAtlas`.
                    AtlasSpriteLocator.ProcessEntry(entry, aaContext);
                }
            }

            return string.Empty;
        }
    }
}
