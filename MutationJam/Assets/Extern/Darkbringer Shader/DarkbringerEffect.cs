using UnityEngine;
using System.Collections;
using UnityEditor;

namespace Picturesque.Darkbringer
{
    [ExecuteInEditMode]
    public class DarkbringerEffect : MonoBehaviour
    {
        //  [HideInInspector]
        public Shader sha;
        //[HideInInspector]
        public Material mat;
        public Texture2D flatLut;
        public Texture2D bayerTexture;
        //[Range(0.1f, 1.5f)]
        public float ColorBleed = 0.25f;
        //[Range(0.0f, 1.5f)]
        public float LinesMult = 0.8f;
        public float ColorShift = 0.29f;
        public Vector2 newScreenSize = new Vector2(160, 200);
        public Vector2 screenStretching = new Vector2(2, 1);

        public bool CustomScreenSize = true;
        public bool AspectRatioBars = true;

        public bool OneBitColor = false;

        public bool VerticalLines = false;
        public bool HorizontalLines = false;
        public bool Dithering = true;
        public bool Paletting = true;

        /// <summary>
        /// Gesamtintensität des Darkbringer-Effekts.
        /// 0 = vollständig Original-Bild, 1 = vollständiger Shader-Effekt.
        /// </summary>
        [Range(0f, 1f)]
        public float Intensity = 1f;

        private Texture3D vTex;
        private Texture2D v2DTex;
        public bool clearNextUpdate;

        // Blend-Material (einfacher Lerp zwischen zwei Texturen)
        private Material blendMat;
        private Shader blendShader;

        void Awake()
        {
            if (sha != null)
            {
                mat = new Material(sha);
                mat.SetTexture("_BayerTex", bayerTexture);
            }

            // Internes Blend-Material erstellen
            CreateBlendMaterial();
        }

        void OnDisable()
        {
            if (mat)
            {
                DestroyImmediate(mat);
                mat = null;
            }
            if (blendMat)
            {
                DestroyImmediate(blendMat);
                blendMat = null;
            }
        }

        // Erstellt ein einfaches Material zum Überblenden zweier Texturen
        private void CreateBlendMaterial()
        {
            // Wir nutzen den eingebauten Unity-Blit-Shader mit Alpha-Steuerung
            // als Fallback verwenden wir einen eigenen Inline-Shader
            string blendShaderSource = @"
                Shader ""Hidden/DarkbringerBlend""
                {
                    Properties
                    {
                        _MainTex (""Texture"", 2D) = ""white"" {}
                        _OverlayTex (""Overlay"", 2D) = ""white"" {}
                        _Blend (""Blend"", Float) = 1.0
                    }
                    SubShader
                    {
                        Pass
                        {
                            ZTest Always Cull Off ZWrite Off
                            CGPROGRAM
                            #pragma vertex vert_img
                            #pragma fragment frag
                            #include ""UnityCG.cginc""
                            sampler2D _MainTex;
                            sampler2D _OverlayTex;
                            float _Blend;
                            fixed4 frag(v2f_img i) : SV_Target
                            {
                                fixed4 original = tex2D(_MainTex, i.uv);
                                fixed4 processed = tex2D(_OverlayTex, i.uv);
                                return lerp(original, processed, _Blend);
                            }
                            ENDCG
                        }
                    }
                }
            ";

            blendShader = ShaderUtil.CreateShaderAsset(blendShaderSource);
            if (blendShader != null)
            {
                blendMat = new Material(blendShader);
            }
        }

