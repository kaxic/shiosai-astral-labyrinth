using UnityEngine;
using static AstralMesh;
using static AstralMesh.Finish;

internal sealed class AstralArchitecture
{
    readonly AstralMesh m;
    readonly int theme;
    readonly string paving,wall,cap,trim,soul,crown;
    readonly Finish rock;
    public AstralArchitecture(AstralMesh mesh,int floor)
    {
        m=mesh;theme=Mathf.Min(3,(Mathf.Max(1,floor)-1)/2);
        paving=new[]{"7B889B","688C98","8A8096","7785A2"}[theme];
        wall=new[]{"4B5A74","466D79","5D4D69","4D5879"}[theme];
        cap=new[]{"788499","72909A","87728C","8188A6"}[theme];
        crown=new[]{"53647C","476C78","615970","555F80"}[theme];
        trim=new[]{"AE9B70","A2B5A0","C59676","C9B079"}[theme];
        soul=new[]{"9FE0DE","A6E3FC","F2A7BC","F9DB94"}[theme];
        rock=theme==2?Quartz:theme==3?Enamel:Stone;
    }
    void Ink(string hex,Finish mat=Plain,float glow=0)=>m.Ink(hex,mat,glow);
    void Block(float x,float y,float z,float w,float h,float d,float turn=0)=>m.Block(x,y,z,w,h,d,turn);
    void Gem(float x,float y,float z,float w,float h,float d,int n=7)=>m.Jewel(At(x,y,z),At(w,h,d),n,.16f);
    void Drum(float x,float y,float z,float r,float s,float h,int n=8)=>m.Drum(x,y,z,r,s,h,n);
    public void Tile(int x,int z,AstralTile cell,bool horizontal,bool boundary)
    {
        int detail=(x*13+z*29+theme*11)%17;
        m.originOffset=At(x,0,z);m.fogCell=new Vector2(x,z);
        if(cell==AstralTile.Wall)
        {
            float height=boundary?.42f:.30f;
            Ink(wall,rock);Block(0,height*.5f,0,.995f,height,.995f);
            Ink(boundary?crown:wall,rock);Block(0,height+.024f,0,.950f,.051f,.950f);
            if(boundary)
            {
                Ink(trim,Metal);Block(0,.245f,-.495f,.81f,.022f,.014f);
                Ink(cap,Metal);Block(0,height+.053f,.35f,.56f,.012f,.025f);
                if(detail%7==0)
                {
                    Ink(cap,Metal);Drum(0,.46f,0,.214f,.16f,.084f,6);
                    Ink(soul,Luminous,.58f);Gem(0,.57f,0,.085f,.089f,.085f,6);
                }
            }
            return;
        }
        Ink(detail<3?cap:paving,rock);
        Block(0,-.012f,0,.974f,.084f,.974f);
        if(detail==4 || detail==10)
        {
            Ink(trim,Metal);m.Star(At(.28f,.033f,.28f),detail==4?.080f:.053f,4,.13f);
        }
        if(cell!=AstralTile.Floor)
        {
            // Golden corners distinguish an interaction site from the ordinary paving.
            Ink(trim,Metal);
            foreach(float sx in new[]{-1f,1f})
            foreach(float sz in new[]{-1f,1f})
            {
                Block(sx*.36f,.036f,sz*.405f,.12f,.013f,.022f);
                Block(sx*.405f,.036f,sz*.36f,.022f,.013f,.12f);
            }
        }
        switch(cell)
        {
            case AstralTile.Door: Door(horizontal,false);break;
            case AstralTile.OpenDoor: Door(horizontal,true);break;
            case AstralTile.Stairs: Stairs();break;
            case AstralTile.Chest: Chest();break;
            case AstralTile.Shrine: Shrine();break;
            case AstralTile.Trap: Trap();break;
            case AstralTile.Potion: Potion();break;
            case AstralTile.Crystal: Crystal();break;
            case AstralTile.Beacon: Beacon(true);break;
            case AstralTile.SpentBeacon: Beacon(false);break;
        }
    }
    void Door(bool horizontal,bool open)
    {
        Quaternion axis=Quaternion.Euler(0,horizontal?0:90,0);
        void panel(float x,float y,float z,float w,float h,float d,float wing=0)
        {m.Box(axis*At(x,y,z),At(w,h,d),axis*Quaternion.Euler(0,wing,0));}
        Ink(open?"78918A":"B39A6B",Metal);panel(0,.049f,0,.88f,.027f,.235f);
        foreach(float x in new[]{-.40f,.40f})
        {
            Ink(wall,Stone);panel(x,.29f,0,.12f,.52f,.18f);
            Ink(trim,Metal);panel(x,.56f,0,.16f,.07f,.22f);
        }
        Ink(open?"617B79":"94764F",Metal);
        if(open)panel(-.37f,.29f,.205f,.38f,.45f,.080f,88);
        else
        {
            panel(0,.28f,0,.70f,.47f,.112f);
            Ink("D5B86F",Metal);panel(0,.38f,-.066f,.67f,.038f,.02f);
            panel(0,.528f,0,.69f,.034f,.161f);
            panel(.18f,.26f,-.080f,.081f,.09f,.025f);
        }
    }
    void Stairs()
    {
        Ink("2C3C54",Enamel);Block(0,.051f,0,.79f,.052f,.82f);
        for(int j=0;j<5;j++)
        {
            float height=.06f+j*.049f;Ink(j%2==0?"A6B5BB":"8C9DAD",Stone);
            Block(0,.055f+height*.5f,.30f-j*.15f,.66f,height,.145f);
            Ink("D0C5A1",Metal);Block(0,.060f+height,.23f-j*.15f,.65f,.011f,.019f);
        }
        foreach(float x in new[]{-.384f,.384f})
        {
            Ink(trim,Metal);m.Beam(At(x,.11f,.38f),At(x,.34f,-.38f),.042f,.042f);
        }
        Ink(soul,Luminous,.84f);m.Star(At(0,.337f,-.31f),.081f);
    }
    void Chest()
    {
        Ink("845E53",Quartz);Block(0,.22f,0,.66f,.32f,.41f);
        Ink("AF865B",Metal);Block(0,.414f,0,.685f,.13f,.44f);
        foreach(float x in new[]{-.217f,.217f}){Ink("D1AD6B",Metal);Block(x,.296f,0,.060f,.393f,.46f);}
        Ink("EBD391",Metal,.28f);Block(0,.29f,-.235f,.118f,.125f,.042f);
    }
    void Shrine()
    {
        Ink(wall,Stone);Drum(0,.047f,0,.36f,.31f,.12f);
        Ink(trim,Metal);Drum(0,.166f,0,.29f,.21f,.095f);
        Ink("77949F",Stone);Drum(0,.258f,0,.17f,.215f,.17f);
        Ink("CEB487",Metal);Drum(0,.428f,0,.21f,.30f,.09f);
        Ink("CAB8EC",Luminous,.8f);Gem(0,.69f,0,.115f,.18f,.12f);
        Ink("B4A4C7",Metal);m.Orbit(At(0,.70f,0),.205f,.017f,Quaternion.Euler(65,35,12),18);
    }
    void Trap()
    {
        Ink("875663",Metal);m.Ring(At(0,.047f,0),.265f,.31f,3,90,450);
        for(int j=0;j<3;j++){float a=j*2.0944f;float x=Mathf.Cos(a)*.145f,z=Mathf.Sin(a)*.145f;
            Ink("CE8C92",Metal);Drum(x,.052f,z,.075f,.007f,.188f,5);}
        Ink("F7A27A",Luminous,.74f);m.Star(At(0,.062f,0),.092f,4);
    }
    void Potion()
    {
        Ink("A8A391",Metal);m.Ring(At(0,.045f,0),.17f,.22f,14);
        Ink("D66D84",Enamel,.32f);Gem(0,.237f,0,.161f,.165f,.153f);
        Ink("E2A0A7",Luminous,.35f);Drum(0,.355f,0,.077f,.070f,.074f);
        Ink("C6A17A",Metal);Drum(0,.429f,0,.085f,.083f,.045f);
    }
    void Crystal()
    {
        Ink("74CDD5",Quartz,.60f);Gem(-.055f,.326f,0,.117f,.27f,.111f,6);
        Ink("C0E6D8",Quartz,.64f);Gem(.10f,.216f,-.011f,.074f,.157f,.082f,5);
        Ink("CDD2A3",Metal);m.Ring(At(0,.046f,0),.197f,.221f,14);
    }
    void Beacon(bool lit)
    {
        Ink(wall,Stone);Drum(0,.05f,0,.29f,.26f,.135f,6);
        Ink(lit?"BEA76B":"79837F",Metal);Drum(0,.185f,0,.25f,.15f,.17f,6);
        Ink(lit?"9CECE3":"7D8D95",lit?Luminous:Stone,lit?.95f:0);Gem(0,.553f,0,.135f,.220f,.128f,6);
        Ink(lit?"D8C286":"8A8D85",Metal);m.Orbit(At(0,.513f,0),.23f,.022f,Quaternion.Euler(52,20,33),20);
        m.Orbit(At(0,.513f,0),.245f,.012f,Quaternion.Euler(10,-30,-57),18);
    }
    public void Surroundings(int cols,int rows,int seed)
    {
        m.fogCell=new Vector2(-1,-1);m.originOffset=Vector3.zero;
        float cx=(cols-1)*.5f,cz=(rows-1)*.5f;
        Ink(theme==2?"27273D":"1A2B44",Sky);Block(cx,-2.05f,cz,130,.15f,110);
        var stars=new System.Random(701+theme);
        for(int j=0;j<260;j++)
        {
            float x=cx+((float)stars.NextDouble()-.5f)*44,z=cz+((float)stars.NextDouble()-.5f)*34;
            if(x>-.9f&&x<cols&&z>-.9f&&z<rows)continue;
            float r=.014f+.048f*(float)stars.NextDouble();
            Ink(j%5==0?"9FAEBE":"627E9B",Luminous,.21f);
            m.Star(At(x,-1.96f,z),r,4,(float)stars.NextDouble());
        }
        // The chart bed is visible through the uncharted portion. It carries no map information.
        Ink("28384B",Metal);Block(cx,-.36f,cz,cols+.46f,.56f,rows+.46f);
        Ink("22334B",Chart);Block(cx,-.105f,cz,cols+.055f,.153f,rows+.055f);
        foreach(float side in new[]{-1f,1f})
        {
            Ink(trim,Metal);Block(cx+side*(cols*.5f+.13f),-.056f,cz,.13f,.105f,rows+.37f);
            Block(cx,-.056f,cz+side*(rows*.5f+.13f),cols+.34f,.105f,.13f);
            for(int i=0;i<cols;i++)
            {
                Ink(i%5==0?"C8B88D":"8E978F",Metal);
                Block(i,.005f,cz+side*(rows*.5f+.14f),i%5==0?.052f:.022f,.016f,i%5==0?.13f:.066f);
            }
        }
        foreach(float x in new[]{-1.45f,cols+.46f})
        foreach(float z in new[]{-.97f,rows+.32f})
        {
            Ink(wall,Stone);Drum(x,-1.55f,z,.72f,.54f,1.57f,8);
            Ink(trim,Metal);Drum(x,.02f,z,.55f,.55f,.115f,8);
            if(theme==0)Telescope(x,.15f,z);
            else if(theme==1)Mirrors(x,.15f,z);
            else if(theme==2)Crystals(x,.15f,z);
            else Armillary(x,.15f,z,1);
        }
        float back=rows+1.24f;
        if(theme==0||theme==3)
        {
            for(int j=0;j<5;j++)
            {
                float x=cx+(j-2)*3.7f;
                Ink(wall,Stone);Drum(x,-1.35f,back,.41f,.33f,2.13f,8);
                Ink(trim,Metal);Drum(x,.78f,back,.42f,.48f,.13f,8);
                if(j==2)Armillary(x,.91f,back,.76f);
                else{Ink(cap,Quartz);Gem(x,1.14f,back,.24f,.25f,.24f);}
            }
        }
        else if(theme==1)
        {
            Ink("426879",Enamel);Block(cx,-1.66f,back+1,cols+4,.14f,2.15f);
            for(int j=0;j<5;j++){float x=cx+(j-2)*3.6f;Ink(wall,Stone);Drum(x,-1.63f,back,.47f,.31f,1.35f,7);Mirrors(x,-.28f,back);}
        }
        else for(int j=0;j<8;j++){float x=cx+(j-3.5f)*2.15f;Crystals(x,-.88f,back+.42f*Mathf.Cos(j));}
    }
    void Telescope(float x,float y,float z)
    {
        Vector3 p=At(x,y,z);
        Ink("8E8066",Metal);
        for(int i=0;i<3;i++){float a=i*2.094f;m.Beam(p+At(Mathf.Cos(a)*.40f,.0f,Mathf.Sin(a)*.40f),p+Vector3.up*.78f,.085f,.087f);}
        Vector3 axis=At(.67f,.51f,.36f).normalized,body=p+Vector3.up*.90f;
        Ink("637D85",Metal);m.Tube(body-axis*.45f,body+axis*.57f,.147f,.215f,10);
        Ink("C4AB75",Metal);m.Tube(body+axis*.48f,body+axis*.63f,.226f,.229f,10);
        Ink("97D5D7",Luminous,.62f);m.Tube(body+axis*.63f,body+axis*.65f,.180f,.175f,10);
        Ink(trim,Metal);m.Tube(body-axis*.49f,body-axis*.61f,.12f,.093f,8);
    }
    void Mirrors(float x,float y,float z)
    {
        Ink(trim,Metal);Drum(x,y,z,.29f,.17f,.34f);
        Ink("749BAA",Metal);m.Orbit(At(x,y+.80f,z),.44f,.055f,Quaternion.Euler(69,18,-16),22);
        Ink("A2D4DD",Enamel,.40f);Gem(x,y+.78f,z,.235f,.43f,.124f,6);
    }
    void Crystals(float x,float y,float z)
    {
        for(int j=0;j<5;j++){float a=j*2.4f;float h=.34f+.19f*(j%3);
            Ink(j%2==0?"B788AE":"896FA2",Quartz,.19f);Gem(x+Mathf.Sin(a)*.22f,y+h,z+Mathf.Cos(a)*.23f,.18f,h,.175f,6);}
    }
    void Armillary(float x,float y,float z,float s)
    {
        Ink(trim,Metal);Drum(x,y,z,.28f*s,.16f*s,.31f*s);
        Vector3 center=At(x,y+.84f*s,z);for(int j=0;j<3;j++)
        {Ink(j==0?"D1B87F":"9FA992",Metal);m.Orbit(center,(.43f+j*.07f)*s,.026f*s,Quaternion.Euler(22+j*29,11+j*37,16+j*23),26);}
        Ink(soul,Enamel,.45f);m.Jewel(center,At(.17f,.17f,.17f)*s,10);
    }
}
