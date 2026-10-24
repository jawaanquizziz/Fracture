using System.Collections;
using System.Linq;
using UnityEngine;

namespace Valmon.Campaign
{
    public partial class CampaignController
    {
        private string helpPage="";
        private readonly Texture2D[] arenaPreviews=new Texture2D[3], portraits=new Texture2D[4];
        private bool capturingArenaPreviews;
        private static string CleanPlayerName(string value)
        {
            string clean=new string((value??"").Where(c=>!char.IsControl(c)&&c!='<'&&c!='>').Take(24).ToArray()).Trim();
            return clean.Length==0?"Player":clean;
        }
        private void EnterLobby()
        {
            playerName=CleanPlayerName(playerName); PlayerPrefs.SetString("VALMON.PlayerName",playerName); PlayerPrefs.Save();
            ScreenState=CampaignScreen.Lobby; World.ShowMapMenu(selectedLevel); StartCoroutine(PrepareArenaPreviews());
        }
        private IEnumerator PrepareArenaPreviews()
        {
            if(capturingArenaPreviews) yield break;
            capturingArenaPreviews=true;
            for(int i=0;i<3;i++) if(arenaPreviews[i]==null)
            {
                World.ShowMapMenu(i); yield return null;
                arenaPreviews[i]=World.CaptureMapPreview();
            }
            for(int i=0;i<4;i++) if(portraits[i]==null) { portraits[i]=World.CapturePortrait(i); yield return null; }
            World.ShowMapMenu(selectedLevel); capturingArenaPreviews=false;
        }
        private void DrawHomeMenu()
        {
            Panel(new Rect(0,55,565,665),new Color(.015f,.035f,.065f,.78f));
            var brand=new GUIStyle(titleStyle){fontSize=82};
            Text(43,86,520,108,"VALMON",brand);
            Text(48,183,510,32,"T A M E   /   B A T T L E   /   P R O T E C T",smallStyle);
            if(Button(new Rect(45,239,350,49),"PLAY ONLINE",true,true)) EnterLobby();
            if(Button(new Rect(45,297,350,49),"SETTINGS")) settings=true;
            if(Button(new Rect(45,355,350,49),"HOW TO PLAY")) helpPage="HOW TO PLAY";
            if(Button(new Rect(45,413,350,49),"CREDITS")) helpPage="CREDITS";
            if(Button(new Rect(45,471,350,49),"CREATURE ROSTER")) { collection=true; World.ShowCollection(collectionIndex); }
            if(Button(new Rect(45,529,350,49),"EXIT")) Quit();
            Text(46,615,310,27,"YOUR PLAYER NAME",smallStyle);
            playerName=GUI.TextField(new Rect(45,645,300,40),playerName,24,fieldStyle);
            if(Button(new Rect(365,645,165,40),"SOLO CAMPAIGN",true,false,true)) menuStep=1;
            Text(1060,685,200,25,"LAN UDP / UNITY 6",smallStyle);
        }
        private void DrawInfoPage()
        {
            Panel(new Rect(255,155,770,510),ink); Text(290,185,700,65,helpPage,titleStyle);
            string content=helpPage=="CREDITS"?
                "VALMON / The Rift Championship\n\nCharacters, creature models and Blender arenas are supplied by this project's asset collection. Original source archives remain in Assets.\n\nThe championship interface, local UDP match flow and original opening score are included with this project.\n\nSee the original asset archives for their creator and license information.":
                "TWO PLAYERS / THREE ROUNDS\nHost or join on the same Wi-Fi or hotspot. Roles are assigned by the host and swap every round.\n\nTAMER: WASD move, Shift sprint, right mouse orbit, Z catch, Q dash, R respawn. Catch at least five with ten balls in eight minutes. Finish at five or continue to six for +1 bonus point.\n\nWARDEN: Corrupt six distinct creatures for +1 bonus point; this does not end the round. WASD move, right mouse orbit, Q corrupt nearby creature, E place trap, R lock a nearby gate, F scan. Abilities cost energy and have cooldowns.\n\nCorruption turns captures into battles. Choose your own side's moves when it is your turn. Esc pauses both players. Win two of three rounds to win the match.";
            Text(292,266,690,293,content,new GUIStyle(textStyle){fontSize=16});
            if(Button(new Rect(292,585,690,52),"BACK",true,true)) helpPage="";
        }
        private void ArenaCard(Rect r,int index,bool canSelect)
        {
            Panel(r,ink);
            if(arenaPreviews[index]!=null) GUI.DrawTexture(r,arenaPreviews[index],ScaleMode.ScaleAndCrop);
            Panel(r,new Color(.01f,.025f,.045f,.58f));
            Panel(new Rect(r.x,r.y,r.width,3),selectedLevel==index?accent:World.Theme(index));
            Text(r.x+14,r.y+17,r.width-25,31,mapNames[index],new GUIStyle(headingStyle){fontSize=21});
            Text(r.x+14,r.y+55,r.width-25,24,index==0?"GRASS / LEVELS 1–15":index==1?"FIRE / LEVELS 16–30":"WATER / LEVELS 31–50",smallStyle);
            if(canSelect&&GUI.Button(r,"",GUIStyle.none)) { selectedLevel=index; World.ShowMapMenu(index); }
        }
        private void PlayerCard(Rect r,string name,int character,string role,bool ready,bool local)
        {
            Color color=role=="WARDEN"?new Color(1,.22f,.27f):new Color(.14f,.65f,1);
            Panel(r,ink); Panel(new Rect(r.x,r.y,3,r.height),ready?color:new Color(.3f,.36f,.42f));
            if(character>=0&&character<portraits.Length&&portraits[character]!=null) GUI.DrawTexture(new Rect(r.x+20,r.y+20,100,112),portraits[character],ScaleMode.ScaleToFit);
            else Text(r.x+52,r.y+42,65,65,ready?"P":"?",titleStyle);
            Text(r.x+138,r.y+23,r.width-150,32,name,new GUIStyle(headingStyle){fontSize=22});
            string waiting = "Waiting for player 2" + LoadingDots();
            Text(r.x+138,r.y+65,r.width-150,32,role==""?(ready?"CONNECTED":local?"YOUR CHARACTER":waiting):role,new GUIStyle(headingStyle){fontSize=22});
            Text(r.x+138,r.y+112,r.width-150,28,ready?"READY / "+(local?"YOU":"PEER"):local?"Host or join a room below":"No second player connected",smallStyle);
        }
        private void DrawMatchLobby()
        {
            MenuChrome();
            Text(335,82,850,65,"MULTIPLAYER LOBBY",titleStyle);
            string statusText = capturingArenaPreviews ? ("Preparing arena and character previews" + LoadingDots()) : (netMode != NetMode.None && !netConnected ? (netStatus + LoadingDots()) : netStatus);
            Text(335,148,870,38, statusText, smallStyle);
            PlayerCard(new Rect(175,197,440,175),CleanPlayerName(playerName),selectedCharacter,"",netMode==NetMode.Host||netConnected,true);
            PlayerCard(new Rect(665,197,440,175),netConnected?remotePlayerName:"WAITING FOR PLAYER 2",netConnected?netRemoteCharacter:-1,"",netConnected,false);
            bool editable=netMode==NetMode.None&&!capturingArenaPreviews;
            for(int i=0;i<4;i++) if(Button(new Rect(188+i*105,340,98,28),catalog.characters[i].name,editable,selectedCharacter==i,true)) selectedCharacter=i;
            for(int i=0;i<3;i++) ArenaCard(new Rect(175+i*317,388,302,93),i,(netMode==NetMode.None||netMode==NetMode.Host)&&!capturingArenaPreviews);
            Panel(new Rect(175,496,930,179),ink);
            Text(194,509,390,26,"HOST IPv4",smallStyle); Text(625,509,120,26,"UDP PORT",smallStyle); Text(776,509,160,26,"ROOM CODE",smallStyle);
            GUI.enabled=editable;
            lobbyJoinIp=GUI.TextField(new Rect(194,538,411,40),lobbyJoinIp,fieldStyle);
            lobbyPort=GUI.TextField(new Rect(625,538,127,40),lobbyPort,5,fieldStyle);
            if(netMode!=NetMode.Host) netRoomCode=GUI.TextField(new Rect(776,538,154,40),netRoomCode,6,fieldStyle);
            else Text(776,538,154,40,netRoomCode,headingStyle);
            GUI.enabled=true;
            if(Button(new Rect(194,595,295,49),netMode==NetMode.Host?"WAITING FOR PLAYER":"HOST ROOM",editable,true)) { if(ReadPort()) NetHost(); }
            if(Button(new Rect(505,595,295,49),netMode==NetMode.Client?"CONNECTING / READY":"JOIN ROOM",editable)) { if(ReadPort()&&netRoomCode.Trim().Length==6) NetJoin(lobbyJoinIp); else netStatus="Enter the host IPv4 and six-digit room code."; }
            if(Button(new Rect(816,595,270,49),"LEAVE LOBBY")) { NetDisconnect(); netRoomCode=""; OpenMenu(); }
            if(netMode==NetMode.Host) Text(190,681,900,35,"YOUR IPv4: "+netLocalIp,smallStyle);
            if(netMode==NetMode.Host&&!autoStartArmed&&Button(new Rect(925,83,280,55),"START MATCH",netRemoteReady,true)) HostStartMatch();
            if(netMode==NetMode.None) Text(194,650,880,22,"Same Wi-Fi / Ethernet / hotspot. Host chooses the first arena. Roles are assigned automatically.",smallStyle);
        }
        private void DrawCountdown()
        {
            Panel(new Rect(0,0,1280,720),new Color(.01f,.02f,.04f,.86f));
            Text(360,87,720,70,waitingForArena?"PLAYERS READY":"MATCH READY",titleStyle);
            Text(420,160,680,40,"Authoritative roles assigned / Round "+(roundIndex+1)+" of 3");
            PlayerRole hostRole=netMode==NetMode.Host?Role:RemoteRole;
            int hostCharacter=netMode==NetMode.Host?netLocalCharacter:netRemoteCharacter;
            int clientCharacter=netMode==NetMode.Client?netLocalCharacter:netRemoteCharacter;
            PlayerCard(new Rect(175,230,440,180),hostPlayerName,hostCharacter,RoleName(hostRole),true,netMode==NetMode.Host);
            PlayerCard(new Rect(665,230,440,180),clientPlayerName,clientCharacter,RoleName(hostRole==PlayerRole.Tamer?PlayerRole.Corruptor:PlayerRole.Tamer),true,netMode==NetMode.Client);
            ArenaCard(new Rect(320,445,640,95),Run.stage,false);
            Text(490,575,420,55,waitingForArena?"Loading both laptops...":"MATCH STARTS IN",headingStyle);
            if(!waitingForArena) Text(607,622,160,75,Mathf.Max(1,Mathf.CeilToInt(countdownLeft)).ToString(),titleStyle);
        }
        private void DrawRoundResults()
        {
            Panel(new Rect(0,0,1280,720),new Color(.01f,.02f,.04f,.92f));
            bool final=ScreenState==CampaignScreen.MatchResult;
            Text(285,75,950,75,final?"FINAL MATCH RESULTS":"ROUND "+(roundIndex+1)+" COMPLETE",titleStyle);
            Text(330,165,880,42,final?"WINNER: "+(hostMatchWinner?hostPlayerName:clientPlayerName):(outcomeTamer?"TAMER WINS THE ROUND":"WARDEN WINS THE ROUND"),headingStyle);
            Panel(new Rect(165,241,450,220),ink); Panel(new Rect(665,241,450,220),ink);
            Text(190,260,410,45,"TAMER",headingStyle); Text(690,260,410,45,"WARDEN",headingStyle);
            Text(190,315,405,115,"Creatures captured: "+Run.catches+"\nTime remaining: "+Mathf.CeilToInt(Run.seconds)+" seconds\nRound score: "+roundTamerScore);
            Text(690,315,405,115,"Captures stopped: "+roundStopped+"\nSabotage actions: "+roundSabotages+"\nBonus point: +"+WardenBonus+"\nRound score: "+roundWardenScore);
            Text(165,490,450,50,hostPlayerName+"\n"+hostWins+" rounds / "+hostScore+" points",new GUIStyle(headingStyle){fontSize=19});
            Text(665,490,450,50,clientPlayerName+"\n"+clientWins+" rounds / "+clientScore+" points",new GUIStyle(headingStyle){fontSize=19});
            Text(165,548,980,48,final?"Three rounds complete. Match winner is decided by rounds won.":"Next round: roles swap automatically and both players load the next arena.",smallStyle);
            if(Button(new Rect(165,610,450,60),final?"PLAY AGAIN":"NEXT ROUND",true,true)) { if(final) PlayAgain(); else NextRound(); }
            if(Button(new Rect(665,610,450,60),"RETURN TO LOBBY")) ReturnToLobby();
        }
    }
}
