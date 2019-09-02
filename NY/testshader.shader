Shader "Custom/testshader" {
	Properties{
		//_Color ("Color", Color) = (1,1,1,1)
		_MainTex("Albedo (RGB)", 2D) = "white" {}
	//_Glossiness ("Smoothness", Range(0,1)) = 0.5
	//_Metallic ("Metallic", Range(0,1)) = 0.0
	//_Middle ("Middle", float4) = (0,0,0,0)
	_Amount("Amount", float) = 0.0
	}
		SubShader{
		Tags{ "RenderType" = "Opaque" }
		LOD 200
		//Cull Off

		CGPROGRAM
#pragma surface surf Lambert vertex:vert

		sampler2D _MainTex;
	//float3 _Middle;
	float _Amount = 0;

	struct Input {
		float2 uv_MainTex;
	};

	void vert(inout appdata_full v)
	{   //전체 vertex에 대한 코드      
		//자기자신의 위치를 vertex color에 저장된 것에 
		//if alpha <1 vertex color rgb 가 중심점임 중심에서 알파만큼의 거리로 설정
		//v.vertex.x = (v.r, v.g, v.b) + v.vertex.x 
		//float3 loc = v.vertex;
		//float3 tempmiddle = 0;

		//alpha값 어떻게 불러오는지 확인하기 색도
		//이런식으로 하면 되지않을까
		//if(true){
		//float3 tempvector = loc - tempmiddle;
		//v.vertex.x += tempvector.x * _Amount;
		//v.vertex.y += tempvector.y * _Amount;
		//v.vertex.z += tempvector.z * _Amount;
		if (v.color.a < 1) {
			float3 loc = v.vertex;
			loc.x -= v.color.r;
			loc.y -= v.color.g;
			loc.z -= v.color.b;
			//v.vertex.x = v.color.r + loc.x * ((pow(2.7, (v.color.a-0.5)*2)/pow(2.7, (v.color.a - 0.5)*2 + 1))*1/2 + 0.5);
			v.vertex.x = v.color.r + loc.x * ((1/(1+pow( 2.7 , -(v.color.a*12 - 6)*1/2 ) )) * 1 / 2 + 0.5);
			v.vertex.y = v.color.g + loc.y * ((1 / (1 + pow(2.7, -(v.color.a * 12 - 6) * 1 / 2))) * 1 / 2 + 0.5);
			v.vertex.z = v.color.b + loc.z * ((1 / (1 + pow(2.7, -(v.color.a * 12 - 6) * 1 / 2))) * 1 / 2 + 0.5);
		}
		//_Amount += 0.2;
		//}
	}

	void surf(Input IN, inout SurfaceOutput o) {
		half4 c = tex2D(_MainTex, IN.uv_MainTex);
		o.Albedo = tex2D(_MainTex, IN.uv_MainTex).rgb;
		o.Alpha = c.a;
	}
	ENDCG
	}
		FallBack "Diffuse"
}
