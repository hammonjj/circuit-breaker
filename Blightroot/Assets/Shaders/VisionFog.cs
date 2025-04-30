using UnityEngine;

public class VisionFog : MonoBehaviour {
    [Header("Scene Blur")]
  public Material   blurMat;       // your Gaussian blur
  [Header("Composite")]
  public Material   compositeMat;  // FogOfWar_Mat
  public Transform  player;
  [Range(0,1)] public float      radius  = 0.3f;
  [Range(0,0.5f)]public float     feather = 0.05f;
  RenderTexture    tmp;

  
    
    [Header("Mask Source")]
    public VisionFogController fogController;

    RenderTexture blurRT;

  void OnRenderImage(RenderTexture src, RenderTexture dest) {
    // 1) Ensure blur render target
        int w = src.width, h = src.height;
        if (blurRT == null || blurRT.width != w || blurRT.height != h)
            blurRT = new RenderTexture(w, h, 0);

        // 2) Generate blurRT
        Graphics.Blit(src, blurRT, blurMat);

        // 3) Send both textures into Shader Graph
        compositeMat.SetTexture("_BlurTex", blurRT);
        compositeMat.SetTexture("_MaskTex", fogController.maskTexture);

        // 4) Final composite pass
        Graphics.Blit(src, dest, compositeMat);
    Graphics.Blit(src, dest, compositeMat);
  }
}

