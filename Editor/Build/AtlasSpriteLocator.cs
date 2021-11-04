using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets.Build.DataBuilders;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.U2D;
using AtlasSpriteProvider = com.aaframework.Runtime.Provider.AtlasSpriteProvider;

namespace com.aaframework.Editor.Build
{
    public static class AtlasSpriteLocator
    {
        /// <summary>
        /// 找出 atlas 中所有的 sprite，写入 catalog 以便 Runtime 解析 dependencies 的时候能通过 locator 找到所属 atlas
        /// 进而实现通过 sprite 路径也能自动加载所属的 atlas
        /// </summary>
        /// <param name="entry"></param>
        /// <param name="context"></param>
        public static void ProcessEntry(AddressableAssetEntry entry, AddressableAssetsBuildContext context) {
            var type = AssetDatabase.GetMainAssetTypeAtPath(entry.AssetPath);

            if (type == typeof(SpriteAtlas)) {
                var atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(entry.AssetPath);

                var dependencies = AssetDatabase.GetDependencies(entry.AssetPath);
                var spritePaths = new Dictionary<string, string>();
                var startIndex = "Assets/".Length;
                for (var i = 0; i < dependencies.Length; i++) {
                    var dependency = dependencies[i];
                    if (dependency.EndsWith(".spriteatlas") || !dependency.Contains(".")) {
                        continue;
                    }
                    var name = Path.GetFileNameWithoutExtension(dependency);
                    if (!spritePaths.ContainsKey(name)) {
                        spritePaths.Add(name, dependency.Substring(startIndex));
                    }
                }

                var sprites = new Sprite[atlas.spriteCount];
                atlas.GetSprites(sprites);

                var entryKeys = entry.CreateKeyList();

                for (var i = 0; i < atlas.spriteCount; i++) {
                    var spriteName = sprites[i].name;

                    // GetSprites 后会自动加"(Clone)"后缀
                    if (spriteName.EndsWith("(Clone)")) {
                        spriteName = spriteName.Replace("(Clone)", "");
                    }

                    var exist = spritePaths.TryGetValue(spriteName, out var spritePath);
                    if (exist) {
                        context.locations.Add(new ContentCatalogDataEntry(
                            typeof(Sprite),
                            spriteName,
                            typeof(AtlasSpriteProvider).FullName,
                            new object[] { spritePath },
                            new object[] { entryKeys[0] }));
                    }
                    else {
                        Debug.LogError($"{entry.AssetPath} 中的 Sprite[{spriteName}]，并未找到对应的路径");
                    }
                }

                context.providerTypes.Add(typeof(AtlasSpriteProvider));
            }
        }
    }
}