        // Called by the camera to apply the image effect
        public void OnRenderImage(RenderTexture source, RenderTexture destination)
        {
            if (sha == null || flatLut == null)
            {
                Graphics.Blit(source, destination);
                return;
            }

            // Bei Intensity = 0 direkt das Original durchreichen
            if (Intensity <= 0f)
            {
                Graphics.Blit(source, destination);
                return;
            }

            if (mat == null)
            {
                mat = new Material(sha);
            }

            if (clearNextUpdate || v2DTex == null || mat.GetTexture("_Lut2D") == null || ((mat.GetTexture("_Lut2D") != null) && flatLut.name != mat.GetTexture("_Lut2D").name))
            {
                if (v2DTex != null || clearNextUpdate)
                {
                    DestroyImmediate(v2DTex);
                    v2DTex = null;
                }

                int dim = flatLut.height;
                v2DTex = new Texture2D(dim * dim, dim * dim, TextureFormat.ARGB32, false);
                v2DTex.filterMode = FilterMode.Point;
                v2DTex.name = flatLut.name;

                Color[] c = flatLut.GetPixels();
                Color[] newC = new Color[dim * dim * dim * dim];

                for (int i = 0; i < dim; i++)
                {
                    for (int j = 0; j < dim; j++)
                    {
                        for (int x = 0; x < dim; x++)
                        {
                            for (int y = 0; y < dim; y++)
                            {
                                float b = (i + j * dim * 1.0f) / dim;
                                int bi0 = Mathf.FloorToInt(b);
                                int bi1 = Mathf.Min(bi0 + 1, dim - 1);
                                float f = b - bi0;

                                int index = x + (dim - y - 1) * dim * dim;
                                // perform filtering of B channel in code
                                Color col1 = c[index + bi0 * dim];
                                Color col2 = c[index + bi1 * dim];

                                newC[x + i * dim + y * dim * dim + j * dim * dim * dim] = (f < 0.5f) ? col1 : col2;
                            }
                        }
                    }
                }

                v2DTex.wrapMode = TextureWrapMode.Clamp;
                v2DTex.SetPixels(newC);
                v2DTex.Apply();
                mat.SetTexture("_Lut2D", v2DTex);
            }

            float lutSquare = 16;

            mat.SetFloat("_ScaleRG", 0.05859375f);
            mat.SetFloat("_Dim", lutSquare);
            mat.SetFloat("_Offset", 0.001953125f);

            mat.SetTexture("_Lut2d", v2DTex);
            mat.SetFloat("_HORLINES", (HorizontalLines) ? 1 : 0);
            mat.SetFloat("_VERTLINES", (VerticalLines) ? 1 : 0);
            mat.SetFloat("_BIT1", (OneBitColor) ? 1 : 0);
            mat.SetFloat("_DITHER", (Dithering) ? 1 : 0);
            mat.SetFloat("_PALETTE", (Paletting) ? 1 : 0);

            mat.SetFloat("_ARC", (AspectRatioBars) ? 1 : 0);
            mat.SetFloat("_CSIZE", (CustomScreenSize) ? 1 : 0);
            mat.SetFloat("_LinesMult", LinesMult);
            mat.SetFloat("_ColorBleed", ColorBleed);
            mat.SetFloat("_ColorShift", ColorShift);
            mat.SetVector("_ScreenSize", new Vector4(newScreenSize.x, newScreenSize.y, 0, 0));
            mat.SetVector("_Stretching", new Vector4(screenStretching.x, screenStretching.y, 0, 0));

            // Bei Intensity = 1 direkt rendern (kein Blend-Overhead)
            if (Intensity >= 1f)
            {
                Graphics.Blit(source, destination, mat);
                return;
            }

            // Zwischenpuffer: Shader-Ergebnis in temporäre Textur rendern
            RenderTexture temp = RenderTexture.GetTemporary(source.descriptor);
            Graphics.Blit(source, temp, mat);

            // Blend: Original (source) + Shader-Output (temp) → destination
            if (blendMat != null)
            {
                blendMat.SetTexture("_MainTex", source);
                blendMat.SetTexture("_OverlayTex", temp);
                blendMat.SetFloat("_Blend", Intensity);
                Graphics.Blit(source, destination, blendMat);
            }
            else
            {
                // Fallback falls Blend-Shader nicht kompiliert werden konnte
                Graphics.Blit(temp, destination);
            }

            RenderTexture.ReleaseTemporary(temp);
        }
    }
    /* public enum EffectType
     {
         None,
         OneBit,
         DitheringOnly,
         Full
     }*/
}