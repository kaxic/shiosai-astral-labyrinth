Shader "Astral/Celestial Enamel" {
 Properties {
  [NoScaleOffset] _Slate("Lunar limestone",2D)="gray"{}
  [NoScaleOffset] _Bronze("Orbital bronze",2D)="gray"{}
  [NoScaleOffset] _Quartz("Veined amethyst",2D)="gray"{}
  [NoScaleOffset] _Enamel("Constellation enamel",2D)="gray"{}
  [NoScaleOffset] _Visibility("Discovery and threat",2D)="white"{}
  _Size("Board dimensions",Vector)=(19,15,0,0)
  _Surveyor("Observer position",Vector)=(0,0,0,0)
  _Flash("Starlight",Range(0,1))=0
 }
 SubShader { Tags { "RenderType"="Opaque" }
  Pass {
   CGPROGRAM
   #pragma target 3.0
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   sampler2D _Slate,_Bronze,_Quartz,_Enamel,_Visibility;
   float4 _Size,_Surveyor;half _Flash;
   struct app {float4 vertex:POSITION;float3 normal:NORMAL;float4 uv:TEXCOORD0;float2 cell:TEXCOORD1;half4 pigment:COLOR;};
   struct px {float4 pos:SV_POSITION;float4 uv:TEXCOORD0;float3 world:TEXCOORD1;half3 norm:TEXCOORD2;float2 cell:TEXCOORD3;float4 screen:TEXCOORD4;half3 color:COLOR;};
   px vert(app v){px o;o.pos=UnityObjectToClipPos(v.vertex);o.screen=ComputeScreenPos(o.pos);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.uv=v.uv;o.norm=UnityObjectToWorldNormal(v.normal);o.color=v.pigment.rgb;o.cell=v.cell;return o;}
   half3 ink(sampler2D page,float2 uv){
    half3 a=tex2D(page,uv).rgb;
    #ifndef UNITY_COLORSPACE_GAMMA
    a=LinearToGammaSpace(a);
    #endif
    return a;
   }
   half4 frag(px i):SV_Target{
    half3 c=i.color;float t=i.uv.z;float2 uv=i.uv.xy;half3 n=normalize(i.norm);
    half2 mask=i.cell.x<-.1?half2(1,0):tex2D(_Visibility,(i.cell+.5)/_Size.xy).rg;
    clip(mask.x-.02);
    if(t>.5&&t<1.5)c+=(ink(_Slate,uv/2.40)-half3(.215,.276,.367))*.46;
    else if(t>1.5&&t<2.5)c+=(ink(_Bronze,uv/1.93)-half3(.169,.265,.265))*.52;
    else if(t>2.5&&t<3.5)c+=(ink(_Quartz,uv/2.5)-half3(.222,.205,.292))*.62;
    else if(t>3.5&&t<4.5)c+=(ink(_Enamel,uv/3.11)-half3(.121,.159,.295))*.49;
    else if(t>6.5)c+=(ink(_Enamel,uv/8.20)-half3(.121,.159,.295))*.27;
    half sun=saturate(dot(n,normalize(half3(-.43,.82,-.36))));
    half fill=saturate(dot(n,normalize(half3(.56,.33,.61))));
    half3 lit=saturate(c*(half3(.52,.57,.66)+sun*half3(.46,.42,.39)+fill*half3(.075,.082,.13)));
    half rim=pow(1-saturate(dot(n,normalize(_WorldSpaceCameraPos-i.world))),3);
    if(t>1.5&&t<2.5)lit+=rim*half3(.077,.080,.068);
    lit=lerp(lit,saturate(c*1.10),saturate(i.uv.w));
    if(t>5.5){
     float drift=sin(uv.x*.22+uv.y*.10+.69*sin(uv.y*.26));
     lit+=max(drift,0)*half3(.018,.040,.074);
    }
    if(mask.x<.76){
     half gray=dot(lit,half3(.23,.60,.17));
     lit=lerp(half3(.102,.145,.228),half3(.38,.46,.61),saturate(gray))*.74;
    }
    else if(mask.y>.5&&i.world.y<.11){
     float2 cell=abs(frac(i.world.xz+.5)-.5);
     float edge=smoothstep(.33,.43,max(cell.x,cell.y));
     float beat=.5+.5*sin(_Time.y*4.1);
     lit=lerp(lit,half3(.99,.32,.38),.22+.54*edge+.055*beat);
    }
    if(i.cell.x>=0 && mask.x>.76){
     float nearby=1-smoothstep(1.0,4.8+_Surveyor.z,length(i.world.xz-_Surveyor.xy));
     lit+=half3(.039,.040,.025)*nearby;
    }
    lit=lerp(lit,half3(.72,.96,1),_Flash*.45);
    if(_Surveyor.w>.5)lit=lerp(half3(.075,.114,.180),lit,smoothstep(.175,.205,i.screen.x/i.screen.w));
    #ifndef UNITY_COLORSPACE_GAMMA
    lit=GammaToLinearSpace(saturate(lit));
    #endif
    return half4(lit,1);
   }
   ENDCG
  }
 }
}
