using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace Valmon.Campaign
{
    public sealed partial class CampaignWorld
    {
        private bool mapMenu;
        private Vector3 menuCameraBase, menuFocus;
        private GameObject menuActors;
        public void ShowStadium() { Clear(); mapMenu = false; BuildStadium(); }
        public void ShowMapMenu(int arena)
        {
            Clear(); mapMenu = true; collectionView = false; presentationTime = 0;
            presentationActors.Clear(); presentationPositions.Clear();
            presentationRoot = new GameObject("VALMON Map Menu");
            GameObject map = Object.Instantiate(catalog.maps[Mathf.Clamp(arena,0,2)],presentationRoot.transform);
            DisableImportedSceneComponents(map);
            foreach(Collider collider in map.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
            Bounds bounds=BoundsOf(map.GetComponentsInChildren<Renderer>(true));
            map.transform.localScale *= 80f/Mathf.Max(bounds.size.x,bounds.size.z,1);
            bounds=BoundsOf(map.GetComponentsInChildren<Renderer>(true));
            map.transform.position-=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            FixMaterials(map.GetComponentsInChildren<Renderer>(true));
            camera.fieldOfView=52; camera.backgroundColor=new Color(.035f,.08f,.14f);
            menuCameraBase=new Vector3(-24,16,-32); menuFocus=new Vector3(6,3,0);
            camera.transform.position=menuCameraBase; camera.transform.LookAt(menuFocus);
            menuActors=new GameObject("Menu trainer and companion"); menuActors.transform.SetParent(presentationRoot.transform,false);
            Vector3 hero=camera.transform.position+camera.transform.forward*10+camera.transform.right*3.5f-Vector3.up*3.5f;
            GameObject trainer=Model(catalog.characters[1].model,menuActors.transform,hero,4,true);
            Vector3 forward=camera.transform.forward; forward.y=0;
            trainer.transform.rotation=Quaternion.LookRotation(forward)*Quaternion.Euler(0,catalog.characters[1].yawOffset,0);
            Model(catalog.Find("charmander").model,menuActors.transform,hero+camera.transform.right*1.8f,1.8f,true);
        }
        public Texture2D CaptureMapPreview()
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null) return null;
            if(menuActors!=null) menuActors.SetActive(false);
            var target=new RenderTexture(512,256,24); target.Create();
            RenderPipeline.SubmitRenderRequest(camera,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture old=RenderTexture.active; RenderTexture.active=target;
            var image=new Texture2D(512,256,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,512,256),0,0); image.Apply();
            RenderTexture.active=old; target.Release(); Object.Destroy(target);
            if(menuActors!=null) menuActors.SetActive(true);
            return image;
        }
        public Texture2D CapturePortrait(int character)
        {
            if(SystemInfo.graphicsDeviceType==GraphicsDeviceType.Null) return null;
            Vector3 origin=new Vector3(10000,0,0);
            GameObject actor=Model(catalog.characters[character].model,null,origin,1.8f,true);
            actor.transform.rotation=Quaternion.Euler(0,180+catalog.characters[character].yawOffset,0);
            foreach(Transform child in actor.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=30;
            var holder=new GameObject("Temporary portrait camera"); Camera portrait=holder.AddComponent<Camera>(); portrait.enabled=false; portrait.cullingMask=1<<30;
            portrait.backgroundColor=new Color(.025f,.055f,.1f); portrait.clearFlags=CameraClearFlags.SolidColor;
            portrait.fieldOfView=30; portrait.nearClipPlane=.05f; portrait.farClipPlane=15;
            portrait.transform.position=origin+new Vector3(0,1.45f,-2.4f); portrait.transform.LookAt(origin+Vector3.up*1.4f);
            var target=new RenderTexture(256,256,24); target.Create();
            RenderPipeline.SubmitRenderRequest(portrait,new UniversalRenderPipeline.SingleCameraRequest{destination=target});
            RenderTexture old=RenderTexture.active; RenderTexture.active=target;
            var image=new Texture2D(256,256,TextureFormat.RGB24,false); image.ReadPixels(new Rect(0,0,256,256),0,0); image.Apply();
            RenderTexture.active=old; target.Release(); Object.Destroy(target); Object.Destroy(actor); Object.Destroy(holder); return image;
        }
        private void TickMapMenu(float dt)
        {
            presentationTime+=dt;
            camera.transform.position=menuCameraBase+Vector3.right*Mathf.Sin(presentationTime*.12f)*.3f;
            camera.transform.LookAt(menuFocus);
        }
    }
}
