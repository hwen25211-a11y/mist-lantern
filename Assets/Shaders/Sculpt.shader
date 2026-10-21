Shader "Mist/Sculpt" {
 Properties {
  _Color("Pigment",Color)=(1,1,1,1)
  _CraftTex("Craft surfaces",2D)="gray"{}
  _DetailTex("Stone pigment",2D)="gray"{}
  _SurfaceType("Surface family",Float)=0
  _CraftStrength("Material grain strength",Range(0,1))=1
  _CavityStrength("Cavity strength",Range(0,1))=1
  _ToonSkin("Graphic skin shading",Range(0,1))=0
  _Metallic("Metal",Range(0,1))=0
  _Smoothness("Polish",Range(0,1))=.2
  _EmissionColor("Light",Color)=(0,0,0,0)
  _OutlineWidth("Contour",Float)=.006
  _Outline("Contour ink",Color)=(.038,.052,.055,1)
 }
 SubShader {
 Tags {"RenderType"="Opaque"}
 Pass {
  Cull Front ZWrite On
  CGPROGRAM
  #pragma vertex vert
  #pragma fragment frag
  #include "UnityCG.cginc"
  float _OutlineWidth;fixed4 _Outline;
  struct v2f{float4 pos:SV_POSITION;};
  v2f vert(appdata_base v){v2f o;v.vertex.xyz+=v.normal*_OutlineWidth;o.pos=UnityObjectToClipPos(v.vertex);return o;}
  fixed4 frag(v2f i):SV_Target{return _Outline;}
  ENDCG
 }
 CGPROGRAM
 #pragma surface surf Sculpt fullforwardshadows addshadow vertex:vert
 #pragma target 3.0
 #include "UnityPBSLighting.cginc"
 fixed4 _Color,_EmissionColor;half _Metallic,_Smoothness;float _SurfaceType,_CraftStrength,_CavityStrength,_ToonSkin;sampler2D _DetailTex,_CraftTex;
 struct Input{float3 localPos;float3 localNormal;float3 worldPos;float3 worldNormal;float3 viewDir;float4 color:COLOR;};
 void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.color=v.color;o.localPos=v.vertex.xyz;o.localNormal=v.normal;}
 half4 LightingSculpt(SurfaceOutput s,half3 lightDir,half3 viewDir,half atten){
  half n=dot(s.Normal,lightDir);
  half diffuse=.13+.64*smoothstep(.02,.2,n)+.23*smoothstep(.63,.78,n);
  if(_SurfaceType>6.5)diffuse=lerp(.25+.75*smoothstep(-.4,.75,n),.28+.57*smoothstep(.02,.22,n)+.15*smoothstep(.65,.8,n),_ToonSkin);
  half3 h=normalize(lightDir+viewDir);
  half spec=pow(saturate(dot(s.Normal,h)),lerp(18,96,s.Gloss))*(_Metallic*.65);
  half3 specColor=lerp(half3(.85,.88,.9),s.Albedo,_Metallic*.65);
  half3 rgb=(s.Albedo*diffuse+specColor*spec)*_LightColor0.rgb*atten;
  return half4(rgb,s.Alpha);
 }
 void surf(Input i,inout SurfaceOutput o){
  // Geometric cavity is baked into vertex alpha; the surface remains fully opaque.
  half ao=lerp(1,lerp(.38,1,saturate(i.color.a)),_CavityStrength);
  half textureVariation=sin(i.worldPos.x*63+i.worldPos.y*37)*sin(i.worldPos.z*71-i.worldPos.y*43);
  float pigment=1;
  if(_SurfaceType>.5&&_SurfaceType<1.5){
   float3 n=abs(i.worldNormal);n/=max(.001,n.x+n.y+n.z);
   float2 uvX=frac(i.worldPos.zy/3.7),uvY=frac(i.worldPos.xz/4.6),uvZ=frac(i.worldPos.xy/3.7);
   uvX.x/=3;uvY.x/=3;uvZ.x/=3;
   float a=dot(tex2D(_DetailTex,uvX).rgb,float3(.299,.587,.114))*n.x+dot(tex2D(_DetailTex,uvY).rgb,float3(.299,.587,.114))*n.y+dot(tex2D(_DetailTex,uvZ).rgb,float3(.299,.587,.114))*n.z;
   pigment=.72+a*.9;
  }else if(_SurfaceType>1.5&&_SurfaceType<2.5){pigment=.92+.08*sin(i.worldPos.y*4+sin(i.worldPos.x*33+i.worldPos.z*25));}
  else if(_SurfaceType>2.5&&_SurfaceType<6.5){
   float2 offset=_SurfaceType<3.5?float2(0,.5):_SurfaceType<4.5?float2(.5,.5):_SurfaceType<5.5?float2(0,0):float2(.5,0);
   float3 n=pow(abs(i.localNormal),4);n/=max(.001,n.x+n.y+n.z);
   float scale=_SurfaceType<3.5?1.1:.85;
   float2 u=frac(i.localPos.zy*scale)*.49+offset+.005;
   float2 v=frac(i.localPos.xz*scale)*.49+offset+.005;
   float2 w=frac(i.localPos.xy*scale)*.49+offset+.005;
   float grain=tex2D(_CraftTex,u).r*n.x+tex2D(_CraftTex,v).r*n.y+tex2D(_CraftTex,w).r*n.z;
   pigment=lerp(1,.65+grain*.85,(_SurfaceType<3.5?.5:.85)*_CraftStrength);
  }
  o.Albedo=_Color.rgb*i.color.rgb*ao*pigment*(.985+textureVariation*.015);
  o.Gloss=_Smoothness;o.Specular=.3;
  o.Emission=_EmissionColor.rgb*.4;
  o.Alpha=1;
 }
 ENDCG
 }
 Fallback "Diffuse"
}
