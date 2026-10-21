Shader "Mist/Hair" {
 Properties { _MainTex("Strand pigment and coverage",2D)="white"{} _Color("Tint",Color)=(1,1,1,1) _Cutoff("Coverage threshold",Range(0,1))=.38 }
 SubShader {
  Tags {"Queue"="AlphaTest" "RenderType"="TransparentCutout"}
  Cull Off
  CGPROGRAM
  #pragma surface surf Lambert alphatest:_Cutoff addshadow fullforwardshadows vertex:vert
  #pragma target 3.0
  sampler2D _MainTex;fixed4 _Color;
  struct Input{float2 uv_MainTex;float4 color:COLOR;};
  void vert(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.color=v.color;}
  void surf(Input i,inout SurfaceOutput o){fixed4 t=tex2D(_MainTex,i.uv_MainTex);o.Albedo=t.rgb*_Color.rgb*i.color.rgb;o.Alpha=t.a;o.Emission=o.Albedo*.08;}
  ENDCG
 }
 Fallback "Transparent/Cutout/Diffuse"
}
