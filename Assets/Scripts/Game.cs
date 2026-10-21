using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace Mist
{
    public sealed class Game:MonoBehaviour
    {
        public static Game Instance;
        public SaveData data;
        public Simulation sim;
        public World world;
        public Sound sound;
        public bool InTitle=true,Paused;
        public Transform canvas,overlay,hud;
        public string Screen="title";
        TextMeshProUGUI healthText,oilText,roomText,hintText,flaskText,weaponText,modeText,toastText;
        Image hpBar,oilBar,skillBar,bossBar;
        TextMeshProUGUI bossText;
        readonly Dictionary<int,RectTransform> bars=new Dictionary<int,RectTransform>();
        Transform enemyHud;
        Phase drawnPhase;
        float toastUntil,saveClock,qaClock;
        int selectedWeapon;
        bool ready;
#if UNITY_EDITOR
        public bool TestDriving;
        public bool SuppressQuitSave;
        public InputFrame TestInput;
#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot(){if(!FindObjectOfType<Game>())new GameObject("Mist Lantern").AddComponent<Game>();}
        void Awake()
        {
            if(Instance&&Instance!=this){Destroy(gameObject);return;}Instance=this;
            Application.targetFrameRate=60;
            UI.Font=Resources.Load<TMP_FontAsset>("Fonts/Mist SDF");
            var c=new GameObject("Interface",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var cv=c.GetComponent<Canvas>();cv.renderMode=RenderMode.ScreenSpaceOverlay;
            var sc=c.GetComponent<CanvasScaler>();sc.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;sc.referenceResolution=new Vector2(1600,900);sc.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var frame=UI.Rect("Design frame",c.transform,0,0,1600,900);frame.anchorMin=frame.anchorMax=frame.pivot=new Vector2(.5f,.5f);frame.anchoredPosition=Vector2.zero;canvas=frame;
            if(!FindObjectOfType<EventSystem>())new GameObject("EventSystem",typeof(EventSystem),typeof(StandaloneInputModule));
            EventSystem.current.sendNavigationEvents=false;
            world=gameObject.AddComponent<World>();world.Setup();sound=gameObject.AddComponent<Sound>();sound.Setup();
            data=Saves.Read();BuildHud();ready=true;Title();
            if(!string.IsNullOrEmpty(Saves.Error))Toast(Saves.Error,10);
        }
        void ClearOverlay(string screen)
        {
            Screen=screen;
            if(overlay){overlay.gameObject.SetActive(false);Destroy(overlay.gameObject);}
            overlay=UI.Rect("Overlay",canvas,0,0,1600,900);
        }
        Transform Sheet(string screen,string eyebrow,string title,string subtitle,float width=1160)
        {
            ClearOverlay(screen);
            UI.Panel(overlay,"Dim",0,0,1600,900,new Color(.025f,.07f,.1f,.85f));
            var p=UI.Panel(overlay,"Sheet",(1600-width)/2,120,width,650,Models.Hex("172E3B")).transform;
            UI.Panel(p,"TopLine",0,0,width,4,UI.Gold);
            UI.Text(p,"Eyebrow",eyebrow,45,32,width-90,35,18,UI.Gold);
            UI.Text(p,"Heading",title,45,78,width-90,65,40,UI.Cream,true);
            UI.Text(p,"Subtitle",subtitle,45,151,width-90,90,21,UI.Muted);
            return p;
        }
        public void Title()
        {
            InTitle=true;Paused=false;sim=null;world.ResetModels();hud.gameObject.SetActive(false);ClearOverlay("title");
            var art=UI.Rect("Title art",overlay,0,0,1600,900).gameObject.AddComponent<RawImage>();
            art.texture=Resources.Load<Texture2D>("Art/Title");art.raycastTarget=false;
            UI.Panel(overlay,"Menu shade",0,0,610,900,new Color(.035f,.075f,.11f,.7f));
            UI.Text(overlay,"Studio","一盏灯，一座城，一次归途",85,112,490,36,22,UI.Gold);
            UI.Text(overlay,"Title","雾灯遗城",78,167,510,116,72,UI.Cream,true);
            UI.Text(overlay,"English title","M I S T   L A N T E R N",88,292,490,40,20,UI.Gold);
            UI.Text(overlay,"Tagline","钟声停了，雾还没有。\n提起灯，去找那最后一声回响。",88,350,440,100,25,UI.Cream);
            bool available=data.run!=null&&data.run.phase!=Phase.Lost&&data.run.phase!=Phase.Won;
            UI.Button(overlay,"Continue","继续冒险",88,485,380,59,Continue,true,available);
            UI.Button(overlay,"New","踏入雾中",88,558,380,59,()=>{if(available)ConfirmNew();else Loadout();});
            UI.Button(overlay,"Workshop","营地工坊  ·  余烬 "+data.profile.embers,88,631,380,59,Workshop);
            UI.Button(overlay,"Guide","操作与玩法",88,704,245,52,()=>Guide(true));
            UI.Button(overlay,"Audio",sound.Muted?"声音：关":"声音：开",345,704,123,52,()=>{sound.Toggle();Title();});
            UI.Text(overlay,"MenuKeys","Enter 开始 · W 工坊 · H 手册",88,775,470,32,17,UI.Gold);
            UI.Text(overlay,"Version","v1.0.0  ·  单机冒险  ·  默认静音",88,819,470,28,16,UI.Muted);
            UI.Text(overlay,"Credits","原创角色与场景 · Unity / C#",1100,849,430,28,16,UI.Muted,false,TextAlignmentOptions.Right);
        }
        void ConfirmNew()
        {
            var p=Sheet("confirm","重新出发","放下这次冒险？","开始新冒险会替换当前途中进度。已经带回营地的余烬与成长会保留。",900);
            UI.Button(p,"Cancel","保留进度",45,420,380,65,Title);
            UI.Button(p,"ConfirmNew","选择武器，重新出发",475,420,380,65,Loadout,true);
        }
        public void Loadout()
        {
            var p=Sheet("loadout","最后的守灯人","选一件趁手的武器","三种武器都能完整通关；在补给点还能更换武器。明灯破雾甲，暗灯回灯油。");
            for(int i=0;i<3;i++)
            {
                int id=i;float x=45+i*360;
                UI.Panel(p,"Weapon card "+i,x,245,345,242,Models.Hex(i==selectedWeapon?"36535B":"203B49"));
                UI.Text(p,"Weapon number "+i,"0"+(i+1),x+22,265,80,45,28,UI.Gold);
                UI.Text(p,"Weapon title "+i,Catalog.Weapons[i],x+22,311,295,44,28,UI.Cream,true);
                UI.Text(p,"Weapon info "+i,Catalog.WeaponInfo[i],x+22,363,295,85,19,UI.Muted);
                UI.Button(p,"Weapon"+i,selectedWeapon==i?"已选择":"选择",x,504,345,50,()=>{selectedWeapon=id;Loadout();},selectedWeapon==i);
            }
            UI.Button(p,"Back","返回",45,577,260,48,Title);
            UI.Button(p,"Begin","提灯出发",755,577,360,48,()=>Begin(UnityEngine.Random.Range(1,int.MaxValue),selectedWeapon),true);
        }
        public void Begin(int seed,int weapon)
        {
            data.run=Simulation.NewRun(seed,weapon,data.profile);sim=new Simulation(data.run,data.profile);sim.Enter();Saves.Write(data);
            InTitle=false;Paused=false;world.ResetModels();hud.gameObject.SetActive(true);drawnPhase=(Phase)(-1);RefreshPhase();
            Toast("WASD 移动 · 按住 J 连击 · K 闪避 · Q 灯态 · R 放灯/收灯 · C 格挡",7);saveClock=0;
            Debug.Log("MIST_RUN_BEGIN seed="+seed+" weapon="+weapon);
        }
        public void Continue()
        {
            data=Saves.Read();if(data.run==null||data.run.phase==Phase.Won||data.run.phase==Phase.Lost){Title();return;}
            sim=new Simulation(data.run,data.profile);InTitle=false;Paused=false;world.ResetModels();hud.gameObject.SetActive(true);drawnPhase=(Phase)(-1);RefreshPhase();Toast("已恢复冒险进度",3);
        }
        public void Workshop()
        {
            var p=Sheet("workshop","营地工坊 · 余烬 "+data.profile.embers,"把余温留给下一次","击败敌人并结束冒险可以带回余烬。升级只影响下一次出发。");
            string[] names={"厚实斗篷","淬火灯刃","备用药袋"};int[] levels={data.profile.vitality,data.profile.power,data.profile.flask};int[] caps={5,5,2};
            string[] details={"每级增加 12 点初始生命","每级增加 8% 基础伤害","每级增加 1 瓶初始药水"};
            for(int i=0;i<3;i++)
            {
                int id=i,price=15+levels[i]*12;float x=45+i*360;
                UI.Panel(p,"Upgrade"+i,x,250,345,230,Models.Hex("203B49"));
                UI.Text(p,"UpgradeName"+i,names[i],x+22,275,300,45,28,UI.Cream,true);
                UI.Text(p,"UpgradeDetail"+i,details[i]+"\n\n等级 "+levels[i]+" / "+caps[i],x+22,336,300,120,21,UI.Muted);
                UI.Button(p,"Buy"+i,levels[i]>=caps[i]?"已升满":price+" 余烬 · 升级",x,500,345,55,()=>{
                    if(data.profile.embers<price||levels[id]>=caps[id])return;
                    data.profile.embers-=price;if(id==0)data.profile.vitality++;else if(id==1)data.profile.power++;else data.profile.flask++;
                    Saves.Write(data);sound.Play("heal");Workshop();
                },true,levels[i]<caps[i]&&data.profile.embers>=price);
            }
            UI.Button(p,"Back","返回扉页",45,583,1070,45,Title);
        }
        public void Guide(bool title)
        {
            if(!title)Paused=true;
            var p=Sheet("guide","守灯人的手册","先学会活下来","红色地面预警是敌人的实际攻击范围。离开它、闪避穿过，或跳过地面攻击。");
            UI.Text(p,"Controls","WASD / 方向键    走位\nJ / 鼠标左键    连击（可按住）\nK / Shift    无敌闪避\nSpace    跳跃\nL / 鼠标右键    武器技",45,252,505,245,23,UI.Cream);
            UI.Text(p,"Mechanics","Q 明暗切换 · 明灯近身破雾甲\nR 放灯 / 收灯 · 铜偶与雾犬会扑灯\nC 按住格挡 · 起手瞬间可精准格挡\nF 喝药 · Esc 暂停 · I 遗物\n重锤按住蓄力，松开或蓄满挥击",610,252,505,245,23,UI.Cream);
            UI.Text(p,"Checkpoint","房间与奖励会自动保存。暂停可返回扉页；失败后，带回余烬再次出发。",45,511,1070,70,20,UI.Muted);
            UI.Button(p,"Back",title?"返回扉页":"返回冒险",45,583,1070,45,()=>{if(title)Title();else Resume();},true);
        }
        public void Pause()
        {
            if(InTitle||sim==null)return;Paused=true;Saves.Write(data);
            var p=Sheet("pause","灯还亮着","歇一会儿","进度已保存。切换窗口时游戏也会暂停。",900);
            UI.Button(p,"Resume","继续冒险",45,260,810,58,Resume,true);
            UI.Button(p,"Guide","查看操作",45,334,395,56,()=>Guide(false));
            UI.Button(p,"Audio",sound.Muted?"声音：关":"声音：开",460,334,395,56,()=>{sound.Toggle();Pause();});
            UI.Button(p,"Inventory","遗物与构筑",45,407,810,56,Inventory);
            UI.Button(p,"SaveTitle","保存并返回扉页",45,481,810,56,()=>{Saves.Write(data);Title();});
        }
        public void Resume(){Paused=false;drawnPhase=(Phase)(-1);RefreshPhase();}
        public void Inventory()
        {
            if(sim==null)return;Paused=true;var s=sim.s;
            var p=Sheet("inventory","当前构筑","随身的微光",Catalog.Weapons[s.weapon]+" · 基础伤害倍率 "+sim.DamageMultiplier.ToString("0.00")+" · 技能耗油 "+sim.SkillCost.ToString("0"));
            var list=Enumerable.Range(0,12).Where(i=>s.relics[i]>0).ToList();
            if(list.Count==0)UI.Text(p,"Empty","尚未获得遗物。清理房间后，从三个随机遗物中选取一个。",45,280,1070,100,25,UI.Cream);
            for(int j=0;j<list.Count;j++)
            {int i=list[j];float x=45+(j%2)*550,y=245+(j/2)*50;UI.Text(p,"Relic"+i,Catalog.RelicNames[i]+" ×"+s.relics[i]+"  "+Catalog.RelicText[i],x,y,520,48,18,UI.Cream);}
            UI.Button(p,"Back","返回冒险",45,583,1070,45,Resume,true);
        }
        void RefreshPhase()
        {
            if(sim==null||Paused||InTitle)return;var s=sim.s;
            if(drawnPhase==s.phase)return;drawnPhase=s.phase;Saves.Write(data);
            Debug.Log("MIST_PHASE room="+s.room+" phase="+s.phase+" hp="+s.hp.ToString("0.0")+" kills="+s.kills);
            if(s.phase==Phase.Playing){ClearOverlay("playing");Toast(Catalog.Areas[s.room/4]+" · "+(s.room%4==3?"首领战":s.elite?"险路":"探索"),3);return;}
            if(s.phase==Phase.Reward)
            {
                var p=Sheet("reward",s.room%4==3?"首领已击败 · 回复生命并补充药水":"雾散了一些","带走哪一道微光？","选择一个遗物。多数遗物可以叠加，具体效果见说明。");
                for(int i=0;i<3;i++)
                {
                    int choice=i,id=s.offers[i];float x=45+360*i;
                    UI.Panel(p,"Relic card "+i,x,250,345,245,Models.Hex("24424E"));
                    UI.RelicIcon(p,id,x+115,245,112);
                    UI.Text(p,"Relic name "+i,Catalog.RelicNames[id],x+22,363,300,45,28,UI.Cream,true);
                    UI.Text(p,"Relic info "+i,Catalog.RelicText[id],x+22,419,300,70,20,UI.Muted);
                    UI.Button(p,"Pick"+i,"收下遗物",x,518,345,63,()=>{if(sim.ChooseRelic(choice)){sound.Play("heal");RefreshPhase();}},true);
                }
            }
            else if(s.phase==Phase.Route)
            {
                string next=s.room%4==2?"前方是区域首领。两条路都将抵达同一座门。":s.room%4==0?"前方有一处补给点，可以疗伤、强化或更换武器。":"险路增加敌人和生命，但清场能带回更多余烬。";
                var p=Sheet("route","地图 · "+(s.room+1)+" / 12","继续深入雾城",next);
                bool choice=s.room%4==1;
                UI.Panel(p,"Route strip",45,244,1070,74,Models.Hex("24424E"));
                UI.Text(p,"Route progress","旧桥  1—4     →     雾庭  5—8     →     钟楼  9—12",65,262,1030,42,26,UI.Gold,false,TextAlignmentOptions.Center);
                UI.Text(p,"Route story","你把灯举高了一点。远处的钟楼，似乎也亮了一下。",45,355,1070,60,25,UI.Cream);
                UI.Button(p,"Safe",choice?"走石阶 · 普通道路":"推开下一扇门",45,474,choice?515:1070,78,()=>Travel(false),true);
                if(choice)UI.Button(p,"Risk","穿浓雾 · 险路 / 额外余烬",600,474,515,78,()=>Travel(true));
                UI.Text(p,"Autosave","进度已保存，可按 Esc 暂停。",45,590,1070,35,19,UI.Muted);
            }
            else if(s.phase==Phase.Rest)
            {
                var p=Sheet("rest","守灯人的补给点","火堆旁，还有一张空椅子","每个补给点可以选择一次。继续前进之前，照顾好自己。");
                string[] names={"添一把柴","磨亮灯刃","试试别的武器"},details={"回复 55 生命，并补充 1 瓶药水。","获得一层锋利灯芯：伤害 +15%。","换为"+Catalog.Weapons[(s.weapon+1)%3]+"，回复 20 生命。"};
                for(int i=0;i<3;i++){int id=i;float x=45+i*360;UI.Text(p,"RestTitle"+i,names[i],x,280,325,50,29,UI.Cream,true);UI.Text(p,"RestDetail"+i,details[i],x,351,325,100,23,UI.Muted);UI.Button(p,"Rest"+i,"就选这个",x,500,345,65,()=>{if(sim.Rest(id)){sound.Play("heal");RefreshPhase();}},true);}
            }
            else
            {
                bool won=s.phase==Phase.Won;
                var p=Sheet(won?"won":"lost",won?"第十三声钟，终于响起":"灯灭了，余温还在",won?"你把黎明带回来了":"下次，再走远一点",won?"雾从石桥上退去。你终于看清，那些追逐你的影子，也曾是这座城的守灯人。":"营地的火还在等你。带回的余烬可以强化下一次冒险。");
                UI.Text(p,"EndStats","抵达房间   "+(s.room+1)+" / 12\n击败敌人   "+s.kills+"\n冒险时间   "+TimeString(s.elapsed)+"\n营地余烬   "+data.profile.embers,45,260,485,250,28,UI.Cream);
                UI.Text(p,"EndBuild","最后的武器\n"+Catalog.Weapons[s.weapon]+"\n\n遗物总数  "+s.relics.Sum()+"\n随机种子  "+s.seed,610,260,505,250,24,UI.Gold);
                UI.Button(p,"Again","再次出发",45,552,515,63,()=>{InTitle=true;hud.gameObject.SetActive(false);Loadout();},true);
                UI.Button(p,"ReturnTitle","返回营地",600,552,515,63,Title);
            }
        }
        public void Travel(bool elite){if(sim!=null&&sim.Travel(elite)){world.ResetModels();RefreshPhase();}}
        void BuildHud()
        {
            hud=UI.Rect("HUD",canvas,0,0,1600,900);
            UI.Panel(hud,"Status background",24,22,460,102,new Color(.04f,.09f,.13f,.8f));
            UI.Text(hud,"HunterLabel","守灯人",44,34,170,34,22,UI.Cream,true);
            healthText=UI.Text(hud,"HP","",250,35,210,32,21,UI.Cream,false,TextAlignmentOptions.Right);
            UI.Panel(hud,"HP track",44,76,414,15,Models.Hex("334650"));
            hpBar=UI.Panel(hud,"HP fill",44,76,414,15,Models.Hex("D17866"));
            UI.Panel(hud,"Oil track",44,100,414,8,Models.Hex("334650"));
            oilBar=UI.Panel(hud,"Oil fill",44,100,414,8,UI.Gold);
            roomText=UI.Text(hud,"Room","",560,27,480,42,25,UI.Cream,true,TextAlignmentOptions.Center);
            modeText=UI.Text(hud,"Mode","",530,69,540,31,18,UI.Gold,false,TextAlignmentOptions.Center);
            UI.Button(hud,"Pause","暂停  Esc",1390,26,180,52,Pause);
            UI.Button(hud,"Bag","遗物  I",1192,26,180,52,Inventory);
            bossBar=UI.Panel(hud,"BossHP",450,155,700,12,Models.Hex("C77568"));
            bossText=UI.Text(hud,"BossName","",450,122,700,33,22,UI.Cream,true,TextAlignmentOptions.Center);
            UI.Panel(hud,"Weapon panel",24,802,300,76,new Color(.04f,.09f,.13f,.8f));
            UI.Panel(hud,"Controls shade",324,812,1252,66,new Color(.04f,.09f,.13f,.62f));
            weaponText=UI.Text(hud,"WeaponHUD","",45,809,260,35,22,UI.Gold,true);
            flaskText=UI.Text(hud,"PotionHUD","",45,846,260,28,18,UI.Cream);
            hintText=UI.Text(hud,"Keys","WASD 移动   J 攻击   K 闪避   Space 跳跃   L 技能   C 格挡",337,820,895,28,20,UI.Cream);
            oilText=UI.Text(hud,"OilHUD","",337,850,895,24,17,UI.Muted);
            skillBar=UI.Panel(hud,"Skill ready",1270,855,280,5,UI.Gold);
            UI.Text(hud,"SkillKey","Q 灯态 · R 放灯",1270,818,280,30,22,UI.Cream);
            enemyHud=UI.Rect("Enemy health",hud,0,0,1600,900);
            toastText=UI.Text(canvas,"Toast","",280,730,1040,58,24,UI.Cream,true,TextAlignmentOptions.Center);
        }
        public void Toast(string text,float seconds=3){toastText.text=text;toastUntil=Time.unscaledTime+seconds;toastText.transform.SetAsLastSibling();}
        void UpdateHud()
        {
            var s=sim.s;healthText.text=Mathf.CeilToInt(s.hp)+" / "+s.maxHp;
            hpBar.rectTransform.sizeDelta=new Vector2(414*s.hp/s.maxHp,15);oilBar.rectTransform.sizeDelta=new Vector2(414*s.oil/100,8);
            roomText.text=Catalog.Areas[s.room/4]+"   "+(s.room+1)+" / 12";
            modeText.text=(s.bright?(s.lampPlaced?"灯已放置 · 守住光区":"明灯 · 近身破雾甲"):"暗灯 · 正在回油")+"   "+s.wave+"/"+s.waves+" 波";
            oilText.text="灯油 "+Mathf.FloorToInt(s.oil)+" / 100    技能消耗 "+sim.SkillCost.ToString("0")+"    "+(s.skillCd>0?"冷却 "+s.skillCd.ToString("0.0")+" 秒":"武器技已就绪")+"    "+(s.weapon==0?"锋意":s.weapon==1?"蓄力":"星能")+" "+(s.weapon==1?Mathf.RoundToInt(s.charge/.7f*100):Mathf.RoundToInt(s.resource))+"%";
            flaskText.text="F 药水 ×"+s.potions+"     "+TimeString(s.elapsed);weaponText.text=Catalog.Weapons[s.weapon];
            skillBar.rectTransform.sizeDelta=new Vector2(280*(1-s.skillCd/4.8f),5);
            Enemy boss=s.enemies.Find(e=>e.kind>=4&&e.hp>0);bossBar.gameObject.SetActive(boss!=null);bossText.gameObject.SetActive(boss!=null);
            if(boss!=null){bossBar.rectTransform.sizeDelta=new Vector2(700*boss.hp/boss.maxHp,12);bossText.text=Catalog.Bosses[s.room/4];}
            foreach(var e in s.enemies)
            {
                RectTransform bar;
                if(!bars.TryGetValue(e.id,out bar)){bar=UI.Panel(enemyHud,"EnemyHP"+e.id,0,0,62,5,Models.Hex("D89279")).rectTransform;bars[e.id]=bar;}
                bar.gameObject.SetActive(e.hp>0&&e.kind<4&&s.phase==Phase.Playing);
                Vector2 local;RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas,world.cam.WorldToScreenPoint(World.P(e.pos,e.kind==2?2.55f:2.2f)),null,out local);
                bar.anchoredPosition=local+new Vector2(800-31,-450);bar.sizeDelta=new Vector2(62*e.hp/e.maxHp,5);
            }
            var active=new HashSet<int>(s.enemies.Select(e=>e.id));foreach(var id in bars.Keys.Where(id=>!active.Contains(id)).ToList()){Destroy(bars[id].gameObject);bars.Remove(id);}
        }
        public static string TimeString(float time){int t=(int)time;return (t/60).ToString("00")+":"+(t%60).ToString("00");}
        void InvokeMenu(string name)
        {
            if(!overlay)return;var button=overlay.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name==name);
            if(button&&button.interactable)button.onClick.Invoke();
        }
        void MenuKeys()
        {
            int choice=Input.GetKeyDown(KeyCode.Alpha1)?0:Input.GetKeyDown(KeyCode.Alpha2)?1:Input.GetKeyDown(KeyCode.Alpha3)?2:-1;
            bool confirm=Input.GetKeyDown(KeyCode.Return)||Input.GetKeyDown(KeyCode.KeypadEnter);
            if(InTitle&&Input.GetKeyDown(KeyCode.Escape)&&Screen!="title"){Title();return;}
            if(Screen=="title"){
                if(Input.GetKeyDown(KeyCode.W)){InvokeMenu("Workshop");return;}
                if(Input.GetKeyDown(KeyCode.H)){InvokeMenu("Guide");return;}
                if(Input.GetKeyDown(KeyCode.N)){InvokeMenu("New");return;}}
            if(Screen=="workshop"){if(choice>=0)InvokeMenu("Buy"+choice);else if(confirm)InvokeMenu("Back");return;}
            if(Screen=="guide"&&confirm){InvokeMenu("Back");return;}
            if(Screen=="confirm"){if(choice==1)InvokeMenu("ConfirmNew");else if(choice==0||confirm)InvokeMenu("Cancel");return;}
            if(Screen=="loadout"){if(choice>=0){selectedWeapon=choice;Loadout();}if(confirm)Begin(UnityEngine.Random.Range(1,int.MaxValue),selectedWeapon);}
            else if(Screen=="title"&&confirm){if(data.run!=null&&data.run.phase!=Phase.Won&&data.run.phase!=Phase.Lost)Continue();else Loadout();}
            else if(Screen=="reward"&&(choice>=0||confirm)){sim.ChooseRelic(choice>=0?choice:0);RefreshPhase();}
            else if(Screen=="rest"&&(choice>=0||confirm)){sim.Rest(choice>=0?choice:0);RefreshPhase();}
            else if(Screen=="route"&&confirm)Travel(false);
            else if(Screen=="lost"&&confirm){InTitle=true;hud.gameObject.SetActive(false);Loadout();}
            else if(Screen=="won"&&confirm)Title();
        }
        void Update()
        {
            if(!ready)return;
            MenuKeys();
            float dt=Time.unscaledDeltaTime;
            if(Time.unscaledTime>toastUntil)toastText.text="";
            if(!InTitle&&sim!=null)
            {
                if(Input.GetKeyDown(KeyCode.Escape)){if(Paused)Resume();else Pause();}
                if(Input.GetKeyDown(KeyCode.I)){if(Paused)Resume();else Inventory();}
                if(!Paused)
                {
                    bool pointerUI=EventSystem.current&&EventSystem.current.IsPointerOverGameObject();
                    var input=new InputFrame{
                        move=new Vector2((Input.GetKey(KeyCode.D)||Input.GetKey(KeyCode.RightArrow)?1:0)-(Input.GetKey(KeyCode.A)||Input.GetKey(KeyCode.LeftArrow)?1:0),(Input.GetKey(KeyCode.W)||Input.GetKey(KeyCode.UpArrow)?1:0)-(Input.GetKey(KeyCode.S)||Input.GetKey(KeyCode.DownArrow)?1:0)),
                        attack=Input.GetKey(KeyCode.J)||(!pointerUI&&Input.GetMouseButton(0)),
                        dodge=Input.GetKeyDown(KeyCode.K)||Input.GetKeyDown(KeyCode.LeftShift)||Input.GetKeyDown(KeyCode.RightShift),
                        skill=Input.GetKeyDown(KeyCode.L)||(!pointerUI&&Input.GetMouseButtonDown(1)),
                        jump=Input.GetKeyDown(KeyCode.Space),toggle=Input.GetKeyDown(KeyCode.Q),potion=Input.GetKeyDown(KeyCode.F),guard=Input.GetKey(KeyCode.C),place=Input.GetKeyDown(KeyCode.R)};
#if UNITY_EDITOR
                    if(TestDriving){input=TestInput;dt=.05f;}
#endif
                    sim.Tick(dt,input);
                    foreach(var e in sim.signals){world.Signal(e);sound.Play(e.kind);if(e.kind=="lowoil")Toast("灯油不足 · 按 Q 切至暗灯，稍候回油");if(e.kind=="empty")Toast("灯油耗尽，已切至暗灯");if(e.kind=="wave")Toast("雾中又有脚步声 · 第 "+sim.s.wave+" 波",2);}
                    if(Input.GetKeyDown(KeyCode.E)&&sim.s.phase==Phase.Route)Travel(false);
                    RefreshPhase();saveClock+=dt;
                    if(saveClock>8){saveClock=0;Saves.Write(data);}
                }
                world.Render(sim.s,dt,!Paused&&sim.s.phase==Phase.Playing);UpdateHud();
            }
            else world.Render(null,dt,false);
            sound.Apply();
            if(Saves.IsTest)
            {
                qaClock+=Time.unscaledDeltaTime;
                if(qaClock>1)
                {
                    qaClock=0;
                    File.WriteAllText(Path.Combine(Application.persistentDataPath,"qa-state.json"),JsonUtility.ToJson(new QaState{screen=Screen,paused=Paused,muted=AudioListener.volume==0,run=sim==null?null:sim.s}));
                }
            }
        }
        [Serializable] public class QaState {public string screen;public bool paused,muted;public Run run;}
        void OnApplicationFocus(bool focus)
        {if(ready&&!focus&&!InTitle&&!Paused&&sim!=null&&sim.s.phase==Phase.Playing)Pause();}
        void OnApplicationPause(bool pause){if(ready&&pause&&!InTitle)Pause();}
        void OnApplicationQuit(){
#if UNITY_EDITOR
            if(SuppressQuitSave)return;
#endif
            if(data!=null)Saves.Write(data);
        }
    }
}
