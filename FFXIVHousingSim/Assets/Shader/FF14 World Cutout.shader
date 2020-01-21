// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Silent/FF14 World Cutout"
{
	Properties
	{
		_Cutoff( "Mask Clip Value", Float ) = 0.5
		[NoScaleOffset]_Albedo0("Albedo 0", 2D) = "white" {}
		[NoScaleOffset][Normal]_NormalMap0("Normal Map 0", 2D) = "bump" {}
		[NoScaleOffset]_Metallic0("Metallic 0", 2D) = "black" {}
		_EmissionPow("Emission Pow", Range( 0 , 10)) = 0
		[Toggle(_)]_ApplyVertexColouring("Apply Vertex Colouring", Float) = 0
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "TransparentCutout"  "Queue" = "AlphaTest+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#pragma target 5.0
		#pragma multi_compile_instancing
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows 
		struct Input
		{
			float2 uv_texcoord;
			float4 vertexColor : COLOR;
		};

		uniform sampler2D _NormalMap0;
		uniform float _ApplyVertexColouring;
		uniform sampler2D _Albedo0;
		uniform sampler2D _Metallic0;
		uniform float _EmissionPow;
		uniform float _Cutoff = 0.5;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			float2 uv_NormalMap03 = i.uv_texcoord;
			o.Normal = UnpackNormal( tex2D( _NormalMap0, uv_NormalMap03 ) );
			float2 uv_Albedo02 = i.uv_texcoord;
			float4 tex2DNode2 = tex2D( _Albedo0, uv_Albedo02 );
			float4 AlbedoFinal71 = lerp(tex2DNode2,( tex2DNode2 + -0.5 + i.vertexColor ),_ApplyVertexColouring);
			o.Albedo = AlbedoFinal71.rgb;
			float2 uv_Metallic04 = i.uv_texcoord;
			float4 break64 = tex2D( _Metallic0, uv_Metallic04 );
			o.Emission = ( break64.a * AlbedoFinal71 * _EmissionPow ).rgb;
			o.Metallic = break64.b;
			o.Smoothness = break64;
			o.Alpha = 1;
			clip( tex2DNode2.a - _Cutoff );
		}

		ENDCG
	}
	Fallback "Diffuse"
	CustomEditor "ASEMaterialInspector"
}
/*ASEBEGIN
Version=16700
1311;92;2529;1614;1369.036;879.3705;1;True;False
Node;AmplifyShaderEditor.SamplerNode;2;-1033.894,-180.5342;Float;True;Property;_Albedo0;Albedo 0;1;1;[NoScaleOffset];Create;True;0;0;False;0;None;6fa8bb16eaede0a4c98f725a7fbfd23f;True;0;False;white;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RangedFloatNode;95;-417.7131,-63.52338;Float;False;Constant;_Float0;Float 0;8;0;Create;True;0;0;False;0;-0.5;0;0;0;0;1;FLOAT;0
Node;AmplifyShaderEditor.VertexColorNode;54;-455.6009,-237.7999;Float;False;0;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleAddOpNode;83;-160.9803,-235.6643;Float;False;3;3;0;COLOR;0,0,0,0;False;1;FLOAT;0;False;2;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.ToggleSwitchNode;97;-56.0365,-497.3705;Float;False;Property;_ApplyVertexColouring;Apply Vertex Colouring;5;0;Create;True;0;0;False;0;0;2;0;COLOR;0,0,0,0;False;1;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.SamplerNode;4;-936.0978,797.7661;Float;True;Property;_Metallic0;Metallic 0;3;1;[NoScaleOffset];Create;True;0;0;False;0;None;b19a473904436df45bb7744e0fcec074;True;0;False;black;Auto;False;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;COLOR;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.RegisterLocalVarNode;71;346.3309,-494.1566;Float;False;AlbedoFinal;-1;True;1;0;COLOR;0,0,0,0;False;1;COLOR;0
Node;AmplifyShaderEditor.RangedFloatNode;6;-533.3942,1125.066;Float;False;Property;_EmissionPow;Emission Pow;4;0;Create;True;0;0;False;0;0;0;0;10;0;1;FLOAT;0
Node;AmplifyShaderEditor.BreakToComponentsNode;64;-528.9376,820.2851;Float;False;COLOR;1;0;COLOR;0,0,0,0;False;16;FLOAT;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4;FLOAT;5;FLOAT;6;FLOAT;7;FLOAT;8;FLOAT;9;FLOAT;10;FLOAT;11;FLOAT;12;FLOAT;13;FLOAT;14;FLOAT;15
Node;AmplifyShaderEditor.GetLocalVarNode;73;-486.852,1019.953;Float;False;71;AlbedoFinal;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.GetLocalVarNode;72;-175.5316,-5.514511;Float;False;71;AlbedoFinal;1;0;OBJECT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.LerpOp;40;-403.0685,389.4814;Float;False;3;0;FLOAT;0.18;False;1;FLOAT;0.81;False;2;FLOAT;0;False;1;FLOAT;0
Node;AmplifyShaderEditor.SamplerNode;3;-1022.85,60.7894;Float;True;Property;_NormalMap0;Normal Map 0;2;2;[NoScaleOffset];[Normal];Create;True;0;0;False;0;None;f713dd6797bbb0e42a653bffc152159a;True;0;True;bump;Auto;True;Object;-1;Auto;Texture2D;6;0;SAMPLER2D;;False;1;FLOAT2;0,0;False;2;FLOAT;0;False;3;FLOAT2;0,0;False;4;FLOAT2;0,0;False;5;FLOAT;1;False;5;FLOAT3;0;FLOAT;1;FLOAT;2;FLOAT;3;FLOAT;4
Node;AmplifyShaderEditor.SimpleMultiplyOpNode;5;-105.1217,934.5651;Float;False;3;3;0;FLOAT;1;False;1;COLOR;0,0,0,0;False;2;FLOAT;0;False;1;COLOR;0
Node;AmplifyShaderEditor.StandardSurfaceOutputNode;0;74.60001,43.59999;Float;False;True;7;Float;ASEMaterialInspector;0;0;Standard;Silent/FF14 World Cutout;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;False;True;False;False;False;False;Back;0;False;-1;0;False;-1;False;0;False;-1;0;False;-1;False;0;Masked;0.5;True;True;0;False;TransparentCutout;;AlphaTest;All;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;True;0;False;-1;False;0;False;-1;255;False;-1;255;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;-1;False;2;15;10;25;False;0.5;True;0;0;False;-1;0;False;-1;0;0;False;-1;0;False;-1;0;False;-1;0;False;-1;0;False;0;0,0,0,0;VertexOffset;True;False;Cylindrical;False;Relative;0;;0;-1;-1;-1;0;False;0;0;False;-1;-1;0;False;-1;0;0;0;False;0.1;False;-1;0;False;-1;16;0;FLOAT3;0,0,0;False;1;FLOAT3;0,0,0;False;2;FLOAT3;0,0,0;False;3;FLOAT;0;False;4;FLOAT;0;False;5;FLOAT;0;False;6;FLOAT3;0,0,0;False;7;FLOAT3;0,0,0;False;8;FLOAT;0;False;9;FLOAT;0;False;10;FLOAT;0;False;13;FLOAT3;0,0,0;False;11;FLOAT3;0,0,0;False;12;FLOAT3;0,0,0;False;14;FLOAT4;0,0,0,0;False;15;FLOAT3;0,0,0;False;0
WireConnection;83;0;2;0
WireConnection;83;1;95;0
WireConnection;83;2;54;0
WireConnection;97;0;2;0
WireConnection;97;1;83;0
WireConnection;71;0;97;0
WireConnection;64;0;4;0
WireConnection;40;2;64;0
WireConnection;5;0;64;3
WireConnection;5;1;73;0
WireConnection;5;2;6;0
WireConnection;0;0;72;0
WireConnection;0;1;3;0
WireConnection;0;2;5;0
WireConnection;0;3;64;2
WireConnection;0;4;64;0
WireConnection;0;10;2;4
ASEEND*/
//CHKSM=6FA65E3BF36CA24EA75AEB406B90B9CF1FB62B5E