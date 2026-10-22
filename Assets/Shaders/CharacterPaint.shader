Shader "Mist/CharacterPaint" {
 Properties {
  _ImpactFlash("Contact flash",Range(0,1))=0
  _Dissolve("Defeat dissolve",Range(0,1))=0
  _Color("Pigment tint",Color)=(1,1,1,1)
  _MainTex("Painted character albedo",2D)="white"{}
  _OutlineWidth("Contour",Float)=.003
  _Outline("Contour ink",Color)=(.055,.045,.037,1)
  _Smoothness("Polish",Range(0,1))=.12
 }
 SubShader {
  Tags {"RenderType"="Opaque"}
  Pass {
   Cull Front ZWrite On
   CGPROGRAM
   #pragma vertex vert
   #pragma fragment frag
   #include "UnityCG.cginc"
   float _OutlineWidth,_Dissolve;fixed4 _Outline;
   struct v2f {float4 pos:SV_POSITION;float3 world:TEXCOORD0;};
   v2f vert(appdata_base v){v2f o;v.vertex.xyz+=v.normal*_OutlineWidth;o.pos=UnityObjectToClipPos(v.vertex);o.world=mul(unity_ObjectToWorld,v.vertex).xyz;return o;}
   fixed4 frag(v2f i):SV_Target{clip(frac(sin(dot(floor(i.world*55),float3(12.9898,78.233,37.719)))*43758.5453)+.001-_Dissolve);return _Outline;}
   ENDCG
  }
  CGPROGRAM
  #pragma surface surf Painted fullforwardshadows addshadow
  #pragma target 3.0
  sampler2D _MainTex;fixed4 _Color;half _Smoothness;float _ImpactFlash,_Dissolve;
  struct Input {float3 worldPos;float2 uv_MainTex;float4 color:COLOR;};
  half4 LightingPainted(SurfaceOutput s,half3 lightDir,half3 viewDir,half attenuation){
   half n=dot(s.Normal,lightDir);
   half bands=.68+.25*smoothstep(-.12,.20,n)+.07*smoothstep(.58,.82,n);
   return half4(s.Albedo*_LightColor0.rgb*bands*attenuation,s.Alpha);
  }
  void surf(Input i,inout SurfaceOutput o){
   clip(frac(sin(dot(floor(i.worldPos*55),float3(12.9898,78.233,37.719)))*43758.5453)+.001-_Dissolve);
   fixed3 paint=tex2D(_MainTex,i.uv_MainTex).rgb*_Color.rgb*i.color.rgb;
   // Preserve the graphic painted planes while still receiving scene shadows.
   o.Albedo=lerp(paint*.76,fixed3(1,.91,.74),_ImpactFlash);o.Emission=paint*.24+_ImpactFlash*.32;o.Gloss=_Smoothness;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Diffuse"
}
