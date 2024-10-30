Ethan Schledewitz-Edwards 100908840  
Andrew Hoult 100866035  
Zachary O’Brien 100909491

# CG Assignment 1 Read Me

Youtube link: [https://youtu.be/WUwO7fqUvqs](https://youtu.be/Hlf02GrOUnM)   
Github link: [https://github.com/mrrobottrax/ShaderAssignment1](https://github.com/mrrobottrax/ShaderAssignment1)  
Slideshow:[https://docs.google.com/presentation/d/1y1T7qb2CbIzT4xLl4gqmGeddgcwzwbdhPEF7CduINS4/edit?usp=sharing](https://docs.google.com/presentation/d/1y1T7qb2CbIzT4xLl4gqmGeddgcwzwbdhPEF7CduINS4/edit?usp=sharing) 

# Base

This project is our GDW game. Our game is a multiplayer, first-person survival experience where players are prospectors who must plunder mines and caves to reach a profit quota for their company. This quota must be met within a specified number of days. If the quota is not met, players will be fired. However, if they meet the quota, they will begin a new cycle and head to a new mine.

Gameplay will work in cycles, with each consecutive cycle having a slightly higher profit quota than the last. This requires the team to budget their money to buy supplies that will keep them going.

Graphically, our game is heavily inspired by early 3D games such as Half life, Quake, Morrowind, and Silent Hill.

Note: Our multiplayer implementation requires that the user has Steam installed and running in order to work properly, whether in the Unity editor or a built version of the project.

# Illumination

## **No lighting**

Overview: This shader is an unlit shader that simply renders a texture to a meshes surface without any lighting. In this project we will mainly be using this on distant objects that act as billboards.

Properties

* \_MainTex: The main 2D texture applied to the object.

SubShader Settings

* RenderPipeline: Universal Render Pipeline  
* RenderType: Opaque, meaning no transparency.

Shader Structure

1) Attributes  
   * positionOS: Object space position (POSITION).  
   * uv: Texture coordinates (TEXCOORD0).  
2) Varyings  
   * positionHCS: Homogeneous clip-space position for rendering (SV\_POSITION).  
   * uv: Pass-through texture coordinates for fragment shader.  
3) Vertex Function (vert)  
   * Transforms object position to clip space using TransformObjectToHClip.  
   * Passes UV coordinates to the fragment shader.  
4) Fragment Function (frag)  
   * Samples the main texture using UV coordinates.  
   * Outputs the sampled color for display.

## **Ambient**

Overview: This shader applies both ambient and directional lighting to a texture, using Lambertian diffuse shading and spherical harmonics for ambient light. In this project, we intend to use ambient shaders on everything since we don't want many of the standard Unity features like specular highlights and normal maps.

Properties

* \_MainTex: The main 2D texture applied to the object.  
* \_BaseColor: Base color applied to the texture.

SubShader Settings

* RenderPipeline: Universal Render Pipeline (URP)  
* RenderType: Opaque, meaning no transparency.

Shader Structure

1) Attributes  
   * positionOS: Object space position (POSITION).  
   * normalOS:  Object normal in object space (NORMAL)  
   * uv: Texture coordinates (TEXCOORD0).  
2) Varyings  
   * positionHCS: Homogeneous clip-space position for rendering (SV\_POSITION).  
   * uv: Pass-through texture coordinates for fragment shader.  
3) Vertex Function (vert)  
   * Transforms object position to clip space using TransformObjectToHClip.  
   * Passes UV coordinates to the fragment shader.  
4) Fragment Function (frag)  
   * Samples the main texture using UV coordinates.  
   * Adds an ambient factor to the sampled colour.  
   * Outputs the sampled colour for display.

## **Specular**

Overview: This shader applies specular directional lighting to a texture, using Blinn-Phong shading. We don’t intend to use specular lighting for many objects in this project in order to maintain a retro art style. But, we may use it for certain objects in order to make them stand out and catch the player’s attention.

Properties

* \_MainTex: The main 2D texture applied to the object.  
* \_BaseColor: Base colour applied to the texture.  
* \_SpecColor: The colour of the specular highlights.  
* \_Shininess: The sharpness of the specular highlight.

SubShader Settings

* RenderPipeline: Universal Render Pipeline (URP)  
* RenderType: Opaque, meaning no transparency.

Shader Structure

5) Attributes  
   * positionOS: Object space position (POSITION).  
   * normalOS:  Object normal in object space (NORMAL)  
   * uv: Texture coordinates (TEXCOORD0).  
