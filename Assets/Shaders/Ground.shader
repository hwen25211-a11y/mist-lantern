Shader "Mist/Ground" {
 Properties {_MainTex("Painted atlas",2D)="white"{} _Area("Region",Float)=0}
 SubShader { Tags {"RenderType"="Opaque"}
 CGPROGRAM
 #pragma surface surf Lambert fullforwardshadows addshadow
 #pragma target 3.0
 sampler2D _MainTex;float _Area;struct Input {float3 worldPos;};
 void surf(Input i,inout SurfaceOutput o){float2 uv=frac(i.worldPos.xz/8);uv.x=(uv.x*.998+.001+_Area)/3;o.Albedo=tex2D(_MainTex,uv).rgb;o.Alpha=1;}
 ENDCG
 } Fallback "Diffuse"
}
