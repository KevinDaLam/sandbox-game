using UnityEngine;

namespace Starter
{
    /// <summary>
    /// Placeholder HUD UI for networking state.
    /// </summary>
    public class NetworkHudUI : MonoBehaviour
    {
        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(10, 10, 320, 120));
            GUILayout.Label("<b>--- SANDBOX PROTOTYPE ---</b>");
            GUILayout.Label("WASD: Move | Mouse: Look | E: Grab/Drop Box");
            GUILayout.EndArea();
        }
    }
}
