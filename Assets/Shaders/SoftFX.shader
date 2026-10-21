Shader "Mist/SoftFX" {
 Properties {_Color("Tint",Color)=(1,1,1,.5)}
 SubShader { Tags {"Queue"="Transparent" "RenderType"="Transparent"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off Offset -1,-1
 Pass { CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 fixed4 _Color;struct app{float4 vertex:POSITION;fixed4 color:COLOR;};struct outp{float4 pos:SV_POSITION;fixed4 color:COLOR;};
 outp vert(app v){outp o;o.pos=UnityObjectToClipPos(v.vertex);o.color=v.color;return o;}
 fixed4 frag(outp i):SV_Target{return _Color*i.color;}
 ENDCG }
 }
}
