// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'
// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

//
// Relief Terrain - 2 Parallax mapped materials with water
// Tomasz Stobierski 2014
//
//  material blend vertex color R
// (water mask taken from vertex color B)
//
Shader "Relief Pack/WaterShader_2VertexPaint U5-PBL" {
    Properties {
		_Color("Color A", Color) = (0.5,0.5,0.5,1)	
		_MainTex("Albedo A", 2D) = "white" {}
		_Glossiness("Smoothness A", Range(0.0, 1.0)) = 0.0
		_SpecColor("Specular color A", Color) = (0.2,0.2,0.2)	
		_SpecGlossMap("Specular (RGB)+Gloss(A)", 2D) = "white" {}
		_BumpMap("Normal Map A", 2D) = "bump" {}
		_HeightMap ("Heightmap A", 2D) = "black" {}
		
		_Color2("Color B", Color) = (0.5,0.5,0.5,1)	
		_MainTex2("Albedo B", 2D) = "white" {}
		_Glossiness2("Smoothness B", Range(0.0, 1.0)) = 0.0
		_SpecColor2("Specular color B", Color) = (0.2,0.2,0.2)	
		_SpecGlossMap2("Specular (RGB)+Gloss(A)", 2D) = "white" {}
		_BumpMap2("Normal Map B", 2D) = "bump" {}
		_HeightMap2 ("Heightmap B", 2D) = "black" {}		

		TERRAIN_ExtrudeHeight ("Extrude Height", Range(0.001,0.08)) = 0.02 

		TERRAIN_FlowingMap ("Flowingmap (water bumps)", 2D) = "gray" {}
		TERRAIN_RippleMap ("Ripplemap (droplets)", 2D) = "gray" {}
		TERRAIN_RippleScale ("Ripple scale", Float) = 1

		TERRAIN_LayerWetStrength ("Layer wetness", Range(0,1)) = 1
		TERRAIN_WaterLevelSlopeDamp ("Water level slope damp", Range(0.25,8)) = 4
		TERRAIN_WaterLevel ("Water Level", Range(0,2)) = 0.5
		TERRAIN_WaterColor ("Water Color", Color) = (1,1,1,1)
      	TERRAIN_WaterSpecGloss ("Water SpecColor(RGB)+Gloss(A)", Color) = (0.25,0.25,0.25,0.8)
		TERRAIN_WaterEdge ("Water Edge", Range(1, 4)) = 1
		TERRAIN_WaterOpacity ("Water Opacity", Range(0,1)) =0.5
		TERRAIN_DropletsSpeed ("Droplets speed", Float) = 15
		TERRAIN_RainIntensity ("Rain intensity", Range(0,1)) = 1
		TERRAIN_WetDropletsStrength ("Rain on wet", Range(0,1)) = 0
		TERRAIN_Refraction("Refraction", Range(0,0.04)) = 0.02
		TERRAIN_WetRefraction ("Wet refraction", Range(0,1)) = 1
		TERRAIN_Flow ("Flow", Range(0, 1)) = 0.1
		TERRAIN_FlowScale ("Flow Scale", Float) = 1
		TERRAIN_FlowSpeed ("Flow Speed", Range(0, 3)) = 0.25
		TERRAIN_WetGloss ("Wet gloss", Range(0, 1)) = 0.1
    }
    
 	CGINCLUDE
		#define _GLOSSYENV 1
		#define UNITY_SETUP_BRDF_INPUT SpecularSetup
	ENDCG
	    
    SubShader {
	Tags { "RenderType" = "Opaque" }
	CGPROGRAM
	#pragma surface surf StandardSpecular vertex:vert
	#pragma only_renderers d3d9 opengl d3d11
	#pragma glsl
	#pragma target 3.0
	#include "UnityPBSLighting.cginc"
	#pragma multi_compile RTP_PM_SHADING RTP_SIMPLE_SHADING
	
	#define RTP_REFLECTION
	#define RTP_ROTATE_REFLECTION
	
	#define RTP_WETNESS
	// enabled below if you don't want to use water flow
	//#define SIMPLE_WATER
	#define RTP_WET_RIPPLE_TEXTURE	  
	
	struct Input {
	float2 uv_MainTex;
	
	float3 viewDir;
	float4 _auxDir;
	
	float4 color:COLOR;
	};
	
	// TTT
	half4 _Color;
	half _Glossiness;
	sampler2D _SpecGlossMap;
	half4 _Color2;
	half4 _SpecColor2;
	half _Glossiness2;
	sampler2D _SpecGlossMap2;
	
	half4 TERRAIN_WaterSpecGloss;
	float TERRAIN_WetGloss;
	////
		
	float TERRAIN_LayerWetStrength;
	float TERRAIN_WaterLevelSlopeDamp;
	float TERRAIN_ExtrudeHeight;
	
	float _Shininess;
	
	sampler2D _MainTex;
	sampler2D _BumpMap;
	sampler2D _HeightMap;
	
	sampler2D _MainTex2;
	sampler2D _BumpMap2;
	sampler2D _HeightMap2;
	
	sampler2D TERRAIN_RippleMap;
	sampler2D TERRAIN_FlowingMap;
	
	sampler2D _ReflectionMap;
	half4 TERRAIN_ReflColorA;
	half4 TERRAIN_ReflColorB;
	float TERRAIN_ReflDistortion;
	float TERRAIN_ReflectionRotSpeed;
	
	float TERRAIN_DropletsSpeed;
	float TERRAIN_RainIntensity;
	float TERRAIN_WetDropletsStrength;
	float TERRAIN_Refraction;
	float TERRAIN_WetRefraction;
	float TERRAIN_Flow;
	float TERRAIN_FlowScale;
	float TERRAIN_RippleScale;
	float TERRAIN_FlowSpeed;
	float TERRAIN_FlowSpeedMap;
//	float TERRAIN_WetSpecularity;
//	float TERRAIN_WetReflection;
//	float TERRAIN_LayerReflection; // niezalezne od wody i mokrości
	float TERRAIN_WaterLevel;
	half4 TERRAIN_WaterColor;
	float TERRAIN_WaterEdge;
	float TERRAIN_WaterSpecularity;
//	float TERRAIN_WaterGloss;
	float TERRAIN_WaterOpacity;
	float TERRAIN_FresnelPow;
	float TERRAIN_FresnelOffset;
      
	inline float2 GetRipple(float2 UV, float Intensity)
	{
	    float4 Ripple = tex2D(TERRAIN_RippleMap, UV);
	    Ripple.xy = Ripple.xy * 2 - 1;
	
	    float DropFrac = frac(Ripple.w + _Time.x*TERRAIN_DropletsSpeed);
	    float TimeFrac = DropFrac - 1.0f + Ripple.z;
	    float DropFactor = saturate(0.2f + Intensity * 0.8f - DropFrac);
	    float FinalFactor = DropFactor * Ripple.z * sin( clamp(TimeFrac * 9.0f, 0.0f, 3.0f) * 3.1415);
	    
	    return Ripple.xy * FinalFactor * 0.35f;
	}
	
	void vert (inout appdata_full v, out Input o) {
	    #if defined(SHADER_API_D3D11) || defined(SHADER_API_D3D11_9X) || defined(UNITY_PI)
			UNITY_INITIALIZE_OUTPUT(Input, o);
		#endif
		#if defined(RTP_REFLECTION) || defined(RTP_WETNESS)
			float3 binormal = cross( v.normal, v.tangent.xyz ) * v.tangent.w;
			float3x3 rotation = float3x3( v.tangent.xyz, binormal, v.normal.xyz );		
			
			float3 viewDir = -ObjSpaceViewDir(v.vertex);
			float3 viewRefl = reflect (viewDir, v.normal);
			float2 refl_vec = normalize(mul((float3x3)unity_ObjectToWorld, viewRefl)).xz;
			#ifdef RTP_ROTATE_REFLECTION
				float3 refl_rot;
				refl_rot.x=sin(_Time.x*TERRAIN_ReflectionRotSpeed);
				refl_rot.y=cos(_Time.x*TERRAIN_ReflectionRotSpeed);
				refl_rot.z=-refl_rot.x;
				o._auxDir.x=dot(refl_vec, refl_rot.yz);
				o._auxDir.y=dot(refl_vec, refl_rot.xy);
			#else
				o._auxDir.xy=refl_vec;
			#endif
			o._auxDir.xy=o._auxDir.xy*0.5+0.5;
		#endif
		#if defined(RTP_WETNESS)
		o._auxDir.zw = ( mul (rotation, mul(unity_WorldToObject, float4(0,1,0,0)).xyz) ).xy;		
		#endif
	}
	
	void surf (Input IN, inout SurfaceOutputStandardSpecular o) {
      	float2 tH;
      	tH.x=tex2D(_HeightMap, IN.uv_MainTex).a;
      	tH.y=tex2D(_HeightMap2, IN.uv_MainTex).a;
      	float2 uv=IN.uv_MainTex + ParallaxOffset(tH.x, TERRAIN_ExtrudeHeight, IN.viewDir.xyz);
      	float2 uv2=IN.uv_MainTex + ParallaxOffset(tH.y, TERRAIN_ExtrudeHeight, IN.viewDir.xyz);
      	float2 control=float2(IN.color.r, 1-IN.color.r);
      	control*=(tH+0.01);
      	control*=control;
      	control*=control;
      	control*=control;
      	control/=dot(control, 1);
      		
      	float3 rayPos;
      	rayPos.z=dot(control, tH);
      	#if defined(RTP_SIMPLE_SHADING)
      	rayPos.xy=IN.uv_MainTex;
      	#else
      	rayPos.xy=lerp(uv, uv2, control.y);
      	#endif
      	
		float3 flat_dir;
		flat_dir.xy=IN._auxDir.zw;
		flat_dir.z=sqrt(1 - saturate(dot(flat_dir.xy, flat_dir.xy)));
		float wetSlope=1-flat_dir.z;

		float perlinmask=tex2D(TERRAIN_FlowingMap, IN.uv_MainTex/8).a;
		TERRAIN_LayerWetStrength*=saturate(IN.color.b*2 - perlinmask*(1-TERRAIN_LayerWetStrength)*2);
		float2 roff=0;
		wetSlope=saturate(wetSlope*TERRAIN_WaterLevelSlopeDamp);
		float _RippleDamp=saturate(TERRAIN_LayerWetStrength*2-1)*saturate(1-wetSlope*4);
		TERRAIN_RainIntensity*=_RippleDamp;
		TERRAIN_LayerWetStrength=saturate(TERRAIN_LayerWetStrength*2);
		TERRAIN_WaterLevel=clamp(TERRAIN_WaterLevel + ((TERRAIN_LayerWetStrength - 1) - wetSlope)*2, 0, 2);
		TERRAIN_LayerWetStrength=saturate(TERRAIN_LayerWetStrength - (1-TERRAIN_LayerWetStrength)*rayPos.z);
		TERRAIN_Flow*=TERRAIN_LayerWetStrength*TERRAIN_LayerWetStrength;
		
		float p = saturate((TERRAIN_WaterLevel-rayPos.z)*TERRAIN_WaterEdge);
		p*=p;
		#if !defined(RTP_SIMPLE_SHADING) && !defined(SIMPLE_WATER)
			float2 flowUV=lerp(IN.uv_MainTex, rayPos.xy, 1-p*0.5)*TERRAIN_FlowScale;
			float _Tim=frac(_Time.x*4)*2;
			float ft=abs(frac(_Tim)*2 - 1);
			float2 flowSpeed=clamp((IN._auxDir.zw+0.01)*4,-1,1)/4;
			flowSpeed*=TERRAIN_FlowSpeed*TERRAIN_FlowScale;
			float2 flowOffset=tex2D(TERRAIN_FlowingMap, flowUV+frac(_Tim.xx)*flowSpeed).ag*2-1;
			flowOffset=lerp(flowOffset, tex2D(TERRAIN_FlowingMap, flowUV+frac(_Tim.xx+0.5)*flowSpeed*1.1).ag*2-1, ft);
			flowOffset*=TERRAIN_Flow*p*TERRAIN_LayerWetStrength;
		#else
			float2 flowOffset=0;
			float2 flowSpeed=0;
		#endif
		
		#if defined(RTP_WET_RIPPLE_TEXTURE) && !defined(RTP_SIMPLE_SHADING)
			float2 rippleUV = IN.uv_MainTex*TERRAIN_RippleScale + flowOffset*0.1*flowSpeed/TERRAIN_FlowScale;
		  	roff = GetRipple( rippleUV, TERRAIN_RainIntensity);
			roff += GetRipple( rippleUV+float2(0.25,0.25), TERRAIN_RainIntensity);
		  	roff*=4*_RippleDamp*lerp(TERRAIN_WetDropletsStrength, 1, p);
		  	roff+=flowOffset;
		#else
			roff = flowOffset;
		#endif
		
		#if !defined(RTP_SIMPLE_SHADING)
			rayPos.xy+=TERRAIN_Refraction*roff*max(p, TERRAIN_WetRefraction);
		#endif
		
      	fixed4 col = control.x*tex2D(_MainTex, rayPos.xy)*_Color*2 + control.y*tex2D(_MainTex2, rayPos.xy)*_Color2*2;
      	
		half4 specGloss = control.x*tex2D (_SpecGlossMap, rayPos.xy)*half4(_SpecColor.rgb, _Glossiness) + control.y*tex2D (_SpecGlossMap2, rayPos.xy)*half4(_SpecColor2.rgb, _Glossiness2);
        o.Specular = specGloss.rgb;
        o.Smoothness = specGloss.a;
        
        o.Specular = lerp(o.Specular, max(o.Specular, TERRAIN_WaterSpecGloss.rgb), p);
        o.Smoothness = lerp(lerp(_Glossiness, TERRAIN_WetGloss, TERRAIN_LayerWetStrength), TERRAIN_WaterSpecGloss.a, p);
        float _WaterOpacity=TERRAIN_WaterOpacity*p;

        col.rgb = lerp(col.rgb, TERRAIN_WaterColor.rgb, _WaterOpacity );
        
        o.Normal = lerp(UnpackNormal ( tex2D(_BumpMap, rayPos.xy)*control.x + tex2D (_BumpMap2, rayPos.xy)*control.y ), float3(0,0,1), p);
        o.Normal.xy+=roff;
        o.Normal=normalize(o.Normal);
  		
		col.rgb*=1-saturate(TERRAIN_LayerWetStrength*2)*0.3;
                
        o.Albedo = col.rgb;
	}
	ENDCG
      
    } 
    Fallback "Diffuse"
}
