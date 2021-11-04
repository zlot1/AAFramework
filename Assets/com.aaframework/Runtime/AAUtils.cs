using System;
using System.IO;
using UnityEngine;

namespace com.aaframework.Runtime
{
    public class AAUtils
    {
        public static string GetAssetsPathByAssetsRootFolder(string assetPath) {
            var path = $"Assets/{AAConfig.AssetsRootFolder}/{assetPath}";
            return path;
        }

        public static string GetRelativePath(string assetPath) {
            return assetPath.Replace($"Assets/{AAConfig.AssetsRootFolder}/", "");
        }

        public static string GetFullPathByAssetPath(string assetPath) {
            var fullPath = Application.dataPath + "/" + assetPath.Replace("Assets/", "");
            return fullPath;
        }

        public static string GetVersionCode() {
            // 大版本
            var v = Application.version;
            
            // AA 的 catalog Hash 值末4位
            FileInfo fi = null;
            var directoryInfo = new DirectoryInfo(Application.persistentDataPath);
            var fileInfos = directoryInfo.GetFiles($"catalog_{v}.hash", SearchOption.AllDirectories);
            if (fileInfos.Length > 0) {
                fi = fileInfos[0];
            }
            
            if (fi != null && fi.Exists) {
                using var sr = fi.OpenText();
                var hash = sr.ReadToEnd();
                var subHex = hash.Substring(hash.Length - 4, 4);
                var subVer = Convert.ToInt32($"0x{subHex}", 16);
                v = $"{v}.{subVer}";
            }
            
            return v;
        }
    }
}
