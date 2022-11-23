using System.Linq;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;

namespace MXR.SDK.Samples {
    // NOTE: A simple library example that instantiates cells for content types.
    // Every time the Device Status of the Runtime Settings Summary changes,
    // this script destroys the previously instantiated cells and instantiates 
    // them again. Not efficient, we know!. But this is just a demo.
    public class LibraryPanel : MonoBehaviour {
        [SerializeField] Transform cellContainer;
        [SerializeField] RuntimeAppCell appCellTemplate;
        [SerializeField] WebXRAppCell webXRAppCellTemplate;
        [SerializeField] VideoCell videoCellTemplate;
        [SerializeField] GameObject errPanel;
        [SerializeField] Text errLabel;

        List<WebXRAppCell> webXRAppCells = new List<WebXRAppCell>();
        List<VideoCell> videoCells = new List<VideoCell>();
        List<RuntimeAppCell> appCells = new List<RuntimeAppCell>();

        void Start() {
            MXRManager.Init();
            // Disable the cell template gameobjects
            appCellTemplate.gameObject.SetActive(false);
            webXRAppCellTemplate.gameObject.SetActive(false);
            videoCellTemplate.gameObject.SetActive(false);

            OnRuntimeSettingsSummaryChange(MXRManager.System.RuntimeSettingsSummary);
            OnDeviceStatusChange(MXRManager.System.DeviceStatus);

            MXRManager.System.OnRuntimeSettingsSummaryChange += OnRuntimeSettingsSummaryChange;
            MXRManager.System.OnDeviceStatusChange += OnDeviceStatusChange;
        }

        void OnDestroy() {
            MXRManager.System.OnRuntimeSettingsSummaryChange -= OnRuntimeSettingsSummaryChange;
            MXRManager.System.OnDeviceStatusChange -= OnDeviceStatusChange;
        }

        void OnRuntimeSettingsSummaryChange(RuntimeSettingsSummary obj) {
            if (obj == null) return;
            Debug.Log("Runtime Settings Summary changed, destroy and instantiate cells");
            DestroyContentCells();
            InstantiateContentCells();
        }

        void OnDeviceStatusChange(DeviceStatus obj) {
            if (obj == null) return;
            Debug.Log("Device Status changed, destroy and instantiate cells");
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
                    instance.webXRApp = x;
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
                    instance.runtimeApp = x;
                    instance.status = MXRManager.System.DeviceStatus.AppInstallStatusForRuntimeApp(x);
                    instance.Refresh();
                    appCells.Add(instance);
                });
        }
    }
}
