Shader "Mist/Backdrop" {
 Properties{_MainTex("Distant matte",2D)="white"{}}
 SubShader{Tags{"Queue"="Background"}Cull Off ZWrite Off
 Pass{CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex;float4 _MainTex_ST;
 struct appdata{float4 vertex:POSITION;float2 uv:TEXCOORD0;};struct v2f{float4 position:SV_POSITION;float2 uv:TEXCOORD0;};
 v2f vert(appdata v){v2f o;o.position=UnityObjectToClipPos(v.vertex);o.uv=TRANSFORM_TEX(v.uv,_MainTex);return o;}
 fixed4 frag(v2f i):SV_Target{float3 c=tex2D(_MainTex,i.uv).rgb;float gray=dot(c,float3(.299,.587,.114));c=lerp(gray.xxx,c,.6);return float4(lerp(float3(.12,.2,.235),c*.82,.85),1);}
 ENDCG}
 }
}
