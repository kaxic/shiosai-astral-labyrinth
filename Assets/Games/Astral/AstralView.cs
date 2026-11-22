using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static AstralMesh;

/// <summary>Renders the survey from the current turn. Uncharted tiles are clipped by a point-filtered mask.</summary>
public sealed class AstralView : MonoBehaviour
{
    const int ChunkSize=5;
    sealed class Piece
    {
        public Transform root,form,warning,ice,bar,life;
        public Vector2Int target;
        public bool visible;
        public int model;
        public float walk;
    }
    readonly Dictionary<string,Mesh> stock=new Dictionary<string,Mesh>();
    readonly Dictionary<int,Piece> enemies=new Dictionary<int,Piece>();
    readonly List<int> removed=new List<int>();
    Transform world,setting,heroRing,heading,actionRing;
    Piece player;
    Material art,wash;
    Mesh settingMesh;
    MeshFilter[] chunks;
    bool[] dirty;
    AstralTile[] tiles;
    Color32[] pixels;
    Texture2D mask;
    Camera cameraView;
    int cols,rows,chunkCols,seed,level=-1,version=int.MinValue,step;
    float actionAge=3;
    string action="";
    Vector3 cameraFocus;
    bool closeCamera;

    public void Render(AstralGame g)
    {
        if(!world)Initialize();
        bool changedFloor=level!=g.Floor||seed!=g.Seed||cols!=g.Cols||rows!=g.Rows;
        if(changedFloor)BuildBoard(g);
        if(g.MapVersion!=version||changedFloor) UpdateMap(g,changedFloor);
        Frame(g,changedFloor);
        if(player==null||player.model!=g.Job)
        {
            if(player!=null)Release(player.root.gameObject);
            player=NewPiece(g.Job,g.Hero,true);
        }
        if(g.StepSerial!=step)
        {
            actionAge=0;action=g.LastAction;step=g.StepSerial;
        }
        else actionAge+=Mathf.Min(Time.unscaledDeltaTime,.06f);
        float burst=Mathf.Max(0,1-actionAge*3.55f);
        Pose(player,g.Hero,g.Facing,changedFloor,burst,g.LastAction=="hit"||g.LastAction=="skill");
        heroRing.position=player.root.position+Vector3.up*.060f;
        var target=g.Hero+g.Facing;
        bool show=Inside(target.x,target.y)&&g.Visible(target.x,target.y)&&g.Facing.sqrMagnitude>0;
        heading.gameObject.SetActive(show);
        if(show)
        {
            heading.SetPositionAndRotation(At(g.Hero.x+g.Facing.x*.44f,.089f,g.Hero.y+g.Facing.y*.44f),
                Quaternion.Euler(0,Mathf.Atan2(g.Facing.x,g.Facing.y)*Mathf.Rad2Deg,0));
        }
        bool ring=burst>0&&(action=="skill"||action=="light"||action=="heal"||action=="hit");
        actionRing.gameObject.SetActive(ring);
        if(ring)
        {
            actionRing.position=heroRing.position+Vector3.up*.016f;
            actionRing.localScale=Vector3.one*((action=="hit"?.65f:1.3f)*(1.14f-burst*.73f));
        }
        foreach(var piece in enemies.Values)piece.visible=false;
        foreach(var monster in g.Enemies)
        {
            if(!Inside(monster.pos.x,monster.pos.y)||!g.Visible(monster.pos.x,monster.pos.y))continue;
            if(!enemies.TryGetValue(monster.id,out var piece))
            {
                piece=NewPiece(monster.kind+3,monster.pos,false);enemies.Add(monster.id,piece);
            }
            piece.visible=true;
            Vector2Int face=monster.intent>0?monster.aim-monster.pos:monster.pos-piece.target;
            Pose(piece,monster.pos,face,changedFloor,0,false);
            piece.warning.gameObject.SetActive(monster.intent>0&&monster.frozen<=0);
            piece.ice.gameObject.SetActive(monster.frozen>0);
            if(piece.warning.gameObject.activeSelf)piece.warning.Rotate(0,Time.unscaledDeltaTime*43,0);
            piece.bar.gameObject.SetActive(monster.hp<monster.maxHp||monster.kind==4);
            piece.bar.rotation=cameraView.transform.rotation;
            piece.life.localScale=At(Mathf.Clamp01(monster.maxHp>0?(float)monster.hp/monster.maxHp:0),1,1);
        }
        removed.Clear();foreach(var p in enemies)if(!p.Value.visible)removed.Add(p.Key);
        foreach(int id in removed){enemies[id].root.gameObject.SetActive(false);Release(enemies[id].root.gameObject);enemies.Remove(id);}
    }
    static void Release(UnityEngine.Object o)
    {if(!o)return;if(Application.isPlaying)Destroy(o);else DestroyImmediate(o);}
    Transform Group(string name,Transform parent)
    {var o=new GameObject(name);o.transform.SetParent(parent,false);return o.transform;}
    Mesh Remember(string name,Action<AstralMesh> draw)
    {
        if(stock.TryGetValue(name,out var mesh))return mesh;
        var shape=new AstralMesh();draw(shape);mesh=shape.Build(name);stock[name]=mesh;return mesh;
    }
    MeshFilter Draw(string name,Transform parent,Mesh mesh,bool translucent=false)
    {
        var obj=Group(name,parent).gameObject;
        var filter=obj.AddComponent<MeshFilter>();filter.sharedMesh=mesh;
        var surface=obj.AddComponent<MeshRenderer>();surface.sharedMaterial=translucent?wash:art;
        surface.shadowCastingMode=ShadowCastingMode.Off;surface.lightProbeUsage=LightProbeUsage.Off;
        surface.reflectionProbeUsage=ReflectionProbeUsage.Off;surface.receiveShadows=false;
        return filter;
    }
    void Initialize()
    {
        world=Group("The sidereal atlas",transform);
        art=new Material(Resources.Load<Shader>("Astral/Celestial")){name="Celestial stone and enamel"};
        wash=new Material(Resources.Load<Shader>("Astral/StarWash")){name="Observatory contact wash"};
        string[] names={"MoonSlate","OrbitalBronze","Amethyst","StarEnamel"};
        string[] slots={"_Slate","_Bronze","_Quartz","_Enamel"};
        art.SetTexture("_Starleaf",Resources.Load<Texture2D>("Astral/Starleaf"));
        for(int i=0;i<names.Length;i++)art.SetTexture(slots[i],Resources.Load<Texture2D>("Astral/"+names[i]));
        cameraView=Camera.main;
        if(!cameraView){var lens=Group("Atlas camera",world).gameObject;lens.tag="MainCamera";cameraView=lens.AddComponent<Camera>();}
        if(!FindAnyObjectByType<AudioListener>())cameraView.gameObject.AddComponent<AudioListener>();
        cameraView.orthographic=true;cameraView.clearFlags=CameraClearFlags.SolidColor;
        cameraView.farClipPlane=145;cameraView.nearClipPlane=.15f;cameraView.allowHDR=false;
        cameraView.backgroundColor=new Color32(19,29,46,255);
        heroRing=Draw("Keeper's compass",world,Remember("Keeper's compass",m=>{
            m.Ink("A1E5DB",Finish.Luminous,.93f);for(int j=0;j<4;j++)m.Ring(Vector3.zero,.345f,.377f,7,j*90+8,j*90+79);
        })).transform;
        heading=Draw("Facing",world,Remember("Facing",m=>{m.Ink("BDEDE0",Finish.Luminous,.95f);
            m.Tri(At(-.106f,0,-.044f),At(.106f,0,-.044f),At(0,0,.089f),Vector3.up);
        })).transform;
        actionRing=Draw("Turn pulse",world,Remember("Turn pulse",m=>{m.Ink("D6D7F5",Finish.Luminous,.9f);
            for(int j=0;j<8;j++)m.Ring(Vector3.zero,.918f,.948f,5,j*45+4,j*45+39);
        })).transform;
    }
    void Frame(AstralGame g,bool newFloor)
    {
        float aspect=Mathf.Max(.75f,cameraView.aspect);
        bool close=g.Mode==AstralMode.Explore||g.Mode==AstralMode.Relic;
        float size=close?Mathf.Max(g.Lit>0?5.6f:4.85f,6.3f/(aspect*.765f)):
            Mathf.Max(8.57f,(cols*.51f+rows*.068f+1.01f)/(aspect*.765f));
        cameraView.orthographicSize=size;
        var offset=At(2.4f,24,-16.2f);
        Quaternion rotation=Quaternion.LookRotation(-offset);
        // Leave the 190-unit left HUD column free in a 1080-unit viewport.
        Vector3 center=close?At(Mathf.Clamp(g.Hero.x,2.2f,cols-3.2f),.16f,Mathf.Clamp(g.Hero.y,2.1f,rows-3.1f)+.25f):
            At((cols-1)*.5f,.16f,(rows-1)*.5f+.25f);
        Vector3 focus=center-rotation*Vector3.right*(size*aspect*.181f);
        cameraFocus=newFloor||close!=closeCamera?focus:Vector3.Lerp(cameraFocus,focus,1-Mathf.Exp(-Time.unscaledDeltaTime*9));
        cameraView.transform.SetPositionAndRotation(cameraFocus+offset,rotation);closeCamera=close;
        art.SetVector("_Surveyor",new Vector4(g.Hero.x,g.Hero.y,g.Lit>0?1:0,close?1:0));
    }
    void BuildBoard(AstralGame g)
    {
        if(setting){setting.gameObject.SetActive(false);Release(setting.gameObject);}
        Release(settingMesh);
        if(chunks!=null)foreach(var m in chunkMeshes)Release(m);
        foreach(var p in enemies.Values){p.root.gameObject.SetActive(false);Release(p.root.gameObject);}enemies.Clear();
        Release(mask);
        level=g.Floor;seed=g.Seed;cols=g.Cols;rows=g.Rows;step=g.StepSerial;actionAge=3;
        chunkCols=(cols+ChunkSize-1)/ChunkSize;
        int count=chunkCols*((rows+ChunkSize-1)/ChunkSize);
        chunks=new MeshFilter[count];chunkMeshes=new Mesh[count];dirty=new bool[count];
        tiles=new AstralTile[cols*rows];pixels=new Color32[cols*rows];
        mask=new Texture2D(cols,rows,TextureFormat.RGBA32,false,true){name="Turn visibility",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp};
        art.SetTexture("_Visibility",mask);art.SetVector("_Size",new Vector4(cols,rows,0,0));
        setting=Group("Floor "+level,world);
        var structure=new AstralMesh();new AstralArchitecture(structure,level).Surroundings(cols,rows,seed);
        settingMesh=structure.Build("The observatory");Draw("Celestial instruments",setting,settingMesh);
        for(int i=0;i<count;i++)chunks[i]=Draw("Survey sector "+i,setting,null);
    }
    Mesh[] chunkMeshes;
    bool Inside(int x,int y)=>x>=0&&y>=0&&x<cols&&y<rows;
    int Sector(int x,int y)=>x/ChunkSize+y/ChunkSize*chunkCols;
    bool Wall(int x,int y)=>!Inside(x,y)||tiles[x+y*cols]==AstralTile.Wall;
    void UpdateMap(AstralGame g,bool all)
    {
        version=g.MapVersion;
        for(int y=0;y<rows;y++)
        for(int x=0;x<cols;x++)
        {
            int index=x+cols*y;var tile=g.Cell(x,y);
            if(all||tile!=tiles[index])
            {
                tiles[index]=tile;dirty[Sector(x,y)]=true;
                if(x>0)dirty[Sector(x-1,y)]=true;if(x+1<cols)dirty[Sector(x+1,y)]=true;
                if(y>0)dirty[Sector(x,y-1)]=true;if(y+1<rows)dirty[Sector(x,y+1)]=true;
            }
            bool visible=g.Visible(x,y), known=visible||g.Seen(x,y);
            pixels[index]=new Color32(visible?(byte)255:known?(byte)110:(byte)0,visible&&g.ThreatAt(x,y)?(byte)255:(byte)0,0,255);
        }
        mask.SetPixels32(pixels);mask.Apply(false,false);
        for(int i=0;i<chunks.Length;i++)
        {
            if(!dirty[i])continue;dirty[i]=false;
            int sx=(i%chunkCols)*ChunkSize,sy=(i/chunkCols)*ChunkSize;
            var mesh=new AstralMesh();var artist=new AstralArchitecture(mesh,level);
            for(int y=sy;y<Mathf.Min(sy+ChunkSize,rows);y++)
            for(int x=sx;x<Mathf.Min(sx+ChunkSize,cols);x++)
                artist.Tile(x,y,tiles[x+cols*y],Wall(x-1,y)&&Wall(x+1,y),!Wall(x-1,y)||!Wall(x+1,y)||!Wall(x,y-1)||!Wall(x,y+1));
            Mesh old=chunkMeshes[i];chunkMeshes[i]=mesh.Build("Sector "+i);chunks[i].sharedMesh=chunkMeshes[i];Release(old);
        }
    }
    Piece NewPiece(int model,Vector2Int p,bool hero)
    {
        var item=new Piece{model=model,target=p};item.root=Group(hero?"Surveyor":"Guardian",world);
        item.root.position=At(p.x,.041f,p.y);
        var shadow=Draw("Ground wash",item.root,Remember("Foot shadow",Shadow),true);
        shadow.transform.localPosition=Vector3.up*.011f;shadow.transform.localScale=At(model==7?.55f:.39f,1,model==7?.46f:.33f);
        item.form=Draw("Miniature",item.root,Remember("Miniature "+model,m=>new AstralMiniature(m).Build(model))).transform;
        if(hero)return item;
        item.warning=Draw("Readied attack",item.root,Remember("Prediction",m=>{m.Ink("FFC390",Finish.Luminous,.94f);
            for(int j=0;j<3;j++)m.Ring(Vector3.zero,.316f,.371f,4,j*120+6,j*120+96);
        })).transform;item.warning.localPosition=Vector3.up*.034f;
        item.ice=Draw("Suspended",item.root,Remember("Icy orbit",m=>{m.Ink("93DDED",Finish.Luminous,.77f);m.Ring(Vector3.zero,.33f,.377f,6);
            for(int j=0;j<4;j++){float a=j*Mathf.PI*.5f;m.Jewel(At(Mathf.Sin(a)*.29f,.15f,Mathf.Cos(a)*.29f),At(.041f,.119f,.052f),5);}
        })).transform;item.ice.localPosition=Vector3.up*.033f;
        item.bar=Group("Vitality",item.root);item.bar.localPosition=Vector3.up*(model==7?1.61f:model==3?.63f:model==5?.83f:1.06f);
        Draw("Backing",item.bar,Remember("Vitality rail",m=>{m.Ink("313347",Finish.Luminous,1);m.Block(0,0,0,.50f,.045f,.012f);}));
        item.life=Group("Remaining",item.bar);item.life.localPosition=Vector3.left*.235f;
        Draw("Health",item.life,Remember("Vitality pigment",m=>{m.Ink("EDAA93",Finish.Luminous,1);m.Block(.235f,0,-.009f,.47f,.024f,.009f);}));
        return item;
    }
    void Pose(Piece p,Vector2Int cell,Vector2Int face,bool instant,float burst,bool strike)
    {
        Vector3 target=At(cell.x,.041f,cell.y);
        float distance=Vector3.Distance(target,p.root.position);
        Vector3 previous=p.root.position;
        p.root.position=instant||distance>2.3f?target:Vector3.MoveTowards(p.root.position,target,Time.unscaledDeltaTime*8.5f);
        if(face.sqrMagnitude>0)
        {
            var direction=Quaternion.LookRotation(At(face.x,0,face.y));
            p.form.rotation=instant?direction:Quaternion.RotateTowards(p.form.rotation,direction,Mathf.Max(Time.unscaledDeltaTime*950,1));
        }
        p.walk+=(previous-p.root.position).magnitude*3.14f;
        float height=distance>.025f?Mathf.Abs(Mathf.Sin(p.walk))*.083f:0;
        if(p.model==4)height+=.028f*Mathf.Sin(Time.unscaledTime*2.3f+cell.x);
        p.form.localPosition=Vector3.up*height+(strike?p.form.forward*Mathf.Sin(burst*Mathf.PI)*.145f:Vector3.zero);
        p.target=cell;
    }
    static void Shadow(AstralMesh m)
    {
        string[] c={"101927BD","101927A6","10192742","10192700"};float[] r={0,.35f,.68f,1};
        for(int b=0;b<3;b++)for(int j=0;j<18;j++)
        {
            float a=j*Mathf.PI/9,ang=(j+1)*Mathf.PI/9;
            Vector3 u=At(Mathf.Cos(a),0,Mathf.Sin(a)),v=At(Mathf.Cos(ang),0,Mathf.Sin(ang));
            m.GradientTri(u*r[b],u*r[b+1],v*r[b+1],c[b],c[b+1],c[b+1]);
            m.GradientTri(u*r[b],v*r[b+1],v*r[b],c[b],c[b+1],c[b]);
        }
    }
    void OnDestroy()
    {
        if(chunkMeshes!=null)foreach(var mesh in chunkMeshes)Release(mesh);
        foreach(var mesh in stock.Values)Release(mesh);Release(settingMesh);Release(mask);Release(art);Release(wash);
    }
}
