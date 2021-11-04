using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
#endif

namespace com.aaframework.Runtime
{
    public class AAManager
    {
        private static AAManager _instance = null;
        private static readonly object _locker = new object();
        
        public static AAManager Instance {
            get {
                if (_instance == null) {
                    lock (_locker) {
                        if (_instance == null) {
                            _instance = new AAManager();
                        }
                    }
                }
                return _instance;
            }
        }
        
        /// <summary>
        /// 获取完整的资源路径
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        private string GetAddress(string assetPath) {
            var address = $"{AAConfig.AssetsRootFolder}/{assetPath}";
            return address;
        }
        
        /// <summary>
        /// 异步加载 ScriptableObject 类型的资源
        /// </summary>
        /// <param name="assetPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> LoadScriptableAssetAsync<T>(string assetPath) where T : ScriptableObject {
#if UNITY_EDITOR
            var taskCompletionSource = new TaskCompletionSource<T>();
            var asset = EditorLoadOrCreateScriptableObjectSync<T>(assetPath);
            taskCompletionSource.SetResult(asset);
            return await taskCompletionSource.Task;
#else
            var ret = await Addressables.LoadAssetAsync<T>(GetAddress(assetPath)).Task;
            return ret;
#endif
        }
        
        /// <summary>
        /// 异步加载 csv 类型的资源
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public async Task<string> LoadCsvAssetAsync(string assetPath) {
#if UNITY_EDITOR
            var taskCompletionSource = new TaskCompletionSource<string>();
            var text = EditorLoadCsvSync(assetPath);
            taskCompletionSource.SetResult(text);
            return await taskCompletionSource.Task;
#else
            var textAsset = await Addressables.LoadAssetAsync<TextAsset>(GetAddress(assetPath)).Task;
            return textAsset.text;
#endif
        }
        
        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="assetPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public async Task<T> LoadAssetAsync<T>(string assetPath) where T : Object {
            if (string.IsNullOrEmpty(assetPath)) {
                return null;
            }
            
            var loader = Addressables.LoadAssetAsync<T>(GetAddress(assetPath));
            await loader.Task;
            if (loader.Status == AsyncOperationStatus.Succeeded) {
                return loader.Result;
            }
            
            throw loader.OperationException;
        }

        public async Task<IList<IResourceLocation>> LoadResourceLocationsAsync(string label) {
            var locations = await Addressables.LoadResourceLocationsAsync(label).Task;
            return locations;
        }

        /// <summary>
        /// 异步实例化 GameObject
        /// </summary>
        /// <param name="prefabPath"></param>
        /// <returns></returns>
        public async Task<GameObject> InstantiateAsync(string prefabPath) {
            var go = await InstantiateAsync(prefabPath, null, false);
            return go;
        }
        
        /// <summary>
        /// 异步实例化 GameObject
        /// </summary>
        /// <param name="prefab"></param>
        /// <returns></returns>
        public async Task<GameObject> InstantiateAsync(AssetReference prefab) {
            var go = await InstantiateAsync(prefab, null, false);
            return go;
        }
        
        /// <summary>
        /// 异步实例化 GameObject
        /// </summary>
        /// <param name="prefabPath"></param>
        /// <param name="parent"></param>
        /// <param name="worldPositionStays"></param>
        /// <returns></returns>
        public async Task<GameObject> InstantiateAsync(string prefabPath, Transform parent, bool worldPositionStays) {
            var go = await Addressables.InstantiateAsync(GetAddress(prefabPath), parent, worldPositionStays).Task;
            return go;
        }
        
        /// <summary>
        /// 异步实例化 GameObject
        /// </summary>
        /// <param name="prefab"></param>
        /// <param name="parent"></param>
        /// <param name="worldPositionStays"></param>
        /// <returns></returns>
        public async Task<GameObject> InstantiateAsync(AssetReference prefab, Transform parent, bool worldPositionStays) {
            var go = await prefab.InstantiateAsync(parent, false).Task;
            return go;
        }

        /// <summary>
        /// 释放异步实例化的 GameObject
        /// </summary>
        /// <param name="go"></param>
        /// <returns></returns>
        public bool ReleaseInstance(GameObject go) {
            return Addressables.ReleaseInstance(go);
        }

        /// <summary>
        /// 加载场景
        /// </summary>
        /// <param name="assetPath"></param>
        /// <param name="loadMode"></param>
        /// <param name="activateOnLoad"></param>
        /// <param name="priority"></param>
        public async Task LoadSceneAsync(string assetPath,
            LoadSceneMode loadMode = LoadSceneMode.Single,
            bool activateOnLoad = true,
            int priority = 100) {
            await Addressables.LoadSceneAsync(GetAddress(assetPath), loadMode, activateOnLoad, priority).Task;
        }

        /// <summary>
        /// 卸载场景
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="autoReleaseHandle"></param>
        public async Task UnloadSceneAsync(SceneInstance scene, bool autoReleaseHandle = true) {
            await Addressables.UnloadSceneAsync(scene, autoReleaseHandle).Task;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 同步加载 ScriptableObject 类型的资源，如果不存在则创建资源
        /// </summary>
        /// <param name="assetPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T EditorLoadOrCreateScriptableObjectSync<T>(string assetPath) where T : ScriptableObject {
            var path = AAUtils.GetAssetsPathByAssetsRootFolder(assetPath);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) {
                asset = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(asset, path);
                
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            return asset;
        }
        
        /// <summary>
        /// 同步加载 csv 类型的资源
        /// </summary>
        /// <param name="assetPath"></param>
        /// <returns></returns>
        public string EditorLoadCsvSync(string assetPath) {
            var path = AAUtils.GetAssetsPathByAssetsRootFolder(assetPath);
            var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            return textAsset == null ? null : textAsset.text;
        }
        
        /// <summary>
        /// 同步加载资源（仅限Editor下调用！！！）
        /// </summary>
        /// <param name="assetPath"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T LoadAssetSync<T>(string assetPath) where T : Object {
            var path = $"Assets/{AAConfig.AssetsRootFolder}/{assetPath}";
            var ret = AssetDatabase.LoadAssetAtPath<T>(path);
            return ret;
        }
#endif
    }
}
