using UnityEngine;

// Attach to any object that makes noise (player footsteps, opening chests, etc.)
// Fires GameEvents.OnSoundEmitted so any AI listening can react
public class SoundEmitter : MonoBehaviour
{
    // Call this from any script that wants to produce a sound
    // intensity: 0 = very quiet, 1 = very loud
    public static void Emit(Vector3 position, float intensity)
    {
        GameEvents.OnSoundEmitted?.Invoke(position, intensity);
    }
}
