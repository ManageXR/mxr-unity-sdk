using System.Linq;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace MXR.SDK.Editor {
    public class SceneExportValidator : ISceneExportValidator {
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
        private SceneExportViolation GetRenderPipelineViolation() {
            var renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;
            var violation = new SceneExportViolation(
                SceneExportViolation.Types.UnsupportedRenderPipeline,
                true,
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
        /// - Error Shader. We allow this so that the export can be tested early without worrying about every material.
        /// - Universal Render Pipeline
        /// - Unlit
        /// - UI
        /// - Sprites
        /// - Skybox
        /// </summary>
        private List<SceneExportViolation> GetShaderViolations() {
            var dependencies = AssetDatabase.GetDependencies(new string[] {
                SceneManager.GetActiveScene().path
            });
            string[] supportedShaders = new string[] {
                "Universal Render Pipeline/",
                "Unlit/",
                "UI/",
                "Sprites/",
                "Skybox/",
                "Hidden/InternalErrorShader"
            };
            var unsupportedMaterials = dependencies
                .Where(x => x.EndsWith(".mat"))
                .Select(x => AssetDatabase.LoadAssetAtPath<Material>(x))
                .Where(x => {
                    // If the shader name matches any of the supported shaders, this material is supported
                    foreach (var supportedShader in supportedShaders) {
                        if (x.shader.name.StartsWith(supportedShader)) {
                            return false;
                        }
                    }
                    return true;
                });
            return unsupportedMaterials.Select(x => new SceneExportViolation(
                SceneExportViolation.Types.UnsupportedShader,
                true,
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
        private List<SceneExportViolation> GetScriptViolations() {
            var supportedDLLs = new string[]{
                "Unity.TextMeshPro.dll",
                "UnityEngine.UI.dll",
                "Unity.RenderPipelines.Universal.Runtime.dll"
            };
            var unsupportedComponents = Object.FindObjectsOfType<MonoBehaviour>()
                .Where(x => {
                    var codeBasePath = x.GetType().Assembly.Location;
                    var codeBaseFileName = Path.GetFileName(codeBasePath);
                    return !supportedDLLs.Contains(codeBaseFileName);
                });
            return unsupportedComponents.Select(x => new SceneExportViolation(
                SceneExportViolation.Types.CustomScriptFound,
                true,
                "Custom scripts/components are not supported. Please remove them from the scene.",
                x
            )).ToList();
        }

        /// <summary>
        /// Checks and ensures the scene doesn't have a camera.
        /// </summary>
        /// <returns></returns>
        private List<SceneExportViolation> GetCameraViolations() {
            var cameras = Object.FindObjectsOfType<Camera>();
            return cameras.Select(x => new SceneExportViolation(
                SceneExportViolation.Types.CameraFound,
                true,
                "Scene cameras are not supported. Please remove cameras from the scene.",
                x
            )).ToList();
        }

        /// <summary>
        /// Checks and warns if there are realtime or mixed lights in the scene.
        /// These violations are a warning, not errors that prevents export.
        /// </summary>
        /// <returns></returns>
        private List<SceneExportViolation> GetLightViolations() {
            var nonBakedLights = Object.FindObjectsOfType<Light>()
                .Where(x => x.lightmapBakeType != LightmapBakeType.Baked).ToList();
            return nonBakedLights.Select(x => new SceneExportViolation(
                SceneExportViolation.Types.NonBakedLight,
                false,
                "Realtime and Mixed lights are not recommended. " +
                "Consider lightmapping your scene with baked lights. " +
                "Only use Realtime or Mixed lights if you truly need them " +
                "as they can impact performance.",
                x
            )).ToList();
        }

        /// <summary>
        /// Checks and ensures the scene doesn't have an EventSystem
        /// </summary>
        /// <returns></returns>
        private List<SceneExportViolation> GetEventSystemViolations() {
            var eventSystems = Object.FindObjectsOfType<EventSystem>();
            return eventSystems.Select(x => new SceneExportViolation(
                SceneExportViolation.Types.EventSystemFound,
                true,
                "There cannot be an EventSystem on the scene. Please remove them from the scene.",
                x
            )).ToList();
        }
    }
}
