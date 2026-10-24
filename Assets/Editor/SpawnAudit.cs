using System.IO;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Valmon.Campaign;
namespace Valmon.Editor {
public static class SpawnAudit {
 public static void ValidateInterior() {
  var lines=new List<string>();
  var catalog=AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignBuilder.CatalogPath);
  for(int stage=0;stage<3;stage++) for(int seed=0;seed<3;seed++) {
   var camera=new GameObject("Audit camera").AddComponent<Camera>();
   var world=new CampaignWorld(catalog,camera); var run=new CampaignRun(); run.StartStage(stage);
   try {
    world.LoadStage(run,0,new System.Random(seed)); world.ShowCorruptor(1);
    if(!world.InsideSpawnArea(world.spawn)||!world.InsideSpawnArea(world.wardenSpawn)) throw new System.Exception("Player outside arena");
    foreach(var creature in world.wild) if(!world.InsideSpawnArea(creature.root.transform.position)||world.RouteTo(creature).Count==0) throw new System.Exception("Creature outside arena or unreachable");
    lines.Add("PASS map "+(stage+1)+" seed "+seed+": "+world.NavigableNodes+" nodes; "+world.wild.Count+" interior reachable creatures; both players inside");
   } catch(System.Exception e) { lines.Add("FAIL map "+(stage+1)+" seed "+seed+": "+e.Message); }
   var root=GameObject.Find("Playable map "+(stage+1)); if(root!=null) Object.DestroyImmediate(root);
   Object.DestroyImmediate(camera.gameObject);
   Physics.SyncTransforms();
  }
  File.WriteAllLines("Tools/interior-validation.txt",lines);
 }
 public static void Inspect() {
  var lines=new List<string>();
  var catalog=AssetDatabase.LoadAssetAtPath<CampaignCatalog>(CampaignBuilder.CatalogPath);
  for(int stage=0;stage<3;stage++) {
   var map=Object.Instantiate(catalog.maps[stage]); var rs=map.GetComponentsInChildren<Renderer>(); var b=rs[0].bounds; foreach(var r in rs)b.Encapsulate(r.bounds);
   map.transform.localScale*=80/Mathf.Max(b.size.x,b.size.z); b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);map.transform.position-=new Vector3(b.center.x,b.min.y,b.center.z);
   lines.Add("MAP "+stage);
   foreach(var r in rs) lines.Add(r.name+" center="+r.bounds.center.ToString("F2")+" size="+r.bounds.size.ToString("F2"));
   Object.DestroyImmediate(map);
  }
  File.WriteAllLines("Tools/spawn-audit.txt",lines);
 }
}}
