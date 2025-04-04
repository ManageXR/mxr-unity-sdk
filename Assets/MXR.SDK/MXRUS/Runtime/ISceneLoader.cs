using Cysharp.Threading.Tasks;

namespace MXR.SDK {
    /// <summary>
    /// Loads a .mxrus file
    /// </summary>
    public interface ISceneLoader {
        UniTask<bool> Load(string sourceFilePath, string extractLocation);
        void Unload();
    }
}
