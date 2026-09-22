// ICameraEffect
// Responsibility: Contract for one additive camera layer (headbob, landing dip, lean, FOV kick).
// Effects never touch the camera transform; they only add into the shared CameraOffset.
namespace Game.Player
{
    public interface ICameraEffect
    {
        void Evaluate(float deltaTime, ref CameraOffset offset);
    }
}
