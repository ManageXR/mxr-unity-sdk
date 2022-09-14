using System.Linq;
using System.Collections.Generic;

using UnityEngine;

namespace MXR.SDK.Samples {
    public class LibraryPanel : MonoBehaviour {
        [SerializeField] Transform cellContainer;
        [SerializeField] RuntimeAppCell appCellTemplate;
        [SerializeField] WebXRAppCell webXRAppCellTemplate;
        [SerializeField] VideoCell videoCellTemplate;

        List<WebXRAppCell> webXRAppCells = new List<WebXRAppCell>();
        List<VideoCell> videoCells = new List<VideoCell>();
        List<RuntimeAppCell> appCells = new List<RuntimeAppCell>();

        void Awake() {
            MXRManager.Init();
        }

        void Start() {
            // Disable the cell template gameobjects
            appCellTemplate.gameObject.SetActive(false);
            webXRAppCellTemplate.gameObject.SetActive(false);
            videoCellTemplate.gameObject.SetActive(false);

            DestroyContentCells();
            InstantiateContentCells();

            MXRManager.System.OnRuntimeSettingsSummaryChange += OnRuntimeSettingsSummaryChange;
            MXRManager.System.OnDeviceStatusChange += OnDeviceStatusChange;
        }

        void OnDestroy() {
            MXRManager.System.OnRuntimeSettingsSummaryChange -= OnRuntimeSettingsSummaryChange;
            MXRManager.System.OnDeviceStatusChange -= OnDeviceStatusChange;
        }

        void OnRuntimeSettingsSummaryChange(RuntimeSettingsSummary runtime) {
            DestroyContentCells();
            InstantiateContentCells();
        }

        void OnDeviceStatusChange(DeviceStatus device) {
            DestroyContentCells();
            InstantiateContentCells();
        }

        // Destroy all the cell instances of each content type 
        // that have been created.
        void DestroyContentCells() {
            foreach (var instance in webXRAppCells)
                Destroy(instance.gameObject);
            webXRAppCells.Clear();

            foreach (var instance in videoCells)
                Destroy(instance.gameObject);
            videoCells.Clear();

            foreach (var cell in appCells)
                Destroy(cell.gameObject);
            appCells.Clear();
        }

        void InstantiateContentCells() {
            InstantiateAppCells();
            InstantiateWebXRCells();
            InstantaiteVideoCells();
        }

        void InstantiateWebXRCells() {
            MXRManager.System.RuntimeSettingsSummary.webXRApps.Values.ToList()
                .ForEach(x => {
                    var instance = Instantiate(webXRAppCellTemplate, cellContainer);
                    instance.gameObject.SetActive(true);
                    instance.site = x;
                    instance.Refresh();
                    webXRAppCells.Add(instance);
                });
        }

        void InstantaiteVideoCells() {
            MXRManager.System.RuntimeSettingsSummary.videos.Values.ToList()
                .ForEach(x => {
                    var instance = Instantiate(videoCellTemplate, cellContainer);
                    instance.gameObject.SetActive(true);
                    instance.video = x;
                    instance.status = MXRManager.System.DeviceStatus.FileInstallStatusForVideo(x);
                    instance.Refresh();
                    videoCells.Add(instance);
                });
        }

        void InstantiateAppCells() {
            MXRManager.System.RuntimeSettingsSummary.apps.Values.ToList()
                .ForEach(x => {
                    var instance = Instantiate(appCellTemplate, cellContainer);
                    instance.gameObject.SetActive(true);
                    instance.app = x;
                    instance.status = MXRManager.System.DeviceStatus.AppInstallStatusForRuntimeApp(x);
                    instance.Refresh();
                    appCells.Add(instance);
                });
        }
    }
}
