using System.IO;
using Unity.Barracuda;
namespace TCS.MLAgents.PredVsPray {
    [CreateAssetMenu( menuName = "Create MlBrainData", fileName = "MlBrainData", order = 0 )] 
    public class MlBrainData : NNModelData {
        public byte[] Data {
            get => Value;
            set => Value = value;
        }
        
        public bool IsEmpty => Value == null || Value.Length == 0;
        
        /// <summary>
        /// Load model data from a .onnx file at the specified path
        /// </summary>
        /// <param name="filePath">Path to the .onnx model file</param>
        public void LoadFromFile(string filePath) {
            if (File.Exists(filePath)) {
                Data = File.ReadAllBytes(filePath);
                Debug.Log($"Loaded model data from {filePath} ({Data.Length} bytes)");
            }
            else {
                Debug.LogError($"Model file not found: {filePath}");
            }
        }
    }
}