Shader "CustomRP/Unlit"
{
	Properties
	{
		[Toggle] _TRIPLANAR ("Triplanar", Float) = 1
		[Toggle] _ROTATE_90 ("Rotate triplanar", Float) = 1
		_MainTex ("Main Texture", 2D) = "white" {}
		_Color ("Color", Color) = (1,1,1,1)
	}

	SubShader
	{
		Tags
		{
			"Queue" = "Transparent"
		}

		Pass
		{
			Tags
			{ 
				"LightMode" = "ForwardBase"
			}

			HLSLPROGRAM
			
			#include_with_pragmas "Assets/Code/CustomRP/Shaders/Include/StandardVertex.hlsl"
			#include_with_pragmas "Assets/Code/CustomRP/Shaders/Include/StandardFragment.hlsl"

			ENDHLSL
		}
	}
}