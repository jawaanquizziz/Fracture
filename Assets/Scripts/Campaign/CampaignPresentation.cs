using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Valmon.Campaign
{
    public sealed partial class CampaignWorld
    {
        private readonly List<Transform> presentationActors = new List<Transform>();
        private readonly List<Vector3> presentationPositions = new List<Vector3>();
        private float presentationTime;
        private bool collectionView;
        private Material Glow(Color color)
        {
            Material material = Material(color);
            material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor", color * 2.2f);
            material.SetFloat("_Smoothness", .65f); return material;
        }
        private void StadiumRing(float radius, float y, Color color, float width = .08f)
        {
            GameObject obj = new GameObject("Stadium light ring"); obj.transform.SetParent(presentationRoot.transform, false);
            LineRenderer line = obj.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true;
            line.positionCount = 96; line.widthMultiplier = width; line.sharedMaterial = Glow(color);
            for (int i = 0; i < 96; i++) { float a = i * Mathf.PI * 2 / 96; line.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius + 4)); }
        }
        private void PresentationActor(GameObject model, Vector3 position, float size, float yawOffset = 0, bool fitHeight = false)
        {
            GameObject actor = Model(model, presentationRoot.transform, position, size, fitHeight);
            actor.transform.rotation = Quaternion.Euler(0, 180 + yawOffset, 0);
            presentationActors.Add(actor.transform); presentationPositions.Add(position);
        }
        private void BuildStadium()
        {
            collectionView = false; presentationTime = 0;
            presentationActors.Clear(); presentationPositions.Clear();
            presentationRoot = new GameObject("VALOMON Championship Stadium");
            Platform("Obsidian arena", presentationRoot.transform, new Vector3(0, -.35f, 4), new Vector3(43, .5f, 40), new Color(.035f, .065f, .13f));
            StadiumRing(7.7f, -.04f, new Color(.03f, .8f, 1));
            StadiumRing(13f, .02f, new Color(.26f, .25f, .7f), .12f);
            StadiumRing(18f, 8.5f, new Color(.22f, .7f, 1), .16f);
            StadiumRing(18.4f, 9.2f, new Color(.65f, .32f, 1), .1f);
            for (int i = 0; i < 56; i++)
            {
                float a = i * Mathf.PI * 2 / 56;
                Vector3 p = new Vector3(Mathf.Cos(a) * 18, 0, Mathf.Sin(a) * 18 + 4);
                if (p.z < -4) continue;
                for (int tier = 0; tier < 3; tier++)
                {
                    GameObject seat = Platform("Stadium terraces", presentationRoot.transform, p + new Vector3(Mathf.Cos(a) * tier, 1 + tier * 1.2f, Mathf.Sin(a) * tier), new Vector3(1.7f, .8f, 1.5f), new Color(.045f + tier * .015f, .09f, .19f));
                    seat.transform.rotation = Quaternion.Euler(0, -a * Mathf.Rad2Deg, 0);
                }
                if (i % 4 == 0)
                {
                    GameObject lamp = Platform("Championship floodlight", presentationRoot.transform, p + Vector3.up * 8, new Vector3(1.5f, .16f, .7f), Color.white);
                    lamp.GetComponent<Renderer>().sharedMaterial = Glow(new Color(.65f, .88f, 1));
                }
            }
            // Every available species is present in the stadium, with larger hero creatures at the sides.
            for (int i = 0; i < catalog.species.Length; i++)
            {
                float a = Mathf.Lerp(-145, 145, (float)i / (catalog.species.Length - 1)) * Mathf.Deg2Rad;
                Vector3 p = new Vector3(Mathf.Sin(a) * 11.2f, .2f, 6 + Mathf.Cos(a) * 7f);
                float size = 2f;
                bool hero = catalog.species[i].id == "charizard" || catalog.species[i].id == "blastoise";
                if (catalog.species[i].id == "charizard") { p = new Vector3(-8.4f, .2f, -.7f); size = 6.1f; }
                if (catalog.species[i].id == "blastoise") { p = new Vector3(8.4f, .2f, -.7f); size = 5.6f; }
                PresentationActor(catalog.species[i].model, p, size, 0, hero);
                Platform("Creature plinth", presentationRoot.transform, p - Vector3.up * .15f, new Vector3(1.9f, .15f, 1.9f), new Color(.07f, .16f, .24f));
            }
            for (int i = 0; i < catalog.characters.Length; i++)
            {
                float x = i < 2 ? -4.5f - i * 1.7f : 4.5f + (i - 2) * 1.7f;
                PresentationActor(catalog.characters[i].model, new Vector3(x, .15f, -3.4f), 3.2f, catalog.characters[i].yawOffset, true);
            }
            camera.backgroundColor = new Color(.015f, .025f, .07f); camera.fieldOfView = 48;
            camera.transform.position = new Vector3(0, 5.2f, -20.5f);
            camera.transform.LookAt(new Vector3(0, 2.6f, 3));
        }
        public void TickPresentation(float dt, float intro = 1)
        {
            if (mapMenu) { TickMapMenu(dt); return; }
            if (presentationRoot == null || presentationActors.Count == 0) return;
            presentationTime += dt;
            for (int i = 0; i < presentationActors.Count; i++)
            {
                Transform actor = presentationActors[i]; if (actor == null) continue;
                actor.localPosition = presentationPositions[i] + Vector3.up * (Mathf.Sin(presentationTime * 1.4f + i) * .07f);
                if (collectionView) actor.Rotate(0, dt * 12, 0, Space.World);
            }
            if (collectionView) return;
            Vector3 position = new Vector3(Mathf.Sin(presentationTime * .12f) * .35f, Mathf.Lerp(7.5f, 5.2f, intro), Mathf.Lerp(-29f, -20.5f, Mathf.SmoothStep(0, 1, intro)));
            camera.transform.position = position; camera.transform.LookAt(new Vector3(0, 2.6f, 3));
        }
        public void ShowCollection(int index)
        {
            mapMenu=false;
            Clear(); collectionView = true; presentationActors.Clear(); presentationPositions.Clear();
            presentationRoot = new GameObject("VALOMON Creature Collection");
            Platform("Collection stage", presentationRoot.transform, new Vector3(-3.2f, -.25f, 1), new Vector3(9, .5f, 8), new Color(.04f, .09f, .16f));
            PresentationActor(catalog.species[index].model, new Vector3(-3.2f, 0, 1), 4);
            camera.transform.position = new Vector3(0, 3.4f, -11); camera.transform.LookAt(new Vector3(0, 1.6f, 1)); camera.fieldOfView = 48;
        }
        public void ShowCorruptor(int character)
        {
            SpawnWarden(Mathf.Clamp(character,0,3));
        }
    }
}
