using UnityEngine;
using UnityEngine.InputSystem;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        private void ReadCorruptorClick()
        {
            if (ScreenState != CampaignScreen.Explore && ScreenState != CampaignScreen.Capture) return;
            if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
            Vector2 screen = Mouse.current.position.ReadValue();
            Vector2 canvas = new Vector2((screen.x - uiOffset.x) / uiScale, (Screen.height - screen.y - uiOffset.y) / uiScale);
            if (canvas.x < 275 || canvas.x > 940 || canvas.y < 95 || canvas.y > 550) return;
            Ray ray = view.ScreenPointToRay(screen);
            if (Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed)
            {
                if (Physics.Raycast(ray, out RaycastHit ground, 500, 1 << CampaignWorld.TerrainLayer, QueryTriggerInteraction.Ignore)) TryRiftBurst(ground.point);
            }
            else if (Physics.Raycast(ray, out RaycastHit hit, 500, (1 << CampaignWorld.CreatureLayer) | (1 << CampaignWorld.TerrainLayer), QueryTriggerInteraction.Collide))
            {
                WildCreature target = World.wild.Find(w => w.root == hit.collider.gameObject);
                if (target != null) TryCorrupt(target);
            }
        }
        private void TogglePause()
        {
            if (netMode == NetMode.Client) NetCommand("pause");
            else paused = !paused;
        }
        private void BackToTitle()
        {
            NetDisconnect(); Run = null; netRoomCode = ""; OpenMenu();
        }
    }
}
