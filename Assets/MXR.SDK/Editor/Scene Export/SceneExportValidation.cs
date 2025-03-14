using System.Linq;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;

namespace MXR.SDK.Editor {
    public class Violation {
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

        public Violation(Types type, bool isWarning, string description, Object obj) {
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
        public List<Violation> Validate() {
            var violations = new List<Violation>();

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

        Violation GetRenderPipelineViolation() {
            var renderPipelineAsset = GraphicsSettings.defaultRenderPipeline;
            var violation = new Violation(
                Violation.Types.UnsupportedRenderPipeline, 
                false, 
                "Only Universal Render Pipeline is supported.", 
                null
            );
            if (renderPipelineAsset != null && renderPipelineAsset.GetType().Name == "UniversalRenderPipelineAsset")
                return null;
            else
                return violation;
        }

        List<Violation> GetShaderViolations() {
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
            return unsupportedMaterials.Select(x => new Violation(
                Violation.Types.UnsupportedShader,
                false,
                "Only default URP, Unlit, UI, Sprites and Skybox shaders are supported.",
                x)).ToList();
        }

        List<Violation> GetScriptViolations() {
            var components = Object.FindObjectsOfType<MonoBehaviour>()
                    .Where(x => !x.GetType().FullName.StartsWith("TMPro"))
                    .Where(x => !x.GetType().FullName.StartsWith("UnityEngine"));
            return components.Select(x => new Violation(
                Violation.Types.CustomScriptFound,
                false,
                "Custom scripts and components are not supported. Please remove them from the scene.",
                x
            )).ToList();
        }

        List<Violation> GetCameraViolations() {
            var cameras = Object.FindObjectsOfType<Camera>();
            return cameras.Select(x => new Violation(
                Violation.Types.CameraFound,
                false,
                "Scene cameras are not supported. Please remove cameras from the scene.",
                x
            )).ToList();
        }

        List<Violation> GetLightViolations() {
            var nonBakedLights = Object.FindObjectsOfType<Light>()
                .Where(x => x.lightmapBakeType != LightmapBakeType.Baked).ToList();
            return nonBakedLights.Select(x => new Violation(
                Violation.Types.NonBakedLight,
                true,
                "Realtime and Mixed lights are not recommended. Consider lightmapping your scene with baked lights.",
                x
            )).ToList();
        }

        List<Violation> GetEventSystemViolations() {
            var eventSystems = Object.FindObjectsOfType<EventSystem>();
            return eventSystems.Select(x => new Violation(
                Violation.Types.EventSystemFound,
                false,
                "There cannot be an EventSystem on the scene. Please remove them from the scene.",
                x
            )).ToList();
        }
    }
}
