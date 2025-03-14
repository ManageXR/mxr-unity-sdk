using System.Linq;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using System.IO;

namespace MXR.SDK.Editor {
    public class SceneExportViolation {
        /// <summary>
        /// Enumerates all the difference kind of violations 
        /// a scene can have that can prevent or affect export.
        /// </summary>
        public enum Types {
            /// <summary>
            /// If the project is using an unsupported render pipeline.
            /// Only the Universal Rendering Pipeline is supported.
            /// </summary>
            UnsupportedRenderPipeline,

            /// <summary>
            /// If a material is using an unsupported shader.
            /// Only URP shaders and select in-built shaders are supported.
            /// </summary>
            UnsupportedShader,

            /// <summary>
            /// If a gameobject on the scene has a custom/user-authored script
            /// </summary>
            CustomScriptFound,

            /// <summary>
            /// If the scene has a camera we prevent export 
            /// </summary>
            CameraFound,

            /// <summary>
            /// If the scene has a realtime or mixed light. This doesn't block export
            /// but is used to show a warning about potential performance issues.
            /// </summary>
            NonBakedLight,

            /// <summary>
            /// If the scene has an EventSystem. The homescreen has its own and it doesn't
            /// allow another.
            /// </summary>
            EventSystemFound
        }

        public Types Type { get; private set; }
        public bool IsWarning { get; private set; }
        public string Description { get; private set; }
        public Object Object { get; private set; }

        public SceneExportViolation(Types type, bool isWarning, string description, Object obj) {
            Type = type;
            IsWarning = isWarning;
            Description = description;
            Object = obj;
        }
    }

    public class SceneExportValidator {
        /// <summary>
        /// Returns a list of violations in the active scene
        /// </summary>
        public List<SceneExportViolation> Validate() {
            var violations = new List<SceneExportViolation>();

            var renderPipelineViolation = GetRenderPipelineViolation();
            if (renderPipelineViolation != null)
                violations.Add(renderPipelineViolation);

            violations.AddRange(GetShaderViolations());
            violations.AddRange(GetScriptViolations());
            violations.AddRange(GetCameraViolations());
            violations.AddRange(GetLightViolations());
            violations.AddRange(GetEventSystemViolations());

            return violations;
        }

        /// <summary>
        /// Checks and ensures the project not configured to use a render pipeline other than Universal Render Pipeline
        /// </summary>
        SceneExportViolation GetRenderPipelineViolation() {
            var renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;
            var violation = new SceneExportViolation(
                SceneExportViolation.Types.UnsupportedRenderPipeline, 
                false, 
                "Only Universal Render Pipeline is supported.", 
                null
            );
            if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name == "UniversalRenderPipelineAsset")
                return null;
            else
                return violation;
        }

        /// <summary>
        /// Checks and ensures the scene doesn't have materials that use unsupported shaders.
        /// Only shaders in the following namespaces/family are supported:
        /// - Universal Render Pipeline
        /// - Unlit
        /// - UI
        /// - Sprites
        /// - Skybox
        /// </summary>
        List<SceneExportViolation> GetShaderViolations() {
            var dependencies = AssetDatabase.GetDependencies(new string[] {
                SceneManager.GetActiveScene().path
            });
            var unsupportedMaterials = dependencies
                .Where(x => x.EndsWith(".mat"))
                .Select(x => AssetDatabase.LoadAssetAtPath<Material>(x))
                .Where(x => !x.shader.name.StartsWith("Universal Render Pipeline/"))
                .Where(x => !x.shader.name.StartsWith("Unlit/"))
                .Where(x => !x.shader.name.StartsWith("UI/"))
                .Where(x => !x.shader.name.StartsWith("Sprites/"))
                .Where(x => !x.shader.name.StartsWith("Skybox/"));
            return unsupportedMaterials.Select(x => new SceneExportViolation(
                SceneExportViolation.Types.UnsupportedShader,
                false,
                "Only default URP, Unlit, UI, Sprites and Skybox shaders are supported.",
                x)).ToList();
        }

        /// <summary>
        /// Checks and ensures the scene doesn't use any custom scripts. 
        /// Only scripts in the following DLLs are supported:
        /// Unity.TextMeshPro.dll
        /// UnityEngine.UI.dll
        /// Unity.RenderPipelines.Universal.Runtime.dll
        /// </summary>
        /// <returns></returns>
        List<SceneExportViolation> GetScriptViolations() {
            var supportedDLLs = new string[]{
                "Unity.TextMeshPro.dll",
                "UnityEngine.UI.dll",
                "Unity.RenderPipelines.Universal.Runtime.dll"
            };
            var unsupportedComponents = Object.FindObjectsOfType<MonoBehaviour>()
                .Where(x => {
                    var codeBasePath = x.GetType().Assembly.GetName().CodeBase;
                    var codeBaseFileName = Path.GetFileName(codeBasePath);
                    return !supportedDLLs.Contains(codeBaseFileName);
                });
            return unsupportedComponents.Select(x => new SceneExportViolation(
                SceneExportViolation.Types.CustomScriptFound,
                false,
                "Custom scripts/components are not supported. Please remove them from the scene.",
                x
            )).ToList();
        }

        /// <summary>
        /// Checks and ensures the scene doesn't have a camera.
        /// </summary>
        /// <returns></returns>
        List<SceneExportViolation> GetCameraViolations() {
            var cameras = Object.FindObjectsOfType<Camera>();
            return cameras.Select(x => new SceneExportViolation(
                SceneExportViolation.Types.CameraFound,
                false,
                "Scene cameras are not supported. Please remove cameras from the scene.",
                x
            )).ToList();
        }

        /// <summary>
        /// Checks and warns if there are realtime or mixed lights in the scene.
        /// These violations are a warning, not errors that prevents export.
        /// </summary>
        /// <returns></returns>
        List<SceneExportViolation> GetLightViolations() {
            var nonBakedLights = Object.FindObjectsOfType<Light>()
                .Where(x => x.lightmapBakeType != LightmapBakeType.Baked).ToList();
            return nonBakedLights.Select(x => new SceneExportViolation(
                SceneExportViolation.Types.NonBakedLight,
                true,
                "Realtime and Mixed lights are not recommended. " +
                "Consider lightmapping your scene with baked lights " +
                "and only using realtime lights if you truly need them" +
                "as they can impact performance.",
                x
            )).ToList();
        }

        /// <summary>
        /// Checks and ensures the scene doesn't have an EventSystem
        /// </summary>
        /// <returns></returns>
        List<SceneExportViolation> GetEventSystemViolations() {
            var eventSystems = Object.FindObjectsOfType<EventSystem>();
            return eventSystems.Select(x => new SceneExportViolation(
                SceneExportViolation.Types.EventSystemFound,
                false,
                "There cannot be an EventSystem on the scene. Please remove them from the scene.",
                x
            )).ToList();
        }
    }
}
