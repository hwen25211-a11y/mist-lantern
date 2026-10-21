Shader "Mist/CharacterPaint" {
 Properties {
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
   float _OutlineWidth;fixed4 _Outline;
   struct v2f {float4 pos:SV_POSITION;};
   v2f vert(appdata_base v){v2f o;v.vertex.xyz+=v.normal*_OutlineWidth;o.pos=UnityObjectToClipPos(v.vertex);return o;}
   fixed4 frag(v2f i):SV_Target{return _Outline;}
   ENDCG
  }
  CGPROGRAM
  #pragma surface surf Painted fullforwardshadows addshadow
  #pragma target 3.0
  sampler2D _MainTex;fixed4 _Color;half _Smoothness;
  struct Input {float2 uv_MainTex;float4 color:COLOR;};
  half4 LightingPainted(SurfaceOutput s,half3 lightDir,half3 viewDir,half attenuation){
   half n=dot(s.Normal,lightDir);
   half bands=.68+.25*smoothstep(-.12,.20,n)+.07*smoothstep(.58,.82,n);
   return half4(s.Albedo*_LightColor0.rgb*bands*attenuation,s.Alpha);
  }
  void surf(Input i,inout SurfaceOutput o){
   fixed3 paint=tex2D(_MainTex,i.uv_MainTex).rgb*_Color.rgb*i.color.rgb;
   // Preserve the graphic painted planes while still receiving scene shadows.
   o.Albedo=paint*.76;o.Emission=paint*.24;o.Gloss=_Smoothness;o.Alpha=1;
  }
  ENDCG
 }
 Fallback "Diffuse"
}
