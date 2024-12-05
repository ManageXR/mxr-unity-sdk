using System.Collections.Generic;

namespace MXR.SDK.Editor {
    public interface ISceneExportValidator {
        List<SceneExportViolation> Validate();
    }
}
