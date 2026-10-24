using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Valmon.Campaign
{
    public sealed partial class CampaignWorld
    {
        public GameObject warden;
        public CharacterController wardenController;
        public Vector3 wardenSpawn;
        public readonly List<GameObject> gates = new List<GameObject>();
        private readonly Dictionary<int, GameObject> trapVisuals = new Dictionary<int, GameObject>();
        public void SpawnWarden(int character)
        {
            if(terrainRoot==null) return;
            if(warden!=null) { warden.SetActive(false); Object.Destroy(warden); }
            int node=reachable.Where(n=>InsideSpawnArea(nodes[n]) && SpawnClear(nodes[n])).OrderBy(n=>Mathf.Abs(Vector3.Distance(nodes[n],spawn)-12f)).First();
            wardenSpawn=nodes[node]+Vector3.up*.12f;
            warden=new GameObject("Warden Player"); warden.transform.SetParent(terrainRoot.transform,false); warden.transform.position=wardenSpawn;
            Model(catalog.characters[character].model,warden.transform,Vector3.zero,PlayerHeight,true).transform.localRotation=Quaternion.Euler(0,catalog.characters[character].yawOffset,0);
            wardenController=warden.AddComponent<CharacterController>(); warden.AddComponent<CharacterLocomotion>().Initialize(warden.transform.GetChild(0));
            wardenController.height=PlayerHeight; wardenController.radius=.22f; wardenController.center=Vector3.up*(PlayerHeight*.5f); wardenController.stepOffset=.35f;
            gates.Clear(); trapVisuals.Clear();
            for(int i=0;i<3;i++)
            {
                Vector3 desired=Vector3.Lerp(spawn,wardenSpawn,(i+1)/4f)+Quaternion.Euler(0,i*100,0)*Vector3.forward*4;
                Vector3 ground=nodes[reachable.OrderBy(n=>(nodes[n]-desired).sqrMagnitude).First()];
                GameObject root=new GameObject("Interactive rift gate "+i); root.transform.SetParent(terrainRoot.transform,false); root.transform.position=ground;
                Platform("Gate left post",root.transform,new Vector3(-1.8f,1.6f,0),new Vector3(.24f,3.2f,.35f),new Color(.05f,.3f,.42f)).GetComponent<Collider>().enabled=false;
                Platform("Gate right post",root.transform,new Vector3(1.8f,1.6f,0),new Vector3(.24f,3.2f,.35f),new Color(.05f,.3f,.42f)).GetComponent<Collider>().enabled=false;
                GameObject barrier=Platform("Lockable barrier",root.transform,new Vector3(0,1.5f,0),new Vector3(3.5f,3,.25f),new Color(.55f,.025f,.06f));
                barrier.name="Barrier"; barrier.SetActive(false); gates.Add(root);
            }
            Physics.SyncTransforms();
        }
        public void GateState(int index,bool locked)
        {
            if(index>=0&&index<gates.Count&&gates[index]!=null) gates[index].transform.Find("Barrier").gameObject.SetActive(locked);
        }
        public void TrapState(int id,Vector3 position,bool active)
        {
            if(!active)
            {
                if(trapVisuals.TryGetValue(id,out GameObject old)&&old!=null) { old.SetActive(false); Object.Destroy(old); }
                trapVisuals.Remove(id); return;
            }
            if(trapVisuals.ContainsKey(id)) return;
            GameObject root=new GameObject("Warden trap "+id); root.transform.SetParent(terrainRoot.transform,false); root.transform.position=position;
            Ring(root.transform,new Color(1,.08f,.12f));
            Platform("Trap core",root.transform,new Vector3(0,.12f,0),new Vector3(.5f,.15f,.5f),new Color(.7f,.02f,.06f)); trapVisuals[id]=root;
        }
        public void ResetWarden()
        {
            if(wardenController==null) return;
            wardenController.enabled=false; warden.transform.position=wardenSpawn; wardenController.enabled=true;
        }
    }
}