6) Varyings  
   * positionHCS: Homogeneous clip-space position for rendering (SV\_POSITION).  
   * uv: Pass-through texture coordinates for fragment shader.  
   * viewDirWS: World space direction to the player camera.  
   * normalWS: World space normal direction.  
7) Vertex Function (vert)  
   * Transforms object position to clip space using TransformObjectToHClip.  
   * Passes UV coordinates to the fragment shader.  
   * Calculates direction to the camera.  
   * Calculates world space normal.  
8) Fragment Function (frag)  
   * Samples the main texture using UV coordinates.  
   * Outputs the sampled color for display.  
   * Calculates the specular factor using Blinn-Phong.  
   * Blends to the specular colour.

## **Ambient \+ Specular**

Overview: This shader applies specular directional lighting to a texture using Blinn-Phong shading, as well as an ambient factor. We don’t intend to use specular lighting for many objects in this project in order to maintain a retro art style. But, we may use it for certain objects in order to make them stand out and catch the player’s attention. We would use this shader over pure specular because it looks much better.

Properties

* \_MainTex: The main 2D texture applied to the object.  
* \_BaseColor: Base colour applied to the texture.  
* \_SpecColor: The colour of the specular highlights.  
* \_Shininess: The sharpness of the specular highlight.

SubShader Settings

* RenderPipeline: Universal Render Pipeline (URP)  
* RenderType: Opaque, meaning no transparency.

Shader Structure

9) Attributes  
   * positionOS: Object space position (POSITION).  
   * normalOS:  Object normal in object space (NORMAL)  
   * uv: Texture coordinates (TEXCOORD0).  
10) Varyings  
    * positionHCS: Homogeneous clip-space position for rendering (SV\_POSITION).  
    * uv: Pass-through texture coordinates for fragment shader.  
    * viewDirWS: World space direction to the player camera.  
    * normalWS: World space normal direction.  
11) Vertex Function (vert)  
    * Transforms object position to clip space using TransformObjectToHClip.  
    * Passes UV coordinates to the fragment shader.  
    * Calculates direction to the camera.  
    * Calculates world space normal.  
12) Fragment Function (frag)  
    * Samples the main texture using UV coordinates.  
    * Outputs the sampled color for display.  
    * Calculates the specular factor using Blinn-Phong.  
    * Blends to the specular colour.  
    * Adds an ambient factor.

# Colour Grading

Assets/GFX/Shaders/LUTPost.shader

This shader matches each RGB colour value to a value from a lookup table.

It applies this to the entire screen as a post processing effect.

You can blend between two LUTs by editing the material properties, or turn down the contribution:

LUT Height: The edge size of the LUT.  
LUT0: The LUT to blend from.  
LUT1: The LUT to blend to.  
Blend: The blend factor between the two LUTS.  
Contribution: The intensity of the effect.

# Stencil Shadows

Assets/GFX/BasicLit.shader

Renders shadows using procedural meshes and the stencil buffer.

**THIS SHADER USES A CUSTOM RENDER PIPELINE AND COULDN’T BE MERGED WITH URP SHADERS IN TIME. IT’S ON THE BRANCH “dev/renderpipeline”.**  

Shadow volumes are generated in real time using the main directional light and a geometry shader.

After a depth prepass, the shadow meshes are rendered with the depth test set to greater/equal. Backfaces increment the stencil buffer, and frontfaces decrement the stencil buffer. After rendering, the stencil buffer will have 0 on all pixels in shadow, and 1 on all lit pixels. The scene is rendered again with lighting only where the stencil buffer equals 0\.

##   **Shaders**

**Ghost Shader**  
There is a horizontal and vertical scroll you can control that moves noise like in candle shader. These values are multiplied together and fed into the IN for a step function with the y values of the object fed into the EDGE. This is then used as the transparency. Colour, emissions and a fresnel effect are applied on top of this.

**Flame Shader**  
The flickering and waving of the flame is accomplished by using a time node and inserting into the y of a vector 2\. By putting the offset into a tiling and offset node that is connected to the UVs of a gradient noise node you get an effect of the noise scrolling upwards. By lerping this with the UVs of the object you can apply this effect to the objects UV. I used a gradient imputed into the T value of the lerp node so that the effect was applied more to the top of the flame. These UVs were placed into a sample texture node. This applies the effect to the desired texture. This texture is then fed onto the alpha channel of the shader to cut out the desired shape. The texture is then blended with a colour gradient to match the look of a candle. This result is then applied to colour and elision channels

**Stencil Shader**  
It sets the stencil buffer to one for each pixel that is being rendered
