using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace com.aaframework.Runtime
{
    public class AADownloader : MonoBehaviour
    {
        public static AADownloader Instance;

        [RuntimeInitializeOnLoadMethod]
        private static void RuntimeInitializeOnLoad() {
            var go = new GameObject("AADownloader", typeof(AADownloader));
            DontDestroyOnLoad(go);
        }

        private void Awake() {
            Instance = this;
        }

        public struct AAUpdateInfo
        {
            public bool NeedUpdate;
            public long DownloadSize;
        }
        
        private readonly List<object> _resourceLocators = new List<object>();
        
        private bool _downloading = false;
        private AsyncOperationHandle _downloadDependenciesHandle;
        
        private Action<DownloadStatus> _downloadStatusCallback;

        public void DeleteCatalogs() {
            var sb = new StringBuilder();
            sb.AppendLine($"Delete all Catalog Files in \"{Application.persistentDataPath}\":");

            var files = new List<string>();
            
            if (Directory.Exists(Application.persistentDataPath)) {
                var directoryInfo = new DirectoryInfo(Application.persistentDataPath);
                var fileInfos = directoryInfo.GetFiles("catalog_*.hash", SearchOption.AllDirectories);
                foreach (var fileInfo in fileInfos) {
                    files.Add(fileInfo.FullName);
                }
                fileInfos = directoryInfo.GetFiles("catalog_*.json", SearchOption.AllDirectories);
                foreach (var fileInfo in fileInfos) {
                    files.Add(fileInfo.FullName);
                }
            }
            
            foreach (var file in files) {
                sb.AppendLine(file);
                File.Delete(file);
            }
            
            Debug.Log(sb.ToString());
        }

        /// <summary>
        /// 检查是否需要更新 catalog
        /// </summary>
        /// <returns></returns>
        public async Task<AAUpdateInfo> CheckCatalogUpdate() {
            var info = new AAUpdateInfo {
                NeedUpdate = false,
                DownloadSize = 0
            };

            await Addressables.InitializeAsync().Task;
        
            var catalogs = await Addressables.CheckForCatalogUpdates().Task;
            if (catalogs == null || catalogs.Count == 0) {
                Debug.Log("No Need to Update Catalogs. Load cache Catalog List.");
                catalogs = LoadCacheCatalogs();
                if (catalogs == null) {
                    return info;
                }
            }
            else {
                foreach (var catalog in catalogs) {
                    Debug.Log($"Need Update Catalog: {catalog}");
                }
                // 缓存目录
                CacheCatalogList(catalogs);
            }
            
            _resourceLocators.Clear();

            var locators = await Addressables.UpdateCatalogs(catalogs).Task;
            foreach (var locator in locators) {
                _resourceLocators.AddRange(locator.Keys);
            }

            info.DownloadSize = await Addressables.GetDownloadSizeAsync(_resourceLocators).Task;
            if (info.DownloadSize > 0) {
                info.NeedUpdate = true;
            }

            return info;
        }

        /// <summary>
        /// 开始下载
        /// </summary>
        public void StartDownload(Action<DownloadStatus> downloadStatusCallback) {
            if (!_downloading) {
                _downloadStatusCallback = downloadStatusCallback;
                StartCoroutine(DownloadFromServer());
            }
        }

        private void CacheCatalogList(List<string> catalogs) {
            var path = Path.Combine(Application.persistentDataPath, "catalogs.json");
            var json = JsonUtility.ToJson(catalogs);
            using (var stream = new FileStream(path, FileMode.Create, FileAccess.ReadWrite)) {
                var bytes = new List<byte> { };
                var bts = System.Text.Encoding.UTF8.GetBytes(json);
                for (var i = 0; i < bts.Length; i++) {
                    bytes.Add(bts[i]);
                }
                stream.Write(bytes.ToArray(), 0, bytes.Count);
            }
        }

        private List<string> LoadCacheCatalogs() {
            var path = Path.Combine(Application.persistentDataPath, "catalogs.json");
            if (!File.Exists(path)) {
                return null;
            }
            
            try {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read)) {
                    var bufferLen = stream.Length;
                    var buf = new byte[bufferLen];
                    stream.Read(buf, 0, (int)bufferLen);
                    var json = System.Text.Encoding.UTF8.GetString(buf);
                    var list = JsonUtility.FromJson<List<string>>(json);
                    return list;
                }
            }
            catch (Exception e) {
                Debug.LogError(e);
                return null;
            }
        }

        private void Update() {
            if (_downloading) {
                var downloadStatus = _downloadDependenciesHandle.GetDownloadStatus();
                _downloadStatusCallback(downloadStatus);
            }
        }
        
        private IEnumerator DownloadFromServer() {
            _downloading = true;
        
            _downloadDependenciesHandle = Addressables.DownloadDependenciesAsync(_resourceLocators, Addressables.MergeMode.Union);
            yield return _downloadDependenciesHandle;
        
            _downloading = false;

            var sb = new StringBuilder();

            sb.AppendLine("Download: ");
            foreach (var item in (List<IAssetBundleResource>)_downloadDependenciesHandle.Result) {
                var ab = item.GetAssetBundle();
                sb.AppendLine($"\tab > {ab.name}");
                foreach (var assetName in ab.GetAllAssetNames()) {
                    sb.AppendLine($"\t\tasset > {assetName}");
                }
            }
        
            Debug.Log(sb.ToString());

            _downloadStatusCallback(_downloadDependenciesHandle.GetDownloadStatus());
        
            Addressables.Release(_downloadDependenciesHandle);
        }
    }
}