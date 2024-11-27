#pragma multi_compile NO_LIGHTING DIFFUSE_ONLY SPECULAR_ONLY DIFFUSE_SPECULAR

#if SPECULAR_ONLY || DIFFUSE_SPECULAR
#define SPECULAR
#endif

#if DIFFUSE_ONLY || DIFFUSE_SPECULAR
#define DIFFUSE
#endif

#include_with_pragmas "Phong.hlsl"