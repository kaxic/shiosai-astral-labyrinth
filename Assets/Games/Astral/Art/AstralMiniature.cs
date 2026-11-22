using UnityEngine;
using static AstralMesh;
using static AstralMesh.Finish;

internal sealed class AstralMiniature
{
    readonly AstralMesh m;
    public AstralMiniature(AstralMesh mesh){m=mesh;}
    void I(string c,Finish f=Plain,float glow=0)=>m.Ink(c,f,glow);
    void Jewel(float x,float y,float z,float w,float h,float d,int n=7)=>m.Jewel(At(x,y,z),At(w,h,d),n,.12f);
    void Rod(Vector3 a,Vector3 b,float r,float s,int n=7)=>m.Tube(a,b,r,s,n);
    void B(float x,float y,float z,float w,float h,float d)=>m.Block(x,y,z,w,h,d);
    public void Build(int model)
    {
        switch(model)
        {
            case 0: Hero(0);break;
            case 1: Hero(1);break;
            case 2: Hero(2);break;
            case 3: Scarab();break;
            case 4: PrismEye();break;
            case 5: Lynx();break;
            case 6: Priest(false);break;
            default: Priest(true);break;
        }
    }
    void Hero(int job)
    {
        string robe=job==0?"4C7A91":job==1?"7B689E":"488C87";
        foreach(float x in new[]{-.105f,.105f})
        {
            I("424D64",Stone);Rod(At(x,.10f,0),At(x,.43f,0),.073f,.088f);
            I("886E5E",Metal);Jewel(x,.089f,.019f,.095f,.076f,.133f);
        }
        I(robe,Enamel);m.Drum(0,.31f,0,job==1?.245f:.20f,.143f,.284f,8);
        Jewel(0,.59f,0,.209f,.194f,.149f);
        I("D2B783",Metal);m.Drum(0,.44f,0,.179f,.176f,.045f,8);
        I("E9C5A3");Jewel(0,.835f,.031f,.141f,.145f,.132f);
        I("58616E");B(0,.852f,.157f,.136f,.022f,.015f);
        foreach(float x in new[]{-.196f,.196f})
        {
            I(robe,Enamel);Rod(At(x,.69f,0),At(x*1.18f,.51f,.041f),.085f,.065f);
            I("D9AB8D");Jewel(x*1.17f,.493f,.068f,.065f,.074f,.067f);
        }
        I(robe,Enamel);m.Quad(At(-.146f,.718f,-.101f),At(.146f,.718f,-.101f),At(.214f,.332f,-.184f),At(-.171f,.282f,-.195f),Vector3.back);
        I("EACB8B",Metal);m.Beam(At(-.080f,.565f,-.160f),At(0,.465f,-.178f),.019f,.020f);m.Beam(At(0,.465f,-.178f),At(.083f,.562f,-.161f),.019f,.02f);
        if(job==0)
        {
            I("BFC4B9",Metal);Jewel(0,.925f,0,.166f,.149f,.156f);I("ADC3C5",Metal);
            Jewel(-.205f,.682f,0,.137f,.116f,.123f);Jewel(.205f,.682f,0,.137f,.116f,.123f);
            I("8AD1D8",Enamel);Jewel(0,1.053f,-.041f,.044f,.125f,.113f);
            I("C7AE71",Metal);Jewel(-.235f,.515f,.12f,.165f,.245f,.046f,6);
            I("56899D",Enamel);Jewel(-.235f,.515f,.153f,.134f,.189f,.035f,6);
            I("F4DA9B",Luminous,.54f);Jewel(-.235f,.515f,.184f,.042f,.068f,.019f,4);
            I("927356",Metal);Rod(At(.246f,.430f,.060f),At(.255f,.583f,.233f),.029f,.027f);
            I("DFE4DD",Metal);m.Tri(At(.197f,.581f,.222f),At(.308f,.581f,.222f),At(.260f,.757f,.521f),Vector3.up);
        }
        else if(job==1)
        {
            I("8B749F",Enamel);m.Drum(0,.903f,-.014f,.276f,.272f,.035f,13);
            m.Drum(0,.93f,-.015f,.179f,.025f,.307f,8);
            I("E6C98F",Metal);m.Drum(0,.979f,-.015f,.152f,.141f,.032f,8);Jewel(0,1.247f,-.014f,.035f,.042f,.033f,5);
            I("9C7C5D",Metal);Rod(At(.267f,.063f,.082f),At(.318f,.96f,.099f),.033f,.027f);
            I("BCD7FF",Luminous,.84f);Jewel(.323f,1.024f,.095f,.077f,.089f,.074f);
            I("CCB57F",Metal);m.Orbit(At(.323f,1.026f,.097f),.105f,.012f,Quaternion.Euler(64,32,5),15);
            I("CDBA95",Plain);m.Box(At(-.268f,.56f,.15f),At(.198f,.040f,.173f),Quaternion.Euler(-25,-18,8));
        }
        else
        {
            I("428C89",Enamel);Jewel(0,.867f,-.03f,.182f,.193f,.149f,8);
            I("B3DDD1",Metal);B(0,.863f,.169f,.189f,.043f,.027f);
            I("E4D3A8");m.Drum(0,.709f,0,.151f,.137f,.057f,8);
            m.Quad(At(.07f,.724f,-.09f),At(.139f,.714f,-.095f),At(.338f,.529f,-.213f),At(.253f,.511f,-.20f),Vector3.up);
            I("CEDCD9",Metal);
            foreach(float sign in new[]{-1f,1f})m.Tri(At(sign*.21f,.47f,.14f),At(sign*.31f,.463f,.15f),At(sign*.38f,.54f,.36f),Vector3.up);
        }
    }
    void Scarab()
    {
        foreach(float sign in new[]{-1f,1f})
        for(int j=0;j<3;j++)
        {
            float z=(j-1)*.14f;I("9D8A78",Metal);
            Rod(At(sign*.13f,.22f,z),At(sign*.285f,.159f,z+.060f),.035f,.032f,5);
            Rod(At(sign*.285f,.159f,z+.060f),At(sign*.35f,.055f,z+.020f),.033f,.014f,5);
        }
        I("926C75",Quartz);Jewel(0,.278f,-.07f,.24f,.167f,.286f,9);
        I("C8936E",Metal);Jewel(-.10f,.334f,-.051f,.105f,.079f,.211f);Jewel(.10f,.334f,-.051f,.105f,.079f,.211f);
        I("505369",Stone);Jewel(0,.246f,.194f,.174f,.127f,.134f);
        I("FFB887",Luminous,.89f);Jewel(-.098f,.293f,.291f,.039f,.034f,.038f,5);Jewel(.098f,.293f,.291f,.039f,.034f,.038f,5);
        I("BBA783",Metal);Rod(At(0,.339f,.194f),At(.014f,.499f,.345f),.056f,.004f,5);
    }
    void PrismEye()
    {
        I("887C9B",Metal);Jewel(0,.58f,0,.245f,.257f,.189f,7);
        I("B7AFB8",Metal);m.Orbit(At(0,.58f,.03f),.277f,.034f,Quaternion.Euler(83,5,-12),21);
        I("E2B4DC",Enamel,.69f);Jewel(0,.58f,.125f,.205f,.170f,.115f);
        I("4F4864");Jewel(0,.58f,.230f,.080f,.098f,.021f);
        I("F5CDDE",Luminous,.91f);Jewel(0,.58f,.246f,.032f,.085f,.013f);
        foreach(float s in new[]{-1f,1f}){I("AFA7B4",Metal);Jewel(s*.237f,.54f,0,.123f,.084f,.072f,5);}
        I("B981B4",Luminous,.48f);Jewel(0,.248f,0,.063f,.109f,.060f,6);
    }
    void Lynx()
    {
        foreach(float s in new[]{-1f,1f})
        foreach(float z in new[]{-.14f,.15f})
        {I("69647A",Quartz);Rod(At(s*.15f,.34f,z),At(s*.20f,.073f,z+.034f),.059f,.036f);}
        I("696180",Quartz);Jewel(0,.357f,-.018f,.182f,.156f,.31f);
        I("887599",Enamel);Jewel(0,.492f,.196f,.157f,.149f,.138f);
        foreach(float s in new[]{-1f,1f})
        {
            I("957B96",Quartz);Rod(At(s*.095f,.572f,.165f),At(s*.164f,.76f,.105f),.072f,.004f,5);
            I("F5B9DB",Luminous,.83f);Jewel(s*.100f,.523f,.297f,.036f,.027f,.027f,5);
        }
        I("B7A2C5",Metal);Jewel(0,.487f,-.013f,.066f,.064f,.232f);I("796C92",Quartz);
        Rod(At(0,.38f,-.263f),At(.118f,.60f,-.419f),.071f,.033f);
    }
    void Priest(bool sovereign)
    {
        float s=sovereign?1.30f:1;
        Vector3 p(float x,float y,float z)=>At(x,y,z)*s;
        I(sovereign?"705F81":"656783",Enamel);m.Tube(p(0,.05f,0),p(0,.54f,0),.254f*s,.135f*s,9);
        I(sovereign?"AB9479":"9096A3",Metal);m.Jewel(p(0,.61f,0),p(.244f,.186f,.151f),8);
        I("4C4D6B",Quartz);m.Jewel(p(0,.829f,0),p(.158f,.189f,.143f),7);
        I(sovereign?"F7C477":"DAA6DD",Luminous,.93f);m.Jewel(p(0,.852f,.134f),p(.085f,.038f,.027f),6);
        foreach(float sign in new[]{-1f,1f})
        {
            I(sovereign?"CFB37D":"ADA9BC",Metal);m.Jewel(p(sign*.238f,.627f,0),p(.15f,.091f,.133f),6);
            I("736680",Enamel);Rod(p(sign*.195f,.57f,0),p(sign*.248f,.441f,.063f),.068f*s,.049f*s);
        }
        I("B39B73",Metal);Rod(p(.255f,.075f,.032f),p(.274f,1.058f,.044f),.034f*s,.026f*s);
        I(sovereign?"E1BD79":"AB9AC0",Metal);m.Orbit(p(.274f,1.088f,.043f),.122f*s,.019f*s,Quaternion.Euler(73,24,0),15);
        I("DBC1E8",Luminous,.77f);m.Jewel(p(.274f,1.09f,.046f),p(.049f,.077f,.048f),6);
        if(sovereign)
        {
            I("D8B377",Metal);m.Orbit(p(0,.922f,-.082f),.267f*s,.026f*s,Quaternion.Euler(67,0,0),21);
            for(int j=0;j<5;j++){float a=j*Mathf.PI*2/5;m.Tube(p(Mathf.Cos(a)*.13f,.941f,Mathf.Sin(a)*.13f),p(Mathf.Cos(a)*.215f,1.155f,Mathf.Sin(a)*.215f),.055f*s,.004f,5);}
        }
    }
}
