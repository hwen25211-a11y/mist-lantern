Shader "Mist/Ink" {
 Properties { _Color("Pigment",Color)=(1,1,1,1) _EmissionColor("Lantern emission",Color)=(0,0,0,0) _Outline("Ink",Color)=(.025,.035,.045,1) _OutlineWidth("Ink width",Float)=.012 }
 SubShader {
 Tags { "RenderType"="Opaque" }
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
 #pragma surface surf Ink fullforwardshadows addshadow vertex:vertColor
 #pragma target 3.0
 fixed4 _Color,_EmissionColor;
 struct Input {float3 worldPos;float3 viewDir;float4 color:COLOR;};
 void vertColor(inout appdata_full v,out Input o){UNITY_INITIALIZE_OUTPUT(Input,o);o.color=v.color;}
 half4 LightingInk(SurfaceOutput s,half3 lightDir,half atten){half n=dot(s.Normal,lightDir);half band=smoothstep(-.12,.2,n)*.55+smoothstep(.55,.8,n)*.2+.25;return half4(s.Albedo*_LightColor0.rgb*band*atten,s.Alpha);}
 void surf(Input i,inout SurfaceOutput o){float grain=frac(sin(dot(floor(i.worldPos*85),float3(12.9898,78.233,37.41)))*43758.5453);o.Albedo=_Color.rgb*i.color.rgb*(.92+grain*.08);o.Emission=_EmissionColor.rgb*.24;o.Alpha=1;}
 ENDCG
 }
 Fallback "Diffuse"
}
