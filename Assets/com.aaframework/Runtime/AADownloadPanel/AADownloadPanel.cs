using System;
using com.aaframework.Runtime;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace HeSh.Game.Loading
{
    public class AADownloadPanel : MonoBehaviour
    {
        public static AADownloadPanel Instance;
        
        public CanvasGroup canvasGroup;

        public Text downloadHintLabel;
        public Text downloadPercentLabel;
        public Slider downloadBar;
        public Text downloadProgressLabel;

        public Button downloadButton;
        public Button cancelButton;

        private Action _finishCallback;

        private const long SIZE_KB = 1024;
        private const long SIZE_MB = SIZE_KB * 1024;
        private const long SIZE_GB = SIZE_MB * 1024;

        private void Awake() {
            Instance = this;

            canvasGroup.alpha = 0;
        }

        public void Show(long downloadSize, Action finishCallback) {
            canvasGroup.alpha = 1;

            _finishCallback = finishCallback;

            downloadHintLabel.text = $"检测到更新：<color=#E0C450>{GetSizeText(downloadSize)}</color>";
            
            downloadPercentLabel.gameObject.SetActive(false);
            downloadBar.gameObject.SetActive(false);
            downloadProgressLabel.gameObject.SetActive(false);
            downloadButton.gameObject.SetActive(true);
            cancelButton.gameObject.SetActive(true);
        }

        private string GetSizeText(long size) {
            if (size >= SIZE_GB) {
                return $"{size / SIZE_GB:F2}GB";
            }
            
            if (size >= SIZE_MB) {
                return $"{size / SIZE_MB:F2}MB";
            }
            
            return $"{size / SIZE_KB:F2}KB";
        }

        public void ToggleDownloadButton() {
            downloadPercentLabel.gameObject.SetActive(true);
            downloadBar.gameObject.SetActive(true);
            downloadProgressLabel.gameObject.SetActive(true);
            downloadButton.gameObject.SetActive(false);
            cancelButton.gameObject.SetActive(false);
            
            AADownloader.Instance.StartDownload(OnDownloadStatus);
        }

        public void ToggleCancelButton() {
            _finishCallback.Invoke();
        }

        private void OnDownloadStatus(DownloadStatus status) {
            var downloaded = GetSizeText(status.DownloadedBytes);
            var total = GetSizeText(status.TotalBytes);
            downloadHintLabel.text = $"下载资源中...";
            downloadPercentLabel.text = $"{status.Percent:P}";
            downloadBar.value = status.Percent;
            downloadProgressLabel.text = $"{downloaded} / {total}";

            if (status.IsDone) {
                _finishCallback.Invoke();
            }
        }
    }
}